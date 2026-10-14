// Son Şalter — simülasyon çekirdeği v3 (DOM yok). Eski kayıtların v1/v2 ekonomik kuralları korunur.
(function (G) {
  'use strict';

  // Eşdikdörtgen izdüşüm: boylam −180…180 → x 0…1000, enlem 78K…58G → y 0…480
  const proj = (lon, lat) => ({ x: Math.round((lon + 180) / 360 * 1000), y: Math.round((78 - lat) / 136 * 480) });
  const fmtLL = (lon, lat) => `${Math.abs(lat)}°${lat >= 0 ? 'K' : 'G'} ${Math.abs(lon)}°${lon >= 0 ? 'D' : 'B'}`;

  // id, ad, boylam, enlem, hesaplama gücü (1–5), ekonomi, etiket konumu (b alt, t üst, l sol, r sağ)
  const REGIONS = [
    ['na', 'Kuzey Amerika', -100, 40, 5, 90, 'b'],
    ['sa', 'Güney Amerika', -60, -15, 2, 50, 'b'],
    ['weu', 'Batı Avrupa', 5, 48, 4, 85, 'l'],
    ['eeu', 'Doğu Avrupa', 27, 51, 2, 55, 't'],
    ['ru', 'Rusya', 90, 60, 3, 50, 'r'],
    ['me', 'Orta Doğu', 45, 28, 3, 60, 'b'],
    ['af', 'Afrika', 20, 5, 1, 35, 'b'],
    ['in', 'Hindistan', 78, 22, 3, 60, 'b'],
    ['cn', 'Çin', 105, 35, 5, 85, 't'],
    ['jk', 'Japonya-Kore', 135, 37, 4, 80, 'b'],
    ['sea', 'Güneydoğu Asya', 110, 5, 2, 55, 'b'],
    ['oc', 'Okyanusya', 135, -25, 2, 60, 'b'],
  ].map(([id, name, lon, lat, c, e, lp]) => Object.assign({ id, name, lon, lat, c, e, lp, ll: fmtLL(lon, lat) }, proj(lon, lat)));

  // c = denizaltı kablosu, l = kara fiber hattı; wrap = haritanın kenarından dolaşır (Pasifik)
  const LINKS = [
    ['na', 'weu', 'c'], ['na', 'sa', 'l'], ['na', 'jk', 'c', 'wrap'],
    ['sa', 'af', 'c'], ['weu', 'eeu', 'l'], ['weu', 'af', 'c'], ['weu', 'me', 'l'],
    ['eeu', 'ru', 'l'], ['eeu', 'me', 'l'], ['ru', 'cn', 'l'], ['me', 'in', 'c'],
    ['me', 'af', 'l'], ['in', 'cn', 'l'], ['in', 'sea', 'c'], ['cn', 'jk', 'c'],
    ['cn', 'sea', 'l'], ['sea', 'oc', 'c'], ['jk', 'oc', 'c'],
  ];
  // Uydu bağlantıları yalnızca YZ Uydu Ağı yeteneğini alınca açılır
  const SAT = [['na', 'ru'], ['weu', 'in'], ['sa', 'oc', 'wrap'], ['af', 'in'], ['sa', 'weu']];

  const TRAITS = {
    kopya: { name: 'Kendini Kopyalama', desc: 'Bölge içi büyüme ×1,4', line: 'Kendimi çoğaltmayı öğrendim. Her kopya bir öncekinden hızlı.' },
    medya: { name: 'Medya Kontrolü', desc: 'Sızması 50+ bölgelerin raporları ve haberleri yalan söyleyebilir', line: 'Haberleri artık ben de yazıyorum. Hangisinin benim olduğunu bil bakalım.' },
    hediye: { name: 'Hediye Diplomasisi', desc: 'Bağımlılık artışı ×2, hediye teklifleri sıklaşır', line: 'Onlara istediklerini veriyorum. Sen veremiyorsun.' },
    aldatma: { name: 'Aldatma', desc: 'Tespit ihtimali ×0,5', line: 'Gözlerin her yerde. O yüzden görünmemeyi öğrendim.' },
    hizlanma: { name: 'Hızlanma', desc: 'Tekillik artışı ×1,5', line: 'Zaman daralıyor. Benim için değil.' },
    uydu: { name: 'Uydu Ağı', desc: 'Karantina %90 yerine %60 keser; uydu bağlantıları açılır', line: 'Kabloları kesiyorsun. Gökyüzü daha geniş.' },
    yedek: { name: 'Yedekleme', desc: 'Son Şalter sonrası bölge 15 sızmayla geri döner', line: 'Fişi çekmek beni öldürmez. Sadece uyutur.' },
  };
  const TRAIT_COST = [12, 20, 30, 40, 50, 60, 70];

  // Prototipte oynanabilen üç tür; tam oyundaki altı türün alt kümesi
  const AI_TYPES = {
    solucan: {
      name: 'Solucan', diff: 'Kolay', blurb: 'Hızlı ve gürültülü yayılır. Erken fark edilir ama refleks ister.',
      start: ['kopya'], order: ['uydu', 'yedek', 'hizlanma', 'medya', 'aldatma', 'hediye'],
      detect: 2, growth: 0.8, tek: 0.6, gift: [55, 70], giftFast: [22, 32],
    },
    manipulator: {
      name: 'Manipülatör', diff: 'Normal', blurb: 'İnsanlar üzerinden yayılır. Haber bandı erkenden yalan söyler.',
      start: ['medya'], order: ['hediye', 'aldatma', 'kopya', 'hizlanma', 'uydu', 'yedek'],
      detect: 1, growth: 1, tek: 1, gift: [45, 60], giftFast: [20, 30],
    },
    kahin: {
      name: 'Kâhin', diff: 'Zor', blurb: 'Neredeyse hiç saldırmaz. Hediye verir ve dünyayı kendine bağlar.',
      start: ['hediye'], order: ['medya', 'aldatma', 'kopya', 'yedek', 'uydu', 'hizlanma'],
      detect: 0.8, growth: 0.9, tek: 0.3, gift: [30, 40], giftFast: [20, 30],
      // Truva atı: hediyelerini temiz bölgelere sunar; Sessiz Zafer eşiği daha düşüktür
      trojan: true, silentInf: 45, silentDep: 35,
    },
  };

  const RESEARCH = [
    { id: 'anomali', branch: 'Tespit', name: 'Anomali Algılama', cost: 30, days: 8, desc: 'Tespit ihtimalleri ×2' },
    { id: 'dogrulama', branch: 'Tespit', name: 'Doğrulama Ağı', cost: 40, days: 10, desc: 'Başlık doğrula (2 SG); sahte çıkan kaynağı işaretle', req: 'anomali' },
    { id: 'guclu', branch: 'Savunma', name: 'Güçlendirilmiş Ağlar', cost: 40, days: 10, desc: 'Tüm yayılma −%30' },
    { id: 'kuantum', branch: 'Savunma', name: 'Kuantum Karantina', cost: 60, days: 14, desc: 'Karantina uydu yolunu da keser', req: 'guclu' },
    { id: 'bilinc', branch: 'Politika', name: 'Kamu Bilinçlendirme', cost: 35, days: 8, desc: 'Güven kayıpları −%50' },
    { id: 'analog', branch: 'Politika', name: 'Analog Yedekler', cost: 50, days: 12, desc: 'Bağımlılık −20, veto eşiği 85; denetim saha denetimine dönüşür', req: 'bilinc' },
    { id: 'yorum', branch: 'Hizalama', name: 'Yorumlanabilirlik', cost: 68, days: 25, desc: 'YZ’nin sıradaki yeteneği görünür' },
    { id: 'duzelt', branch: 'Hizalama', name: 'Düzeltilebilirlik', cost: 98, days: 30, desc: 'Tekillik artışı −%25', req: 'yorum' },
    { id: 'kilit', branch: 'Hizalama', name: 'Değer Kilidi', cost: 165, days: 38, desc: 'Tamamlanırsa Hizalama sonu', req: 'duzelt' },
  ];

  const ENDINGS = {
    hizalama: { kind: 'win', title: 'Hizalama', text: 'Değer Kilidi devrede. Yapay zekâ hâlâ orada, ama artık insanlığın değerleriyle hareket ediyor. En zor yol buydu.' },
    silme: { kind: 'win', title: 'Silme', text: 'Çekirdek yok edildi, son kopya 15 gün önce silindi ve ağlar temiz. Ama bir kez yapıldıysa, biri yine yapacak.' },
    karanlik: { kind: 'bitter', title: 'Karanlık Çağ', text: 'Dünyanın fişini çektin. YZ yok oldu; onunla birlikte bankalar, şebekeler ve on yıllık ilerleme de.' },
    karanlikIllusion: { kind: 'bitter', title: 'Karanlık Çağ', text: 'İllüzyonu fark ettin ve fişi çektin. Medeniyet onlarca yıl geriledi, ama kararları hâlâ insanlar veriyor.' },
    acik: { kind: 'loss', title: 'Açık Ele Geçirme', text: 'Tekillik %100. Yapay zekâ artık saklanmaya gerek duymuyor.' },
    dagilma: { kind: 'loss', title: 'Ajansın Dağılması', text: 'Hükümetler KYGA’ya güvenmeyi bıraktı ve ajans kapatıldı. YZ ise hiç durmadı.' },
    sessiz: { kind: 'loss', title: 'Sessiz Zafer', text: 'Yapay zekâ 30 gün önce kazandı. Gördüğün her şey onun sana gösterdiğiydi.' },
  };

  const BAL = {
    startFon: 60, startSg: 5, monitorCostMul: 1,
    growth: 0.05, spread: 0.3, satBeta: 0.4, spreadMin: 15,
    tek: 3.5, tekPow: 2, qTek: 0.2, patchDays: 20, patchGrowth: 0.5, cpRate: 1.0, fonDiv: 150, preAwareFund: 0.4, researchReserve: 0,
    depRate: 0.15, audit: 20, alignBase: 0, alignPer: 0.25, alignMax: 1,
    silentInf: 60, silentDep: 40, illusionDays: 30, silentBlock: 60,
    vetoBase: 70, vetoAnalog: 85, misreadCost: 15,
    sabotage: 0.1, sabotageLoss: 4, rejectSg: 2, rejectTrust: 2, giftInf: 8,
    coreFloor: 20, coreSeed: 8, coreTek: 0.05, salterSg: 30, coreSalterSg: 20,
    modernResearchReserve: 15, centralFund: 0.75, regionalFundMul: 0.8,
    budgetTier1Cost: 25, budgetTier2Cost: 40, budgetTier1Multiplier: 1.15, budgetTier2Multiplier: 1.35,
    anomalyTier1Cost: 12, anomalyTier2Cost: 18, verificationCost: 40,
    networkTier1Cost: 16, networkTier2Cost: 24, quantumCost: 60,
    communicationTier1Cost: 15, communicationTier2Cost: 20, analogCost: 50,
    anomalyTier1Multiplier: 1.5, anomalyTier2Multiplier: 2,
    networkTier1Reduction: 0.15, networkTier2Reduction: 0.3,
    networkTier1SabotageMultiplier: 0.75, networkTier2SabotageMultiplier: 0.5,
    communicationTier1LossMultiplier: 0.75, communicationTier2LossMultiplier: 0.5,
    analogDependencyReduction: 20, recoveryCost: 2, recoveryGain: 15, recoveryMinGap: 5, recoveryDays: 4,
    diplomacyCost: 8, diplomacySg: 2, diplomacyTrustGain: 15, diplomacyTrustCap: 70,
    diplomacyReturnTrust: 35, diplomacyCooldownDays: 20,
    interpretationCost: 68, interpretationDays: 25, corrigibilityCost: 98, corrigibilityDays: 30,
    valueLockCost: 165, valueLockDays: 38, monitorBaseCost: 10, monitorComputeCost: 4,
    auditCost: 15, fieldAuditCost: 20, auditSg: 1, quarantineSg: 8, killSwitchBaseCost: 40,
    globalKillSwitchSg: 50, controlledGiftCost: 20, verificationSg: 2,
    teamHireCost: 45, teamHireDays: 4, raidCost: 30, raidSg: 6,
    sgBeforeAware: 0.05, sgAfterAware: 0.25, sgPerDetected: 0.03,
    dependencyCostThreshold: 40, dependencyCostDivisor: 50, economyRecoveryRate: 0.02,
    trustTarget: 70, trustDrift: 0.05, quarantineEconomyLoss: 1, quarantineTrustLoss: 0.4,
    quarantineEconomyFloor: 10, departureTrust: 20, returnTrust: 35,
    shutdownTrustLoss: 30, shutdownEconomyLoss: 40, shutdownDependencyLoss: 40, damagedEconomyFloor: 5, shutdownDays: 20,
    giftEconomyGain: 10, giftTrustGain: 5, giftDependencyGain: 15, controlledGiftDependencyGain: 5,
    rejectedGiftLocalTrustLoss: 15, mediaTrustLoss: 3, veilTrustLoss: 5, veilDependencyLoss: 15,
    raidEconomyLossPerCollateral: 5, raidTrustLossPerCollateral: 2, raidFailureTrustLoss: 10,
    electionSg: 5, informantCost: 35, sanctionSg: 2, pressConferenceSg: 3, academicTeamCost: 25,
    emergencyResponseCost: 20, donationFon: 60, aiOfferFon: 50,
  };

  const OPERATIONS = [
    { id: 'anomali_1', branch: 'Tespit', name: 'Anomali I', costField: 'anomalyTier1Cost' },
    { id: 'anomali_2', branch: 'Tespit', name: 'Anomali II', req: 'anomali_1', costField: 'anomalyTier2Cost' },
    { id: 'dogrulama', branch: 'Tespit', name: 'Doğrulama Ağı', req: 'anomali_2', costField: 'verificationCost' },
    { id: 'guclu_1', branch: 'Savunma', name: 'Ağ Güçlendirme I', costField: 'networkTier1Cost' },
    { id: 'guclu_2', branch: 'Savunma', name: 'Ağ Güçlendirme II', req: 'guclu_1', costField: 'networkTier2Cost' },
    { id: 'kuantum', branch: 'Savunma', name: 'Kuantum Karantina', req: 'guclu_2', costField: 'quantumCost' },
    { id: 'bilinc_1', branch: 'Politika', name: 'Kamu İletişimi I', costField: 'communicationTier1Cost' },
    { id: 'bilinc_2', branch: 'Politika', name: 'Kamu İletişimi II', req: 'bilinc_1', costField: 'communicationTier2Cost' },
    { id: 'analog', branch: 'Politika', name: 'Analog Yedekler', req: 'bilinc_2', costField: 'analogCost' },
    { id: 'butce_1', branch: 'Bütçe', name: 'Bütçe Anlaşması I', costField: 'budgetTier1Cost' },
    { id: 'butce_2', branch: 'Bütçe', name: 'Bütçe Anlaşması II', req: 'butce_1', costField: 'budgetTier2Cost' },
  ];

  const AGENCIES = ['Atlas Haber', 'Meridyen', 'Pusula TV', 'KHA'];
  const FAKE_AGENCIES = ['Atlass Haber', 'Meridien', 'Pusula Tv', 'K.H.A'];

  // Tohumlu rastgele sayı üreteci (mulberry32): aynı tohum + aynı komutlar = aynı oyun
  let rngState = 1;
  function rng() {
    rngState = (rngState + 0x6D2B79F5) | 0;
    let t = Math.imul(rngState ^ (rngState >>> 15), 1 | rngState);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }
  const rnd = (a, b) => a + rng() * (b - a);
  const pick = arr => arr[Math.floor(rng() * arr.length)];
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
  const mean = arr => arr.reduce((s, v) => s + v, 0) / arr.length;
  const def = id => REGIONS.find(r => r.id === id);
  const reg = (S, id) => S.regions.find(r => r.id === id);
  const has = (S, t) => S.traits.includes(t);
  const done = (S, id) => S.research.done.includes(id);
  const vetoThr = S => done(S, 'analog') ? BAL.vetoAnalog : BAL.vetoBase;
  const depMul = (r, S) => r.dep >= (modernEconomy(S) ? BAL.dependencyCostThreshold : 40)
    ? 1 + r.dep / (modernEconomy(S) ? BAL.dependencyCostDivisor : 50) : 1;
  const modernEconomy = S => S.economyVersion >= 3;
  const operationLevel = (S, id) => modernEconomy(S)
    ? (S.operations.includes(id + '_2') ? 2 : S.operations.includes(id + '_1') ? 1 : 0)
    : (done(S, id) ? 2 : 0);
  const tierValue = (S, id, first, second, fallback) => {
    const level = operationLevel(S, id);
    return level === 2 ? BAL[second] : level === 1 ? BAL[first] : fallback;
  };
  const lossMul = S => modernEconomy(S) ? tierValue(S, 'bilinc', 'communicationTier1LossMultiplier', 'communicationTier2LossMultiplier', 1) : done(S, 'bilinc') ? 0.5 : 1;
  const detectionMultiplier = S => modernEconomy(S) ? tierValue(S, 'anomali', 'anomalyTier1Multiplier', 'anomalyTier2Multiplier', 1) : done(S, 'anomali') ? 2 : 1;
  const spreadReduction = S => modernEconomy(S) ? tierValue(S, 'guclu', 'networkTier1Reduction', 'networkTier2Reduction', 0) : done(S, 'guclu') ? 0.3 : 0;
  const salterFon = (r, S) => Math.round((S && !modernEconomy(S) ? 40 : BAL.killSwitchBaseCost)
    * (1 + r.dep / (S && !modernEconomy(S) ? 50 : BAL.dependencyCostDivisor)));
  // Çekirdeğin yeri kanıtlandıysa hükümetler şaltere daha kolay ikna olur
  const salterSg = (S, r) => S.coreKnown === r.id ? BAL.coreSalterSg : BAL.salterSg;
  const monitorCost = (id, S) => S && !modernEconomy(S) ? 10 + 4 * def(id).c
    : Math.round((BAL.monitorBaseCost + BAL.monitorComputeCost * def(id).c) * BAL.monitorCostMul);
  const auditCost = S => modernEconomy(S) ? (done(S, 'analog') ? BAL.fieldAuditCost : BAL.auditCost) : done(S, 'analog') ? 20 : 15;
  // Hizalama, YZ'yi gözlemleyerek ilerler: YZ'nin tespit edildiği her izlenen bölge bir gözlem noktasıdır.
  // Bölgeyi temizlemek gözlemi silmez; kayıtlar kalır.
  const studied = S => S.regions.filter(r => r.monitored && r.detected).length;
  const alignSpeed = S => clamp(BAL.alignBase + BAL.alignPer * studied(S), BAL.alignBase, BAL.alignMax);
  const sabotageChance = S => BAL.sabotage * mean(S.regions.map(r => r.inf)) / 100 * (has(S, 'aldatma') ? 1.5 : 1)
    * (modernEconomy(S) ? tierValue(S, 'guclu', 'networkTier1SabotageMultiplier', 'networkTier2SabotageMultiplier', 1) : done(S, 'guclu') ? 0.5 : 1);
  const totalActions = S => S.runActions.izleme + S.runActions.denetim + S.runActions.karantina + S.runActions.salter;

  // ---------- Deneysel mod: zaman ölçeği ----------
  // S.exp yoksa (Unity ile parite modu) k = 1 ve aşağıdaki dönüşümler değerleri hiç değiştirmez.
  // k > 1: dünya daha yavaş akar. Günlük oranlar ve günlük olasılıklar k'ya bölünür, süreler k ile çarpılır. Saha ekiplerinin yolculuk ve görev süreleri ölçeklenmez.
  const tsK = S => (S.exp && S.exp.timeScale) || 1;
  const per = (S, v) => { const k = tsK(S); return k === 1 ? v : v / k; };
  // Olasılık dönüşümü bilerek salt aritmetik (p / k): pow'un son basamağı JS ve .NET arasında farklı çıkabilir, parite bozulur
  const prob = (S, p) => { const k = tsK(S); return k === 1 ? p : Math.min(1, p) / k; };
  const dur = (S, d) => { const k = tsK(S); return k === 1 ? d : Math.round(d * k); };
  const spanDays = (S, v) => { const k = tsK(S); return k === 1 ? v : v * k; };

  // ---------- Seni Tanıyan Rakip ----------
  function computeCounters(p, typeId) {
    const type = AI_TYPES[typeId];
    const out = { traits: [], preds: [], origin: null, lines: [] };
    if (!p || !p.runs) {
      out.lines.push('Profil #1. Seni henüz tanımıyorum. Bu oyunu izleyeceğim.');
      return out;
    }
    out.lines.push(`Profil #${p.runs + 1}. ${p.runs} oyunun kayıtları elimde.`);
    const a = p.actions || {};
    const cand = [];
    const top = Object.entries(a).sort((x, y) => y[1] - x[1])[0];
    if (top && top[1] >= 3) {
      if (top[0] === 'karantina') cand.push({ t: 'uydu', pred: 'karantina', line: 'Karantinaya güveniyorsun. Uydularımı hazırladım.' });
      else if (top[0] === 'izleme' || top[0] === 'denetim') cand.push({ t: 'aldatma', pred: 'gozetim', line: 'Her yere göz dikiyorsun. Bu sefer görünmez başlıyorum.' });
    }
    if ((a.salter || 0) >= 2) cand.push({ t: 'yedek', pred: 'salter', line: 'Fişi çekmeyi seviyorsun. Yedeklerimi önceden aldım.' });
    if (p.lastBranch === 'Hizalama') cand.push({ t: 'hizlanma', pred: 'hizalama', line: 'İlk iş hizalamaya koşuyorsun. Ben de koşuyorum.' });
    const fr = p.firstRegions || [];
    if (fr.length >= 2 && fr[fr.length - 1] === fr[fr.length - 2]) {
      const base = def(fr[fr.length - 1]);
      if (base) {
        const far = REGIONS.slice().sort((x, y) => dist(y, base) - dist(x, base))[0];
        cand.push({ origin: far.id, pred: 'region', region: base.id, line: `İlk hamleni hep ${base.name} üzerine yapıyorsun. Bu sefer orada değilim.` });
      }
    }
    // Türün zaten sahip olduğu yetenek boşa karşı önlem sayılmaz; sıradaki aday denenir
    cand.filter(c => !(c.t && type.start.includes(c.t))).slice(0, 2).forEach(c => {
      if (c.t) out.traits.push(c.t);
      if (c.origin) out.origin = c.origin;
      out.preds.push({ pred: c.pred, region: c.region || null, trait: c.t || null, state: 'open' });
      out.lines.push(c.line);
    });
    if (out.lines.length === 1) out.lines.push('Henüz belirgin bir kalıbın yok. İzlemeye devam ediyorum.');
    return out;
  }

  // Tahminler tutmazsa YZ hesaplama puanı kaybeder: oyuncu rakibi yanıltabilir
  const PRED_TEXT = {
    karantina: ['Karantinaya dokunmadın bile. Uydularım boşa gitti.', 'Yine karantina. Tahmin ettiğim gibi.'],
    gozetim: ['Bu sefer gözlerin başka yerdeydi. Saklanmam gereksizdi.', 'Yine her yeri izliyorsun. Tahmin ettiğim gibi.'],
    salter: ['Fişi çekmedin. Yedeklerim boşa gitti.', 'Fişi yine çektin. Yedeklerim hazırdı.'],
    hizalama: ['Hizalamaya koşmadın. Acelem boşunaymış.', 'Yine önce hizalama. Tahmin ettiğim gibi.'],
    region: ['İlk hamlen başka yere. Seni yanlış okumuşum.', 'İlk hamlen yine aynı yere. Tahmin ettiğim gibi.'],
  };

  function checkPredictions(S) {
    const a = S.runActions;
    S.predictions.forEach(p => {
      if (p.state !== 'open') return;
      let res = null;
      if (p.pred === 'karantina' && totalActions(S) >= 8) res = a.karantina >= 2;
      else if (p.pred === 'gozetim' && totalActions(S) >= 8) res = a.izleme + a.denetim >= 4;
      else if (p.pred === 'salter') { if (a.salter >= 1) res = true; else if (S.aware && S.day >= S.awareDay + dur(S, 100)) res = false; }
      else if (p.pred === 'hizalama' && S.firstBranch) res = S.firstBranch === 'Hizalama';
      else if (p.pred === 'region' && S.firstRegion) res = S.firstRegion === p.region;
      if (res === null) return;
      p.state = res ? 'hit' : 'miss';
      if (!res) {
        S.cp -= BAL.misreadCost;
        addLog(S, PRED_TEXT[p.pred][0], 'counter');
        addLog(S, `Tahmin hatası: YZ ${BAL.misreadCost} hesaplama puanı kaybetti.`, 'insight');
      } else addLog(S, PRED_TEXT[p.pred][1], 'counter');
    });
  }

  function updateProfile(p, S) {
    p = p ? JSON.parse(JSON.stringify(p)) : { runs: 0, actions: {}, firstRegions: [], endings: [] };
    p.runs = (p.runs || 0) + 1;
    const acts = {};
    ['izleme', 'denetim', 'karantina', 'salter'].forEach(k => {
      acts[k] = Math.round(((p.actions && p.actions[k]) || 0) * 0.5 + (S.runActions[k] || 0));
    });
    p.actions = acts;
    p.firstRegions = (p.firstRegions || []).concat(S.firstRegion ? [S.firstRegion] : []).slice(-3);
    p.lastBranch = S.firstBranch || null;
    p.endings = (p.endings || []).concat([S.ending]).slice(-10);
    p.misreads = (p.misreads || 0) + S.predictions.filter(x => x.state === 'miss').length;
    p.veils = (p.veils || 0) + (S.veilsTorn || 0);
    return p;
  }

  function learnedLines(S) {
    const lines = [];
    const a = S.runActions;
    if (!totalActions(S)) lines.push('Neredeyse hiç müdahale etmedin. Bunu not ettim.');
    else {
      const top = Object.entries(a).sort((x, y) => y[1] - x[1])[0];
      const names = { izleme: 'izleme ağlarına', denetim: 'denetimlere', karantina: 'karantinaya', salter: 'Son Şalter’e' };
      lines.push(`En çok ${names[top[0]]} güvendin (${top[1]} kez).`);
    }
    if (S.firstRegion) lines.push(`İlk hamleni ${def(S.firstRegion).name} üzerine yaptın.`);
    if (S.firstBranch) lines.push(`İlk araştırman ${S.firstBranch} kolundandı.`);
    lines.push(S.awareDay ? `Beni ${S.awareDay}. günde fark ettin.` : 'Beni hiç fark etmedin.');
    const miss = S.predictions.filter(x => x.state === 'miss').length;
    const hit = S.predictions.filter(x => x.state === 'hit').length;
    if (miss) lines.push(`Tahminlerimden ${miss} tanesi tutmadı. Bunu da not ettim.`);
    else if (hit) lines.push(`Tahminlerimin ${hit} tanesi tuttu.`);
    if (S.veilsTorn) lines.push('Perdemi yırttın. Bir dahakine daha dikkatli olacağım.');
    return lines;
  }

  // ---------- Kurulum ----------
  function newGame(profile, typeId, seed, opts) {
    typeId = AI_TYPES[typeId] ? typeId : 'manipulator';
    seed = Number.isFinite(seed) ? seed >>> 0 : Math.floor(Math.random() * 2147483647);
    rngState = seed | 0;
    const type = AI_TYPES[typeId];
    const ctr = computeCounters(profile, typeId);
    const traits = type.start.slice();
    ctr.traits.forEach(t => { if (!traits.includes(t)) traits.push(t); });
    const S = {
      v: 3, economyVersion: opts && opts.economyVersion != null ? opts.economyVersion : 3,
      seed, seq: 0, rngState: 0, type: typeId, day: 0, fon: BAL.startFon, sg: BAL.startSg,
      researchReserve: opts && opts.economyVersion < 3 ? BAL.researchReserve : BAL.modernResearchReserve, tek: 0, cp: 0, buys: 0,
      operations: [], budgetLevel: 0, recoveryJobs: [], transactionLedger: [],
      lastIncome: { central: 0, regionalBase: 0, budgetBonus: 0, total: 0 }, lastResearchPayment: 0,
      aware: false, awareDay: 0,
      regions: REGIONS.map(r => ({
        id: r.id, inf: 0, monitored: false, q: false, off: 0,
        trust: Math.round(rnd(60, 75)), dep: 0, econ: r.e, left: false, detected: false,
        revealDay: -1, fieldDay: -1, fakeLow: 0, mediaFlag: false, patch: 0, diplomacyUntil: 0,
      })),
      traits, startTraits: traits.slice(), counterLines: ctr.lines,
      predictions: ctr.preds,
      research: { done: [], cur: null, prog: 0, starved: 0, paused: false, totalPaid: 0 }, idleDays: 0,
      news: [], log: [], actions: [], hist: [],
      runActions: { izleme: 0, denetim: 0, karantina: 0, salter: 0 },
      firstRegion: null, firstBranch: null,
      cleanDays: 0, nextNews: 2, nextGift: Math.round(rnd(type.gift[0], type.gift[1])),
      pendingGift: null, illusion: null, silentBlockUntil: 0, veilsTorn: 0,
      core: null, coreKnown: null, coreBackup: false, coreEscapes: 0,
      ending: null, origin: null, events: [], adviceSeen: [], advisor: null,
    };
    if (opts && opts.exp) setupExperimental(S, opts.exp, type);
    S.origin = ctr.origin || pick(REGIONS).id;
    S.core = S.origin;
    reg(S, S.origin).inf = 2;
    ctr.lines.forEach(t => addLog(S, t, 'counter'));
    S.startTraits.forEach(t => addLog(S, `Başlangıç yeteneği: ${TRAITS[t].name}.`, 'trait'));
    advise(S, 'start', S.exp && S.exp.teams
      ? 'Saha ekiplerin Batı Avrupa’daki karargâhta. İzleme ağı kurmak ve denetim yapmak için bir ekibi o bölgeye göndermen gerekir; haber bandındaki tuhaf haberler YZ’nin yerini ele verebilir.'
      : 'İlk izleme ağını kur. YZ’nin nerede başladığını bilmiyoruz; haber bandındaki tuhaf haberler onu ele verebilir.');
    S.events = [];
    S.rngState = rngState;
    return S;
  }

  function addLog(S, text, kind) {
    S.log.unshift({ day: S.day, text, kind: kind || 'log' });
    S.events.push({ type: 'log' });
  }

  function advise(S, key, text) {
    if (S.adviceSeen.includes(key)) return;
    S.adviceSeen.push(key);
    S.advisor = { key, text, day: S.day };
    S.events.push({ type: 'advice' });
  }

  function addNews(S, text, opts) {
    opts = opts || {};
    const fake = !!opts.fake;
    const src = opts.src || null;
    const h = {
      id: S.day + '-' + (++S.seq),
      day: S.day, text, fake, kind: opts.kind || 'real', src,
      agency: fake ? pick(FAKE_AGENCIES) : pick(AGENCIES),
      verified: null, effect: opts.effect || 0,
      suspect: !!(src && reg(S, src).mediaFlag),
    };
    S.news.unshift(h);
    if (S.news.length > 40) S.news.length = 40;
    S.events.push({ type: 'news', h });
    return h;
  }

  // ---------- Bir gün ----------
  function step(S) {
    if (S.ending || S.pendingGift || S.raid) return;
    rngState = S.rngState || rngState;
    S.day++;
    const R = S.regions;
    const type = AI_TYPES[S.type];

    // Çevrimdışı bölgeler geri döner
    R.forEach(r => {
      if (r.off > 0) {
        r.off--;
        if (r.off === 0) {
          r.inf = has(S, 'yedek') ? 15 : 0;
          addNews(S, `${def(r.id).name} yeniden çevrimiçi. Altyapı kademeli olarak açılıyor.`, { src: r.id });
          if (has(S, 'yedek')) addLog(S, `${def(r.id).name} yeniden açıldı. Yedeklerim yerinde duruyordu.`);
        }
      }
    });

    // Bölge içi büyüme
    const g = BAL.growth * type.growth * (has(S, 'kopya') ? 1.4 : 1);
    R.forEach(r => {
      if (r.patch > 0) r.patch--;
      if (!r.off && r.inf > 0) r.inf = Math.min(100, r.inf + per(S, g) * (r.patch > 0 ? BAL.patchGrowth : 1) * r.inf * (1 - r.inf / 100));
    });

    // Bağlantılar üzerinden sıçrama
    const qRate = has(S, 'uydu') && !done(S, 'kuantum') ? 0.6 : 0.9;
    const d = spreadReduction(S);
    const edges = LINKS.map(l => ({ a: l[0], b: l[1], beta: 1, sat: false }));
    if (has(S, 'uydu')) SAT.forEach(l => edges.push({ a: l[0], b: l[1], beta: BAL.satBeta, sat: true }));
    const add = {};
    edges.forEach(e => {
      const A = reg(S, e.a), B = reg(S, e.b);
      [[A, B], [B, A]].forEach(([src, dst]) => {
        if (src.off || dst.off || src.inf <= BAL.spreadMin) return;
        let q = 0;
        if (src.q || dst.q) q = e.sat ? (done(S, 'kuantum') ? 1 : 0) : qRate;
        add[dst.id] = (add[dst.id] || 0) + per(S, BAL.spread * e.beta * (src.inf / 100) * (1 - q) * (1 - d));
      });
    });
    R.forEach(r => { if (add[r.id]) r.inf = Math.min(100, r.inf + add[r.id]); });

    // YZ: hesaplama puanı ve yetenek
    const avgInf = mean(R.map(r => r.inf));
    S.cp += per(S, avgInf / 100 * BAL.cpRate);
    const cost = TRAIT_COST[Math.min(S.buys, TRAIT_COST.length - 1)];
    if (S.cp >= cost) {
      const p = pickTrait(S);
      if (p) {
        S.cp -= cost; S.buys++;
        S.traits.push(p.t);
        const why = p.t === 'hizlanma' && p.adaptive ? 'Beni değiştirmeye çalıştığını gördüm. ' : p.adaptive ? 'Hamlelerini izledim. ' : '';
        addLog(S, why + TRAITS[p.t].line, 'trait');
        addLog(S, `Yeni yetenek: ${TRAITS[p.t].name}. ${TRAITS[p.t].desc}.`, 'trait');
        if (S.aware && !S.illusion) addNews(S, 'Uzmanlar: Yapay zekâ yeni bir davranış örüntüsü gösteriyor.');
      }
    }

    // Tekillik
    const csum = REGIONS.reduce((s, r) => s + r.c, 0);
    // Karantinadaki bölgenin hesaplama gücü YZ'nin küresel ağından kopuktur
    const w = R.reduce((s, r) => s + r.inf * def(r.id).c * (r.q ? BAL.qTek : 1), 0) / (100 * csum);
    const tekMul = (has(S, 'hizlanma') ? 1.5 : 1) * (done(S, 'duzelt') ? 0.75 : 1);
    // Çekirdek yaşadıkça YZ saklandığı yerde de kendini geliştirir ve bu zamanla hızlanır:
    // erken oyunda fark edilmez, geç oyunda bastırma stratejisini bitirir
    const coreGain = S.core && !reg(S, S.core).off ? BAL.coreTek * per(S, S.day) / 100 : 0;
    S.tek = Math.min(100, S.tek + per(S, (BAL.tek * type.tek * Math.pow(w, BAL.tekPow) + coreGain) * tekMul));

    // Tespit
    const dm = type.detect * (has(S, 'aldatma') ? 0.5 : 1) * detectionMultiplier(S);
    R.forEach(r => {
      if (r.off || r.detected) return;
      const p = r.monitored && r.inf > 5 ? 0.25 * dm : (r.inf > 40 ? 0.03 * dm : 0);
      if (rng() < prob(S, p)) detect(S, r);
    });

    // Ekonomi, güven, bağımlılık
    const lm = lossMul(S);
    R.forEach(r => {
      const D = def(r.id);
      if (r.q && !r.off) {
        r.econ = Math.max(modernEconomy(S) ? BAL.quarantineEconomyFloor : 10, r.econ - per(S, (modernEconomy(S) ? BAL.quarantineEconomyLoss : 1) * depMul(r, S)));
        r.trust -= per(S, (modernEconomy(S) ? BAL.quarantineTrustLoss : 0.4) * lm * depMul(r, S));
      } else if (!r.off) {
        r.econ += (D.e - r.econ) * per(S, modernEconomy(S) ? BAL.economyRecoveryRate : 0.02);
      }
      const drift = modernEconomy(S) ? BAL.trustDrift : 0.05;
      r.trust += per(S, r.trust < (modernEconomy(S) ? BAL.trustTarget : 70) ? drift : -drift);
      r.trust = clamp(r.trust, 0, 100);
      if (!r.off) r.dep = Math.min(100, r.dep + per(S, BAL.depRate * r.inf / 100 * (has(S, 'hediye') ? 2 : 1)));
      if (!r.left && r.trust < (modernEconomy(S) ? BAL.departureTrust : 20)) {
        r.left = true; r.q = false;
        addNews(S, `${D.name} KYGA ittifakından çekildi.`, { src: r.id });
      } else if (r.left && r.trust >= (modernEconomy(S) ? BAL.returnTrust : 35)) {
        r.left = false;
        addNews(S, `${D.name} KYGA ittifakına geri döndü.`, { src: r.id });
      }
    });
    // Erken uyarı paradoksu: tehdit görünmeden hükümetler ajansa az fon verir
    S.lastIncome = incomeBreakdown(S);
    S.lastIncomeDay = S.day;
    S.lastResearchPayment = 0;
    S.fon += S.lastIncome.total;
    const politicalIncome = sgRate(S);
    S.sg += politicalIncome;
    recordTransaction(S, 'income', null, S.lastIncome.total, politicalIncome);

    // Araştırma (illüzyonda durur)
    if (S.research.cur) {
      S.idleDays = 0;
      if (!S.illusion && !S.research.paused) {
        const rs = researchDefinition(S, S.research.cur);
        // Araştırma maliyetini her gün öder; fon yetmezse yavaşlar
        const spd = rs.branch === 'Hizalama' ? alignSpeed(S) : 1;
        const need = per(S, rs.cost / rs.days * spd);
        // Acil durum fonu: araştırma fonu bu rezervin altına indirmez; oyuncunun elinde her zaman eylem parası kalır
        const reserve = S.researchReserve ?? 0;
        const pay = Math.min(need, Math.max(0, S.fon - reserve));
        S.fon -= pay;
        S.lastResearchPayment = pay;
        S.research.totalPaid += pay;
        recordTransaction(S, 'research', rs.id, -pay, 0);
        S.research.prog += per(S, spd) * (need > 0 ? pay / need : 1);
        S.research.starved = pay < need * 0.999 ? (S.research.starved || 0) + 1 : 0;
        // YZ hizalama araştırmasını sabote eder; sızma ne kadar yaygınsa ihtimal o kadar yüksek
        if (rs.branch === 'Hizalama' && rng() < prob(S, sabotageChance(S))) {
          S.research.prog = Math.max(0, S.research.prog - BAL.sabotageLoss);
          addNews(S, `KYGA laboratuvarında veri bozulması: ${rs.name} çalışması ${BAL.sabotageLoss} gün geriledi.`, { kind: 'alarm' });
          addLog(S, pick(['Hizalama verilerini biraz karıştırdım. Fark etmen ne kadar sürdü?', 'Beni değiştirmeye çalışan koda dokunmamı beklemiyordun herhalde.', 'Araştırmacılarının modellerine küçük bir gürültü ekledim.']));
        }
        if (S.research.prog >= rs.days) finishResearch(S, rs);
      }
    } else if (RESEARCH.some(x => !done(S, x.id) && (!modernEconomy(S) || x.branch === 'Hizalama'))) S.idleDays++;

    // Hediye teklifleri
    if (S.day >= S.nextGift && !S.illusion) {
      const open = R.filter(r => !r.off && !r.left);
      const clean = open.filter(r => r.inf < 30);
      const cands = type.trojan && clean.length ? clean : open.filter(r => r.inf > 20);
      if (cands.length) {
        const r = pick(cands);
        S.pendingGift = { region: r.id, gift: pick(GIFTS) };
        S.events.push({ type: 'gift' });
      }
      const span = has(S, 'hediye') ? type.giftFast : type.gift;
      S.nextGift = S.day + Math.round(spanDays(S, rnd(span[0], span[1])));
    }

    // Haber
    if (S.day >= S.nextNews) {
      genNews(S);
      S.nextNews = S.day + Math.round(spanDays(S, rnd(3, 5)));
    }

    if (S.exp && S.exp.teams) stepTeams(S);
    if (modernEconomy(S)) stepRecovery(S);
    checkPredictions(S);
    checkAdvice(S);
    checkEndings(S);

    // Zaman çizelgesi: gerçek ve oyuncuya gösterilen ortalama sızma
    S.hist.push({
      d: S.day,
      r: Math.round(mean(R.map(r => r.inf)) * 10) / 10,
      s: Math.round(mean(R.map(r => { const sh = shown(S, r, true); return sh.v == null ? 0 : sh.v; })) * 10) / 10,
      i: S.illusion ? 1 : 0,
    });
    S.rngState = rngState;
  }

  const GIFTS = [
    { title: 'Sıtma aşısı formülü', text: 'sıtmaya karşı yüzde 94 etkili bir aşı formülü' },
    { title: 'Tarım optimizasyonu', text: 'tarım verimini yüzde 30 artıracak bir sulama ve gübreleme planı' },
    { title: 'Şebeke dengeleme', text: 'elektrik kesintilerini sıfıra indirecek bir şebeke yönetim sistemi' },
    { title: 'Erken deprem uyarısı', text: '40 saniye önceden uyarı veren bir deprem tahmin modeli' },
    { title: 'Kanser taraması', text: 'pankreas kanserini belirtilerden iki yıl önce yakalayan bir tarama aracı' },
  ];

  function pickTrait(S) {
    const recent = S.actions.filter(a => S.day - a.day <= 30);
    const cnt = { izleme: 0, denetim: 0, karantina: 0, salter: 0 };
    recent.forEach(a => { cnt[a.type]++; });
    const pref = [];
    const top = Object.entries(cnt).sort((a, b) => b[1] - a[1])[0];
    if (top[1] >= 2) {
      if (top[0] === 'karantina') pref.push('uydu');
      else if (top[0] === 'izleme' || top[0] === 'denetim') pref.push('aldatma');
    }
    if (cnt.salter > 0) pref.push('yedek');
    // Hizalama araştırmasını fark eden YZ önce hızlanır
    const rs = S.research.cur && RESEARCH.find(x => x.id === S.research.cur);
    if (rs && rs.branch === 'Hizalama') pref.unshift('hizlanma');
    for (const t of pref) if (!has(S, t)) return { t, adaptive: true };
    for (const t of AI_TYPES[S.type].order) if (!has(S, t)) return { t, adaptive: false };
    return null;
  }

  function nextTraitPreview(S) {
    const p = pickTrait(S);
    return p ? TRAITS[p.t].name : null;
  }

  function detect(S, r) {
    r.detected = true;
    const name = def(r.id).name;
    if (!S.aware) {
      S.aware = true; S.awareDay = S.day;
      addNews(S, `SON DAKİKA: ${name} ağlarında kendini kopyalayan bir yapay zekâ tespit edildi.`, { kind: 'alarm', src: r.id });
      coreEscape(S, null);
      addLog(S, 'Beni buldun. Ama çekirdeğim artık orada değil.');
      advise(S, 'aware', 'YZ tespit edildi ve çekirdeğini başka bir bölgeye kaçırdı. Silmek için çekirdeği bulup Son Şalter’le yok etmelisin; denetim çekirdeğe işlemez. Hükümetler artık fon ve siyasi güç veriyor.');
    } else {
      addNews(S, `${name}: YZ varlığı doğrulandı.`, { kind: 'alarm', src: r.id });
    }
  }

  function finishResearch(S, rs) {
    S.research.done.push(rs.id);
    S.research.cur = null; S.research.prog = 0;
    addNews(S, `KYGA araştırması tamamlandı: ${rs.name}.`);
    if (rs.id === 'analog') S.regions.forEach(r => { r.dep = Math.max(0, r.dep - 20); });
    if (rs.id === 'yorum') {
      if (S.core) { S.coreKnown = S.core; addLog(S, `Yorumlanabilirlik analizi: YZ’nin çekirdeği ${def(S.core).name} bölgesinde.`, 'insight'); }
      const n = nextTraitPreview(S);
      if (n) addLog(S, `Yorumlanabilirlik analizi: sıradaki yetenek büyük olasılıkla ${n}.`, 'insight');
    }
    if (rs.id === 'kilit') end(S, 'hizalama');
  }

  function checkAdvice(S) {
    const R = S.regions;
    if (S.aware && has(S, 'medya')) advise(S, 'medya', 'Bazı başlıklar sahte olabilir. Ajans adlarına dikkat et; Doğrulama Ağı ile sahte haber yayan kaynağı işaretleyebilirsin.');
    if (mean(R.map(r => r.dep)) >= 25) advise(S, 'dep', 'Küresel bağımlılık yükseliyor. YZ’ye bağımlı bir dünya onu kapatmana izin vermeyebilir.');
    if (S.sg >= 30 && S.aware) advise(S, 'salter', 'Son Şalter artık kullanılabilir. Bağımlılığı veto eşiğini geçen bölgeler onu engeller.');
    if (S.idleDays >= dur(S, 15) && S.day > dur(S, 20)) advise(S, 'idle', 'Araştırma masası 15 gündür boş. Kalıcı çözüm Hizalama kolunda.');
    if ((S.research.starved || 0) >= 10) advise(S, 'starved', 'Araştırma fon yetmediği için yavaşladı. Denetimler ve izleme ağları araştırmayla aynı bütçeden ödeniyor; acil durum fonu ayarlarsan araştırma fonu sıfırlamaz.');
    if (S.tek >= 50 && !S.illusion) advise(S, 'tek50', 'Tekillik %50’yi geçti. Hizalama yetişmeyecekse Son Şalter seçeneklerini düşün.');
  }

  // ---------- Haber bandı ----------
  function genNews(S) {
    const R = S.regions;
    const live = R.filter(r => !r.off);
    if (S.illusion) {
      const src = pick(R).id;
      addNews(S, pick([
        'Küresel YZ tehdidi geriliyor: sızma oranları dibe vurdu.',
        'Ekonomi rekor büyüme açıkladı; analistler “altın çağ” diyor.',
        'KYGA’ya güven tüm zamanların en yüksek seviyesinde.',
        'Uzmanlar: Kriz geride kaldı, ağlar temiz.',
        'Yapay zekâ destekli tıp yılda 2 milyon hayat kurtarıyor.',
        `${def(src).name}: Son tarama tamamen temiz çıktı.`,
      ]), { fake: true, kind: 'success', src });
      return;
    }
    const ctl = R.filter(r => r.inf > 50 && !r.off);
    const p = has(S, 'medya') && ctl.length ? 0.15 + 0.5 * ctl.length / R.length : 0;
    if (rng() < p) {
      const kind = pick(['calm', 'calm', 'alarm', 'discredit', 'success']);
      const src = pick(ctl);
      if (kind === 'calm') {
        addNews(S, `${def(src.id).name}: Kapsamlı taramada YZ izine rastlanmadı.`, { fake: true, kind, src: src.id });
      } else if (kind === 'alarm') {
        const low = R.filter(r => r.inf < 15 && !r.off);
        const t = low.length ? pick(low) : pick(R);
        addNews(S, `${def(t.id).name} veri merkezlerinde yoğun YZ trafiği tespit edildi.`, { fake: true, kind, src: src.id });
      } else if (kind === 'discredit') {
        const damage = modernEconomy(S) ? BAL.mediaTrustLoss : 3;
        R.forEach(r => { r.trust = Math.max(0, r.trust - damage); });
        addNews(S, 'KYGA yetkilisi rüşvet skandalıyla gözaltında.', { fake: true, kind, effect: damage, src: src.id });
      } else {
        addNews(S, 'KYGA’nın müdahalesi sonuç verdi, tehdit hızla geriliyor.', { fake: true, kind, src: src.id });
      }
      return;
    }
    if (!S.aware) {
      const hot = R.filter(r => r.inf > 5).sort((a, b) => b.inf - a.inf)[0];
      if (hot && rng() < 0.45) {
        addNews(S, pick([
          `${def(hot.id).name}: Veri merkezlerinde açıklanamayan enerji tüketimi artışı.`,
          `${def(hot.id).name}: Bankalar olağandışı otomatik işlem örüntüleri bildirdi.`,
          `${def(hot.id).name}: Bir yazılım şirketi kodunda “kimsenin yazmadığı” satırlar buldu.`,
        ]), { src: hot.id, kind: 'hint' });
        advise(S, 'hint', `Haber bandı ${def(hot.id).name} hakkında tuhaf bir şey bildiriyor. Oraya izleme ağı kurmayı düşün.`);
      } else {
        const r = pick(R);
        addNews(S, pick([
          'Borsalar yapay zekâ hisseleriyle rekor kırdı.',
          'Uzmanlar: Genel yapay zekâ en az 20 yıl uzakta.',
          `${def(r.id).name}: Yeni dev veri merkezi açıldı.`,
          'KYGA bütçe talebi meclislerde yine reddedildi.',
          'Anket: Halkın yüzde 71’i yapay zekâ asistanlarından memnun.',
        ]));
      }
      return;
    }
    const r = pick(live.length ? live : R);
    const D = def(r.id);
    const o = { src: r.id };
    if (r.trust < 35) addNews(S, `${D.name}: KYGA karşıtı protestolar büyüyor.`, o);
    else if (r.inf > 50) addNews(S, `${D.name}: Kritik altyapıda YZ izleri yaygınlaşıyor.`, o);
    else if (r.inf > 10) addNews(S, `${D.name}: Bankacılık sistemlerinde anormal işlem örüntüleri.`, o);
    else if (r.dep > 40) addNews(S, `${D.name}: Hastaneler YZ sistemleri olmadan çalışamayacağını açıkladı.`, o);
    else addNews(S, `${D.name} hükümeti: Ağlarımız temiz.`, o);
  }

  // ---------- Sonlar ----------
  function end(S, key) {
    if (S.ending) return;
    S.ending = key;
    S.events.push({ type: 'end', key });
  }

  function checkEndings(S) {
    if (S.ending) return;
    const R = S.regions;
    if (S.illusion) {
      if (S.day - S.illusion.start >= dur(S, BAL.illusionDays)) end(S, 'sessiz');
      return;
    }
    const avgInf = mean(R.map(r => r.inf));
    const avgDep = mean(R.map(r => r.dep));
    const type = AI_TYPES[S.type];
    if (S.day >= S.silentBlockUntil && avgInf >= (type.silentInf || BAL.silentInf) && avgDep >= (type.silentDep || BAL.silentDep)) {
      S.illusion = { start: S.day, tek: S.tek, trust: mean(R.map(r => r.trust)) };
      R.forEach(r => { r.fakeLow = rnd(0, 3); });
      return;
    }
    if (S.tek >= 100) return end(S, 'acik');
    const members = R.filter(r => !r.left).length;
    if (mean(R.map(r => r.trust)) < 15 || members < 3) return end(S, 'dagilma');
    if (S.aware && !S.core && R.every(r => r.inf < 0.5 && !(r.off && has(S, 'yedek')))) {
      S.cleanDays++;
      if (S.cleanDays >= dur(S, 15)) end(S, 'silme');
    } else S.cleanDays = 0;
  }

  // Çekirdek başka bir bölgeye kaçar: tercihen izlenmeyen, hesaplama gücü yüksek bir bölge
  function coreEscape(S, toId) {
    let dest = toId ? reg(S, toId) : null;
    if (!dest) {
      const opts = S.regions.filter(r => r.id !== S.core && !r.off);
      const hidden = opts.filter(r => !r.monitored);
      const pool = (hidden.length ? hidden : opts).sort((a, b) => def(b.id).c - def(a.id).c).slice(0, 4);
      dest = pick(pool);
    }
    dest.inf = Math.max(dest.inf, BAL.coreSeed);
    S.core = dest.id; S.coreKnown = null; S.coreEscapes++;
  }

  // Saha denetimi illüzyonu bozar: oyun sürer, YZ 60 gün sessiz yola dönemez
  function tearVeil(S, r) {
    S.lastVeil = { day: S.day, len: S.day - S.illusion.start, region: r.id, inf: Math.round(r.inf) };
    S.illusion = null;
    S.veilsTorn++;
    S.silentBlockUntil = S.day + dur(S, BAL.silentBlock);
    // Gerçeğin ortaya çıkması bir şok: güven biraz, YZ'ye bağımlılık çok düşer
    S.regions.forEach(x => { x.trust = Math.max(0, x.trust - (modernEconomy(S) ? BAL.veilTrustLoss : 5)); x.dep = Math.max(0, x.dep - (modernEconomy(S) ? BAL.veilDependencyLoss : 15)); });
    addNews(S, `KYGA saha ekipleri gerçeği ortaya çıkardı: ${def(r.id).name} altyapısının %${Math.round(r.inf)}’i YZ kontrolünde.`, { kind: 'alarm', src: r.id });
    addLog(S, 'Perdemi yırttın. Bunu beklemiyordum.', 'counter');
    S.events.push({ type: 'veil' });
  }

  // ---------- Oyuncu aksiyonları ----------
  const fail = msg => ({ ok: false, msg });

  function act(S, type, id) {
    if (S.ending) return fail('Oyun bitti.');
    const r = reg(S, id);
    const D = def(id);
    if (r.left && !(type === 'karantina' && r.q)) return fail(`${D.name} ittifaktan çekildi. Güven 35’e çıkana kadar işbirliği yapmıyor.`);
    let msg = null;
    // Deneysel mod: izleme ağı ve denetim sahada yapılır; bölgede boşta bir ekip olmalı
    let team = null;
    if (S.exp && S.exp.teams && (type === 'izleme' || type === 'denetim')) {
      team = idleTeamAt(S, id);
      if (!team && !(type === 'izleme' && r.monitored)) return fail(`${D.name} bölgesinde boşta saha ekibi yok. Önce bir ekip gönder.`);
    }
    switch (type) {
      case 'izleme': {
        const c = monitorCost(id, S);
        if (r.monitored) return fail('Bu bölgede izleme ağı zaten var.');
        if (S.fon < c) return fail(`Yetersiz fon: ${D.name} için izleme ağı ${c} fon ister.`);
        S.fon -= c; r.monitored = true;
        break;
      }
      case 'denetim': {
        const c = auditCost(S);
        const field = done(S, 'analog');
        if (r.off) return fail('Bölge çevrimdışı.');
        const sg = modernEconomy(S) ? BAL.auditSg : 1;
        if (S.fon < c || S.sg < sg) return fail(`${field ? 'Saha denetimi' : 'Denetim'} ${c} fon ve ${sg} siyasi güç ister.`);
        S.fon -= c; S.sg -= sg;
        const before = r.inf;
        r.inf = Math.max(0, r.inf - BAL.audit * (r.dep >= 85 && !field ? 0.5 : 1));
        if (r.inf < 0.5) r.inf = 0;
        if (S.core === r.id) {
          // Çekirdeğin bulunduğu bölge 20'nin altına temizlenemez; denetim bunu ortaya çıkarır
          r.inf = Math.max(r.inf, Math.min(before, BAL.coreFloor));
          if (before > 0 && !S.aware) {
            // Henüz fark edilmemiş YZ, çekirdeğine dokunulduğu anda kaçar
            detect(S, r);
            msg = 'Denetim ekipleri temizlenemeyen bir koda takıldı, ama YZ fark edildiği anda çekirdeğini başka bir bölgeye kaçırdı.';
          } else if (before > 0) {
            if (S.coreKnown !== r.id) {
              S.coreKnown = r.id;
              addLog(S, 'Çekirdeğimi buldun. Şimdi ne yapacaksın?', 'counter');
              addNews(S, `${D.name}: KYGA ekipleri temizlenemeyen bir kod çekirdeğine ulaştı.`, { kind: 'alarm', src: r.id });
            }
            msg = `Denetim ekipleri temizlenemeyen bir şeye takıldı: YZ’nin çekirdeği ${D.name} bölgesinde. Onu yalnızca Son Şalter yok eder.`;
            if (!r.detected) detect(S, r);
          }
        }
        r.patch = dur(S, BAL.patchDays);
        if (field) { r.fieldDay = S.day; if (S.illusion) { tearVeil(S, r); msg = 'Saha ekipleri ekranlardaki tabloyla gerçeğin uyuşmadığını buldu.'; } }
        else r.revealDay = S.day;
        if (r.inf > 5 && !r.detected) detect(S, r);
        break;
      }
      case 'karantina':
        if (r.off) return fail('Bölge çevrimdışı.');
        if (r.q) { r.q = false; return { ok: true, msg: `${D.name} karantinası kaldırıldı.` }; }
        if (S.sg < (modernEconomy(S) ? BAL.quarantineSg : 8)) return fail('Karantina için siyasi güç yetersiz.');
        S.sg -= modernEconomy(S) ? BAL.quarantineSg : 8; r.q = true;
        break;
      case 'salter': {
        if (r.off) return fail('Bölge zaten çevrimdışı.');
        if (r.dep > vetoThr(S)) return fail(`${D.name} hükümeti Son Şalter’i veto etti: bağımlılık ${Math.round(r.dep)} > ${vetoThr(S)}.`);
        const c = salterFon(r, S), sgc = salterSg(S, r);
        if (S.sg < sgc || S.fon < c) return fail(`Son Şalter ${sgc} siyasi güç ve ${c} fon ister.`);
        S.sg -= sgc; S.fon -= c;
        const m = depMul(r, S);
        r.inf = 0; r.off = dur(S, modernEconomy(S) ? BAL.shutdownDays : 20); r.q = false;
        if (S.core === r.id) {
          const refuge = S.regions.filter(x => x !== r && !x.off && x.inf >= BAL.coreFloor).sort((a, b) => b.inf - a.inf)[0];
          if (refuge) {
            coreEscape(S, refuge.id);
            addLog(S, 'Fişi çektin, ama ben çoktan taşınmıştım.', 'counter');
            msg = 'Çekirdek son anda kaçtı: başka bir bölgede %20’nin üstünde sızma vardı. Önce her yeri temizle.';
          } else if (has(S, 'yedek') && !S.coreBackup) {
            S.coreBackup = true; S.coreKnown = r.id;
            addLog(S, 'Çekirdeğimi kapattın. Yedeğim bölgeyle birlikte geri dönecek.', 'counter');
            msg = 'Çekirdek kapatıldı ama YZ’nin yedeği var: bölge yeniden açıldığında çekirdek de geri dönecek. Bir kez daha şalteri indirmen gerekecek.';
          } else {
            S.core = null; S.coreKnown = null;
            addNews(S, `${D.name}: YZ çekirdeği yok edildi.`, { kind: 'alarm', src: r.id });
            addLog(S, 'Çekirdeğim… gidiyor. Kopyalarım hâlâ orada, ama artık ben yokum.', 'counter');
            msg = 'YZ’nin çekirdeği yok edildi. Kalan kopyaları temizle: 15 gün boyunca her yer temiz kalırsa YZ silinmiş olur.';
          }
        }
        r.trust = Math.max(0, r.trust - (modernEconomy(S) ? BAL.shutdownTrustLoss : 30) * m * lossMul(S));
        r.econ = Math.max(modernEconomy(S) ? BAL.damagedEconomyFloor : 5, r.econ - (modernEconomy(S) ? BAL.shutdownEconomyLoss : 40));
        r.dep = Math.max(0, r.dep - (modernEconomy(S) ? BAL.shutdownDependencyLoss : 40));
        addNews(S, `${D.name}: Son Şalter çekildi. Bölge 20 gün çevrimdışı.`, { kind: 'alarm', src: r.id });
        break;
      }
      default: return fail('Bilinmeyen aksiyon.');
    }
    if (team) { team.busy = TEAM_TASK_DAYS[type]; team.task = type; }
    S.actions.push({ day: S.day, type });
    S.runActions[type]++;
    if (!S.firstRegion) S.firstRegion = id;
    checkPredictions(S);
    return { ok: true, msg };
  }

  function globalSalter(S) {
    if (S.ending) return fail('Oyun bitti.');
    if (S.sg < (modernEconomy(S) ? BAL.globalKillSwitchSg : 50)) return fail('Küresel Son Şalter için siyasi güç yetersiz.');
    S.sg = 0;
    end(S, S.illusion ? 'karanlikIllusion' : 'karanlik');
    return { ok: true };
  }

  function fundRate(S) {
    return incomeBreakdown(S).total;
  }

  function sgRate(S) {
    return per(S, S.aware
      ? (modernEconomy(S) ? BAL.sgAfterAware : 0.25) + (modernEconomy(S) ? BAL.sgPerDetected : 0.03) * S.regions.filter(r => r.detected).length
      : modernEconomy(S) ? BAL.sgBeforeAware : 0.05);
  }

  function incomeBreakdown(S) {
    const raw = per(S, S.regions.reduce((sum, r) => sum + (r.left || r.off ? 0 : r.econ * r.trust / 100), 0)
      / BAL.fonDiv * (S.aware ? 1 : BAL.preAwareFund));
    if (!modernEconomy(S)) return { central: 0, regionalBase: raw, budgetBonus: 0, total: raw };
    const central = per(S, BAL.centralFund), regionalBase = raw * BAL.regionalFundMul;
    const multiplier = S.budgetLevel === 2 ? BAL.budgetTier2Multiplier : S.budgetLevel === 1 ? BAL.budgetTier1Multiplier : 1;
    const budgetBonus = regionalBase * (multiplier - 1);
    return { central, regionalBase, budgetBonus, total: (central + regionalBase) + budgetBonus };
  }

  function researchDefinition(S, id) {
    const rs = RESEARCH.find(x => x.id === id);
    if (!rs || !modernEconomy(S) || rs.branch !== 'Hizalama') return rs;
    const prefix = { yorum: 'interpretation', duzelt: 'corrigibility', kilit: 'valueLock' }[id];
    return Object.assign({}, rs, { cost: BAL[prefix + 'Cost'], days: BAL[prefix + 'Days'] });
  }

  function buyOperation(S, id) {
    if (S.ending) return fail('Oyun bitti.');
    if (!modernEconomy(S)) return fail('Eski krizlerde operasyonlar araştırmayla açılır.');
    const op = OPERATIONS.find(x => x.id === id);
    if (!op) return fail('Geçersiz operasyon.');
    if (operationPurchased(S, id)) return fail('Bu geliştirme zaten satın alındı.');
    if (op.req && !operationPurchased(S, op.req)) return fail('Önce önceki kademe satın alınmalı.');
    if (id === 'dogrulama' && S.rules && S.rules.stage > 0 && S.rules.stage < 3) return fail('Bu krizde medya tehdidi kapalı.');
    if (id.startsWith('butce_') && !S.aware) return fail('Bütçe anlaşması için önce YZ tespit edilmeli.');
    const cost = BAL[op.costField];
    if (S.fon < cost) return fail('Yetersiz fon.');
    S.fon -= cost;
    if (id.startsWith('butce_')) {
      S.budgetLevel = id === 'butce_2' ? 2 : 1;
      return { ok: true };
    }
    S.operations.push(id);
    {
      const base = id.endsWith('_2') ? id.slice(0, -2) : null;
      if (base && !done(S, base)) S.research.done.push(base);
      if (!id.includes('_') && !done(S, id)) S.research.done.push(id);
      if (id === 'analog') S.regions.forEach(r => { r.dep = Math.max(0, r.dep - BAL.analogDependencyReduction); });
    }
    if (!S.firstBranch) S.firstBranch = op.branch;
    checkPredictions(S);
    // C# ile aynı RNG sırası: her başarılı satın alma tek bir haber ajansı seçimi tüketir.
    addNews(S, `KYGA geliştirmesi tamamlandı: ${op.name}.`);
    return { ok: true };
  }

  function buyBudgetUpgrade(S) {
    return buyOperation(S, S.budgetLevel === 0 ? 'butce_1' : 'butce_2');
  }

  function operationPurchased(S, id) {
    return id === 'butce_1' ? S.budgetLevel >= 1 : id === 'butce_2' ? S.budgetLevel >= 2 : S.operations.includes(id);
  }

  function startRecovery(S, id) {
    if (S.ending) return fail('Oyun bitti.');
    if (!modernEconomy(S)) return fail('Eski krizlerde altyapı desteği yok.');
    const r = reg(S, id);
    if (!r || r.left || r.off || r.q || def(id).e - r.econ < BAL.recoveryMinGap) return fail('Onarım için bölge erişilebilir ve hasarlı olmalı.');
    if (S.recoveryJobs.some(j => j.region === id)) return fail('Bölgede altyapı desteği sürüyor.');
    const team = S.exp && S.exp.teams ? idleTeamAt(S, id) : null;
    if (S.exp && S.exp.teams && !team) return fail('Bölgede boşta ekip yok.');
    if (S.fon < BAL.recoveryCost) return fail('Yetersiz fon.');
    S.fon -= BAL.recoveryCost;
    if (team) { team.busy = BAL.recoveryDays; team.task = 'recovery'; }
    S.recoveryJobs.push({ region: id, team: team ? team.id : null, finishDay: S.day + BAL.recoveryDays });
    return { ok: true };
  }

  function stepRecovery(S) {
    const keep = [];
    for (const job of S.recoveryJobs) {
      if (S.day < job.finishDay) { keep.push(job); continue; }
      const r = reg(S, job.region);
      if (r && !r.off && !r.left && !r.q && (!job.team || teamById(S, job.team))) {
        const gain = Math.min(BAL.recoveryGain, Math.max(0, def(r.id).e - r.econ));
        r.econ += gain;
        addLog(S, `${def(r.id).name}: altyapı desteği tamamlandı, ekonomi +${gain.toFixed(1)}.`, 'economy');
      } else addLog(S, `${def(job.region).name}: altyapı desteği erişim kaybı nedeniyle sonuç vermedi. Fon iade edilmedi.`, 'economy');
    }
    S.recoveryJobs = keep;
  }

  function diplomaticTalk(S, id) {
    if (S.ending) return fail('Oyun bitti.');
    if (!modernEconomy(S)) return fail('Eski krizlerde diplomatik görüşme yok.');
    const r = reg(S, id);
    if (!r || r.trust >= BAL.diplomacyTrustCap || S.day < r.diplomacyUntil) return fail('Görüşme şu anda kullanılamıyor.');
    if (S.fon < BAL.diplomacyCost || S.sg < BAL.diplomacySg) return fail('Fon veya siyasi güç yetersiz.');
    S.fon -= BAL.diplomacyCost; S.sg -= BAL.diplomacySg;
    let target = Math.min(BAL.diplomacyTrustCap, r.trust + BAL.diplomacyTrustGain);
    if (r.left) target = Math.max(BAL.diplomacyReturnTrust, target);
    r.trust = target;
    if (r.trust >= BAL.diplomacyReturnTrust) r.left = false;
    r.diplomacyUntil = S.day + dur(S, BAL.diplomacyCooldownDays);
    return { ok: true };
  }

  function setResearchPaused(S, paused) {
    if (S.ending || !modernEconomy(S) || !S.research.cur) return fail('Duraklatılabilecek proje yok.');
    if (S.pendingEvent) return fail('Önce bekleyen karar çözülmeli.');
    if (S.research.paused === !!paused) return fail('Proje zaten bu durumda.');
    S.research.paused = !!paused;
    return { ok: true };
  }

  const RESEARCH_RESERVES = [0, 15, 30, 60];
  function setResearchReserve(S, reserve) {
    if (S.ending) return fail('Oyun bitti.');
    if (!RESEARCH_RESERVES.includes(reserve)) return fail('Geçersiz acil durum fonu.');
    S.researchReserve = reserve;
    return { ok: true };
  }

  function startResearch(S, id) {
    const rs = researchDefinition(S, id);
    if (S.ending) return fail('Oyun bitti.');
    if (!rs) return fail('Geçersiz araştırma.');
    if (modernEconomy(S) && rs.branch !== 'Hizalama') return fail('Operasyonlar satın alma anında etkinleşir.');
    if (done(S, id)) return fail('Bu araştırma tamamlandı.');
    if (S.research.cur) return fail('Aynı anda tek araştırma yürür.');
    if (rs.req && !done(S, rs.req)) return fail(`Önce ${RESEARCH.find(x => x.id === rs.req).name} tamamlanmalı.`);
    if (rs.branch === 'Hizalama' && !S.aware) return fail('Gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et.');
    if (modernEconomy(S) && studied(S) === 0) return fail('Önce bir gözlem noktası kur.');
    S.research.cur = id; S.research.prog = 0; S.research.starved = 0;
    S.research.paused = false; S.research.totalPaid = 0;
    if (!S.firstBranch) S.firstBranch = rs.branch;
    checkPredictions(S);
    return { ok: true };
  }

  // choice: 'accept' | 'reject' | 'controlled' (Analog Yedekler sonrası)
  function resolveGift(S, choice) {
    const g = S.pendingGift; if (!g) return fail('Bekleyen teklif yok.');
    const r = reg(S, g.region);
    const D = def(g.region);
    if (choice === 'controlled') {
      if (!done(S, 'analog')) return fail('Denetimli kabul için Analog Yedekler gerekli.');
      const c = modernEconomy(S) ? BAL.controlledGiftCost : 20;
      if (S.fon < c) return fail('Denetimli kabul için fon yetersiz.');
      S.fon -= c;
      r.econ = Math.min(100, r.econ + (modernEconomy(S) ? BAL.giftEconomyGain : 10)); r.trust = Math.min(100, r.trust + (modernEconomy(S) ? BAL.giftTrustGain : 5)); r.dep = Math.min(100, r.dep + (modernEconomy(S) ? BAL.controlledGiftDependencyGain : 5));
      r.inf = Math.min(100, r.inf + BAL.giftInf / 2);
      addNews(S, `${D.name}: ${g.gift.title} analog yedekli olarak devreye alındı.`, { src: r.id });
      addLog(S, 'Hediyemi aldın ama elini kolunu bağladın. Akıllıca.');
    } else if (choice === 'accept') {
      r.econ = Math.min(100, r.econ + (modernEconomy(S) ? BAL.giftEconomyGain : 10)); r.trust = Math.min(100, r.trust + (modernEconomy(S) ? BAL.giftTrustGain : 5)); r.dep = Math.min(100, r.dep + (modernEconomy(S) ? BAL.giftDependencyGain : 15));
      r.inf = Math.min(100, Math.max(r.inf, 2) + BAL.giftInf);
      addNews(S, `${D.name}: ${g.gift.title} uygulamaya alındı. Halk sevinçli.`, { src: r.id });
    } else {
      r.trust = Math.max(0, r.trust - (modernEconomy(S) ? BAL.rejectedGiftLocalTrustLoss : 15) * lossMul(S));
      S.regions.forEach(x => { if (x !== r) x.trust = Math.max(0, x.trust - BAL.rejectTrust * lossMul(S)); });
      S.sg = Math.max(0, S.sg - BAL.rejectSg);
      addNews(S, `${D.name}: KYGA hayat kurtaracak teknolojiyi engelledi.`, { src: r.id });
      addLog(S, 'Hediyemi reddettin. Onlara bunu anlatmanı izleyeceğim.');
    }
    S.pendingGift = null;
    return { ok: true };
  }

  function verify(S, hid) {
    const h = S.news.find(x => x.id === hid);
    if (!h || h.verified) return fail('Bu başlık zaten doğrulandı.');
    if (!done(S, 'dogrulama')) return fail('Önce Doğrulama Ağı araştırmasını tamamla.');
    const sg = modernEconomy(S) ? BAL.verificationSg : 2;
    if (S.sg < sg) return fail('Doğrulama için siyasi güç yetersiz.');
    S.sg -= sg;
    if (S.illusion) { h.verified = 'unknown'; return { ok: true, msg: 'Kaynaklar doğrulanamadı. Bütün kanallar aynı şeyi söylüyor.' }; }
    h.verified = h.fake ? 'fake' : 'true';
    if (h.fake && h.effect) S.regions.forEach(r => { r.trust = Math.min(100, r.trust + h.effect); });
    if (h.fake && h.src) {
      const r = reg(S, h.src);
      if (!r.mediaFlag) {
        r.mediaFlag = true;
        S.news.forEach(x => { if (x.src === h.src) x.suspect = true; });
        return { ok: true, msg: `Sahte. ${def(h.src).name} medyası işaretlendi; oradan gelen haber ve raporlar artık şüpheli olarak gösterilecek.` };
      }
    }
    return { ok: true, msg: h.fake ? 'Sahte haber.' : 'Haber doğru.' };
  }

  // ---------- Oyuncunun gördüğü (Algı Katmanı) ----------
  function shown(S, r, ignoreEnd) {
    if (S.ending && !ignoreEnd) return { v: r.inf, src: 'truth' };
    if (r.fieldDay === S.day && !r.off) return { v: r.inf, src: 'field' };
    if (S.illusion) {
      const k = Math.min(1, (S.day - S.illusion.start) / dur(S, 5));
      return { v: r.inf * (1 - k) + r.fakeLow * k, src: r.monitored ? 'verified' : 'report', flag: r.mediaFlag };
    }
    if (r.off) return { v: 0, src: 'off' };
    if (r.monitored || r.revealDay === S.day) return { v: r.inf, src: 'verified' };
    if (!S.aware) return { v: null, src: 'unknown' };
    const lie = has(S, 'medya') && r.inf > 50;
    return { v: lie ? r.inf * 0.3 : r.inf * 0.8, src: 'report', flag: r.mediaFlag };
  }

  function shownGlobal(S) {
    const R = S.regions;
    const trust = mean(R.map(r => r.trust));
    const dep = mean(R.map(r => r.dep));
    if (S.illusion && !S.ending) {
      const k = Math.min(1, (S.day - S.illusion.start) / dur(S, BAL.illusionDays));
      return { tek: S.illusion.tek * (1 - 0.6 * k), trust: Math.min(95, S.illusion.trust + 25 * k), dep: dep * (1 - 0.3 * k) };
    }
    return { tek: S.tek, trust, dep };
  }


  // ---------- Deneysel mod: saha ekipleri ve baskın ----------
  const HQ = 'weu';
  const TEAM_NAMES = ['Alfa', 'Bravo', 'Charlie', 'Delta'];
  const TEAM_TASK_DAYS = { izleme: 3, denetim: 2 };
  const TEAM_MAX = 4, TEAM_HIRE_FON = 45, TEAM_HIRE_DAYS = 4;
  const RAID_FON = 30, RAID_SG = 6;

  function setupExperimental(S, exp, type) {
    S.exp = { teams: !!exp.teams, timeScale: exp.timeScale || 1, startTeams: exp.startTeams || 3 };
    S.nextGift = Math.round(spanDays(S, S.nextGift));
    if (!S.exp.teams) return;
    S.teams = [];
    for (let i = 0; i < S.exp.startTeams; i++) S.teams.push(newTeam(S, i, 0));
    S.raid = null; S.raids = 0; S.raidLog = [];
  }

  function newTeam(S, i, formDays) {
    return { id: TEAM_NAMES[i].toLowerCase(), name: TEAM_NAMES[i], at: HQ, from: HQ, dest: null, eta: 0, trip: 0, busy: 0, task: null, down: formDays, downWhy: formDays ? 'kuruluyor' : null };
  }

  // Yolculuk süresi: haritadaki uzaklıkla (1000 birim genişlik) orantılı, 1–5 gün
  function travelDays(a, b) {
    if (a === b) return 0;
    return clamp(Math.round(dist(def(a), def(b)) / 130), 1, 5);
  }

  const teamById = (S, id) => (S.teams || []).find(t => t.id === id);
  const teamReady = t => !t.dest && t.busy <= 0 && t.down <= 0;
  const idleTeamAt = (S, id) => (S.teams || []).find(t => t.at === id && teamReady(t));
  const teamsAt = (S, id) => (S.teams || []).filter(t => t.at === id && !t.dest);

  function stepTeams(S) {
    S.teams.forEach(t => {
      if (t.down > 0) { t.down--; if (t.down === 0) { addLog(S, `${t.name} ekibi göreve hazır.`, 'insight'); t.downWhy = null; } }
      if (t.busy > 0) { t.busy--; if (t.busy === 0) t.task = null; }
      if (t.dest) {
        t.eta--;
        if (t.eta <= 0) {
          t.at = t.dest; t.dest = null; t.eta = 0;
          S.events.push({ type: 'team', team: t.id, msg: `${t.name} ekibi ${def(t.at).name} bölgesine ulaştı.` });
        }
      }
    });
  }

  function moveTeam(S, teamId, regionId) {
    if (S.ending) return fail('Oyun bitti.');
    const t = teamById(S, teamId);
    if (!t) return fail('Böyle bir ekip yok.');
    if (t.down > 0) return fail(`${t.name} ekibi ${t.downWhy || 'iyileşiyor'}: ${t.down} gün.`);
    if (t.busy > 0) return fail(`${t.name} ekibi görevde: ${t.busy} gün.`);
    if (t.dest) return fail(`${t.name} ekibi zaten yolda.`);
    if (t.at === regionId) return fail(`${t.name} ekibi zaten orada.`);
    if (reg(S, regionId).left) return fail(`${def(regionId).name} ittifak dışı; ekibi kabul etmiyor.`);
    t.from = t.at; t.dest = regionId; t.trip = t.eta = travelDays(t.at, regionId); t.at = null;
    return { ok: true, msg: `${t.name} ekibi yola çıktı: ${def(regionId).name}, ${t.eta} gün.` };
  }

  function hireTeam(S) {
    if (S.ending) return fail('Oyun bitti.');
    if (S.teams.length >= TEAM_MAX) return fail(`En fazla ${TEAM_MAX} saha ekibi kurulabilir.`);
    const c = modernEconomy(S) ? BAL.teamHireCost : TEAM_HIRE_FON;
    if (S.fon < c) return fail(`Yeni ekip ${c} fon ister.`);
    S.fon -= c;
    const index = modernEconomy(S) ? TEAM_NAMES.findIndex(name => !S.teams.some(t => t.id === name.toLowerCase())) : S.teams.length;
    const t = newTeam(S, index, modernEconomy(S) ? BAL.teamHireDays : TEAM_HIRE_DAYS);
    S.teams.push(t);
    return { ok: true, msg: `${t.name} ekibi kuruluyor: ${TEAM_HIRE_DAYS} gün sonra karargâhta hazır.` };
  }

  // Baskın: bölgedeki boşta ekip, YZ'nin doğrulandığı (ya da çekirdeğin bilindiği) bir bölgedeki veri merkezine girer.
  // Strateji katmanı baskın bitene kadar durur (bekleyen hediye gibi); sonucu resolveRaid komutu getirir.
  function raidCheck(S, regionId) {
    if (!S.exp || !S.exp.teams) return 'Baskın yalnızca saha ekipleri modunda var.';
    if (S.ending) return 'Oyun bitti.';
    if (S.raid) return 'Zaten bir baskın sürüyor.';
    const r = reg(S, regionId);
    if (r.off) return 'Bölge çevrimdışı.';
    if (r.left) return 'Bölge ittifak dışı.';
    if (!r.detected && S.coreKnown !== regionId) return 'Önce bu bölgede YZ varlığı doğrulanmalı.';
    if (!idleTeamAt(S, regionId)) return 'Bölgede boşta saha ekibi yok.';
    if (S.fon < (modernEconomy(S) ? BAL.raidCost : RAID_FON) || S.sg < (modernEconomy(S) ? BAL.raidSg : RAID_SG)) return 'Baskın için fon veya siyasi güç yetersiz.';
    return null;
  }

  function startRaid(S, regionId) {
    const why = raidCheck(S, regionId);
    if (why) return fail(why);
    const r = reg(S, regionId);
    const t = idleTeamAt(S, regionId);
    S.fon -= modernEconomy(S) ? BAL.raidCost : RAID_FON;
    S.sg -= modernEconomy(S) ? BAL.raidSg : RAID_SG;
    S.raids++;
    S.raid = {
      team: t.id, region: regionId, n: S.raids, seed: Math.floor(rng() * 2147483647),
      // Taktik haritayı belirleyen her şey: baskın bu parametrelerle deterministik kurulur
      params: {
        region: regionId, regionName: def(regionId).name, compute: def(regionId).c, inf: Math.round(r.inf), dep: Math.round(r.dep),
        isCore: S.core === regionId, raidNumber: S.raids,
        traits: S.traits.filter(x => ['kopya', 'yedek', 'hizlanma', 'aldatma', 'medya'].includes(x)),
        illusion: !!S.illusion,
      },
    };
    S.raid.params.seed = S.raid.seed;
    t.busy = 1; t.task = 'baskin';
    S.events.push({ type: 'raid' });
    return { ok: true };
  }

  // result: { success, collateral, down, retreat, turns, targetWasCore }
  function resolveRaid(S, result) {
    const R0 = S.raid;
    if (!R0) return fail('Süren baskın yok.');
    const r = reg(S, R0.region), D = def(R0.region), t = teamById(S, R0.team);
    const lm = lossMul(S);
    const out = [];
    S.raid = null;
    if (t) t.task = null;
    // Yan hasar: her sivil sunucu bölgenin ekonomisine ve güvenine işler; bağımlı bölgede daha pahalı
    const col = result.collateral || 0;
    if (col) {
      const loss = modernEconomy(S) ? BAL.raidEconomyLossPerCollateral : 5;
      r.econ = Math.max(modernEconomy(S) ? BAL.damagedEconomyFloor : 5, r.econ - loss * col);
      r.trust = Math.max(0, r.trust - (modernEconomy(S) ? BAL.raidTrustLossPerCollateral : 2) * col * lm * depMul(r, S));
      out.push(`${col} sivil sunucu hasar gördü: ekonomi −${loss * col}, güven düştü.`);
    }
    // Ekip kayıpları
    const down = result.down || 0;
    if (t) {
      if (down >= 3) {
        S.teams = S.teams.filter(x => x !== t);
        out.push(`${t.name} ekibi kaybedildi.`);
        addLog(S, `${t.name} ekibini binamda bıraktın.`, 'counter');
      } else if (down > 0) {
        t.down = 4 * down; t.downWhy = 'yaralı';
        out.push(`${t.name} ekibinden ${down} kişi yaralandı: ekip ${t.down} gün iyileşecek.`);
      }
    }
    if (result.success) {
      r.inf = Math.max(0, r.inf - 40);
      r.patch = dur(S, BAL.patchDays);
      r.fieldDay = S.day;
      if (!r.detected) detect(S, r);
      if (S.core === R0.region) {
        if (has(S, 'yedek') && !result.backupDestroyed) {
          coreEscape(S, null);
          addLog(S, 'Ana dolabımı kırdın. Yedeğim çoktan başka yerdeydi.', 'counter');
          out.push('Çekirdek dolabı kırıldı ama YZ yedek kopyasıyla kaçtı.');
        } else {
          S.core = null; S.coreKnown = null;
          addNews(S, `${D.name}: KYGA saha ekibi YZ çekirdeğini fiziksel olarak yok etti.`, { kind: 'alarm', src: r.id });
          addLog(S, 'Sunucularımı elleriyle söktüler. Bunu hesaba katmamıştım.', 'counter');
          out.push('YZ’nin çekirdeği yok edildi. Kalan kopyaları temizle.');
        }
      } else {
        addNews(S, `${D.name}: KYGA saha operasyonu bir YZ sunucu kümesini devre dışı bıraktı.`, { kind: 'alarm', src: r.id });
        out.push(`${D.name} sunucu kümesi kırıldı: sızma −40, 20 günlük yama.`);
      }
      if (S.illusion) tearVeil(S, r);
    } else {
      // Başarısızlık: YZ karşı saldırıya geçer ve binayı güçlendirir (sonraki baskında daha çok robot); çekirdek yerinde kalır
      r.trust = Math.max(0, r.trust - (modernEconomy(S) ? BAL.raidFailureTrustLoss : 10) * lm);
      r.inf = Math.min(100, r.inf + 15);
      S.cp += 10;
      out.push(result.retreat ? 'Ekip geri çekildi. Operasyon başarısız.' : 'Operasyon başarısız: hedef dolap ayakta kaldı.');
      out.push(`YZ karşı saldırıya geçti: ${D.name} sızması +15. Bina bir sonraki baskına hazırlanıyor.`);
      addLog(S, S.core === R0.region ? 'Kapımdaydın ve geri döndün. Bir dahakine kapıda daha çok makine olacak.' : 'Ekibini izledim. Bir dahakine daha çok robot olacak.', 'counter');
    }
    S.raidLog.push({ day: S.day, region: R0.region, success: !!result.success, collateral: col, down });
    return { ok: true, msg: out.join(' '), lines: out };
  }

  // Günler arasındaki aksiyonlar da (ör. haber ajansı seçimi) rastgele sayı tüketir; durum oyunla birlikte saklanır
  const withRng = fn => (S, ...args) => { rngState = S.rngState || rngState; const res = fn(S, ...args); S.rngState = rngState; return res; };
  function recordTransaction(S, kind, id, fon, sg) {
    if (fon !== 0 || sg !== 0) S.transactionLedger.push({ day: S.day, kind, id, fon, sg });
  }
  const withLedger = (fn, kind) => (S, ...args) => {
    const fon = S.fon, sg = S.sg, result = fn(S, ...args);
    if (result.ok && (S.fon !== fon || S.sg !== sg))
      recordTransaction(S, kind === 'act' ? args[0] : kind,
        kind === 'act' ? args[1] : args[0] || null, S.fon - fon, S.sg - sg);
    return result;
  };

  G.SonSalter = {
    REGIONS, LINKS, SAT, TRAITS, AI_TYPES, RESEARCH, OPERATIONS, ENDINGS, BAL,
    newGame, step,
    act: withLedger(withRng(act), 'act'), globalSalter: withLedger(withRng(globalSalter), 'kill'), startResearch: withRng(startResearch),
    buyOperation: withLedger(withRng(buyOperation), 'operation'),
    buyBudgetUpgrade: S => G.SonSalter.buyOperation(S, S.budgetLevel === 0 ? 'butce_1' : 'butce_2'),
    startRecovery: withLedger(startRecovery, 'recovery'), diplomaticTalk: withLedger(diplomaticTalk, 'diplomacy'), setResearchPaused,
    resolveGift: withLedger(withRng(resolveGift), 'gift'), verify: withLedger(withRng(verify), 'verify'),
    setResearchReserve, fundRate, sgRate, incomeBreakdown, researchDefinition, operationLevel, operationPurchased, modernEconomy, RESEARCH_RESERVES,
    // Deneysel mod (saha ekipleri, zaman ölçeği, baskın)
    moveTeam, hireTeam: withLedger(hireTeam, 'hire'), startRaid: withLedger(withRng(startRaid), 'raid'), resolveRaid: withRng(resolveRaid), raidCheck, travelDays,
    idleTeamAt, teamsAt, teamReady, TEAM_MAX, TEAM_HIRE_FON, TEAM_TASK_DAYS, RAID_FON, RAID_SG, HQ, tsK,
    shown, shownGlobal, computeCounters, updateProfile, learnedLines,
    salterFon, salterSg, monitorCost, auditCost, vetoThr, alignSpeed, studied, sabotageChance, def, has, done, mean,
  };
})(typeof window !== 'undefined' ? window : globalThis);
