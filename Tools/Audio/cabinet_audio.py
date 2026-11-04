#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Corrigible (Son Şalter) - ayarlar dolabı ve menü sahnesinin yer tutucu sesleri (Docs/AYARLAR-DOLABI-VE-MENU.md).

Tek seferlik, tohumlu ve deterministik sentez; yalnız Python standart kitaplığı (numpy gerekmez). Mono 44,1 kHz 16 bit WAV'ları
doğrudan Assets/Corrigible/Audio/Placeholder altına yazar (Cue*.wav; SoundBank.asset'teki girişler bu adlara bağlı):

  CueCabinetOpen     mandalın “tık”ı, menteşenin kısa gıcırtısı
  CueCabinetClose    kanadın tok “güm”ü, mandal
  CueFaderDetent     sürgünün çentiği (kısa tık)
  CueSwitchClack     yarasa kollu anahtarın “klak”ı
  CueSelectorClunk   kademe anahtarının ağır “klonk”u
  CueTimeClockPunch  kart basma saatinin “ka-çunk”u
  CueDoorBuzzer      kapının elektrikli zili
  CuePowerDown       son şalter: bıçağın “klak”ı, jeneratörün yavaşlayıp susması

Çalıştırma (proje kökünden):   python3 -I Tools/Audio/cabinet_audio.py
"""
import math
import os
import random
import struct
import wave
import zlib

SR = 44100
SEED = 20261103
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Corrigible', 'Audio', 'Placeholder')


def rng(name):
    return random.Random(SEED ^ zlib.crc32(name.encode('ascii')))


def n(t):
    return int(round(t * SR))


def silence(t):
    return [0.0] * n(t)


def add(dst, src, at, gain=1.0):
    i0 = n(at)
    need = i0 + len(src)
    if need > len(dst):
        dst.extend([0.0] * (need - len(dst)))
    for i, v in enumerate(src):
        dst[i0 + i] += v * gain


def lowpass(x, fc):
    a = 1 - math.exp(-2 * math.pi * fc / SR)
    y, s = [], 0.0
    for v in x:
        s += a * (v - s)
        y.append(s)
    return y


def highpass(x, fc):
    lp = lowpass(x, fc)
    return [a - b for a, b in zip(x, lp)]


def noise(r, t):
    return [r.uniform(-1, 1) for _ in range(n(t))]


def env_exp(x, tau, attack=0.0005):
    out = []
    for i, v in enumerate(x):
        t = i / SR
        a = min(1.0, t / attack) if attack > 0 else 1.0
        out.append(v * a * math.exp(-t / tau))
    return out


def sine(f, t, phase=0.0):
    return [math.sin(2 * math.pi * f * i / SR + phase) for i in range(n(t))]


def ping(f, t, tau):
    return env_exp(sine(f, t), tau)


def click(r, t=0.012, fc=2500, tau=0.003):
    return env_exp(highpass(noise(r, t), fc), tau)


def fade_tail(x, t=0.01):
    k = n(t)
    for i in range(min(k, len(x))):
        x[len(x) - 1 - i] *= i / k
    return x


def normalize(x, peak):
    m = max(1e-9, max(abs(v) for v in x))
    return [v * peak / m for v in x]


def write(name, x, peak=0.8):
    x = fade_tail(normalize(x, peak))
    path = os.path.join(OUT, name + '.wav')
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, v)) * 32767)) for v in x))
    print(name, '%.2f s' % (len(x) / SR))


def cabinet_open():
    r = rng('CabinetOpen')
    x = silence(0.62)
    add(x, click(r, 0.02, 1800, 0.004), 0.0, 1.0)
    add(x, ping(2300, 0.05, 0.008), 0.002, 0.35)
    add(x, click(r, 0.015, 1200, 0.005), 0.13, 0.6)
    # Menteşe: titreyen 650–820 Hz gıcırtı, kısık
    creak = []
    ph = 0.0
    for i in range(n(0.38)):
        t = i / SR
        f = 650 + 170 * t / 0.38 + 25 * math.sin(2 * math.pi * 31 * t) + r.uniform(-15, 15)
        ph += 2 * math.pi * f / SR
        a = math.sin(math.pi * min(1, t / 0.38)) * (0.6 + 0.4 * math.sin(2 * math.pi * 9 * t))
        creak.append(math.sin(ph) * a + 0.3 * math.sin(2 * ph) * a)
    add(x, lowpass(creak, 2500), 0.18, 0.18)
    return x


def cabinet_close():
    r = rng('CabinetClose')
    x = silence(0.45)
    thud = env_exp([math.sin(2 * math.pi * 105 * i / SR) + 0.5 * math.sin(2 * math.pi * 210 * i / SR) for i in range(n(0.3))], 0.07, 0.002)
    add(x, thud, 0.0, 1.0)
    add(x, env_exp(lowpass(noise(r, 0.12), 1500), 0.025), 0.0, 0.7)
    add(x, ping(1400, 0.08, 0.02), 0.004, 0.18)
    add(x, click(r, 0.02, 1800, 0.004), 0.17, 0.55)
    return x


def fader_detent():
    r = rng('FaderDetent')
    x = silence(0.06)
    add(x, click(r, 0.01, 3000, 0.0018), 0.0, 1.0)
    add(x, ping(1800, 0.04, 0.006), 0.0, 0.4)
    return x


def switch_clack():
    r = rng('SwitchClack')
    x = silence(0.14)
    add(x, click(r, 0.012, 1500, 0.003), 0.0, 1.0)
    add(x, click(r, 0.015, 900, 0.004), 0.009, 0.8)
    add(x, ping(320, 0.1, 0.02), 0.009, 0.5)
    add(x, ping(2600, 0.05, 0.006), 0.009, 0.25)
    return x


def selector_clunk():
    r = rng('SelectorClunk')
    x = silence(0.24)
    add(x, click(r, 0.02, 900, 0.006), 0.0, 1.0)
    add(x, ping(180, 0.2, 0.04), 0.002, 0.8)
    add(x, ping(950, 0.08, 0.015), 0.002, 0.35)
    for k in range(3):
        add(x, click(r, 0.008, 2200, 0.002), 0.03 + 0.017 * k, 0.25 / (k + 1))
    return x


def time_clock_punch():
    r = rng('TimeClockPunch')
    x = silence(0.6)
    add(x, click(r, 0.015, 2500, 0.003), 0.0, 0.8)           # “ka”
    add(x, ping(2100, 0.06, 0.01), 0.0, 0.3)
    chunk = env_exp([math.sin(2 * math.pi * 92 * i / SR) + 0.6 * math.sin(2 * math.pi * 184 * i / SR) for i in range(n(0.35))], 0.06, 0.002)
    add(x, chunk, 0.12, 1.0)                                  # “çunk”
    add(x, env_exp(lowpass(highpass(noise(r, 0.15), 350), 1800), 0.03), 0.12, 0.9)
    add(x, ping(1650, 0.3, 0.07), 0.122, 0.22)                # zil yayı
    for k in range(4):                                        # mekanizmanın cırcırı
        add(x, click(r, 0.006, 2800, 0.0015), 0.28 + 0.035 * k, 0.3)
    return x


def door_buzzer():
    r = rng('DoorBuzzer')
    t_total = 0.72
    x = []
    for i in range(n(t_total)):
        t = i / SR
        a = min(1, t / 0.03) * (1 if t < 0.6 else max(0, 1 - (t - 0.6) / 0.1))
        v = 0.0
        for h in range(1, 12, 2):
            v += math.sin(2 * math.pi * 118 * h * t) / h
        v = math.tanh(2.2 * v) + 0.05 * r.uniform(-1, 1)
        x.append(v * a)
    return lowpass(highpass(x, 180), 3200)


def power_down():
    r = rng('PowerDown')
    x = silence(1.8)
    add(x, click(r, 0.03, 700, 0.008), 0.0, 1.0)              # bıçağın “klak”ı
    add(x, ping(210, 0.2, 0.05), 0.0, 0.6)
    gen = []
    ph = 0.0
    for i in range(n(1.6)):
        t = i / SR
        f = 25 + 95 * math.exp(-t / 0.45)
        ph += 2 * math.pi * f / SR
        saw = 2 * ((ph / (2 * math.pi)) % 1) - 1
        a = math.exp(-t / 0.7) * min(1, t / 0.01)
        gen.append(saw * a + 0.4 * math.sin(2 * math.pi * 50 * t) * math.exp(-t / 0.25))
    add(x, lowpass(gen, 900), 0.04, 0.8)
    return x


def main():
    os.makedirs(OUT, exist_ok=True)
    write('CueCabinetOpen', cabinet_open(), 0.7)
    write('CueCabinetClose', cabinet_close(), 0.75)
    write('CueFaderDetent', fader_detent(), 0.55)
    write('CueSwitchClack', switch_clack(), 0.7)
    write('CueSelectorClunk', selector_clunk(), 0.75)
    write('CueTimeClockPunch', time_clock_punch(), 0.8)
    write('CueDoorBuzzer', door_buzzer(), 0.55)
    write('CuePowerDown', power_down(), 0.8)


if __name__ == '__main__':
    main()
