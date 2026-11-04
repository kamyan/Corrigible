using UnityEngine;

namespace Corrigible.Game.Audio
{
    /// <summary>
    /// Tek seferlik ses olayları. Klipleri <see cref="SoundBank.cues"/> bağlar; asset değeri sayı olarak sakladığı için her
    /// üyenin sayısı sabittir (silinen sesler numaralarını boş bırakır).
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
