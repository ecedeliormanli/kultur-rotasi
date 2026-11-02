import L from 'leaflet';

// leaflet.heat is an older plugin that registers itself on a global `L`, so Leaflet is exposed there first
(globalThis as unknown as { L: typeof L }).L = L;
