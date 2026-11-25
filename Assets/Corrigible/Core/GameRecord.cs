using System.Collections.Generic;
using System.Globalization;
using System;

namespace Corrigible.Core
{
    /// <summary>Oyuncunun bir komutu ve verildiği gün.</summary>
    [System.Serializable]
    public sealed class SavedCommand
    {
        public int day;
        /// <summary>liberation_region, liberation_raid (a: sektör, b: Teknisyen'in yanına seçilen roller), raidres, shelter_upgrade,
        /// shelter_assign, door_answer (a: seçenek, b: kart), candidate_choose (a: aday, b: yollanacak kişi ya da boş), candidate_pass,
        /// event_decide (a: açık sığınak olayının seçenek kimliği), last_switch (son şalter), raid_room (a: biten tesis odasının raporu,
        /// RoomReport.Encode), server_reward (a: ödülü bekleyen bölge, b: "tarif" | "kazanim"; tesis aşama 8; aşama 10: cevap tarifinde temelin taslak iş emri iptal edilir),
        /// siege_defend ve siege_yield (tesis aşama 11; argümansız: bekleyen kuşatmanın kanadı savunulur ya da bırakılır).</summary>
        public string kind;
        public string a, b;
    }

    /// <summary>
    /// Kayıt dosyası: tohum + denge değerleri + komut geçmişi. Simülasyon deterministik olduğu için komutları tekrar
    /// oynatmak oyunu aynı duruma getirir. Vardiyalar komutların günlerinden ve son günden yeniden kurulur.
    /// </summary>
    [System.Serializable]
    public sealed class GameRecord
    {
        /// <summary>Dosya biçimi. Bu sürümden eski kayıtlar yüklenmez (22: Boşta olmayan yatmayan herkes vardiya kapanışında iyileşir (<c>Balance.boardDutyHeal</c>); 21: tek tahta kaldırıldı; tesis baskını tek kip, her emirde üç oda, sonuç yalnız oda raporlarından; tesis izi hep f8/f9; emirde harita sırası ve görev ek turları yok; sahadaki sızmacı varsayılan açık; 20: üretilmiş haritada paket yerleşimi ve YZ adları; 19: tesis baskınının oda zinciri (raid_room) ve tesis parmak izi (<see cref="facility"/>); bayrak kapalıyken de; tesis kurallarının sonraki değişiklikleri sürümü değil izi artırır (aşama 6 "f2", aşama 8 "f3", aşama 10 önlem defteri "f4", aşama 11 kalıcı tahta ve savunma "f5", aşama 12 Katar ve Yalpa "f6"); 18: üretilmiş harita (<c>Balance.liberationMapGen</c>, <c>WorldMap</c>), haritanın parmak izi (<see cref="map"/>), sınırda <c>Simulation.Links</c>; 17: ifşa eşiği (tavansız sayaç, sorguda uyarının hemen kalkması), alarm ara bedeli, sığınağın düşmesi (dusus) ve terk sonları (sonlar olay açılışından önce sınanır), son şalter (last_switch); aşama 8, yayından önce aynı sürümde: kararla varılan eşikte iki uyarı vardiyası, sorgu tecrit yeri istemez, açlık tahliye nedeni; 16: aday grubu (2–3 aday, en çok biri alınır), şüphe kademeleri ve özellikler, kişiye bağlı teçhizat, acil grup, grup üretimi pano kapanışının arkasında, eksik rol kitinin aç kapanışta ve devresiz üretimi, sığınak olayları (event_decide), tecrit, tayın kipi ve açlıktan ayrılma; 15: dış kayıt ve dosya incelemesinin gözetmeni Boşta'nın ilk uygun kişisi (K4-A); İNCELE+DIŞ KAYIT takılması ve kurucu kimliği düzeltildi; 14: isimli sakinler, kartlı görev/teçhizat, ayrılmış üretim girdileri, kişisel eğitim, devir fişi ve topluluk kabulü; 13: Madenci ve Sis uzmanı atölyenin seviyesiyle katılır; 12: beş uzmanlı kadro ve baskın öncesi seçim; 11: 30 haritalık havuz, kıt sığınak ekonomisi; 10: sığınağın kapısı ve ziyaretçiler; öncekiler başka kurallarla oynandı).</summary>
        public const int CurrentVersion = 22;
        public int version = CurrentVersion;
        public uint seed;
        public Balance balance;
        public List<SavedCommand> commands = new List<SavedCommand>();
        public int day;
        /// <summary>
        /// Haritanın kısa parmak izi (<c>Simulation.MapFingerprint</c>): komşuluk, paket yerleşimi ve adların özeti. Harita kayda
        /// yazılmaz, tohum ve dengeden yeniden üretilir; parmak izi tutmayan kayıt (üretici değişmiş) yüklenmez.
        /// </summary>
        public string map;
        /// <summary>
        /// Tesis odalarının üretici izi (<see cref="FacilityMaps.FingerprintFor"/>): saha kuralına göre f8s10 veya f10 (sürüm 10; önce f8/f9; kayıt 21'den beri
        /// hiç boş değil). Odalar kayda yazılmaz; üretici değişince sürüm değil iz artar ve izi tutmayan kayıt yüklenmez (<see cref="map"/> kalıbı).
        /// </summary>
        public string facility;
    }

    public static class Replay
    {
        /// <summary>Kayıttan oyunu yeniden kurar; oluşan olaylar atılır.</summary>
        /// <remarks>
        /// Parmak izi taşıyan kayıtta harita tutmazsa yeniden oynatılmaz (komutlar başka haritada sessizce başarısız olurdu).
        /// Parmak izi olmayan kayıt (koddan kurulan geçici kayıtlar) denetlenmez ve bu haritanın iziyle doldurulur (sonra diske
        /// yazılırsa yükleme süzgecinden geçsin). Başarısız komutlar <see cref="Simulation.ReplayFailures"/>'ta sayılır.
        /// </remarks>
        public static Simulation Restore(GameRecord rec)
        {
            var sim = Simulation.NewGame(rec.seed, rec.balance);
            if (!string.IsNullOrEmpty(rec.map) && rec.map != sim.MapFingerprint)
                throw new InvalidOperationException("Kaydın haritası uyuşmuyor: " + rec.map + " / " + sim.MapFingerprint);
            if (string.IsNullOrEmpty(rec.map)) rec.map = sim.MapFingerprint;
            // Tesis odalarının üretici izi (map kalıbı): izli kayıtta tutmazsa yeniden oynatılmaz, izsiz kayıt bu izle dolar
            string fac = sim.FacilityFingerprint;
            if (!string.IsNullOrEmpty(rec.facility) && rec.facility != fac)
                throw new InvalidOperationException("Kaydın tesis planı uyuşmuyor: " + rec.facility + " / " + fac);
            if (string.IsNullOrEmpty(rec.facility)) rec.facility = fac;
            var s = sim.S;
            int failed = 0;
            foreach (var c in rec.commands)
            {
                AdvanceUntil(sim, c.day);
                if (!Apply(sim, c)) failed++;
            }
            AdvanceUntil(sim, rec.day);
            s.Events.Clear();
            sim.Record = rec;
            sim.ReplayFailures = failed;
            return sim;
        }

        /// <summary>Kaydın parmak izi bu sürümün tohum ve dengeden ürettiği haritayla aynı (yükleme süzgeci; eksik iz uyuşmaz) ve
        /// tesis izi bu sürümün tesis üreticisiyle aynı (kayıt 21'den beri iz hep f8/f9; boş izli kayıt reddedilir).</summary>
        public static bool MapMatches(GameRecord rec) =>
            rec != null && !string.IsNullOrEmpty(rec.map) && rec.map == WorldMap.FingerprintFor(rec.seed, rec.balance)
            && (rec.facility ?? "") == FacilityMaps.FingerprintFor(rec.balance);

        static void AdvanceUntil(Simulation sim, int day)
        {
            while (sim.S.Day < day && sim.S.Ending == null && !sim.S.Blocked)
            {
                int before = sim.S.Day;
                sim.Step();
                // Kartlı modelde her günün devir komutu zorunludur; eksik fiş sonsuz döngüye girmesin.
                if (sim.S.Day == before) throw new InvalidOperationException("Kayıtta vardiya devir komutu eksik.");
            }
        }

        /// <summary>
        /// Komutun sonucu (başarısızsa false). Bilinmeyen tür sessizce atlanır (başarılı sayılır). Aşama 7'de silinen eski kabul komutları (inspectApplicant, decideApplicant, reviewResident,
        /// restrictResident, dismissResident) olaylar açıkken hiç kaydedilmezdi; yalnız olaylar kapalı oynanmış bir sürüm 17 kaydında
        /// bulunabilirdi (yayınlanmadı, diskte yok). Oyun böyle kaydı yüklemez (<c>SaveStore.HasRetiredCommand</c>; yükleme süzgeci, sürüm 17'de kalır);
        /// burada doğrudan verilirse bu komutlar olmadan yeniden oynanır.
        /// </summary>
        static bool Apply(Simulation sim, SavedCommand c)
        {
            switch (c.kind)
            {
                case "liberation_region": return sim.SelectLiberationRegion(c.a).Ok;
                case "liberation_raid": return sim.StartLiberationRaid(c.a, c.b).Ok;
                case "raidres": return sim.ResolveRaid(RaidResult.Decode(c.a)).Ok;
                case "shelter_upgrade": return sim.ShelterUpgrade(c.a).Ok;
                case "shelter_assign": return sim.ShelterAssign(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)).Ok;
                case "door_answer": return sim.AnswerDoor(c.a).Ok;
                case "board_assign": return sim.AssignResident(c.a, c.b).Ok;
                case "board_train": return sim.TrainResident(c.a, c.b).Ok;
                case "board_equip": return sim.EquipResident(c.a, c.b).Ok;
                case "board_scrap": return sim.ScrapEquipment(c.a, previouslyRecorded: true).Ok;
                case "board_auto": return sim.EnableAutomaticProduction().Ok;
                case "board_stage": return sim.StageProductionMaterial(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)).Ok;
                case "board_queue": return sim.QueueProduction(c.a).Ok;
                case "board_reserve": return sim.ReserveProductionMaterial(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)).Ok;
                case "board_cancel": return sim.CancelProduction(c.a).Ok;
                case "board_print": return sim.PrintShift().Ok;
                case "board_file": return sim.FileShift(c.a).Ok;
                case "candidate_choose": return sim.ChooseCandidate(c.a, c.b).Ok;
                case "candidate_pass": return sim.PassCandidates().Ok;
                case "event_decide": return sim.DecideShelterEvent(c.a).Ok;
                case "last_switch": return sim.PullLastSwitch().Ok;
                // Decode(null) null döner: denetim reddeder (bilinmeyen tür gibi sessiz geçmez)
                case "raid_room": return sim.AdvanceRaidRoom(RoomReport.Decode(c.a)).Ok;
                case "server_reward": return sim.ChooseServerReward(c.a, c.b).Ok;
                // Tesis aşama 11: bekleyen kuşatmanın kararı (eksik case sessizce başarılı sayılırdı; testler durumu canlıyla karşılaştırır)
                case "siege_defend": return sim.DefendSector().Ok;
                case "siege_yield": return sim.YieldSector().Ok;
                default: return true;
            }
        }
    }
}
