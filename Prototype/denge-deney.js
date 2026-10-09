// Deneysel mod denge testi: saha ekipleri, zaman ölçeği ve baskın açıkken oyun ne kadar sürüyor, kim kazanıyor?
// denge-testi.js'teki oyuncu modellerini (Acemi, Usta, Dikkatli usta) aynen kullanır; komutları ekip kurallarına uydurur:
// izleme/denetim için bölgede ekip yoksa en yakın boştaki ekibi yola çıkarır. Çekirdeğin yeri bilinince baskın yapar ve
// baskını baskin-testi.js'teki taktik bot oynar.
// Kullanım (macOS):
//   echo 'globalThis.DENGE_NO_RUN = true; globalThis.BASKIN_NO_RUN = true;' > /tmp/pre.js
//   $JSC /tmp/pre.js sim.js baskin.js baskin-testi.js denge-testi.js denge-deney.js
const SS = globalThis.SonSalter;
const BOT = globalThis.BaskinBot;
const out = globalThis.print || console.log;
const NG = globalThis.N_DENEY || 150;
const rawAct = SS.act;

let raidStats = { n: 0, win: 0, col: 0 };

// Ekip kurallı act: bölgede boşta ekip yoksa en yakın hazır ekibi gönder, komutu sonra tekrar dene
SS.act = function (S, type, id) {
  if (S.exp && S.exp.teams && (type === 'izleme' || type === 'denetim')) {
    const r = S.regions.find(x => x.id === id);
    if (!(type === 'izleme' && r.monitored) && !SS.idleTeamAt(S, id)) { dispatch(S, id); return { ok: false, msg: 'ekip yok' }; }
  }
  return rawAct(S, type, id);
};

function dispatch(S, id) {
  if (S.teams.some(t => t.dest === id)) return;
  const ready = S.teams.filter(t => SS.teamReady(t) && t.at && t.at !== id && !(S.coreKnown && t.at === S.coreKnown));
  if (!ready.length) return;
  ready.sort((a, b) => SS.travelDays(a.at, id) - SS.travelDays(b.at, id));
  SS.moveTeam(S, ready[0].id, id);
}

function expLogic(S) {
  if (!S.exp || !S.exp.teams) return;
  // Fon bolsa ekip kur
  if (S.teams.length < 3 && S.fon > 110) SS.hireTeam(S);
  // Çekirdeğin yeri biliniyorsa ekibi oraya gönder ve baskın yap
  if (S.coreKnown && !globalThis.NO_RAID) {
    const c = S.regions.find(r => r.id === S.coreKnown);
    if (!SS.idleTeamAt(S, c.id)) dispatch(S, c.id);
    else if (!SS.raidCheck(S, c.id)) {
      SS.startRaid(S, c.id);
      // RAID_P verilirse baskın sonucu bu başarı oranıyla üretilir (insan oyuncu varsayımı); yoksa taktik bot oynar
      const res = globalThis.RAID_P != null
        ? (Math.random() < globalThis.RAID_P ? { success: true, collateral: 1, down: 0, backupDestroyed: true } : { success: false, collateral: 2, down: 1 })
        : BOT.play(S.raid.params);
      raidStats.n++; if (res.success) raidStats.win++; raidStats.col += res.collateral;
      SS.resolveRaid(S, res);
    }
  }
}

function runExp(type, kind, opts) {
  const res = {}; const days = []; let raids = 0;
  for (let i = 0; i < NG; i++) {
    const S = SS.newGame(null, type, 7000 + i, opts);
    while (!S.ending && S.day < 3000) {
      if (S.pendingGift) SS.resolveGift(S, giftChoice(S, kind));
      if (S.day % 2 === 0) {
        if (kind === 'naive') naive(S);
        else if (kind === 'smart') smart(S, false);
        else if (kind === 'attentive') smart(S, true);
        if (kind !== 'passive') expLogic(S);
      }
      SS.step(S);
      S.events = [];
    }
    res[S.ending] = (res[S.ending] || 0) + 1; days.push(S.day); raids += S.raids || 0;
  }
  days.sort((a, b) => a - b);
  const win = ((res.hizalama || 0) + (res.silme || 0)) / NG * 100;
  const pct = Object.entries(res).sort((a, b) => b[1] - a[1]).map(([k, v]) => `${k} ${Math.round(v / NG * 100)}`).join(' ');
  return { win, pct, med: days[NG >> 1], raids: raids / NG };
}

const KIND_TR = { passive: 'Pasif', naive: 'Acemi', smart: 'Usta', attentive: 'Dikkatli usta' };
const CONFIGS = globalThis.DENEY_CONFIGS || [
  { label: 'temel (deney kapalı)', opts: undefined, dayMs: 900 },
  { label: 'ekipler, ölçek 1', opts: { exp: { teams: true, timeScale: 1 } }, dayMs: 900 },
  { label: 'ekipler, ölçek 1,5', opts: { exp: { teams: true, timeScale: 1.5 } }, dayMs: 1100 },
];
for (const c of CONFIGS) {
  out(`=== ${c.label}`);
  for (const t of ['solucan', 'manipulator', 'kahin']) for (const k of ['passive', 'naive', 'smart']) {
    raidStats = { n: 0, win: 0, col: 0 };
    const r = runExp(t, k, c.opts);
    const minutes = r.med * c.dayMs / 60000;
    out(`${SS.AI_TYPES[t].name.padEnd(12)} ${KIND_TR[k].padEnd(8)} | KAZANMA ${String(Math.round(r.win)).padStart(3)}% | ${r.pct} | gün ${r.med} (~${minutes.toFixed(1)} dk 1×'te) | baskın/oyun ${r.raids.toFixed(2)}` +
      (raidStats.n ? ` | baskın başarı %${Math.round(raidStats.win / raidStats.n * 100)}` : ''));
  }
}
