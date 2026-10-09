// Baskın motoru testi: açgözlü bir bot her haritayı farklı YZ yetenekleriyle oynar.
// Hata yakalar, kazanma oranı ve ortalama yan hasar yazar. İnsan oyuncu bottan daha iyi olmalı (önizleme, öngörü).
// Kullanım (macOS):  $JSC baskin.js baskin-testi.js
const K = globalThis.Baskin;
const log = globalThis.print || console.log;
const NR = globalThis.N_RAIDS || 300;

function dangerTiles(B) {
  const s = new Set();
  K.intents(B).forEach(i => { if (i.target && !i.target.none && i.kind !== 'repair') s.add(i.target.x + ',' + i.target.y); });
  B.spawns.forEach(t => s.add(t.x + ',' + t.y));
  return s;
}

function evalState(B0, B) {
  const tg = K.targetsLeft(B).reduce((a, t) => a + t.o.hp, 0), tg0 = K.targetsLeft(B0).reduce((a, t) => a + t.o.hp, 0);
  const ehp = K.alive(B, 'e').reduce((a, u) => a + u.hp, 0), ehp0 = K.alive(B0, 'e').reduce((a, u) => a + u.hp, 0);
  const php = K.alive(B, 'p').reduce((a, u) => a + u.hp, 0), php0 = K.alive(B0, 'p').reduce((a, u) => a + u.hp, 0);
  let sc = (tg0 - tg) * 12 + (ehp0 - ehp) * 3 - (php0 - php) * 8 - (B.collateral - B0.collateral) * 4;
  if (B.phase === 'won') sc += 1000;
  const danger = dangerTiles(B);
  // Robotların niyet okuyla tehdit ettiği sivil sunucular (oyuncu bunları korumaya çalışır)
  danger.forEach(k => { const [x, y] = k.split(',').map(Number); const o = K.cell(B, x, y).o; if (o && o.k === 'civ' && !K.unitAt(B, x, y)) sc -= 3; });
  const targets = K.targetsLeft(B);
  K.alive(B, 'p').forEach(u => {
    if (danger.has(u.x + ',' + u.y)) sc -= 5;
    if (u.type === 'tek' && targets.length) sc -= 0.8 * Math.min(...targets.map(t => Math.abs(t.x - u.x) + Math.abs(t.y - u.y)));
  });
  return sc;
}

function botTurn(B) {
  ['tek', 'emp', 'ana'].forEach(type => {
    const u = B.units.find(v => v.type === type && !v.dead);
    if (!u || B.phase !== 'player') return;
    let best = null, bestSc = -Infinity;
    const spots = [{ x: u.x, y: u.y, stay: true }].concat(K.reachable(B, u));
    spots.forEach(sp => {
      const C = K.clone(B);
      if (!sp.stay) K.doMove(C, u.id, sp.x, sp.y);
      const cu = C.units.find(v => v.id === u.id);
      const opts = [null].concat(K.targets(C, cu));
      opts.forEach(t => {
        const D = K.clone(C);
        if (t) K.doAct(D, u.id, t.x, t.y);
        const sc = evalState(B, D);
        if (sc > bestSc) { bestSc = sc; best = { sp, t }; }
      });
    });
    if (!best.sp.stay) K.doMove(B, u.id, best.sp.x, best.sp.y);
    if (best.t) K.doAct(B, u.id, best.t.x, best.t.y);
  });
}

let colPlayer = 0, colEnemy = 0;
function play(params) {
  const B = K.create(params);
  let guard = 0;
  while (B.phase === 'player' && guard++ < 20) {
    const c0 = B.collateral; botTurn(B); colPlayer += B.collateral - c0;
    const c1 = B.collateral; if (B.phase === 'player') K.endTurn(B); colEnemy += B.collateral - c1;
  }
  return K.result(B);
}

globalThis.BaskinBot = { play, botTurn };
// Başka bir script bu dosyayı bot kütüphanesi olarak yüklüyorsa ölçümleri çalıştırma
if (!globalThis.BASKIN_NO_RUN) {
const variants = [
  { label: 'düğüm, yeteneksiz', traits: [], isCore: false },
  { label: 'çekirdek', traits: [], isCore: true },
  { label: 'çekirdek + yedek', traits: ['yedek'], isCore: true },
  { label: 'aldatma', traits: ['aldatma'], isCore: false },
  { label: 'kopya + hızlanma', traits: ['kopya', 'hizlanma'], isCore: true },
];
for (const v of variants) {
  let win = 0, col = 0, down = 0, turns = 0;
  const byLayout = {};
  for (let i = 0; i < NR; i++) {
    const p = { seed: 1000 + i * 7919, compute: 1 + (i % 5), inf: 20 + (i * 13) % 70, isCore: v.isCore, raidNumber: 1 + (i % 2), traits: v.traits };
    const r = play(p);
    const L = K.create(p).layout;
    byLayout[L] = byLayout[L] || [0, 0]; byLayout[L][1]++; if (r.success) byLayout[L][0]++;
    if (r.success) win++;
    col += r.collateral; down += r.down; turns += r.turns;
  }
  log(`${v.label.padEnd(20)} | bot kazanma %${Math.round(win / NR * 100)} | yan hasar ${(col / NR).toFixed(2)} | düşen ${(down / NR).toFixed(2)} | tur ${(turns / NR).toFixed(1)} | ` +
    Object.entries(byLayout).map(([k, [w, n]]) => `${k} %${Math.round(w / n * 100)}`).join(', '));
}
log(`yan hasarın kaynağı: oyuncu ${colPlayer}, robotlar ${colEnemy}`);
}
