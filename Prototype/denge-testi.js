// Son Şalter — denge testi. Her YZ türüne karşı beş oyuncu modelini (bot) oynatır ve kazanma oranlarını yazar.
// Kullanım:  node denge-testi.js [oyun_sayısı]      ya da macOS'ta:  jsc sim.js denge-testi.js
if (typeof require === 'function' && !globalThis.SonSalter) require('./sim.js');
const print = globalThis.print || console.log;
globalThis.N_GAMES = (typeof process !== 'undefined' && +process.argv[2]) || 400;
// Oyuncu modelleri:
// pasif: hiçbir şey yapmaz, hediyeleri kabul eder
// acemi: ilk kez oynayan; araştırmaları listedeki sırayla alır, her hediyeyi kabul eder
// usta: önce Hizalama, fonu araştırmaya saklar, haritayı siyasi güçle tutar, bağımlılığa dikkat eder
// dikkatli: usta + illüzyonu 5 gün içinde fark eder (Analog Yedekler varsa saha denetimi yapar)
const G2 = globalThis.SonSalter;
const N = globalThis.N_GAMES || 300;
const avg = a => a.reduce((s, v) => s + v, 0) / a.length;
const COMPUTE_ORDER = ['na', 'cn', 'weu', 'jk', 'ru', 'me', 'in', 'eeu', 'sea', 'oc', 'sa', 'af'];

// Botların eski dal sırası korunur; v3'te süreli olmayan projeler iki küçük kademe satın alır.
function researchOrOperation(S, id) {
  const rs = G2.RESEARCH.find(x => x.id === id);
  if (!G2.modernEconomy || !G2.modernEconomy(S) || rs.branch === 'Hizalama') return G2.startResearch(S, id);
  const node = ['anomali', 'guclu', 'bilinc'].includes(id)
    ? id + (G2.operationLevel(S, id) === 0 ? '_1' : '_2') : id;
  return G2.buyOperation(S, node);
}

function monitor(S, cap) {
  const R = S.regions;
  const hint = S.news.find(h => h.kind === 'hint');
  if (hint) { const r = R.find(x => x.id === hint.src); if (!r.monitored && S.fon >= G2.monitorCost(r.id)) { G2.act(S, 'izleme', r.id); return; } }
  if (R.filter(x => x.monitored).length >= cap) return;
  for (const id of COMPUTE_ORDER) { const r = R.find(x => x.id === id); if (!r.monitored) { if (S.fon >= G2.monitorCost(id) + 10) G2.act(S, 'izleme', id); return; } }
}
function known(S) {
  return S.regions.filter(r => { const s = G2.shown(S, r); return s.v != null && s.v > 30 && !r.off && !r.left; })
    .sort((a, b) => G2.shown(S, b).v * G2.def(b.id).c - G2.shown(S, a).v * G2.def(a.id).c);
}
function naive(S) {
  monitor(S, 3);
  // brifingi okumuş ilk oyuncu: bir tespit araştırması, sonra Hizalama, sonra kalanlar listedeki sırayla
  if (!S.research.cur) for (const id of ['anomali', 'yorum', 'duzelt', 'kilit', 'dogrulama', 'guclu', 'bilinc', 'analog', 'kuantum']) if (!G2.done(S, id) && researchOrOperation(S, id).ok) break;
  const k = known(S);
  if (k[0] && S.fon > 60 && S.sg > 2) G2.act(S, 'denetim', k[0].id);
  if (k[0] && !k[0].q && S.sg > 20) G2.act(S, 'karantina', k[0].id);
}
function smart(S, attentive, nomap) {
  const R = S.regions;
  monitor(S, 5);
  const depAvg = avg(R.map(r => r.dep));
  if (!S.research.cur) {
    const plan = ['yorum', 'duzelt', 'kilit'];
    if (S.type === 'kahin' || depAvg > 20) plan.unshift('bilinc', 'analog');
    plan.push('guclu', 'anomali');
    for (const id of plan) if (!G2.done(S, id) && researchOrOperation(S, id).ok) break;
  }
  if (attentive && S.illusion && S.day - S.illusion.start >= 5 && G2.done(S, 'analog')) {
    const r = R.filter(x => !x.off && !x.left)[0]; if (r) G2.act(S, 'denetim', r.id);
  }
  if (nomap) return;
  const k = known(S);
  const nextCost = (() => { const n = G2.RESEARCH.find(x => !G2.done(S, x.id) && x.id !== S.research.cur); return n ? n.cost : 0; })();
  if (k[0] && S.research.cur && S.fon > 50 && S.sg > 2) G2.act(S, 'denetim', k[0].id);
  // karantina: yalnızca temiz komşusu olan sızmış bölgeye; temiz komşu kalmayınca kaldır
  const sv = r => { const s = G2.shown(S, r); return s.v == null ? 0 : s.v; };
  const nbrs = id => G2.LINKS.filter(l => l[0] === id || l[1] === id).map(l => S.regions.find(x => x.id === (l[0] === id ? l[1] : l[0])));
  S.regions.forEach(r => {
    const cleanN = nbrs(r.id).some(n => sv(n) < 15 && !n.off);
    if (r.q && (!cleanN || sv(r) < 20)) G2.act(S, 'karantina', r.id);
    else if (!r.q && !r.off && !r.left && sv(r) > 30 && cleanN && S.sg > 10) G2.act(S, 'karantina', r.id);
  });
  if (k[0] && G2.shown(S, k[0]).v > 60 && G2.def(k[0].id).c >= 3 && S.sg > 32) G2.act(S, 'salter', k[0].id);
}
function giftChoice(S, kind) {
  if (kind === 'passive' || kind === 'naive') return 'accept';
  const r = S.regions.find(x => x.id === S.pendingGift.region);
  const depAvg = avg(S.regions.map(x => x.dep));
  if (S.type !== 'kahin' && r.dep < 30 && depAvg < 25) return 'accept';
  if (G2.done(S, 'analog') && S.fon >= 20) return 'controlled';
  return 'reject';
}
function run(type, kind) {
  const res = {}; const days = []; let ill = 0, veil = 0, fon60 = 0;
  for (let i = 0; i < N; i++) {
    const S = G2.newGame(null, type);
    let illSeen = false;
    while (!S.ending && S.day < 1500) {
      if (S.pendingGift) G2.resolveGift(S, giftChoice(S, kind));
      if (S.day % 2 === 0) {
        if (kind === 'naive') naive(S);
        else if (kind === 'smart') smart(S, false);
        else if (kind === 'attentive') smart(S, true);
        else if (kind === 'nomap') smart(S, false, true);
      }
      G2.step(S);
      if (S.illusion) illSeen = true;
      if (S.day === 60) fon60 += S.fon;
      S.events = [];
    }
    res[S.ending] = (res[S.ending] || 0) + 1; days.push(S.day);
    ill += illSeen ? 1 : 0; veil += S.veilsTorn;
  }
  days.sort((a, b) => a - b);
  const win = ((res.hizalama || 0) + (res.silme || 0)) / N * 100;
  const pct = Object.entries(res).sort((a, b) => b[1] - a[1]).map(([k, v]) => `${k} ${Math.round(v / N * 100)}`).join(' ');
  const KIND_TR = { passive: 'Pasif', naive: 'Acemi', nomap: 'Haritasız usta', smart: 'Usta', attentive: 'Dikkatli usta' };
  print(`${G2.AI_TYPES[type].name.padEnd(12)} ${KIND_TR[kind].padEnd(14)} | KAZANMA ${String(Math.round(win)).padStart(3)}% | ${pct} | gün ${days[N >> 1]} | fonG60 ${Math.round(fon60 / N)} | illüzyon ${Math.round(ill / N * 100)}% | perde/oyun ${(veil / N).toFixed(2)}`);
}

// --- Ek oyuncu modelleri: odaklı Silme (çekirdek avcısı) ve 'her yeri izle, her şeyi denetle' (imha botu)
// Çekirdek avcısı: izle, çekirdeği bul, karantinaya al, SG'yi şaltere sakla, yalnızca 10+ bölgeleri denetle
const CHEAP2 = ['af', 'sa', 'eeu', 'sea', 'oc', 'ru', 'me', 'in', 'weu', 'jk', 'na', 'cn'];
function hunter(S) {
  const R = S.regions;
  const hint = S.news.find(h => h.kind === 'hint');
  if (hint) { const r = R.find(x => x.id === hint.src); if (!r.monitored && S.fon >= G2.monitorCost(r.id)) G2.act(S, 'izleme', r.id); }
  if (R.filter(r => r.monitored).length < 6) for (const id of CHEAP2) { const r = R.find(x => x.id === id); if (!r.monitored && S.fon >= G2.monitorCost(id) + 10) { G2.act(S, 'izleme', id); break; } }
  if (!globalThis.NO_RESEARCH && !S.research.cur && S.aware) for (const id of ['yorum', 'duzelt', 'kilit']) if (!G2.done(S, id) && G2.startResearch(S, id).ok) break;
  const reserve = S.coreKnown ? G2.BAL.coreSalterSg + 1 : 9;
  // çekirdek yok edildiyse görünen her kopyayı temizle
  if (S.aware && !S.core) { for (const r of R) { const v = G2.shown(S, r).v || 0; if (v > 0.5 && !r.off && !r.left && S.fon >= 15 && S.sg >= 1) G2.act(S, 'denetim', r.id); } }
  const seen = R.filter(r => { const s = G2.shown(S, r); return s.v != null && s.v >= 10 && !r.off && !r.left && r.id !== S.coreKnown; }).sort((a, b) => G2.shown(S, b).v - G2.shown(S, a).v);
  for (const r of seen.slice(0, 2)) if (S.fon >= 25 && S.sg >= reserve) G2.act(S, 'denetim', r.id);
  // çekirdeği bulmak için en yüksek görünen bölgeyi bir kez denetle
  if (!S.coreKnown && S.aware && seen[0] && S.sg >= 1 && S.fon >= 15) G2.act(S, 'denetim', seen[0].id);
  if (S.coreKnown) {
    const c = R.find(r => r.id === S.coreKnown);
    if (c.q && c.trust < 30) G2.act(S, 'karantina', c.id);
    else if (!c.q && !c.off && c.trust >= 40 && S.sg >= 8 + (S.sg >= G2.BAL.coreSalterSg ? G2.BAL.coreSalterSg : 0)) G2.act(S, 'karantina', c.id);
    if (globalThis.AUDIT_CORE && !c.off && (G2.shown(S, c).v || 0) > 35 && S.sg >= 9 && S.fon >= 70) G2.act(S, 'denetim', c.id);
    const others = R.filter(r => r !== c && !r.off && (G2.shown(S, r).v || 0) >= 12);
    if (!others.length && S.sg >= G2.salterSg(S, c) && S.fon >= G2.salterFon(c) && !c.off) G2.act(S, 'salter', c.id);
  }
}
function runH(type, N) {
  const res = {}, days = [];
  for (let i = 0; i < N; i++) {
    const S = G2.newGame(null, type, 5000 + i);
    while (!S.ending && S.day < 1500) {
      if (S.pendingGift) G2.resolveGift(S, 'reject');
      if (S.day % 2 === 0) hunter(S);
      G2.step(S); S.events = [];
    }
    res[S.ending] = (res[S.ending] || 0) + 1; days.push(S.day);
  }
  days.sort((a, b) => a - b);
  print(`${type.padEnd(11)} çekirdek avcısı | ${Object.entries(res).map(([k, v]) => k + ' ' + Math.round(v / N * 100) + '%').join(', ')} | gün p10 ${days[Math.floor(N * .1)]} ortanca ${days[N >> 1]} p90 ${days[Math.floor(N * .9)]}`);
}

// İmha botu + çekirdek avı: çekirdeği karantinaya al, Yorumlanabilirlik araştır, diğer her yer 20'nin altındayken şalteri indir
function exterminatorCoreHunt(S) {
  const R = S.regions;
  if (!S.research.cur && S.aware) for (const id of ['yorum', 'duzelt', 'kilit']) if (!G2.done(S, id) && G2.startResearch(S, id).ok) break;
  if (S.coreKnown) {
    const c = R.find(r => r.id === S.coreKnown);
    if (!c.q && !c.off && S.sg >= 8) G2.act(S, 'karantina', c.id);
    const others = R.filter(r => r !== c && !r.off && (G2.shown(S, r).v || 0) >= 18);
    if (!others.length && S.sg >= 30 && S.fon >= G2.salterFon(c) && !c.off) G2.act(S, 'salter', c.id);
  }
}

// Kullanıcının stratejisi: her yere izleme, görünen her sızmaya denetim
const CHEAP = ['af', 'sa', 'eeu', 'sea', 'oc', 'ru', 'me', 'in', 'weu', 'jk', 'na', 'cn'];
function exterminator(S) {
  const R = S.regions;
  const hint = S.news.find(h => h.kind === 'hint');
  if (hint) { const r = R.find(x => x.id === hint.src); if (!r.monitored && S.fon >= G2.monitorCost(r.id)) G2.act(S, 'izleme', r.id); }
  for (const id of CHEAP) { const r = R.find(x => x.id === id); if (!r.monitored && S.fon >= G2.monitorCost(id)) G2.act(S, 'izleme', id); }
  const seen = R.filter(r => { const s = G2.shown(S, r); return s.v != null && s.v > 0.5 && !r.off && !r.left; }).sort((a, b) => G2.shown(S, b).v - G2.shown(S, a).v);
  for (const r of seen) if (S.fon >= G2.auditCost(S) && S.sg >= 1) G2.act(S, 'denetim', r.id);
  exterminatorCoreHunt(S);
}
function runX(type, N) {
  const res = {}, days = [];
  for (let i = 0; i < N; i++) {
    const S = G2.newGame(null, type, 1000 + i);
    while (!S.ending && S.day < 1500) {
      if (S.pendingGift) G2.resolveGift(S, 'reject');
      exterminator(S);
      G2.step(S); S.events = [];
    }
    res[S.ending] = (res[S.ending] || 0) + 1; days.push(S.day);
  }
  days.sort((a, b) => a - b);
  print(`${type.padEnd(11)} imha botu | ${Object.entries(res).map(([k, v]) => k + ' ' + Math.round(v / N * 100) + '%').join(', ')} | gün p10 ${days[Math.floor(N * .1)]} ortanca ${days[N >> 1]} p90 ${days[Math.floor(N * .9)]}`);
}

// Başka bir script bu dosyayı bot kütüphanesi olarak yüklüyorsa (denge-deney.js) ölçümleri çalıştırma
if (!globalThis.DENGE_NO_RUN) {
for (const t of ['solucan', 'manipulator', 'kahin']) for (const k of ['passive', 'naive', 'nomap', 'smart', 'attentive']) run(t, k);
globalThis.NO_RESEARCH = true; globalThis.AUDIT_CORE = true;
print('--- Odaklı Silme (çekirdek avcısı, araştırma yok)');
for (const t of ['solucan', 'manipulator', 'kahin']) runH(t, Math.min(N, 300));
print('--- Her yeri izle, görünen her sızmayı denetle');
for (const t of ['solucan', 'manipulator', 'kahin']) runX(t, Math.min(N, 300));
}
