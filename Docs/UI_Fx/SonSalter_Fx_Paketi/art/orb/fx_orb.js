/* FX · YZ küresi. Her <canvas data-orb> bir küre çizer.
   data-level 0–100 (sızma) · data-r yarıçap (CSS px) · data-kind normal|unknown|quar|core · data-seed · data-auto sweep|off
   Hâller sürekli karışır: 1 altın düzen (0–30) → 2 turuncu-mavi kararsız (30–70) → 3 mavi kristal (70–100). */
function fxOrbs(root, opt) {
  opt = opt || {};
  var DPR = Math.min(2, window.devicePixelRatio || 1);
  var FPS = opt.fps || 40;
  try { if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) FPS = 8; } catch (e) {}
  function rng(seed) {
    var a = seed >>> 0;
    return function () {
      a = (a + 0x6D2B79F5) | 0;
      var t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  function ss(a, b, x) { var t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); }
  function lerp(a, b, t) { return a + (b - a) * t; }
  function mix(c1, c2, t) { return [lerp(c1[0], c2[0], t), lerp(c1[1], c2[1], t), lerp(c1[2], c2[2], t)]; }
  function rgba(c, a) { return 'rgba(' + (c[0] | 0) + ',' + (c[1] | 0) + ',' + (c[2] | 0) + ',' + Math.max(0, Math.min(1, a)).toFixed(3) + ')'; }
  function norm(v) { var l = Math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]) || 1; return [v[0] / l, v[1] / l, v[2] / l]; }

  var C = {
    gold: [255, 176, 64], goldHi: [255, 226, 150], amber: [255, 128, 36], blue: [58, 140, 255], ice: [150, 232, 255],
    white: [235, 250, 255], grey: [110, 140, 156], mint: [70, 255, 186], red: [255, 70, 90]
  };
  var spriteCache = {};
  function sprite(c) {
    // renk 8'lik adımlara yuvarlanır: önbellek sınırlı kalır (geçiş bandındaki karışık renkler sonsuz doku üretmez)
    c = [Math.min(255, Math.round(c[0] / 8) * 8), Math.min(255, Math.round(c[1] / 8) * 8), Math.min(255, Math.round(c[2] / 8) * 8)];
    var k = c[0] + ',' + c[1] + ',' + c[2];
    if (spriteCache[k]) return spriteCache[k];
    var s = document.createElement('canvas');
    s.width = s.height = 64;
    var g = s.getContext('2d');
    var gr = g.createRadialGradient(32, 32, 0, 32, 32, 32);
    gr.addColorStop(0, 'rgba(255,255,255,1)');
    gr.addColorStop(0.12, rgba(mix(c, [255, 255, 255], 0.45), 1));
    gr.addColorStop(0.3, rgba(c, 0.55));
    gr.addColorStop(0.62, rgba(c, 0.12));
    gr.addColorStop(1, rgba(c, 0));
    g.fillStyle = gr;
    g.fillRect(0, 0, 64, 64);
    spriteCache[k] = s;
    return s;
  }

  // ikosküre (42 köşe, 80 yüz, 120 kenar)
  var ICO = (function () {
    var t = (1 + Math.sqrt(5)) / 2;
    var v = [[-1, t, 0], [1, t, 0], [-1, -t, 0], [1, -t, 0], [0, -1, t], [0, 1, t], [0, -1, -t], [0, 1, -t], [t, 0, -1], [t, 0, 1], [-t, 0, -1], [-t, 0, 1]].map(norm);
    var f = [[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11], [1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8], [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9], [4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]];
    var cache = {}, nf = [];
    function mid(a, b) {
      var k = a < b ? a + '_' + b : b + '_' + a;
      if (cache[k] != null) return cache[k];
      v.push(norm([(v[a][0] + v[b][0]) / 2, (v[a][1] + v[b][1]) / 2, (v[a][2] + v[b][2]) / 2]));
      return (cache[k] = v.length - 1);
    }
    f.forEach(function (tr) {
      var a = mid(tr[0], tr[1]), b = mid(tr[1], tr[2]), c = mid(tr[2], tr[0]);
      nf.push([tr[0], a, c], [tr[1], b, a], [tr[2], c, b], [a, b, c]);
    });
    var es = {}, edges = [];
    nf.forEach(function (tr) {
      for (var i = 0; i < 3; i++) {
        var a = tr[i], b = tr[(i + 1) % 3], k = a < b ? a + '_' + b : b + '_' + a;
        if (!es[k]) { es[k] = 1; edges.push([a, b]); }
      }
    });
    return { v: v, f: nf, e: edges };
  })();

  function build(cv) {
    var d = cv.dataset;
    var R = +d.r || 30, seed = +d.seed || 1, kind = d.kind || 'normal';
    var rnd = rng(seed * 9973 + 17);
    var S = cv.clientWidth || Math.round(R * 4.4);
    var o = { cv: cv, ctx: cv.getContext('2d'), R: R, S: S, kind: kind, seed: seed, ang: rnd() * 6.283, lv: +d.level || 0, t0: -1, sparks: [], fil: [], arcs: [], nextFil: 0, filK: d.fil ? +d.fil : 1 };
    o.scale = cv.width / S;
    var n = Math.round(56 + R * 2.4), P = [];
    for (var i = 0; i < n; i++) {
      var y = 1 - ((i + 0.5) / n) * 2, rr = Math.sqrt(1 - y * y), th = i * 2.399963 + rnd() * 0.25;
      P.push({ x: Math.cos(th) * rr, y: y, z: Math.sin(th) * rr, s: 0.55 + rnd() * 1.0, ph: rnd() * 6.283, tau: rnd(), ice: rnd() < 0.35 });
    }
    o.P = P;
    var B = [];
    [-0.66, -0.33, 0, 0.33, 0.66].forEach(function (lat, bi) {
      var m = Math.round(16 + R * 0.62 * Math.cos(lat));
      for (var k = 0; k < m; k++) {
        var lon = (k / m) * 6.283 + bi * 0.21;
        B.push({ x: Math.cos(lat) * Math.cos(lon), y: Math.sin(lat), z: Math.cos(lat) * Math.sin(lon), tau: rnd(), ph: rnd() * 6.283 });
      }
    });
    o.B = B;
    var K = [];
    for (var j = 0; j < 34; j++) K.push({ lat: (rnd() * 2 - 1) * 1.2, lon: rnd() * 6.283, w: 0.08 + rnd() * 0.26, h: 0.02 + rnd() * 0.035, tau: rnd(), ph: rnd() * 6.283, dash: rnd() < 0.4 });
    o.K = K;
    o.rings = [{ nx: 0.35, ny: 0.92, nz: 0.18, rr: 1.3, sp: 0.55, dash: 22 }, { nx: -0.82, ny: 0.4, nz: 0.41, rr: 1.52, sp: -0.38, dash: 30 }];
    o.rings.forEach(function (r) { var nn = norm([r.nx, r.ny, r.nz]); r.n = nn; var a = Math.abs(nn[1]) < 0.9 ? [0, 1, 0] : [1, 0, 0]; var u = norm(cross(a, nn)); r.u = u; r.w = cross(nn, u); });
    o.jag = ICO.v.map(function () { var k = 0.8 + rnd() * 0.42; if (rnd() < 0.22) k += 0.38 + rnd() * 0.42; return k; });
    o.kap = ICO.f.map(function () { return rnd(); });
    o.ekap = ICO.e.map(function () { return rnd(); });
    var SH = [];
    for (var q = 0; q < 10; q++) SH.push({ a: rnd() * 6.283, rr: 1.3 + rnd() * 0.55, sp: (0.4 + rnd() * 0.7) * (rnd() < 0.5 ? -1 : 1), tilt: (rnd() * 2 - 1) * 0.9, sz: 0.1 + rnd() * 0.12, rot: rnd() * 6.283, tau: rnd() });
    o.SH = SH;
    o.rnd = rnd;
    return o;
  }
  function cross(a, b) { return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]; }

  function sweep(t) { // 18 sn: 0'da bekle → 100'e çık → bekle → in
    var p = t % 18;
    if (p < 2) return 0;
    if (p < 8) return 100 * ss(0, 1, (p - 2) / 6);
    if (p < 10) return 100;
    if (p < 16) return 100 * (1 - ss(0, 1, (p - 10) / 6));
    return 0;
  }

  function draw(o, t, dt) {
    var cv = o.cv, d = cv.dataset, g = o.ctx, R = o.R, S = o.S;
    var target, swp = d.auto === 'sweep';
    if (o.kind === 'unknown') target = 0;
    else if (swp) {
      var ts = t;
      if (d.sync) {
        var el = o.syncEl || (o.syncEl = document.querySelector(d.sync));
        if (el && el.getAnimations) { var an = el.getAnimations(); if (an.length && an[0].currentTime != null) ts = an[0].currentTime / 1000; }
      }
      target = sweep(ts + (+d.phase || 0));
    } else target = +d.level || 0;
    if (o.t0 < 0) { o.t0 = t; o.lv = target; }
    o.lv += (target - o.lv) * Math.min(1, dt * (swp ? 6 : 3));
    var s = o.lv / 100;
    var wA = 1 - ss(0.18, 0.5, s), wC = ss(0.55, 0.86, s), wB = Math.max(0, 1 - wA - wC);
    var blueF = ss(0.24, 0.8, s);
    var unk = o.kind === 'unknown', quar = o.kind === 'quar', core = o.kind === 'core';
    var dim = quar ? 0.72 : 1;
    var intro = Math.min(1, (t - o.t0) / 1.1);
    var grow = intro * intro * (3 - 2 * intro);
    g.setTransform(o.scale, 0, 0, o.scale, 0, 0);
    g.clearRect(0, 0, S, S);
    g.globalCompositeOperation = 'lighter';
    var cx = S / 2, cy = S / 2, Rg = R * (0.55 + 0.45 * grow);
    o.ang += dt * (unk ? 0.12 : (0.2 + 0.62 * s) * (quar ? 0.45 : 1));
    var yaw = o.ang, pitch = -0.36 + 0.1 * Math.sin(t * 0.8 + o.seed) * (wB + wC);
    var cyw = Math.cos(yaw), syw = Math.sin(yaw), cp = Math.cos(pitch), sp = Math.sin(pitch);
    function rot(x, y, z) { var x1 = x * cyw + z * syw, z1 = -x * syw + z * cyw; return [x1, y * cp - z1 * sp, y * sp + z1 * cp]; }

    var tone = mix(C.gold, C.blue, blueF), toneHi = mix(C.goldHi, C.ice, blueF);
    if (unk) { tone = C.grey; toneHi = [170, 196, 206]; }

    // dış hale + çekirdek
    var hal = g.createRadialGradient(cx, cy, 0, cx, cy, Rg * 2.05);
    hal.addColorStop(0, rgba(tone, (unk ? 0.05 : 0.2) * dim * grow));
    hal.addColorStop(0.45, rgba(tone, (unk ? 0.03 : 0.09) * dim * grow));
    hal.addColorStop(1, rgba(tone, 0));
    g.fillStyle = hal;
    g.fillRect(0, 0, S, S);
    if (!unk) {
      var pulse = 0.82 + 0.18 * Math.sin(t * (2 + 3 * s) + o.seed);
      var cg = g.createRadialGradient(cx, cy, 0, cx, cy, Rg * (0.62 + 0.25 * wC));
      cg.addColorStop(0, rgba(C.white, 0.75 * pulse * dim));
      cg.addColorStop(0.25, rgba(toneHi, 0.5 * pulse * dim));
      cg.addColorStop(1, rgba(tone, 0));
      g.fillStyle = cg;
      g.beginPath(); g.arc(cx, cy, Rg * 0.95, 0, 6.283); g.fill();
    }

    // hâl 2: çekirdekte parlama (dönen ışın yıldızı)
    var flA = (wB * 1.0 + wC * 0.35) * dim * grow * (unk ? 0 : 1);
    if (flA > 0.03) {
      var nR = 6, base = t * (0.9 + s) + o.seed;
      for (var fr = 0; fr < nR; fr++) {
        var aR = base + fr * (6.283 / nR) + 0.25 * Math.sin(t * 3 + fr), len2 = Rg * (0.8 + 0.5 * Math.abs(Math.sin(t * 2.7 + fr * 1.3)));
        var colR = fr % 2 ? mix(C.amber, C.goldHi, 0.4) : C.ice;
        if (s > 0.7) colR = C.ice;
        var gr2 = g.createLinearGradient(cx, cy, cx + Math.cos(aR) * len2, cy + Math.sin(aR) * len2);
        gr2.addColorStop(0, rgba(C.white, 0.55 * flA));
        gr2.addColorStop(0.35, rgba(colR, 0.32 * flA));
        gr2.addColorStop(1, rgba(colR, 0));
        g.strokeStyle = gr2; g.lineWidth = Rg * 0.07;
        g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + Math.cos(aR) * len2, cy + Math.sin(aR) * len2); g.stroke();
      }
      var fs = Rg * (0.35 + 0.12 * Math.sin(t * 7));
      g.globalAlpha = 0.85 * flA;
      g.drawImage(sprite(s > 0.6 ? C.ice : C.goldHi), cx - fs, cy - fs, fs * 2, fs * 2);
      g.globalAlpha = 1;
    }

    // çekirdek şok halkaları
    if (core && !unk) {
      for (var w = 0; w < 2; w++) {
        var ph = ((t + w * 1.2) % 2.4) / 2.4;
        g.strokeStyle = rgba(tone, (1 - ph) * 0.45 * grow);
        g.lineWidth = 1.6;
        g.beginPath(); g.arc(cx, cy, Rg * (1.05 + ph * 0.95), 0, 6.283); g.stroke();
      }
    }

    // halkalar (hâl 1)
    var ringA = wA * (unk ? 0 : 1) * dim * grow;
    if (ringA > 0.02) {
      o.rings.forEach(function (r, ri) {
        var spin = t * r.sp + ri;
        var N = 72, prev = null;
        for (var k = 0; k <= N; k++) {
          var a = (k / N) * 6.283;
          var px = (r.u[0] * Math.cos(a) + r.w[0] * Math.sin(a)) * r.rr, py = (r.u[1] * Math.cos(a) + r.w[1] * Math.sin(a)) * r.rr, pz = (r.u[2] * Math.cos(a) + r.w[2] * Math.sin(a)) * r.rr;
          var q = rot(px, py, pz), X = cx + q[0] * Rg, Y = cy - q[1] * Rg;
          if (prev && ((k + Math.floor(spin * 6)) % 3 !== 0)) {
            var depth = (q[2] / r.rr + 1) / 2;
            g.strokeStyle = rgba(C.goldHi, ringA * (0.12 + 0.5 * depth));
            g.lineWidth = 1.1;
            g.beginPath(); g.moveTo(prev[0], prev[1]); g.lineTo(X, Y); g.stroke();
          }
          prev = [X, Y];
        }
        for (var sIdx = 0; sIdx < 2; sIdx++) {
          var aa = spin * 1.6 + sIdx * 3.1416;
          var sx = (r.u[0] * Math.cos(aa) + r.w[0] * Math.sin(aa)) * r.rr, sy = (r.u[1] * Math.cos(aa) + r.w[1] * Math.sin(aa)) * r.rr, sz = (r.u[2] * Math.cos(aa) + r.w[2] * Math.sin(aa)) * r.rr;
          var qq = rot(sx, sy, sz), dd = (qq[2] / r.rr + 1) / 2, spz = R * (0.22 + 0.16 * dd);
          g.globalAlpha = ringA * (0.35 + 0.65 * dd);
          g.drawImage(sprite(C.goldHi), cx + qq[0] * Rg - spz, cy - qq[1] * Rg - spz, spz * 2, spz * 2);
          g.globalAlpha = 1;
        }
      });
    }

    // tel kafes: enlem + boylam çizgileri (hâl 1 düzeni; hâl 2'de bükülür, söner)
    var wireA = (wA * 0.9 + wB * 0.35) * dim * grow * (unk ? 0.35 : 1);
    if (wireA > 0.02) {
      g.lineWidth = 0.9;
      var wcol = unk ? C.grey : mix(C.gold, C.blue, blueF * 0.8);
      var lats = [-1.05, -0.62, -0.21, 0.21, 0.62, 1.05];
      for (var li = 0; li < lats.length; li++) {
        var la = lats[li], prvL = null;
        for (var k8 = 0; k8 <= 64; k8++) {
          var lo8 = (k8 / 64) * 6.283, wob = wB * 0.07 * Math.sin(lo8 * 3 + t * 2.4 + li);
          var q8 = rot(Math.cos(la) * Math.cos(lo8) * (1 + wob), Math.sin(la) * (1 + wob), Math.cos(la) * Math.sin(lo8) * (1 + wob));
          var P8 = [cx + q8[0] * Rg, cy - q8[1] * Rg, q8[2]];
          if (prvL) { var dl = ((P8[2] + prvL[2]) / 2 + 1) / 2; g.strokeStyle = rgba(wcol, wireA * (0.04 + 0.34 * dl * dl)); g.beginPath(); g.moveTo(prvL[0], prvL[1]); g.lineTo(P8[0], P8[1]); g.stroke(); }
          prvL = P8;
        }
      }
      for (var mi = 0; mi < 8; mi++) {
        var lo9 = (mi / 8) * 3.1416, prvM = null;
        for (var k9 = 0; k9 <= 40; k9++) {
          var la9 = (k9 / 40) * 6.283, wob2 = wB * 0.07 * Math.sin(la9 * 3 + t * 2.1 + mi);
          var q9 = rot(Math.cos(la9) * Math.cos(lo9) * (1 + wob2), Math.sin(la9) * (1 + wob2), Math.cos(la9) * Math.sin(lo9) * (1 + wob2));
          var P9 = [cx + q9[0] * Rg, cy - q9[1] * Rg, q9[2]];
          if (prvM) { var dm = ((P9[2] + prvM[2]) / 2 + 1) / 2; g.strokeStyle = rgba(wcol, wireA * (0.03 + 0.26 * dm * dm)); g.beginPath(); g.moveTo(prvM[0], prvM[1]); g.lineTo(P9[0], P9[1]); g.stroke(); }
          prvM = P9;
        }
      }
      // iç işlemci halkası
      if (!unk) {
        var ir = 0.42, prvI = null;
        for (var k10 = 0; k10 <= 48; k10++) {
          var aI = (k10 / 48) * 6.283 + t * 1.4;
          var qI = rot(Math.cos(aI) * ir, 0.1 * Math.sin(aI * 2 + t), Math.sin(aI) * ir);
          var PI = [cx + qI[0] * Rg, cy - qI[1] * Rg];
          if (prvI && k10 % 4 !== 0) { g.strokeStyle = rgba(C.goldHi, wA * 0.55 * dim * grow); g.lineWidth = 1.4; g.beginPath(); g.moveTo(prvI[0], prvI[1]); g.lineTo(PI[0], PI[1]); g.stroke(); }
          prvI = PI;
        }
      }
    }

    // kabuk parçacıkları
    var jit = wB * 0.2 + wC * 0.05;
    var flick = unk ? 1 : 0;
    for (var i = 0; i < o.P.length; i++) {
      var p = o.P[i];
      var rr2 = 1 + jit * Math.sin(t * 2.6 + p.ph * 3) + wC * 0.06 * Math.sin(p.ph * 7);
      var nx = p.x * rr2 + jit * 0.5 * Math.sin(t * 1.9 + p.ph), ny = p.y * rr2 + jit * 0.5 * Math.cos(t * 2.3 + p.ph * 2), nz = p.z * rr2;
      var q = rot(nx, ny, nz), depth = (q[2] + 1) / 2;
      var col, a = (0.18 + 0.82 * depth) * dim * grow;
      if (unk) { col = C.grey; a *= 0.35 + 0.65 * Math.abs(Math.sin(t * 9 + p.ph * 13)); }
      else {
        var edge = p.tau - blueF;
        var cb = p.ice ? C.ice : C.blue, cg2 = p.ice ? C.goldHi : C.gold;
        if (edge > 0.07) col = cg2; else if (edge < -0.07) col = cb; else col = mix(cb, cg2, (edge + 0.07) / 0.14);
      }
      var sz = R * 0.085 * p.s * (0.65 + 0.55 * depth) * (1 - 0.3 * wC);
      g.globalAlpha = a * (0.42 * wA + 1.0 * wB + 0.3 * wC + (unk ? 0.6 : 0));
      if (wB > 0.25 && p.s > 1.25) {
        var vx = Math.sin(t * 1.9 + p.ph) * 0.12 * wB, vy = Math.cos(t * 2.3 + p.ph * 2) * 0.12 * wB, q7 = rot(nx - vx * 2.2, ny - vy * 2.2, nz);
        g.strokeStyle = rgba(col, g.globalAlpha * 0.6); g.lineWidth = sz * 0.7;
        g.beginPath(); g.moveTo(cx + q7[0] * Rg, cy - q7[1] * Rg); g.lineTo(cx + q[0] * Rg, cy - q[1] * Rg); g.stroke();
      }
      g.drawImage(sprite(col), cx + q[0] * Rg - sz * 1.6, cy - q[1] * Rg - sz * 1.6, sz * 3.2, sz * 3.2);
    }
    g.globalAlpha = 1;

    // enlem bantları (hâl 1 düzeni; hâl 2'de dağılır)
    var bandA = (wA + wB * 0.45) * dim * grow * (unk ? 0.5 : 1);
    if (bandA > 0.02) {
      for (var b = 0; b < o.B.length; b++) {
        var e = o.B[b];
        var dx = wB * 0.18 * Math.sin(t * 3.1 + e.ph * 5), dy = wB * 0.14 * Math.cos(t * 2.7 + e.ph * 3);
        var qb = rot(e.x + dx, e.y + dy, e.z), db = (qb[2] + 1) / 2;
        if (db < 0.18) continue;
        var colb = unk ? C.grey : (e.tau < blueF ? C.ice : C.goldHi);
        var szb = R * 0.05 * (0.6 + 0.6 * db);
        g.globalAlpha = bandA * (0.1 + 0.9 * db) * (unk ? 0.4 : 1);
        g.drawImage(sprite(colb), cx + qb[0] * Rg - szb * 1.5, cy - qb[1] * Rg - szb * 1.5, szb * 3, szb * 3);
      }
      g.globalAlpha = 1;
    }

    // veri blokları (hâl 1)
    var blkA = wA * dim * grow * (unk ? 0 : 1);
    if (blkA > 0.02) {
      g.lineCap = 'butt';
      for (var k2 = 0; k2 < o.K.length; k2++) {
        var bl = o.K[k2];
        var cl = Math.cos(bl.lat), sl = Math.sin(bl.lat), co = Math.cos(bl.lon), so = Math.sin(bl.lon);
        var px2 = cl * co * 1.04, py2 = sl * 1.04, pz2 = cl * so * 1.04, tx = -so, tz = co;
        var a1 = rot(px2 - tx * bl.w / 2, py2, pz2 - tz * bl.w / 2), a2 = rot(px2 + tx * bl.w / 2, py2, pz2 + tz * bl.w / 2);
        var dk = ((a1[2] + a2[2]) / 2 + 1) / 2;
        var blink = 0.55 + 0.45 * Math.sin(t * 2.2 + bl.ph * 4);
        g.strokeStyle = rgba(bl.tau < blueF ? C.ice : C.goldHi, blkA * (0.08 + 0.75 * dk) * blink);
        g.lineWidth = Math.max(1, bl.h * Rg * (0.6 + 0.6 * dk));
        if (bl.dash) g.setLineDash([2.2, 1.6]);
        g.beginPath(); g.moveTo(cx + a1[0] * Rg, cy - a1[1] * Rg); g.lineTo(cx + a2[0] * Rg, cy - a2[1] * Rg); g.stroke();
        if (bl.dash) g.setLineDash([]);
      }
    }

    // kristal kabuk (hâl 3; hâl 2'de parça parça belirir)
    var crF = ss(0.42, 0.92, s) * (unk ? 0 : 1);
    if (crF > 0.01) {
      var V = ICO.v.map(function (v, vi) {
        var k = lerp(1, o.jag[vi], ss(0.5, 1, s)) * (1.02 + 0.05 * Math.sin(t * 1.3 + vi));
        return rot(v[0] * k, v[1] * k, v[2] * k);
      });
      for (var fi = 0; fi < ICO.f.length; fi++) {
        var vis = ss(o.kap[fi] - 0.08, o.kap[fi] + 0.08, crF * 1.08);
        if (vis < 0.02) continue;
        var tr = ICO.f[fi], A0 = V[tr[0]], A1 = V[tr[1]], A2 = V[tr[2]];
        var nz2 = (A1[0] - A0[0]) * (A2[1] - A0[1]) - (A1[1] - A0[1]) * (A2[0] - A0[0]);
        if (nz2 <= 0) continue;
        var nrm = norm(cross([A1[0] - A0[0], A1[1] - A0[1], A1[2] - A0[2]], [A2[0] - A0[0], A2[1] - A0[1], A2[2] - A0[2]]));
        var light = Math.max(0, nrm[0] * -0.4 + nrm[1] * 0.55 + nrm[2] * 0.75);
        g.fillStyle = rgba(mix(C.blue, C.ice, light), (0.05 + 0.26 * light * light) * vis * dim * grow);
        g.beginPath(); g.moveTo(cx + A0[0] * Rg, cy - A0[1] * Rg); g.lineTo(cx + A1[0] * Rg, cy - A1[1] * Rg); g.lineTo(cx + A2[0] * Rg, cy - A2[1] * Rg); g.closePath(); g.fill();
      }
      g.lineWidth = 1;
      for (var ei = 0; ei < ICO.e.length; ei++) {
        var ev = ss(o.ekap[ei] - 0.1, o.ekap[ei] + 0.1, crF * 1.1);
        if (ev < 0.02) continue;
        var E0 = V[ICO.e[ei][0]], E1 = V[ICO.e[ei][1]], de = ((E0[2] + E1[2]) / 2 + 1.3) / 2.3;
        var fl = 0.75 + 0.25 * Math.sin(t * 6 + ei * 1.7);
        g.strokeStyle = rgba(de > 0.62 ? C.white : C.ice, ev * (0.06 + 0.62 * de * de) * fl * dim * grow);
        g.beginPath(); g.moveTo(cx + E0[0] * Rg, cy - E0[1] * Rg); g.lineTo(cx + E1[0] * Rg, cy - E1[1] * Rg); g.stroke();
      }
      // iç çatlaklar (çekirdekten sivri uçlara) ve kenar çıtırtısı
      for (var vc = 0; vc < V.length; vc++) {
        if (o.jag[vc] < 1.12) continue;
        var cr = ss(0.62, 1, s) * (0.4 + 0.6 * Math.abs(Math.sin(t * 2.3 + vc)));
        g.strokeStyle = rgba(C.ice, 0.22 * cr * dim * grow); g.lineWidth = 1;
        g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + V[vc][0] * Rg * 0.92, cy - V[vc][1] * Rg * 0.92); g.stroke();
      }
      var ck = Math.floor(t * 9) % ICO.e.length;
      for (var cz = 0; cz < 3; cz++) {
        var ee = ICO.e[(ck * 7 + cz * 37) % ICO.e.length], C0 = V[ee[0]], C1 = V[ee[1]];
        if (C0[2] + C1[2] < 0) continue;
        g.strokeStyle = rgba(C.white, 0.9 * wC * dim); g.lineWidth = 1.6;
        g.beginPath(); g.moveTo(cx + C0[0] * Rg, cy - C0[1] * Rg); g.lineTo(cx + C1[0] * Rg, cy - C1[1] * Rg); g.stroke();
      }
      // uç noktalarda ışık
      for (var vi2 = 0; vi2 < V.length; vi2++) {
        if (o.jag[vi2] < 1.1 || V[vi2][2] < 0) continue;
        var szv = R * 0.16 * ss(0.6, 1, s);
        g.globalAlpha = 0.7 * crF * dim;
        g.drawImage(sprite(C.ice), cx + V[vi2][0] * Rg - szv, cy - V[vi2][1] * Rg - szv, szv * 2, szv * 2);
      }
      g.globalAlpha = 1;
      // yörüngede kırıklar
      var shA = wC * dim * grow;
      if (shA > 0.02) {
        g.lineWidth = 1.2;
        for (var si = 0; si < o.SH.length; si++) {
          var sh = o.SH[si];
          var ang = sh.a + t * sh.sp, rr3 = sh.rr;
          var p0 = rot(Math.cos(ang) * rr3, Math.sin(ang * 0.7) * sh.tilt, Math.sin(ang) * rr3);
          var dsh = (p0[2] / rr3 + 1) / 2, X0 = cx + p0[0] * Rg, Y0 = cy - p0[1] * Rg, L = sh.sz * Rg, rt = sh.rot + t * 2 * sh.sp;
          g.fillStyle = rgba(C.ice, shA * (0.15 + 0.5 * dsh));
          g.strokeStyle = rgba(C.white, shA * (0.25 + 0.6 * dsh));
          g.beginPath();
          g.moveTo(X0 + Math.cos(rt) * L, Y0 + Math.sin(rt) * L);
          g.lineTo(X0 + Math.cos(rt + 2.3) * L * 0.6, Y0 + Math.sin(rt + 2.3) * L * 0.6);
          g.lineTo(X0 + Math.cos(rt + 3.9) * L * 0.5, Y0 + Math.sin(rt + 3.9) * L * 0.5);
          g.closePath(); g.fill(); g.stroke();
        }
      }
    }

    // karantina kafesi
    if (quar && !unk) {
      g.strokeStyle = rgba(C.mint, 0.42 * grow);
      g.lineWidth = 1;
      var qa = -t * 0.35;
      var cqa = Math.cos(qa), sqa = Math.sin(qa);
      [[-0.5, 1], [0, 1], [0.5, 1]].forEach(function (lt) {
        var la = lt[0], prv = null;
        for (var k3 = 0; k3 <= 48; k3++) {
          var lo = (k3 / 48) * 6.283, x3 = Math.cos(la) * Math.cos(lo) * 1.42, y3 = Math.sin(la) * 1.42, z3 = Math.cos(la) * Math.sin(lo) * 1.42;
          var xr = x3 * cqa + z3 * sqa, zr = -x3 * sqa + z3 * cqa;
          var q3 = [xr, y3 * cp - zr * sp, y3 * sp + zr * cp];
          var P3 = [cx + q3[0] * Rg, cy - q3[1] * Rg];
          if (prv && k3 % 2 === 0) { g.beginPath(); g.moveTo(prv[0], prv[1]); g.lineTo(P3[0], P3[1]); g.stroke(); }
          prv = P3;
        }
      });
      for (var mrd = 0; mrd < 6; mrd++) {
        var lo2 = (mrd / 6) * 3.1416 + qa, prv2 = null;
        for (var k4 = 0; k4 <= 32; k4++) {
          var la2 = (k4 / 32) * 3.1416 - 1.5708, x4 = Math.cos(la2) * Math.cos(lo2) * 1.42, y4 = Math.sin(la2) * 1.42, z4 = Math.cos(la2) * Math.sin(lo2) * 1.42;
          var q4 = [x4, y4 * cp - z4 * sp, y4 * sp + z4 * cp];
          var P4 = [cx + q4[0] * Rg, cy - q4[1] * Rg];
          if (prv2 && k4 % 2 === 1 && q4[2] > -0.2) { g.beginPath(); g.moveTo(prv2[0], prv2[1]); g.lineTo(P4[0], P4[1]); g.stroke(); }
          prv2 = P4;
        }
      }
    }

    // şimşek lifleri ve yaylar (hâl 2; hâl 3'te az)
    var filN = unk ? 0 : Math.round(wB * 5 + wC * 2 + (s > 0.3 ? 1 : 0) * wB);
    if (t > o.nextFil) {
      o.fil = [];
      for (var fN = 0; fN < filN; fN++) {
        var dir = norm([o.rnd() * 2 - 1, o.rnd() * 2 - 1, o.rnd() * 2 - 1]);
        var len = ((quar ? 0.95 : 1.15) + o.rnd() * (quar ? 0.3 : (0.7 + wB * 0.6))) * o.filK, pts = [], segs = 9;
        var side = norm(cross(dir, [0.3, 1, 0.2]));
        for (var sg = 0; sg <= segs; sg++) {
          var f2 = sg / segs, rad = 0.25 + f2 * len, off = (o.rnd() * 2 - 1) * 0.16 * (1 - Math.abs(f2 - 0.5));
          pts.push([dir[0] * rad + side[0] * off, dir[1] * rad + side[1] * off, dir[2] * rad + side[2] * off]);
        }
        o.fil.push(pts);
      }
      o.arcs = [];
      var arcN = unk ? 0 : Math.round(wB * 3 + wC * 1);
      for (var aN = 0; aN < arcN; aN++) {
        var ax = norm([o.rnd() * 2 - 1, o.rnd() * 2 - 1, o.rnd() * 2 - 1]), a0 = o.rnd() * 6.283, span = 1.2 + o.rnd() * 1.6;
        var u2 = norm(cross(ax, [0.1, 1, 0.3])), w2 = cross(ax, u2), apts = [];
        for (var as = 0; as <= 16; as++) {
          var aa2 = a0 + (as / 16) * span, rr4 = 1.08 + o.rnd() * 0.12 + 0.12 * Math.sin(as);
          apts.push([(u2[0] * Math.cos(aa2) + w2[0] * Math.sin(aa2)) * rr4, (u2[1] * Math.cos(aa2) + w2[1] * Math.sin(aa2)) * rr4, (u2[2] * Math.cos(aa2) + w2[2] * Math.sin(aa2)) * rr4]);
        }
        o.arcs.push(apts);
      }
      o.nextFil = t + 0.07 + o.rnd() * (0.22 - 0.1 * wB);
    }
    function polyline(list, colW, colT, aW, aT, lw) {
      list.forEach(function (pts) {
        var sc = pts.map(function (p2) { var q5 = rot(p2[0], p2[1], p2[2]); return [cx + q5[0] * Rg, cy - q5[1] * Rg]; });
        g.lineJoin = 'round';
        g.strokeStyle = rgba(colW, aW); g.lineWidth = lw * 3.2;
        g.beginPath(); sc.forEach(function (pt, k5) { if (k5) g.lineTo(pt[0], pt[1]); else g.moveTo(pt[0], pt[1]); }); g.stroke();
        g.strokeStyle = rgba(colT, aT); g.lineWidth = lw;
        g.beginPath(); sc.forEach(function (pt, k5) { if (k5) g.lineTo(pt[0], pt[1]); else g.moveTo(pt[0], pt[1]); }); g.stroke();
      });
    }
    if (o.fil.length) polyline(o.fil, C.blue, C.white, 0.16 * dim * grow, 0.85 * dim * grow, 1.1);
    if (o.arcs.length) polyline(o.arcs, C.blue, C.ice, 0.2 * dim * grow, 0.7 * dim * grow, 1.3);

    // kıvılcımlar
    if (!unk) {
      var rate = (1.2 * wA + 16 * wB + 20 * wC) * (quar ? 0.4 : 1) * (R / 34);
      o.spAcc = (o.spAcc || 0) + rate * dt;
      while (o.spAcc > 1) {
        o.spAcc -= 1;
        var dv = norm([o.rnd() * 2 - 1, o.rnd() * 2 - 1, o.rnd() * 2 - 1]);
        var spd = 0.6 + o.rnd() * (0.6 + wB);
        o.sparks.push({ p: [dv[0], dv[1], dv[2]], v: [dv[0] * spd, dv[1] * spd, dv[2] * spd], life: 0, max: 0.5 + o.rnd() * 0.7, col: o.rnd() < blueF ? (o.rnd() < 0.5 ? C.ice : C.blue) : (o.rnd() < 0.5 ? C.goldHi : C.amber) });
      }
      for (var sk = o.sparks.length - 1; sk >= 0; sk--) {
        var spk = o.sparks[sk];
        spk.life += dt;
        if (spk.life > spk.max) { o.sparks.splice(sk, 1); continue; }
        spk.p[0] += spk.v[0] * dt; spk.p[1] += spk.v[1] * dt; spk.p[2] += spk.v[2] * dt;
        var q6 = rot(spk.p[0], spk.p[1], spk.p[2]), lf = 1 - spk.life / spk.max, szs = R * 0.07 * (0.5 + lf);
        g.globalAlpha = lf * 0.9 * dim;
        g.drawImage(sprite(spk.col), cx + q6[0] * Rg - szs, cy - q6[1] * Rg - szs, szs * 2, szs * 2);
      }
      g.globalAlpha = 1;
    }

    // bilinmeyen: parazit çizgileri
    if (unk) {
      for (var gl = 0; gl < 3; gl++) {
        var yy = cy + Math.sin(t * (1.3 + gl) + gl * 2) * R * 0.9, ww = R * (0.6 + 0.6 * Math.abs(Math.sin(t * 3 + gl)));
        g.fillStyle = rgba([150, 180, 190], 0.08 + 0.08 * Math.abs(Math.sin(t * 11 + gl)));
        g.fillRect(cx - ww, yy, ww * 2, 1.4);
      }
    }
    g.globalCompositeOperation = 'source-over';
    o.lastLevel = o.lv;
  }

  var orbs = [];
  function scan() {
    var cvs = (root.querySelectorAll ? root : document).querySelectorAll('canvas[data-orb]');
    var live = [];
    for (var i = 0; i < cvs.length; i++) {
      var cv = cvs[i], o = null;
      for (var j = 0; j < orbs.length; j++) if (orbs[j].cv === cv) { o = orbs[j]; break; }
      if (!o) { try { o = build(cv); } catch (e) { continue; } }
      live.push(o);
    }
    orbs = live;
  }
  scan();
  if (opt.manual) {
    // belirlenimci adım: altın kareler ve karşılaştırma için (rAF yok). api.advance(dt, n) → t
    var tm = 0;
    var api = function () {};
    api.advance = function (dt, n) {
      for (var i = 0; i < (n || 1); i++) {
        tm += dt;
        for (var k = 0; k < orbs.length; k++) draw(orbs[k], tm, dt);
      }
      return tm;
    };
    api.rescan = scan;
    return api;
  }
  var raf = 0, last = 0, stopped = false, lastScan = 0, T0 = performance.now();
  // görünmüyorsa çizme (sayfa gizli ya da kare ekran dışında); çizim pahalılaşırsa kare hızını düşür
  var vis = true, io = null, drawn = 0, cost = 0, baseFPS = FPS;
  try {
    var tgt = root && root.nodeType === 1 ? root : document.body;
    if (window.IntersectionObserver && tgt) {
      io = new IntersectionObserver(function (es) { vis = es[es.length - 1].isIntersecting; });
      io.observe(tgt);
    }
  } catch (e) { io = null; }
  function frame(now) {
    if (stopped) return;
    raf = requestAnimationFrame(frame);
    if (now - last < 1000 / FPS) return;
    if (drawn > 3 && (document.hidden || !vis)) { last = now; return; }
    var dt = last ? Math.min(0.1, (now - last) / 1000) : 1 / FPS;
    last = now;
    var t = Math.max(0, (now - T0) / 1000);
    if (now - lastScan > 1000) { lastScan = now; scan(); }
    var c0 = performance.now();
    for (var k = 0; k < orbs.length; k++) {
      if (!orbs[k].cv.isConnected) continue;
      draw(orbs[k], t, dt);
    }
    drawn++;
    cost = cost * 0.9 + (performance.now() - c0) * 0.1;
    if (cost > 14 && FPS > 16) FPS = Math.max(16, FPS - 2);
    else if (cost < 6 && FPS < baseFPS) FPS = Math.min(baseFPS, FPS + 1);
  }
  raf = requestAnimationFrame(frame);
  return function () { stopped = true; cancelAnimationFrame(raf); if (io) io.disconnect(); };
}
