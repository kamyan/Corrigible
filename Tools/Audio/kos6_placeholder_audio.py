#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Corrigible (Son Şalter) - KOS aşama 6: İFŞA şeridinin hücre sesi (Cue.ExposureStep = 129) için yer tutucu.

Tek seferlik, tohumlu ve deterministik sentez; yalnız Python standart kitaplığı. Yapı taşları cabinet_audio.py'den
(desen rev3_placeholder_audio.py). Mono 44,1 kHz 16 bit WAV'ı Assets/Corrigible/Audio/Placeholder/CueExposureStep.wav
olarak yazar. Ses: rölenin kuru "klak"ı ve ardından kısa, alçak bir lamba vızıltısı (≤ 0,35 s); hücreler 0,12 s arayla
yandığı için kuyruk kısa tutuldu. Klibi bankaya (SoundBank.cues) bir kerelik editör adımı bağlar.

Çalıştırma (proje kökünden):   python3 -I Tools/Audio/kos6_placeholder_audio.py
"""
import importlib.util
import math
import os
import random
import sys
import zlib

sys.dont_write_bytecode = True
_spec = importlib.util.spec_from_file_location(
    'cabinet_audio', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'cabinet_audio.py'))
ca = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ca)

SR, n, silence, add, lowpass, highpass, noise = ca.SR, ca.n, ca.silence, ca.add, ca.lowpass, ca.highpass, ca.noise
env_exp, ping, click, write = ca.env_exp, ca.ping, ca.click, ca.write
SEED = 20261114


def rng(name):
    return random.Random(SEED ^ zlib.crc32(name.encode('ascii')))


def exposure_step():
    r = rng('ExposureStep')
    x = silence(0.34)
    add(x, click(r, 0.010, 1800, 0.0025), 0.0, 1.0)            # röle dili
    add(x, ping(620, 0.05, 0.008), 0.0, 0.45)                 # metal "klak"
    add(x, click(r, 0.006, 3200, 0.0012), 0.012, 0.5)         # kontak sekmesi
    hum = []
    T = 0.28
    for i in range(n(T)):
        t = i / SR
        a = min(1.0, t / 0.02) * math.exp(-t / 0.11)
        hum.append(a * (math.sin(2 * math.pi * 100 * t) + 0.45 * math.sin(2 * math.pi * 200 * t) + 0.2 * math.sin(2 * math.pi * 300 * t)))
    add(x, lowpass(hum, 900), 0.03, 0.35)                     # lamba vızıltısı
    return x


def main():
    os.makedirs(ca.OUT, exist_ok=True)
    write('CueExposureStep', exposure_step(), 0.6)


if __name__ == '__main__':
    main()
