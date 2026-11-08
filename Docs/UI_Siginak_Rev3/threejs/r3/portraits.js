// Portrait system (supplement 2026-11-05, R16): "kimlik çizimi" — a front-facing ink bust in the style of an ID-card pen sketch.
// One generator for everyone (humans and robots alike): the same parts, strokes and shading. Nothing in a portrait can hint at a
// robot. Build follows the body profile (shoulder width, neck, jaw); identity comes from face shape, hair, brows, facial hair,
// glasses, age lines and collar. portraitSVG(spec, o) → SVG string (viewBox 0 0 300 340). Works in Node and in the browser.

const INK = '#1E1B16', PAPER = '#EFE6D0';
const f = (v) => (+v).toFixed(1);

// build per body profile (supplement: dayanıklı/yavaş, çevik/narin, güçlü/temkinli, dengeli)
export const BUILD = {
  dayanikli: { sw: 128, nw: 52, slope: 22, jw: 0.86, hw: 60 },
  cevik:     { sw: 100, nw: 36, slope: 30, jw: 0.70, hw: 53 },
  guclu:     { sw: 134, nw: 50, slope: 16, jw: 0.84, hw: 58 },
  dengeli:   { sw: 114, nw: 42, slope: 24, jw: 0.76, hw: 55 },
};

// the ten people of community.json
export const PORTRAITS = {
  p_cem:    { profile: 'cevik',     sex: 'm', age: 36, face: 'long',   hair: 'sidepart', tone: 'dark',  brow: 'straight', mouth: 'thin',  fh: 'mustache',   collar: 'uniform' },
  p_ipek:   { profile: 'guclu',     sex: 'f', age: 41, face: 'square', hair: 'bun',      tone: 'dark',  brow: 'arched',   mouth: 'full',  collar: 'sweater' },
  p_zeynep: { profile: 'cevik',     sex: 'f', age: 31, face: 'oval',   hair: 'long',     tone: 'dark',  brow: 'soft',     mouth: 'full',  glasses: 'round', collar: 'shirt' },
  p_eren:   { profile: 'dayanikli', sex: 'm', age: 52, face: 'round',  hair: 'receding', tone: 'grey',  brow: 'heavy',    mouth: 'thin',  fh: 'beard',      collar: 'jacket' },
  p_nermin: { profile: 'guclu',     sex: 'f', age: 56, face: 'round',  hair: 'scarf',    tone: 'dark',  brow: 'soft',     mouth: 'thin',  collar: 'sweater', lines: 2 },
  p_suna:   { profile: 'dengeli',   sex: 'f', age: 45, face: 'square', hair: 'crop',     tone: 'dark',  brow: 'straight', mouth: 'thin',  collar: 'overall', lines: 1 },
  p_deniz:  { profile: 'cevik',     sex: 'm', age: 20, face: 'oval',   hair: 'curly',    tone: 'dark',  brow: 'soft',     mouth: 'full',  collar: 'sweater' },
  p_selin:  { profile: 'dengeli',   sex: 'f', age: 34, face: 'oval',   hair: 'ponytail', tone: 'light', brow: 'arched',   mouth: 'full',  collar: 'shirt' },
  p_aylin:  { profile: 'dengeli',   sex: 'f', age: 28, face: 'heart',  hair: 'bob',      tone: 'dark',  brow: 'arched',   mouth: 'full',  collar: 'jacket' },
  p_rauf:   { profile: 'guclu',     sex: 'm', age: 46, face: 'square', hair: 'cap',      tone: 'dark',  brow: 'heavy',    mouth: 'thin',  fh: 'mustache',   collar: 'jacket', lines: 1 },
};

// ---------------------------------------------------------------- geometry helpers
function smoothPath(pts, closed = true) { // Catmull-Rom → cubic Bézier
  const n = pts.length; let d = `M${f(pts[0][0])} ${f(pts[0][1])}`;
  const P = (i) => pts[closed ? (i + n) % n : Math.max(0, Math.min(n - 1, i))];
  const last = closed ? n : n - 1;
  for (let i = 0; i < last; i++) { const p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
    const c1 = [p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6], c2 = [p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6];
    d += ` C${f(c1[0])} ${f(c1[1])} ${f(c2[0])} ${f(c2[1])} ${f(p2[0])} ${f(p2[1])}`; }
  return d + (closed ? ' Z' : ''); }
const mirror = (pts, cx) => pts.map(([x, y]) => [2 * cx - x, y]);

// head outline points (clockwise from the top), given centre, half width, half height, jaw factor and face shape
function headPts(cx, cy, hw, hh, jw, face) {
  const pts = []; const N = 18; const sq = face === 'square' ? 0.72 : face === 'round' ? 0.95 : 0.86; const top = face === 'long' ? 1.04 : 0.98;
  for (let i = 0; i <= N; i++) { const a = -Math.PI / 2 + (i / N) * Math.PI; const s = Math.sin(a), c = Math.cos(a);
    let x, y;
    if (s < 0) { x = hw * Math.pow(Math.abs(c), sq * 0.9) ; y = hh * top * s; }
    else { const taper = face === 'heart' ? 1 - (1 - jw * 0.9) * Math.pow(s, 1.2) : 1 - (1 - jw) * Math.pow(s, face === 'square' ? 2.6 : 1.6); x = hw * Math.pow(c, sq) * taper; y = hh * s; }
    pts.push([cx + x, cy + y]); }
  const left = pts.slice(1, -1).reverse().map(([x, y]) => [2 * cx - x, y]);
  return pts.concat(left); }

// hatch pattern defs (diagonal and cross)
function defs(id, tone) {
  const dense = tone === 'dark' ? 3.2 : tone === 'grey' ? 5.2 : 6.5;
  return `<defs>
<pattern id="${id}h" width="${dense}" height="${dense}" patternUnits="userSpaceOnUse" patternTransform="rotate(38)"><line x1="0" y1="0" x2="0" y2="${dense}" stroke="${INK}" stroke-width="1.15"/></pattern>
<pattern id="${id}x" width="${dense}" height="${dense}" patternUnits="userSpaceOnUse" patternTransform="rotate(-38)"><line x1="0" y1="0" x2="0" y2="${dense}" stroke="${INK}" stroke-width="${tone === 'dark' ? 1.0 : 0.7}"/></pattern>
<pattern id="${id}s" width="4.2" height="4.2" patternUnits="userSpaceOnUse" patternTransform="rotate(52)"><line x1="0" y1="0" x2="0" y2="4.2" stroke="${INK}" stroke-width="0.8"/></pattern>
<pattern id="${id}c" width="5" height="5" patternUnits="userSpaceOnUse" patternTransform="rotate(-20)"><line x1="0" y1="0" x2="0" y2="5" stroke="${INK}" stroke-width="0.75"/></pattern>
<pattern id="${id}d" width="3.4" height="3.4" patternUnits="userSpaceOnUse"><circle cx="1.7" cy="1.7" r="0.55" fill="${INK}"/></pattern>
</defs>`; }

// ---------------------------------------------------------------- main
export function portraitSVG(spec, o = {}) {
  const id = (o.id || 'p') + Math.random().toString(36).slice(2, 7);
  const B = BUILD[spec.profile] || BUILD.dengeli;
  const W = 300, H = 340, cx = 150, cy = 132;
  const hw = B.hw * (spec.sex === 'f' ? 0.95 : 1), hh = hw * (spec.face === 'long' ? 1.38 : spec.face === 'round' ? 1.18 : 1.28);
  const jw = spec.face === 'heart' ? 0.62 : B.jw * (spec.sex === 'f' ? 0.94 : 1);
  const sw = B.sw * (spec.sex === 'f' ? 0.92 : 1), nw = B.nw * (spec.sex === 'f' ? 0.86 : 1);
  const chinY = cy + hh, neckTop = cy + hh * 0.72, shY = chinY + 26, shDrop = B.slope;
  const stroke = `stroke="${INK}" stroke-linecap="round" stroke-linejoin="round" fill="none"`;
  const S = [];
  const vb = o.crop || [0, 0, W, H];
  S.push(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="${vb.join(' ')}" width="${o.w || vb[2]}" height="${o.h || vb[3]}" preserveAspectRatio="xMidYMid slice">`);
  S.push(defs(id, spec.tone));
  if (o.bg !== false) S.push(`<rect width="${W}" height="${H}" fill="${o.bg || PAPER}"/>`);
  // background: faint vertical ID-photo lines
  if (o.lines !== false) { let l = ''; for (let x = 8; x < W; x += 9) l += `M${x} 0V${H}`; S.push(`<path d="${l}" stroke="${INK}" stroke-width="0.5" opacity="0.10"/>`); }

  // ---- back hair (long / ponytail / bob / scarf back) — behind body and face
  const hairFill = `url(#${id}h)`, hairX = `url(#${id}x)`;
  const hairPaint = (d, extra = '') => `<path d="${d}" fill="${PAPER}"/><path d="${d}" fill="${hairFill}"/>${spec.tone === 'dark' ? `<path d="${d}" fill="${hairX}"/>` : ''}<path d="${d}" ${stroke} stroke-width="2.2" ${extra}/>`;
  if (spec.hair === 'long') {
    const d = smoothPath([[cx, cy - hh * 1.08], [cx + hw * 1.12, cy - hh * 0.62], [cx + hw * 1.25, cy + hh * 0.2], [cx + hw * 1.32, chinY + 40], [cx + hw * 1.05, chinY + 64], [cx + hw * 0.7, chinY + 30],
      [cx - hw * 0.7, chinY + 30], [cx - hw * 1.05, chinY + 64], [cx - hw * 1.32, chinY + 40], [cx - hw * 1.25, cy + hh * 0.2], [cx - hw * 1.12, cy - hh * 0.62]]);
    S.push(hairPaint(d)); }
  if (spec.hair === 'bob') {
    const d = smoothPath([[cx, cy - hh * 1.1], [cx + hw * 1.15, cy - hh * 0.6], [cx + hw * 1.24, cy + hh * 0.35], [cx + hw * 1.08, chinY - 2], [cx + hw * 0.55, chinY - 14],
      [cx - hw * 0.55, chinY - 14], [cx - hw * 1.08, chinY - 2], [cx - hw * 1.24, cy + hh * 0.35], [cx - hw * 1.15, cy - hh * 0.6]]);
    S.push(hairPaint(d)); }
  // ponytail: tied back — invisible from the front except the centre part (drawn with the front hair)
  if (spec.hair === 'scarf') { // the fall of the headscarf onto the shoulders
    const d = smoothPath([[cx - hw * 1.32, cy - hh * 0.2], [cx - hw * 1.42, chinY + 10], [cx - sw * 0.82, shY + shDrop + 26], [cx, shY + 46], [cx + sw * 0.82, shY + shDrop + 26], [cx + hw * 1.42, chinY + 10], [cx + hw * 1.32, cy - hh * 0.2], [cx, cy - hh * 1.24]]);
    S.push(`<path d="${d}" fill="${PAPER}"/><path d="${d}" fill="url(#${id}c)" opacity="0.55"/><path d="${d}" ${stroke} stroke-width="2.2"/>`); }

  // ---- body: shoulders and collar
  const bodyD = `M${f(cx - nw / 2)} ${f(neckTop)} L${f(cx - nw / 2)} ${f(shY - 6)} C${f(cx - nw / 2 - 18)} ${f(shY + 2)} ${f(cx - sw + 30)} ${f(shY + shDrop - 6)} ${f(cx - sw + 4)} ${f(shY + shDrop + 14)} L${f(cx - sw - 8)} ${H + 2} L${f(cx + sw + 8)} ${H + 2} L${f(cx + sw - 4)} ${f(shY + shDrop + 14)} C${f(cx + sw - 30)} ${f(shY + shDrop - 6)} ${f(cx + nw / 2 + 18)} ${f(shY + 2)} ${f(cx + nw / 2)} ${f(shY - 6)} L${f(cx + nw / 2)} ${f(neckTop)} Z`;
  S.push(`<path d="${bodyD}" fill="${PAPER}"/>`);
  // garment tone (light hatch on clothing, darker on jacket)
  const garmentTone = { jacket: `url(#${id}h)`, uniform: `url(#${id}s)`, overall: `url(#${id}s)`, sweater: `url(#${id}c)`, shirt: null }[spec.collar];
  const clothD = `M${f(cx - sw - 8)} ${H + 2} L${f(cx - sw + 4)} ${f(shY + shDrop + 14)} C${f(cx - sw + 30)} ${f(shY + shDrop - 6)} ${f(cx - nw / 2 - 18)} ${f(shY + 2)} ${f(cx - nw / 2 - 2)} ${f(shY - 4)} L${f(cx)} ${f(shY + 30)} L${f(cx + nw / 2 + 2)} ${f(shY - 4)} C${f(cx + nw / 2 + 18)} ${f(shY + 2)} ${f(cx + sw - 30)} ${f(shY + shDrop - 6)} ${f(cx + sw - 4)} ${f(shY + shDrop + 14)} L${f(cx + sw + 8)} ${H + 2} Z`;
  if (garmentTone) S.push(`<path d="${clothD}" fill="${garmentTone}" opacity="${spec.collar === 'jacket' ? 0.55 : 0.5}"/>`);
  // neck shadow under the chin
  S.push(`<path d="M${f(cx - nw / 2)} ${f(neckTop)} L${f(cx + nw / 2)} ${f(neckTop)} L${f(cx + nw / 2)} ${f(chinY + 12)} Q${f(cx)} ${f(chinY + 22)} ${f(cx - nw / 2)} ${f(chinY + 12)} Z" fill="url(#${id}s)" opacity="0.8"/>`);
  S.push(`<path d="${bodyD}" ${stroke} stroke-width="2.4"/>`);
  // collars
  const cl = [];
  if (spec.collar === 'shirt' || spec.collar === 'uniform') {
    cl.push(`M${f(cx - nw / 2 - 2)} ${f(shY - 8)} L${f(cx - 30)} ${f(shY + 26)} L${f(cx - 4)} ${f(shY + 18)} L${f(cx)} ${f(shY + 4)}`, `M${f(cx + nw / 2 + 2)} ${f(shY - 8)} L${f(cx + 30)} ${f(shY + 26)} L${f(cx + 4)} ${f(shY + 18)} L${f(cx)} ${f(shY + 4)}`);
    cl.push(`M${f(cx)} ${f(shY + 18)} L${f(cx)} ${H}`); for (let y = shY + 40; y < H; y += 30) cl.push(`M${f(cx + 7)} ${f(y)} a2.6 2.6 0 1 0 0.1 0`);
    if (spec.collar === 'uniform') { for (const s of [-1, 1]) { cl.push(`M${f(cx + s * (sw - 18))} ${f(shY + shDrop + 4)} L${f(cx + s * (nw / 2 + 22))} ${f(shY + 6)}`); cl.push(`M${f(cx + s * 38)} ${f(shY + 58)} h${f(s * 34)} v16 h${f(-s * 34)} Z`); } }
  } else if (spec.collar === 'jacket') {
    for (const s of [-1, 1]) cl.push(`M${f(cx + s * (nw / 2 + 4))} ${f(shY - 6)} L${f(cx + s * 42)} ${f(shY + 30)} L${f(cx + s * 26)} ${f(shY + 40)} L${f(cx + s * 6)} ${H}`);
    cl.push(`M${f(cx - 22)} ${f(shY + 4)} L${f(cx)} ${f(shY + 30)} L${f(cx + 22)} ${f(shY + 4)}`);
  } else if (spec.collar === 'sweater') {
    cl.push(`M${f(cx - nw / 2 - 6)} ${f(shY - 4)} Q${f(cx)} ${f(shY + 22)} ${f(cx + nw / 2 + 6)} ${f(shY - 4)}`, `M${f(cx - nw / 2 - 10)} ${f(shY + 4)} Q${f(cx)} ${f(shY + 32)} ${f(cx + nw / 2 + 10)} ${f(shY + 4)}`);
  } else if (spec.collar === 'overall') {
    cl.push(`M${f(cx - nw / 2 - 4)} ${f(shY - 4)} Q${f(cx)} ${f(shY + 16)} ${f(cx + nw / 2 + 4)} ${f(shY - 4)}`);
    for (const s of [-1, 1]) { cl.push(`M${f(cx + s * 44)} ${f(shY + 12)} L${f(cx + s * 40)} ${H}`, `M${f(cx + s * 62)} ${f(shY + 18)} L${f(cx + s * 58)} ${H}`); cl.push(`M${f(cx + s * 51)} ${f(shY + 56)} a4 4 0 1 0 0.1 0`); }
  }
  if (cl.length) S.push(`<path d="${cl.join(' ')}" ${stroke} stroke-width="2"/>`);

  // ---- ears
  const eyeY = cy + hh * 0.06, noseY = cy + hh * 0.42;
  for (const s of [-1, 1]) { const ex = cx + s * hw * 0.98; S.push(`<path d="M${f(ex)} ${f(eyeY - 10)} C${f(ex + s * 14)} ${f(eyeY - 16)} ${f(ex + s * 16)} ${f(noseY + 6)} ${f(ex + s * 2)} ${f(noseY + 8)}" fill="${PAPER}" ${stroke.replace('fill="none"', '')} stroke-width="2"/>`);
    S.push(`<path d="M${f(ex + s * 4)} ${f(eyeY - 4)} C${f(ex + s * 10)} ${f(eyeY)} ${f(ex + s * 9)} ${f(noseY - 2)} ${f(ex + s * 3)} ${f(noseY)}" ${stroke} stroke-width="1.2"/>`); }

  // ---- face
  const hp = headPts(cx, cy, hw, hh, jw, spec.face); const faceD = smoothPath(hp);
  S.push(`<clipPath id="${id}f"><path d="${faceD}"/></clipPath>`);
  S.push(`<path d="${faceD}" fill="${PAPER}"/>`);
  // side shading (light from the left): hatch on the right third of the face, under the jaw, by the nose
  S.push(`<g clip-path="url(#${id}f)"><path d="M${f(cx + hw * 0.52)} ${f(cy - hh)} Q${f(cx + hw * 0.32)} ${f(cy + hh * 0.2)} ${f(cx + hw * 0.6)} ${f(cy + hh * 1.1)} L${f(cx + hw * 1.3)} ${f(cy + hh * 1.1)} L${f(cx + hw * 1.3)} ${f(cy - hh)} Z" fill="url(#${id}s)" opacity="0.75"/></g>`);
  if (spec.fh === 'beard') S.push(`<g clip-path="url(#${id}f)"><path d="M${f(cx - hw * 1.1)} ${f(cy + hh * 0.25)} Q${f(cx - hw * 0.6)} ${f(cy + hh * 0.6)} ${f(cx - hw * 0.25)} ${f(cy + hh * 0.56)} Q${f(cx)} ${f(cy + hh * 0.5)} ${f(cx + hw * 0.25)} ${f(cy + hh * 0.56)} Q${f(cx + hw * 0.6)} ${f(cy + hh * 0.6)} ${f(cx + hw * 1.1)} ${f(cy + hh * 0.25)} L${f(cx + hw * 1.1)} ${f(cy + hh * 1.2)} L${f(cx - hw * 1.1)} ${f(cy + hh * 1.2)} Z" fill="url(#${id}d)"/></g>`);
  S.push(`<path d="${faceD}" ${stroke} stroke-width="2.4"/>`);

  // ---- features
  const ex = hw * 0.40, ew = hw * 0.25, eh = ew * 0.42;
  const F = [];
  for (const s of [-1, 1]) { const x = cx + s * ex;
    F.push(`M${f(x - ew / 2)} ${f(eyeY)} Q${f(x)} ${f(eyeY - eh * 1.25)} ${f(x + ew / 2)} ${f(eyeY)}`); // upper lid
    F.push(`M${f(x - ew / 2 + 3)} ${f(eyeY + 1)} Q${f(x)} ${f(eyeY + eh * 0.85)} ${f(x + ew / 2 - 2)} ${f(eyeY + 1)}`); }
  // brows
  const bY = eyeY - hh * 0.19, bl = hw * 0.36;
  const bw = spec.brow === 'heavy' ? 4.6 : spec.brow === 'straight' ? 3.4 : 2.6;
  const B2 = [];
  for (const s of [-1, 1]) { const x0 = cx + s * (ex - bl * 0.48), x1 = cx + s * (ex + bl * 0.55);
    const lift = spec.brow === 'arched' ? 7 : spec.brow === 'soft' ? 4 : 1.5; B2.push(`M${f(x0)} ${f(bY + 2)} Q${f((x0 + x1) / 2)} ${f(bY - lift)} ${f(x1)} ${f(bY + 3)}`); }
  // nose
  F.push(`M${f(cx - 3)} ${f(bY + 12)} C${f(cx - 6)} ${f(noseY - 22)} ${f(cx - 9)} ${f(noseY - 6)} ${f(cx - 7)} ${f(noseY)}`);
  F.push(`M${f(cx - 13)} ${f(noseY + 1)} Q${f(cx - 7)} ${f(noseY + 7)} ${f(cx)} ${f(noseY + 4)} Q${f(cx + 7)} ${f(noseY + 7)} ${f(cx + 13)} ${f(noseY + 1)}`);
  // mouth
  const mY = cy + hh * 0.64, mw = hw * (spec.mouth === 'full' ? 0.5 : 0.44);
  F.push(`M${f(cx - mw / 2)} ${f(mY)} Q${f(cx)} ${f(mY + (spec.mouth === 'full' ? 3 : 1.5))} ${f(cx + mw / 2)} ${f(mY)}`);
  const lip = `M${f(cx - mw * 0.16)} ${f(mY + 8)} Q${f(cx)} ${f(mY + 10)} ${f(cx + mw * 0.16)} ${f(mY + 8)}`;
  // age lines
  const lines = spec.lines ?? (spec.age > 48 ? 2 : spec.age > 38 ? 1 : 0);
  const A = [];
  if (lines >= (spec.sex === 'f' ? 2 : 1)) { A.push(`M${f(cx - 16)} ${f(noseY + 5)} Q${f(cx - 24)} ${f(mY - 6)} ${f(cx - 22)} ${f(mY + 2)}`, `M${f(cx + 16)} ${f(noseY + 5)} Q${f(cx + 24)} ${f(mY - 6)} ${f(cx + 22)} ${f(mY + 2)}`); }
  if (lines >= 2) { A.push(`M${f(cx - hw * 0.35)} ${f(bY - 16)} Q${f(cx)} ${f(bY - 21)} ${f(cx + hw * 0.35)} ${f(bY - 16)}`, `M${f(cx - hw * 0.25)} ${f(bY - 25)} Q${f(cx)} ${f(bY - 29)} ${f(cx + hw * 0.25)} ${f(bY - 25)}`);
    for (const s of [-1, 1]) A.push(`M${f(cx + s * (ex + ew * 0.62))} ${f(eyeY - 2)} l${f(s * 7)} -3 M${f(cx + s * (ex + ew * 0.62))} ${f(eyeY + 3)} l${f(s * 7)} 3`); }
  S.push(`<path d="${F.join(' ')}" ${stroke} stroke-width="2"/>`);
  S.push(`<path d="${lip}" ${stroke} stroke-width="1.3" opacity="0.7"/>`);
  if (A.length) S.push(`<path d="${A.join(' ')}" ${stroke} stroke-width="1.3" opacity="0.85"/>`);
  S.push(`<path d="${B2.join(' ')}" ${stroke} stroke-width="${bw}"/>`);
  for (const s of [-1, 1]) S.push(`<circle cx="${f(cx + s * ex)}" cy="${f(eyeY + 0.5)}" r="${f(eh * 0.62)}" fill="${INK}"/>`);
  // facial hair
  if (spec.fh === 'mustache') S.push(`<path d="M${f(cx - mw * 0.62)} ${f(mY - 3)} Q${f(cx - mw * 0.3)} ${f(noseY + 4)} ${f(cx)} ${f(noseY + 9)} Q${f(cx + mw * 0.3)} ${f(noseY + 4)} ${f(cx + mw * 0.62)} ${f(mY - 3)} Q${f(cx)} ${f(mY - 7)} ${f(cx - mw * 0.62)} ${f(mY - 3)} Z" fill="${INK}"/>`);
  if (spec.fh === 'beard') S.push(`<path d="M${f(cx - mw * 0.6)} ${f(mY - 3)} Q${f(cx)} ${f(noseY + 8)} ${f(cx + mw * 0.6)} ${f(mY - 3)}" ${stroke} stroke-width="3.2"/>`);
  // glasses
  if (spec.glasses) { const r = ew * 0.72; S.push(`<g ${stroke} stroke-width="2.2">${[-1, 1].map(s => `<circle cx="${f(cx + s * ex)}" cy="${f(eyeY + 1)}" r="${f(r)}"/>`).join('')}<path d="M${f(cx - ex + r)} ${f(eyeY - 1)} Q${f(cx)} ${f(eyeY - 7)} ${f(cx + ex - r)} ${f(eyeY - 1)} M${f(cx - ex - r)} ${f(eyeY)} L${f(cx - hw * 0.98)} ${f(eyeY - 4)} M${f(cx + ex + r)} ${f(eyeY)} L${f(cx + hw * 0.98)} ${f(eyeY - 4)}"/></g>`); }

  // ---- front hair / headwear
  const top = cy - hh * 1.02;
  if (spec.hair === 'sidepart' || spec.hair === 'crop' || spec.hair === 'long' || spec.hair === 'bob' || spec.hair === 'ponytail' || spec.hair === 'bun' || spec.hair === 'cap') {
    const hl = spec.hair === 'crop' ? 0.62 : spec.hair === 'cap' ? 0.7 : 0.55;           // hairline height (fraction of hh above centre)
    const vol = spec.hair === 'sidepart' ? 1.12 : spec.hair === 'crop' ? 1.06 : spec.hair === 'long' || spec.hair === 'bob' ? 1.14 : 1.08;
    const pts = [[cx - hw * 1.03, cy - hh * 0.05], [cx - hw * 1.08, cy - hh * 0.55], [cx - hw * 0.72, top - hh * (vol - 1) - 4], [cx - hw * 0.1, top - hh * (vol - 1) * 1.4 - 6], [cx + hw * 0.62, top - hh * (vol - 1) - 3], [cx + hw * 1.08, cy - hh * 0.52], [cx + hw * 1.03, cy - hh * 0.05],
      [cx + hw * 0.92, cy - hh * 0.32], [cx + hw * 0.55, cy - hh * hl], [cx + hw * 0.05, cy - hh * (hl + 0.08)], [cx - (spec.hair === 'sidepart' ? hw * 0.18 : hw * 0.45), cy - hh * (hl - 0.02)], [cx - hw * 0.88, cy - hh * 0.3]];
    if (spec.hair === 'bob') { pts.splice(9, 3, [cx + hw * 0.35, cy - hh * 0.42], [cx - hw * 0.1, cy - hh * 0.38], [cx - hw * 0.6, cy - hh * 0.44], [cx - hw * 0.92, cy - hh * 0.2]); }
    if (spec.hair !== 'cap') S.push(hairPaint(smoothPath(pts)));
    if (spec.hair === 'ponytail') S.push(`<path d="M${f(cx + 2)} ${f(cy - hh * 0.62)} Q${f(cx + 1)} ${f(top - 2)} ${f(cx)} ${f(top - 8)}" ${stroke} stroke-width="1.6"/><path d="M${f(cx - hw * 0.9)} ${f(cy - hh * 0.36)} Q${f(cx - hw * 0.5)} ${f(cy - hh * 0.8)} ${f(cx - 2)} ${f(cy - hh * 0.62)} M${f(cx + hw * 0.9)} ${f(cy - hh * 0.36)} Q${f(cx + hw * 0.5)} ${f(cy - hh * 0.8)} ${f(cx + 4)} ${f(cy - hh * 0.62)}" ${stroke} stroke-width="1.1" opacity="0.8"/>`);
    if (spec.hair === 'sidepart') S.push(`<path d="M${f(cx - hw * 0.18)} ${f(cy - hh * (hl - 0.02))} Q${f(cx - hw * 0.22)} ${f(top)} ${f(cx - hw * 0.12)} ${f(top - 10)}" ${stroke} stroke-width="1.6" opacity="0.9"/>`);
    if (spec.hair === 'bun') { const d = smoothPath([[cx - 24, top - 4], [cx - 22, top - 34], [cx, top - 42], [cx + 22, top - 34], [cx + 24, top - 4]]); S.push(hairPaint(d)); }
  }
  if (spec.hair === 'cap') { // flat cap: crown + brim over a short side
    const sidePts = (s) => [[cx + s * hw * 1.03, cy - hh * 0.02], [cx + s * hw * 1.05, cy - hh * 0.42], [cx + s * hw * 0.86, cy - hh * 0.36], [cx + s * hw * 0.92, cy - hh * 0.05]];
    for (const s of [-1, 1]) S.push(hairPaint(smoothPath(sidePts(s))));
    const crown = smoothPath([[cx - hw * 1.18, cy - hh * 0.46], [cx - hw * 1.1, top - 8], [cx - hw * 0.4, top - 22], [cx + hw * 0.5, top - 20], [cx + hw * 1.16, top - 4], [cx + hw * 1.2, cy - hh * 0.48], [cx, cy - hh * 0.56]]);
    S.push(`<path d="${crown}" fill="${PAPER}"/><path d="${crown}" fill="url(#${id}c)" opacity="0.7"/><path d="${crown}" ${stroke} stroke-width="2.4"/>`);
    const brim = smoothPath([[cx - hw * 1.12, cy - hh * 0.48], [cx - hw * 0.4, cy - hh * 0.64], [cx + hw * 0.5, cy - hh * 0.64], [cx + hw * 1.14, cy - hh * 0.5], [cx + hw * 0.6, cy - hh * 0.42], [cx - hw * 0.6, cy - hh * 0.42]]);
    S.push(`<path d="${brim}" fill="${INK}" opacity="0.88"/><path d="M${f(cx - hw * 0.2)} ${f(top - 18)} Q${f(cx - hw * 0.1)} ${f(cy - hh * 0.75)} ${f(cx - hw * 0.05)} ${f(cy - hh * 0.62)}" ${stroke} stroke-width="1.4"/>`);
  }
  if (spec.hair === 'curly') { const C = []; const n = 13;
    for (let i = 0; i < n; i++) { const a = Math.PI * (1.02 + 0.96 * i / (n - 1)); C.push([cx + Math.cos(a) * hw * 0.98, cy - hh * 0.3 + Math.sin(a) * hh * 0.78, 13 + (i % 3) * 2]); }
    for (let i = 0; i < 6; i++) C.push([cx - hw * 0.55 + i * hw * 0.22, cy - hh * (0.62 + (i % 2) * 0.05), 11]);
    const d = C.map(([x, y, r]) => `M${f(x - r)} ${f(y)} a${r} ${r} 0 1 0 ${2 * r} 0 a${r} ${r} 0 1 0 ${-2 * r} 0 Z`).join(' ');
    S.push(`<path d="${d}" fill="${PAPER}"/><path d="${d}" fill="${hairFill}"/><path d="${d}" fill="${hairX}"/><path d="${d}" ${stroke} stroke-width="1.8"/>`); }
  if (spec.hair === 'receding') { for (const s of [-1, 1]) { const d = smoothPath([[cx + s * hw * 1.03, cy - hh * 0.0], [cx + s * hw * 1.1, cy - hh * 0.5], [cx + s * hw * 0.82, cy - hh * 0.86], [cx + s * hw * 0.64, cy - hh * 0.62], [cx + s * hw * 0.9, cy - hh * 0.28]]); S.push(hairPaint(d)); }
    S.push(`<path d="M${f(cx - hw * 0.5)} ${f(top + 8)} Q${f(cx)} ${f(top - 2)} ${f(cx + hw * 0.5)} ${f(top + 8)}" ${stroke} stroke-width="1" stroke-dasharray="2 3"/>`); }
  if (spec.hair === 'scarf') { // headscarf framing the face, knot under the chin
    const outer = smoothPath([[cx - hw * 1.32, cy + hh * 0.3], [cx - hw * 1.38, cy - hh * 0.5], [cx - hw * 0.8, cy - hh * 1.2], [cx, cy - hh * 1.3], [cx + hw * 0.8, cy - hh * 1.2], [cx + hw * 1.38, cy - hh * 0.5], [cx + hw * 1.32, cy + hh * 0.3],
      [cx + hw * 1.05, chinY + 8], [cx + hw * 0.4, chinY + 18], [cx - hw * 0.4, chinY + 18], [cx - hw * 1.05, chinY + 8]]);
    const inner = smoothPath([[cx - hw * 0.94, cy + hh * 0.2], [cx - hw * 0.95, cy - hh * 0.45], [cx - hw * 0.5, cy - hh * 0.86], [cx, cy - hh * 0.94], [cx + hw * 0.5, cy - hh * 0.86], [cx + hw * 0.95, cy - hh * 0.45], [cx + hw * 0.94, cy + hh * 0.2],
      [cx + hw * 0.75, cy + hh * 0.78], [cx + hw * 0.3, chinY - 3], [cx - hw * 0.3, chinY - 3], [cx - hw * 0.75, cy + hh * 0.78]]);
    S.push(`<path d="${outer} ${inner}" fill-rule="evenodd" fill="${PAPER}"/><path d="${outer} ${inner}" fill-rule="evenodd" fill="url(#${id}c)" opacity="0.6"/><path d="${outer}" ${stroke} stroke-width="2.4"/><path d="${inner}" ${stroke} stroke-width="2.2"/>`);
    S.push(`<path d="M${f(cx - hw * 0.6)} ${f(cy - hh * 1.05)} Q${f(cx - hw * 0.2)} ${f(cy - hh * 0.6)} ${f(cx - hw * 1.1)} ${f(cy + hh * 0.05)} M${f(cx + hw * 0.7)} ${f(cy - hh * 1.0)} Q${f(cx + hw * 1.3)} ${f(cy - hh * 0.3)} ${f(cx + hw * 1.0)} ${f(cy + hh * 0.5)}" ${stroke} stroke-width="1.3"/>`);
    S.push(`<path d="M${f(cx - 10)} ${f(chinY + 14)} q10 -8 20 0 q-4 16 -10 22 q-6 -6 -10 -22 Z" fill="${PAPER}" ${stroke.replace('fill="none"', '')} stroke-width="2"/>`); }
  // ---- frame
  if (o.frame !== false) S.push(`<rect x="${vb[0] + 1.5}" y="${vb[1] + 1.5}" width="${vb[2] - 3}" height="${vb[3] - 3}" fill="none" stroke="${INK}" stroke-width="3"/>`);
  S.push('</svg>');
  return S.join('\n');
}

// data URL helper (browser and Node)
export const CARD_CROP = [36, 26, 228, 250];   // head and shoulders for cards and documents (w:h ≈ 0.91)
export function portraitDataURL(spec, o) { const s = portraitSVG(spec, o); return 'data:image/svg+xml;base64,' + (typeof btoa === 'function' ? btoa(unescape(encodeURIComponent(s))) : Buffer.from(s).toString('base64')); }
