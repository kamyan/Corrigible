using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.EditorTools;
using NUnit.Framework;

namespace Corrigible.Tests
{
    public class LocalizationTests
    {
        // Türkçe harfler (büyük/küçük harf duyarlı: IgnoreCase "ı"yı "I" ile eşleştirir) ya da Türkçe harf içermeyen sık
        // Türkçe kelimeler (çevrilmeden gösterilen kısa metinler için)
        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]");
        static readonly Regex TurkishWords = new Regex(@"\b(bir|gun|fon|ekip|ekibi|bolge|sabotaj|elektrik|denetim|karantina|baskin|hasar|tur|yolda|hazir|ile|daha|yok|var|kez|sira|hedef|yetenek|bekliyor|ayakta|izleme)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        /// <summary>Koddaki, eğitimdeki, prefab ve sahnedeki her kaynak metnin çevirisi var; yer tutucular ve etiketler uyuşuyor.</summary>
        [Test]
        public void Table_CoversAllSources()
        {
            var problems = LocalizationTool.Validate();
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
        }

        /// <summary>
        /// İngilizce oynanan oyunlarda (temel, deneysel + baskın) üretilen hiçbir metin tabloda eksik değil ve Türkçe harf
        /// içermiyor. Kodda Loc'a uğramadan birleştirilen bir metni ya da çevrilmeden gösterilen bir adı yakalar.
        /// </summary>
        [TestCase("manipulator", 41u, false)]
        [TestCase("kahin", 42u, true)]
        [TestCase("solucan", 43u, true)]
        public void EnglishGame_ProducesNoTurkishText(string type, uint seed, bool exp)
        {
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            try
            {
                Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
                var texts = new List<string>();
                var profile = new PlayerProfile();
                for (int game = 0; game < 2; game++)
                {
                    var sim = PlayRandom(type, seed + (uint)game * 100, exp, profile, texts);
                    texts.AddRange(Profiling.LearnedLines(sim.S));
                    profile = Profiling.UpdateProfile(profile, sim.S);
                }
                texts.AddRange(Profiling.ComputeCounters(profile, type).Lines);
                foreach (var r in GameData.Regions) texts.AddRange(new[] { r.Name, r.LatLon });
                foreach (var t in GameData.Traits) texts.AddRange(new[] { t.Name, t.Desc, t.Line });
                foreach (var t in GameData.AiTypes) texts.AddRange(new[] { t.Name, t.Difficulty, t.Blurb });
                foreach (var r in GameData.Research) texts.AddRange(new[] { r.Name, r.Desc, Loc.T(r.Branch) });
                foreach (var e in GameData.Endings) texts.AddRange(new[] { e.Title, e.Text });
                foreach (var g in GameData.Gifts) texts.AddRange(new[] { g.Title, g.Text });
                foreach (var a in GameData.Agencies.Concat(GameData.FakeAgencies)) texts.Add(Loc.T(a));

                Assert.IsEmpty(missing, "Tabloda olmayan metinler:\n" + string.Join("\n", missing.Take(20)));
                var turkish = texts.Where(t => t != null && (TurkishLetters.IsMatch(t) || TurkishWords.IsMatch(t))).Distinct().ToList();
                Assert.IsEmpty(turkish, "Türkçe kalan metinler:\n" + string.Join("\n", turkish.Take(20)));
                Assert.Greater(texts.Count, 100);
            }
            finally { Loc.Missing -= OnMissing; }
        }

        /// <summary>Dil yalnızca metni değiştirir: aynı tohum ve komutlarla iki dilde aynı oyun oynanır.</summary>
        [Test]
        public void Language_DoesNotChangeTheGame()
        {
            string Play()
            {
                var sim = PlayRandom("manipulator", 77, true, null, new List<string>());
                return $"{sim.S.Ending}@{sim.S.Day} fon={sim.S.Fon:R} rng={sim.S.Rng.State} raids={sim.S.Raids}";
            }
            string tr = Play();
            Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
            Assert.AreEqual(tr, Play());
        }

        /// <summary>Rastgele bir oyuncu: her eylemi dener (başarısız olanların mesajları da toplanır), baskınları taktik motorda oynar.</summary>
        static Simulation PlayRandom(string type, uint seed, bool exp, PlayerProfile profile, List<string> texts)
        {
            var sim = Simulation.NewGame(profile, type, seed, null, exp ? new ExpOptions { TimeScale = 1.5 } : null);
            var s = sim.S;
            var rng = new Rng(seed * 7 + 3);
            string[] ids = GameData.Regions.Select(r => r.Id).ToArray();
            void Res(ActResult r) => texts.Add(r.Msg);
            while (s.Ending == null && s.Day < 400)
            {
                if (s.PendingGift != null) Res(sim.ResolveGift(rng.Pick(new[] { Simulation.GiftAccept, Simulation.GiftReject, Simulation.GiftControlled })));
                if (rng.Next() < 0.3) Res(sim.Act(rng.Pick(Actions.All), rng.Pick(ids)));
                if (rng.Next() < 0.05 && s.News.Count > 0) Res(sim.Verify(s.News[0].Id));
                if (s.Research.Cur == null && rng.Next() < 0.2) Res(sim.StartResearch(rng.Pick(GameData.Research).Id));
                if (exp)
                {
                    if (rng.Next() < 0.15) Res(sim.MoveTeam(rng.Pick(Simulation.TeamNames).ToLowerInvariant(), rng.Pick(ids)));
                    if (rng.Next() < 0.02) Res(sim.HireTeam());
                    foreach (var r in s.Regions)
                    {
                        texts.Add(sim.RaidCheck(r.Id));
                        if (sim.RaidCheck(r.Id) != null || rng.Next() < 0.5) continue;
                        Res(sim.StartRaid(r.Id));
                        Res(sim.ResolveRaid(PlayRaid(s.Raid, rng, texts)));
                        break;
                    }
                }
                sim.Step();
                foreach (var e in s.Events) { texts.Add(e.Text); texts.Add(e.Headline?.Text); }
                s.Events.Clear();
                if (s.Advisor != null) texts.Add(s.Advisor.Text);
            }
            texts.AddRange(s.News.Select(n => n.Text));
            texts.AddRange(s.Log.Select(l => l.Text));
            if (s.Day > 3) Res(sim.GlobalKillSwitch());
            return sim;
        }

        static RaidResult PlayRaid(RaidState raid, Rng rng, List<string> texts)
        {
            var B = Baskin.Create(raid.Params);
            for (int guard = 0; guard < 40 && B.Phase == "player"; guard++)
            {
                foreach (var u in B.Units.Where(u => u.Side == 'p' && !u.Dead).ToList())
                {
                    var moves = Baskin.Reachable(B, u);
                    if (moves.Count > 0 && rng.Next() < 0.7) { var m = rng.Pick(moves.ToArray()); Baskin.DoMove(B, u.Id, m.X, m.Y); }
                    var targets = Baskin.Targets(B, u);
                    if (targets.Count > 0) { var t = rng.Pick(targets.ToArray()); Baskin.DoAct(B, u.Id, t.X, t.Y); }
                }
                Baskin.EndTurn(B);
            }
            if (B.Phase == "player") Baskin.Retreat(B);
            texts.AddRange(B.Log.Select(l => l.Text));
            texts.Add(B.Layout == null ? null : Loc.T(B.Layout));
            foreach (var t in Baskin.UnitTypes.Values) texts.Add(t.Name);
            var res = Baskin.Result(B);
            texts.Add(res.Reason);
            return res;
        }
    }
}
