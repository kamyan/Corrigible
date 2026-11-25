using System;
using System.Linq;
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
    /// deneme öncesi yedek alınmalıdır. Kartlı pano kipinde (Revizyon 3; yeni oyunun varsayılanı) gün oyunun komut yoluyla
    /// ilerler: her vardiya <c>PrintShift</c> + <c>FileShift(ReceiptId)</c> (kayda <c>board_print</c> / <c>board_file</c> girer);
    /// plan geçersizse ya da fiş reddedilirse fikstür nedeniyle durur. Devir fişinin (R19c) sunum fikstürleri
    /// <see cref="HandoverSlipFixtures"/>'tedir.
    /// V.014 — Batı Avrupa özgür, Doğu Avrupa cephe 1/3, ekip hazır; sığınak PROMPT §7'deki gibi (bellekte). V.019 — V.014'ten devam, Doğu Avrupa özgürleşir
    /// (K3 dizisi oynar). V.015-yaralı — V.014'ten devam, baskın başarısız, Teknisyen düştü (tesiste kaldı; yerine yedek çıkar).
    /// K6 — V.014'te baskın başarısız (fon 62 → 50, Teknisyen düştü) ve bir YAPILAMADI satırı; şalter indirilince
    /// V.014 → 015 raporu çıkar. V.031 (Revizyon 2) — beş bölge özgür (Batı Avrupa, Doğu Avrupa, Orta Doğu, Afrika, Rusya),
    /// cephe Hindistan 1/3. V.015-karşı saldırı — V.014'ten devam, alarm eşiğe varır, Doğu Avrupa'nın 01'i yeniden işgal.
    /// Bölge adları dünya haritasınındır (anahtar 0). Üretilmiş haritada (anahtar 1) aynı adımlar paket kimliğiyle oynanır:
    /// istenen paket sınırda değilse sınırdaki ilk bölge seçilir (<see cref="Next"/>), cephe sektörleri o bölgenindir. Yakalama
    /// setleri tohumu sabitler (Rev3CaptureSet: 79), yani aynı harita ve aynı sıra.
    /// </summary>
    public static class SiginakFixtures
    {
        static GameController Game => UnityEngine.Object.FindFirstObjectByType<GameController>();

        /// <summary>
        /// Baskın emri ve oda zinciriyle sonucu (<see cref="RaidChainFixture"/>; tek kip: sonuç elle verilmez). Başarısızlıkta giriş
        /// odasında geri çekilinir. Düşenler kadronun başından (önce Teknisyen); tesiste düşüp çıkarılmayan ölür. Bekleyen sunucu ödülü
        /// (bölgenin son sektörü) tarifle seçilir: kartlı oyunda sonraki emri kilitler.
        /// </summary>
        static void Raid(Simulation sim, string sector, bool success = true, int down = 0)
        {
            var r = sim.StartLiberationRaid(sector);
            if (!r.Ok) throw new InvalidOperationException(sector + ": " + r.Msg);
            var fates = new int[Math.Max(0, down)];
            for (int i = 0; i < fates.Length; i++) fates[i] = RaidChainFixture.Dies;
            RaidChainFixture.Resolve(sim, success, fates);
            RaidChainFixture.ChooseReward(sim);
        }

        /// <summary>
        /// Gün ilerletir. Kartlı pano kipinde vardiya yalnız devir fişiyle kapanır (<c>Step</c> fişsiz ilerlemez): basılı fiş yoksa
        /// <c>PrintShift</c>, sonra <c>FileShift(ReceiptId)</c> (oyunun kendi komutları, kayda yazılır; RNG ve gün oyundaki gibi).
        /// Eski yolda <c>Step</c>. Basım ya da dosyalama reddedilirse ya da gün ilerlemezse (plan geçersiz, harekât bitti, baskın
        /// sürüyor) sonsuz döngü yerine nedeniyle hata verir.
        /// </summary>
        static void StepTo(Simulation sim, int day)
        {
            while (sim.S.Day < day)
            {
                int before = sim.S.Day;
                if (sim.Board != null)
                {
                    if (!sim.Board.Printed)
                    {
                        var printed = sim.PrintShift();
                        if (!printed.Ok) throw new InvalidOperationException($"Gün {before}: devir fişi basılamadı ({printed.Msg}).");
                    }
                    var filed = sim.FileShift(sim.Board.ReceiptId);
                    if (!filed.Ok) throw new InvalidOperationException($"Gün {before}: devir fişi dosyalanamadı ({filed.Msg}).");
                }
                else sim.Step();
                if (sim.S.Day == before)
                    throw new InvalidOperationException($"Gün {before} ilerlemiyor (baskın sürüyor ya da harekât bitti).");
            }
        }

        /// <summary>
        /// Sıradaki cephe: <paramref name="preferred"/> sınırdaysa o (dünya haritasında hep öyle), değilse sınırdaki ilk bölge
        /// (<c>LiberationFrontier</c>, <c>GameData.Regions</c> sırası). Sınır boşsa <paramref name="preferred"/> (seçim reddedilir).
        /// </summary>
        public static string Next(Simulation sim, string preferred)
        {
            var frontier = sim.LiberationFrontier.ToList();
            return frontier.Contains(preferred) ? preferred : frontier.FirstOrDefault() ?? preferred;
        }

        /// <summary>Açık cephe (V.014'ten devam eden fikstürler: dünya haritasında Doğu Avrupa).</summary>
        static string Front(Simulation sim) => sim.S.Liberation.CurrentRegion;

        /// <summary>Yeni oyundan V.014: üç baskınla Batı Avrupa, cephe Doğu Avrupa, 01 geri alındı; gün 13 (vardiya 014).</summary>
        public static void ApplyV014(Simulation sim)
        {
            if (sim.S.Day != 0) throw new InvalidOperationException("Fikstür yeni bir harekât ister (gün 0).");
            Raid(sim, Simulation.HQ + "_1");
            StepTo(sim, 3);
            Raid(sim, Simulation.HQ + "_2");
            StepTo(sim, 6);
            Raid(sim, Simulation.HQ + "_3");
            string second = Next(sim, "eeu");
            var r = sim.SelectLiberationRegion(second);
            if (!r.Ok) throw new InvalidOperationException(r.Msg);
            StepTo(sim, 9);
            Raid(sim, second + "_1");
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
            // Atölye SV3: Madenci ve Sis uzmanı kadroda
            sim.SyncSpecialists();
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
            string[] order = { Simulation.HQ, "eeu", "me", "af", "ru" };
            int day = 0;
            void Keep()
            {
                sim.S.Fon = Math.Max(sim.S.Fon, 60);
                sim.S.Alarm = 0;
                sim.Shelter.Supplies = Math.Max(sim.Shelter.Supplies, 20);
            }
            for (int k = 0; k < order.Length; k++)
            {
                string region = order[k];
                if (k > 0)
                {
                    region = Next(sim, region);
                    var r = sim.SelectLiberationRegion(region);
                    if (!r.Ok) throw new InvalidOperationException(region + ": " + r.Msg);
                }
                for (int i = 1; i <= 3; i++)
                {
                    Keep();
                    Raid(sim, region + "_" + i);
                    // Son bölgenin son baskını bir gün arayla: Hindistan'ın 01'i vardiya 030'da, ekip 031'de hazır
                    day += k == order.Length - 1 && i == 3 ? 1 : 2;
                    Keep();
                    StepTo(sim, day);
                }
            }
            string last = Next(sim, "in");
            var front = sim.SelectLiberationRegion(last);
            if (!front.Ok) throw new InvalidOperationException(last + ": " + front.Msg);
            Keep();
            Raid(sim, last + "_1");
            Keep();
            StepTo(sim, 30);
            sim.S.Alarm = 7;
            sim.S.Fon = 88.5;
        }

        /// <summary>V.014'ten V.015: alarm eşiğe varır, YZ karşı saldırır: Doğu Avrupa'nın 01'i yeniden işgal altında. Tesis savunması
        /// açıkken kanat önce kuşatılır; fikstür kanadı komutla bırakır (siege_yield), kanat düşer (anlık karşı saldırının sonucu).</summary>
        public static void ApplyCounterattack(Simulation sim)
        {
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerShift;
            StepTo(sim, 14);
            if (sim.DefenseOn)
            {
                if (sim.PendingSiege == null) throw new InvalidOperationException("Karşı saldırı olmadı (kuşatma yok).");
                var yielded = sim.YieldSector();
                if (!yielded.Ok) throw new InvalidOperationException("Kanat bırakılamadı: " + yielded.Msg);
            }
            if (sim.S.LastCounterattack == null) throw new InvalidOperationException("Karşı saldırı olmadı.");
        }

        /// <summary>V.014'ten V.019: Doğu Avrupa'nın iki sektörü daha; bölge vardiya 019'da özgürleşir.</summary>
        public static void ApplyV019(Simulation sim)
        {
            Raid(sim, Front(sim) + "_2");
            StepTo(sim, 18);
            Raid(sim, Front(sim) + "_3");
        }

        /// <summary>V.014'ten V.015: baskın başarısız, Teknisyen düştü (yerine yedek çıkar).</summary>
        public static void ApplyInjured(Simulation sim)
        {
            StepTo(sim, 14);
            Raid(sim, Front(sim) + "_2", false, 1);
        }

        /// <summary>V.014'ün kendi vardiyasında baskın başarısız: Teknisyen düştü (K6 öncesi).</summary>
        public static void ApplyFailedRaid(Simulation sim) => Raid(sim, Front(sim) + "_2", false, 1);

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
