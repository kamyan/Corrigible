using TMPro;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>Baskın sahnesinin 3D parçaları, vurgu malzemeleri ve renkleri. Tahta (<see cref="RaidBoard3D"/>) buradan üretir.</summary>
    [CreateAssetMenu(menuName = "Corrigible/Raid Piece Library", fileName = "RaidPieceLibrary")]
    public sealed class RaidPieceLibrary : ScriptableObject
    {
        [Header("Zemin")]
        public GameObject floor;
        public GameObject wall;
        public GameObject cable;

        [Header("Bina kitleri (BASKIN-MEKANLARI Katman 3–5)")]
        [Tooltip("Bugünkü görünüm: eğitim, örnek baskın, testler (MapSeed 0) ve kiti olmayan bölge.")]
        public RaidRoomKit defaultKit;
        [Tooltip("Bölgelerin kitleri (kimlik = bölge kimliği). Boş bölge varsayılan kite düşer.")]
        public RaidRoomKit[] kits = new RaidRoomKit[0];

        [Header("Dolaplar")]
        public RaidPiece core;
        public RaidPiece node;
        public RaidPiece civilian;
        public RaidPiece cooling;
        public RaidPiece spawner;
        [Tooltip("Yağma görevinin hedefi: KYGA ikmal sandığı (zeytin yeşili boyalı ahşap, çelik köşe, halat tutamak, kapakta krem şablon; kırmızı yok). Boşsa düğüm dolabı.")]
        public RaidPiece lootCrate;
        [Tooltip("Boşaltılmış ikmal sandığı: alçak boş tepsi (≤ 0,25, karenin içinde; kare artık yürünür); Teknisyen malzemeleri alınca yerinde kalır (kırık dolap enkazı değil).")]
        public GameObject lootCrateOpen;
        [Tooltip("Kırılan dolabın enkazı.")]
        public GameObject rubble;

        [Header("Birimler")]
        public RaidPiece technician;
        public RaidPiece emp;
        public RaidPiece analyst;
        public RaidPiece miner;
        public RaidPiece smokeSpecialist;
        public RaidPiece drone;
        public RaidPiece maintenance;
        [Header("Bölge imza düşmanları")]
        public RaidPiece armoredDrone;
        public RaidPiece mortar;
        public RaidPiece bomber;
        public RaidPiece locker;
        public RaidPiece shieldDrone;
        public RaidPiece replicator;

        [Header("Etiket işaretleri (Revizyon 2 · S6/S9)")]
        [Tooltip("Makine türünün işareti: can etiketinde 18 px, haritadaki imza işaretiyle aynı çizim.")]
        public Sprite glyphDrone;
        public Sprite glyphArmoredDrone, glyphShieldDrone, glyphMaintenance, glyphLocker, glyphMortar, glyphReplicator, glyphBomber;
        [Tooltip("Etiketteki zırh / plaka kalkanı.")]
        public Sprite armorIcon;
        [Tooltip("Niyet rozetleri (S9-C/C2): kilit kıskacı Ø26, onarım anahtarı, kalkan Ø28, kopyalama (dron) Ø24.")]
        public Sprite badgeClamp, badgeWrench, badgeShield, badgeClone;
        [Tooltip("Niyet okları: zarar veren #B8241A; zarar vermeyen (kalkan, kopyalama) #854005.")]
        public Color intent = new Color32(0xB8, 0x24, 0x1A, 0xFF);
        public Color intentQuiet = new Color32(0x85, 0x40, 0x05, 0xFF);

        [Header("İşaretler")]
        [Tooltip("Robotun uyanacağı kare.")]
        public GameObject spawnMarker;
        [Tooltip("Karo vurgusu (derinlik testli, saydam).")]
        public Material highlight;
        [Tooltip("Niyet okları ve rozetler (her zaman üstte).")]
        public Material overlay;
        [Tooltip("Karo işaretleri (tarama, seçim, hedef, uyanma halkası): raid.js'teki gibi parçalar örter. Boşsa overlay.")]
        public Material tileMarks;
        public TMP_FontAsset font;
        [Tooltip("Dünya içindeki yazılar (koyu konturlu).")]
        public Material labelMaterial;

        [Header("Renkler")]
        public Color move = new Color(0.36f, 0.78f, 0.85f, 0.42f);
        public Color target = new Color(0.94f, 0.69f, 0.25f, 1f);
        public Color danger = new Color(1f, 0.36f, 0.3f, 0.5f);
        public Color heal = new Color(0.45f, 0.85f, 0.5f, 0.4f);
        public Color hover = new Color(1f, 1f, 1f, 0.9f);
        public Color threat = new Color(1f, 0.36f, 0.3f, 1f);
        public Color good = new Color(0.45f, 0.85f, 0.5f, 1f);
        public Color ink = new Color(0.9f, 0.95f, 0.96f, 1f);
        [Tooltip("Sis bulutu ve sise kesilen niyetin oku.")]
        public Color smoke = new Color(0.56f, 0.55f, 0.52f, 0.55f);

        [Header("Bölge olaylarının nesneleri (Docs/BOLGE-OLAYLARI.md)")]
        [Tooltip("Basınçlı tüp (tup). Boşsa soğutma ünitesi tubeFallbackScale ölçeğiyle (ince uzun silindir).")]
        public RaidPiece tube;
        [Tooltip("Çöken tavanın enkaz yığını (enkaz, 2 can). Boşsa kırık dolap enkazı debrisFallbackScale ölçeğiyle parçaya çevrilir.")]
        public RaidPiece debris;
        [Tooltip("Kaynak robotu kolunun kaidesi (kol). Boşsa soğutma ünitesi weldArmFallbackScale ölçeğiyle (basık kaide).")]
        public RaidPiece weldArm;
        [Tooltip("Tüp atanmamışken yedek parçanın ölçeği.")]
        public Vector3 tubeFallbackScale = new Vector3(0.5f, 1.45f, 0.5f);
        [Tooltip("Enkaz yığını atanmamışken kırık dolap enkazının ölçeği.")]
        public Vector3 debrisFallbackScale = new Vector3(1.15f, 1.7f, 1.15f);
        [Tooltip("Kaynak kolu kaidesi atanmamışken yedek parçanın ölçeği.")]
        public Vector3 weldArmFallbackScale = new Vector3(1.2f, 0.35f, 1.2f);

        [Header("Bölge olaylarının renkleri (okunurluk: bulut beyaz, sis gri, gaz sarı)")]
        [Tooltip("Söndürme bulutu (Batı Avrupa): dolu bulut beyaz.")]
        public Color hazardCloud = new Color(0.99f, 0.99f, 1f, 1f);
        [Tooltip("Bulutun, memenin ve buluta kesilen okun çizgisi: beyaz bulut krem tahtada okunsun diye soğuk gri.")]
        public Color hazardCloudEdge = new Color(0.45f, 0.54f, 0.62f, 1f);
        [Tooltip("Yanıcı gaz (Orta Doğu): sarı tarama ve sızıntı halkası.")]
        public Color hazardGas = new Color(0.85f, 0.66f, 0.05f, 1f);
        [Tooltip("Alev almış gaz bulutu: kırmızı-turuncu, titrer.")]
        public Color hazardFlame = new Color(0.88f, 0.3f, 0.08f, 1f);
        [Tooltip("Kriyo bölgesi, donan birim ve buz kabuğu (Rusya): camgöbeği.")]
        public Color hazardFreeze = new Color(0.16f, 0.66f, 0.82f, 1f);
        [Tooltip("Kaynak karesi ve kaynak kolu (Çin): turuncu.")]
        public Color hazardWeld = new Color(0.93f, 0.47f, 0.1f, 1f);
        [Tooltip("Canlı kablo kanalı (Japonya-Kore): parlak kehribar.")]
        public Color hazardLive = new Color(1f, 0.7f, 0.08f, 1f);
        [Tooltip("Sönük kablo kanalı: gri.")]
        public Color hazardDead = new Color(0.55f, 0.55f, 0.53f, 1f);
        [Tooltip("Hidrolik yağ (Okyanusya): parlak siyah.")]
        public Color hazardOil = new Color(0.07f, 0.07f, 0.08f, 1f);
        [Tooltip("Tavan çatlağı (Afrika): grafit-kahverengi.")]
        public Color hazardCrack = new Color(0.36f, 0.26f, 0.18f, 1f);
        [Tooltip("Arşiv rafının rayı (Doğu Avrupa): grafit şerit.")]
        public Color hazardRail = new Color(0.25f, 0.24f, 0.22f, 1f);
        [Tooltip("Yük asansörünün durakları, kabin ve inecek birimin hayaleti (Kuzey Amerika): sarı.")]
        public Color hazardLift = new Color(0.9f, 0.7f, 0.04f, 1f);
        [Tooltip("Turnike holü (Hindistan): soluk mavi zemin.")]
        public Color hazardHall = new Color(0.3f, 0.5f, 0.85f, 1f);
        [Tooltip("Turnikenin yön okları.")]
        public Color hazardGate = new Color(0.1f, 0.28f, 0.54f, 1f);
        [Tooltip("Süzgeç (Güneydoğu Asya): koyu halka ve çekiş okları.")]
        public Color hazardDrain = new Color(0.12f, 0.16f, 0.2f, 1f);
        [Tooltip("Basınçlı tüpün uçuş hattı (Güney Amerika): soluk turuncu şerit, ıslık çalanda parlak.")]
        public Color hazardTube = new Color(0.93f, 0.5f, 0.15f, 1f);

        public RaidPiece Object(string kind) => kind switch
        {
            "core" => core, "node" => node, "civ" => civilian, "cool" => cooling, "spawner" => spawner,
            // Bölge olaylarının nesneleri: atanmamışsa yedek parça (tahta ölçekler; enkazı kırık dolap enkazından kurar)
            "tup" => Or(tube, cooling), "kol" => Or(weldArm, cooling), "enkaz" => Or(debris, civilian),
            _ => civilian,
        };

        /// <summary>Nesnenin kendi parçası atanmış mı (olay nesnelerinde yedek parça kullanılırken false).</summary>
        public bool HasOwnPiece(string kind) => kind switch
        {
            "tup" => tube != null, "kol" => weldArm != null, "enkaz" => debris != null, _ => true,
        };

        /// <summary>Yedek parçanın ölçeği (kendi parçası olan nesnede 1).</summary>
        public Vector3 FallbackScale(string kind) => HasOwnPiece(kind) ? Vector3.one : kind switch
        {
            "tup" => tubeFallbackScale, "kol" => weldArmFallbackScale, "enkaz" => debrisFallbackScale, _ => Vector3.one,
        };

        // Unity nesnesinde ?? sahte null'u görmez
        static RaidPiece Or(RaidPiece piece, RaidPiece fallback) => piece != null ? piece : fallback;

        /// <summary>
        /// Baskının bina kiti: harekât baskını (MapSeed ≠ 0) bölgesinin kitini alır; MapSeed 0 (eğitim, örnek baskın, yakalama seti,
        /// testler), boş ya da kiti olmayan bölge varsayılan kiti.
        /// </summary>
        public RaidRoomKit KitFor(string region, uint mapSeed)
        {
            if (mapSeed == 0 || string.IsNullOrEmpty(region)) return defaultKit;
            var kit = Find(k => k.Serves(region));
            return kit != null ? kit : defaultKit;
        }

        /// <summary>Kimliğiyle kit (editördeki kitOverride ve önizleme); "default" ya da bilinmeyen kimlik varsayılan kit.</summary>
        public RaidRoomKit KitById(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "default") return defaultKit;
            var kit = Find(k => k.id == id);
            return kit != null ? kit : defaultKit;
        }

        /// <summary>Kırık dolap enkazının gedikteki ölçeği (kitin kendi molozu yoksa): yarı yüksekliğin altında basık yığın.</summary>
        public static readonly Vector3 BreachRubbleFallbackScale = new Vector3(0.9f, 0.3f, 0.9f);

        /// <summary>
        /// Madenci'nin yıktığı duvarın yerinde kalan moloz (BASKIN-MEKANLARI Katman 5): kitin molozu (<see cref="RaidRoomKit.wallRubble"/>,
        /// ölçek 1) ya da yoksa (varsayılan kit) kırık dolap enkazı basık ölçekle (<see cref="BreachRubbleFallbackScale"/>). Tahta ve
        /// önizleme aynı seçimi kullanır. İkisi de yoksa null.
        /// </summary>
        public GameObject BreachRubble(RaidRoomKit kit, out Vector3 scale)
        {
            if (kit != null && kit.wallRubble != null) { scale = Vector3.one; return kit.wallRubble; }
            scale = BreachRubbleFallbackScale;
            return rubble;
        }

        RaidRoomKit Find(System.Func<RaidRoomKit, bool> match)
        {
            if (kits == null) return null;
            foreach (var k in kits) if (k != null && match(k)) return k;
            return null;
        }

        /// <summary>Makine türünün etiket işareti; uzmanlarda ve bilinmeyen türde null.</summary>
        public Sprite Glyph(string type) => type switch
        {
            "dron" => glyphDrone, "zdron" => glyphArmoredDrone, "kalkan" => glyphShieldDrone, "bakim" => glyphMaintenance,
            "kilit" => glyphLocker, "havan" => glyphMortar, "cogalt" => glyphReplicator, "bomba" => glyphBomber,
            _ => null,
        };

        public RaidPiece Unit(string type) => type switch
        {
            "tek" => technician, "emp" => emp, "ana" => analyst, "dron" => drone, "bakim" => maintenance,
            "maden" => Or(miner, technician), "duman" => Or(smokeSpecialist, technician),
            // Yeni parça henüz atanmadıysa ailesinin parçası (havada olanlar dron, yerdekiler bakım robotu)
            "zdron" => Or(armoredDrone, drone), "kalkan" => Or(shieldDrone, drone), "havan" => Or(mortar, maintenance),
            "bomba" => Or(bomber, maintenance), "kilit" => Or(locker, maintenance), "cogalt" => Or(replicator, maintenance),
            _ => drone,
        };
    }
}
