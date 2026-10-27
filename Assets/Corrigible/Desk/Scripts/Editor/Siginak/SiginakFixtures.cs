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
    /// (K3 dizisi oynar). V.015-yaralı — V.014'ten devam, baskın başarısız, Alfa yaralı (figür S-07'de).
    /// K6 — V.014'te baskın başarısız (fon 62 → 50, Alfa 2 gün yaralı) ve bir YAPILAMADI satırı; şalter indirilince
    /// V.014 → 015 raporu K6b'deki sayıları verir.
    /// </summary>
    public static class SiginakFixtures
    {
        static GameController Game => UnityEngine.Object.FindFirstObjectByType<GameController>();

        static void Raid(Simulation sim, string sector, bool success = true, int down = 0)
        {
            var r = sim.StartLiberationRaid(sector);
            if (!r.Ok) throw new InvalidOperationException(sector + ": " + r.Msg);
            r = sim.ResolveRaid(new RaidResult { Success = success, Down = down, Turns = 6 });
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
        /// V.014'ün sığınağı (PROMPT §7): yaşam SV1 2/2, jeneratör SV1 0/2, atölye SV3 2/6 (enerjisiz), revir hasarlı, boşta 4,
        /// topluluk 8/8, erzak 31/40, malzeme 19, fon 62,0. Enerjisiz hâl kurallarla oluşamadığından (Soru 1) yalnız bellekte
        /// kurulur; kayda yazılmaz, simülasyon kodu değişmez.
        /// </summary>
        public static void ApplyShelterV014(Simulation sim)
        {
            var s = sim.Shelter;
            s.Population = 8;
            s.Room(ShelterLogic.Living).Level = 1; s.Room(ShelterLogic.Living).Workers = 2;
            s.Room(ShelterLogic.Generator).Level = 1; s.Room(ShelterLogic.Generator).Workers = 0;
            s.Room(ShelterLogic.Workshop).Level = 3; s.Room(ShelterLogic.Workshop).Workers = 2;
            s.Room(ShelterLogic.Infirmary).Level = 0; s.Room(ShelterLogic.Infirmary).Workers = 0;
            s.Supplies = 31; s.Materials = 19; s.Starving = false;
            s.LastSuppliesChange = 0.4; s.LastMaterialsChange = 0;
            sim.S.Fon = 62;
        }

        /// <summary>V.014'ten V.019: Doğu Avrupa'nın iki sektörü daha; bölge vardiya 019'da özgürleşir.</summary>
        public static void ApplyV019(Simulation sim)
        {
            Raid(sim, "eeu_2");
            StepTo(sim, 18);
            Raid(sim, "eeu_3");
        }

        /// <summary>V.014'ten V.015: baskın başarısız, bir üye düştü (Alfa 2 gün yaralı).</summary>
        public static void ApplyInjured(Simulation sim)
        {
            StepTo(sim, 14);
            Raid(sim, "eeu_2", false, 1);
        }

        /// <summary>V.014'ün kendi vardiyasında baskın başarısız: bir üye düştü, Alfa 2 gün yaralı (K6 öncesi).</summary>
        public static void ApplyFailedRaid(Simulation sim) => Raid(sim, "eeu_2", false, 1);

        /// <summary>
        /// Play içinde: <paramref name="name"/> = “V014”, “K6”, “V019” (V.014 yüklüyken) ya da “injured” (V.014 yüklüyken).
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
