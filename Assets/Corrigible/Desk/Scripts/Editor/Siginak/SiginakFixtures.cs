using System;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// PROMPT §7 durum fikstürleri (yalnız editörde, Play içinde): açık yeni bir kurtuluş oyununa mevcut komutları
    /// sırayla uygular (baskın + sonuç, cephe seçimi, gün ilerletme). Simülasyon kodu değişmez; baskın sahnesi yüklenmez
    /// (komutlar doğrudan simülasyona gider, olaylar boşaltılır). Kayıt dosyası bir sonraki komutta güncellenir:
    /// deneme öncesi yedek alınmalıdır.
    /// V.014 — Batı Avrupa özgür, Doğu Avrupa cephe 1/3, ekip hazır; sığınak PROMPT §7'deki gibi (bellekte). V.019 — V.014'ten devam, Doğu Avrupa özgürleşir
    /// (K3 dizisi oynar). V.015-yaralı — V.014'ten devam, baskın başarısız, Teknisyen düştü (yerine yedek çıkar, piyon yatık).
    /// K6 — V.014'te baskın başarısız (fon 62 → 50, Teknisyen düştü) ve bir YAPILAMADI satırı; şalter indirilince
    /// V.014 → 015 raporu çıkar. V.031 (Revizyon 2) — beş bölge özgür (Batı Avrupa, Doğu Avrupa, Orta Doğu, Afrika, Rusya),
    /// cephe Hindistan 1/3. V.015-karşı saldırı — V.014'ten devam, alarm eşiğe varır, Doğu Avrupa'nın 01'i yeniden işgal.
    /// </summary>
    public static class SiginakFixtures
    {
        static GameController Game => UnityEngine.Object.FindFirstObjectByType<GameController>();

        static void Raid(Simulation sim, string sector, bool success = true, int down = 0)
        {
            var r = sim.StartLiberationRaid(sector);
            if (!r.Ok) throw new InvalidOperationException(sector + ": " + r.Msg);
            // Düşen üyeler kadronun başından (önce Teknisyen); ayaktakiler çıktıkları canla ve plakalarıyla döner
            var squad = sim.S.Raid.Params.Squad;
            var result = new RaidResult { Success = success, Down = down, Turns = 6 };
            for (int i = 0; i < squad.Count; i++) { result.Hp.Add(i < down ? 0 : squad[i].Hp); result.Armor.Add(squad[i].Armor); }
            r = sim.ResolveRaid(result);
            if (!r.Ok) throw new InvalidOperationException(sector + " sonuç: " + r.Msg);
        }

        static void StepTo(Simulation sim, int day) { while (sim.S.Day < day) sim.Step(); }

        /// <summary>Yeni oyundan V.014: üç baskınla Batı Avrupa, cephe Doğu Avrupa, 01 geri alındı; gün 13 (vardiya 014).</summary>
        public static void ApplyV014(Simulation sim)
        {
            if (sim.S.Day != 0) throw new InvalidOperationException("Fikstür yeni bir harekât ister (gün 0).");
            Raid(sim, "weu_1");
            StepTo(sim, 3);
            Raid(sim, "weu_2");
            StepTo(sim, 6);
            Raid(sim, "weu_3");
            var r = sim.SelectLiberationRegion("eeu");
            if (!r.Ok) throw new InvalidOperationException(r.Msg);
            StepTo(sim, 9);
            Raid(sim, "eeu_1");
            StepTo(sim, 13);
        }

        /// <summary>
        /// V.014'ün sığınağı (PROMPT §7): yaşam SV1 2/2, telsiz SV1 0/2, atölye SV3 2/6, revir hasarlı, boşta 4,
        /// topluluk 8/8, erzak 22/30, malzeme 19, fon 62,0, alarm 4. Kurallarla bu vardiyada oluşamayacağından yalnız bellekte
        /// kurulur; kayda yazılmaz, simülasyon kodu değişmez.
        /// </summary>
        public static void ApplyShelterV014(Simulation sim)
        {
            var s = sim.Shelter;
            s.Population = 8;
            s.Room(ShelterLogic.Living).Level = 1; s.Room(ShelterLogic.Living).Workers = 2;
            s.Room(ShelterLogic.Radio).Level = 1; s.Room(ShelterLogic.Radio).Workers = 0;
            s.Room(ShelterLogic.Workshop).Level = 3; s.Room(ShelterLogic.Workshop).Workers = 2;
            s.Room(ShelterLogic.Infirmary).Level = 0; s.Room(ShelterLogic.Infirmary).Workers = 0;
            s.Supplies = 22; s.Materials = 19; s.Starving = false;
            s.LastSuppliesChange = -1.4; s.LastMaterialsChange = 0;
            sim.S.Fon = 62;
            // Alarm 4/10: K6'nın başarısız baskını (+3) ve vardiyası (+1) eşiğe varmaz
            sim.S.Alarm = 4;
        }

        /// <summary>
        /// Yeni oyundan V.031 (Revizyon 2 · states.js v031): Batı Avrupa → Doğu Avrupa → Orta Doğu → Afrika → Rusya özgür,
        /// cephe Hindistan, 01 geri alındı; gün 30. Otuz vardiya boyunca kaynak ve alarm kurallarla tutmayacağından her
        /// baskından önce fon, erzak ve alarm bellekte düzeltilir (karşı saldırı ve açlık fikstürü bozmasın); sonunda alarm 7.
        /// </summary>
        public static void ApplyV031(Simulation sim)
        {
            if (sim.S.Day != 0) throw new InvalidOperationException("Fikstür yeni bir harekât ister (gün 0).");
            string[] order = { "weu", "eeu", "me", "af", "ru" };
            int day = 0;
            void Keep()
            {
                sim.S.Fon = Math.Max(sim.S.Fon, 60);
                sim.S.Alarm = 0;
                sim.Shelter.Supplies = Math.Max(sim.Shelter.Supplies, 20);
            }
            foreach (var region in order)
            {
                if (region != "weu")
                {
                    var r = sim.SelectLiberationRegion(region);
                    if (!r.Ok) throw new InvalidOperationException(region + ": " + r.Msg);
                }
                for (int i = 1; i <= 3; i++)
                {
                    Keep();
                    Raid(sim, region + "_" + i);
                    // Son bölgenin son baskını bir gün arayla: Hindistan'ın 01'i vardiya 030'da, ekip 031'de hazır
                    day += region == "ru" && i == 3 ? 1 : 2;
                    Keep();
                    StepTo(sim, day);
                }
            }
            var front = sim.SelectLiberationRegion("in");
            if (!front.Ok) throw new InvalidOperationException("in: " + front.Msg);
            Keep();
            Raid(sim, "in_1");
            Keep();
            StepTo(sim, 30);
            sim.S.Alarm = 7;
            sim.S.Fon = 88.5;
        }

        /// <summary>V.014'ten V.015: alarm eşiğe varır, YZ karşı saldırır; Doğu Avrupa'nın 01'i yeniden işgal altında.</summary>
        public static void ApplyCounterattack(Simulation sim)
        {
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerShift;
            StepTo(sim, 14);
            if (sim.S.LastCounterattack == null) throw new InvalidOperationException("Karşı saldırı olmadı.");
        }

        /// <summary>V.014'ten V.019: Doğu Avrupa'nın iki sektörü daha; bölge vardiya 019'da özgürleşir.</summary>
        public static void ApplyV019(Simulation sim)
        {
            Raid(sim, "eeu_2");
            StepTo(sim, 18);
            Raid(sim, "eeu_3");
        }

        /// <summary>V.014'ten V.015: baskın başarısız, Teknisyen düştü (yerine yedek çıkar).</summary>
        public static void ApplyInjured(Simulation sim)
        {
            StepTo(sim, 14);
            Raid(sim, "eeu_2", false, 1);
        }

        /// <summary>V.014'ün kendi vardiyasında baskın başarısız: Teknisyen düştü (K6 öncesi).</summary>
        public static void ApplyFailedRaid(Simulation sim) => Raid(sim, "eeu_2", false, 1);

        /// <summary>
        /// Play içinde: <paramref name="name"/> = “V014”, “V031”, “K6”, “V019”, “injured” ya da “V015ca” (son üçü V.014 yüklüyken).
        /// K6 bir yükleme gibi bildirilir; ardından en üst seviyedeki atölyeyi geliştirme denenir (teleks YAPILAMADI satırı).
        /// V014 bir “yükleme” gibi bildirilir (dizi oynamaz); V019 ve injured bir komut sonrası gibi (K3 dizisi oynar).
        /// </summary>
        public static string Load(string name)
        {
            if (!Application.isPlaying) return "Play gerekli";
            var game = Game;
            var sim = game != null ? game.Sim : null;
            if (sim == null) return "Kurtuluş oyunu yok";
            bool loaded = false;
            switch (name)
            {
                case "V014": ApplyV014(sim); ApplyShelterV014(sim); loaded = true; break;
                case "V031": ApplyV031(sim); loaded = true; break;
                case "V015ca": ApplyCounterattack(sim); break;
                case "K6": ApplyV014(sim); ApplyShelterV014(sim); ApplyFailedRaid(sim); loaded = true; break;
                case "V019": ApplyV019(sim); break;
                case "injured": ApplyInjured(sim); break;
                default: return "Bilinmeyen fikstür: " + name;
            }
            sim.DrainEvents();
            if (loaded) Raise(game, "GameLoaded");
            Raise(game, "Changed");
            if (name == "K6") UnityEngine.Object.FindFirstObjectByType<Corrigible.Desk.Bridge.LiberationDeskView>()?.UpgradeRoom(ShelterLogic.Workshop);
            return $"{name}: gün {sim.S.Day}, fon {sim.S.Fon:0.0}, cephe {sim.S.Liberation.CurrentRegion}";
        }

        static void Raise(GameController game, string evt)
        {
            var field = typeof(GameController).GetField(evt, BindingFlags.Instance | BindingFlags.NonPublic);
            (field?.GetValue(game) as Action)?.Invoke();
        }
    }
}
