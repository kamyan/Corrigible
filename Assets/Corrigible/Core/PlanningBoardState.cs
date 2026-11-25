using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Panodaki tek kişi kartı. Meslek başlangıç avantajıdır; öğrenilen alanlar kişide kalır.</summary>
    [Serializable]
    public sealed class ResidentState
    {
        public string Id, Name, Profession, Trait, Job = "rest", TrainingSubject;
        public int Hp, MaxHp, Move, Power, Down;
        public bool Alive = true, Restricted;
        /// <summary>Tecritte: hiçbir göreve konamaz, teçhizat alamaz, yer.</summary>
        public bool Isolated;
        public List<ResidentSkill> Experience = new List<ResidentSkill>();
        public FacilityTitle Titles;
        public int[] Deeds;
        public int Wounds => Math.Max(0, MaxHp - Hp);
        public int Skill(string subject) => Experience.FirstOrDefault(x => x.Subject == subject)?.Points ?? 0;
    }

    [Serializable]
    public sealed class ResidentSkill
    {
        public string Subject;
        public int Points;
    }

    [Serializable]
    public sealed class BoardMaterial
    {
        public string Kind;
        public int Quantity;
    }

    /// <summary>Her eşya tek kimlik ve tek sahibin cebindedir. Boş sahip depodur.</summary>
    [Serializable]
    public sealed class BoardEquipment
    {
        public string Id, Kind, OwnerId;
        public int Charges = 1;
        /// <summary>Kişiye bağlı teçhizat: devredilemez, sökülemez; sahibi sığınaktan çıkınca onunla gider.</summary>
        public bool Bound;
        /// <summary>Tesis aşama 8: düşen köpeğin dinlenmesi (vardiya; her kapanışta bir azalır). 0 değilken baskına gitmez.</summary>
        public int Rest;
    }

    [Serializable]
    public sealed class BoardProductionOrder
    {
        public string Id, RecipeId;
        public int Remaining;
        public bool Started;
        public List<BoardMaterial> Reserves = new List<BoardMaterial>();
    }

    /// <summary>Tarifler denge nesnesinden türetilir; işin ilerlemesi işçiden bağımsızdır.</summary>
    public sealed class BoardRecipe
    {
        public string Id, Name, Output;
        public int Turns;
        public List<BoardMaterial> Inputs = new List<BoardMaterial>();
        /// <summary>Tesis aşama 8: erzak bedeli (yalnız köpek); iş emri başlarken hammaddeyle birlikte düşer, yetmezse emir bekler.</summary>
        public double Supplies;
        /// <summary>Tesis aşama 8: sunucu ödülüyle açılan tarif (<see cref="FacilityGear.Recipe"/>). Otomatik üretim yolu bunları görmez;
        /// Tezgâh kazanımı bunlara işler.</summary>
        public bool Reward;
        /// <summary>Tesis aşama 10: sayfa cevap tarifine dönüştü (<see cref="FacilityGear.Answers"/> kimliği; yoksa null). Sayfanın
        /// <see cref="Id"/>, <see cref="Output"/>, <see cref="Reward"/> ve <see cref="Turns"/>'ü temel tarifinkidir; ad cevabın, girdiye
        /// <c>facilityAnswerCircuits</c> devre eklenir.</summary>
        public string Answer;
    }

    [Serializable]
    public sealed partial class PlanningBoardState
    {
        public List<ResidentState> People = new List<ResidentState>();
        public List<BoardEquipment> Equipment = new List<BoardEquipment>();
        public List<BoardMaterial> Materials = new List<BoardMaterial>();
        public List<BoardProductionOrder> Orders = new List<BoardProductionOrder>();
        public bool Locked, Printed, Filing;
        public bool AutomaticProduction;
        public List<BoardMaterial> ProductionInputs = new List<BoardMaterial>();
        public int Shift, GatheringProgress, NextResidentId = 9, NextEquipmentId = 1, NextOrderId = 1;
        public string ReceiptId, LastFiledReceiptId;
        /// <summary>Fişte o anda bilinen plan; sonraki vardiyanın gizli olaylarını açmaz.</summary>
        public string ReceiptText, RaidSummary;
        public double LastSuppliesChange;
        public List<string> LastCompleted = new List<string>();
        public ResidentState Person(string id) => People.FirstOrDefault(p => p.Id == id);
        public BoardEquipment EquipmentItem(string id) => Equipment.FirstOrDefault(e => e.Id == id);
        public BoardProductionOrder Order(string id) => Orders.FirstOrDefault(o => o.Id == id);
        public BoardMaterial Material(string kind) => Materials.FirstOrDefault(m => m.Kind == kind);
    }

    public static class PlanningBoardData
    {
        public static readonly string[] FieldJobs = { "tek", "emp", "ana" };
        public static readonly string[] Subjects = { "tek", "emp", "ana", "craft", "cook", "care", "gather" };
        /// <summary>Kapıya gelen adayların konuları (bakım ve ikmal oyuncuya kapalı).</summary>
        public static readonly string[] CandidateSubjects = { "tek", "emp", "ana", "craft", "cook" };
        public static readonly string[] Jobs = { "tek", "emp", "ana", "craft", "cook", "care", "gather", "train", "rest", "watch", "contact" };
        public static readonly string[] MaterialKinds = { "metal", "devre", "kumas", "kimyasal" };

        public static string JobName(string id)
        {
            switch (id)
            {
                case "tek": return Loc.T("Teknisyen");
                case "emp": return Loc.T("EMP uzmanı");
                case "ana": return Loc.T("Analist");
                case "craft": return Loc.T("Üretim");
                case "cook": return Loc.T("Mutfak");
                case "care": return Loc.T("Bakım ve dönüş triyajı");
                case "gather": return Loc.T("İkmal");
                case "train": return Loc.T("Eğitim");
                case "watch": return Loc.T("Gözetim");
                case "contact": return Loc.T("Dış temas");
                default: return Loc.T("Dinlenme / yedek");
            }
        }

        public static string SubjectName(string id) => JobName(id);

        public static string MaterialName(string id)
        {
            switch (id)
            {
                case "metal": return Loc.T("Metal");
                case "devre": return Loc.T("Devre");
                case "kumas": return Loc.T("Kumaş");
                case "kimyasal": return Loc.T("Kimyasal");
                default: return id;
            }
        }

        public static string EquipmentSlot(string kind) => kind != null && kind.StartsWith("kit_", StringComparison.Ordinal)
            ? "kit" : kind == "armor" ? "armor" : "utility";

        public static string EquipmentName(string id)
        {
            switch (id)
            {
                case "kit_tek": return Loc.T("Teknisyen kiti");
                case "kit_emp": return Loc.T("EMP kiti");
                case "kit_ana": return Loc.T("Analist kiti");
                case "armor": return Loc.T("Zırh plakası");
                case "medkit": return Loc.T("Bakım kiti");
                case "emp_cell": return Loc.T("EMP hücresi");
                // Tesis aşama 8: sunucu ödülüyle açılan yedi ekipman (FacilityGear.Kinds)
                case "wire_kit": return Loc.T("Örme seti");
                case "charge": return Loc.T("Dinamit lokumu");
                case "turret": return Loc.T("Yaylı taret");
                case "decoy": return Loc.T("Yürüyen yem");
                case "winch": return Loc.T("Makaralı vinç");
                case "clamp": return Loc.T("Kumanda kelepçesi");
                case "dog": return Loc.T("Köpek");
                default: return id;
            }
        }

        /// <summary>
        /// Tarif kartının kısa adı (tek kip, kayıt 21): 112 × 80 kartta ad 12 mm'den küçülmez (Revizyon 3 D6-A) ve çıktı simgesine
        /// 80 mm kalır; tam adı buna sığmayan tarifte kart yüzü bunu yazar (defter sayfası tam adı iki satıra kırar). Kimlik tarifin
        /// çıktısı ya da cevap sayfasında cevabın kimliği (<see cref="BoardRecipe.Answer"/>). Kısa adı olmayanda null.
        /// </summary>
        public static string CardName(string id)
        {
            switch (id)
            {
                case FacilityGear.Charge: return Loc.T("Dinamit");
                case FacilityGear.Turret: return Loc.T("Taret");
                case FacilityGear.Decoy: return Loc.T("Yem");
                case FacilityGear.Winch: return Loc.T("Vinç");
                case FacilityGear.Clamp: return Loc.T("Kelepçe");
                case FacilityGear.AnsSokum: return Loc.T("Anahtar");
                case FacilityGear.AnsPiercer: return Loc.T("Delici");
                case FacilityGear.AnsSpring: return Loc.T("Zemberek");
                default: return null;
            }
        }
    }
}
