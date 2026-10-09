// Son Şalter — Baskın: hücre tabanlı taktik savaş (Into the Breach esinli). DOM yok.
// Durum düz JSON'dur: geri alma ve önizleme için kopyalanır. Rastgelelik yalnızca durumdaki mulberry32 (B.rs) ile;
// aynı parametreler + aynı hamleler = aynı savaş.
//
// Tur yapısı: oyuncu turu (düşman niyetleri önceden görünür) → düşman evresi: niyetler sırayla uygulanır,
// işaretli karelerden yeni robotlar uyanır, düşmanlar yer değiştirip yeni niyet seçer, yeni uyanma kareleri işaretlenir.
(function (G) {
  'use strict';

  const N = 8;
  const DIRS = [[0, -1], [1, 0], [0, 1], [-1, 0]];
  const SCAN_RADIUS = 3;
  const MAX_ENEMIES = 7;

  // '.' zemin, '#' duvar, '~' elektrikli kablo kanalı (üstüne itilen 1 hasar alır),
  // 'C' hedef dolap (çekirdek ya da YZ düğümü), 'Y' yedek çekirdek (Yedekleme yoksa sivil sunucu),
  // 'S' sivil sunucu (kırılırsa yan hasar), 'K' soğutma ünitesi (kırılınca çevresine 1 hasar), 'U' kopya dolabı (robot uyandırır),
  // 'E' ekip giriş noktası, 'd' dron, 'm' bakım robotu
  const LAYOUTS = [
    { name: 'Ana sunucu salonu', rows: [
      'S.~...U.',
      '..~.#..S',
      'E.#.K#..',
      'E.#..#C.',
      'E..~~...',
      '..#..#dm',
      '.S#.d.Y.',
      '....K..U',
    ] },
    { name: 'Soğutma koridoru', rows: [
      'E..S#..U',
      'E...#.d.',
      'E.K.~..S',
      '..#.~##.',
      '..#.~.C.',
      '.S..#.mK',
      '....#..d',
      'S.~~.Y.U',
    ] },
    { name: 'Yedekli veri kasası', rows: [
      '.S..U..S',
      '..#.d...',
      'E.#K..#.',
      'E.~.##C.',
      'E.#....m',
      '..#.Y#.d',
      '..S.K.#.',
      '.~..U..S',
    ] },
  ];

  const UNIT_TYPES = {
    tek: { side: 'p', name: 'Teknisyen', short: 'T', hp: 3, move: 3, ability: 'sabotaj' },
    emp: { side: 'p', name: 'EMP uzmanı', short: 'E', hp: 3, move: 3, ability: 'emp' },
    ana: { side: 'p', name: 'Analist', short: 'A', hp: 2, move: 4, ability: 'kanca' },
    dron: { side: 'e', name: 'Güvenlik dronu', short: 'D', hp: 2, move: 3 },
    bakim: { side: 'e', name: 'Bakım robotu', short: 'B', hp: 2, move: 2 },
  };
  const ABILITIES = {
    sabotaj: { name: 'Sabotaj', desc: 'Bitişik kare. Dolaba 2 hasar; robota 1 hasar ve 1 kare itme.' },
    emp: { name: 'EMP atışı', desc: 'Düz hatta 2–5 kare uzağa, engellerin üstünden. Hedefteki robota ya da sıradan dolaba 1 hasar (zırhlı hedef dolaplara işlemez); çevresindeki 4 karedeki robotları dışarı iter.' },
    kanca: { name: 'Kanca', desc: 'Düz hatta 5 kare. Çarptığı robotu 1 kare kendine çeker; duvara ya da dolaba çarparsa Analist onun yanına atılır. Analistin 3 kare çevresindeki robotların niyeti kesin görünür.' },
  };
  const OBJ_NAMES = { core: 'Çekirdek dolabı', node: 'YZ düğüm dolabı', backup: 'Yedek çekirdek', civ: 'Sivil sunucu', cool: 'Soğutma ünitesi', spawner: 'Kopya dolabı' };
  const TARGET_KINDS = ['core', 'node', 'backup'];

  // ---------- Yardımcılar ----------
  function rnd(B) {
    B.rs = (B.rs + 0x6D2B79F5) | 0;
    let t = Math.imul(B.rs ^ (B.rs >>> 15), 1 | B.rs);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }
  const inb = (x, y) => x >= 0 && y >= 0 && x < N && y < N;
  const cell = (B, x, y) => B.cells[y][x];
  const unitAt = (B, x, y) => B.units.find(u => !u.dead && u.x === x && u.y === y) || null;
  const man = (a, b) => Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
  const clone = B => JSON.parse(JSON.stringify(B));
  const alive = (B, side) => B.units.filter(u => !u.dead && u.side === side);
  const fx = (B, e) => { B.fx.push(e); };
  const say = (B, text, kind) => { B.log.push({ turn: B.turn, text, kind: kind || 'sys' }); };

  // ---------- Kurulum ----------
  // params: { seed, compute, inf, dep, isCore, raidNumber, traits[], regionName }
  function create(params) {
    const p = Object.assign({ seed: 1, compute: 3, inf: 30, dep: 0, isCore: false, raidNumber: 1, traits: [], regionName: 'Bölge' }, params || {});
    const B = {
      v: 1, params: p, rs: p.seed | 0, turn: 1, phase: 'player',
      maxTurns: p.traits.includes('hizlanma') ? 5 : 6,
      deceptive: p.traits.includes('aldatma') || p.traits.includes('medya'),
      spawnPerTurn: p.traits.includes('kopya') ? 2 : 1,
      isCore: !!p.isCore, hasBackup: !!p.isCore && p.traits.includes('yedek'),
      cells: [], units: [], spawns: [], log: [], fx: [], hist: [], collateral: 0, nextId: 1, seq: 0, outcome: null,
    };
    rnd(B);
    const L = LAYOUTS[(p.compute + p.raidNumber + (p.seed & 7)) % LAYOUTS.length];
    B.layout = L.name;
    const entries = [];
    for (let y = 0; y < N; y++) {
      const row = [];
      for (let x = 0; x < N; x++) {
        const ch = L.rows[y][x];
        const c = { t: ch === '#' ? 'wall' : ch === '~' ? 'cable' : 'floor', o: null };
        if (ch === 'C') c.o = obj(B.isCore ? 'core' : 'node', B.hasBackup ? 4 : B.isCore ? 5 : 4);
        else if (ch === 'Y') c.o = B.hasBackup ? obj('backup', 2) : obj('civ', 1);
        else if (ch === 'S') c.o = obj('civ', 1);
        else if (ch === 'K') c.o = obj('cool', 1);
        else if (ch === 'U') c.o = obj('spawner', 2);
        row.push(c);
        if (ch === 'E') entries.push({ x, y });
        if (ch === 'd') B.units.push(newUnit(B, 'dron', x, y));
        if (ch === 'm') B.units.push(newUnit(B, 'bakim', x, y));
      }
      B.cells.push(row);
    }
    ['tek', 'emp', 'ana'].forEach((t, i) => B.units.push(newUnit(B, t, entries[i].x, entries[i].y)));
    // Bölge sızması yüksekse ve YZ seni daha önce gördüyse bina daha kalabalık
    const extra = (p.inf >= 50 ? 1 : 0) + Math.min(2, p.raidNumber - 1);
    for (let i = 0; i < extra; i++) {
      const spot = freeTiles(B).filter(t => t.x >= 4).sort((a, b) => b.x - a.x);
      if (spot.length) { const t = spot[Math.floor(rnd(B) * Math.min(6, spot.length))]; B.units.push(newUnit(B, rnd(B) < 0.6 ? 'dron' : 'bakim', t.x, t.y)); }
    }
    const tgt = B.isCore ? 'çekirdeğimin' : 'sunucularımın';
    say(B, p.raidNumber > 1 ? 'Yine sen. Bu sefer kapıda daha çok makine var.' : `Demek ${tgt} yerini buldun. Bakalım içeri girebilecek misin.`, 'ai');
    if (B.deceptive) say(B, 'Makinelerimin ne yapacağını gösterdiğimi mi sanıyorsun?', 'ai');
    if (B.hasBackup) say(B, 'Tek dolap mı? Hayır. Yedeğim de burada.', 'ai');
    planEnemies(B);
    placeSpawns(B);
    B.fx = [];
    return B;
  }

  function obj(k, hp) { return { k, hp, max: hp }; }
  function newUnit(B, type, x, y) {
    const T = UNIT_TYPES[type];
    return { id: B.nextId++, type, side: T.side, x, y, hp: T.hp, max: T.hp, moved: false, acted: false, dead: false, intent: null, order: 0 };
  }
  function freeTiles(B) {
    const out = [];
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) if (cell(B, x, y).t !== 'wall' && !cell(B, x, y).o && !unitAt(B, x, y)) out.push({ x, y });
    return out;
  }

  // ---------- Hasar, itme ----------
  function hurt(B, u, n, why) {
    if (u.dead || n <= 0) return;
    u.hp -= n;
    fx(B, { k: 'dmg', x: u.x, y: u.y, n, unit: u.id });
    if (u.hp <= 0) {
      u.dead = true; u.hp = 0;
      fx(B, { k: 'die', x: u.x, y: u.y, unit: u.id });
      if (u.side === 'p') say(B, `${UNIT_TYPES[u.type].name} yere düştü${why ? ' (' + why + ')' : ''}.`, 'bad');
      else say(B, `${UNIT_TYPES[u.type].name} devre dışı${why ? ' (' + why + ')' : ''}.`, 'good');
    }
  }

  function hurtObj(B, x, y, n) {
    const c = cell(B, x, y);
    if (!c.o || n <= 0) return;
    const o = c.o;
    o.hp -= n;
    fx(B, { k: 'dmg', x, y, n, obj: o.k });
    if (o.hp > 0) return;
    c.o = null;
    fx(B, { k: 'break', x, y, obj: o.k });
    if (o.k === 'civ') { B.collateral++; say(B, 'Sivil sunucu kırıldı: hastane ve banka hizmetleri kesildi.', 'bad'); }
    else if (o.k === 'spawner') say(B, 'Kopya dolabı kırıldı: oradan robot uyanmayacak.', 'good');
    else if (o.k === 'cool') { say(B, 'Soğutma ünitesi patladı.', 'sys'); DIRS.forEach(([dx, dy]) => damageAt(B, x + dx, y + dy, 1, 'patlama')); }
    else if (TARGET_KINDS.includes(o.k)) { say(B, `${OBJ_NAMES[o.k]} kırıldı.`, 'good'); checkWin(B); }
  }

  function damageAt(B, x, y, n, why) {
    if (!inb(x, y)) return;
    const u = unitAt(B, x, y);
    if (u) hurt(B, u, n, why);
    else if (cell(B, x, y).o) hurtObj(B, x, y, n);
  }

  function push(B, u, d) {
    if (u.dead) return;
    const nx = u.x + d[0], ny = u.y + d[1];
    fx(B, { k: 'push', x: u.x, y: u.y, dx: d[0], dy: d[1], unit: u.id });
    if (!inb(nx, ny) || cell(B, nx, ny).t === 'wall') { hurt(B, u, 1, 'duvara çarptı'); return; }
    if (cell(B, nx, ny).o) { hurt(B, u, 1, 'dolaba çarptı'); hurtObj(B, nx, ny, 1); return; }
    const o = unitAt(B, nx, ny);
    if (o) { hurt(B, u, 1, 'çarpışma'); hurt(B, o, 1, 'çarpışma'); return; }
    u.x = nx; u.y = ny;
    if (cell(B, nx, ny).t === 'cable') hurt(B, u, 1, 'elektrik');
  }

  function checkWin(B) {
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) { const o = cell(B, x, y).o; if (o && TARGET_KINDS.includes(o.k)) return; }
    B.phase = 'won';
    B.outcome = { success: true, reason: B.isCore ? 'Çekirdek yok edildi.' : 'YZ düğümü kırıldı.' };
  }

  function targetsLeft(B) {
    const out = [];
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) { const o = cell(B, x, y).o; if (o && TARGET_KINDS.includes(o.k)) out.push({ x, y, o }); }
    return out;
  }

  // ---------- Hareket ----------
  // Dost birimlerin içinden geçilebilir ama üstünde durulamaz; düşman, dolap ve duvar geçilmez.
  function reachable(B, u) {
    const T = UNIT_TYPES[u.type];
    const seen = { [u.x + ',' + u.y]: 0 };
    const q = [[u.x, u.y]];
    const out = [];
    while (q.length) {
      const [x, y] = q.shift();
      const d = seen[x + ',' + y];
      if (d >= T.move) continue;
      for (const [dx, dy] of DIRS) {
        const nx = x + dx, ny = y + dy, key = nx + ',' + ny;
        if (!inb(nx, ny) || key in seen) continue;
        const c = cell(B, nx, ny);
        if (c.t === 'wall' || c.o) continue;
        const o = unitAt(B, nx, ny);
        if (o && o.side !== u.side) continue;
        seen[key] = d + 1;
        q.push([nx, ny]);
        if (!o) out.push({ x: nx, y: ny });
      }
    }
    return out;
  }

  // ---------- Oyuncu yetenekleri ----------
  // Hedef kareler: tıklanabilir kareler ve her biri için yön bilgisi
  function targets(B, u) {
    const out = [];
    const T = UNIT_TYPES[u.type];
    if (T.ability === 'sabotaj') {
      DIRS.forEach(([dx, dy]) => {
        const x = u.x + dx, y = u.y + dy;
        if (inb(x, y) && (unitAt(B, x, y) || cell(B, x, y).o)) out.push({ x, y });
      });
    } else if (T.ability === 'emp') {
      DIRS.forEach(([dx, dy]) => { for (let k = 2; k <= 5; k++) { const x = u.x + dx * k, y = u.y + dy * k; if (inb(x, y)) out.push({ x, y }); } });
    } else if (T.ability === 'kanca') {
      DIRS.forEach(d => { const h = hookTrace(B, u, d); if (h) out.push({ x: h.x, y: h.y }); });
    }
    return out;
  }

  function hookTrace(B, u, d) {
    for (let k = 1; k <= 5; k++) {
      const x = u.x + d[0] * k, y = u.y + d[1] * k;
      if (!inb(x, y)) return k > 1 ? { x: x - d[0], y: y - d[1], k: k - 1, edge: true } : null;
      if (cell(B, x, y).t === 'wall' || cell(B, x, y).o || unitAt(B, x, y)) return { x, y, k };
    }
    return null;
  }

  function applyAbility(B, u, x, y) {
    const T = UNIT_TYPES[u.type];
    const d = [Math.sign(x - u.x), Math.sign(y - u.y)];
    if (T.ability === 'sabotaj') {
      const t = unitAt(B, x, y);
      if (t) { hurt(B, t, 1, 'sabotaj'); push(B, t, d); }
      else if (cell(B, x, y).o) hurtObj(B, x, y, 2);
    } else if (T.ability === 'emp') {
      fx(B, { k: 'emp', x, y });
      const o = cell(B, x, y).o;
      if (o && TARGET_KINDS.includes(o.k) && !unitAt(B, x, y)) fx(B, { k: 'armor', x, y });
      else damageAt(B, x, y, 1, 'EMP');
      DIRS.forEach(dd => { const t = unitAt(B, x + dd[0], y + dd[1]); if (t) push(B, t, dd); });
    } else if (T.ability === 'kanca') {
      const h = hookTrace(B, u, d);
      if (!h) return;
      fx(B, { k: 'hook', x: h.x, y: h.y, from: { x: u.x, y: u.y } });
      const t = !h.edge && unitAt(B, h.x, h.y);
      if (t) {
        if (h.k > 1) { const back = [-d[0], -d[1]]; t.x += back[0]; t.y += back[1]; fx(B, { k: 'pull', unit: t.id }); if (cell(B, t.x, t.y).t === 'cable') hurt(B, t, 1, 'elektrik'); }
      } else if (h.k > 1 || h.edge) {
        const tx = h.edge ? h.x : h.x - d[0], ty = h.edge ? h.y : h.y - d[1];
        if (!(tx === u.x && ty === u.y)) { u.x = tx; u.y = ty; if (cell(B, tx, ty).t === 'cable') hurt(B, u, 1, 'elektrik'); }
      }
    }
  }

  function snapshot(B) { const c = Object.assign({}, B, { hist: [] }); B.hist.push(JSON.stringify(c)); }

  function canMove(B, u) { return B.phase === 'player' && !u.dead && u.side === 'p' && !u.moved && !u.acted; }
  function canAct(B, u) { return B.phase === 'player' && !u.dead && u.side === 'p' && !u.acted; }

  function doMove(B, uid, x, y) {
    const u = B.units.find(v => v.id === uid);
    if (!u || !canMove(B, u)) return false;
    if (!reachable(B, u).some(t => t.x === x && t.y === y)) return false;
    snapshot(B);
    u.x = x; u.y = y; u.moved = true;
    return true;
  }

  function doAct(B, uid, x, y) {
    const u = B.units.find(v => v.id === uid);
    if (!u || !canAct(B, u)) return false;
    if (!targets(B, u).some(t => t.x === x && t.y === y)) return false;
    snapshot(B);
    B.fx = [];
    applyAbility(B, u, x, y);
    u.acted = true; u.moved = true;
    if (alive(B, 'p').length === 0 && B.phase === 'player') lose(B, 'Ekibin tamamı yere düştü.');
    return true;
  }

  function undo(B) {
    if (!B.hist.length || B.phase !== 'player') return false;
    const prev = JSON.parse(B.hist.pop());
    const hist = B.hist;
    Object.keys(B).forEach(k => { delete B[k]; });
    Object.assign(B, prev, { hist });
    return true;
  }

  // Bir yeteneğin sonucu: durumun kopyasında uygulanır, değişen her şey listelenir
  function preview(B, uid, x, y) {
    const C = clone(B);
    C.hist = []; C.fx = []; C.log = [];
    const u = C.units.find(v => v.id === uid);
    applyAbility(C, u, x, y);
    const moves = [];
    C.units.forEach(cu => {
      const ou = B.units.find(v => v.id === cu.id);
      if (ou && (ou.x !== cu.x || ou.y !== cu.y)) moves.push({ unit: cu.id, from: { x: ou.x, y: ou.y }, to: { x: cu.x, y: cu.y } });
    });
    return { fx: C.fx, moves, collateral: C.collateral - B.collateral, won: C.phase === 'won' };
  }

  // ---------- Düşman ----------
  // Niyetin vuracağı kare: ateş düz hatta ilk engele, yakın dövüş bitişik kareye
  function intentTarget(B, e, intent, pos) {
    const px = pos ? pos.x : e.x, py = pos ? pos.y : e.y;
    const [dx, dy] = DIRS[intent.dir];
    if (intent.kind === 'shoot') {
      for (let k = 1; k < N; k++) {
        const x = px + dx * k, y = py + dy * k;
        if (!inb(x, y)) return { x: x - dx, y: y - dy, k: k - 1, none: true, path: k - 1 };
        if (cell(B, x, y).t === 'wall' || cell(B, x, y).o) return { x, y, k, path: k };
        const u = unitAt(B, x, y);
        if (u && u.id !== e.id) return { x, y, k, path: k };
      }
      return null;
    }
    const x = px + dx, y = py + dy;
    return inb(x, y) ? { x, y, k: 1, path: 1 } : null;
  }

  function scoreTarget(B, e, intent, pos) {
    const t = intentTarget(B, e, intent, pos);
    if (!t || t.none) return 0;
    if (cell(B, t.x, t.y).t === 'wall') return 0;
    const u = unitAt(B, t.x, t.y);
    // Onarım yalnızca hasarlı hedef dolaba anlamlı; birime yöneltilen onarım hiçbir şey yapmaz
    if (intent.kind === 'repair') { const ro = !u || u.id === e.id ? cell(B, t.x, t.y).o : null; return ro && TARGET_KINDS.includes(ro.k) && ro.hp < ro.max ? 14 : -1; }
    if (u && u.id !== e.id) return u.side === 'p' ? 10 + (u.type === 'tek' ? 3 : 0) + (u.hp <= (intent.kind === 'melee' ? 2 : 1) ? 4 : 0) : -6;
    const o = cell(B, t.x, t.y).o;
    if (!o) return 0;
    if (TARGET_KINDS.includes(o.k)) return -20;
    if (o.k === 'spawner') return -10;
    if (o.k === 'civ') return 1;
    if (o.k === 'cool') return 2 + 4 * DIRS.filter(([dx, dy]) => { const v = unitAt(B, t.x + dx, t.y + dy); return v && v.side === 'p'; }).length;
    return 0;
  }

  function planEnemies(B) {
    let order = 0;
    alive(B, 'e').forEach(e => {
      const kinds = e.type === 'dron' ? ['shoot'] : ['melee', 'repair'];
      const spots = [{ x: e.x, y: e.y }].concat(reachable(B, e));
      const players = alive(B, 'p');
      let best = [], bestScore = -Infinity;
      spots.forEach(pos => {
        // Konum tercihi: dronlar mesafede durmayı, bakım robotları yaklaşmayı sever
        const near = players.length ? Math.min(...players.map(p => man(p, pos))) : 0;
        const posScore = e.type === 'dron' ? (near <= 1 ? -2 : 0) : -0.2 * near;
        kinds.forEach(kind => DIRS.forEach((_, dir) => {
          const intent = { kind, dir };
          const sc = scoreTarget(B, e, intent, pos) + posScore;
          if (sc > bestScore + 1e-9) { bestScore = sc; best = [{ pos, intent }]; }
          else if (Math.abs(sc - bestScore) < 1e-9) best.push({ pos, intent });
        }));
      });
      const pickd = best[Math.floor(rnd(B) * best.length)];
      e.x = pickd.pos.x; e.y = pickd.pos.y;
      const it = { kind: pickd.intent.kind, dir: pickd.intent.dir, shown: pickd.intent.dir, fake: false };
      // Aldatma: bazı niyetler yalan gösterilir; Analistin tarama alanındakiler her zaman doğru görünür
      if (B.deceptive && it.kind !== 'repair' && rnd(B) < 0.35) { it.fake = true; it.shown = (it.dir + 1 + Math.floor(rnd(B) * 3)) % 4; }
      e.intent = it;
      e.order = ++order;
    });
  }

  function placeSpawns(B) {
    B.spawns = [];
    if (B.phase !== 'player') return;
    const room = MAX_ENEMIES - alive(B, 'e').length;
    const cand = [];
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      const o = cell(B, x, y).o;
      if (!o || o.k !== 'spawner') continue;
      DIRS.forEach(([dx, dy]) => {
        const nx = x + dx, ny = y + dy;
        if (inb(nx, ny) && cell(B, nx, ny).t !== 'wall' && !cell(B, nx, ny).o && !unitAt(B, nx, ny) && !cand.some(c => c.x === nx && c.y === ny)) cand.push({ x: nx, y: ny });
      });
    }
    for (let i = 0; i < Math.min(B.spawnPerTurn, room) && cand.length; i++) B.spawns.push(cand.splice(Math.floor(rnd(B) * cand.length), 1)[0]);
  }

  // Analistin tarama alanındaki düşmanın niyeti kesin görünür
  function scanned(B, e) { return alive(B, 'p').some(p => p.type === 'ana' && man(p, e) <= SCAN_RADIUS); }

  // Arayüz için: her düşmanın gösterilen niyeti
  function intents(B) {
    return alive(B, 'e').filter(e => e.intent).sort((a, b) => a.order - b.order).map(e => {
      const sc = scanned(B, e);
      const uncertain = B.deceptive && !sc && e.intent.kind !== 'repair';
      const dir = uncertain ? e.intent.shown : e.intent.dir;
      const shownIntent = { kind: e.intent.kind, dir };
      const t = intentTarget(B, e, shownIntent);
      return { unit: e.id, order: e.order, kind: e.intent.kind, dir, uncertain, scanned: sc && B.deceptive, target: t, from: { x: e.x, y: e.y } };
    });
  }

  function lose(B, reason) { B.phase = 'lost'; B.outcome = { success: false, reason }; }

  function endTurn(B) {
    if (B.phase !== 'player') return false;
    B.fx = []; B.hist = [];
    say(B, `— Tur ${B.turn}: makineler hareket ediyor —`, 'turn');
    // 1) Niyetler sırayla (itilen robot niyetini yeni yerinden uygular)
    alive(B, 'e').sort((a, b) => a.order - b.order).forEach(e => {
      if (e.dead || !e.intent || B.phase !== 'player') return;
      const t = intentTarget(B, e, e.intent);
      fx(B, { k: 'attack', unit: e.id, kind: e.intent.kind, from: { x: e.x, y: e.y }, to: t && { x: t.x, y: t.y } });
      if (!t || t.none) return;
      if (e.intent.kind === 'repair') {
        const o = cell(B, t.x, t.y).o;
        if (o && TARGET_KINDS.includes(o.k) && o.hp < o.max) { o.hp++; fx(B, { k: 'heal', x: t.x, y: t.y }); say(B, `Bakım robotu ${OBJ_NAMES[o.k].toLowerCase()} onardı.`, 'bad'); }
      } else damageAt(B, t.x, t.y, e.type === 'dron' ? 1 : 2, e.type === 'dron' ? 'dron ateşi' : 'bakım robotu');
    });
    if (B.phase !== 'player') return true;
    // 2) Uyanma: işaretli karede biri duruyorsa uyanma engellenir ama duran hasar alır
    B.spawns.forEach(s => {
      const u = unitAt(B, s.x, s.y);
      if (u) { hurt(B, u, 1, 'uyanmayı engelledi'); say(B, 'Uyanan kopya engellendi.', u.side === 'p' ? 'good' : 'sys'); return; }
      if (cell(B, s.x, s.y).o) return;
      const t = rnd(B) < 0.6 ? 'dron' : 'bakim';
      B.units.push(newUnit(B, t, s.x, s.y));
      fx(B, { k: 'spawn', x: s.x, y: s.y });
      say(B, `Yeni bir ${UNIT_TYPES[t].name.toLowerCase()} uyandı.`, 'bad');
    });
    B.spawns = [];
    if (alive(B, 'p').length === 0) { lose(B, 'Ekibin tamamı yere düştü.'); return true; }
    B.turn++;
    if (B.turn > B.maxTurns) {
      lose(B, B.isCore ? 'Süre doldu: YZ çekirdeğini binadan taşıdı.' : 'Süre doldu: YZ sunucularını yedekledi ve binayı kilitledi.');
      return true;
    }
    // 3) Yeni plan
    B.units.forEach(u => { if (u.side === 'p') { u.moved = false; u.acted = false; } });
    planEnemies(B);
    placeSpawns(B);
    if (B.turn === B.maxTurns) say(B, 'Son tur.', 'turn');
    return true;
  }

  function retreat(B) {
    if (B.phase !== 'player') return false;
    lose(B, 'Ekip geri çekildi.');
    B.outcome.retreat = true;
    return true;
  }

  function result(B) {
    const left = targetsLeft(B);
    return {
      success: B.phase === 'won',
      retreat: !!(B.outcome && B.outcome.retreat),
      collateral: B.collateral,
      down: B.units.filter(u => u.side === 'p' && u.dead).length,
      turns: Math.min(B.turn, B.maxTurns),
      targetWasCore: B.isCore,
      backupDestroyed: B.hasBackup ? !left.some(t => t.o.k === 'backup') : true,
      reason: B.outcome ? B.outcome.reason : '',
    };
  }

  G.Baskin = {
    N, DIRS, UNIT_TYPES, ABILITIES, OBJ_NAMES, TARGET_KINDS, SCAN_RADIUS, LAYOUTS,
    create, reachable, targets, doMove, doAct, undo, preview, endTurn, retreat, result, intents, scanned,
    unitAt, cell, alive, targetsLeft, canMove, canAct, clone,
  };
})(typeof window !== 'undefined' ? window : globalThis);
