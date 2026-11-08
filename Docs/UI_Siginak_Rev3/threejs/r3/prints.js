// Rev3 print textures (canvas): no words, only digits, ticks, marks and icons (words are TMP slots).
import * as THREE from 'three';
import { canvas } from './scene3.js';
import { rnd, noise } from '../rev2/lib.js';

export function T(c, o = {}) { const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; if (o.repeat) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(...o.repeat); } return t; }

// hazard stripes 45°, yellow #E3B23C / ink #1E1B16, with paint wear
export function hazardTex(w = 512, h = 128, o = {}) { const [c, x] = canvas(w, h); x.fillStyle = o.a || '#E3B23C'; x.fillRect(0, 0, w, h); x.fillStyle = o.b || '#1E1B16';
  const p = o.pitch || 64; for (let i = -h; i < w + h; i += p) { x.beginPath(); x.moveTo(i, h); x.lineTo(i + p / 2, h); x.lineTo(i + p / 2 + h, 0); x.lineTo(i + h, 0); x.fill(); }
  x.globalCompositeOperation = 'source-atop'; for (let i = 0; i < (o.chips ?? 70); i++) { x.fillStyle = `rgba(70,70,62,${0.3 + rnd() * 0.5})`; x.beginPath(); x.ellipse(rnd() * w, rnd() * h, 1 + rnd() * 5, 1 + rnd() * 3, rnd() * 3, 0, 7); x.fill(); }
  x.globalCompositeOperation = 'source-over'; noise(x, w, h, 16); return T(c, o); }
// glazed tile wall: 150 mm tiles, dark grout, crazing, grime toward the floor, a few cracked/missing
export function tileTex(wM, hM, o = {}) { const ppm = o.ppm || 400; const W = Math.round(wM * ppm), H = Math.round(hM * ppm); const [c, x] = canvas(W, H);
  const tile = 0.15 * ppm, g = Math.max(2, 0.004 * ppm); x.fillStyle = o.grout || '#4A4C44'; x.fillRect(0, 0, W, H);
  for (let ty = 0; ty < H; ty += tile) for (let tx = 0; tx < W; tx += tile) {
    const miss = rnd() < (o.missing ?? 0.012); const v = (rnd() - 0.5) * 14;
    if (miss) { x.fillStyle = '#5D605A'; x.fillRect(tx + g / 2, ty + g / 2, tile - g, tile - g); continue; }
    const base = new THREE.Color(o.base || '#B7BBA3'); const k = 1 + v / 255; x.fillStyle = `rgb(${base.r * 255 * k | 0},${base.g * 255 * k | 0},${base.b * 255 * k | 0})`;
    x.fillRect(tx + g / 2, ty + g / 2, tile - g, tile - g);
    const gr = x.createLinearGradient(tx, ty, tx + tile, ty + tile); gr.addColorStop(0, 'rgba(255,255,255,0.10)'); gr.addColorStop(1, 'rgba(0,0,0,0.08)'); x.fillStyle = gr; x.fillRect(tx + g / 2, ty + g / 2, tile - g, tile - g);
    if (rnd() < 0.06) { x.strokeStyle = 'rgba(40,40,36,0.55)'; x.lineWidth = 1.2; x.beginPath(); x.moveTo(tx + rnd() * tile, ty); x.lineTo(tx + rnd() * tile, ty + tile * (0.4 + rnd() * 0.6)); x.stroke(); }
  }
  // grime: darker toward the floor + vertical streaks
  const gg = x.createLinearGradient(0, H, 0, H * (1 - (o.grimeH ?? 0.45))); gg.addColorStop(0, 'rgba(30,32,26,0.62)'); gg.addColorStop(1, 'rgba(30,32,26,0)'); x.fillStyle = gg; x.fillRect(0, 0, W, H);
  for (let i = 0; i < (o.streaks ?? 26); i++) { const sx = rnd() * W, len = H * (0.2 + rnd() * 0.6); const s = x.createLinearGradient(0, 0, 0, len); s.addColorStop(0, 'rgba(40,44,34,0.28)'); s.addColorStop(1, 'rgba(40,44,34,0)'); x.fillStyle = s; x.fillRect(sx, 0, 2 + rnd() * 6, len); }
  noise(x, W, H, 8); return T(c); }
// clock dial: 12 hour ticks, 60 minute ticks, numerals 1–12 (digits allowed), maker ring
export function clockDialTex(o = {}) { const S = 1024; const [c, x] = canvas(S, S); const r = S / 2; x.fillStyle = o.bg || '#ECE3C8'; x.fillRect(0, 0, S, S);
  const g = x.createRadialGradient(r, r * 0.9, r * 0.1, r, r, r); g.addColorStop(0, 'rgba(255,255,255,0.12)'); g.addColorStop(1, 'rgba(60,50,30,0.22)'); x.fillStyle = g; x.fillRect(0, 0, S, S);
  x.translate(r, r); x.strokeStyle = '#1E1B16'; x.fillStyle = '#1E1B16';
  for (let i = 0; i < 60; i++) { const a = i * Math.PI / 30; const big = i % 5 === 0; x.lineWidth = big ? 14 : 5; x.beginPath(); x.moveTo(Math.sin(a) * r * 0.9, -Math.cos(a) * r * 0.9); x.lineTo(Math.sin(a) * r * (big ? 0.76 : 0.84), -Math.cos(a) * r * (big ? 0.76 : 0.84)); x.stroke(); }
  x.font = '700 92px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle';
  for (let i = 1; i <= 12; i++) { const a = i * Math.PI / 6; x.fillText(String(i), Math.sin(a) * r * 0.62, -Math.cos(a) * r * 0.62 + 4); }
  x.lineWidth = 3; x.beginPath(); x.arc(0, 0, r * 0.94, 0, 7); x.stroke(); x.setTransform(1, 0, 0, 1, 0, 0); noise(x, S, S, 8); return T(c); }
// time card: 8 × 10 numbered cells (1–80), header rule area left blank for TMP, punched holes drawn as dark circles
export function cardTex(o = {}) { const W = 900, H = 1840; const [c, x] = canvas(W, H); x.fillStyle = o.bg || '#EEE5CA'; x.fillRect(0, 0, W, H); noise(x, W, H, 7);
  const ppm = W / 0.09; const g = { cols: 10, rows: 8, pitch: [0.0076, 0.0105], first: [-0.0342, 0.184 - 0.070] };
  x.strokeStyle = 'rgba(60,70,90,0.55)'; x.lineWidth = 2; x.fillStyle = 'rgba(60,70,90,0.7)'; x.font = '700 22px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle';
  const holes = o.holes || 0;
  for (let k = 0; k < 80; k++) { const ci = k % 10, ri = Math.floor(k / 10); const cx = (g.first[0] + ci * g.pitch[0] + 0.045) * ppm, cy = (0.184 - (g.first[1] - ri * g.pitch[1])) * ppm;
    x.strokeRect(cx - g.pitch[0] * ppm / 2, cy - g.pitch[1] * ppm / 2, g.pitch[0] * ppm, g.pitch[1] * ppm);
    x.fillText(String(k + 1), cx, cy - 26);
    if (k < holes) { x.fillStyle = '#151310'; x.beginPath(); x.arc(cx, cy + 6, 0.0021 * ppm, 0, 7); x.fill(); x.fillStyle = 'rgba(78,59,114,0.85)'; x.font = '700 16px "Courier Prime"'; x.fillText(String(k + 1).padStart(3, '0'), cx, cy + 34); x.font = '700 22px "Courier Prime"'; x.fillStyle = 'rgba(60,70,90,0.7)'; }
  }
  // header rule (printed)
  x.fillStyle = '#353B30'; x.fillRect(20, 0.0605 * ppm, W - 40, 6); x.fillRect(20, 0.184 * ppm - 0.019 * ppm, W - 40, 3);
  return T(c); }
// VU gauge dial: arc scale 0–10, amber zone 8–10 (no red), ticks
export function gaugeTex(o = {}) { const W = 680, H = 480; const [c, x] = canvas(W, H); x.fillStyle = '#EFE6CC'; x.fillRect(0, 0, W, H);
  const cx = W / 2, cy = H * 0.86, R = H * 0.68; x.strokeStyle = '#1E1B16';
  x.lineWidth = 26; x.strokeStyle = 'rgba(214,150,40,0.85)'; x.beginPath(); x.arc(cx, cy, R - 30, -Math.PI / 2 + 40 * Math.PI / 180 * 0.6, -Math.PI / 2 + 50 * Math.PI / 180); x.stroke();
  x.strokeStyle = '#1E1B16'; x.lineWidth = 4; x.beginPath(); x.arc(cx, cy, R, -Math.PI / 2 - 50 * Math.PI / 180, -Math.PI / 2 + 50 * Math.PI / 180); x.stroke();
  x.font = '700 40px "Courier Prime"'; x.textAlign = 'center'; x.textBaseline = 'middle'; x.fillStyle = '#1E1B16';
  for (let i = 0; i <= 20; i++) { const a = (-50 + i * 5) * Math.PI / 180; const big = i % 2 === 0; x.lineWidth = big ? 6 : 3;
    x.beginPath(); x.moveTo(cx + Math.sin(a) * R, cy - Math.cos(a) * R); x.lineTo(cx + Math.sin(a) * (R - (big ? 40 : 24)), cy - Math.cos(a) * (R - (big ? 40 : 24))); x.stroke();
    if (i % 4 === 0) x.fillText(String(i / 2), cx + Math.sin(a) * (R + 34), cy - Math.cos(a) * (R + 34) + 4); }
  noise(x, W, H, 6); return T(c); }
// slider scale strip: ticks every 1 (0–10), half ticks, numerals 0/5/10
export function scaleTex(o = {}) { const W = 160, H = 1900; const [c, x] = canvas(W, H); x.fillStyle = '#E6DCC0'; x.fillRect(0, 0, W, H); x.fillStyle = '#1E1B16';
  for (let i = 0; i <= 20; i++) { const y = H * (0.95 - i * 0.045); const big = i % 2 === 0; x.fillRect(big ? 60 : 90, y - (big ? 5 : 3), W - (big ? 70 : 100), big ? 10 : 6);
    if (i % 10 === 0) { x.font = '700 64px "Courier Prime"'; x.textAlign = 'left'; x.textBaseline = 'middle'; x.fillText(String(i / 2), 4, y); } }
  noise(x, W, H, 6); return T(c); }
// enamel badge with gear icon
export function gearBadgeTex(o = {}) { const S = 512; const [c, x] = canvas(S, S); x.fillStyle = '#E8E2CF'; x.fillRect(0, 0, S, S); x.translate(S / 2, S / 2);
  x.fillStyle = o.ink || '#1E1B16'; const n = 10, ro = 168, ri = 132; x.beginPath();
  for (let i = 0; i < n * 2; i++) { const a0 = (i / (n * 2)) * Math.PI * 2, a1 = ((i + 1) / (n * 2)) * Math.PI * 2; const r = i % 2 ? ri : ro; x.arc(0, 0, r, a0 + 0.04, a1 - 0.04); }
  x.closePath(); x.fill(); x.fillStyle = '#E8E2CF'; x.beginPath(); x.arc(0, 0, 70, 0, 7); x.fill(); x.lineWidth = 10; x.strokeStyle = o.ink || '#1E1B16'; x.beginPath(); x.arc(0, 0, 224, 0, 7); x.stroke();
  x.setTransform(1, 0, 0, 1, 0, 0); noise(x, S, S, 6); return T(c); }
// concrete (board formed) — darker landing version
export function concrete(wM, hM, o = {}) { const ppm = o.ppm || 260; const W = Math.round(wM * ppm), H = Math.round(hM * ppm); const [c, x] = canvas(W, H);
  x.fillStyle = o.base || '#5C625D'; x.fillRect(0, 0, W, H); const b = 0.15 * ppm;
  for (let y = 0; y < H; y += b) { const v = (rnd() - 0.5) * 18; x.fillStyle = `rgba(${v > 0 ? 255 : 0},${v > 0 ? 255 : 0},${v > 0 ? 255 : 0},${Math.abs(v) / 255 * 1.4})`; x.fillRect(0, y, W, b); x.fillStyle = 'rgba(15,17,14,0.25)'; x.fillRect(0, y, W, 2); }
  noise(x, W, H, 12);
  for (let i = 0; i < (o.streaks ?? 24); i++) { const sx = rnd() * W, sy = rnd() * H * 0.4, len = (0.2 + rnd() * 0.8) * H * 0.6; const g = x.createLinearGradient(0, sy, 0, sy + len); g.addColorStop(0, 'rgba(18,22,18,0.28)'); g.addColorStop(1, 'rgba(18,22,18,0)'); x.fillStyle = g; x.fillRect(sx, sy, 2 + rnd() * 7, len); }
  for (const [sx, sy, r, a] of o.stains || []) { const g = x.createRadialGradient(sx * W, sy * H, 0, sx * W, sy * H, r * W); g.addColorStop(0, `rgba(20,26,20,${a})`); g.addColorStop(1, 'rgba(20,26,20,0)'); x.fillStyle = g; x.fillRect(0, 0, W, H); }
  return T(c, o); }
export function floorTex(wM, hM, o = {}) { const ppm = o.ppm || 200; const W = Math.round(wM * ppm), H = Math.round(hM * ppm); const [c, x] = canvas(W, H);
  x.fillStyle = o.base || '#3E423E'; x.fillRect(0, 0, W, H); noise(x, W, H, 26);
  for (let i = 0; i < 400; i++) { x.fillStyle = `rgba(${rnd() > 0.5 ? '200,200,190' : '10,10,8'},${0.05 + rnd() * 0.1})`; x.beginPath(); x.arc(rnd() * W, rnd() * H, 0.5 + rnd() * 2.2, 0, 7); x.fill(); }
  for (const [sx, sy, r, a] of o.stains || []) { const g = x.createRadialGradient(sx * W, sy * H, 0, sx * W, sy * H, r * W); g.addColorStop(0, `rgba(10,12,10,${a})`); g.addColorStop(1, 'rgba(10,12,10,0)'); x.fillStyle = g; x.fillRect(0, 0, W, H); }
  return T(c, o); }
