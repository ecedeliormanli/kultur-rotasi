#!/usr/bin/env bash
# Publishes the Kültür Rotası database in GeoServer as OGC services (WMS, WFS).
#
# Run from the repository root after:
#   docker compose --profile gis up -d    (SQL Server + GeoServer)
#   dotnet run --project backend/src/KulturRotasi.Api   (once, so migrations create the map views)
#
# Safe to run again: everything that already exists is updated, not duplicated.
# Everything goes through GeoServer's REST API, so the configuration is code, not clicks in the web UI.
set -euo pipefail

cd "$(dirname "$0")/.."
[ -f .env ] && set -a && . ./.env && set +a

: "${MSSQL_SA_PASSWORD:?Set MSSQL_SA_PASSWORD in .env}"
GEOSERVER_URL="${GEOSERVER_URL:-http://localhost:8080/geoserver}"
GS_USER="${GEOSERVER_ADMIN_USER:-admin}"
GS_PASS="${GEOSERVER_ADMIN_PASSWORD:-ChangeMe_Ge0server!}"
DB_READER_PASSWORD="${GEOSERVER_DB_PASSWORD:-ChangeMe_Read0nly!}"
WORKSPACE=kultur
STORE=kulturrotasi-db

step() { printf '\n\033[1;34m==> %s\033[0m\n' "$*"; }

# curl against the REST API; prints the HTTP status code only
rest() {
  local method=$1 path=$2 content_type=${3:-application/json} data=${4:-}
  local args=(-s -o /dev/null -w '%{http_code}' -u "$GS_USER:$GS_PASS" -X "$method" -H "Content-Type: $content_type")
  if [ -n "$data" ]; then
    if [ "${data:0:1}" = "@" ]; then args+=(--data-binary "$data"); else args+=(-d "$data"); fi
  fi
  curl "${args[@]}" "$GEOSERVER_URL/rest$path"
}

# The .json suffix matters: without it GeoServer may answer 500 for styles (content negotiation)
exists() { [ "$(rest GET "$1.json")" = "200" ]; }

expect() {
  local got=$1; shift
  for ok in "$@"; do [ "$got" = "$ok" ] && return 0; done
  echo "Unexpected HTTP status $got" >&2
  exit 1
}

sql() {
  docker compose exec -T -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" db \
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -Q "SET NOCOUNT ON; $1"
}

step "Waiting for SQL Server and the map views"
for i in $(seq 1 30); do
  if [ "$(sql "SELECT COUNT(*) FROM KulturRotasi.sys.views WHERE name IN ('MapPlaces','MapProvinces')" 2>/dev/null | tr -d '[:space:]')" = "2" ]; then
    break
  fi
  [ "$i" = 30 ] && { echo "Views not found. Run the API once so the migrations create them." >&2; exit 1; }
  sleep 2
done

step "Read-only database login for GeoServer (geoserver_reader)"
# GeoServer only reads; least privilege means a leaked GeoServer config cannot change data.
escaped=${DB_READER_PASSWORD//\'/\'\'}
sql "
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'geoserver_reader')
  CREATE LOGIN geoserver_reader WITH PASSWORD = '$escaped', CHECK_POLICY = OFF;
ELSE
  ALTER LOGIN geoserver_reader WITH PASSWORD = '$escaped';
USE KulturRotasi;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'geoserver_reader')
  CREATE USER geoserver_reader FOR LOGIN geoserver_reader;
ALTER ROLE db_datareader ADD MEMBER geoserver_reader;"

step "Primary key metadata for the views"
# Views have no primary key, so GeoServer would invent random feature ids (places.fid-5ae8...).
# This GeoTools metadata table tells it which column is the key, giving stable ids like places.5.
sql "
USE KulturRotasi;
IF SCHEMA_ID('geoserver') IS NULL EXEC('CREATE SCHEMA geoserver');
IF OBJECT_ID('geoserver.gt_pk_metadata') IS NULL
  CREATE TABLE geoserver.gt_pk_metadata (
    table_schema VARCHAR(32) NOT NULL, table_name VARCHAR(32) NOT NULL, pk_column VARCHAR(32) NOT NULL,
    pk_column_idx INT, pk_policy VARCHAR(32), pk_sequence VARCHAR(64),
    CONSTRAINT gt_pk_metadata_uq UNIQUE (table_schema, table_name, pk_column));
MERGE geoserver.gt_pk_metadata AS t
USING (VALUES ('dbo','MapPlaces','Id'), ('dbo','MapProvinces','PlateCode')) AS s(table_schema, table_name, pk_column)
  ON t.table_schema = s.table_schema AND t.table_name = s.table_name
WHEN MATCHED THEN UPDATE SET pk_column = s.pk_column, pk_policy = 'assigned'
WHEN NOT MATCHED THEN INSERT (table_schema, table_name, pk_column, pk_column_idx, pk_policy)
  VALUES (s.table_schema, s.table_name, s.pk_column, NULL, 'assigned');"

step "Waiting for GeoServer at $GEOSERVER_URL"
for i in $(seq 1 60); do
  [ "$(rest GET /about/version.json)" = "200" ] && break
  [ "$i" = 60 ] && { echo "GeoServer did not answer. Check: docker compose logs geoserver" >&2; exit 1; }
  sleep 3
done

step "Workspace '$WORKSPACE'"
if ! exists "/workspaces/$WORKSPACE"; then
  expect "$(rest POST /workspaces application/json "{\"workspace\":{\"name\":\"$WORKSPACE\"}}")" 201
fi
# Its own namespace URI, so WFS feature types are named kultur:places
expect "$(rest PUT "/namespaces/$WORKSPACE" application/json \
  "{\"namespace\":{\"prefix\":\"$WORKSPACE\",\"uri\":\"https://github.com/ecedeliormanli/kultur-rotasi\"}}")" 200

step "SQL Server data store '$STORE'"
# Inside the Docker network the database host is the compose service name "db".
store_json=$(cat <<JSON
{"dataStore":{"name":"$STORE","type":"Microsoft SQL Server","enabled":true,
 "connectionParameters":{"entry":[
  {"@key":"dbtype","\$":"sqlserver"},
  {"@key":"host","\$":"db"},
  {"@key":"port","\$":"1433"},
  {"@key":"database","\$":"KulturRotasi"},
  {"@key":"schema","\$":"dbo"},
  {"@key":"user","\$":"geoserver_reader"},
  {"@key":"passwd","\$":"$DB_READER_PASSWORD"},
  {"@key":"Expose primary keys","\$":"true"},
  {"@key":"Primary key metadata table","\$":"geoserver.gt_pk_metadata"}
 ]}}}
JSON
)
if exists "/workspaces/$WORKSPACE/datastores/$STORE"; then
  expect "$(rest PUT "/workspaces/$WORKSPACE/datastores/$STORE" application/json "$store_json")" 200
else
  expect "$(rest POST "/workspaces/$WORKSPACE/datastores" application/json "$store_json")" 201
fi

step "Styles (SLD)"
for style in places provinces; do
  if exists "/workspaces/$WORKSPACE/styles/$style"; then
    expect "$(rest PUT "/workspaces/$WORKSPACE/styles/$style" application/vnd.ogc.sld+xml "@geoserver/styles/$style.sld")" 200
  else
    expect "$(rest POST "/workspaces/$WORKSPACE/styles?name=$style" application/vnd.ogc.sld+xml "@geoserver/styles/$style.sld")" 201
  fi
done

step "Layers"
publish() {
  local name=$1 view=$2 title=$3 abstract=$4
  local json="{\"featureType\":{\"name\":\"$name\",\"nativeName\":\"$view\",\"title\":\"$title\",\"abstract\":\"$abstract\",
    \"srs\":\"EPSG:4326\",\"projectionPolicy\":\"FORCE_DECLARED\",
    \"nativeBoundingBox\":{\"minx\":25.5,\"maxx\":45,\"miny\":35.8,\"maxy\":42.2,\"crs\":\"EPSG:4326\"},
    \"latLonBoundingBox\":{\"minx\":25.5,\"maxx\":45,\"miny\":35.8,\"maxy\":42.2,\"crs\":\"EPSG:4326\"}}}"
  local base="/workspaces/$WORKSPACE/datastores/$STORE/featuretypes"
  if exists "$base/$name"; then
    expect "$(rest PUT "$base/$name" application/json "$json")" 200
  else
    expect "$(rest POST "$base" application/json "$json")" 201
  fi
  expect "$(rest PUT "/layers/$WORKSPACE:$name" application/json \
    "{\"layer\":{\"defaultStyle\":{\"name\":\"$name\",\"workspace\":\"$WORKSPACE\"}}}")" 200
  echo "  $WORKSPACE:$name ← dbo.$view"
}
publish places MapPlaces "Cultural places" "Museums, archaeological sites, monuments and palaces in Türkiye"
publish provinces MapProvinces "Places per province" "Province boundaries with the number of cultural places inside each"

step "Done"
cat <<EOF
  Web UI           $GEOSERVER_URL/web/   (user: $GS_USER)
  WMS capabilities $GEOSERVER_URL/$WORKSPACE/wms?service=WMS&request=GetCapabilities
  WFS capabilities $GEOSERVER_URL/$WORKSPACE/wfs?service=WFS&request=GetCapabilities
  Map image        $GEOSERVER_URL/$WORKSPACE/wms?service=WMS&version=1.1.1&request=GetMap&layers=$WORKSPACE:provinces,$WORKSPACE:places&bbox=25.5,35.8,45,42.2&width=1000&height=330&srs=EPSG:4326&format=image/png
  Features (JSON)  $GEOSERVER_URL/$WORKSPACE/wfs?service=WFS&version=2.0.0&request=GetFeature&typeNames=$WORKSPACE:places&outputFormat=application/json&count=5
More examples: docs/geoserver.md
EOF
