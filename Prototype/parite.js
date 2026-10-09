// Son Şalter — C# parite testi değerleri. SimulationTests'teki beklenen metinleri üretir; sim.js ya da baskin.js
// değişince bu çıktıyı testteki TestCase'lere kopyala.
// Kullanım (macOS):  /System/Library/Frameworks/JavaScriptCore.framework/Versions/Current/Helpers/jsc sim.js baskin.js parite.js
const P = globalThis.SonSalter;
const K = globalThis.Baskin;
const print = globalThis.print || console.log;

// 1) Temel oyun — C# PlayScripted ile aynı senaryo: Kuzey Amerika ve Çin'e izleme, hediyeleri kabul, tespitte Yorumlanabilirlik
function playScripted(type, seed) {
  const S = P.newGame(null, type, seed);
  P.act(S, 'izleme', 'na'); P.act(S, 'izleme', 'cn');
  while (!S.ending && S.day < 2000) {
    if (S.pendingGift) P.resolveGift(S, 'accept');
    if (S.aware && !S.research.cur) P.startResearch(S, 'yorum');
    P.step(S); S.events = [];
  }
  return `${S.ending}@${S.day} fon=${S.fon.toFixed(6)}`;
}
print('// FullGame_MatchesBrowserPrototype');
for (const [type, seed] of [['manipulator', 7], ['kahin', 99], ['solucan', 12345]])
  print(`[TestCase("${type}", ${seed}u, "${playScripted(type, seed)}")]`);

// 2) Deneysel mod — C# PlayScriptedExp ile aynı senaryo: Alfa→na, Bravo→cn; varınca izleme; tespitte Yorumlanabilirlik;
// çekirdek bilinince Charlie'yi gönder ve baskın yap (sonuç sabit: başarılı, 1 sivil, 1 yaralı)
function playScriptedExp(type, seed, ts) {
  const S = P.newGame(null, type, seed, { exp: { teams: true, timeScale: ts, startTeams: 3 } });
  P.moveTeam(S, 'alfa', 'na'); P.moveTeam(S, 'bravo', 'cn');
  while (!S.ending && S.day < 3000) {
    if (S.pendingGift) P.resolveGift(S, 'accept');
    for (const id of ['na', 'cn']) { const r = S.regions.find(x => x.id === id); if (!r.monitored && P.idleTeamAt(S, id)) P.act(S, 'izleme', id); }
    if (S.aware && !S.research.cur) P.startResearch(S, 'yorum');
    if (S.coreKnown) {
      if (!P.raidCheck(S, S.coreKnown)) { P.startRaid(S, S.coreKnown); P.resolveRaid(S, { success: true, collateral: 1, down: 1, backupDestroyed: true }); }
      else { const c = S.teams.find(t => t.id === 'charlie'); if (c && P.teamReady(c) && c.at && c.at !== S.coreKnown) P.moveTeam(S, 'charlie', S.coreKnown); }
    }
    P.step(S); S.events = [];
  }
  return `${S.ending}@${S.day} fon=${S.fon.toFixed(6)} raids=${S.raids}`;
}
print('// FullGameExp_MatchesBrowserPrototype');
for (const [type, seed, ts] of [['manipulator', 7, 1.5], ['kahin', 99, 2], ['solucan', 12345, 1.5]])
  print(`[TestCase("${type}", ${seed}u, ${ts}, "${playScriptedExp(type, seed, ts)}")]`);

// 3) Baskın — basit senaryo oyuncusu oynar, hamleleri kaydedilir; C# aynı hamleleri tekrar oynayıp son durumu karşılaştırır
function sig(B) {
  const objs = [];
  for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) { const o = K.cell(B, x, y).o; if (o) objs.push(`${o.k}${x}${y}:${o.hp}`); }
  return `${B.phase}|t${B.turn}|c${B.collateral}|rs${B.rs}|` + B.units.map(u => `${u.type}${u.x}${u.y}:${u.hp}${u.dead ? 'x' : ''}`).join(',') + '|' + objs.join(',');
}
function playRaid(params) {
  const B = K.create(params);
  const moves = [];
  let guard = 0;
  while (B.phase === 'player' && guard++ < 10) {
    for (const type of ['tek', 'emp', 'ana']) {
      const u = B.units.find(v => v.type === type && !v.dead);
      if (!u || B.phase !== 'player') continue;
      const tl = K.targetsLeft(B);
      const goal = tl.length ? tl[0] : { x: 4, y: 4 };
      const reach = K.reachable(B, u);
      if (reach.length) {
        const best = reach.slice().sort((a, b) => (Math.abs(a.x - goal.x) + Math.abs(a.y - goal.y)) - (Math.abs(b.x - goal.x) + Math.abs(b.y - goal.y)))[0];
        K.doMove(B, u.id, best.x, best.y); moves.push(`m${u.id}.${best.x}.${best.y}`);
      }
      const tg = K.targets(B, u);
      const pick = tg.find(t => { const o = K.cell(B, t.x, t.y).o; return o && K.TARGET_KINDS.includes(o.k); })
        || tg.find(t => { const v = K.unitAt(B, t.x, t.y); return v && v.side === 'e'; });
      if (pick) { K.doAct(B, u.id, pick.x, pick.y); moves.push(`a${u.id}.${pick.x}.${pick.y}`); }
    }
    if (B.phase === 'player') { K.endTurn(B); moves.push('e'); }
  }
  return { moves: moves.join(' '), sig: sig(B) };
}
print('// Raid_MatchesBrowserPrototype (tohum, hesaplama, sızma, çekirdek, baskın no, yetenekler, hamleler, imza)');
for (const [seed, compute, inf, isCore, n, traits] of [[11, 5, 45, true, 1, ''], [12345, 3, 60, false, 2, 'aldatma'], [777, 4, 30, true, 1, 'yedek,kopya,hizlanma']]) {
  const r = playRaid({ seed, compute, inf, isCore, raidNumber: n, traits: traits ? traits.split(',') : [] });
  print(`[TestCase(${seed}, ${compute}, ${inf}, ${isCore}, ${n}, "${traits}",\n    "${r.moves}",\n    "${r.sig}")]`);
}
