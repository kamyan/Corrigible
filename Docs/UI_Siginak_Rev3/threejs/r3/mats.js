// Rev3 material table: key -> URP Lit values. Single source for the model library and for tokens 1.3.
// hex = base colour (sRGB) · rough = 1 − smoothness · metal = metallic · emissive/ei = emission colour and intensity
// alpha < 1 = URP Lit Transparent. tex = optional texture note (≤1024 px). Families may add keys with defMat().
import BASE from './mats_base.js';

export const MATS = { ...BASE };
export const NEW_KEYS = new Set();
export function defMat(key, v) {
  if (MATS[key] && !NEW_KEYS.has(key)) return MATS[key];
  MATS[key] = v; NEW_KEYS.add(key); return v;
}

// ---- R3 shared keys (room, menu, cabinet). Painted steel is dielectric (metal ≤ 0.1); bare metal shows at worn edges and shafts.
const R3 = {
  paintOlive:      { hex: '#4A5140', rough: 0.6, metal: 0.05, note: 'boyalı sac (dolap, konsol, saat gövdesi)' },
  paintOliveDark:  { hex: '#353B30', rough: 0.62, metal: 0.05, note: 'kapı kanadı, kasa, çerçeve' },
  paintGreyGreen:  { hex: '#5D665C', rough: 0.58, metal: 0.05, note: 'puantaj saati, kart rafı' },
  paintCream:      { hex: '#D6CDB4', rough: 0.55, metal: 0, note: 'emaye panel, ölçek zemini' },
  paintBlack:      { hex: '#20211E', rough: 0.5, metal: 0.05, note: 'lamba başlığı, ızgara' },
  paintHazard:     { hex: '#E3B23C', rough: 0.6, metal: 0, tex: 'tx_hazard (45° şerit #1E1B16, 1024×128)', note: 'tehlike bandı' },
  steelBare:       { hex: '#8E918C', rough: 0.38, metal: 0.9, note: 'mil, cıvata, aşınmış kenar' },
  steelCast:       { hex: '#4E524D', rough: 0.55, metal: 0.8, note: 'döküm menteşe, mandal' },
  castIron:        { hex: '#3A3B37', rough: 0.72, metal: 0.6, note: 'dış çark, döküm kaide' },
  brassWorn:       { hex: '#A0814C', rough: 0.48, metal: 0.8, note: 'el değen pirinç' },
  copper:          { hex: '#B8682C', rough: 0.42, metal: 0.9, note: 'bıçaklı şalter bıçağı' },
  bakeliteBlack:   { hex: '#181412', rough: 0.34, metal: 0, note: 'düğme, kol başı' },
  slate:           { hex: '#2F3431', rough: 0.68, metal: 0, note: 'şalter tabanı (arduvaz)' },
  rubberGasket:    { hex: '#141413', rough: 0.85, metal: 0, note: 'conta' },
  glassClear:      { hex: '#DCE6E3', rough: 0.05, metal: 0, alpha: 0.18, note: 'saat camı, gösterge camı (Transparent)' },
  dialCream:       { hex: '#ECE3C8', rough: 0.6, metal: 0, tex: 'tx_dials (ölçek ve rakam, kelime yok)', note: 'kadran zemini' },
  cardStock:       { hex: '#EEE5CA', rough: 0.9, metal: 0, tex: 'tx_card (80 hücre ızgarası, rakamlar)', note: 'vardiya kartı' },
  cardBlank:       { hex: '#E6DCBE', rough: 0.9, metal: 0, note: 'boş kart destesi' },
  punchHole:       { hex: '#151310', rough: 1, metal: 0, note: 'zımba deliği (kartın arkası karanlık)' },
  tileGlazed:      { hex: '#B7BBA3', rough: 0.32, metal: 0, tex: 'tx_tile150 (15 cm fayans, derz #4F5148, çatlak)', note: 'sahanlık fayansı' },
  tileGlazedDirty: { hex: '#8E9282', rough: 0.45, metal: 0, tex: 'tx_tile150', note: 'alt sıra, kirli' },
  concreteDark:    { hex: '#5C625D', rough: 0.95, metal: 0, tex: 'tx_concrete (kalıp izi, nem)', note: 'sahanlık üst duvarı, tavan' },
  concreteFloorWet:{ hex: '#3B3F3B', rough: 0.55, metal: 0, note: 'ıslak beton zemin' },
  puddle:          { hex: '#1E2220', rough: 0.08, metal: 0, note: 'su birikintisi (ayrı düzlem)' },
  stencilPaint:    { hex: '#D9D2BF', rough: 0.8, metal: 0, alpha: 0.92, note: 'şablon boya (TMP, Big Shoulders Stencil)' },
  lampGlassCold:   { hex: '#E8F0EE', rough: 0.25, metal: 0, emissive: '#D8E6E2', ei: 2.2, note: 'sahanlık ampulü' },
  lampGlassWarm:   { hex: '#FFF0C8', rough: 0.3, metal: 0, emissive: '#FFD27A', ei: 2.5, note: 'oda ampulü' },
  jewelAmberOn:    { hex: '#FFB347', rough: 0.2, metal: 0, emissive: '#FF9A2A', ei: 2.4, note: 'amber mücevher lamba yanık' },
  jewelAmberOff:   { hex: '#4A2F12', rough: 0.25, metal: 0, note: 'amber mücevher lamba sönük' },
  woodVarnish:     { hex: '#5E4128', rough: 0.45, metal: 0, tex: 'tx_wood (lif)', note: 'cilalı masa ahşabı' },
  woodRaw:         { hex: '#8A6A44', rough: 0.8, metal: 0, note: 'ham çıta, sandık' },
  linoDesk:        { hex: '#2F4A3B', rough: 0.6, metal: 0, note: 'masa üstü linolyum' },
  feltBlotter:     { hex: '#3E4A3A', rough: 0.95, metal: 0, note: 'harita altlığı keçe' },
};
for (const [k, v] of Object.entries(R3)) defMat(k, v);

// ---- three.js material cache (preview / renders)
import * as THREE from 'three';
const cache = new Map();
export function material(key) {
  if (cache.has(key)) return cache.get(key);
  const v = MATS[key];
  if (!v) throw new Error('unknown material key: ' + key);
  const m = new THREE.MeshStandardMaterial({ color: v.hex, roughness: v.rough ?? 0.8, metalness: v.metal ?? 0 });
  if (v.emissive) { m.emissive = new THREE.Color(v.emissive); m.emissiveIntensity = v.ei ?? 1; }
  if (v.alpha != null && v.alpha < 1) { m.transparent = true; m.opacity = v.alpha; m.depthWrite = v.alpha > 0.5; }
  m.name = key;
  cache.set(key, m);
  return m;
}
export function setMaterialOverride(key, m) { m.name = key; cache.set(key, m); }
