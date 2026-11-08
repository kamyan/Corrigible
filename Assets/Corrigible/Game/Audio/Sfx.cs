using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Tek seferlik ses olayları. Klipleri <see cref="SoundBank.cues"/> bağlar; asset değeri sayı olarak sakladığı için her
    /// üyenin sayısı sabittir (silinen sesler numaralarını boş bırakır). 56–66 ve 97–128'in klipleri yer tutucudur
    /// (<c>Tools/Audio/rev3_placeholder_audio.py</c>); ShiftLever (109) geçici olarak SwitchClack klibini kullanır.
    /// </summary>
    public enum Cue
    {
        // Arayüz
        Click = 0, TeamRadio = 22,
        // Son
        EndErase = 25,
        // Baskın
        RaidStep = 31, RaidEmpBlast = 34, RaidDroneShot = 36, RaidMelee = 37, RaidHit = 38, RaidHitObject = 39,
        RaidArmor = 40, RaidRepair = 41, RaidPush = 42, RaidUnitDown = 43, RaidRobotDown = 44, RaidCivilianBreak = 45, RaidExplosion = 46,
        RaidCoreBreak = 47, RaidBreak = 48, RaidSpawn = 49,
        // Madenci ve Sis uzmanı: dinamitin patlaması, sis bombası, sise kesilen atış
        RaidDynamite = 32, RaidSmoke = 33, RaidSmoked = 35,
        // Sığınağın kapısı
        DoorKnock = 50, DoorOpen = 51, DoorShot = 52,
        // Telsiz: sahayla tek bağ (hat açılır, emir iletilir, gözcü bildirir)
        RadioTune = 53, RadioSend = 54, RadioReport = 55,
        // Görev fişi (Revizyon 2 · S2): iğne bölgeye düşer, kâğıt hışırdar, kurşun kalem satırı işaretler
        PinDrop = 56, PaperSlide = 57, Pencil = 58,
        // Sığınak (Revizyon 2 · S3): piyon odaya oturur (“tok”), oda geliştirilir (çekiç)
        PawnPlace = 59, Hammer = 60,
        // Kapı (Revizyon 2 · S4): gözetleme kapağı (metal “şık”), kanat kapanırken çarkın “klak”ı, ağır damga (“tak”)
        DoorPeep = 61, DoorClose = 62, StampHeavy = 63,
        // Vardiya raporu (Revizyon 2 · S5b): karşı saldırıdan önce alarm lambasıyla alçak uğultu
        AlarmHum = 64,
        // Sonuç kartı (Revizyon 2 · S7): başarılı damgada kısa zil (başarısızda StampHeavy)
        ResultBell = 65,
        // Harekât sonu raporu (Revizyon 2 · S8): kazanınca uzun zil (kaybedince StampHeavy)
        FinalBell = 66,
        // Bölge olaylarının sesleri (Docs/BASKIN-MEKANLARI.md Katman 8; RaidBoard3D.EventFx). Saha sesidir: telsiz bandından
        // duyulur (AudioDirector.FromField). Her biri ≤1,2 sn; patlama ve çekirdek kırılma seslerinden yüksek değil.
        // weu bulut: iki alarm bipi + meme “pşşt”, bulut dağılır
        EvCo2Burst = 67, EvCloudFade = 68,
        // eeu raf: el çarkı cırcırı, yuvarlanma, “klonk”; sıkışınca sert duruş
        EvShelfRoll = 69, EvShelfJam = 70,
        // me gaz: dolum, boşalma, döküntüsüz “vuuumf” (alev alma ve parlama)
        EvGasFill = 71, EvGasVent = 72, EvGasFlash = 73,
        // af tavan: gıcırtı ve çatırtı, çakıl şelalesi ve gümleme (toz da)
        EvCeilingCrack = 74, EvCeilingFall = 75,
        // na asansör: motor, varış “dıng”ı
        EvLiftMotor = 76, EvLiftDing = 77,
        // ru kriyo: sıvı azot tıslaması, buz çıtırtısı
        EvFreeze = 78, EvIce = 79,
        // in turnike: üç cırcır ve vardiya zili
        EvTurnstile = 80,
        // cn kaynak: ark cızırtısı, kolun ağır çelik “klank”ı
        EvWeldArc = 81, EvArmClank = 82,
        // jk faz (ve na kabin bekliyor): kontaktör “klak”ı
        EvPhase = 83,
        // sea süzgeç: guruldama
        EvDrain = 84,
        // sa tüp: vana ıslığı, fırlama, indiği karede çınlama
        EvValveWhistle = 85, EvTubeLaunch = 86, EvTubeLand = 87,
        // oc yağ: hortum “pat”ı ve püskürme
        EvOilSlide = 88,
        // Ayarlar dolabı (Docs/AYARLAR-DOLABI-VE-MENU.md): mandal ve menteşe, kapanış, sürgünün çentiği, yarasa kollu anahtarın
        // “klak”ı, kademe anahtarının “klonk”u
        CabinetOpen = 89, CabinetClose = 90, FaderDetent = 91, SwitchClack = 92, SelectorClunk = 93,
        // Menü sahnesi: kart basma saatinin “ka-çunk”u, kapının zili, son şalter (ışıklar söner, jeneratör susar)
        TimeClockPunch = 94, DoorBuzzer = 95, PowerDown = 96,
        // Kartlı pano (Revizyon 3 · Aşama 3, PLAN §1.8): kart kalkışı (kâğıt hışırtısı), mıknatıs “tık”ı, geri dönüşün yumuşak
        // “tak”ı, kilitli planın “tok”u, destenin kayışı ve toplanışı (“şırp”), eşyanın kartın arkasına sıkışması (“şık”), hurda
        // kutusunun “tunk”u, asma kilidin “klak”ı, plan kapağının metal “tık”ı.
        BoardLift = 97, BoardMagnet = 98, BoardReturn = 99, BoardDeny = 100, BoardDeckSlide = 101, BoardDeckCollect = 102,
        BoardItemTuck = 103, BoardHurda = 104, BoardPadlock = 105, BoardFlap = 106,
        // Vardiya kapanışı (Revizyon 3 · Aşama 4, R19 · PLAN §1.11): kapağın metal menteşe “tık”ı ve kapanışı, kolun “klak-klunk”u
        // (tek klip; bankada geçici olarak SwitchClack klibi), yazıcının “tırrt”ı (0,3 s), kopmanın “çıt”ı, fişin EVRAK'a düşüşünün
        // “şap”ı, yeni vardiyanın kısık “gong”u (düzey 0,5), basım reddinde boş ağzın “tık”ı. Kilitli tık SwitchClack, fişin ele
        // gelişi BoardLift, dönüşü BoardReturn çalar.
        ShiftCoverOpen = 107, ShiftCoverShut = 108, ShiftLever = 109, SlipPrint = 110, SlipTear = 111, SlipFile = 112,
        ShiftGong = 113, SlipMouthEmpty = 114,
        // Tarif defteri ve üretim (Revizyon 3 · Aşama 5, R18 · PLAN §1.13): defterin kancadan öne gelip açılışı, sayfa çevirmenin
        // kâğıt sesi, kapanışı, tarif kartının defterden kalkışının “şırp”ı, ürün kartının depoya kayışının “şık”ı. Tarif kartının
        // iş emrine oturuşu BoardMagnet, dönüşü BoardReturn, reddi BoardDeny çalar.
        BoardBookOpen = 115, BoardPageTurn = 116, BoardBookClose = 117, BoardRecipeLift = 118, BoardProductSlide = 119,
        // Kapı ve kabul (Revizyon 3 · Aşama 6, R9/R17 · PLAN §1.13): tezgâh kanadının inişindeki “tank”, evrak çekmecesi (tenekelerle
        // dışarı da aynı ses), talimat işaretçisinin “tık”ı, bulgu fişinin yazıcı tıkırtısı, interkomun 0,2 s cızırtısı, buğu camı
        // (kapak ve buğu), talep defterinden koparma, damganın raftan kalkışı, tezgâhın katlanışı. Damganın basışı StampHeavy, rafa
        // dönüşü BoardReturn, reddi BoardDeny, belgenin tezgâha düşüşü PaperSlide, kapı DoorKnock/DoorOpen/DoorClose çalar.
        AdmissionCounterDown = 120, AdmissionDrawer = 121, AdmissionMarker = 122, AdmissionSlip = 123, AdmissionIntercom = 124,
        AdmissionGlass = 125, AdmissionPadTear = 126, AdmissionStampLift = 127, AdmissionCounterFold = 128,
    }

    /// <summary>Sürekli çalan döngüler (sesi duruma göre açılıp kısılır).</summary>
    public enum LoopCue { RaidAmbience = 2, RadioHiss = 3 }

    /// <summary>
    /// Ses çalmanın tek giriş noktası (görünümler buradan çağırır). Sesi ana sahnedeki <see cref="AudioDirector"/> çalar;
    /// o yoksa (ör. Baskin sahnesi tek başına açıldı) sessizce hiçbir şey yapmaz.
    /// </summary>
    public static class Sfx
    {
        /// <param name="pan">-1 sol, 1 sağ.</param>
        /// <param name="pitch">1 = özgün perde.</param>
        public static void Play(Cue cue, float volume = 1f, float pan = 0f, float pitch = 1f) => AudioDirector.Instance?.PlayCue(cue, volume, pan, pitch);

        /// <summary>
        /// Açılan baskının yeri: bölgenin bina sesi (<see cref="SoundBank.sites"/>, bölge kimliğiyle) baskın boyunca genel
        /// uğultunun yerine çalar; bölgenin yatağı yoksa uğultu kalır. Baskın kapanınca kendiliğinden söner.
        /// </summary>
        /// <param name="region">Bölge kimliği (weu, eeu…); null ya da bilinmeyen bölge uğultuya düşer.</param>
        /// <param name="details">Seyrek ayrıntılar çalsın mı (eğitimde kapalı).</param>
        public static void RaidSite(string region, bool details) => AudioDirector.Instance?.SetRaidSite(region, details);

        /// <summary>Dünya konumunu ekrandaki yatay konuma göre sağ-sol dengesine çevirir.</summary>
        public static float PanOf(Camera cam, Vector3 world)
        {
            if (cam == null) return 0;
            return Mathf.Clamp((cam.WorldToViewportPoint(world).x - 0.5f) * 1.4f, -0.8f, 0.8f);
        }
    }
}
