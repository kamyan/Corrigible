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

        /// <summary>Koddaki, prefab ve sahnedeki her kaynak metnin çevirisi var; yer tutucular ve etiketler uyuşuyor.</summary>
        [Test]
        public void Table_CoversAllSources()
        {
            var problems = LocalizationTool.Validate();
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
        }

        /// <summary>
        /// İngilizce oynanan harekâtlarda (sektör baskınları, cephe seçimi, sığınak komutları) üretilen hiçbir metin tabloda eksik
        /// değil ve Türkçe harf içermiyor. Kodda Loc'a uğramadan birleştirilen bir metni ya da çevrilmeden gösterilen bir adı yakalar.
        /// </summary>
        [TestCase(41u)]
        [TestCase(42u)]
        public void EnglishGame_ProducesNoTurkishText(uint seed)
        {
            var missing = new HashSet<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            try
            {
                Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
                var texts = new List<string>();
                var sim = PlayRandom(seed, texts);
                texts.AddRange(sim.S.Moments.Select(m => m.Text));
                foreach (var r in GameData.Regions) texts.Add(r.Name);
                foreach (var e in Baskin.RegionEvents.Values) texts.AddRange(new[] { Loc.T(e.Name), Loc.T(e.Desc), Loc.T(e.Tag) });
                foreach (var r in GameData.Regions) texts.AddRange(new[] { r.Building, r.BuildingNote, r.Sector(0), r.Sector(1), r.Sector(2) });
                foreach (var e in GameData.Endings) texts.AddRange(new[] { e.Title, e.Text });
                foreach (var r in sim.S.Liberation.Regions) texts.AddRange(r.Sectors.Select(x => x.Name));
                foreach (var room in sim.Shelter.Rooms) texts.AddRange(new[] { ShelterLogic.RoomName(room.Id), ShelterLogic.RoomDescription(room.Id) });
                texts.AddRange(sim.S.Teams.Select(t => t.Name));

                Assert.IsEmpty(missing, "Tabloda olmayan metinler:\n" + string.Join("\n", missing.Take(20)));
                var turkish = texts.Where(t => t != null && (TurkishLetters.IsMatch(t) || TurkishWords.IsMatch(t))).Distinct().ToList();
                Assert.IsEmpty(turkish, "Türkçe kalan metinler:\n" + string.Join("\n", turkish.Take(20)));
                Assert.Greater(texts.Count, 100);
            }
            finally { Loc.Missing -= OnMissing; }
        }

        /// <summary>Dil yalnızca metni değiştirir: aynı tohum ve komutlarla iki dilde aynı harekât oynanır.</summary>
        [Test]
        public void Language_DoesNotChangeTheGame()
        {
            string Play()
            {
                var sim = PlayRandom(77, new List<string>());
                return $"{sim.S.Ending}@{sim.S.Day} fon={sim.S.Fon:R} rng={sim.S.Rng.State} raids={sim.S.Raids} occ={sim.Occupation:R}";
            }
            string tr = Play();
            Loc.SetLanguage(Loc.English, Loc.ParseTable(File.ReadAllText(LocalizationTool.TableFile))[Loc.English]);
            Assert.AreEqual(tr, Play());
        }

        /// <summary>
        /// Rastgele bir oyuncu: sığınağı geliştirir ve personel atar (başarısız olanların mesajları da toplanır), sıradaki sektöre
        /// baskın yapıp taktik motorda oynar, bölge bitince sınırdan yeni cephe seçer, vardiyayı bitirir.
        /// </summary>
        static Simulation PlayRandom(uint seed, List<string> texts)
        {
            var sim = Simulation.NewGame(seed);
            var s = sim.S;
            var rng = new Rng(seed * 7 + 3);
            string[] rooms = { ShelterLogic.Living, ShelterLogic.Radio, ShelterLogic.Workshop, ShelterLogic.Infirmary };
            void Res(ActResult r) => texts.Add(r.Msg);
            while (s.Ending == null && s.Day < 300)
            {
                string room = rng.Pick(rooms);
                texts.Add(sim.ShelterUpgradeCheck(room));
                if (rng.Next() < 0.3) Res(sim.ShelterUpgrade(room));
                if (rng.Next() < 0.3) Res(sim.ShelterAssign(room, (int)(rng.Next() * 4)));
                var area = sim.LiberationRegion(s.Liberation.CurrentRegion);
                if (area.Completed)
                {
                    var next = sim.LiberationFrontier.FirstOrDefault();
                    if (next != null) Res(sim.SelectLiberationRegion(next));
                }
                var sector = sim.NextLiberationSector(s.Liberation.CurrentRegion);
                if (sector != null)
                {
                    texts.Add(sim.LiberationRaidCheck(sector.Id));
                    if (sim.LiberationRaidCheck(sector.Id) == null)
                    {
                        Res(sim.StartLiberationRaid(sector.Id));
                        Res(sim.ResolveRaid(PlayRaid(s.Raid, rng, texts)));
                    }
                }
                AnswerDoor(sim, rng, texts);
                sim.Step();
                s.Events.Clear();
            }
            texts.AddRange(s.Door.Log.Where(v => v.Result != null).Select(v => Loc.T(v.Result)));
            return sim;
        }

        /// <summary>Kapıdaki ziyaretçi: metinleri toplanır; rastgele açılır ya da açılmaz (ölümcül kart hiç açılmaz), açık kapıda rastgele bir seçim.</summary>
        static void AnswerDoor(Simulation sim, Rng rng, List<string> texts)
        {
            var card = sim.DoorVisitorCard;
            if (card == null) return;
            texts.AddRange(new[] { Loc.T(card.Who), Loc.T(card.Knock), Loc.T(card.Peek), Loc.T(card.Ask) });
            void Options()
            {
                foreach (var o in sim.DoorOptions()) { texts.Add(Loc.T(o.Label)); texts.Add(sim.DoorAnswerCheck(o.Id)); }
            }
            Options();
            if (card.Deadly || rng.Next() < 0.25) { sim.AnswerDoor(Simulation.DoorRefuse); return; }
            if (!sim.AnswerDoor(Simulation.DoorOpen).Ok || sim.DoorVisitor == null) return;
            texts.Add(Loc.T(card.Opened));
            Options();
            var open = sim.DoorOptions().Where(o => sim.DoorAnswerCheck(o.Id) == null).ToList();
            if (open.Count > 0) sim.AnswerDoor(rng.Pick(open).Id);
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
                // Bölge olayının tahtadaki etiketleri, birim uyarıları ve tur şeridi
                var hz = Baskin.Hazards(B);
                texts.Add(hz.Name);
                texts.AddRange(hz.Marks.Select(m => m.Label));
                texts.AddRange(hz.Warnings.Select(w => w.Text));
                texts.AddRange(hz.Turns);
                Baskin.EndTurn(B);
            }
            if (B.Phase == "player") Baskin.Retreat(B);
            texts.AddRange(B.Log.Select(l => l.Text));
            texts.Add(B.Layout == null ? null : Loc.T(B.Layout));
            texts.Add(Baskin.RoomName(B));
            foreach (var t in Baskin.UnitTypes.Values) texts.Add(t.Name);
            var res = Baskin.Result(B);
            texts.Add(res.Reason);
            return res;
        }
    }
}
