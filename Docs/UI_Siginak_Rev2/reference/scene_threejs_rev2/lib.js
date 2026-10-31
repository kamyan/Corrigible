import * as THREE from 'three';

// Unity-style coordinates: x right, y up, z forward (toward the back wall).
export const U = (x, y, z) => new THREE.Vector3(x, y, -z);
export const D2R = Math.PI / 180;

export function mat(hex, rough = 0.8, metal = 0, extra = {}) {
  return new THREE.MeshStandardMaterial({ color: hex, roughness: rough, metalness: metal, ...extra });
}
export const M = {
  concrete: mat(0x8a8e84, 0.96),
  floor: mat(0x45463f, 0.95),
  ceiling: mat(0x2c2d29, 0.95),
  lino: mat(0x34503f, 0.72),
  steel: mat(0x4a5045, 0.55, 0.25),
  steelDark: mat(0x33382f, 0.6, 0.25),
  brass: mat(0xb8955a, 0.36, 0.85),
  brassDark: mat(0x8c6f3e, 0.45, 0.8),
  bakelite: mat(0x24180f, 0.38, 0.0),
  bakeliteRed: mat(0x5a1a12, 0.4, 0.0),
  olive: mat(0x4b5340, 0.62, 0.15),
  oliveLight: mat(0x5c6455, 0.6, 0.15),
  red: mat(0x9a1e14, 0.45, 0.05),
  chrome: mat(0xc8cccc, 0.22, 1.0),
  paper: mat(0xefe6d0, 0.92),
  paperWhite: mat(0xf4efe2, 0.9),
  cork: mat(0xa27b52, 0.97),
  wood: mat(0x6b4a2e, 0.7),
  woodLight: mat(0x9a7448, 0.7),
  woodPale: mat(0xc9a77a, 0.75),
  glassGreen: mat(0x1e5236, 0.18, 0.1),
  crtCase: mat(0x4f5446, 0.62, 0.05),
  rubber: mat(0x1c1c1a, 0.9),
  galv: mat(0x70756e, 0.5, 0.45),
  pipe: mat(0x3f4a3d, 0.55, 0.2),
  cream: mat(0xcfc6ae, 0.9),
  soil1: mat(0x5a4632, 0.98),
  soil2: mat(0x6e5a40, 0.98),
  soil3: mat(0x3f3328, 0.98),
  slab: mat(0x8a8a80, 0.9),
  rubble: mat(0x77746a, 0.95),
  enamel: mat(0xe9e6da, 0.35),
  blanket: mat(0x2e4a3a, 0.9),
  genGreen: mat(0x3e6b4a, 0.5, 0.2),
};

export function box(w, h, d, material, x, y, z, parent) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
  m.position.copy(U(x, y, z));
  m.castShadow = true; m.receiveShadow = true;
  if (parent) parent.add(m);
  return m;
}
// local box (already in parent's local three coords; z given Unity-style and negated)
export function lbox(parent, w, h, d, material, x, y, z, opt = {}) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
  m.position.set(x, y, -z);
  m.castShadow = opt.cast !== false; m.receiveShadow = true;
  if (opt.rx) m.rotation.x = opt.rx; if (opt.ry) m.rotation.y = opt.ry; if (opt.rz) m.rotation.z = opt.rz;
  parent.add(m);
  return m;
}
export function lcyl(parent, r, h, material, x, y, z, opt = {}) {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(opt.r2 ?? r, r, h, opt.seg ?? 24, 1, !!opt.open, opt.t0 ?? 0, opt.tl ?? Math.PI * 2), material);
  m.position.set(x, y, -z);
  m.castShadow = opt.cast !== false; m.receiveShadow = true;
  if (opt.rx) m.rotation.x = opt.rx; if (opt.ry) m.rotation.y = opt.ry; if (opt.rz) m.rotation.z = opt.rz;
  parent.add(m);
  return m;
}
export function lsphere(parent, r, material, x, y, z, opt = {}) {
  const m = new THREE.Mesh(new THREE.SphereGeometry(r, 18, 12), material);
  m.position.set(x, y, -z);
  m.castShadow = opt.cast !== false; m.receiveShadow = true;
  parent.add(m);
  return m;
}
// plane facing the chair (three +z); w,h in meters; texture optional
export function lplane(parent, w, h, material, x, y, z, opt = {}) {
  const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), material);
  m.position.set(x, y, -z);
  if (opt.rx) m.rotation.x = opt.rx; if (opt.ry) m.rotation.y = opt.ry; if (opt.rz) m.rotation.z = opt.rz;
  m.receiveShadow = opt.receive !== false; m.castShadow = !!opt.cast;
  parent.add(m);
  return m;
}
export function group(parent, x = 0, y = 0, z = 0, yawDeg = 0) {
  const g = new THREE.Group();
  g.position.set(x, y, -z);
  g.rotation.y = -yawDeg * D2R;
  parent.add(g);
  return g;
}

// ---------- canvas textures
export function canvas(w, h) {
  const c = document.createElement('canvas'); c.width = w; c.height = h;
  return [c, c.getContext('2d')];
}
export function tex(c, opt = {}) {
  const t = new THREE.CanvasTexture(c);
  t.colorSpace = THREE.SRGBColorSpace;
  t.anisotropy = 8;
  if (opt.repeat) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(opt.repeat[0], opt.repeat[1]); }
  return t;
}
let seed = 1234567;
export function rnd() { seed = (seed * 16807) % 2147483647; return (seed - 1) / 2147483646; }

export function noise(ctx, w, h, amt, alpha = 1) {
  const img = ctx.getImageData(0, 0, w, h); const d = img.data;
  for (let i = 0; i < d.length; i += 4) {
    const n = (rnd() - 0.5) * amt;
    d[i] += n; d[i + 1] += n; d[i + 2] += n;
  }
  ctx.putImageData(img, 0, 0);
}

export async function loadFonts() {
  const fams = [
    ['Courier Prime', '400'], ['Courier Prime', '700'],
    ['Libre Franklin', '400'], ['Libre Franklin', '600'], ['Libre Franklin', '700'],
    ['Kalam', '700'], ['Big Shoulders Stencil Display', '800'],
  ];
  await Promise.all(fams.map(([f, w]) => document.fonts.load(`${w} 32px "${f}"`, 'AĞIİŞÇÖÜğışçöü09')));
}

export function img(src) {
  return new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
}

// fit text into width by shrinking letter spacing / size
export function textFit(ctx, s, x, y, maxW, font, sizePx, opt = {}) {
  let size = sizePx;
  ctx.font = font.replace('{s}', size + 'px');
  while (ctx.measureText(s).width > maxW && size > 6) { size -= 1; ctx.font = font.replace('{s}', size + 'px'); }
  ctx.fillText(s, x, y);
  return size;
}
