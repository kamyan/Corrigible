#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Corrigible (Son Şalter) - placeholder audio for raid-place layers 7 (region beds) and 8 (event cues).

One-off, seeded, deterministic synthesiser (numpy only, no downloads, no recordings). It writes mono 44.1 kHz
16-bit WAVs to Temp/raid_site_audio/out (Bed*.wav, CueEv*.wav) and Temp/raid_site_audio/report.md under the project
root (git-ignored). It never touches Unity or the project's Assets folder; the reference clips in
Assets/Corrigible/Audio/Placeholder are only read for level comparison. Copy the clips you keep into that folder by hand
(same file names, so SoundBank.asset and the .meta import settings stay as they are).

Run (from the project root, needs numpy):   python3 -I Tools/Audio/raid_site_audio.py

Design rules it follows (places_spec.md "Sound (Layers 7 and 8)"):
- Beds: 20-24 s seamless loops. Every continuous layer is built circularly (FFT-shaped noise, smooth periodic
  control curves, oscillators snapped to whole cycles per loop, events wrapped around the loop end, circular
  reverb), so the loop has no seam by construction; the seam is still measured on the written file.
- Identity lives in 450-3000 Hz (rhythm and tonal events). Levels are set after the radio band: each bed is
  scaled so its steady-state post-band RMS is 0.078 (today's LoopRaidAmbience hum).
- Cues: <= 1.15 s, smooth start/end, levelled against the cue they replace, capped by RaidExplosion's in-band
  loudness so no event cue is louder than the explosion / core break.
- Band = biquad HP 450 / LP 3000, Q 0.7071, exactly as bandcheck.py.
"""
import math
import os
import wave
import zlib

import numpy as np

SR = 44100
TAU = 2 * np.pi
SEED = 20261102
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
HERE = os.path.join(ROOT, 'Temp', 'raid_site_audio')
OUT = os.path.join(HERE, 'out')
REF_DIR = os.path.join(ROOT, 'Assets', 'Corrigible', 'Audio', 'Placeholder')
BED_BAND_RMS = 0.078          # today's LoopRaidAmbience after the band
BED_VOLUME = 0.45             # today's bank volume of LoopRaidAmbience
FILE_PEAK_MAX = 0.9


def rng(name):
    """Independent stream per clip: output never depends on generation order."""
    return np.random.default_rng([SEED, zlib.crc32(name.encode('ascii'))])


def ns(t):
    return int(round(t * SR))


# ====================================================================== spectral shapes (magnitude vs Hz)
def _f(f):
    return np.maximum(f, 1.0)


def bump(f, fc, oct_sd):
    """Log-Gaussian bump centred on fc, standard deviation in octaves (fc may be an array)."""
    return np.exp(-0.5 * (np.log2(_f(f) / fc) / oct_sd) ** 2)


def hpm(f, fc, order=2):
    return 1 / np.sqrt(1 + (fc / _f(f)) ** (2 * order))


def lpm(f, fc, order=2):
    return 1 / np.sqrt(1 + (_f(f) / fc) ** (2 * order))


def tilt(f, db_oct, f0=1000.0):
    return (_f(f) / f0) ** (db_oct / 6.0206)


# ====================================================================== building blocks
def noise(r, n, shape=None):
    """Circular (loop-safe) Gaussian noise with amplitude spectrum shape(f); unit RMS."""
    m = n // 2 + 1
    spec = r.standard_normal(m) + 1j * r.standard_normal(m)
    if shape is not None:
        spec = spec * shape(np.fft.rfftfreq(n, 1 / SR))
    spec[0] = 0
    x = np.fft.irfft(spec, n)
    return x / (np.sqrt(np.mean(x * x)) + 1e-15)


def curve(r, n, kmin, kmax, slope=1.0):
    """Smooth periodic random control curve with kmin..kmax cycles per n samples, peak-normalised to +-1."""
    spec = np.zeros(n // 2 + 1, complex)
    k = np.arange(kmin, kmax + 1)
    spec[k] = k ** (-slope) * np.exp(1j * r.uniform(0, TAU, len(k)))
    c = np.fft.irfft(spec, n)
    return c / np.max(np.abs(c))


def unit(c):
    return (c - c.min()) / (c.max() - c.min())


def smoothstep(e0, e1, x):
    u = np.clip((np.asarray(x, float) - e0) / (e1 - e0), 0, 1)
    return u * u * (3 - 2 * u)


def phase(freq, loop=False):
    """Phase in cycles from an instantaneous-frequency array. loop=True nudges the mean frequency so the
    oscillator completes a whole number of cycles over the buffer (the old generator's LoopHz idea)."""
    freq = np.asarray(freq, float)
    if loop:
        c = freq.sum() / SR
        freq = freq + (round(c) - c) * SR / len(freq)
    return np.concatenate(([0.0], np.cumsum(freq)[:-1])) / SR


def snap(f, n):
    return max(1, round(f * n / SR)) * SR / n


def ramp(n):
    """Raised cosine 0 -> 1 over n samples (first sample exactly 0)."""
    if n <= 0:
        return np.ones(0)
    return 0.5 - 0.5 * np.cos(np.pi * np.arange(n) / n)


def env(n, attack=0.002, tau=None, release=0.01, hold=0.0):
    """Raised-cosine attack, optional hold, exponential decay (tau s), raised-cosine release ending at 0."""
    e = np.ones(n)
    a = min(n, ns(attack))
    e[:a] = ramp(a)
    if tau is not None:
        t = np.arange(n) / SR - attack - hold
        e *= np.exp(-np.maximum(t, 0) / tau)
    rl = min(n, ns(release))
    if rl > 0:
        e[n - rl:] *= ramp(rl)[::-1]
    return e


def place(buf, ev, t0, loop=False):
    """Add ev into buf at t0 seconds; in a loop the part past the end wraps to the start."""
    n = len(buf)
    s = ns(t0)
    if loop:
        s %= n
        k = min(len(ev), n - s)
        buf[s:s + k] += ev[:k]
        rest = ev[k:]
        while len(rest):
            m = min(len(rest), n)
            buf[:m] += rest[:m]
            rest = rest[m:]
    else:
        if s >= n or s < 0:
            return
        k = min(len(ev), n - s)
        buf[s:s + k] += ev[:k]


def modal(dur, modes, attack=0.0004):
    """Sum of exponentially decaying sines: (freq Hz, tau s, amp). Starts at zero phase (no step) and fades the
    last few ms, so a ring cut short never leaves a step."""
    n = ns(dur)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for f, tau, a in modes:
        x += a * np.sin(TAU * f * t) * np.exp(-t / tau)
    a_ = ns(attack)
    if a_ > 0:
        x[:a_] *= ramp(a_)
    rl = min(ns(0.003), n // 4)
    if rl > 0:
        x[n - rl:] *= ramp(rl)[::-1]
    return x


def burst(r, dur, shape, attack=0.001, tau=0.02, release=0.003, hold=0.0):
    n = ns(dur)
    x = noise(r, max(n, 4096), shape)[:n]
    return x * env(n, attack, tau, release, hold)


def tv_noise(r, dur, shapefn, nfft=1024, hop=256):
    """Time-varying filtered noise: STFT of white noise, magnitude shaped per frame by shapefn(t, f), overlap-add."""
    n = ns(dur)
    pad = nfft
    total = n + 2 * pad
    x = r.standard_normal(total)
    win = np.sin(np.pi * (np.arange(nfft) + 0.5) / nfft)
    f = np.fft.rfftfreq(nfft, 1 / SR)
    out = np.zeros(total)
    wsum = np.zeros(total)
    for s in range(0, total - nfft + 1, hop):
        tc = min(max((s + nfft / 2 - pad) / SR, 0.0), dur)
        y = np.fft.irfft(np.fft.rfft(x[s:s + nfft] * win) * shapefn(tc, f), nfft) * win
        out[s:s + nfft] += y
        wsum[s:s + nfft] += win * win
    return out[pad:pad + n] / np.maximum(wsum[pad:pad + n], 1e-6)


def poisson_times(r, rate):
    """Sample indices of an inhomogeneous Poisson process with per-sample rate (events per second)."""
    cum = np.cumsum(rate) / SR
    total = cum[-1]
    k = r.poisson(total)
    u = np.sort(r.uniform(0, total, k))
    return np.minimum(np.searchsorted(cum, u), len(rate) - 1)


def reverb_ir(r, rt60, bright=3500.0, pre=0.01, n_er=6, er_span=0.05):
    """Energy-normalised synthetic room: three decaying noise bands (highs die first) + a few early taps."""
    dur = min(rt60 * 1.1, 3.5)
    n = ns(dur)
    t = np.arange(n) / SR
    tau = rt60 / 6.908
    lo = noise(r, n, lambda f: lpm(f, 700, 2) * hpm(f, 80, 2))
    mi = noise(r, n, lambda f: hpm(f, 700, 2) * lpm(f, 2200, 2))
    hi = noise(r, n, lambda f: hpm(f, 2200, 2) * lpm(f, bright, 2))
    ir = lo * np.exp(-t / tau) + 0.9 * mi * np.exp(-t / (0.8 * tau)) + 0.6 * hi * np.exp(-t / (0.55 * tau))
    ir *= smoothstep(0, 0.012, t)
    peak = np.max(np.abs(ir))
    for _ in range(n_er):
        s = ns(r.uniform(0.003, er_span))
        ir[s:s + 3] += np.array([0.5, 1.0, 0.5]) * r.uniform(0.3, 0.7) * peak * (1 if r.random() < 0.5 else -1)
    ir = np.concatenate([np.zeros(ns(pre)), ir])
    return ir / np.sqrt(np.sum(ir * ir))


def cconv(x, ir):
    """Circular convolution: the reverb tail of the loop end lands on the loop start (seamless)."""
    n = len(x)
    return np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(ir, n), n)


def lconv(x, ir, n_out=None):
    L = len(x) + len(ir) - 1
    nfft = 1 << (L - 1).bit_length()
    y = np.fft.irfft(np.fft.rfft(x, nfft) * np.fft.rfft(ir, nfft), nfft)
    return y[:(n_out if n_out is not None else len(x))]


def room(x, ir, wet):
    return x + wet * lconv(x, ir, len(x))


def add(*parts):
    """Sum of arrays of different lengths (zero-padded to the longest)."""
    y = np.zeros(max(len(q) for q in parts))
    for q in parts:
        y[:len(q)] += q
    return y


def limit_loop(x, ceiling=0.85, width=0.004):
    """Transparent peak safety for loops: a gain dip only around the rare samples above the ceiling, built
    circularly (min-hold over +-width/2, then Hann smoothing of the same width), so the seam stays intact.
    Returns (signal, max reduction dB, % of samples touched)."""
    n = len(x)
    need = np.minimum(1.0, ceiling / np.maximum(np.abs(x), 1e-12))
    hot = np.flatnonzero(need < 1.0)
    if len(hot) == 0:
        return x, 0.0, 0.0
    w = max(3, ns(width))
    g = np.ones(n)
    for i in hot:
        idx = (i - w // 2 + np.arange(w)) % n
        g[idx] = np.minimum(g[idx], need[i])
    k = np.hanning(w + 2)[1:-1]
    k /= k.sum()
    g = np.fft.irfft(np.fft.rfft(g) * np.fft.rfft(np.roll(np.pad(k, (0, n - w)), -(w // 2)), n), n)
    g = np.minimum(g, 1.0)
    return x * g, -db(float(g.min())), float(np.mean(g < 0.999) * 100)


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-15)


# ====================================================================== the radio band (bandcheck.py)
def bq_coeffs(kind, f, q=0.7071):
    w = 2 * math.pi * f / SR
    c = math.cos(w)
    s = math.sin(w)
    al = s / (2 * q)
    a0 = 1 + al
    if kind == 'hp':
        b0 = (1 + c) / 2
        b1 = -(1 + c)
        b2 = (1 + c) / 2
    else:
        b0 = (1 - c) / 2
        b1 = 1 - c
        b2 = (1 - c) / 2
    return (b0 / a0, b1 / a0, b2 / a0, (-2 * c) / a0, (1 - al) / a0)


HPC = bq_coeffs('hp', 450)
LPC = bq_coeffs('lp', 3000)


def _biquad(x, c):
    b0, b1, b2, a1, a2 = c
    y = [0.0] * len(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        o = b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2 = x1
        x1 = v
        y2 = y1
        y1 = o
        y[i] = o
    return y


def band_once(x):
    """Exactly bandcheck.py: HP450 then LP3000 biquads, one pass from silence (a one-shot cue)."""
    return np.array(_biquad(_biquad(np.asarray(x, float).tolist(), HPC), LPC))


def band_resp(n):
    z1 = np.exp(-1j * TAU * np.fft.rfftfreq(n))
    z2 = z1 * z1

    def h(c):
        b0, b1, b2, a1, a2 = c
        return (b0 + b1 * z1 + b2 * z2) / (1 + a1 * z1 + a2 * z2)
    return h(HPC) * h(LPC)


def band_loop(x):
    """Steady state of the same biquads on an endlessly looping clip (exact: periodic input -> DFT bins)."""
    return np.fft.irfft(np.fft.rfft(x) * band_resp(len(x)), len(x))


def band_fast(x):
    pad = ns(0.25)
    return band_loop(np.concatenate([x, np.zeros(pad)]))[:len(x)]


def rms(x):
    return float(np.sqrt(np.mean(np.square(x))))


def st_max(y, win, loop=False):
    """Loudest windowed RMS (win seconds)."""
    n = max(1, ns(win))
    if loop:
        y = np.concatenate([y, y[:n]])
    if len(y) <= n:
        return rms(y)
    c = np.concatenate(([0.0], np.cumsum(y * y)))
    return float(np.sqrt(max(((c[n:] - c[:-n]) / n).max(), 0.0)))


def band_level(x, win=0.05):
    return st_max(band_fast(x), win)


def put(buf, comp, t0, db=0.0, loop=False):
    """Place a component normalised to its in-band loudness (loudest 50 ms after the band) at db."""
    lv = band_level(comp)
    place(buf, comp * (10 ** (db / 20) / max(lv, 1e-12)), t0, loop)


def db(x):
    return 20 * math.log10(max(x, 1e-12))


# ====================================================================== shared sound parts
def beep(f, d, harm=((1, 1.0), (2, 0.25), (3, 0.08)), edge=0.004):
    n = ns(d)
    t = np.arange(n) / SR
    y = sum(a * np.sin(TAU * f * h * t) for h, a in harm)
    return y * env(n, edge, None, edge)


def pawl(r, f=2700.0):
    """Ratchet pawl click: bright steel tick with a lower body tone."""
    y = modal(0.05, [(f * (1 + r.normal(0, 0.01)), 0.0045, 1.0), (f * 1.52, 0.0025, 0.45), (f * 0.48, 0.009, 0.55)])
    y = add(y, 0.5 * burst(r, 0.004, lambda ff: hpm(ff, 1200) * lpm(ff, 6000), 0.0002, 0.0007, 0.001))
    return y


def tick_noise(r, d=0.003, lo=1000, hi=6000):
    return burst(r, d, lambda f: hpm(f, lo) * lpm(f, hi), 0.0002, d * 0.25, d * 0.35)


def contactor(r, strength=1.0, double=True):
    """Relay / contactor: armature hit + contact hit 4-7 ms later; metal ring + small body."""
    def hit(a):
        y = modal(0.12, [(r.uniform(2150, 2300), 0.009, 1.0), (r.uniform(3350, 3500), 0.006, 0.6),
                         (r.uniform(680, 740), 0.018, 0.7), (r.uniform(1250, 1350), 0.012, 0.5)])
        return a * add(y, 0.5 * tick_noise(r, 0.004))
    x = hit(strength)
    if double:
        place(x, hit(0.55 * strength), 0.0045 + r.uniform(0, 0.0025))
    return x


# ====================================================================== beds
BEDS = []


def bed(name, seconds, regions, title):
    def deco(fn):
        BEDS.append(dict(name=name, seconds=seconds, regions=regions, title=title, fn=fn, notes=fn.__doc__))
        return fn
    return deco


@bed('BedServer', 22.0, ['weu', 'ru', 'tutorial'], 'SUNUCU')
def bed_server(r, N):
    """Server hall. Fan airflow: circular noise, gentle -2 dB/oct tilt with a broad 1 kHz hump, rolled off above
    4.2 kHz, slowly breathing (+-10 %). Fan blade-pass whine: five rack fans at 887 / 1183 / 1189 / 1396 / 1402 Hz
    (+2nd harmonic), each with its own slow rpm wander (+-0.15 %), so the close pairs beat at 0.2-6 Hz instead of
    holding a fixed chord. Coil whine: thin 2.63 kHz tone that comes and goes with load (smooth random gate) plus
    a steady faint 1.96 kHz. Disk seeks: irregular clusters of 3-11 tiny 2.6 kHz ticks, 10-45 ms apart, every
    0.5-3 s. UPS double beep: two 110 ms 2.05 kHz beeps 220 ms apart at 3.4 s and a quieter, more distant
    1.77 kHz pair at 14.85 s (different unit, more room), so the beep never repeats identically inside the loop."""
    S = {}
    air = noise(r, N, lambda f: hpm(f, 150) * lpm(f, 4200, 3) * tilt(f, -2.0) * (0.5 + bump(f, 1000, 0.8)))
    S['fan airflow'] = air * (1 + 0.10 * curve(r, N, 2, 10))
    fans = np.zeros(N)
    for f0, a in ((1183.0, 1.0), (1189.4, 0.8), (1396.0, 0.7), (1401.9, 0.6), (887.0, 0.45)):
        ph = phase(f0 * (1 + 0.0015 * curve(r, N, 1, 6)), loop=True)
        fans += a * (1 + 0.3 * curve(r, N, 1, 8)) * (np.sin(TAU * ph) + 0.3 * np.sin(2 * TAU * ph + r.uniform(0, TAU)))
    S['fan blade whine'] = fans
    gate = smoothstep(0.2, 0.75, unit(curve(r, N, 3, 16)))
    S['coil whine'] = (gate * np.sin(TAU * phase(2630 * (1 + 0.003 * curve(r, N, 2, 9)), True))
                       + 0.35 * np.sin(TAU * phase(np.full(N, 1960.0), True)))
    hdd = np.zeros(N)
    tc = 0.4
    while tc < N / SR:
        for _ in range(r.integers(3, 12)):
            tk = add(0.4 * tick_noise(r, 0.004, 1500, 4500), modal(0.006, [(r.uniform(2300, 2900), 0.0012, 1.0)]))
            place(hdd, tk * r.uniform(0.4, 1.0), tc, True)
            tc += r.uniform(0.010, 0.045)
        tc += r.uniform(0.5, 3.0)
    S['disk seeks'] = hdd
    ups = np.zeros(N)
    near = np.zeros(N)
    for t0, f, a, tgt in ((3.40, 2050.0, 1.0, near), (14.85, 1766.0, 0.55, ups)):
        place(tgt, a * beep(f, 0.11), t0, True)
        place(tgt, a * beep(f, 0.11), t0 + 0.22, True)
    ir = reverb_ir(r, 0.8, 4000)
    S['UPS double beep'] = 0.8 * near + 0.35 * cconv(near, ir) + 0.4 * ups + 0.6 * cconv(ups, ir)
    mix = {'fan airflow': (0, 'rms'), 'fan blade whine': (-9, 'rms'), 'coil whine': (-21, 'rms'),
           'disk seeks': (-5, 0.05), 'UPS double beep': (5, 0.1)}
    return S, mix, 'fan airflow'


@bed('BedArchive', 22.0, ['eeu'], 'ARŞİV')
def bed_archive(r, N):
    """Archive stacks, near-silent. Room tone: dark, soft air (-4 dB/oct, rolled off above 2.6 kHz). Fluorescent
    buzz: 100 Hz harmonic series (50 Hz mains) shaped by a ballast resonance around 650 Hz and a weak one at
    2.1 kHz, even harmonics stronger, each harmonic drifting +-12 %; plus a narrow 2.3 kHz tube sizzle modulated at
    100 Hz. Twice per loop (8.6 s, 17.35 s) a failing tube stutters: the buzz dips in five soft pulses over 0.6 s
    with a few starter ticks. Wall clock: one beat per second, alternating tick (1.89 kHz) and tock (1.72 kHz)
    with a wooden 0.33x body tone; the tock comes 35 ms late (a clock slightly out of beat), each beat a little
    different in level; short dull room reverb (0.7 s). 22 beats per 22 s loop keep tick/tock parity across the
    seam."""
    S = {}
    S['room tone'] = noise(r, N, lambda f: hpm(f, 100) * lpm(f, 2600, 3) * tilt(f, -4.0)) * (1 + 0.08 * curve(r, N, 1, 5))
    t = np.arange(N) / SR
    f0 = snap(100.0, N)
    flick = np.ones(N)
    sput = np.zeros(N)
    for tc in (8.6, 17.35):
        n = ns(0.6)
        u = np.arange(n) / n
        dip = 1 - 0.65 * np.sin(np.pi * u) ** 2 * (0.5 + 0.5 * np.cos(TAU * 5 * u))
        flick[(ns(tc) + np.arange(n)) % N] *= dip
        for _ in range(r.integers(3, 6)):
            place(sput, r.uniform(0.4, 1.0) * modal(0.02, [(r.uniform(2200, 2900), 0.002, 1.0)]), tc + r.uniform(0.05, 0.5), True)
    buzz = np.zeros(N)
    for h in range(1, 31):
        fh = f0 * h
        a = (bump(fh, 650, 0.85) + 0.3 * bump(fh, 2100, 0.35)) * (1.0 if h % 2 == 0 else 0.55) * r.uniform(0.55, 1.0)
        buzz += a * (1 + 0.12 * curve(r, N, 1, 6)) * np.sin(TAU * fh * t + r.uniform(0, TAU))
    S['fluorescent buzz'] = buzz * flick
    sizzle = noise(r, N, lambda f: bump(f, 2300, 0.35) * lpm(f, 4000, 4)) * (0.55 + 0.45 * np.sin(np.pi * f0 * t) ** 2)
    S['tube sizzle + starter'] = sizzle * flick + 6.0 * sput
    clk = np.zeros(N)
    for k in range(int(round(N / SR))):
        tock = k % 2 == 1
        f1 = (1720.0 if tock else 1890.0) * (1 + r.normal(0, 0.003))
        tk = modal(0.08, [(f1, 0.010, 1.0), (f1 * 1.67, 0.006, 0.55), (f1 * 0.33, 0.022, 0.6), (f1 * 2.31, 0.003, 0.3)])
        tk = add(tk, 0.4 * tick_noise(r, 0.003, 1500, 6000))
        place(clk, tk * r.uniform(0.88, 1.0), k + 0.25 + (0.035 if tock else 0.0), True)
    S['wall clock'] = clk + 0.35 * cconv(clk, reverb_ir(r, 0.7, 3000))
    mix = {'room tone': (0, 'rms'), 'fluorescent buzz': (-3, 'rms'), 'tube sizzle + starter': (-15, 'rms'),
           'wall clock': (7, 0.03)}
    return S, mix, 'room tone'


@bed('BedRefinery', 21.0, ['me'], 'RAFİNERİ')
def bed_refinery(r, N):
    """Refinery yard. Compressor putt-putt: a slow single-cylinder gas compressor at ~3.6 strokes/s with a slow
    governor wander (+-3.5 %, whole cycles per loop); each firing is a 90 ms exhaust pop (noise through pipe
    resonances at 620 / 1250 Hz + a 78 Hz body), and it is hit-and-miss (fires 90 % after a firing, 70 % after a
    miss) so the rhythm stumbles naturally. Valve tick: a small 2.4-2.5 kHz steel tick in every cycle at 58 % of
    the period, louder or softer at random. Gas hiss: a leaking flange, smooth hiss centred at 2.6 kHz with a
    1.4 kHz shoulder, steeply rolled off above 5 kHz, slowly fluctuating. Sand on louvres: gusts (3-7 per loop)
    drive a Poisson stream of tiny 1.6-3.6 kHz sheet-metal pings (up to ~260/s at the gust peak) over a soft
    gust-shaped wind band."""
    S = {}
    rate = 3.6 * (1 + 0.035 * curve(r, N, 1, 4))
    ph = phase(rate, loop=True)
    cyc = np.floor(ph).astype(np.int64)
    starts = np.concatenate(([0], np.flatnonzero(np.diff(cyc)) + 1))
    putts = np.zeros(N)
    valves = np.zeros(N)
    fired = True
    for i, s in enumerate(starts):
        period = (starts[i + 1] if i + 1 < len(starts) else N) - s
        fired = r.random() < (0.9 if fired else 0.7)
        if fired:
            p = burst(r, 0.09, lambda f: (bump(f, 620, 0.35) + 0.8 * bump(f, 1250, 0.3) + 0.15 * lpm(f, 250, 2)
                                          + 0.25 * bump(f, 2300, 0.4)), 0.004, 0.028, 0.015)
            p += modal(0.09, [(r.uniform(600, 660), 0.03, 0.5), (r.uniform(1220, 1300), 0.02, 0.3), (78, 0.03, 0.25)], 0.003)
            place(putts, p * r.uniform(0.75, 1.0), s / SR, True)
        v = add(modal(0.04, [(r.uniform(2350, 2550), 0.006, 1.0), (r.uniform(3300, 3500), 0.0035, 0.5),
                             (r.uniform(1450, 1600), 0.008, 0.4)], 0.0006), 0.3 * tick_noise(r, 0.002, 1500, 5000))
        place(valves, v * r.uniform(0.6, 1.0), (s + 0.58 * period) / SR, True)
    S['compressor putts'] = putts
    S['valve ticks'] = valves
    S['gas hiss'] = noise(r, N, lambda f: (bump(f, 2600, 0.45) + 0.35 * bump(f, 1400, 0.5)) * lpm(f, 5000, 4)) * (1 + 0.15 * curve(r, N, 2, 10))
    g = unit(curve(r, N, 3, 7, 0.8)) ** 1.5
    grains = np.zeros(N)
    for s in poisson_times(r, 260 * g ** 2 + 4):
        f = 2 ** r.uniform(math.log2(1600), math.log2(3600))
        tau = r.uniform(0.0006, 0.0018)
        place(grains, min(r.exponential(1.0), 2.0) * (0.4 + 0.6 * g[s]) * modal(tau * 6, [(f, tau, 1.0)], 0.0001), s / SR, True)
    wind = noise(r, N, lambda f: bump(f, 700, 0.8) * lpm(f, 3000, 2)) * (0.3 + 0.7 * g)
    S['sand on louvres'] = grains
    S['wind'] = wind
    mix = {'compressor putts': (0, 'rms'), 'valve ticks': (-8, 'rms'), 'gas hiss': (-5, 'rms'),
           'sand on louvres': (-7, 'rms'), 'wind': (-9, 'rms')}
    return S, mix, 'compressor putts'


SHEET = [(1.0, 0.10, 0.45), (1.78, 0.08, 1.0), (2.91, 0.06, 0.8), (4.49, 0.045, 0.6), (6.24, 0.035, 0.45), (8.47, 0.025, 0.3)]


@bed('BedHangar', 23.0, ['af', 'oc'], 'HANGAR')
def bed_hangar(r, N):
    """Open hangar bay. Wind: ten overlapping 1/2-octave noise bands whose gains follow a moving centre
    (430-950 Hz) and a smooth gust envelope (4-8 gusts per loop), so the wind swells and brightens with each gust
    instead of hissing. Bay whistle: a 760 Hz edge tone that only sounds near gust peaks (and a weaker 1.19 kHz one
    at the strongest), gliding +-3 % with the gust and a little random flutter. Corrugated sheet rattle: when a gust
    passes ~0.45 a Poisson stream (up to 22 hits/s) strikes one of three loose sheets (inharmonic modes
    290-2460 Hz, each sheet detuned), 60 % of hits followed by a lighter slap 12-40 ms later (the sheet lifting and
    falling back). Big hall reverb (RT60 2.6 s) on the rattle and lightly on the wind."""
    S = {}
    g = 0.25 + 0.75 * unit(curve(r, N, 2, 8, 1.2))
    centre = 430 * 2 ** (1.15 * g)
    wind = np.zeros(N)
    for fc in 280 * 2 ** (np.arange(10) * 0.42):
        b = noise(r, N, lambda f, fc=fc: bump(f, fc, 0.22))
        wind += b * bump(fc, centre, 0.55) * (0.4 + 0.6 * g)
    ir = reverb_ir(r, 2.6, 3200, pre=0.025, er_span=0.08)
    S['wind'] = 0.9 * wind + 0.3 * cconv(wind, ir)
    wf = 760 * (1 + 0.06 * (g - 0.5)) * (1 + 0.004 * curve(r, N, 20, 80, 0.5))
    wf2 = 1185 * (1 + 0.05 * (g - 0.5)) * (1 + 0.004 * curve(r, N, 20, 80, 0.5))
    S['bay whistle'] = (smoothstep(0.55, 0.95, g) * np.sin(TAU * phase(wf, True))
                        + 0.5 * smoothstep(0.75, 1.0, g) * np.sin(TAU * phase(wf2, True)))
    sheets = [[(f * base * (1 + r.normal(0, 0.015)), tau, a) for f, tau, a in SHEET] for base in (290.0, 328.0, 255.0)]
    rat = np.zeros(N)

    def hit(sh):
        y = modal(0.35, [(f * (1 + r.normal(0, 0.004)), tau, a) for f, tau, a in sh])
        return add(y, 0.6 * burst(r, 0.006, lambda f: bump(f, 1400, 0.8), 0.0003, 0.0015, 0.002))
    for s in poisson_times(r, 22 * smoothstep(0.45, 0.9, g)):
        sh = sheets[r.integers(3)]
        a = (0.35 + 0.65 * r.random()) * g[s]
        place(rat, a * hit(sh), s / SR, True)
        if r.random() < 0.6:
            place(rat, 0.5 * a * hit(sh), s / SR + r.uniform(0.012, 0.04), True)
    S['sheet rattle'] = 0.8 * rat + 0.6 * cconv(rat, ir)
    mix = {'wind': (0, 'rms'), 'bay whistle': (-14, 'rms'), 'sheet rattle': (3, 0.1)}
    return S, mix, 'wind'


@bed('BedLogistics', 22.0, ['na'], 'LOJİSTİK')
def bed_logistics(r, N):
    """Distribution centre. Roller-line clatter: three conveyor lines at different distances (gain 1 / 0.6 /
    0.35, tick rates ~13 / 10.5 / 15.5 per s). Each carton passing over the rollers is a run of 'tok' knocks
    (520-640 Hz carton body + 1.4-1.7 kHz roller tone + 1.7-2.3 kHz bearing click), swelling and fading as it
    passes (1.4-3 s), timing jittered +-4 ms; cartons arrive irregularly and sometimes overlap. Two or three
    carton drops per loop (dull 'bonk' 420-520 Hz). Roller drive whir: narrow noise near 1.1 kHz with a 12.5 Hz
    roller flutter. Ventilation: soft broadband air plus a faint 470 Hz air-handler tone. Warehouse reverb
    RT60 2.0 s."""
    S = {}
    t = np.arange(N) / SR
    roll = np.zeros(N)
    for gain, rate0 in ((1.0, 13.0), (0.6, 10.5), (0.35, 15.5)):
        tc = r.uniform(0, 2)
        while tc < N / SR:
            D = r.uniform(1.4, 3.0)
            R = rate0 * r.uniform(0.92, 1.08)
            k = int(D * R)
            for j in range(k):
                u = (j + 0.5) / k
                f1 = r.uniform(520, 640)
                tk = modal(0.05, [(f1, 0.010, 1.0), (f1 * 2.7, 0.005, 0.5), (r.uniform(1700, 2300), 0.003, 0.45)])
                tk = add(tk, 0.35 * burst(r, 0.006, lambda f: bump(f, 1500, 0.8), 0.0003, 0.0015, 0.002))
                place(roll, gain * np.sin(np.pi * u) ** 1.2 * r.uniform(0.6, 1.0) * tk, tc + j / R + r.normal(0, 0.004), True)
            tc += D * r.uniform(0.5, 1.1) + r.uniform(0.2, 1.4)
    ir = reverb_ir(r, 2.0, 3500, pre=0.02, er_span=0.07)
    S['roller clatter'] = roll + 0.45 * cconv(roll, ir)
    drops = np.zeros(N)
    for t0 in sorted(r.uniform(0, N / SR, r.integers(2, 4))):
        y = modal(0.3, [(r.uniform(180, 240), 0.04, 1.0), (r.uniform(420, 520), 0.03, 0.8), (r.uniform(900, 1100), 0.015, 0.4)])
        y = add(y, 0.6 * burst(r, 0.08, lambda f: lpm(f, 2000, 2) * hpm(f, 200), 0.0005, 0.025, 0.02))
        place(drops, y * r.uniform(0.6, 1.0), t0, True)
    S['carton drops'] = drops + 0.5 * cconv(drops, ir)
    whir = np.zeros(N)
    for fc, fl in ((1100.0, 12.5), (940.0, 10.0), (1280.0, 15.0)):
        whir += noise(r, N, lambda f, fc=fc: bump(f, fc, 0.3)) * (1 + 0.25 * np.sin(TAU * snap(fl, N) * t + r.uniform(0, TAU)))
    S['drive whir'] = whir
    S['ventilation'] = (noise(r, N, lambda f: hpm(f, 120) * lpm(f, 3000, 3) * tilt(f, -3.0)) * (1 + 0.06 * curve(r, N, 1, 6))
                        + 0.12 * np.sin(TAU * snap(470, N) * t) + 0.06 * np.sin(TAU * snap(940, N) * t + 1.0))
    mix = {'ventilation': (0, 'rms'), 'roller clatter': (2, 'rms'), 'carton drops': (4, 0.1), 'drive whir': (-8, 'rms')}
    return S, mix, 'ventilation'


def syllable(f0a, f0b, F1, F2, F3, dur, tiltp):
    """One voiced 'syllable' with no language: harmonic grain whose formants glide between two random targets."""
    n = ns(dur)
    u = np.arange(n) / n
    f0 = f0a + (f0b - f0a) * u
    ph = np.cumsum(f0) / SR
    H = int(min(3800 / max(f0a, f0b), 40))
    K = 8
    uk = np.linspace(0, 1, K)
    f0k = f0a + (f0b - f0a) * uk
    F1k = F1[0] + (F1[1] - F1[0]) * uk
    F2k = F2[0] + (F2[1] - F2[0]) * uk
    h = np.arange(1, H + 1)[:, None]
    fh = h * f0k[None, :]

    def res(f, F, B):
        return 1 / np.sqrt(1 + ((f - F) / (B / 2)) ** 2)
    A = (res(fh, F1k, 90) + 0.55 * res(fh, F2k, 120) + 0.25 * res(fh, F3, 200)) * h ** (-tiltp)
    # hat-function interpolation of the coarse amplitude grid to every sample
    pos = u * (K - 1)
    i0 = np.minimum(pos.astype(int), K - 2)
    w = pos - i0
    Af = A[:, i0] * (1 - w) + A[:, i0 + 1] * w
    x = np.sum(Af * np.sin(TAU * h * ph[None, :]), axis=0)
    e = np.sin(np.pi * u) ** 0.6
    a = min(n // 3, ns(0.02))
    e[:a] *= ramp(a)
    e[n - a:] *= ramp(a)[::-1]
    return x * e


@bed('BedCrowd', 24.0, ['in'], 'KALABALIK')
def bed_crowd(r, N):
    """Call-centre floor. Murmur: 28 synthetic voices (45 % higher, 55 % lower register), each speaking in
    phrases of 3-13 'syllables' (90-260 ms) separated by 0.35-2.4 s pauses, so about half are talking at any time.
    A syllable is a harmonic grain whose two formants glide between random targets (F1 320-780 Hz, F2 900-2300 Hz)
    with phrase declination; there is no vowel table, no phoneme sequence and no recorded speech, so nothing can
    become a word; 25 % of syllables get a soft 2.5-4.5 kHz fricative onset. Voices sit at random distances
    (gain 0.35-1.0, farther = darker), the sum goes through a 1.0 s room and a 3.2 kHz distance roll-off, which
    smears any single voice into babble. Ceiling fan: three blades at 1.25 rev/s give a soft 3.75 Hz air 'whup'
    on a 400-1500 Hz noise band, and a faint 2 kHz squeak once per revolution, varying in level."""
    S = {}
    t = np.arange(N) / SR
    out = np.zeros(N)
    fric = np.zeros(N)
    for _ in range(28):
        high = r.random() < 0.45
        base = r.uniform(175, 235) if high else r.uniform(95, 140)
        gain = r.uniform(0.35, 1.0)
        tiltp = 0.55 + 0.6 * (1 - gain)
        tc = r.uniform(0, N / SR)
        end = tc + N / SR
        while tc < end:
            nsyl = int(r.integers(3, 14))
            pf = base * r.uniform(0.92, 1.12)
            for s in range(nsyl):
                d = r.uniform(0.09, 0.26)
                decl = 1 - 0.12 * s / nsyl
                fa = pf * decl * (1 + r.normal(0, 0.04))
                fb = fa * (1 + r.normal(0, 0.05))
                F1 = (r.uniform(320, 780), r.uniform(320, 780))
                F2 = (r.uniform(900, 2300), r.uniform(900, 2300))
                stress = 1.0 if r.random() < 0.3 else r.uniform(0.5, 0.8)
                place(out, gain * stress * syllable(fa, fb, F1, F2, r.uniform(2400, 3000), d, tiltp), tc, True)
                if r.random() < 0.25:
                    fr = burst(r, r.uniform(0.025, 0.06), lambda f: bump(f, 3400, 0.4), 0.004, 0.02, 0.01)
                    place(fric, 0.12 * gain * fr, tc, True)
                tc += d + r.uniform(0.0, 0.05)
            tc += r.uniform(0.35, 2.4)
    voices = out + fric
    ir = reverb_ir(r, 1.0, 3200, pre=0.015)
    voices = 0.75 * voices + 0.55 * cconv(voices, ir)
    f = np.fft.rfftfreq(N, 1 / SR)
    voices = np.fft.irfft(np.fft.rfft(voices) * lpm(f, 3200, 2), N)
    S['murmur'] = voices
    bp = snap(3.75, N)
    S['ceiling fan'] = noise(r, N, lambda f: bump(f, 800, 0.7) * lpm(f, 2500, 2)) * (0.65 + 0.35 * (0.5 + 0.5 * np.cos(TAU * bp * t)) ** 2)
    sq = np.zeros(N)
    rev = snap(1.25, N)
    for k in range(int(round(N / SR * rev))):
        n = ns(0.03)
        tt = np.arange(n) / SR
        y = np.sin(TAU * (2050 * tt + 1500 * tt * tt)) * env(n, 0.006, None, 0.015)
        place(sq, r.uniform(0.3, 1.0) * y, k / rev + 0.11, True)
    S['fan squeak'] = sq
    mix = {'murmur': (0, 'rms'), 'ceiling fan': (-9, 'rms'), 'fan squeak': (-7, 0.03)}
    return S, mix, 'murmur'


def press_stroke(r, scale=1.0):
    """Stamping press: 'ka-CHUNK' = mid noise slap + die ring (inharmonic) + low body."""
    x = modal(0.9, [(55 * scale, 0.06, 0.5), (r.uniform(330, 350) * scale, 0.05, 0.6), (r.uniform(515, 530) * scale, 0.16, 0.55),
                    (r.uniform(1330, 1360) * scale, 0.11, 0.4), (r.uniform(2190, 2240) * scale, 0.07, 0.3),
                    (r.uniform(2840, 2900) * scale, 0.045, 0.2)])
    x = add(x, 0.9 * burst(r, 0.12, lambda f: bump(f, 700, 0.6) + 0.4 * bump(f, 1800, 0.5), 0.0015, 0.024, 0.02))
    return x


def psst(r, dur, fc=2900.0):
    n = ns(dur)
    x = noise(r, max(n, 4096), lambda f: (bump(f, fc, 0.45) + 0.45 * bump(f, fc * 0.42, 0.5)) * lpm(f, 6000, 4))[:n]
    return x * env(n, 0.012, dur * 0.45, 0.06)


@bed('BedFactory', 22.0, ['cn'], 'FABRİKA')
def bed_factory(r, N):
    """Robot assembly hall. Press A (near): a stamping stroke every 2.75 s (8 per loop): clutch click 130 ms
    before, then 'ka-CHUNK' (mid noise slap 700 Hz/1.8 kHz + die ring 520/1340/2210/2870 Hz detuned per stroke
    + 55 Hz body), then 0.55 s later the pneumatic exhaust 'psst' (2.9 kHz + 1.2 kHz noise, 0.28-0.42 s). Every
    stroke has fresh noise, +-1 dB level and slight detune. Press B (far): every 4.4 s (5 per loop), 0.8x lower in
    pitch, quieter and wetter, so the two rhythms only line up once per loop. Six free pneumatic hisses at random
    times (different centres). Hall tone: soft machine air and two faint servo whines (880/1320 Hz) with slow AM.
    Large hall reverb RT60 2.2 s."""
    S = {}
    t = np.arange(N) / SR
    ir = reverb_ir(r, 2.2, 3400, pre=0.03, er_span=0.09)
    pa = np.zeros(N)
    clutch = np.zeros(N)
    ps = np.zeros(N)
    for k in range(8):
        t0 = 0.6 + k * 2.75 + r.normal(0, 0.006)
        place(pa, press_stroke(r) * r.uniform(0.89, 1.0), t0, True)
        place(clutch, modal(0.03, [(r.uniform(2350, 2500), 0.004, 1.0), (r.uniform(1100, 1200), 0.007, 0.5)]), t0 - 0.13, True)
        place(ps, psst(r, r.uniform(0.28, 0.42)) * r.uniform(0.8, 1.0), t0 + 0.55 + r.uniform(-0.03, 0.03), True)
    S['press A'] = pa + 0.35 * cconv(pa, ir) + 0.25 * clutch
    S['pneumatic psst'] = ps + 0.3 * cconv(ps, ir)
    pb = np.zeros(N)
    for k in range(5):
        place(pb, press_stroke(r, 0.8) * r.uniform(0.8, 1.0), 1.95 + k * (N / SR) / 5 + r.normal(0, 0.01), True)
    S['press B (far)'] = 0.6 * pb + 0.8 * cconv(pb, ir)
    fp = np.zeros(N)
    for t0 in r.uniform(0, N / SR, 6):
        place(fp, psst(r, r.uniform(0.12, 0.22), r.uniform(1900, 3200)) * r.uniform(0.5, 1.0), t0, True)
    S['free pneumatics'] = fp + 0.5 * cconv(fp, ir)
    hall = noise(r, N, lambda f: hpm(f, 100) * lpm(f, 3500, 3) * tilt(f, -3.0)) * (1 + 0.08 * curve(r, N, 1, 6))
    servo = (np.sin(TAU * phase(880 * (1 + 0.002 * curve(r, N, 1, 5)), True)) * (0.6 + 0.4 * curve(r, N, 2, 9))
             + 0.6 * np.sin(TAU * phase(1320 * (1 + 0.002 * curve(r, N, 1, 5)), True)) * (0.6 + 0.4 * curve(r, N, 2, 9)))
    S['hall tone'] = hall
    S['servo whine'] = servo
    mix = {'hall tone': (0, 'rms'), 'press A': (7, 0.1), 'pneumatic psst': (3, 0.1), 'press B (far)': (1, 0.1),
           'free pneumatics': (-2, 0.1), 'servo whine': (-15, 'rms')}
    return S, mix, 'hall tone'


@bed('BedTransformer', 22.0, ['jk'], 'TRAFO')
def bed_transformer(r, N):
    """Chip-plant substation. Transformer buzz: 120 Hz magnetostriction series (60 Hz mains) up to 3 kHz, shaped
    to sit in the band (strong around 840 Hz), every harmonic with its own slow drift; a soft-clipped 120 Hz
    component adds the rasp of a loose lamination. The buzz follows load: six contactor events per loop
    (pull-in 2.3 s, drop-out 6.9 s, pull-in 7.06 s, drop-out 12.6 s, pull-in 17.8 s, drop-out 20.4 s) raise or lower
    it by ~18 % over half a second (net zero, so the loop closes). Contactor tick: pull-in is a double steel click
    (armature + contacts 4-7 ms apart, 2.2/3.4 kHz ring, 700 Hz body), drop-out a single softer one. Clean-room
    airflow: very smooth laminar air (-2.5 dB/oct, no hiss) and faint fan-filter-unit tones at 262/524/786 Hz."""
    S = {}
    t = np.arange(N) / SR
    f0 = snap(120.0, N)
    steps = ((2.3, 1), (6.9, -1), (7.06, 1), (12.6, -1), (17.8, 1), (20.4, -1))
    lvl = np.zeros(N)
    ticks = np.zeros(N)
    for tc, sgn in steps:
        s = ns(tc + 0.02)
        m = ns(0.5)
        tr = np.zeros(N)
        tr[s:] = 1.0
        tr[s:s + m] = ramp(m)
        lvl += sgn * tr
        place(ticks, contactor(r, 1.0 if sgn > 0 else 0.6, double=sgn > 0), tc, True)
    load = 1 + 0.18 * lvl + 0.06 * curve(r, N, 1, 5)
    buzz = np.zeros(N)
    for h in range(1, 26):
        fh = f0 * h
        a = h ** -0.5 * (0.4 + 1.5 * bump(fh, 840, 0.6)) * r.uniform(0.6, 1.0) * (1.0 if h % 2 == 0 else 0.7)
        buzz += a * (1 + 0.2 * curve(r, N, 1, 7)) * load ** (1 + h / 10) * np.sin(TAU * fh * t + r.uniform(0, TAU))
    S['transformer buzz'] = buzz
    fund = np.sin(TAU * f0 * t + 0.3)
    raw = np.tanh(4 * fund)
    S['lamination rasp'] = (raw - fund * np.dot(raw, fund) / np.dot(fund, fund)) * load   # odd harmonics only
    S['clean-room airflow'] = noise(r, N, lambda f: hpm(f, 150) * lpm(f, 5000, 2) * tilt(f, -2.5)) * (1 + 0.05 * curve(r, N, 1, 4))
    S['FFU tones'] = (np.sin(TAU * snap(262, N) * t) + 0.7 * np.sin(TAU * snap(524, N) * t + 1) + 0.5 * np.sin(TAU * snap(786, N) * t + 2)) * (1 + 0.2 * curve(r, N, 1, 6))
    S['contactor ticks'] = ticks + 0.3 * cconv(ticks, reverb_ir(r, 0.9, 4000))
    mix = {'clean-room airflow': (0, 'rms'), 'transformer buzz': (3, 'rms'), 'lamination rasp': (-9, 'rms'),
           'FFU tones': (-14, 'rms'), 'contactor ticks': (6, 0.03)}
    return S, mix, 'clean-room airflow'


@bed('BedWater', 23.0, ['sea', 'sa'], 'SU')
def bed_water(r, N):
    """Flooded basement / plant water. Running water: ~16 000 synthetic bubbles per loop (Minnaert-style: a sine
    with a slight upward chirp and the physical damping law, f 550-2800 Hz log-uniform, bigger = lower = louder),
    Poisson-timed with a slowly varying flow rate (~520-880/s); a soft 1.2 kHz rush band fills between them. No
    flat white noise. Drips: three sources with their own pitch (900 / 1150 / 1480 Hz) and own quasi-period
    (4.6 / 1.7 / 2.9 s, +-10 % jitter); each drip is an upward-chirping 'plink' (x1.6 in 25 ms) with a tiny 4 ms
    splash, into a concrete basement reverb (RT60 1.4 s)."""
    S = {}
    flow = unit(curve(r, N, 1, 6))
    bub = np.zeros(N)
    for s in poisson_times(r, 700 * (0.75 + 0.5 * flow)):
        f0 = 2 ** r.uniform(math.log2(550), math.log2(2800))
        d = 0.043 * f0 + 0.0014 * f0 ** 1.5
        tau = 1 / d
        n = int(5 * tau * SR) + 1
        tt = np.arange(n) / SR
        y = np.sin(TAU * f0 * (tt + 0.05 * d * tt * tt)) * np.exp(-tt / tau)
        k = ns(0.0003)
        y[:k] *= ramp(k)
        place(bub, min(r.exponential(1.0), 4.0) * (1000 / f0) ** 0.6 * y, s / SR, True)
    S['running water'] = bub
    S['rush'] = noise(r, N, lambda f: bump(f, 1200, 0.9) * lpm(f, 4000, 3) * hpm(f, 250)) * (0.85 + 0.3 * flow)
    drips = np.zeros(N)
    for f, period in ((900.0, 4.6), (1150.0, 1.7), (1480.0, 2.9)):
        k = max(1, int(round(N / SR / period)))
        sp = (N / SR) / k
        off = r.uniform(0, sp)
        for j in range(k):
            n = ns(0.15)
            tt = np.arange(n) / SR
            ff = f * (1 + 0.6 * (1 - np.exp(-tt / 0.012))) * (1 + r.normal(0, 0.02))
            y = np.sin(TAU * np.cumsum(ff) / SR) * np.exp(-tt / 0.03)
            y[:ns(0.0005)] *= ramp(ns(0.0005))
            y = add(y, 0.3 * burst(r, 0.004, lambda q: bump(q, 3500, 0.6), 0.0002, 0.001, 0.0015))
            place(drips, r.uniform(0.6, 1.0) * y, off + j * sp + r.uniform(-0.1, 0.1) * sp, True)
    S['drips'] = 0.7 * drips + 0.6 * cconv(drips, reverb_ir(r, 1.4, 3000, pre=0.015))
    mix = {'running water': (0, 'rms'), 'rush': (-6, 'rms'), 'drips': (6, 0.05)}
    return S, mix, 'running water'


# ====================================================================== event cues 67-88
CUES = []


def cue(num, name, uses, jitter):
    """uses: [(EventFx obj kind, call volume, replaced cue, old call volume)], first = primary."""
    def deco(fn):
        CUES.append(dict(num=num, name=name, uses=uses, jitter=jitter, fn=fn, notes=fn.__doc__))
        return fn
    return deco


def buf(d):
    return np.zeros(ns(d))


@cue(67, 'EvCo2Burst', [('bulut', 1.0, 'CueRaidSmoke', 1.0)], 0.03)
def cue_co2burst(r):
    """Two short 1.6 kHz alarm beeps (85 ms, 150 ms apart, soft-edged) then the ceiling nozzle 'pssssht' from
    0.29 s: noise centred at 2.7 kHz sliding to 2.0 kHz with a 950 Hz body, 12 ms attack, exponential decay."""
    x = buf(1.15)
    for t0 in (0.0, 0.15):
        put(x, beep(1600, 0.085), t0, -4)
    d = 0.84
    h = tv_noise(r, d, lambda tc, f: (bump(f, 2700 - 700 * tc / d, 0.55) + 0.5 * bump(f, 950, 0.6)) * lpm(f, 6500, 4) * hpm(f, 250))
    put(x, h * env(len(h), 0.012, 0.32, 0.12, hold=0.03), 0.29, 0)
    return x


@cue(68, 'EvCloudFade', [('bulut_dagil', 0.4, 'CueRaidSmoke', 0.4)], 0.04)
def cue_cloudfade(r):
    """Soft fading hiss: pink-ish noise centred at 1.5 kHz drifting down to 0.8 kHz, 160 ms fade-in, then a slow
    exponential fade (tau 0.33 s). No attack transient."""
    d = 1.1
    x = tv_noise(r, d, lambda tc, f: bump(f, 1500 * 2 ** (-0.9 * tc / d), 0.75) * lpm(f, 4500, 3) * hpm(f, 300))
    return x * env(len(x), 0.16, 0.33, 0.12, hold=0.04)


@cue(69, 'EvShelfRoll', [('raf', 1.0, 'CueRaidPush', 1.0)], 0.04)
def cue_shelfroll(r):
    """Hand-wheel ratchet (four pawl clicks speeding up), then the shelf block rolling 0.17-0.95 s (420/950/
    1900 Hz rumble with a 16 Hz wheel flutter, a 640 Hz wheel whir rising 3 %, three rail-joint knocks), ending in
    a closing 'clonk' at 0.9 s (steel modes 380/874/1462/2236 Hz + 96 Hz body)."""
    x = buf(1.15)
    for k, t0 in enumerate((0.0, 0.075, 0.14, 0.195)):
        put(x, pawl(r, 2650 + 40 * k), t0, -4 - 1.2 * k)
    d = 0.78
    n = ns(d)
    t = np.arange(n) / SR
    rum = noise(r, n, lambda f: bump(f, 420, 0.7) + 0.6 * bump(f, 950, 0.5) + 0.25 * bump(f, 1900, 0.4)) * (1 + 0.35 * np.sin(TAU * 16 * t))
    whir = np.sin(TAU * np.cumsum(640 * (1 + 0.03 * t / d)) / SR)
    roll = (rum / rms(rum) + 0.5 * whir / rms(whir)) * env(n, 0.12, None, 0.2)
    for tj in (0.13, 0.35, 0.55):
        place(roll, 3.0 * modal(0.06, [(r.uniform(500, 560), 0.012, 1.0), (r.uniform(1300, 1400), 0.006, 0.5)]), tj)
    put(x, roll, 0.17, -6)
    clonk = modal(0.24, [(380 * (1 + r.normal(0, 0.01)), 0.07, 1.0), (874, 0.05, 0.75), (1462, 0.035, 0.55), (2236, 0.022, 0.35), (96, 0.05, 0.7)])
    clonk = add(clonk, 0.5 * burst(r, 0.03, lambda f: bump(f, 1300, 0.9), 0.0004, 0.007, 0.01))
    put(x, clonk, 0.9, 0)
    return x


@cue(70, 'EvShelfJam', [('raf_sikisti', 1.0, 'CueRaidHitObject', 1.0)], 0.04)
def cue_shelfjam(r):
    """Five ratchet clicks getting tighter and higher, then a hard stop at 0.30 s: harsh steel clank (612/619 Hz
    beating pair, 1586/2398/3140 Hz) with a noise crack, followed by a three-hit shudder dying away."""
    x = buf(1.0)
    for k, t0 in enumerate((0.0, 0.065, 0.125, 0.18, 0.23)):
        put(x, pawl(r, 2600 + 60 * k), t0, -7 + 0.8 * k)
    modes = [(612, 0.09, 1.0), (619, 0.085, 0.6), (1586, 0.06, 0.75), (2398, 0.04, 0.5), (3140, 0.025, 0.3), (142, 0.04, 0.6)]

    def clank(sc):
        y = modal(0.6, [(f * sc * (1 + r.normal(0, 0.004)), tau, a) for f, tau, a in modes])
        return add(y, 0.4 * burst(r, 0.03, lambda f: hpm(f, 400) * lpm(f, 6000), 0.0006, 0.005, 0.01))
    put(x, clank(1.0), 0.30, 0)
    for k, (dt, g) in enumerate(((0.035, -9), (0.062, -13), (0.085, -17))):
        put(x, clank(1.0 + 0.01 * k), 0.30 + dt, g)
    return x


@cue(71, 'EvGasFill', [('gaz_dol', 1.0, 'CueRaidSmoke', 1.0)], 0.03)
def cue_gasfill(r):
    """Rising gas hiss: noise band sweeping 700 Hz -> 2.4 kHz (with a soft upper partial band) while it swells over
    0.95 s; quick 120 ms release at the end."""
    d = 1.15

    def shape(tc, f):
        c = 700 * 2 ** (1.8 * min(tc / 0.95, 1))
        return (bump(f, c, 0.5) + 0.3 * bump(f, 2 * c, 0.4)) * lpm(f, 6000, 4) * hpm(f, 200)
    x = tv_noise(r, d, shape)
    n = len(x)
    u = np.arange(n) / n
    e = (0.5 - 0.5 * np.cos(np.pi * np.minimum(u / 0.82, 1))) ** 1.2
    return x * e * env(n, 0.001, None, 0.12)


@cue(72, 'EvGasVent', [('gaz_bosal', 0.6, 'CueRaidSmoke', 0.6)], 0.03)
def cue_gasvent(r):
    """Falling hiss: strong onset (20 ms), band sweeping 2.5 kHz -> 0.6 kHz while decaying (tau 0.36 s)."""
    d = 1.1
    x = tv_noise(r, d, lambda tc, f: (bump(f, 2500 * 2 ** (-2.0 * tc / d), 0.55) + 0.25 * bump(f, 5000 * 2 ** (-2.0 * tc / d), 0.4)) * lpm(f, 7000, 4) * hpm(f, 200))
    return x * env(len(x), 0.02, 0.36, 0.1, hold=0.05)


@cue(73, 'EvGasFlash', [('gaz_parla', 1.0, 'CueRaidExplosion', 1.0), ('gaz_alev', 0.6, 'CueRaidDynamite', 0.6)], 0.03)
def cue_gasflash(r):
    """Debris-free 'vuuumf': a 70 ms ignition 'fff' (2.5 kHz), then a fireball roar whose band jumps 380 Hz ->
    1.07 kHz in 90 ms and sinks to ~620 Hz, 45 ms attack, tau 0.24 s decay, with 10-20 Hz flame flutter; a 52 Hz
    body thump underneath. No clicks, no clatter."""
    d = 1.15
    n = ns(d)
    x = np.zeros(n)

    def shape(tc, f):
        c = 380 * 2 ** (1.5 * min(tc / 0.09, 1)) * 2 ** (-0.8 * max(tc - 0.09, 0) / 0.9)
        return (bump(f, c, 0.85) + 0.6 * lpm(f, 260, 2) + 0.2 * bump(f, 2.6 * c, 0.5)) * lpm(f, 5000, 4)
    roar = tv_noise(r, d - 0.03, shape)
    m = len(roar)
    fl = 1 + 0.35 * curve(r, m, 12, 24) * smoothstep(0.05, 0.2, np.arange(m) / SR)
    put(x, roar * env(m, 0.045, 0.24, 0.12, hold=0.03) * fl, 0.03, 0)
    ign = tv_noise(r, 0.07, lambda tc, f: bump(f, 2500, 0.6) * lpm(f, 6000, 4))
    put(x, ign * env(len(ign), 0.03, None, 0.035), 0.0, -9)
    th = modal(0.5, [(52, 0.12, 1.0), (104, 0.07, 0.3)])
    th[:ns(0.01)] *= ramp(ns(0.01))
    place(x, th * 0.6 * np.max(np.abs(x)), 0.03)
    return x


@cue(74, 'EvCeilingCrack', [('catlak', 0.8, 'CueRaidHitObject', 0.8)], 0.05)
def cue_ceilingcrack(r):
    """Creak then crackle. Creak (0-0.55 s): stick-slip pulse train sweeping 38 -> 123 -> 38 pulses/s through a
    beam body (580/1180/1930/2650 Hz). Crackle (0.30-0.95 s): ~30 sharp 1-4 ms cracks with tiny 1.7-3.4 kHz rings,
    thinning out, plus two bigger snaps at 0.33 and 0.60 s."""
    x = buf(1.0)
    d = 0.55
    n = ns(d)
    u = np.arange(n) / n
    rate = 38 + 85 * np.sin(np.pi * u) ** 1.5
    ph = np.cumsum(rate) / SR
    idx = np.flatnonzero(np.diff(np.floor(ph))) + 1
    pulses = np.zeros(n)
    pulses[idx] = (0.5 + 0.5 * r.random(len(idx))) * np.sin(np.pi * u[idx]) ** 0.7
    body = modal(0.05, [(r.uniform(560, 600), 0.011, 1.0), (r.uniform(1150, 1220), 0.008, 0.7), (r.uniform(1880, 1980), 0.005, 0.5), (2650, 0.0035, 0.3)])
    put(x, lconv(pulses, body, n), 0.0, 0)
    cr = np.zeros(ns(0.7))
    tc = 0.0
    while tc < 0.62:
        a = r.uniform(0.3, 1.0) * math.exp(-tc / 0.35)
        y = add(tick_noise(r, r.uniform(0.001, 0.004), 900, 6500), 0.6 * modal(0.012, [(r.uniform(1700, 3400), 0.002, 1.0)]))
        place(cr, a * y, tc)
        tc += r.exponential(1 / 45)
    for t0, g in ((0.03, 2.2), (0.30, 1.6)):
        y = add(modal(0.06, [(r.uniform(850, 950), 0.01, 1.0), (r.uniform(1650, 1800), 0.006, 0.7), (2900, 0.003, 0.4)]), tick_noise(r, 0.004, 600, 7000))
        place(cr, g * y, t0)
    put(x, cr, 0.30, -1)
    return x


@cue(75, 'EvCeilingFall', [('tavan', 1.0, 'CueRaidBreak', 1.0), ('toz', 0.5, 'CueRaidBreak', 0.5)], 0.05)
def cue_ceilingfall(r):
    """Gravel cascade + thud. A concrete chunk lands at 75 ms (66/148 Hz thump + 520/1150 Hz 'whump' noise); a
    Poisson cascade of small stones (up to ~950/s, 1.1-4 kHz pings of 1-4 ms) starts with it and thins out over
    ~0.6 s, with ten bigger 480-1200 Hz pieces; a soft dust hiss trails."""
    d = 1.15
    n = ns(d)
    x = np.zeros(n)
    tt = np.arange(n) / SR
    rate = 950 * smoothstep(0.02, 0.07, tt) * np.exp(-np.maximum(tt - 0.07, 0) / 0.28)
    grav = np.zeros(n)
    for s in poisson_times(r, rate):
        f = 2 ** r.uniform(math.log2(1100), math.log2(4000))
        place(grav, min(r.exponential(0.5), 2.0) * modal(0.02, [(f, r.uniform(0.0012, 0.004), 1.0), (f * 1.7, 0.0012, 0.4)]), s / SR)
    for _ in range(10):
        t0 = r.uniform(0.09, 0.7)
        f = r.uniform(480, 1200)
        place(grav, r.uniform(1.5, 2.5) * math.exp(-(t0 - 0.09) / 0.4) * modal(0.08, [(f, r.uniform(0.01, 0.022), 1.0), (f * 2.3, 0.006, 0.5)]), t0)
    thud = add(modal(0.4, [(66, 0.09, 1.0), (148, 0.05, 0.5)]), 0.8 * burst(r, 0.2, lambda f: bump(f, 520, 0.7) + 0.5 * bump(f, 1150, 0.5), 0.001, 0.045, 0.03))
    dust = tv_noise(r, 1.0, lambda tc, f: bump(f, 1500, 1.0) * lpm(f, 5000, 3)) * env(ns(1.0), 0.05, 0.35, 0.1)
    put(x, grav, 0.0, 0)
    put(x, thud, 0.075, 1)
    put(x, dust, 0.05, -12)
    return x


@cue(76, 'EvLiftMotor', [('kabin_kalk', 1.0, 'CueRaidPush', 1.0)], 0.02)
def cue_liftmotor(r):
    """Freight-lift motor run: a brake-release clack, then the inverter-drive 'singing' tones rising with speed
    (520 -> 1280 Hz plus 1.5x and 2x partials, eased over 0.55 s), a faint fixed 2.48 kHz switching whine, a
    100 Hz motor hum series and rope/sheave whir growing with speed; 60 ms fade-in, 220 ms run-out."""
    d = 1.15
    n = ns(d)
    tt = np.arange(n) / SR
    x = np.zeros(n)
    acc = smoothstep(0.0, 0.55, tt)
    ph = np.cumsum(520 + 760 * acc) / SR
    sing = np.sin(TAU * ph) + 0.55 * np.sin(TAU * 1.5 * ph + 1.0) + 0.3 * np.sin(TAU * 2 * ph + 2.0)
    e = env(n, 0.06, None, 0.22)
    put(x, sing * e, 0, 0)
    put(x, np.sin(TAU * 2480 * tt) * e, 0, -12)
    hum = sum(np.sin(TAU * 100 * h * tt + h) / h for h in range(2, 9))
    put(x, hum * e, 0, -8)
    rope = noise(r, n, lambda f: bump(f, 900, 0.6) * lpm(f, 3500, 2)) * (0.2 + 0.8 * acc)
    put(x, rope * e, 0, -7)
    put(x, contactor(r, 1.0), 0.0, -3)
    return x


@cue(77, 'EvLiftDing', [('kabin_in', 1.0, 'CueRaidPush', 1.0)], 0.0)
def cue_liftding(r):
    """Arrival 'ding': soft-struck chime at 1175 Hz (D6) with 2349/3290 Hz partials and a 587 Hz undertone; a
    second fundamental 2.2 Hz away gives a gentle shimmer. Decay tau 0.42 s, faded at 1.15 s."""
    y = modal(1.15, [(1174.7, 0.42, 1.0), (1176.9, 0.40, 0.35), (2349.3, 0.16, 0.22), (3290, 0.07, 0.10), (587.3, 0.30, 0.10)], 0.0015)
    y = add(y, 0.05 * burst(r, 0.006, lambda f: lpm(f, 3000, 2), 0.0005, 0.0015, 0.002))
    return y


@cue(78, 'EvFreeze', [('donma', 1.0, 'CueRaidSmoke', 1.0)], 0.03)
def cue_freeze(r):
    """Liquid-nitrogen vent: a low vapour 'whuff' (600 Hz) at the start, a bright but rolled-off cold hiss
    (3 kHz + 1.5 kHz bands, cut above 6.5 kHz) and on top a boiling sizzle of ~220 tiny 1.5-3.6 kHz pops per second
    that thins as it settles."""
    d = 1.15
    n = ns(d)
    x = np.zeros(n)
    h = tv_noise(r, d, lambda tc, f: (bump(f, 3000, 0.5) + 0.45 * bump(f, 1500, 0.5)) * lpm(f, 6500, 4))
    put(x, h * env(n, 0.02, 0.9, 0.15, hold=0.05), 0, 0)
    w = tv_noise(r, 0.3, lambda tc, f: bump(f, 600, 0.7))
    put(x, w * env(len(w), 0.01, 0.07, 0.05), 0, -4)
    tt = np.arange(n) / SR
    sz = np.zeros(n)
    for s in poisson_times(r, 220 * env(n, 0.03, 0.6, 0.15)):
        tau = r.uniform(0.0008, 0.002)
        sz_ev = modal(tau * 6, [(r.uniform(1500, 3600), tau, 1.0)], 0.0001)
        place(sz, min(r.exponential(1.0), 3.0) * sz_ev, s / SR)
    put(x, sz, 0, -4)
    return x


@cue(79, 'EvIce', [('buz', 0.7, 'CueRaidArmor', 0.7)], 0.05)
def cue_ice(r):
    """Ice: a first sharp crack (1150/2350/3300 Hz), a run of fourteen smaller high cracks accelerating then
    fading over 0.45 s, then two glassy clinks (2120/2995/4370 Hz + 1260 Hz, 0.48 s and quieter at 0.66 s)."""
    x = buf(0.95)
    put(x, add(modal(0.06, [(1150, 0.012, 1.0), (2350, 0.006, 0.7), (3300, 0.004, 0.5)]), 0.6 * tick_noise(r, 0.004, 700, 6000)), 0.0, -3)
    cr = np.zeros(ns(0.5))
    for k in range(14):
        t0 = 0.03 + 0.42 * (k / 13) ** 1.6 + r.normal(0, 0.006)
        y = add(tick_noise(r, 0.002, 1500, 7000), modal(0.02, [(r.uniform(2000, 3600), r.uniform(0.0015, 0.004), 1.0)]))
        place(cr, r.uniform(0.4, 1.0) * (1 - 0.6 * k / 13) * y, max(t0, 0.0))
    put(x, cr, 0.0, -5)
    clink = [(2120, 0.16, 1.0), (2995, 0.10, 0.45), (4370, 0.05, 0.2), (1260, 0.07, 0.25)]
    put(x, modal(0.45, [(f * (1 + r.normal(0, 0.003)), t, a) for f, t, a in clink]), 0.48, 0)
    put(x, modal(0.28, [(f * 1.04, t, a) for f, t, a in clink]), 0.66, -7)
    return x


@cue(80, 'EvTurnstile', [('turnike', 0.8, 'CueRaidPush', 0.8)], 0.03)
def cue_turnstile(r):
    """Turnstile: three heavy ratchet clicks 100 ms apart (750/1700/2800 Hz + 330 Hz arm body) over a soft arm
    whir, then the shift bell from 0.34 s: a hammer at 24 strikes/s for 0.45 s on a bell with 1080/2506/3070 Hz
    partials (1083.5 Hz double for shimmer), ringing down to 1.15 s."""
    x = buf(1.15)
    for t0 in (0.0, 0.10, 0.20):
        c = modal(0.06, [(r.uniform(730, 770), 0.012, 1.0), (r.uniform(1650, 1750), 0.008, 0.7), (r.uniform(2750, 2850), 0.004, 0.5), (330, 0.015, 0.4)])
        put(x, add(c, 0.5 * tick_noise(r, 0.004)), t0, -3)
    wh = noise(r, ns(0.27), lambda f: bump(f, 1000, 0.6)) * env(ns(0.27), 0.03, None, 0.08)
    put(x, wh, 0.0, -15)
    n = ns(0.81)
    tr = np.zeros(n)
    k = 0
    while k / 24 < 0.45:
        tr[ns(k / 24)] = (0.6 + 0.4 * min(k / 3, 1)) * r.uniform(0.85, 1.0)
        k += 1
    bell = modal(0.8, [(1080, 0.30, 1.0), (1083.5, 0.30, 0.4), (2506, 0.14, 0.45), (3070, 0.08, 0.25)])
    put(x, lconv(tr, bell, n) + 0.15 * lconv(tr, tick_noise(r, 0.003, 1500, 6000), n), 0.34, 0)
    return x


@cue(81, 'EvWeldArc', [('kaynak', 0.8, 'CueRaidEmpBlast', 0.8)], 0.04)
def cue_weldarc(r):
    """Welding arc: a strike pop, then 'frying' arc noise (2.3 kHz + 950 Hz bands) gated by a rectified 60 Hz
    wave (the 120 Hz arc buzz), with 6-20 Hz flutter, faint 120 Hz harmonic tones (480-2400 Hz) and ~80 random
    crackle pops per second; stops with a 70 ms release."""
    d = 1.1
    n = ns(d)
    tt = np.arange(n) / SR
    x = np.zeros(n)
    base = noise(r, n, lambda f: (bump(f, 2300, 0.7) + 0.6 * bump(f, 950, 0.6)) * lpm(f, 5500, 4) * hpm(f, 300))
    arc = base * (0.45 + 0.55 * np.abs(np.sin(TAU * 60 * tt)) ** 3) * (1 + 0.3 * curve(r, n, 6, 22))
    e = env(n, 0.008, None, 0.07)
    put(x, arc * e, 0, 0)
    tones = sum(h ** -0.3 * np.sin(TAU * 120 * h * tt + r.uniform(0, TAU)) for h in range(4, 21))
    put(x, tones * e, 0, -9)
    cr = np.zeros(n)
    for s in poisson_times(r, np.full(n, 80.0)):
        place(cr, min(r.exponential(1.0), 2.0) * tick_noise(r, r.uniform(0.0015, 0.0025), 1200, 4500), s / SR)
    put(x, cr * e, 0, -6)
    put(x, tick_noise(r, 0.008, 400, 5000), 0, -5)
    return x


@cue(82, 'EvArmClank', [('kol', 1.0, 'CueRaidHitObject', 1.0)], 0.04)
def cue_armclank(r):
    """Heavy steel clank: welding-arm boom hitting its stop. Thick steel modes 232/527(+531)/982/1436/2113/
    2786 Hz with long decays (0.30 -> 0.05 s), a noise crack, an 85 Hz body; a lighter rebound clank at 0.17 s."""
    x = buf(1.0)
    modes = [(232, 0.30, 0.9), (527, 0.24, 1.0), (531, 0.24, 0.6), (982, 0.17, 0.75), (1436, 0.12, 0.6), (2113, 0.08, 0.45), (2786, 0.05, 0.3), (85, 0.06, 0.6)]

    def clank(sc, br):
        y = modal(0.95, [(f * sc * (1 + r.normal(0, 0.003)), tau, a * (br if f > 1000 else 1)) for f, tau, a in modes])
        return add(y, 0.8 * burst(r, 0.02, lambda f: hpm(f, 500) * lpm(f, 6000), 0.0003, 0.004, 0.006))
    put(x, clank(1.0, 1.0), 0.0, 0)
    put(x, clank(1.01, 1.3), 0.17, -9)
    return x


@cue(83, 'EvPhase', [('faz', 0.7, 'CueRaidEmpBlast', 0.7), ('kabin_bekle', 0.7, 'CueRaidHitObject', 1.0)], 0.03)
def cue_phase(r):
    """Contactor 'klak': three steel hits 6 and 17 ms apart (1.1/2.3/3.6 kHz ring + 420 Hz body + 150 Hz frame), a
    3 ms contact zip, then a short 120 Hz coil hum (240-1080 Hz harmonics) dying in ~0.3 s."""
    x = buf(0.6)

    def hit(a):
        y = modal(0.2, [(r.uniform(1080, 1140), 0.02, 1.0), (r.uniform(2250, 2350), 0.011, 0.7), (r.uniform(3550, 3650), 0.006, 0.4),
                        (r.uniform(410, 440), 0.03, 0.8), (150, 0.03, 0.5)])
        return a * add(y, 0.6 * tick_noise(r, 0.01, 600, 7000))
    k = hit(1.0)
    place(k, hit(0.6), 0.006)
    place(k, hit(0.25), 0.017)
    put(x, k, 0.0, 0)
    n = ns(0.36)
    tt = np.arange(n) / SR
    hum = sum(np.sin(TAU * 120 * h * tt + h) / h ** 0.5 for h in range(2, 10)) * env(n, 0.01, 0.12, 0.05)
    put(x, hum, 0.012, -9)
    put(x, tick_noise(r, 0.003, 2000, 7000), 0.004, -8)
    return x


def glug(r, f0, d=0.12):
    n = ns(d)
    tt = np.arange(n) / SR
    ph = np.cumsum(f0 * (1 + 0.6 * (1 - np.exp(-tt / 0.03)))) / SR
    y = (np.sin(TAU * ph) + 0.35 * np.sin(2 * TAU * ph)) * np.exp(-tt / r.uniform(0.028, 0.045))
    k = ns(0.002)
    y[:k] *= ramp(k)
    y[n - ns(0.01):] *= ramp(ns(0.01))[::-1]
    return add(y, 0.35 * burst(r, d, lambda f: bump(f, 900, 0.6), 0.004, 0.03, 0.02))


@cue(84, 'EvDrain', [('emme', 1.0, 'CueRaidPush', 1.0)], 0.05)
def cue_drain(r):
    """Floor drain gurgle: eight glugs at irregular gaps (each a 460-760 Hz bubble rising x1.6 with a 2nd harmonic
    and a 900 Hz slosh), a final deeper gulp at 0.98 s, all over a sucking 0.7-1.2 kHz rush."""
    x = buf(1.15)
    s = tv_noise(r, 1.1, lambda tc, f: bump(f, 700 + 500 * math.sin(math.pi * tc / 1.1), 0.6) * lpm(f, 4500, 3))
    put(x, s * env(len(s), 0.15, None, 0.25), 0.0, -8)
    for t0 in (0.06, 0.19, 0.27, 0.41, 0.52, 0.66, 0.78, 0.90):
        put(x, glug(r, r.uniform(460, 760)), t0 + r.normal(0, 0.01), r.uniform(-4, 0))
    put(x, glug(r, 400, 0.15), 0.98, 0)
    return x


@cue(85, 'EvValveWhistle', [('islik', 0.8, 'CueRaidSmoke', 0.8)], 0.02)
def cue_valvewhistle(r):
    """Valve whistle: a 1.85 -> 2.25 kHz whistle rising as pressure builds (0.7 s), 0.5 % random flutter, a
    half-frequency pipe undertone and a narrow breathy noise band riding the whistle pitch; 70 ms in, 160 ms out."""
    d = 1.1
    n = ns(d)
    tt = np.arange(n) / SR
    f = (1850 + 400 * smoothstep(0, 0.7, tt)) * (1 + 0.005 * curve(r, n, 9, 22))
    ph = np.cumsum(f) / SR
    tone = np.sin(TAU * ph) + 0.22 * np.sin(TAU * 0.5 * ph) + 0.06 * np.sin(2 * TAU * ph)
    br = tv_noise(r, d, lambda tc, ff: bump(ff, 1850 + 400 * min(tc / 0.7, 1), 0.12) + 0.25 * bump(ff, 2500, 0.8))
    return (tone / rms(tone) + 0.45 * br / rms(br)) * env(n, 0.07, None, 0.16)


@cue(86, 'EvTubeLaunch', [('tup_uc (launch)', 1.0, 'CueRaidDroneShot', 1.0)], 0.03)
def cue_tubelaunch(r):
    """Cylinder launch: the valve shears ('pang': noise crack + 780/1460/2900/4100 Hz ring), then the jet whoosh:
    band rising 1.3 -> 2.6 kHz in 0.1 s and falling to ~0.75 kHz as it flies off, with a low roar; 25 ms attack,
    tau 0.28 s."""
    d = 0.95
    x = buf(d)
    sn = add(modal(0.12, [(2900, 0.02, 0.6), (1460, 0.03, 0.6), (4100, 0.01, 0.3), (780, 0.025, 0.5)]), tick_noise(r, 0.01, 500, 8000))

    def shape(tc, f):
        c = 1300 * 2 ** (tc / 0.1) if tc < 0.1 else 2600 * 2 ** (-1.8 * (tc - 0.1) / 0.8)
        return (bump(f, c, 0.6) + 0.5 * lpm(f, 700, 2)) * lpm(f, 7000, 4)
    jet = tv_noise(r, d - 0.01, shape)
    put(x, jet * env(len(jet), 0.025, 0.28, 0.1, hold=0.04), 0.01, 0)
    put(x, sn, 0.0, -1)
    return x


@cue(87, 'EvTubeLand', [('tup_uc (landing)', 1.0, 'CueRaidHitObject', 1.0)], 0.04)
def cue_tubeland(r):
    """Cylinder landing: hollow steel-shell ring (812/816.5 Hz beating pair, 1405/2190/2781/3530 Hz) with a
    noise crack and 95 Hz floor thump, a brighter bounce at 0.135 s and a small third touch at 0.235 s; long ring
    (tau 0.4 s) faded at 1.15 s."""
    x = buf(1.15)
    modes = [(812, 0.40, 1.0), (816.5, 0.38, 0.5), (1405, 0.28, 0.7), (2190, 0.19, 0.5), (2781, 0.12, 0.32), (3530, 0.06, 0.15), (95, 0.05, 0.6)]

    def hit(br):
        y = modal(1.1, [(f * (1 + r.normal(0, 0.002)), tau, a * (br if f > 2000 else 1)) for f, tau, a in modes])
        return add(y, 0.6 * burst(r, 0.02, lambda f: hpm(f, 400) * lpm(f, 7000), 0.0003, 0.004, 0.01))
    put(x, hit(1.0), 0.0, 0)
    put(x, hit(1.4), 0.135, -7)
    put(x, hit(1.2), 0.235, -13)
    return x


@cue(88, 'EvOilSlide', [('yag', 0.7, 'CueRaidBreak', 0.7)], 0.04)
def cue_oilslide(r):
    """Hydraulic hose burst: a 'pah' pop (900 Hz body + bright crack + 112 Hz thump), then the oil spray: 2.3 kHz +
    1.05 kHz noise with fast 18-55 Hz spatter modulation, 30 ms rise, tau 0.55 s; small droplet splats land
    0.15-1.05 s."""
    d = 1.15
    n = ns(d)
    x = np.zeros(n)
    pop = burst(r, 0.06, lambda f: bump(f, 900, 0.9) + 0.6 * hpm(f, 2000) * lpm(f, 7000), 0.0003, 0.011, 0.01)
    pop += 0.5 * modal(0.06, [(112, 0.03, 1.0), (235, 0.02, 0.4)])
    sp = tv_noise(r, d - 0.02, lambda tc, f: (bump(f, 2300, 0.65) + 0.55 * bump(f, 1050, 0.55)) * lpm(f, 6500, 4) * hpm(f, 250))
    m = len(sp)
    sp = sp * env(m, 0.03, 0.55, 0.12, hold=0.06) * (1 + 0.45 * curve(r, m, 18, 55))
    put(x, sp, 0.02, 0)
    put(x, pop, 0.0, 1)
    sl = np.zeros(n)
    tt = np.arange(n) / SR
    for s in poisson_times(r, 45 * ((tt > 0.15) & (tt < 1.05))):
        place(sl, r.uniform(0.3, 1.0) * burst(r, 0.012, lambda f: bump(f, r.uniform(1300, 2000), 0.5), 0.0005, 0.003, 0.003), s / SR)
    put(x, sl, 0.0, -8)
    return x


# ====================================================================== I/O and measurement
def write_wav(path, x):
    q = np.clip(np.round(x * 32767), -32768, 32767).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(q.tobytes())


def read_wav(path):
    with wave.open(path, 'rb') as w:
        assert w.getnchannels() == 1 and w.getsampwidth() == 2 and w.getframerate() == SR, path
        return np.frombuffer(w.readframes(w.getnframes()), '<i2').astype(float) / 32768.0


def zcr(x):
    return float(np.mean(np.signbit(x[1:]) != np.signbit(x[:-1])) * SR)


def centroid(x):
    X = np.abs(np.fft.rfft(x * np.hanning(len(x)))) ** 2
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return float((X * f).sum() / max(X.sum(), 1e-20))


def windows_rms(y, win, loop=False):
    n = ns(win)
    k = int(math.ceil(len(y) / n)) if not loop else len(y) // n
    return [rms(y[i * n:(i + 1) * n]) for i in range(k) if len(y[i * n:(i + 1) * n])]


def seam_check(x):
    """Loop seam measured on the written samples (wrap from the last sample to the first)."""
    N = len(x)
    xc = np.concatenate([x[-2:], x, x[:2]])
    d1 = np.abs(np.diff(np.concatenate([x, x[:1]])))           # last entry is the seam step x[0]-x[N-1]
    d2 = np.abs(xc[2:] - 2 * xc[1:-1] + xc[:-2])                # second difference at every sample, circular
    m6 = np.max(np.stack([np.roll(d2, -k) for k in range(6)]), axis=0)   # max over 6 consecutive samples
    d2_seam = m6[N - 3]                                         # the 6 samples straddling the wrap
    d1_seam = d1[-1]
    rank1 = float(np.mean(d1 < d1_seam) * 100)
    rank2 = float(np.mean(m6 < d2_seam) * 100)
    # local high-frequency energy (>6 kHz) in a 1024-sample window centred on the seam vs every other window
    W = 1024
    win = np.hanning(W)
    f = np.fft.rfftfreq(W, 1 / SR)
    hf = f > 6000
    starts = np.arange(0, N, 512)
    ext = np.concatenate([x, x[:W]])
    e = np.array([np.sum(np.abs(np.fft.rfft(ext[s:s + W] * win))[hf] ** 2) for s in starts])
    seam_win = np.concatenate([x[-W // 2:], x[:W // 2]])
    es = np.sum(np.abs(np.fft.rfft(seam_win * win))[hf] ** 2)
    hf_ratio = float(es / max(np.median(e), 1e-20))
    hf_rank = float(np.mean(e < es) * 100)
    # whole-spectrum distance of the seam window to the average spectrum
    spec = np.array([np.abs(np.fft.rfft(ext[s:s + W] * win)) ** 2 for s in starts[::4]]).mean(axis=0)
    ss = np.abs(np.fft.rfft(seam_win * win)) ** 2
    band = (f > 100) & (f < 12000)
    dist = float(np.mean(np.abs(10 * np.log10((ss[band] + 1e-12) / (spec[band] + 1e-12)))))
    typ = np.array([np.mean(np.abs(10 * np.log10((np.abs(np.fft.rfft(ext[s:s + W] * win)) ** 2 + 1e-12)[band] / (spec[band] + 1e-12))))
                    for s in starts[1::3]])
    dist_rank = float(np.mean(typ < dist) * 100)
    ok = rank1 < 99.9 and rank2 < 99.9 and hf_rank < 99.0 and dist_rank < 99.0
    return dict(step=d1_seam, p999=float(np.percentile(d1, 99.9)), rank1=rank1, rank2=rank2, hf_ratio=hf_ratio,
                hf_rank=hf_rank, dist=dist, dist_typ=float(np.median(typ)), dist_rank=dist_rank, ok=ok)


def env_autocorr(yb, lo, hi, detrend):
    """Envelope self-similarity: 50 ms band-RMS frames, circular autocorrelation, highest local peak in
    [lo, hi] s. detrend removes the 1 s moving average first, so slow swells do not count as rhythm."""
    fr = ns(0.05)
    k = len(yb) // fr
    e = np.sqrt(np.mean(yb[:k * fr].reshape(k, fr) ** 2, axis=1))
    if detrend:
        ker = np.zeros(k)
        ker[:20] = 1 / 20
        ker = np.roll(ker, -10)
        e = e - np.fft.irfft(np.fft.rfft(e) * np.fft.rfft(ker), k)
    e = e - e.mean()
    E = np.fft.rfft(e)
    ac = np.fft.irfft(E * np.conj(E), k)
    ac = ac / ac[0]
    lags = np.arange(k) * 0.05
    peak = (ac > np.roll(ac, 1)) & (ac >= np.roll(ac, -1)) & (lags >= lo) & (lags <= hi)
    if not peak.any():
        return 0.0, 0.0
    i = np.argmax(np.where(peak, ac, -9))
    return float(ac[i]), float(lags[i])


def describe(x, loop):
    yb = band_loop(x) if loop else band_once(x)
    d = dict(dur=len(x) / SR, peak=float(np.max(np.abs(x))), rms=rms(x), brms=rms(yb), bpeak=float(np.max(np.abs(yb))),
             st100=st_max(yb, 0.1, loop), st300=st_max(yb, 0.3, loop), zcr=zcr(x), zcr_b=zcr(yb),
             cent=centroid(x), cent_b=centroid(yb))
    d['loss'] = db(d['brms'] / d['rms'])
    return d, yb


# ====================================================================== main
OLD_BANK = {'CueRaidSmoke': 0.55, 'CueRaidPush': 0.5, 'CueRaidHitObject': 0.6, 'CueRaidDynamite': 0.8,
            'CueRaidExplosion': 0.8, 'CueRaidBreak': 0.6, 'CueRaidArmor': 0.5, 'CueRaidEmpBlast': 0.8,
            'CueRaidDroneShot': 0.45, 'CueRaidCoreBreak': 0.9, 'CueRadioReport': 0.4, 'LoopRaidAmbience': 0.45}


def main():
    os.makedirs(OUT, exist_ok=True)
    refs = {}
    for name in OLD_BANK:
        x = read_wav(os.path.join(REF_DIR, name + '.wav'))
        refs[name], _ = describe(x, name.startswith('Loop'))
        if name.startswith('Loop'):
            refs[name]['brms_once'] = rms(band_once(x))
    ex = refs['CueRaidExplosion']
    cb = refs['CueRaidCoreBreak']
    # caps on the effective (x bank volume x call volume) in-band loudness: 0.9 x RaidExplosion at call 1
    CAP300 = 0.9 * ex['st300'] * OLD_BANK['CueRaidExplosion']
    CAP100 = 0.9 * ex['st100'] * OLD_BANK['CueRaidExplosion']
    # peak sanity: no new cue spikier than today's RaidHitObject, the clip most of these events played until now
    ho = refs['CueRaidHitObject']
    CAPPK = 0.9 * max(ho['bpeak'] * OLD_BANK['CueRaidHitObject'], cb['bpeak'] * OLD_BANK['CueRaidCoreBreak'])

    bed_rows = []
    for b in BEDS:
        N = ns(b['seconds'])
        r = rng(b['name'])
        stems, mix, ref = b['fn'](r, N)
        bands = {k: band_loop(v) for k, v in stems.items()}
        ref_level = rms(bands[ref])
        out = np.zeros(N)
        scaled = {}
        for k, (g_db, metric) in mix.items():
            lv = rms(bands[k]) if metric == 'rms' else st_max(bands[k], metric, True)
            gk = ref_level * 10 ** (g_db / 20) / max(lv, 1e-15)
            scaled[k] = gk
            out += gk * stems[k]
        g = BED_BAND_RMS / rms(band_loop(out))
        out *= g
        peak_before = float(np.max(np.abs(out)))
        out, lim_db, lim_pct = limit_loop(out, 0.85)
        g2 = BED_BAND_RMS / rms(band_loop(out))
        out *= g2
        g *= g2
        if np.max(np.abs(out)) > FILE_PEAK_MAX:
            raise SystemExit('%s peak %.3f > %.2f' % (b['name'], np.max(np.abs(out)), FILE_PEAK_MAX))
        path = os.path.join(OUT, b['name'] + '.wav')
        write_wav(path, out)
        xf = read_wav(path)
        d, yb = describe(xf, True)
        d['brms_once'] = rms(band_once(xf))
        d['limiter'] = (peak_before, lim_db, lim_pct)
        d['seam'] = seam_check(xf)
        d['seam_band'] = seam_check(yb)
        d['per_s'] = windows_rms(yb, 1.0, True)
        d['ac_rhythm'] = env_autocorr(yb, 0.15, 3.0, True)
        d['ac_macro'] = env_autocorr(yb, 3.0, b['seconds'] / 2, False)
        mixband = rms(yb)
        d['stems'] = [(k, db(rms(bands[k]) * scaled[k] * g / mixband), mix[k]) for k in mix]
        b['d'] = d
        bed_rows.append(b)
        print('%-15s %.2fs peak %.3f rms %.3f band %.4f (once %.4f) loss %.1f dB seam %s' % (
            b['name'], d['dur'], d['peak'], d['rms'], d['brms'], d['brms_once'], d['loss'], 'ok' if d['seam']['ok'] else 'FAIL'))

    for c in CUES:
        r = rng(c['name'])
        x = c['fn'](r)
        if len(x) > ns(1.2):
            raise SystemExit(c['name'] + ' too long')
        a = ns(0.0005)
        x[:a] *= ramp(a)
        rl = ns(0.025)
        x[-rl:] *= ramp(rl)[::-1]
        raw, _ = describe(x, False)
        uses = c['uses']
        p_obj, p_call, p_old, p_oldcall = uses[0]
        max_call = max(u[1] for u in uses)
        old = refs[p_old]
        old_eff = old['st300'] * OLD_BANK[p_old] * p_oldcall          # today's effective in-band loudness
        K = min(old_eff / p_call, CAP300 / max_call)                   # wanted st300 x bank volume
        lim = {'matched to today' if K * max_call < CAP300 * 0.999 else 'Explosion st300 cap': K / raw['st300'],
               'Explosion st100 cap': CAP100 / (raw['st100'] * max_call), 'peak sanity cap': CAPPK / (raw['bpeak'] * max_call)}
        binding = min(lim, key=lim.get)
        E = lim[binding]
        vol = OLD_BANK[p_old]
        gain = E / vol
        if gain * raw['peak'] > FILE_PEAK_MAX:
            gain = FILE_PEAK_MAX / raw['peak']
        elif gain * raw['peak'] < 0.3:
            gain = 0.3 / raw['peak']
        vol = round(E / gain, 2)
        if vol > 1.0:
            vol = 1.0
            binding += ' + file peak 0.9 at bank volume 1'
        gain = E / vol
        if gain * raw['peak'] > FILE_PEAK_MAX:
            gain = FILE_PEAK_MAX / raw['peak']
        x = x * gain
        path = os.path.join(OUT, 'Cue%s.wav' % c['name'])
        write_wav(path, x)
        xf = read_wav(path)
        d, yb = describe(xf, False)
        d['vol'] = vol
        d['binding'] = binding
        d['max_call'] = max_call
        d['eff300'] = d['st300'] * vol * max_call
        d['eff100'] = d['st100'] * vol * max_call
        d['effpk'] = d['bpeak'] * vol * max_call
        d['old'] = old
        d['old_name'] = p_old
        d['old_eff'] = old_eff
        d['new_eff_primary'] = d['st300'] * vol * p_call
        d['first'] = float(xf[0])
        d['last'] = float(xf[-1])
        d['edge_rms'] = rms(xf[-ns(0.005):])
        d['per100'] = windows_rms(yb, 0.1)
        d['cap_ok'] = d['eff300'] <= CAP300 / 0.9 + 1e-6 and d['eff100'] <= CAP100 / 0.9 + 1e-6 and d['effpk'] <= CAPPK / 0.9 + 1e-6
        c['d'] = d
        print('%d %-15s %.2fs peak %.3f band rms %.3f (old %s %.3f) loss %.1f vol %.2f eff300 %.4f (cap %.4f) %s' % (
            c['num'], c['name'], d['dur'], d['peak'], d['brms'], p_old, old['brms'], d['loss'], vol, d['eff300'], CAP300 / 0.9,
            'ok' if d['cap_ok'] else 'OVER') + '  [' + binding + ']')

    write_report(bed_rows, CUES, refs, CAP300, CAP100, CAPPK)


def write_report(beds, cues, refs, CAP300, CAP100, CAPPK):
    L = []
    w = L.append
    w('# Placeholder audio for raid places: layers 7 (region beds) and 8 (event cues)')
    w('')
    w('Generated by `gen.py` (numpy %s, seed %d, one independent random stream per clip so the output does not depend '
      'on generation order). All files: mono, 44.1 kHz, 16-bit PCM, in `out/`. Nothing was downloaded and nothing '
      'is a recording: the crowd murmur, the running water and every other layer are synthesised. The design doc '
      'wanted CC0 recordings for the crowd (and, in the later detail round, rain and the ship horn); the crowd bed '
      'here is a synthetic placeholder for that recording.' % (np.__version__, SEED))
    w('')
    w('Band = biquad HP 450 Hz then LP 3000 Hz, Q 0.7071 (the formula in `bandcheck.py`). For cues the band is run once '
      'from silence exactly like `bandcheck.py`. For beds the "band RMS" is the steady state of the same biquads on the '
      'endlessly looping clip (computed exactly through the DFT; the one-pass `bandcheck.py` figure is listed next to '
      'it and agrees to 4 decimals). Unity\'s filters run at Q 1, which adds roughly +1 dB right at the two corners; '
      'nothing here relies on energy at the corners.')
    w('')
    w('Terms: **st100 / st300** = the loudest 100 ms / 300 ms window RMS after the band (a short-term loudness '
      'figure; whole-clip RMS of a one-shot depends on how much silent tail it has). **eff** = that value x bank '
      'volume x call volume (the common `sfxVolume` 0.8 and the Effects/Ambience sliders are left out on both sides).')
    w('')
    # ---------------------------------------------------------------- comparison
    w('## 1. Comparison with today\'s clips')
    w('')
    w('| Clip | dur s | peak | RMS | band RMS | loss dB | band st300 | band peak | bank vol | eff RMS (x vol) | eff st300 |')
    w('|---|---|---|---|---|---|---|---|---|---|---|')
    for k in ('LoopRaidAmbience', 'CueRadioReport', 'CueRaidSmoke', 'CueRaidExplosion', 'CueRaidCoreBreak'):
        d = refs[k]
        v = OLD_BANK[k]
        w('| today %s | %.2f | %.3f | %.3f | %.3f | %.1f | %.3f | %.3f | %.2f | %.4f | %.4f |' % (
            k, d['dur'], d['peak'], d['rms'], d['brms'], d['loss'], d['st300'], d['bpeak'], v, d['brms'] * v, d['st300'] * v))
    for b in beds:
        d = b['d']
        w('| new %s | %.2f | %.3f | %.3f | %.4f | %.1f | %.3f | %.3f | %.2f | %.4f | %.4f |' % (
            b['name'], d['dur'], d['peak'], d['rms'], d['brms'], d['loss'], d['st300'], d['bpeak'], BED_VOLUME, d['brms'] * BED_VOLUME, d['st300'] * BED_VOLUME))
    for c in cues:
        d = c['d']
        w('| new Cue%s | %.2f | %.3f | %.3f | %.3f | %.1f | %.3f | %.3f | %.2f | %.4f | %.4f |' % (
            c['name'], d['dur'], d['peak'], d['rms'], d['brms'], d['loss'], d['st300'], d['bpeak'], d['vol'], d['brms'] * d['vol'], d['st300'] * d['vol']))
    w('')
    rr = refs['CueRadioReport']
    bed_eff = BED_BAND_RMS * BED_VOLUME
    rep = rr['brms'] * OLD_BANK['CueRadioReport']
    w('Reading the table: every bed sits at today\'s hum level after the band (band RMS 0.078, eff %.4f at volume '
      '0.45). The spotter\'s `RadioReport` (never banded; band RMS used for a like-for-like figure) is eff %.4f x call '
      '0.7 (attack/spawn) = %.4f and x 0.5 (event) = %.4f, so the beds sit %.1f-%.1f dB above the spotter in RMS terms, '
      'exactly as today\'s hum does. The conflict the audio map notes between "same level as today" and "beds 10 dB '
      'under signal sounds" is therefore unchanged; the spec fixed the bed level. Peaks: today\'s hum already peaks '
      'above the spotter after the band (eff %.3f vs %.3f at call 0.7); the new beds peak at eff %.3f-%.3f, highest '
      'in the transient beds (%s), so a clock tick or a press hit can briefly poke above a spotter line. Worth a '
      'listen in Play; lowering that bed\'s transient layer in `gen.py` is a one-number change.' % (
          bed_eff, rep, rep * 0.7, rep * 0.5, db(bed_eff / (rep * 0.7)), db(bed_eff / (rep * 0.5)),
          refs['LoopRaidAmbience']['bpeak'] * BED_VOLUME, rr['bpeak'] * OLD_BANK['CueRadioReport'] * 0.7,
          min(b_['d']['bpeak'] for b_ in beds) * BED_VOLUME, max(b_['d']['bpeak'] for b_ in beds) * BED_VOLUME,
          ', '.join('%s %.3f' % (b_['name'], b_['d']['bpeak'] * BED_VOLUME) for b_ in sorted(beds, key=lambda q: -q['d']['bpeak'])[:3])))
    w('')
    w('Loudness cap for cues ("never louder than RaidExplosion / RaidCoreBreak after the band"): loudness is read as '
      'short-term RMS after the band, not as sample peak. Every cue at its loudest call volume stays at eff st300 <= '
      '%.4f and eff st100 <= %.4f, i.e. 0.9 (-0.9 dB, margin for Unity\'s Q 1 corners) x `RaidExplosion` at bank 0.8, '
      'call 1 (%.4f / %.4f). `RaidCoreBreak` is louder still (eff st300 %.4f, st100 %.4f), so every cue is under both. '
      'Peaks: Explosion\'s eff band peak is only %.3f and CoreBreak\'s %.3f because their energy is bass; a steel clank '
      'or a crack cannot reach today\'s level under such a peak cap (it cost 5-10 dB in a first pass), so the peak '
      'limit used is a sanity cap instead: no cue spikier than today\'s `RaidHitObject` (eff band peak %.3f, the clip '
      'most of these events played until now) with the same 0.9 margin = %.3f.' % (
          CAP300, CAP100, CAP300 / 0.9, CAP100 / 0.9, refs['CueRaidCoreBreak']['st300'] * 0.9, refs['CueRaidCoreBreak']['st100'] * 0.9,
          refs['CueRaidExplosion']['bpeak'] * 0.8, refs['CueRaidCoreBreak']['bpeak'] * 0.9,
          refs['CueRaidHitObject']['bpeak'] * 0.6, CAPPK))
    w('')
    # ---------------------------------------------------------------- beds
    w('## 2. Beds (layer 7)')
    w('')
    w('| File | regions | dur s | peak | RMS | band RMS (loop) | band RMS (bandcheck one pass) | loss dB | band st100 | seam |')
    w('|---|---|---|---|---|---|---|---|---|---|')
    for b in beds:
        d = b['d']
        s = d['seam']
        w('| %s.wav | %s | %.2f | %.3f | %.3f | %.4f | %.4f | %.1f | %.3f | %s |' % (
            b['name'], ', '.join(b['regions']), d['dur'], d['peak'], d['rms'], d['brms'], d['brms_once'], d['loss'], d['st100'],
            'ok' if s['ok'] and d['seam_band']['ok'] else 'CHECK'))
    w('')
    w('**Loop seam check** (on the written 16-bit samples, raw file and after the band): the step from the last '
      'sample to the first, ranked among all %s sample-to-sample steps of the clip; the same for the second difference '
      '(a click shows as a curvature spike); and the energy above 6 kHz in a 1024-sample window centred on the wrap '
      'against every other window (a click is broadband, so it shows there first); and the mean dB distance of the '
      'seam window\'s spectrum from the clip\'s average spectrum, ranked against the same distance for other windows. '
      'Pass = step and curvature (max of the 6 samples straddling the wrap, ranked against the max of every 6-sample '
      'run) below the 99.9th percentile, HF energy and spectral distance below the 99th percentile. Ranks near 50 %% '
      'mean "an ordinary moment of the clip"; the HF ratio to the median is high for gusty or busy beds whenever a '
      'gust or a voice happens to sit on the wrap, which the rank shows is ordinary. Every bed is built circularly, so the seam is '
      'indistinguishable from any other point of the loop.' % 'N')
    w('')
    w('| File | seam step | p99.9 step | step rank % | curvature rank % | HF rank % (ratio to median) | spectral distance dB, rank % (median of windows) | after band: step / curvature rank % |')
    w('|---|---|---|---|---|---|---|---|')
    for b in beds:
        s = b['d']['seam']
        sb = b['d']['seam_band']
        w('| %s | %.5f | %.5f | %.1f | %.1f | %.0f (%.2f) | %.2f, %.0f (%.2f) | %.1f / %.1f |' % (
            b['name'], s['step'], s['p999'], s['rank1'], s['rank2'], s['hf_rank'], s['hf_ratio'], s['dist'], s['dist_rank'], s['dist_typ'], sb['rank1'], sb['rank2']))
    w('')
    for b in beds:
        d = b['d']
        w('### %s (%s; %s)' % (b['name'], b['title'], ', '.join(b['regions'])))
        w('')
        w(' '.join(b['notes'].split()))
        w('')
        w('- Layers after the band, dB relative to the whole bed: ' + '; '.join(
            '%s %+.1f' % (k, v) for k, v, _ in d['stems']) + '.')
        w('- Band RMS per second: `' + ' '.join('%.3f' % v for v in d['per_s']) + '` (min %.3f, max %.3f).' % (min(d['per_s']), max(d['per_s'])))
        w('- Peak %.3f (band %.3f), crest after band %.1f dB; zero-crossings %.0f/s (band %.0f/s); spectral centroid %.0f Hz (band %.0f Hz).' % (
            d['peak'], d['bpeak'], db(d['bpeak'] / d['brms']), d['zcr'], d['zcr_b'], d['cent'], d['cent_b']))
        pb, ldb, lpc = d['limiter']
        w('- Peak safety: mix peak before the safety limiter %.3f; %s.' % (pb, 'limiter idle' if ldb == 0 else
          'limiter took at most %.1f dB off %.2f %% of the samples (a few ms around the hottest transients)' % (ldb, lpc)))
        w('- Envelope self-similarity (50 ms band-RMS frames, circular): strongest rhythm (1 s trend removed, 0.15-3 s) r = %.2f at %.2f s; strongest longer repeat (3 s to half the loop) r = %.2f at %.2f s.' % (
            d['ac_rhythm'][0], d['ac_rhythm'][1], d['ac_macro'][0], d['ac_macro'][1]))
        w('')
    # ---------------------------------------------------------------- cues
    w('## 3. Event cues (layer 8)')
    w('')
    w('Levelling: each cue is matched to the cue it replaces on eff st300 at the same call volume (`RaidBoard3D.EventFx` '
      'volumes kept, except the ones the spec sets: gaz_alev .6, gaz_parla 1, toz .5, kabin_bekle .7), then capped so '
      'that at its loudest call volume it stays under `RaidExplosion` (cap above). The bank volume starts at the '
      'replaced cue\'s bank volume, so the file\'s band RMS stays comparable to the old file; it moves only when the file '
      'would otherwise peak above 0.9 or below 0.3.')
    w('')
    w('| # | File | dur s | peak | RMS | band RMS | old band RMS | dB vs old | loss dB | band st300 | band peak | start / end sample | replaces |')
    w('|---|---|---|---|---|---|---|---|---|---|---|---|---|')
    for c in cues:
        d = c['d']
        w('| %d | Cue%s.wav | %.2f | %.3f | %.3f | %.3f | %.3f | %+.1f | %.1f | %.3f | %.3f | %.4f / %.4f | %s |' % (
            c['num'], c['name'], d['dur'], d['peak'], d['rms'], d['brms'], d['old']['brms'], db(d['brms'] / d['old']['brms']), d['loss'],
            d['st300'], d['bpeak'], d['first'], d['last'], ', '.join('%s %s@%.1f' % (u[0], u[2][3:], u[3]) for u in c['uses'])))
    w('')
    w('| # | Cue | bank volume | pitch jitter | call volumes | eff st300 new | eff st300 today (same call) | dB vs today | eff st100 / cap | eff band peak / cap | under Explosion |')
    w('|---|---|---|---|---|---|---|---|---|---|---|')
    for c in cues:
        d = c['d']
        w('| %d | %s | %.2f | %.2f | %s | %.4f | %.4f | %+.1f | %.4f / %.4f | %.3f / %.3f | %s |' % (
            c['num'], c['name'], d['vol'], c['jitter'], ', '.join('%s %.1f' % (u[0], u[1]) for u in c['uses']),
            d['new_eff_primary'], d['old_eff'], db(d['new_eff_primary'] / d['old_eff']), d['eff100'], CAP100 / 0.9, d['effpk'], CAPPK / 0.9,
            'yes' if d['cap_ok'] else 'NO'))
    w('')
    for c in cues:
        d = c['d']
        w('- **%d %s** (%.2f s): %s Band RMS per 100 ms: `%s`; zero-crossings %.0f/s; band centroid %.0f Hz.' % (
            c['num'], c['name'], d['dur'], ' '.join(c['notes'].split()), ' '.join('%.3f' % v for v in d['per100']), d['zcr'], d['cent_b']))
    w('')
    # ---------------------------------------------------------------- bank
    w('## 4. Recommended SoundBank values')
    w('')
    w('Beds (`RaidSite`): every bed at `bedVolume` %.2f (today\'s `LoopRaidAmbience` volume); the files already carry '
      'today\'s in-band level, so equal volumes give equal loudness across regions. If the archive reads too busy for '
      '"near-silent" when auditioned, 0.32 (-3 dB) is the first thing to try; that is a taste call for the owner.' % BED_VOLUME)
    w('')
    w('| region | bed clip | bedVolume |')
    w('|---|---|---|')
    order = ['weu', 'eeu', 'me', 'af', 'na', 'ru', 'in', 'cn', 'jk', 'sea', 'sa', 'oc']
    reg = {}
    for b in beds:
        for g in b['regions']:
            reg[g] = b['name']
    for g in order:
        w('| %s | %s | %.2f |' % (g, reg[g], BED_VOLUME))
    w('')
    w('Tutorial (region weu, seed 129) gets BedServer through its region; no extra entry needed.')
    w('')
    w('Cues (`CueClip`):')
    w('')
    w('| cue | clip | volume | pitchJitter |')
    w('|---|---|---|---|')
    for c in cues:
        w('| %d %s | Cue%s.wav | %.2f | %.2f |' % (c['num'], c['name'], c['name'], c['d']['vol'], c['jitter']))
    w('')
    w('EventFx call volumes to use (unchanged unless noted): ' + '; '.join(
        '%s -> %s %.1f' % (u[0], c['name'], u[1]) for c in cues for u in c['uses']) + '.')
    w('')
    w('Import: beds CompressedInMemory, Vorbis 0.7, loadInBackground; cues DecompressOnLoad, PCM (as the spec says).')
    w('Vorbis can add a few samples of encoder padding at a loop point; if a faint tick appears at the wrap in Unity, '
      'switch that bed to PCM or Vorbis quality 1 rather than editing the file.')
    w('')
    with open(os.path.join(HERE, 'report.md'), 'w') as fh:
        fh.write('\n'.join(L) + '\n')


if __name__ == '__main__':
    main()
