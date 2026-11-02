// Import order matters: Leaflet, then the global it needs, then the plugin that extends it
import L from 'leaflet';
import './leaflet-global';
import 'leaflet.heat';

export default L;
