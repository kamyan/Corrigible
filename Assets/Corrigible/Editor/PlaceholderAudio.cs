using System;
using System.IO;
using System.Linq;
using Corrigible.Game.Audio;
using UnityEditor;
using UnityEngine;

namespace Corrigible.EditorTools
{
    /// <summary>
    /// Yer tutucu sesleri kodla üretir (Assets/Corrigible/Audio/Placeholder/*.wav) ve Settings/SoundBank'a bağlar.
    /// Gerçek sesler gelince SoundBank'taki klipler değiştirilir; bu araç yeniden çalıştırılırsa yalnızca kendi
    /// dosyalarını yazar ve SoundBank'ta hâlâ yer tutucuya bakan alanları günceller.
    /// Müzik: Re minör, 90 BPM, 16 vuruşluk (≈10,7 sn) döngüler; bütün katmanlar aynı uzunlukta.
    /// </summary>
    public static class PlaceholderAudio
    {
        const int SR = 44100;
        const string Dir = "Assets/Corrigible/Audio/Placeholder";
        const string BankPath = "Assets/Corrigible/Settings/SoundBank.asset";
        const float Bpm = 90, Beat = 60f / Bpm;
        const int LoopBeats = 16;
        static readonly int Loop = Mathf.RoundToInt(LoopBeats * Beat * SR);
        static System.Random rnd;

        // Ajans jingle'ları (GameData.Agencies sırası); sahte ajansta üçüncü nota yarım ses kayık
        static readonly int[][] Jingles = { new[] { 72, 79, 76, 84 }, new[] { 74, 78, 81, 86 }, new[] { 67, 71, 74, 79 }, new[] { 76, 76, 72, 79 } };

        [MenuItem("Corrigible/Ses/Yer tutucu sesleri üret")]
        public static string Generate()
        {
            rnd = new System.Random(1234);
            Directory.CreateDirectory(Dir);
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }

            Write("MusicDrone", Drone(), true);
            Write("MusicPulse", Pulse(), true);
            Write("MusicArp", Arp(), true);
            Write("MusicTension", Tension(), true);
            Write("MusicWarm", Warm(), true);
            Write("Grit", Grit(), true);
            Write("Wind", Wind(), true);
            Write("MotifWorm", MotifWorm(), false);
            Write("MotifManipulator", MotifManipulator(), false);
            Write("MotifOracle", MotifOracle(), false);
            for (int i = 0; i < Jingles.Length; i++)
            {
                Write($"Jingle{i}", Jingle(Jingles[i], false), false);
                Write($"JingleFake{i}", Jingle(Jingles[i], true), false);
            }
            Write("Teleprinter", Teleprinter(), false);
            Write("Alarm", Alarm(), false);
            Write("SoftChime", SoftChime(), false);
            Write("KillSwitch", KillSwitch(), false);
            Write("GlobalArm", GlobalArm(), false);
            Write("VeilTear", VeilTear(), false);
            AssetDatabase.Refresh();

            AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Dir}/{n}.wav");
            bank.drone = C("MusicDrone"); bank.pulse = C("MusicPulse"); bank.arpeggio = C("MusicArp");
            bank.tension = C("MusicTension"); bank.warm = C("MusicWarm"); bank.grit = C("Grit"); bank.wind = C("Wind");
            bank.voices = new[]
            {
                new MotifVoice { typeId = "solucan", note = C("MotifWorm") },
                new MotifVoice { typeId = "manipulator", note = C("MotifManipulator") },
                new MotifVoice { typeId = "kahin", note = C("MotifOracle") },
            };
            bank.agencyJingles = Enumerable.Range(0, Jingles.Length).Select(i => C($"Jingle{i}")).ToArray();
            bank.fakeJingles = Enumerable.Range(0, Jingles.Length).Select(i => C($"JingleFake{i}")).ToArray();
            bank.teleprinter = C("Teleprinter"); bank.alarm = C("Alarm"); bank.softChime = C("SoftChime");
            bank.killSwitch = C("KillSwitch"); bank.globalArm = C("GlobalArm"); bank.veilTear = C("VeilTear");
            CuesAndLoops(bank, C);
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return $"{Directory.GetFiles(Dir, "*.wav").Length} yer tutucu ses üretildi";
        }

        // ================================================================== yazma
        static void Write(string name, float[] data, bool music)
        {
            Normalize(data, music ? 0.8f : 0.9f);
            var path = $"{Dir}/{name}.wav";
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = data.Length * 2;
                w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
                w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(bytes);
                foreach (var v in data) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
            }
            AssetDatabase.ImportAsset(path);
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            var s = imp.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            s.quality = 0.7f;
            imp.defaultSampleSettings = s;
            imp.loadInBackground = music;
            imp.SaveAndReimport();
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0; foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m < 1e-6f) return;
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        // ================================================================== yapı taşları
        static float Hz(float midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        // Döngüde tam sayıda devir: sürekli osilatörler döngü sınırında tıklamaz
        static float LoopHz(float f) => Mathf.Max(1, Mathf.Round(f * Loop / SR)) * SR / Loop;
        static float Noise() => (float)(rnd.NextDouble() * 2 - 1);
        static float Saw(double ph) => (float)(2 * (ph - Math.Floor(ph + 0.5)));
        static float Tri(double ph) => (float)(1 - 4 * Math.Abs(ph - Math.Floor(ph + 0.75) + 0.25));
        static float Sin(double ph) => (float)Math.Sin(2 * Math.PI * ph);
        static float Sq(double ph) => ph - Math.Floor(ph) < 0.5 ? 1f : -1f;

        static void LowPass(float[] d, float cutoff)
        {
            float a = 1 - Mathf.Exp(-2 * Mathf.PI * cutoff / SR), y = 0;
            for (int i = 0; i < d.Length; i++) { y += a * (d[i] - y); d[i] = y; }
        }

        /// <summary>Döngü için alçak geçiren: filtre durumu döngünün sonundan başlar, başta sıçrama olmaz.</summary>
        static void LowPassLoop(float[] d, float cutoff)
        {
            float a = 1 - Mathf.Exp(-2 * Mathf.PI * cutoff / SR), y = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < d.Length; i++) { y += a * (d[i] - y); if (pass == 1) d[i] = y; }
        }

        static float[] HighPassed(float[] d, float cutoff)
        {
            var lp = (float[])d.Clone(); LowPass(lp, cutoff);
            for (int i = 0; i < d.Length; i++) lp[i] = d[i] - lp[i];
            return lp;
        }

        static void Echo(float[] d, float delay, float feedback)
        {
            int n = (int)(delay * SR);
            for (int i = n; i < d.Length; i++) d[i] += d[i - n] * feedback;
        }

        /// <summary>Döngünün sonundan taşan kuyruğu başa ekler (notalar döngü sınırında kesilmez).</summary>
        static float[] Wrap(float[] d)
        {
            var o = new float[Loop];
            for (int i = 0; i < d.Length; i++) o[i % Loop] += d[i];
            return o;
        }

        /// <summary>Sürekli gürültü dokuları için: kuyruğu başla çapraz geçişle birleştirir.</summary>
        static float[] Crossfade(float[] d, int fade)
        {
            var o = new float[Loop];
            Array.Copy(d, o, Loop);
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                o[i] = d[i] * Mathf.Sqrt(t) + d[Loop + i] * Mathf.Sqrt(1 - t);
            }
            return o;
        }

        // Kısa bir nota: dalga + atak/sönüm zarfı, buffer'a eklenir
        static void Note(float[] d, float start, float freq, float len, Func<double, float> wave, float amp, float attack = 0.005f, float decay = 0.3f)
        {
            int s0 = (int)(start * SR), n = (int)((len + decay * 4) * SR);
            double ph = 0, inc = freq / SR;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / SR;
                float env = t < attack ? t / attack : t < len ? 1 : Mathf.Exp(-(t - len) / decay);
                if (t >= len && attack > len) env = Mathf.Exp(-(t - len) / decay);
                d[s0 + i] += wave(ph) * env * amp;
                ph += inc;
            }
        }

        static float[] Buffer(float seconds) => new float[(int)(seconds * SR)];
        static float[] LoopBuffer(float tail = 3f) => new float[Loop + (int)(tail * SR)];

        // Akorlar (Re minör): Dm, Bb, Gm, A — her biri 4 vuruş
        static readonly int[][] Chords = { new[] { 62, 65, 69, 74 }, new[] { 58, 62, 65, 70 }, new[] { 55, 58, 62, 67 }, new[] { 57, 61, 64, 69 } };

        // ================================================================== müzik
        static float[] Drone()
        {
            var d = new float[Loop];
            float f1 = LoopHz(Hz(38)), f2 = LoopHz(Hz(45)), f0 = LoopHz(Hz(26)), lfo = 2f * SR / Loop;
            for (int i = 0; i < Loop; i++)
            {
                double t = (double)i / SR;
                d[i] = Saw(f1 * t) * 0.5f + Saw(f2 * t * 1.0) * 0.35f + Sin(f0 * t) * 0.6f;
                d[i] *= 0.75f + 0.25f * Sin(lfo * t);
            }
            LowPassLoop(d, 380); LowPassLoop(d, 600);
            return d;
        }

        static float[] Pulse()
        {
            var d = LoopBuffer(1);
            for (int b = 0; b < LoopBeats; b++)
            {
                float t0 = b * Beat;
                Kick(d, t0, b % 2 == 0 ? 1f : 0.75f);
                if (b % 2 == 0) Kick(d, t0 + 0.2f, 0.55f);      // kalp atışı
                Hat(d, t0 + Beat / 2, 0.12f);
            }
            return Wrap(d);
        }

        static void Kick(float[] d, float t0, float amp)
        {
            int s0 = (int)(t0 * SR); double ph = 0;
            for (int i = 0; i < (int)(0.45f * SR) && s0 + i < d.Length; i++)
            {
                float t = (float)i / SR;
                float f = 45 + 75 * Mathf.Exp(-t / 0.04f);
                ph += f / SR;
                d[s0 + i] += Sin(ph) * Mathf.Exp(-t / 0.16f) * amp;
            }
        }

        static void Hat(float[] d, float t0, float amp)
        {
            var b = new float[(int)(0.08f * SR)];
            for (int i = 0; i < b.Length; i++) b[i] = Noise() * Mathf.Exp(-(float)i / SR / 0.018f);
            b = HighPassed(b, 6000);
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < b.Length && s0 + i < d.Length; i++) d[s0 + i] += b[i] * amp;
        }

        static float[] Arp()
        {
            var d = LoopBuffer();
            int[] order = { 0, 1, 2, 3, 2, 1, 2, 3 };
            for (int c = 0; c < 4; c++)
            for (int k = 0; k < 8; k++)
            {
                float t0 = (c * 4 + k * 0.5f) * Beat;
                Note(d, t0, Hz(Chords[c][order[k]] + 12), 0.05f, Tri, 0.35f, 0.004f, 0.22f);
            }
            Echo(d, Beat * 0.75f, 0.28f);
            LowPass(d, 5000);
            return Wrap(d);
        }

        static float[] Tension()
        {
            var d = LoopBuffer();
            for (int c = 0; c < 4; c++)
            {
                foreach (var m in Chords[c].Take(3))
                {
                    Note(d, c * 4 * Beat, Hz(m) * 1.003f, 4 * Beat - 0.3f, Saw, 0.12f, 0.25f, 0.4f);
                    Note(d, c * 4 * Beat, Hz(m) * 0.997f, 4 * Beat - 0.3f, Saw, 0.12f, 0.25f, 0.4f);
                }
            }
            LowPass(d, 1700);
            for (int s = 0; s < LoopBeats * 4; s++) Hat(d, s * Beat / 4, s % 4 == 0 ? 0.1f : 0.05f);
            for (int b = 1; b < LoopBeats; b += 2) Snare(d, b * Beat, 0.35f);
            return Wrap(d);
        }

        static void Snare(float[] d, float t0, float amp)
        {
            var b = new float[(int)(0.25f * SR)];
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                ph += 190.0 / SR;
                b[i] = (Noise() * 0.7f + Sin(ph) * 0.5f) * Mathf.Exp(-t / 0.06f);
            }
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < b.Length && s0 + i < d.Length; i++) d[s0 + i] += b[i] * amp;
        }

        // Rahatlatıcı majör ped: tehlikeli huzur (bağımlılık)
        static float[] Warm()
        {
            var d = LoopBuffer(4);
            int[][] warmChords = { new[] { 65, 69, 72, 77 }, new[] { 58, 62, 65, 74 } };
            for (int c = 0; c < 2; c++)
            foreach (var m in warmChords[c])
            {
                float f = Hz(m);
                Note(d, c * 8 * Beat, f, 8 * Beat - 1.2f, ph => Sin(ph) * 0.8f + Sin(ph * 2) * 0.15f, 0.18f, 1.6f, 1.2f);
            }
            Echo(d, Beat * 1.5f, 0.35f);
            LowPass(d, 2400);
            return Wrap(d);
        }

        // Gerçek dünyanın uğultusu: şebeke hum'u, cızırtı, ara ara parazit
        static float[] Grit()
        {
            var d = LoopBuffer(1);
            float h1 = LoopHz(50), h2 = LoopHz(100), h3 = LoopHz(150);
            var noise = new float[d.Length];
            for (int i = 0; i < noise.Length; i++) noise[i] = Noise();
            LowPass(noise, 1200);
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SR;
                d[i] = Sin(h1 * t) * 0.25f + Sin(h2 * t) * 0.12f + Sin(h3 * t) * 0.06f + noise[i] * 0.35f;
            }
            for (int k = 0; k < 60; k++)
            {
                int at = rnd.Next(d.Length - 2000); float a = (float)rnd.NextDouble() * 0.8f + 0.2f;
                for (int i = 0; i < 300; i++) d[at + i] += Noise() * a * Mathf.Exp(-i / 60f);
            }
            for (int k = 0; k < 3; k++)
            {
                int at = rnd.Next(d.Length - SR); int len = (int)(0.25f * SR);
                for (int i = 0; i < len; i++) d[at + i] += Noise() * 0.5f * Mathf.Sin(Mathf.PI * i / len);
            }
            return Crossfade(d, SR / 2);
        }

        static float[] Wind()
        {
            var d = LoopBuffer(1);
            float a = 0, b = 0, l1 = 1f * SR / Loop, l2 = 3f * SR / Loop;
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SR;
                float cut = 250 + 500 * (0.5f + 0.5f * Sin(l1 * t)) + 150 * Sin(l2 * t);
                float k = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
                a += k * (Noise() - a); b += k * (a - b);
                d[i] = b * (0.6f + 0.4f * Sin(l1 * t + 0.3));
            }
            return Crossfade(d, SR);
        }

        // ================================================================== YZ motifi (A4 = 440 Hz; perdeyle çalınır)
        static float[] MotifWorm()
        {
            var d = Buffer(0.7f);
            double ph = 0; float hold = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / SR;
                ph += 440.0 / SR;
                if (i % 6 == 0) hold = Mathf.Round(Sq(ph) * (0.6f + 0.4f * Noise()) * 4) / 4;   // örnek tut + 2 bit
                d[i] = hold * Mathf.Exp(-t / 0.18f) * Mathf.Min(1, t / 0.002f);
            }
            return d;
        }

        static float[] MotifManipulator()
        {
            var d = Buffer(1.0f);
            double ph = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / SR;
                ph += 440.0 * (1 + 0.006 * Math.Sin(2 * Math.PI * 5.5 * t)) / SR;
                float env = Mathf.Min(1, t / 0.03f) * Mathf.Exp(-t / 0.35f);
                d[i] = (Saw(ph) * 0.7f + Noise() * 0.25f) * env;
            }
            LowPass(d, 1600); LowPass(d, 2400);
            return d;
        }

        static float[] MotifOracle()
        {
            var d = Buffer(1.6f);
            float[] partials = { 1f, 2.76f, 5.4f, 8.93f }, decays = { 0.9f, 0.45f, 0.22f, 0.1f }, amps = { 1f, 0.5f, 0.3f, 0.15f };
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / SR;
                float v = 0;
                for (int p = 0; p < partials.Length; p++) v += Mathf.Sin(2 * Mathf.PI * 440 * partials[p] * t) * amps[p] * Mathf.Exp(-t / decays[p]);
                d[i] = v * Mathf.Min(1, t / 0.003f);
            }
            Echo(d, 0.19f, 0.25f);
            return d;
        }

        // ================================================================== haber bandı
        static float Bright(double ph) => Sin(ph) * 0.75f + Sin(ph * 2) * 0.25f + Sin(ph * 3) * 0.1f;

        static float[] Jingle(int[] notes, bool fake)
        {
            var d = Buffer(1.6f);
            for (int i = 0; i < notes.Length; i++)
            {
                float m = notes[i] + (fake && i == 2 ? 1 : 0);
                bool last = i == notes.Length - 1;
                Note(d, i * 0.15f, Hz(m) * (fake && i == 2 ? 1.004f : 1f), last ? 0.25f : 0.08f, Bright, 0.5f, 0.004f, last ? 0.35f : 0.08f);
            }
            Echo(d, 0.11f, 0.22f);
            return d;
        }

        static float[] Teleprinter()
        {
            var d = Buffer(0.5f);
            float t = 0.01f;
            for (int k = 0; k < 10; k++)
            {
                int s0 = (int)(t * SR);
                for (int i = 0; i < 400 && s0 + i < d.Length; i++)
                    d[s0 + i] += (Noise() * 0.6f + Mathf.Sin(2 * Mathf.PI * 2100 * i / SR) * 0.4f) * Mathf.Exp(-i / 70f);
                t += 0.025f + (float)rnd.NextDouble() * 0.02f;
            }
            return HighPassed(d, 900);
        }

        static float[] Alarm()
        {
            var d = Buffer(1.7f);
            for (int k = 0; k < 6; k++)
                Note(d, k * 0.2f, k % 2 == 0 ? 880 : 660, 0.15f, ph => Mathf.Clamp(Sin(ph) * 2.2f, -1, 1), 0.45f, 0.004f, 0.04f);
            LowPass(d, 4000);
            return d;
        }

        static float[] SoftChime()
        {
            var d = Buffer(1.8f);
            Note(d, 0, Hz(88), 0.02f, Sin, 0.35f, 0.004f, 0.5f);
            Note(d, 0.22f, Hz(83), 0.02f, Sin, 0.3f, 0.004f, 0.7f);
            Echo(d, 0.2f, 0.2f);
            return d;
        }

        // ================================================================== ses olayları ve döngüler
        // (cue, klip üreteci, ses düzeyi, perde oynaması)
        static (Cue cue, Func<float[]> make, float volume, float jitter)[] CueDefs => new (Cue, Func<float[]>, float, float)[]
        {
            (Cue.Click, UiClick, 0.35f, 0.06f), (Cue.Hover, UiHover, 0.12f, 0.08f), (Cue.Deny, UiDeny, 0.45f, 0f), (Cue.Confirm, UiConfirm, 0.4f, 0f),
            (Cue.RegionSelect, () => RegionBlip(false), 0.45f, 0.03f), (Cue.RegionSelectRadio, () => RegionBlip(true), 0.5f, 0.03f),
            (Cue.Monitor, Modem, 0.45f, 0f), (Cue.Audit, AuditSweep, 0.5f, 0f), (Cue.Quarantine, () => Relay(false), 0.7f, 0f), (Cue.QuarantineLift, () => Relay(true), 0.6f, 0f),
            (Cue.Veto, Veto, 0.8f, 0f), (Cue.ResearchStart, ResearchStart, 0.4f, 0f), (Cue.ResearchDone, ResearchDone, 0.55f, 0f), (Cue.Sabotage, Sabotage, 0.6f, 0f),
            (Cue.VerifyTrue, () => Pings(new[] { 79, 86 }), 0.5f, 0f), (Cue.VerifyFake, VerifyFake, 0.55f, 0f), (Cue.VerifyUnknown, StaticShort, 0.45f, 0f),
            (Cue.Gift, GiftMelody, 0.6f, 0f), (Cue.GiftAccept, () => Swell(new[] { 65, 69, 72, 77 }), 0.55f, 0f), (Cue.GiftReject, GiftReject, 0.6f, 0f),
            (Cue.GiftControlled, () => { var d = Swell(new[] { 65, 69, 72 }); Click(d, 0.02f, 0.5f); return d; }, 0.5f, 0f),
            (Cue.Advisor, () => Pings(new[] { 81, 76 }), 0.35f, 0f), (Cue.TeamRadio, () => Radio(0.45f, false), 0.45f, 0.03f), (Cue.LogBlip, LogBlip, 0.4f, 0f),
            (Cue.EndAlign, EndAlign, 0.8f, 0f), (Cue.EndErase, EndErase, 0.8f, 0f), (Cue.EndTakeover, EndTakeover, 0.7f, 0f), (Cue.EndDissolve, EndDissolve, 0.6f, 0f),
            (Cue.FakeFanfare, Fanfare, 0.7f, 0f), (Cue.EndSilent, () => { var d = Fanfare(); Array.Reverse(d); for (int i = 0; i < d.Length; i++) d[i] *= 1 - (float)i / d.Length * 0.5f; return Crush(d, 5); }, 0.75f, 0f),
            (Cue.RaidSelect, () => Chirp(900, 1500, 0.08f), 0.4f, 0.05f), (Cue.RaidStep, Servo, 0.4f, 0.1f), (Cue.RaidCutter, Cutter, 0.55f, 0.05f),
            (Cue.RaidEmpCharge, () => Chirp(300, 2400, 0.55f, true), 0.45f, 0.03f), (Cue.RaidEmpBlast, EmpBlast, 0.8f, 0.05f), (Cue.RaidHookThrow, HookThrow, 0.55f, 0.05f),
            (Cue.RaidDroneShot, () => Chirp(2200, 280, 0.22f), 0.45f, 0.08f), (Cue.RaidMelee, Melee, 0.6f, 0.08f), (Cue.RaidHit, () => Impact(90, 0.25f, 0.7f), 0.7f, 0.1f),
            (Cue.RaidHitObject, MetalHit, 0.6f, 0.1f), (Cue.RaidArmor, Armor, 0.5f, 0.08f), (Cue.RaidRepair, Repair, 0.45f, 0.05f), (Cue.RaidPush, Push, 0.5f, 0.1f),
            (Cue.RaidUnitDown, () => { var d = Radio(0.35f, true); Tone(d, 0.35f, 600, 150, 0.5f, Sin, 0.35f); return d; }, 0.6f, 0f),
            (Cue.RaidRobotDown, () => Chirp(1400, 90, 0.6f, true), 0.5f, 0.08f), (Cue.RaidCivilianBreak, CivBreak, 0.7f, 0.05f),
            (Cue.RaidExplosion, () => Explosion(1.1f), 0.8f, 0.06f), (Cue.RaidCoreBreak, CoreBreak, 0.9f, 0f), (Cue.RaidBreak, () => Crash(0.6f), 0.6f, 0.08f),
            (Cue.RaidSpawn, () => Chirp(120, 900, 0.5f, true), 0.45f, 0.06f), (Cue.IntentLock, () => Lock(false), 0.45f, 0f), (Cue.IntentLockFuzzy, () => Lock(true), 0.45f, 0f),
            (Cue.RaidEndTurn, () => Pings(new[] { 84, 84 }, 0.09f), 0.4f, 0f), (Cue.RaidBreach, Breach, 0.85f, 0f), (Cue.RaidRadio, () => Radio(1.0f, true), 0.55f, 0f),
            (Cue.RaidWin, () => Arpeggio(new[] { 62, 66, 69, 74, 78 }, 0.11f), 0.6f, 0f), (Cue.RaidFail, () => Arpeggio(new[] { 74, 70, 67, 62, 58 }, 0.14f), 0.6f, 0f),
        };

        static (LoopCue loop, Func<float[]> make, float volume)[] LoopDefs => new (LoopCue, Func<float[]>, float)[]
        {
            (LoopCue.Heartbeat, Heartbeat, 0.6f), (LoopCue.Clock, () => Ticks(1f, 1f), 0.25f),
            (LoopCue.RaidAmbience, RaidAmbience, 0.45f), (LoopCue.RaidClock, () => Ticks(0.5f, 1f), 0.35f),
        };

        static void CuesAndLoops(SoundBank bank, Func<string, AudioClip> C)
        {
            foreach (var d in CueDefs) Write("Cue" + d.cue, d.make(), false);
            foreach (var d in LoopDefs) Write("Loop" + d.loop, d.make(), true);
            AssetDatabase.Refresh();
            bank.cues = CueDefs.Select(d => new CueClip { cue = d.cue, clip = C("Cue" + d.cue), volume = d.volume, pitchJitter = d.jitter }).ToArray();
            bank.loops = LoopDefs.Select(d => new LoopClip { loop = d.loop, clip = C("Loop" + d.loop), volume = d.volume }).ToArray();
        }

        // ---------- yapı taşları (kısa sesler)
        /// <summary>Perdesi f0'dan f1'e kayan ton.</summary>
        static void Tone(float[] d, float t0, float f0, float f1, float len, Func<double, float> wave, float amp, float attack = 0.004f)
        {
            int s0 = (int)(t0 * SR); double ph = 0;
            for (int i = 0; i < (int)(len * SR) && s0 + i < d.Length; i++)
            {
                float t = (float)i / SR, k = t / len;
                ph += Mathf.Lerp(f0, f1, k) / SR;
                d[s0 + i] += wave(ph) * amp * Mathf.Min(1, t / attack) * (1 - k) * (1 - k);
            }
        }

        static void NoiseBurst(float[] d, float t0, float len, float amp, float decay, float lp = 20000, float hp = 0)
        {
            var b = new float[(int)(len * SR)];
            for (int i = 0; i < b.Length; i++) b[i] = Noise() * Mathf.Exp(-(float)i / SR / decay);
            if (lp < 20000) LowPass(b, lp);
            if (hp > 0) b = HighPassed(b, hp);
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < b.Length && s0 + i < d.Length; i++) d[s0 + i] += b[i] * amp;
        }

        static void Click(float[] d, float t0, float amp)
        {
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < 500 && s0 + i < d.Length; i++)
                d[s0 + i] += (Mathf.Sin(2 * Mathf.PI * 2600 * i / SR) * 0.6f + Noise() * 0.4f) * Mathf.Exp(-i / 60f) * amp;
        }

        static void Thud(float[] d, float t0, float freq, float decay, float amp)
        {
            int s0 = (int)(t0 * SR); double ph = 0;
            for (int i = 0; i < (int)(decay * 6 * SR) && s0 + i < d.Length; i++)
            {
                float t = (float)i / SR;
                ph += freq * (1 + 1.5f * Mathf.Exp(-t / 0.02f)) / SR;
                d[s0 + i] += Sin(ph) * Mathf.Exp(-t / decay) * amp;
            }
        }

        static void Bell(float[] d, float t0, float freq, float amp, float decay = 0.6f)
        {
            int s0 = (int)(t0 * SR);
            for (int i = 0; i < (int)(decay * 5 * SR) && s0 + i < d.Length; i++)
            {
                float t = (float)i / SR;
                d[s0 + i] += (Mathf.Sin(2 * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * freq * 2.76f * t) * Mathf.Exp(-t / (decay * 0.3f))
                              + 0.2f * Mathf.Sin(2 * Mathf.PI * freq * 5.4f * t) * Mathf.Exp(-t / (decay * 0.15f))) * Mathf.Exp(-t / decay) * amp * Mathf.Min(1, t / 0.002f);
            }
        }

        static float[] Crush(float[] d, int levels)
        {
            float hold = 0;
            for (int i = 0; i < d.Length; i++) { if (i % 4 == 0) hold = Mathf.Round(d[i] * levels) / levels; d[i] = hold; }
            return d;
        }

        static float[] Pings(int[] notes, float gap = 0.12f)
        {
            var d = Buffer(gap * notes.Length + 0.8f);
            for (int i = 0; i < notes.Length; i++) Bell(d, i * gap, Hz(notes[i]), 0.6f, 0.25f);
            return d;
        }

        static float[] Arpeggio(int[] notes, float gap)
        {
            var d = Buffer(gap * notes.Length + 1.2f);
            for (int i = 0; i < notes.Length; i++) Note(d, i * gap, Hz(notes[i]), i == notes.Length - 1 ? 0.4f : 0.06f, Bright, 0.5f, 0.004f, i == notes.Length - 1 ? 0.5f : 0.12f);
            Echo(d, 0.13f, 0.2f);
            return d;
        }

        static float[] Swell(int[] notes)
        {
            var d = Buffer(1.8f);
            foreach (var m in notes) Note(d, 0, Hz(m), 0.8f, ph => Sin(ph) * 0.8f + Sin(ph * 2) * 0.2f, 0.3f, 0.35f, 0.5f);
            Echo(d, 0.17f, 0.25f);
            return d;
        }

        static float[] Chirp(float f0, float f1, float len, bool sine = false)
        {
            var d = Buffer(len + 0.05f);
            Tone(d, 0, f0, f1, len, sine ? (Func<double, float>)Sin : ph => Mathf.Clamp(Sin(ph) * 1.8f, -1, 1), 0.7f);
            return d;
        }

        static float[] Radio(float len, bool voice)
        {
            var d = Buffer(len + 0.15f);
            NoiseBurst(d, 0, 0.06f, 0.8f, 0.03f, 4000, 500);            // açılış cızırtısı
            if (voice)
            {
                // Anlaşılmaz konuşma: değişken bant geçiren gürültü + hece zarfı
                float a = 0, b = 0;
                for (int i = (int)(0.06f * SR); i < (int)((len - 0.05f) * SR); i++)
                {
                    float t = (float)i / SR, cut = 600 + 900 * (0.5f + 0.5f * Mathf.Sin(t * 23)), k = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
                    a += k * (Noise() - a); b += k * (a - b);
                    d[i] += (a - b) * 1.6f * Mathf.Max(0, Mathf.Sin(t * 2 * Mathf.PI * 4.3f));
                }
            }
            else Tone(d, 0.07f, 1400, 1400, 0.12f, Sin, 0.4f);
            NoiseBurst(d, len - 0.05f, 0.08f, 0.6f, 0.04f, 4000, 500);  // kapanış
            return HighPassed(d, 300);
        }

        static float[] Impact(float freq, float decay, float noise)
        {
            var d = Buffer(decay * 5);
            Thud(d, 0, freq, decay, 0.9f);
            NoiseBurst(d, 0, 0.12f, noise, 0.03f, 3000);
            return d;
        }

        static float[] Crash(float len)
        {
            var d = Buffer(len + 0.3f);
            Thud(d, 0, 70, 0.2f, 0.8f);
            NoiseBurst(d, 0, len, 0.8f, len * 0.35f, 6000);
            for (int k = 0; k < 8; k++) Click(d, 0.03f + (float)rnd.NextDouble() * len * 0.8f, 0.4f);
            return d;
        }

        static float[] Explosion(float len)
        {
            var d = Buffer(len + 0.3f);
            Thud(d, 0, 45, 0.35f, 1f);
            NoiseBurst(d, 0, len, 1f, len * 0.3f, 1400);
            NoiseBurst(d, 0.02f, len * 0.6f, 0.4f, len * 0.15f, 8000, 2000);
            return d;
        }

        // ---------- arayüz
        static float[] UiClick() { var d = Buffer(0.06f); Click(d, 0, 0.8f); Thud(d, 0, 220, 0.01f, 0.4f); return d; }
        static float[] UiHover() { var d = Buffer(0.03f); for (int i = 0; i < d.Length; i++) d[i] = Mathf.Sin(2 * Mathf.PI * 4200 * i / SR) * Mathf.Exp(-i / 40f); return d; }
        static float[] UiDeny()
        {
            var d = Buffer(0.4f);
            Note(d, 0, 140, 0.08f, Sq, 0.4f, 0.003f, 0.03f); Note(d, 0.13f, 120, 0.1f, Sq, 0.4f, 0.003f, 0.04f);
            LowPass(d, 1800); return d;
        }
        static float[] UiConfirm() { var d = Buffer(0.5f); Bell(d, 0, Hz(84), 0.4f, 0.15f); Bell(d, 0.07f, Hz(91), 0.4f, 0.2f); return d; }

        static float[] RegionBlip(bool radio)
        {
            var d = Buffer(0.45f);
            Bell(d, 0, 1175, 0.6f, 0.12f); Bell(d, 0.05f, 1760, 0.35f, 0.12f);
            if (!radio) return d;
            // İzlenmeyen bölge: telsiz bandı, cızırtı, kekeleme
            for (int i = 0; i < d.Length; i++) d[i] = d[i] * (0.7f + 0.3f * Mathf.Sign(Mathf.Sin(i * 0.02f))) + Noise() * 0.12f * Mathf.Exp(-(float)i / SR / 0.25f);
            LowPass(d, 2400);
            return HighPassed(Crush(d, 6), 400);
        }

        // ---------- kriz masası
        static float[] Modem()
        {
            var d = Buffer(1.3f);
            Tone(d, 0, 2100, 2100, 0.25f, Sin, 0.35f);
            for (int k = 0; k < 14; k++) Note(d, 0.28f + k * 0.045f, k % 2 == 0 ? 1200 : 2200, 0.035f, Sin, 0.3f, 0.002f, 0.01f);
            NoiseBurst(d, 0.95f, 0.25f, 0.2f, 0.1f, 5000, 1500);
            Bell(d, 1.05f, Hz(86), 0.4f, 0.2f);                         // bağlandı
            return d;
        }

        static float[] AuditSweep()
        {
            var d = Buffer(0.9f);
            Tone(d, 0, 400, 2600, 0.6f, Tri, 0.5f, 0.05f);
            for (int k = 0; k < 6; k++) Click(d, 0.08f + k * 0.09f, 0.25f);
            Bell(d, 0.62f, Hz(88), 0.35f, 0.15f);
            return d;
        }

        static float[] Relay(bool lift)
        {
            var d = Buffer(1.0f);
            if (lift) { Click(d, 0, 0.6f); Tone(d, 0.05f, 80, 160, 0.4f, Saw, 0.3f, 0.05f); Click(d, 0.45f, 0.5f); }
            else { Click(d, 0, 0.7f); Thud(d, 0.02f, 60, 0.22f, 1f); NoiseBurst(d, 0.02f, 0.3f, 0.5f, 0.06f, 900); Click(d, 0.35f, 0.4f); Click(d, 0.42f, 0.3f); }
            LowPass(d, 7000);
            return d;
        }

        static float[] Veto()
        {
            var d = Buffer(0.9f);
            Thud(d, 0, 75, 0.12f, 1f); NoiseBurst(d, 0, 0.08f, 0.7f, 0.02f, 3000);
            Note(d, 0.12f, 110, 0.35f, Sq, 0.25f, 0.005f, 0.08f);
            LowPass(d, 2500);
            return d;
        }

        static float[] ResearchStart() { var d = Buffer(0.6f); Click(d, 0, 0.4f); Tone(d, 0.03f, 500, 900, 0.3f, Sin, 0.4f, 0.02f); return d; }
        static float[] ResearchDone() => Arpeggio(new[] { 69, 74, 78, 81 }, 0.09f);

        static float[] Sabotage()
        {
            var d = Buffer(0.8f);
            Note(d, 0, Hz(81), 0.5f, Tri, 0.5f, 0.01f, 0.1f);
            int seg = (int)(0.035f * SR);
            for (int k = 0; k < 6; k++) { int src = (int)(0.05f * SR), dst = (int)((0.12f + k * 0.06f) * SR); for (int i = 0; i < seg; i++) d[dst + i] = d[src + i] * (k % 2 == 0 ? 1 : -0.6f); }
            NoiseBurst(d, 0.1f, 0.4f, 0.25f, 0.15f, 6000, 1000);
            return Crush(d, 7);
        }

        static float[] VerifyFake() { var d = Pings(new[] { 81, 76, 72 }, 0.1f); Note(d, 0.33f, 130, 0.2f, Sq, 0.15f, 0.005f, 0.05f); return d; }
        static float[] StaticShort() { var d = Buffer(0.5f); NoiseBurst(d, 0, 0.4f, 0.6f, 0.12f, 5000, 600); return d; }

        // Hediye: rahatsız edici derecede güzel müzik kutusu
        static float[] GiftMelody()
        {
            var d = Buffer(3.2f);
            int[] notes = { 77, 81, 84, 89, 88, 84, 86, 89 };
            float[] at = { 0, 0.25f, 0.5f, 0.75f, 1.1f, 1.35f, 1.6f, 2.0f };
            for (int i = 0; i < notes.Length; i++) Bell(d, at[i], Hz(notes[i]), 0.45f, i == notes.Length - 1 ? 0.9f : 0.5f);
            Echo(d, 0.21f, 0.3f);
            return d;
        }

        static float[] GiftReject() { var d = Buffer(0.8f); Thud(d, 0, 65, 0.15f, 0.9f); Note(d, 0.05f, Hz(50), 0.3f, Tri, 0.3f, 0.01f, 0.15f); return d; }

        static float[] LogBlip() { var d = Buffer(0.06f); Note(d, 0, 440, 0.025f, ph => Sq(ph) * 0.6f + Sin(ph) * 0.4f, 0.6f, 0.002f, 0.01f); return d; }

        static float[] Heartbeat()
        {
            int len = (int)(0.9f * SR);
            var d = new float[len];
            Thud(d, 0, 55, 0.06f, 1f); Thud(d, 0.22f, 50, 0.07f, 0.7f);
            LowPass(d, 300);
            return d;
        }

        static float[] Ticks(float interval, float seconds)
        {
            var d = new float[(int)(seconds * SR)];
            for (float t = 0; t < seconds - 0.01f; t += interval)
            {
                int s0 = (int)(t * SR);
                for (int i = 0; i < 700 && s0 + i < d.Length; i++) d[s0 + i] += (Noise() * 0.5f + Mathf.Sin(2 * Mathf.PI * 3400 * i / SR) * 0.5f) * Mathf.Exp(-i / 90f);
            }
            return d;
        }

        // ---------- sonlar
        static float[] EndAlign()
        {
            var d = Buffer(5f);
            int[] motif = { 74, 81, 78, 76, 81, 86 };   // motif majöre çözülür (Fa → Fa#)
            for (int i = 0; i < motif.Length; i++) Bell(d, i * 0.32f, Hz(motif[i]), 0.5f, i == motif.Length - 1 ? 1.5f : 0.6f);
            foreach (var m in new[] { 62, 66, 69, 74 }) Note(d, 1.6f, Hz(m), 2.2f, ph => Sin(ph) * 0.8f + Sin(ph * 2) * 0.15f, 0.2f, 0.8f, 1.2f);
            Echo(d, 0.27f, 0.3f);
            return d;
        }

        static float[] EndErase()
        {
            var d = Buffer(4f);
            int[] motif = { 74, 81, 77, 76, 80, 81, 86 };
            for (int i = 0; i < motif.Length; i++)
            {
                var n = Buffer(0.6f); Note(n, 0, Hz(motif[motif.Length - 1 - i]), 0.05f, Sq, 0.6f * (1 - i / 8f), 0.003f, 0.12f);
                Crush(n, 8 - i);
                int s0 = (int)(i * 0.38f * SR); for (int k = 0; k < n.Length && s0 + k < d.Length; k++) d[s0 + k] += n[k];
            }
            return d;
        }

        static float[] EndTakeover()
        {
            var d = Buffer(3.5f);
            foreach (var m in new[] { 50, 57, 62, 65, 68 }) Note(d, 0, Hz(m), 2.4f, ph => Mathf.Clamp(Saw(ph) * 2.5f, -1, 1), 0.2f, 0.05f, 0.8f);
            NoiseBurst(d, 0, 3f, 0.25f, 1.2f, 3000);
            LowPass(d, 5000);
            return d;
        }

        static float[] EndDissolve()
        {
            var d = Buffer(4.5f);
            foreach (var start in new[] { 0f, 2.2f })
                for (int r = 0; r < 2; r++)
                {
                    float t0 = start + r * 0.45f;
                    int s0 = (int)(t0 * SR);
                    for (int i = 0; i < (int)(0.35f * SR); i++)
                    {
                        float t = (float)i / SR;
                        float ring = (Mathf.Sin(2 * Mathf.PI * 440 * t) + Mathf.Sin(2 * Mathf.PI * 480 * t)) * (Mathf.Sin(2 * Mathf.PI * 20 * t) > 0 ? 1 : 0.2f);
                        d[s0 + i] += ring * 0.3f;
                    }
                }
            LowPass(d, 3000);
            return d;
        }

        static float[] Fanfare()
        {
            var d = Buffer(2.8f);
            int[] notes = { 62, 66, 69, 74, 74, 78, 81 };
            float[] at = { 0, 0.15f, 0.3f, 0.45f, 0.75f, 0.9f, 1.05f };
            for (int i = 0; i < notes.Length; i++)
                Note(d, at[i], Hz(notes[i]), i == notes.Length - 1 ? 0.9f : 0.1f, ph => Mathf.Clamp(Saw(ph) * 1.5f, -1, 1) * 0.6f + Sin(ph) * 0.4f, 0.35f, 0.01f, i == notes.Length - 1 ? 0.6f : 0.1f);
            LowPass(d, 4500);
            Echo(d, 0.2f, 0.25f);
            return d;
        }

        // ---------- baskın
        static float[] Servo()
        {
            var d = Buffer(0.35f);
            Tone(d, 0, 300, 520, 0.3f, ph => Mathf.Clamp(Saw(ph) * 1.5f, -1, 1), 0.25f, 0.03f);
            NoiseBurst(d, 0, 0.3f, 0.15f, 0.1f, 2500);
            for (int k = 0; k < 3; k++) Thud(d, k * 0.09f, 140, 0.02f, 0.35f);   // adımlar
            return d;
        }

        static float[] Cutter()
        {
            var d = Buffer(0.7f);
            for (int i = 0; i < (int)(0.55f * SR); i++)
            {
                float t = (float)i / SR;
                d[i] += (Noise() * 0.6f * (Random01() < 0.3f ? 1 : 0.3f) + Mathf.Sin(2 * Mathf.PI * 3100 * t) * 0.25f) * Mathf.Min(1, t / 0.02f) * Mathf.Exp(-t / 0.35f);
            }
            Click(d, 0.02f, 0.6f);
            return HighPassed(d, 900);
        }

        static float Random01() => (float)rnd.NextDouble();

        static float[] EmpBlast()
        {
            var d = Explosion(0.6f);
            for (int k = 0; k < 10; k++) Click(d, 0.05f + Random01() * 0.5f, 0.35f);   // elektrik çatırtısı
            Tone(d, 0, 1800, 200, 0.35f, Sin, 0.3f);
            return d;
        }

        static float[] HookThrow()
        {
            var d = Buffer(0.6f);
            NoiseBurst(d, 0, 0.3f, 0.5f, 0.12f, 2500, 400);       // fırlatma
            Tone(d, 0, 900, 300, 0.25f, Saw, 0.15f);
            var m = MetalHit(); for (int i = 0; i < m.Length && (int)(0.28f * SR) + i < d.Length; i++) d[(int)(0.28f * SR) + i] += m[i] * 0.7f;
            return d;
        }

        static float[] Melee()
        {
            var d = Buffer(0.5f);
            Tone(d, 0, 200, 420, 0.12f, Saw, 0.3f);
            var m = MetalHit(); for (int i = 0; i < m.Length && (int)(0.12f * SR) + i < d.Length; i++) d[(int)(0.12f * SR) + i] += m[i];
            return d;
        }

        static float[] MetalHit()
        {
            var d = Buffer(0.45f);
            float[] partials = { 523, 1311, 2470, 3600 };
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / SR, v = 0;
                for (int p = 0; p < partials.Length; p++) v += Mathf.Sin(2 * Mathf.PI * partials[p] * t) * Mathf.Exp(-t / (0.12f / (p + 1))) / (p + 1);
                d[i] = v * 0.6f;
            }
            NoiseBurst(d, 0, 0.05f, 0.6f, 0.01f, 8000);
            Thud(d, 0, 110, 0.04f, 0.5f);
            return d;
        }

        static float[] Armor() { var d = MetalHit(); Tone(d, 0.02f, 2400, 1700, 0.25f, Sin, 0.3f); return d; }

        static float[] Repair()
        {
            var d = Buffer(0.7f);
            Tone(d, 0, 600, 1500, 0.5f, Tri, 0.3f, 0.05f);
            for (int k = 0; k < 12; k++) Click(d, Random01() * 0.5f, 0.25f);
            return d;
        }

        static float[] Push() { var d = Buffer(0.4f); NoiseBurst(d, 0, 0.25f, 0.6f, 0.08f, 1500); Thud(d, 0.1f, 90, 0.05f, 0.5f); return d; }

        static float[] CivBreak()
        {
            var d = Crash(0.7f);
            var ext = new float[d.Length + (int)(0.8f * SR)];
            Array.Copy(d, ext, d.Length);
            for (int k = 0; k < 3; k++) Note(ext, 0.55f + k * 0.18f, 1320, 0.07f, Sq, 0.15f * (1 - k * 0.3f), 0.002f, 0.02f);   // uzak hastane çağrısı...
            Tone(ext, 1.1f, 1320, 400, 0.12f, Sq, 0.12f);                                                                    // ...kesilir
            LowPass(ext, 6000);
            return ext;
        }

        static float[] CoreBreak()
        {
            var d = Buffer(3.2f);
            var c = Explosion(1.2f); for (int i = 0; i < c.Length; i++) d[i] += c[i];
            int[] motif = { 86, 81, 80, 76, 77 };
            for (int i = 0; i < motif.Length; i++) Tone(d, 0.5f + i * 0.2f, Hz(motif[i]), Hz(motif[i] - 5), 0.35f, Sq, 0.18f);   // motif kırılıp düşer
            // Binanın gücü gider
            double ph = 0;
            for (int i = (int)(1.6f * SR); i < d.Length; i++)
            {
                float t = (float)i / SR - 1.6f;
                ph += (20 + 90 * Mathf.Exp(-t / 0.4f)) / SR;
                d[i] += Saw(ph) * 0.3f * Mathf.Clamp01(1 - t / 1.5f);
            }
            LowPass(d, 6000);
            return d;
        }

        static float[] Lock(bool fuzzy)
        {
            var d = Buffer(0.25f);
            Click(d, 0, 0.5f);
            if (fuzzy) Note(d, 0.01f, 880, 0.1f, ph => Sin(ph) * (0.6f + 0.4f * Sin(ph / 30)) + 0.3f * Sin(ph * 1.03), 0.4f, 0.003f, 0.06f);
            else Note(d, 0.01f, 880, 0.08f, Sin, 0.45f, 0.003f, 0.05f);
            return d;
        }

        static float[] Breach()
        {
            var d = Explosion(0.8f);
            Thud(d, 0, 50, 0.3f, 0.8f);
            for (int k = 0; k < 10; k++) Click(d, 0.2f + Random01() * 0.6f, 0.3f);   // dökülen moloz
            return d;
        }

        // Sunucu salonu: fan uğultusu, 60 Hz hum, ara ara bip
        static float[] RaidAmbience()
        {
            var d = LoopBuffer(1);
            float h = LoopHz(60), h2 = LoopHz(120);
            var fan = new float[d.Length]; for (int i = 0; i < fan.Length; i++) fan[i] = Noise();
            LowPass(fan, 500); LowPass(fan, 800);
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SR;
                d[i] = Sin(h * t) * 0.18f + Sin(h2 * t) * 0.08f + fan[i] * 1.4f;
            }
            for (int k = 0; k < 5; k++) Note(d, 0.8f + k * 2.1f + Random01(), 1900 + Random01() * 600, 0.04f, Sin, 0.05f, 0.002f, 0.02f);
            return Crossfade(d, SR / 2);
        }

        // ================================================================== kararlar
        static float[] KillSwitch()
        {
            var d = Buffer(3.4f);
            // Şalter: gürültü darbesi + 180 Hz gövde + metal tık
            for (int i = 0; i < (int)(0.4f * SR); i++)
            {
                float t = (float)i / SR;
                d[i] += Noise() * Mathf.Exp(-t / 0.012f) * 0.9f + Mathf.Sin(2 * Mathf.PI * 180 * t) * Mathf.Exp(-t / 0.09f) * 0.8f
                        + Mathf.Sin(2 * Mathf.PI * 3100 * t) * Mathf.Exp(-t / 0.006f) * 0.4f;
            }
            // Şebeke inişi: hum 120 Hz → 18 Hz, sönerek
            double ph = 0;
            int h0 = (int)(0.05f * SR);
            for (int i = h0; i < d.Length; i++)
            {
                float t = (float)(i - h0) / SR;
                float f = 18 + 102 * Mathf.Exp(-t / 0.8f);
                ph += f / SR;
                float env = Mathf.Min(1, t / 0.05f) * Mathf.Clamp01(1 - t / 3.1f);
                d[i] += (Saw(ph) * 0.35f + Sin(ph * 2) * 0.2f) * env;
            }
            // Uzakta kesilen şehirler: röle tıkları
            foreach (var at in new[] { 0.7f, 1.15f, 1.5f })
            {
                int s0 = (int)(at * SR);
                for (int i = 0; i < 600; i++) d[s0 + i] += Noise() * 0.35f * Mathf.Exp(-i / 90f);
            }
            LowPass(d, 6000);
            return d;
        }

        static float[] GlobalArm()
        {
            var d = Buffer(0.8f);
            float a = 0;
            for (int i = 0; i < (int)(0.38f * SR); i++)
            {
                float t = (float)i / SR;
                float cut = 300 + 1600 * t / 0.38f, k = 1 - Mathf.Exp(-2 * Mathf.PI * cut / SR);
                a += k * (Noise() - a);
                d[i] = a * 0.6f * Mathf.Sin(Mathf.PI * t / 0.38f);
            }
            int s0 = (int)(0.4f * SR);
            for (int i = 0; i < (int)(0.3f * SR); i++)
            {
                float t = (float)i / SR;
                d[s0 + i] += Mathf.Sin(2 * Mathf.PI * 2800 * t) * Mathf.Exp(-t / 0.008f) * 0.6f + Mathf.Sin(2 * Mathf.PI * 140 * t) * Mathf.Exp(-t / 0.06f) * 0.7f;
            }
            return d;
        }

        // Perde yırtılır: parazit patlaması, kekeleme, altından alarm
        static float[] VeilTear()
        {
            var d = Buffer(1.6f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = (float)i / SR;
                d[i] = Noise() * Mathf.Min(1, t / 0.06f) * Mathf.Exp(-t / 0.5f) * 0.7f;
            }
            int seg = (int)(0.045f * SR);
            for (int k = 0; k < 8; k++)
            {
                int src = rnd.Next(d.Length / 3), dst = (int)((0.15f + k * 0.07f) * SR);
                for (int i = 0; i < seg && dst + i < d.Length; i++) d[dst + i] = Mathf.Round(d[src + i] * 6) / 6;
            }
            Note(d, 0.35f, 880, 0.5f, ph => Mathf.Clamp(Sin(ph) * 2, -1, 1), 0.35f, 0.2f, 0.3f);
            Note(d, 0.35f, 660, 0.5f, ph => Mathf.Clamp(Sin(ph) * 2, -1, 1), 0.2f, 0.2f, 0.3f);
            return d;
        }
    }
}
