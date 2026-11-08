using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Tests
{
    /// <summary>
    /// Baskının yeri ve olay sesleri (Docs/BASKIN-MEKANLARI.md Katman 7 ve 8): her bölgenin bina sesi (ortam yatağı) var, olay
    /// seslerinin (67–88) klibi var ve telsiz bandından geçer. <see cref="AudioDirector.PlayCue"/> eksik klipte sessizce döndüğü
    /// için unutulan bir giriş olayı ya da bölgeyi sessiz bırakırdı; bu testler onu yakalar.
    /// </summary>
    public class SoundBankTests
    {
        const string BankPath = "Assets/Corrigible/Settings/SoundBank.asset";
        const string BoardSource = "Assets/Corrigible/Game/Raid/RaidBoard3D.cs";
        const int FirstEvent = 67, LastEvent = 88;

        static SoundBank Bank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            Assert.IsNotNull(bank, BankPath);
            return bank;
        }

        static IEnumerable<Cue> EventCues => Enumerable.Range(FirstEvent, LastEvent - FirstEvent + 1).Select(i => (Cue)i);

        /// <summary>Her bölgenin kendi kaydı ve yatağı var; kayıtlar yalnız bilinen bölgeler için ve birer kez.</summary>
        [Test]
        public void EveryRegion_HasASiteWithABed()
        {
            var bank = Bank();
            foreach (var r in GameData.Regions)
            {
                var site = bank.Site(r.Id);
                Assert.AreEqual(r.Id, site.region, $"{r.Id}: SoundBank.sites'ta kaydı yok");
                Assert.IsNotNull(site.bed, $"{r.Id}: yatak klibi yok");
                Assert.That(site.bedVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"{r.Id}: yatak düzeyi");
            }
            var ids = GameData.Regions.Select(r => r.Id).ToHashSet();
            foreach (var s in bank.sites) Assert.IsTrue(ids.Contains(s.region), $"bilinmeyen bölge kaydı: '{s.region}'");
            Assert.AreEqual(bank.sites.Length, bank.sites.Select(s => s.region).Distinct().Count(), "bir bölgenin iki kaydı var (ikincisi hiç çalmaz)");
            // Bilinmeyen bölge ve null boş kayıt verir: uğultuya düşer
            Assert.IsNull(bank.Site(null).bed);
            Assert.IsNull(bank.Site("yok").bed);
        }

        /// <summary>Hiçbir kayıt kayıp bir klibe bakmıyor; ayrıntı varsa düzeyi ve aralığı anlamlı.</summary>
        [Test]
        public void NoSite_PointsToAMissingClip()
        {
            foreach (var s in Bank().sites)
            {
                Assert.IsNotNull(s.bed, $"{s.region}: yatak klibi kayıp");
                Assert.IsNotEmpty(AssetDatabase.GetAssetPath(s.bed), $"{s.region}: yatak bir asset değil");
                Assert.IsNotNull(s.details, $"{s.region}: ayrıntı dizisi null");
                for (int i = 0; i < s.details.Length; i++)
                {
                    Assert.IsNotNull(s.details[i], $"{s.region}: {i}. ayrıntı kayıp");
                    Assert.IsNotEmpty(AssetDatabase.GetAssetPath(s.details[i]), $"{s.region}: {i}. ayrıntı bir asset değil");
                }
                if (s.details.Length == 0) continue;
                Assert.That(s.detailVolume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), $"{s.region}: ayrıntı düzeyi");
                Assert.That(s.detailGap.x, Is.GreaterThanOrEqualTo(1f), $"{s.region}: ayrıntı aralığı");
                Assert.That(s.detailGap.y, Is.GreaterThanOrEqualTo(s.detailGap.x), $"{s.region}: ayrıntı aralığı");
            }
        }

        /// <summary>
        /// Yataklar 20–24 sn'lik tek kanallı döngüler, döngü sesleri gibi içe aktarılır (sıkıştırılmış, arka planda yüklenir);
        /// genel uğultu yedek olarak durur ve sürekli çalan döngü sayısı artmaz (her LoopCue sürekli çalan bir kaynak açar).
        /// </summary>
        [Test]
        public void Beds_AreLongMonoLoops_AndTheHumStaysAsTheFallback()
        {
            var bank = Bank();
            foreach (var bed in bank.sites.Select(s => s.bed).Where(b => b != null).Distinct())
            {
                Assert.That(bed.length, Is.InRange(20f, 24.01f), bed.name);
                Assert.AreEqual(1, bed.channels, bed.name);
                Assert.AreEqual(44100, bed.frequency, bed.name);
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(bed));
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, imp.defaultSampleSettings.loadType, bed.name);
                Assert.AreEqual(AudioCompressionFormat.Vorbis, imp.defaultSampleSettings.compressionFormat, bed.name);
                Assert.IsTrue(imp.loadInBackground, bed.name);
            }
            Assert.IsNotNull(bank.Loop(LoopCue.RaidAmbience).clip, "yatağı olmayan bölgenin yedeği: genel uğultu");
            CollectionAssert.AreEquivalent(new[] { LoopCue.RaidAmbience, LoopCue.RadioHiss }, (LoopCue[])Enum.GetValues(typeof(LoopCue)));
        }

        /// <summary>Olay sesleri 67–88 tanımlı (sayılar asset'te saklı, adlar sabit), her birinin kısa klibi var ve bankada birer kez.</summary>
        [Test]
        public void EventCues_HaveShortClips()
        {
            var names = new[]
            {
                "EvCo2Burst", "EvCloudFade", "EvShelfRoll", "EvShelfJam", "EvGasFill", "EvGasVent", "EvGasFlash", "EvCeilingCrack",
                "EvCeilingFall", "EvLiftMotor", "EvLiftDing", "EvFreeze", "EvIce", "EvTurnstile", "EvWeldArc", "EvArmClank", "EvPhase",
                "EvDrain", "EvValveWhistle", "EvTubeLaunch", "EvTubeLand", "EvOilSlide",
            };
            CollectionAssert.AreEqual(names, EventCues.Select(c => Enum.IsDefined(typeof(Cue), c) ? c.ToString() : "(boş) " + (int)c).ToArray());
            var bank = Bank();
            foreach (var cue in EventCues)
            {
                var c = bank.Cue(cue);
                Assert.IsNotNull(c.clip, $"{(int)cue} {cue}: klip yok (olay sessiz kalır)");
                Assert.That(c.volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), cue.ToString());
                // Hızlı makine modunda sesler üst üste biner: olay sesi 1,2 sn'yi geçmez
                Assert.That(c.clip.length, Is.LessThanOrEqualTo(1.2f), cue.ToString());
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(c.clip));
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, imp.defaultSampleSettings.loadType, cue.ToString());
            }
            var twice = bank.cues.GroupBy(c => c.cue).Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToArray();
            Assert.IsEmpty(twice, "bir sesin iki girişi var (ikincisi hiç çalmaz)");
        }

        /// <summary>
        /// D5 = c: enum'daki her sesin klibi ve duyulur düzeyi var, kısa sesler yüklemede açılır. <see cref="AudioDirector.PlayCue"/> eksik
        /// klipte sessizce döndüğü için yeni bir <see cref="Cue"/> bankaya eklenmeden sessiz kalamaz.
        /// </summary>
        [Test]
        public void EveryCue_HasAnAudibleClip()
        {
            var bank = Bank();
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
            {
                var c = bank.Cue(cue);
                Assert.IsNotNull(c.clip, $"{(int)cue} {cue}: klip yok (ses sessiz kalır)");
                Assert.That(c.volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), cue.ToString());
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(c.clip));
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, imp.defaultSampleSettings.loadType, cue.ToString());
            }
        }

        /// <summary>
        /// Telsiz bandından yalnız saha sesleri geçer: baskın sesleri (31–49) ve bölge olaylarının sesleri (67–88). Kapı, telsiz
        /// (gözcünün bildirimi dahil) ve masa sesleri odada çalar. Olay sesi banda girmezse odada çalıyormuş gibi duyulurdu.
        /// </summary>
        [Test]
        public void FromField_BandsRaidAndEventCuesOnly()
        {
            var fromField = typeof(AudioDirector).GetMethod("FromField", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(fromField, "AudioDirector.FromField");
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
            {
                int n = (int)cue;
                bool expected = n >= 31 && n <= 49 || n >= FirstEvent && n <= LastEvent;
                Assert.AreEqual(expected, (bool)fromField.Invoke(null, new object[] { cue }), $"{n} {cue}");
            }
        }

        /// <summary>
        /// Bölge olayının efekti (RaidBoard3D.EventFx) her olay nesnesinde kendi sesini çalar, oyuncunun eylem seslerini
        /// (duman, EMP, dron atışı) ödünç almaz; çağrı düzeyleri Katman 8'deki eşlemeyle aynı. Yeni bir olay nesnesi sessiz
        /// eklenemez, olay seslerinin hiçbiri boşta kalmaz.
        /// </summary>
        [Test]
        public void EventFx_PlaysEachEventsOwnCue()
        {
            var expected = new Dictionary<string, (string Cue, float Volume)[]>
            {
                ["bulut"] = new[] { ("EvCo2Burst", 1f) }, ["bulut_dagil"] = new[] { ("EvCloudFade", .4f) },
                ["raf"] = new[] { ("EvShelfRoll", 1f) }, ["raf_sikisti"] = new[] { ("EvShelfJam", 1f) },
                ["gaz_dol"] = new[] { ("EvGasFill", 1f) }, ["gaz_bosal"] = new[] { ("EvGasVent", .6f) },
                ["gaz_alev"] = new[] { ("EvGasFlash", .6f) }, ["gaz_parla"] = new[] { ("EvGasFlash", 1f) },
                ["catlak"] = new[] { ("EvCeilingCrack", .8f) }, ["tavan"] = new[] { ("EvCeilingFall", 1f) }, ["toz"] = new[] { ("EvCeilingFall", .5f) },
                ["kabin_kalk"] = new[] { ("EvLiftMotor", 1f) }, ["kabin_in"] = new[] { ("EvLiftDing", 1f) }, ["kabin_bekle"] = new[] { ("EvPhase", .7f) },
                ["donma"] = new[] { ("EvFreeze", 1f) }, ["buz"] = new[] { ("EvIce", .7f) },
                ["turnike"] = new[] { ("EvTurnstile", .8f) },
                ["kaynak"] = new[] { ("EvWeldArc", .8f) }, ["kol"] = new[] { ("EvArmClank", 1f) },
                ["faz"] = new[] { ("EvPhase", .7f) },
                ["emme"] = new[] { ("EvDrain", 1f) },
                ["islik"] = new[] { ("EvValveWhistle", .8f) }, ["tup_uc"] = new[] { ("EvTubeLaunch", 1f), ("EvTubeLand", 1f) },
                ["yag"] = new[] { ("EvOilSlide", .7f) },
            };
            var src = File.ReadAllText(BoardSource);
            int a = src.IndexOf("IEnumerator EventFx(", StringComparison.Ordinal);
            Assert.That(a, Is.GreaterThanOrEqualTo(0), "RaidBoard3D.EventFx bulunamadı");
            // Metodun sonu: switch'ten sonraki ilk metot girintili kapanış
            var close = new Regex(@"\r?\n {8}\}\r?\n").Match(src, src.IndexOf("switch (f.Obj)", a, StringComparison.Ordinal));
            Assert.IsTrue(close.Success, "EventFx'in sonu bulunamadı");
            var body = src.Substring(a, close.Index - a);
            var cases = Regex.Matches(body, "case \"(\\w+)\":").Cast<Match>().ToArray();
            var play = new Regex(@"Sfx\.Play\(Cue\.(\w+)(?:,\s*([0-9.]+)f?)?");
            var found = new Dictionary<string, (string, float)[]>();
            for (int i = 0; i < cases.Length; i++)
            {
                int end = i + 1 < cases.Length ? cases[i + 1].Index : body.Length;
                found[cases[i].Groups[1].Value] = play.Matches(body.Substring(cases[i].Index, end - cases[i].Index)).Cast<Match>()
                    .Select(m => (m.Groups[1].Value, m.Groups[2].Success ? float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 1f)).ToArray();
            }
            CollectionAssert.AreEquivalent(expected.Keys, found.Keys, "EventFx'in olay nesneleri");
            foreach (var kv in expected)
                CollectionAssert.AreEqual(kv.Value.Select(v => $"{v.Cue} {v.Volume:0.##}"), found[kv.Key].Select(v => $"{v.Item1} {v.Item2:0.##}"), kv.Key);
            var used = found.Values.SelectMany(v => v.Select(x => x.Item1)).ToHashSet();
            foreach (var cue in EventCues) Assert.IsTrue(used.Contains(cue.ToString()), $"{cue}: hiçbir olay çalmıyor");
        }

        /// <summary>
        /// Yatak seçimi: bölgenin yatağı sessiz kaynakta başlar, aynı yatak çalıyorsa yeniden başlamaz (aynı binada yeni baskın),
        /// başka binada klip öbür (sessiz) kaynakta değişir; yatağı olmayan ya da bilinmeyen bölgede yatak açılmaz (uğultu çalar).
        /// Kaynakların düzeyi sıfır kalır (Update çalışmaz), boş sahnede duyulmaz.
        /// </summary>
        [Test]
        public void Director_StartsTheBedOnTheSilentSource()
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            var bank = Bank();
            var go = new GameObject("SoundBankTests director") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var d = go.AddComponent<AudioDirector>();
                typeof(AudioDirector).GetField("bank", F).SetValue(d, bank);
                typeof(AudioDirector).GetMethod("Awake", F).Invoke(d, null);
                var beds = (AudioSource[])typeof(AudioDirector).GetField("beds", F).GetValue(d);
                var detail = (AudioSource)typeof(AudioDirector).GetField("detail", F).GetValue(d);
                int Active() => (int)typeof(AudioDirector).GetField("activeBed", F).GetValue(d);
                Assert.AreEqual(2, beds.Length);
                // Yatak ve ayrıntı kaynakları telsiz bandından (uğultu gibi)
                foreach (var s in beds.Append(detail))
                {
                    Assert.AreEqual(bank.fieldThroughRadio, s.GetComponent<AudioHighPassFilter>().enabled, s.name);
                    Assert.AreEqual(bank.radioLowCut, s.GetComponent<AudioHighPassFilter>().cutoffFrequency, s.name);
                    Assert.AreEqual(bank.radioHighCut, s.GetComponent<AudioLowPassFilter>().cutoffFrequency, s.name);
                }
                Assert.IsTrue(beds.All(s => s.loop) && !detail.loop);

                d.SetRaidSite("weu", true);
                Assert.AreEqual(0, Active());
                Assert.AreSame(bank.Site("weu").bed, beds[0].clip);
                Assert.IsTrue(beds[0].isPlaying);

                // ru aynı yatağı paylaşır: aynı kaynak sürer
                beds[0].volume = 0.3f;
                d.SetRaidSite("ru", true);
                Assert.AreEqual(0, Active());

                // Başka bina: klip sessiz kaynakta (B) değişir, çalan (A) kendi klibiyle söner
                d.SetRaidSite("eeu", true);
                Assert.AreEqual(1, Active());
                Assert.AreSame(bank.Site("eeu").bed, beds[1].clip);
                Assert.AreSame(bank.Site("weu").bed, beds[0].clip);

                // Sönmekte olan yatağın binasına dönülürse o kaynak yeniden açılır
                d.SetRaidSite("weu", false);
                Assert.AreEqual(0, Active());

                d.SetRaidSite("yok", true);
                Assert.AreEqual(-1, Active(), "bilinmeyen bölge: yatak yok, uğultu çalar");
                d.SetRaidSite(null, true);
                Assert.AreEqual(-1, Active());
            }
            finally
            {
                // Awake yansımayla çağrıldı: OnDestroy çağrılmayabilir, örnek elle bırakılır (Sfx başka testlerde ölü nesneye gitmesin)
                typeof(AudioDirector).GetProperty("Instance").SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
