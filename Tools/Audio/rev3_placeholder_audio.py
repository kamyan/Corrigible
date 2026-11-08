#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Corrigible (Son Şalter) - Revizyon 3 Aşama 10 (karar D5 = c): bankada klibi olmayan 42 ses olayının yer tutucuları.

Tek seferlik, tohumlu ve deterministik sentez; yalnız Python standart kitaplığı (numpy gerekmez). Küçük yapı taşları
cabinet_audio.py'den gelir. Mono 44,1 kHz 16 bit WAV'ları doğrudan Assets/Corrigible/Audio/Placeholder altına yazar
(Cue<enum adı>.wav). Süreler verildiği yerde tokens_siginak.json'un motion değerlerini ve Docs/UI_Siginak_Rev3/PROMPT.md §7'yi
izler. Klipleri bankaya Desk/Scripts/Editor/Rev3/Rev3Stage10Sounds.cs bağlar; ShiftLever (109) sessiz olmadığı için burada yok.

  56–66    görev fişi, sığınak, kapı, vardiya raporu, sonuç zilleri
  97–106   kartlı pano
  107–114  vardiya kapanışı (109 hariç)
  115–119  tarif defteri ve üretim
  120–128  kapı ve kabul

Çalıştırma (proje kökünden):   python3 -I Tools/Audio/rev3_placeholder_audio.py
"""
import importlib.util
import math
import os
import random
import sys
import zlib

sys.dont_write_bytecode = True    # Tools/Audio'da __pycache__ bırakma
_spec = importlib.util.spec_from_file_location(
    'cabinet_audio', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'cabinet_audio.py'))
ca = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ca)

SR, n, silence, add, lowpass, highpass, noise = ca.SR, ca.n, ca.silence, ca.add, ca.lowpass, ca.highpass, ca.noise
env_exp, sine, ping, click, write = ca.env_exp, ca.sine, ca.ping, ca.click, ca.write
SEED = 20261108


def rng(name):
    return random.Random(SEED ^ zlib.crc32(name.encode('ascii')))


def band(r, t, lo, hi):
    return lowpass(highpass(noise(r, t), lo), hi)


def shaped(x, f):
    return [v * f(i / SR) for i, v in enumerate(x)]


def hump(x, power=0.7):
    t = max(1e-6, len(x) / SR)
    return shaped(x, lambda u: math.sin(math.pi * min(1.0, u / t)) ** power)


def thud(f, t, tau, attack=0.002):
    return env_exp([math.sin(2 * math.pi * f * i / SR) + 0.5 * math.sin(4 * math.pi * f * i / SR) for i in range(n(t))], tau, attack)


def partials(t, parts, attack=0.0005):
    """parts: (frekans, genlik, tau) üçlüleri; çan, teneke ve çelik levha için uyumsuz kısmi sesler."""
    x = silence(t)
    for f, a, tau in parts:
        add(x, env_exp(sine(f, t), tau, attack), 0.0, a)
    return x


def paper(r, t, lo=1200, hi=7000, flutter=70, depth=0.4):
    """Kâğıt hışırtısı: bant sınırlı gürültü, düzensiz çırpıntı, yumuşak zarf."""
    phase, x = r.uniform(0, 6.28), band(r, t, lo, hi)
    return hump(shaped(x, lambda u: 1 - depth + depth * math.sin(2 * math.pi * flutter * u + phase + 2.5 * math.sin(2 * math.pi * 11 * u))))


def sweep(r, t, f0, f1, lo=800):
    """Kesimi f0'dan f1'e kayan gürültü: “şırp”."""
    y, s = [], 0.0
    for i, v in enumerate(noise(r, t)):
        u = i / max(1, n(t) - 1)
        s += (1 - math.exp(-2 * math.pi * (f0 + (f1 - f0) * u) / SR)) * (v - s)
        y.append(s)
    return hump(highpass(y, lo), 0.6)


def creak(r, t, f0, f1, wobble=9):
    """Menteşe: titreyen gıcırtı."""
    y, ph = [], 0.0
    for i in range(n(t)):
        u = i / SR
        ph += 2 * math.pi * (f0 + (f1 - f0) * u / t + 18 * math.sin(2 * math.pi * 27 * u) + r.uniform(-12, 12)) / SR
        a = math.sin(math.pi * min(1, u / t)) * (0.65 + 0.35 * math.sin(2 * math.pi * wobble * u))
        y.append((math.sin(ph) + 0.3 * math.sin(2 * ph)) * a)
    return lowpass(y, 2800)


def tick_train(r, x, start, stop, rate, fc=2600, tau=0.0016, gain=1.0):
    t = start
    while t < stop:
        add(x, click(r, 0.005, fc * r.uniform(0.85, 1.2), tau), t, gain * r.uniform(0.65, 1.0))
        t += (1.0 / rate) * r.uniform(0.88, 1.12)


# ------------------------------------------------------------------ 56–66 görev fişi, sığınak, kapı, rapor
def pin_drop():
    r = rng('PinDrop')
    x = silence(0.16)                                         # pinDropS 0,12 + sıçrama
    add(x, click(r, 0.008, 3500, 0.0015), 0.0, 1.0)
    add(x, ping(3300, 0.05, 0.007), 0.0, 0.5)
    add(x, thud(210, 0.05, 0.012), 0.0, 0.35)
    add(x, click(r, 0.005, 4500, 0.001), 0.055, 0.35)
    add(x, ping(3500, 0.03, 0.004), 0.055, 0.15)
    return x


def paper_slide():
    return paper(rng('PaperSlide'), 0.24, 600, 4200, 90, 0.25)    # slipSlideS 0,22


def pencil():
    r = rng('Pencil')
    x = silence(0.3)
    for at, d, g in ((0.0, 0.18, 1.0), (0.205, 0.08, 0.7)):
        add(x, hump(shaped(band(r, d, 2200, 9000), lambda u: 0.25 + abs(math.sin(2 * math.pi * 62 * u + 3 * math.sin(2 * math.pi * 7 * u))) ** 3)), at, g)
    return x


def pawn_place():
    r = rng('PawnPlace')
    x = silence(0.13)                                         # tahta “tok”
    add(x, ping(560, 0.1, 0.014), 0.0, 1.0)
    add(x, ping(1210, 0.06, 0.006), 0.0, 0.5)
    add(x, lowpass(click(r, 0.01, 500, 0.003), 2200), 0.0, 0.8)
    add(x, thud(130, 0.08, 0.016), 0.0, 0.4)
    return x


def hammer():
    r = rng('Hammer')
    x = silence(0.48)
    for at, g, k in ((0.0, 0.85, 1.0), (0.24, 1.0, 1.06)):
        add(x, click(r, 0.012, 1400, 0.003), at, g)
        add(x, ping(1850 * k, 0.12, 0.02), at, 0.5 * g)
        add(x, ping(3050 * k, 0.08, 0.012), at, 0.25 * g)
        add(x, thud(150, 0.15, 0.03), at, 0.9 * g)
    return x


def door_peep():
    r = rng('DoorPeep')
    x = silence(0.17)                                         # peepCoverS 0,15; metal “şık”
    add(x, hump(band(r, 0.09, 2500, 9000), 0.5), 0.0, 0.55)
    add(x, click(r, 0.01, 2000, 0.002), 0.09, 1.0)
    add(x, ping(2700, 0.06, 0.01), 0.09, 0.4)
    return x


def door_close():
    r = rng('DoorClose')
    x = silence(0.85)                                         # ağır kanat, ardından çarkın “klak”ı
    add(x, hump(lowpass(noise(r, 0.3), 240)), 0.0, 0.8)
    add(x, thud(62, 0.45, 0.11, 0.004), 0.28, 1.0)
    add(x, env_exp(lowpass(noise(r, 0.14), 900), 0.03), 0.28, 0.6)
    add(x, click(r, 0.02, 1100, 0.005), 0.5, 0.8)
    add(x, ping(340, 0.2, 0.03), 0.5, 0.55)
    add(x, ping(1500, 0.08, 0.014), 0.5, 0.25)
    return x


def stamp_heavy():
    r = rng('StampHeavy')
    x = silence(0.2)                                          # stampPressS 0,12; “tak”
    add(x, click(r, 0.012, 1000, 0.003), 0.0, 1.0)
    add(x, thud(140, 0.16, 0.03), 0.0, 1.0)
    add(x, ping(430, 0.08, 0.012), 0.0, 0.6)
    add(x, env_exp(lowpass(highpass(noise(r, 0.05), 300), 3200), 0.009), 0.0, 1.1)    # kâğıda çarpan lastik
    return x


def alarm_hum():
    x = []                                                    # alarm lambasının üç yanışıyla alçak uğultu
    T, ph = 1.3, 0.0
    for i in range(n(T)):
        t = i / SR
        ph += 2 * math.pi * 58 / SR
        saw = 2 * ((ph / (2 * math.pi)) % 1) - 1
        pulse = 0.35 + 0.65 * math.sin(math.pi * ((t * 3 / T) % 1)) ** 2
        fade = min(1, t / 0.06) * min(1, (T - t) / 0.15)
        x.append((saw + 0.5 * math.sin(4 * math.pi * 58 * t) + 0.25 * math.sin(6 * math.pi * 58 * t)) * pulse * fade)
    return lowpass(x, 620)


def bell(f, t, tau):
    return partials(t, [(f, 1.0, tau), (f * 2.01, 0.55, tau * 0.55), (f * 2.76, 0.4, tau * 0.38), (f * 5.4, 0.18, tau * 0.14)], 0.001)


def result_bell():
    r = rng('ResultBell')
    x = bell(1720, 0.75, 0.2)                                 # masa zili: kısa
    add(x, click(r, 0.006, 3000, 0.0012), 0.0, 0.4)
    return x


def final_bell():
    r = rng('FinalBell')
    x = silence(2.6)                                          # uzun zil: alçak perde, üç vuruş
    for at, g in ((0.0, 1.0), (0.72, 0.85), (1.44, 0.7)):
        add(x, bell(880, 2.6 - at, 0.55), at, g)
        add(x, click(r, 0.008, 2200, 0.002), at, 0.3 * g)
    return x


# ------------------------------------------------------------------ 97–106 kartlı pano
def board_lift():
    r = rng('BoardLift')
    return env_exp(paper(r, 0.1, 1500, 8000, 120, 0.3), 0.05, 0.003)    # cardLiftS 0,08


def board_magnet():
    r = rng('BoardMagnet')
    x = silence(0.06)                                         # settleS 0,06; mıknatıs “tık”ı
    add(x, click(r, 0.006, 3200, 0.0012), 0.0, 1.0)
    add(x, ping(2900, 0.04, 0.005), 0.0, 0.45)
    add(x, ping(1150, 0.03, 0.004), 0.0, 0.3)
    return x


def board_return():
    r = rng('BoardReturn')
    x = silence(0.09)                                         # yumuşak “tak”
    add(x, lowpass(click(r, 0.012, 300, 0.004), 1400), 0.0, 1.0)
    add(x, thud(310, 0.08, 0.012), 0.0, 0.8)
    return x


def board_deny():
    r = rng('BoardDeny')
    x = silence(0.15)                                         # kilitli planın boğuk “tok”u
    add(x, ping(205, 0.12, 0.025), 0.0, 1.0)
    add(x, ping(415, 0.06, 0.012), 0.0, 0.4)
    add(x, lowpass(click(r, 0.01, 200, 0.004), 900), 0.0, 0.7)
    add(x, ping(190, 0.06, 0.01), 0.07, 0.35)
    return x


def board_deck_slide():
    r = rng('BoardDeckSlide')
    x = paper(r, 0.32, 900, 5000, 45, 0.3)                    # deckPerCardS 0,32
    tick_train(r, x, 0.03, 0.29, 16, 2200, 0.002, 0.45)
    return x


def board_deck_collect():
    r = rng('BoardDeckCollect')
    x = sweep(r, 0.18, 1500, 8500)                            # “şırp”: yükselen
    add(x, lowpass(click(r, 0.012, 300, 0.004), 1800), 0.17, 0.7)
    return x


def board_item_tuck():
    r = rng('BoardItemTuck')
    x = silence(0.12)                                         # itemTuckS 0,15; “şık”
    add(x, env_exp(band(r, 0.07, 3000, 10000), 0.022, 0.002), 0.0, 1.0)
    add(x, click(r, 0.008, 2400, 0.002), 0.058, 0.55)
    return x


def board_hurda():
    r = rng('BoardHurda')
    x = partials(0.26, [(340, 1.0, 0.05), (785, 0.6, 0.032), (1320, 0.35, 0.018)])    # hurda kutusu “tunk”
    add(x, thud(120, 0.14, 0.03), 0.0, 0.8)
    add(x, click(r, 0.012, 1000, 0.003), 0.0, 0.5)
    return x


def board_padlock():
    r = rng('BoardPadlock')
    x = silence(0.17)                                         # asma kilit “klak”
    add(x, click(r, 0.01, 1500, 0.002), 0.0, 0.9)
    add(x, click(r, 0.014, 900, 0.0035), 0.028, 1.0)
    add(x, ping(1450, 0.08, 0.016), 0.028, 0.5)
    add(x, ping(2900, 0.05, 0.008), 0.028, 0.18)
    return x


def board_flap():
    r = rng('BoardFlap')
    x = partials(0.1, [(2450, 1.0, 0.01), (3800, 0.55, 0.006)])    # plan kapağı, metal “tık”
    add(x, click(r, 0.008, 2800, 0.0015), 0.0, 0.8)
    return x


# ------------------------------------------------------------------ 107–114 vardiya kapanışı
def shift_cover_open():
    r = rng('ShiftCoverOpen')
    x = silence(0.25)                                         # coverLiftS 0,25; menteşe “tık”ı
    add(x, click(r, 0.012, 1800, 0.0025), 0.0, 1.0)
    add(x, ping(2050, 0.05, 0.008), 0.0, 0.4)
    add(x, creak(r, 0.11, 1400, 1720, 21), 0.05, 0.16)
    add(x, click(r, 0.006, 2600, 0.0014), 0.22, 0.45)
    return x


def shift_cover_shut():
    r = rng('ShiftCoverShut')
    x = silence(0.2)
    add(x, click(r, 0.014, 1400, 0.003), 0.0, 1.0)
    add(x, ping(1600, 0.08, 0.015), 0.0, 0.4)
    add(x, thud(260, 0.1, 0.02), 0.0, 0.8)
    return x


def slip_print():
    r = rng('SlipPrint')
    x = silence(0.3)                                          # yazıcı “tırrt”: iğne vuruşları, motor
    tick_train(r, x, 0.0, 0.29, 95, 1900, 0.0013, 1.0)
    add(x, hump(sine(178, 0.3), 0.3), 0.0, 0.18)
    add(x, hump(band(r, 0.3, 600, 2200), 0.3), 0.0, 0.14)
    return x


def slip_tear():
    r = rng('SlipTear')
    x = silence(0.09)                                         # kopma “çıt”ı
    add(x, click(r, 0.008, 4000, 0.0012), 0.0, 1.0)
    add(x, env_exp(band(r, 0.04, 2200, 9000), 0.01, 0.001), 0.003, 0.75)
    add(x, click(r, 0.005, 5000, 0.0009), 0.021, 0.55)
    return x


def slip_file():
    r = rng('SlipFile')
    x = silence(0.13)                                         # EVRAK'a düşüş “şap”ı
    add(x, env_exp(band(r, 0.09, 500, 3200), 0.016, 0.001), 0.0, 1.0)
    add(x, thud(180, 0.05, 0.01), 0.0, 0.55)
    return x


def shift_gong():
    x = partials(1.5, [(138, 1.0, 0.5), (140.2, 0.6, 0.45), (210, 0.55, 0.33), (280, 0.4, 0.26), (374, 0.28, 0.17), (469, 0.16, 0.11)], 0.012)
    return lowpass(x, 1800)                                   # kısık gong; düzeyi çağrı yarıya indirir


def slip_mouth_empty():
    r = rng('SlipMouthEmpty')
    x = silence(0.06)                                         # boş ağzın kuru “tık”ı
    add(x, ping(740, 0.04, 0.006), 0.0, 1.0)
    add(x, click(r, 0.006, 1200, 0.0014), 0.0, 0.7)
    return x


# ------------------------------------------------------------------ 115–119 defter ve üretim
def board_book_open():
    r = rng('BoardBookOpen')
    x = silence(0.5)                                          # bookForwardS 0,35 + kapak
    add(x, paper(r, 0.33, 500, 2600, 30, 0.25), 0.0, 0.45)
    add(x, thud(170, 0.16, 0.03), 0.31, 1.0)
    add(x, env_exp(band(r, 0.06, 1200, 5000), 0.016, 0.002), 0.33, 0.5)
    return x


def board_page_turn():
    r = rng('BoardPageTurn')
    x = shaped(sweep(r, 0.2, 1400, 7500, 900), lambda u: 0.55 + 0.45 * math.sin(2 * math.pi * 26 * u))    # pageTurnS 0,25
    add(x, env_exp(band(r, 0.05, 1500, 6000), 0.012, 0.001), 0.19, 1.3)    # sayfanın inişi
    return x


def board_book_close():
    r = rng('BoardBookClose')
    x = silence(0.32)                                         # bookCloseS 0,3: hava, sonra “pof”
    add(x, paper(r, 0.2, 400, 2400, 22, 0.2), 0.0, 0.5)
    add(x, thud(130, 0.12, 0.03, 0.004), 0.19, 1.0)
    add(x, env_exp(lowpass(noise(r, 0.07), 700), 0.018, 0.003), 0.19, 0.7)
    return x


def board_recipe_lift():
    return sweep(rng('BoardRecipeLift'), 0.11, 7500, 1800, 1100)    # recipeLiftS 0,1; “şırp”: alçalan


def board_product_slide():
    r = rng('BoardProductSlide')
    x = silence(0.24)                                         # karton kayışı ve “şık” duruş
    add(x, paper(r, 0.19, 700, 3500, 55, 0.3), 0.0, 0.8)
    add(x, click(r, 0.01, 2000, 0.0025), 0.185, 1.0)
    return x


# ------------------------------------------------------------------ 120–128 kapı ve kabul
def admission_counter_down():
    r = rng('AdmissionCounterDown')
    x = silence(0.45)                                         # counterLeafS 0,4; kanat “tank”
    add(x, partials(0.42, [(233, 1.0, 0.11), (561, 0.65, 0.07), (947, 0.4, 0.04), (1490, 0.22, 0.025)]), 0.02, 1.0)
    add(x, thud(88, 0.2, 0.05), 0.02, 0.9)
    add(x, click(r, 0.012, 1100, 0.003), 0.02, 0.7)
    return x


def admission_drawer():
    r = rng('AdmissionDrawer')
    x = silence(0.36)                                         # aidDrawerS/drawerS 0,35: makara, dip
    add(x, hump(lowpass(noise(r, 0.3), 420), 0.4), 0.0, 0.7)
    tick_train(r, x, 0.02, 0.29, 42, 1300, 0.0022, 0.3)
    add(x, thud(160, 0.07, 0.016), 0.295, 1.0)
    add(x, ping(620, 0.06, 0.012), 0.295, 0.35)
    return x


def admission_marker():
    r = rng('AdmissionMarker')
    x = silence(0.07)                                         # talimat işaretçisi “tık”ı
    add(x, ping(1650, 0.05, 0.008), 0.0, 1.0)
    add(x, click(r, 0.006, 2000, 0.0014), 0.0, 0.6)
    return x


def admission_slip():
    r = rng('AdmissionSlip')
    x = silence(0.35)                                         # slipS 0,35; yavaş yazıcı tıkırtısı
    t = 0.0
    while t < 0.33:
        f = r.uniform(900, 1250)
        add(x, ping(f, 0.018, 0.0035), t, r.uniform(0.55, 1.0))
        add(x, click(r, 0.004, 1500, 0.001), t, 0.35)
        t += r.uniform(0.024, 0.036)
    return x


def admission_intercom():
    r = rng('AdmissionIntercom')
    x = band(r, 0.2, 600, 3400)                               # intercomCrackleS 0,2
    gates, gate = [], 1.0
    for i in range(len(x)):
        if i % n(0.006) == 0:
            gate = r.choice((0.15, 0.35, 0.7, 1.0, 1.0))
        gates.append(gate)
    y = [v * g + 0.12 * math.sin(2 * math.pi * 100 * i / SR) for i, (v, g) in enumerate(zip(x, gates))]
    return hump(y, 0.25)


def admission_glass():
    r = rng('AdmissionGlass')
    x = silence(0.22)                                         # glassCoverS 0,2; cam “tin”, buğunun nefesi
    add(x, ping(3300, 0.1, 0.02), 0.0, 1.0)
    add(x, ping(5100, 0.06, 0.01), 0.0, 0.45)
    add(x, click(r, 0.005, 4000, 0.001), 0.0, 0.5)
    add(x, hump(band(r, 0.2, 2500, 8000)), 0.015, 0.22)
    return x


def admission_pad_tear():
    r = rng('AdmissionPadTear')
    x = silence(0.2)                                          # delikli sıra boyunca hızlanan yırtık
    t, gap = 0.0, 0.026
    while t < 0.16:
        add(x, env_exp(band(r, 0.02, 1800, 8000), 0.006, 0.0005), t, r.uniform(0.6, 1.0))
        t += gap
        gap = max(0.007, gap * 0.86)
    add(x, click(r, 0.008, 3500, 0.0015), t, 0.7)
    return x


def admission_stamp_lift():
    r = rng('AdmissionStampLift')
    x = silence(0.1)                                          # stampLiftS 0,1; raftan kalkış
    add(x, ping(640, 0.05, 0.008), 0.0, 1.0)
    add(x, env_exp(band(r, 0.04, 800, 4000), 0.01, 0.001), 0.012, 0.55)
    return x


def admission_counter_fold():
    r = rng('AdmissionCounterFold')
    x = silence(0.55)                                         # gıcırtı, kanadın kapanışı, mandal
    add(x, creak(r, 0.27, 540, 430), 0.0, 0.3)
    add(x, click(r, 0.016, 900, 0.004), 0.29, 0.9)
    add(x, ping(350, 0.16, 0.03), 0.29, 0.75)
    add(x, thud(130, 0.16, 0.035), 0.29, 0.9)
    add(x, click(r, 0.008, 2200, 0.0018), 0.46, 0.6)
    return x


# (dosya adı, üretici, tepe düzeyi)
CUES = [
    ('CuePinDrop', pin_drop, 0.6), ('CuePaperSlide', paper_slide, 0.5), ('CuePencil', pencil, 0.45),
    ('CuePawnPlace', pawn_place, 0.65), ('CueHammer', hammer, 0.75), ('CueDoorPeep', door_peep, 0.6),
    ('CueDoorClose', door_close, 0.8), ('CueStampHeavy', stamp_heavy, 0.8), ('CueAlarmHum', alarm_hum, 0.6),
    ('CueResultBell', result_bell, 0.6), ('CueFinalBell', final_bell, 0.65),
    ('CueBoardLift', board_lift, 0.5), ('CueBoardMagnet', board_magnet, 0.55), ('CueBoardReturn', board_return, 0.55),
    ('CueBoardDeny', board_deny, 0.65), ('CueBoardDeckSlide', board_deck_slide, 0.5), ('CueBoardDeckCollect', board_deck_collect, 0.5),
    ('CueBoardItemTuck', board_item_tuck, 0.5), ('CueBoardHurda', board_hurda, 0.7), ('CueBoardPadlock', board_padlock, 0.7),
    ('CueBoardFlap', board_flap, 0.55),
    ('CueShiftCoverOpen', shift_cover_open, 0.6), ('CueShiftCoverShut', shift_cover_shut, 0.65), ('CueSlipPrint', slip_print, 0.55),
    ('CueSlipTear', slip_tear, 0.6), ('CueSlipFile', slip_file, 0.6), ('CueShiftGong', shift_gong, 0.75),
    ('CueSlipMouthEmpty', slip_mouth_empty, 0.5),
    ('CueBoardBookOpen', board_book_open, 0.6), ('CueBoardPageTurn', board_page_turn, 0.5), ('CueBoardBookClose', board_book_close, 0.6),
    ('CueBoardRecipeLift', board_recipe_lift, 0.5), ('CueBoardProductSlide', board_product_slide, 0.5),
    ('CueAdmissionCounterDown', admission_counter_down, 0.8), ('CueAdmissionDrawer', admission_drawer, 0.65),
    ('CueAdmissionMarker', admission_marker, 0.55), ('CueAdmissionSlip', admission_slip, 0.55), ('CueAdmissionIntercom', admission_intercom, 0.5),
    ('CueAdmissionGlass', admission_glass, 0.5), ('CueAdmissionPadTear', admission_pad_tear, 0.55), ('CueAdmissionStampLift', admission_stamp_lift, 0.5),
    ('CueAdmissionCounterFold', admission_counter_fold, 0.7),
]


def main():
    os.makedirs(ca.OUT, exist_ok=True)
    for name, make, peak in CUES:
        write(name, make(), peak)


if __name__ == '__main__':
    main()
