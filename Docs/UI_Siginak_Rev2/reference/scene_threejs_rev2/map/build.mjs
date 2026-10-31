// 12 game regions in the Sade map frame (Miller, 6.25 px/deg, lambda0 = 24.7E, equator y = 639, 1920x1080).
import fs from 'node:fs';
import * as d3 from 'd3-geo';
import { geoMiller } from 'd3-geo-projection';
import * as topojson from 'topojson-client';
const topo = JSON.parse(fs.readFileSync(new URL('../../node_modules/world-atlas/countries-110m.json', import.meta.url)));
const geoms = topo.objects.countries.geometries;
{ const fr = geoms.find(g => g.id === '250'); const keep = [], guiana = [];
  for (const poly of fr.arcs) { const f = topojson.feature(topo, { type: 'Polygon', arcs: poly }); (d3.geoCentroid(f)[0] < -30 ? guiana : keep).push(poly); }
  fr.arcs = keep; geoms.push({ type: 'MultiPolygon', arcs: guiana, id: '254', properties: { name: 'French Guiana' } }); }
const R = {
  na: ['124', '840', '304', '484', '320', '084', '340', '222', '558', '188', '591', '192', '332', '214', '388', '630', '044', '780'],
  sa: ['170', '862', '328', '740', '254', '218', '604', '076', '068', '600', '858', '032', '152', '238'],
  weu: ['826', '372', '250', '724', '620', '056', '528', '442', '276', '756', '040', '380', '208', '578', '752', '246', '352'],
  eeu: ['616', '203', '703', '348', '705', '191', '070', '688', '499', 'Kosovo', '807', '008', '300', '100', '642', '498', '804', '112', '440', '428', '233', '196', 'N. Cyprus'],
  ru: ['643', '398', '860', '795', '417', '762', '268', '051', '031'],
  me: ['792', '760', '422', '376', '275', '400', '368', '364', '682', '887', '512', '784', '634', '414', '818', '434', '788', '012', '504', '732', '729', '004'],
  af: [], in: ['356', '586', '050', '524', '064', '144'], cn: ['156', '496', '158'], jk: ['392', '410', '408'],
  sea: ['104', '764', '418', '116', '704', '458', '360', '608', '096', '626', '598'], oc: ['036', '554', '242', '090', '548', '540'],
};
const EXCLUDE = new Set(['010', '260']);
const regionOf = new Map(); for (const [k, ids] of Object.entries(R)) for (const id of ids) regionOf.set(id, k);
const reg = g => EXCLUDE.has(g.id) ? null : (regionOf.get(g.id) || regionOf.get(g.properties.name) || 'af');
const proj = geoMiller().rotate([-24.7, 0]).scale(358.1).translate([960, 639]).precision(0.3);
const path = d3.geoPath(proj).digits(1);
const out = { regions: {}, outlines: {}, centroids: {}, bbox: {} };
for (const k of Object.keys(R)) {
  const merged = topojson.merge(topo, geoms.filter(g => reg(g) === k));
  out.regions[k] = path(merged);
  const big = merged.coordinates.filter(poly => path.area({ type: 'Polygon', coordinates: poly }) > 1800);
  out.outlines[k] = path({ type: 'MultiPolygon', coordinates: big });
  out.centroids[k] = path.centroid(merged).map(v => Math.round(v));
  out.bbox[k] = path.bounds(merged).map(p => p.map(v => Math.round(v)));
}
const all = { type: 'GeometryCollection', geometries: geoms.filter(g => !EXCLUDE.has(g.id)) };
out.regionBorders = path(topojson.mesh(topo, all, (a, b) => a !== b && reg(a) !== reg(b)));
out.countryBorders = path(topojson.mesh(topo, all, (a, b) => a !== b && reg(a) === reg(b)));
out.graticule = path(d3.geoGraticule().step([20, 20]).extentMajor([[-180, -80], [180, 84]])());
const lat = (phi) => path({ type: 'LineString', coordinates: Array.from({ length: 181 }, (_, i) => -180 + 2 * i).map(l => [l, phi]) });
out.equator = lat(0); out.tropics = lat(23.44) + lat(-23.44);
const P = (lon, la) => proj([lon, la]).map(v => Math.round(v));
out.probe = { paris: P(2.35, 48.85), kyiv: P(30.5, 50.45), warsaw: P(21, 52.2), moscow: P(37.6, 55.75), tokyo: P(139.7, 35.7), seoul: P(127, 37.5), beijing: P(116.4, 39.9), delhi: P(77.2, 28.6), jakarta: P(106.8, -6.2), sydney: P(151.2, -33.9), nyc: P(-74, 40.7), sao: P(-46.6, -23.5), cairo: P(31.2, 30), lagos: P(3.4, 6.5), riyadh: P(46.7, 24.7), bangkok: P(100.5, 13.75), manila: P(121, 14.6), wellington: P(174.8, -41.3), vancouver: P(-123.1, 49.3), mexico: P(-99.1, 19.4), capetown: P(18.4, -33.9), madrid: P(-3.7, 40.4), london: P(-0.13, 51.5), istanbul: P(29, 41) };
fs.writeFileSync(new URL('./paths.json', import.meta.url), JSON.stringify(out));
console.log('centroids', JSON.stringify(out.centroids));
console.log('bbox', JSON.stringify(out.bbox));
console.log('probe', JSON.stringify(out.probe));
