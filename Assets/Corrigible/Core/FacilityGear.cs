using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis baskınlarının ekipmanı, tarifleri ve kazanımları (Docs/TESIS-BASKINLARI-TASARIMI.md §5, §6.1–6.2; aşama 8): kimlikler,
    /// bölge → (tarif, kazanım) ödül tablosu, adlar, ödül tarifinin dengeden kurulması ve baskın hakkı. Tablolar burada statiktir,
    /// sayılar <see cref="Balance"/>'tadır (<c>Balance.Clone</c> sığ olduğu için tablo dengeye konmaz). Durum yazmaz, RNG çekmez.
    /// "Kazanım" (tesis ödülü) aday özelliğiyle (<c>CandidateState.Perks</c>, <c>Balance.perk…</c>) karıştırılmasın: kazanım yalnız
    /// <see cref="FacilityState.Perks"/> ve <see cref="RaidParams.Perks"/> adını alır.
    /// </summary>
    public static class FacilityGear
    {
        // ---------- Ekipman (yardımcı yuva) ----------
        public const string Wire = "wire_kit", Charge = "charge", Turret = "turret", Decoy = "decoy", Winch = "winch", Clamp = "clamp", Dog = "dog";
        /// <summary>Yedi temel ekipman, belge §5.2 sırası (tablo sırası; defter sayfaları açılış sırasıyla gelir).</summary>
        public static readonly string[] Kinds = { Wire, Charge, Turret, Decoy, Winch, Clamp, Dog };
        public static bool IsGear(string kind) => kind != null && Array.IndexOf(Kinds, kind) >= 0;

        // ---------- Cevap tarifleri (aşama 8'de FacilityState.Recipes'e yazılır; aşama 10'da ekip düzeyinde etkili, defterde temelin sayfası) ----------
        public const string AnsSokum = "sokum", AnsRelay = "yedek_role", AnsPiercer = "delici", AnsSteelWire = "celik_tel", AnsSpring = "cift_zemberek";
        public static readonly string[] Answers = { AnsSokum, AnsRelay, AnsPiercer, AnsSteelWire, AnsSpring };
        public static bool IsAnswer(string id) => id != null && Array.IndexOf(Answers, id) >= 0;

        /// <summary>
        /// Cevap tarifinin yerine geçtiği temel tarifler (aşama 10, belge §5.6): Söküm anahtarı Teknisyen kitinin, Delici şarj lokumun,
        /// Çelik tel seti örme setinin, Yedek röle kelepçenin, Çift zemberek üç aygıtın. Bilinmeyende boş.
        /// </summary>
        public static IReadOnlyList<string> AnswerBases(string answer)
        {
            switch (answer)
            {
                case AnsSokum: return new[] { "kit_tek" };
                case AnsPiercer: return new[] { Charge };
                case AnsSteelWire: return new[] { Wire };
                case AnsRelay: return new[] { Clamp };
                case AnsSpring: return new[] { Turret, Decoy, Winch };
                default: return new string[0];
            }
        }

        /// <summary>Temel tarifin (<paramref name="baseKind"/>) açılmış cevabı: <paramref name="recipes"/> (<see cref="FacilityState.Recipes"/>)
        /// içinde bu temelin yerine geçen ilk cevap; yoksa null. Okuma anıdır: sonradan açılan temel de cevaplı okunur.</summary>
        public static string AnswerFor(string baseKind, IEnumerable<string> recipes) =>
            baseKind == null || recipes == null ? null : recipes.FirstOrDefault(r => IsAnswer(r) && AnswerBases(r).Contains(baseKind));

        /// <summary>Cevap tarifinin tek satırlık etkisi (ödül kâğıdı ve defter notu; sayılar dengeden). Bilinmeyende boş.</summary>
        public static string AnswerDesc(Balance b, string answer)
        {
            b = b ?? new Balance();
            switch (answer)
            {
                case AnsSteelWire: return Loc.F("Tel +{0} can.", Math.Max(0, b.facilitySteelWireHp));
                case AnsRelay: return Loc.F("Kelepçe +{0} tur.", Math.Max(0, b.facilityRelayTurns));
                case AnsSpring: return Loc.F("Aygıtlar +{0} tur.", Math.Max(0, b.facilitySpringTurns));
                case AnsPiercer: return Loc.F("Çift cidarı {0} hakla açar.", Math.Max(1, b.facilityPiercerCharges));
                case AnsSokum: return Loc.T("Teknisyen sabotajı cıvatayı söker.");
                default: return "";
            }
        }

        // ---------- Kazanımlar (belge §6.1 sırası) ----------
        public const string Ear = "kulak", Stretcher = "sedye", SpareBag = "yedek_canta", PlanBook = "plan_defteri", OpenLine = "acik_hat",
            Infirmary = "revir", Workbench = "tezgah", Threshold = "esik", Sight = "nisan", Cover = "siper", Fur = "kurk", Pulley = "makara";
        public static readonly string[] Perks = { Ear, Stretcher, SpareBag, PlanBook, OpenLine, Infirmary, Workbench, Threshold, Sight, Cover, Fur, Pulley };

        // ---------- Sunucu ödülü ----------
        /// <summary>Sunucu ödülünün iki seçeneği (<c>server_reward</c> komutunun b parçası).</summary>
        public const string ChoiceRecipe = "tarif", ChoicePerk = "kazanim";

        /// <summary>Bölge → (tarif, kazanım), belge §6.1 tablosu. Bilinmeyen bölge: (null, null).</summary>
        public static (string Recipe, string Perk) Reward(string regionId)
        {
            switch (regionId)
            {
                case "weu": return (Wire, Ear);
                case "af": return (Charge, Stretcher);
                case "sa": return (Turret, SpareBag);
                case "jk": return (Dog, PlanBook);
                case "me": return (Decoy, OpenLine);
                case "in": return (Clamp, Infirmary);
                case "sea": return (Winch, Workbench);
                case "na": return (AnsSokum, Threshold);
                case "cn": return (AnsRelay, Sight);
                case "eeu": return (AnsPiercer, Cover);
                case "ru": return (AnsSteelWire, Fur);
                case "oc": return (AnsSpring, Pulley);
                default: return (null, null);
            }
        }

        /// <summary>Tarifin (temel ya da cevap) adı; temel tarifte eşyanın adı.</summary>
        public static string RecipeName(string id)
        {
            switch (id)
            {
                case AnsSokum: return Loc.T("Söküm anahtarı");
                case AnsRelay: return Loc.T("Yedek röle");
                case AnsPiercer: return Loc.T("Delici şarj");
                case AnsSteelWire: return Loc.T("Çelik tel seti");
                case AnsSpring: return Loc.T("Çift zemberek");
                default: return PlanningBoardData.EquipmentName(id);
            }
        }

        public static string PerkName(string id)
        {
            switch (id)
            {
                case Ear: return Loc.T("Kulak");
                case Stretcher: return Loc.T("Sedye");
                case SpareBag: return Loc.T("Yedek çanta");
                case PlanBook: return Loc.T("Plan defteri");
                case OpenLine: return Loc.T("Açık hat");
                case Infirmary: return Loc.T("Revir");
                case Workbench: return Loc.T("Tezgâh");
                case Threshold: return Loc.T("Eşik");
                case Sight: return Loc.T("Nişan");
                case Cover: return Loc.T("Siper");
                case Fur: return Loc.T("Kürk");
                case Pulley: return Loc.T("Makara");
                default: return id;
            }
        }

        /// <summary>Kazanımın tek satırlık açıklaması (ödül satırı; sayılar dengeden).</summary>
        public static string PerkDesc(Balance b, string id)
        {
            switch (id)
            {
                case Ear: return Loc.F("Dinleme nöbeti vardiya başına +{0} puan.", Math.Max(0, b.facilityEarIntel));
                case Stretcher: return Loc.T("Ekiple taşınan düşen odalar arasında yere konmaz; ekipten biri ayakta çıkarsa o da çıkar.");
                case SpareBag: return Loc.F("Örme seti ve dinamit lokumu baskında +{0} hak.", Math.Max(0, b.facilitySpareCharges));
                case PlanBook: return Loc.T("Yeni cephe seçilince ilk kanadın giriş odasının planı bilinir.");
                case OpenLine: return Loc.F("Dönüş odasında +{0} tur.", Math.Max(0, b.facilityOpenLineTurns));
                case Infirmary: return Loc.F("Tesisten çıkarılan düşenin yatışı {0} vardiya kısa (en az 1).", Math.Max(0, b.facilityInfirmaryShifts));
                case Workbench: return Loc.F("Ödülle açılan tarifler {0} vardiya kısa (en az 1).", Math.Max(0, b.facilityWorkbenchShifts));
                case Threshold: return Loc.F("Gidişte giriş odasında +{0} tur.", Math.Max(0, b.facilityThresholdTurns));
                case Sight: return Loc.F("Pusu hattı +{0} kare.", Math.Max(0, b.facilitySightRange));
                case Cover: return Loc.F("Savunmada saha rölesi +{0} can.", Math.Max(0, b.facilityCoverRelayHp));
                case Fur: return Loc.F("Köpek +{0} can.", Math.Max(0, b.facilityFurHp));
                case Pulley: return Loc.F("Vinç {0} kare çeker.", Math.Max(1, b.facilityPulleyPull));
                default: return "";
            }
        }

        /// <summary>
        /// Ödülle açılan temel tarifin iş emri tarifi (girdiler, erzak bedeli, süre; <see cref="BoardRecipe.Reward"/> true). Tezgâh
        /// (<paramref name="workbench"/>) süreyi <c>facilityWorkbenchShifts</c> kısaltır (en az 1). Cevap ya da bilinmeyen kimlikte null.
        /// </summary>
        public static BoardRecipe Recipe(Balance b, string kind, bool workbench)
        {
            BoardRecipe r;
            switch (kind)
            {
                case Wire: r = Make(kind, b.facilityWireShifts, 0, ("metal", b.facilityWireMetal), ("kumas", b.facilityWireCloth)); break;
                case Charge: r = Make(kind, b.facilityChargeShifts, 0, ("metal", b.facilityChargeMetal), ("kimyasal", b.facilityChargeChemicals)); break;
                case Turret: r = Make(kind, b.facilityTurretShifts, 0, ("metal", b.facilityTurretMetal), ("devre", b.facilityTurretCircuits)); break;
                case Decoy: r = Make(kind, b.facilityDecoyShifts, 0, ("metal", b.facilityDecoyMetal), ("devre", b.facilityDecoyCircuits), ("kumas", b.facilityDecoyCloth)); break;
                case Winch: r = Make(kind, b.facilityWinchShifts, 0, ("metal", b.facilityWinchMetal), ("kumas", b.facilityWinchCloth)); break;
                case Clamp: r = Make(kind, b.facilityClampShifts, 0, ("devre", b.facilityClampCircuits), ("metal", b.facilityClampMetal)); break;
                case Dog: r = Make(kind, b.facilityDogShifts, Math.Max(0, b.facilityDogSupplies), ("kumas", b.facilityDogCloth)); break;
                default: return null;
            }
            if (workbench) r.Turns = Math.Max(1, r.Turns - Math.Max(0, b.facilityWorkbenchShifts));
            return r;
        }

        /// <summary>Sıfır adetli girdi tarife yazılmaz (Simulation'daki eski tariflerin kuralı).</summary>
        static BoardRecipe Make(string kind, int turns, double supplies, params (string Kind, int Count)[] inputs)
        {
            var r = new BoardRecipe { Id = kind, Name = PlanningBoardData.EquipmentName(kind), Output = kind, Turns = Math.Max(1, turns), Supplies = supplies, Reward = true };
            foreach (var input in inputs)
                if (input.Count > 0) r.Inputs.Add(new BoardMaterial { Kind = input.Kind, Quantity = input.Count });
            return r;
        }

        /// <summary>
        /// Ekipmanın baskın emrindeki hakkı (<see cref="SquadSlot.GearCharges"/>): örme ve lokum dengeden (Yedek çanta +<c>facilitySpareCharges</c>),
        /// aygıtlar ve kelepçe 1, köpek −1 (sürekli). Ekipman olmayan türde 0.
        /// </summary>
        public static int RaidCharges(Balance b, string kind, IEnumerable<string> perks)
        {
            int spare = perks != null && perks.Contains(SpareBag) ? Math.Max(0, b.facilitySpareCharges) : 0;
            switch (kind)
            {
                case Wire: return Math.Max(1, b.facilityWireCharges) + spare;
                case Charge: return Math.Max(1, b.facilityChargeCharges) + spare;
                case Turret: case Decoy: case Winch: case Clamp: return 1;
                case Dog: return -1;
                default: return 0;
            }
        }
    }
}
