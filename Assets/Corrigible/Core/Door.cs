using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Sığınağın kapısı: kapıda bekleyen ziyaretçi, sıradaki vuruş, zincir bayrakları ve geçmiş ziyaretler.</summary>
    [Serializable]
    public sealed class DoorState
    {
        /// <summary>Kapıda bekleyen ziyaretçi (yoksa null); vardiya biterse açılmadı sayılır.</summary>
        public DoorVisit Visit;
        /// <summary>Sıradaki vuruşun en erken günü.</summary>
        public int NextKnock;
        /// <summary>Art arda erzak sıkıntısıyla kapanan vardiya (aç sakinlerin heyeti ve silahlı sakin buna bakar).</summary>
        public int HungryShifts;
        /// <summary>Ziyaretlerin bıraktığı bayraklar (ör. "kopya_uyarisi": kopyalar konusunda uyarıldın).</summary>
        public List<string> Flags = new List<string>();
        /// <summary>Zamanı gelince kapıyı çalacak zincir kartları.</summary>
        public List<DoorChain> Chains = new List<DoorChain>();
        /// <summary>Biten ziyaretler, geliş sırasıyla.</summary>
        public List<DoorVisit> Log = new List<DoorVisit>();
    }

    [Serializable]
    public sealed class DoorChain
    {
        public string Card;
        public int Day;
    }

    /// <summary>Bir ziyaret: kart, kapının açılıp açılmadığı, son seçim ve ziyaretin bütün etkisi.</summary>
    [Serializable]
    public sealed class DoorVisit
    {
        /// <summary>Ziyaretçinin geldiği gün (gördüğü vardiya gün + 1).</summary>
        public int Day;
        public string Card;
        public bool Open;
        /// <summary>Son seçim ("acma": açılmadı); Auto: vardiya biterken kapıda kaldı, açılmadı sayıldı.</summary>
        public string Choice;
        public bool Auto;
        /// <summary>Ziyaretin gerçekleşen etkisi (sınırlar uygulandıktan sonra).</summary>
        public double Supplies, Materials, Fon, Alarm;
        public int Trust, Population, Plates, Healed, Wounded;
        /// <summary>Ziyaret YZ'nin karşı saldırısını tetikledi (alarm eşiği aşıldı).</summary>
        public bool Counterattack;
        /// <summary>Son etkinin metni (çevrilmemiş kaynak; gösterirken <c>Loc.T</c>) ve harekâtı bitirdiyse son.</summary>
        public string Result, Ending;
    }

    /// <summary>
    /// Kapı: birkaç vardiyada bir biri kapıyı çalar (<see cref="DoorCards"/>). Oyuncu gözetleme deliğinden bakar, interkomdan
    /// sorar (sunum; durumu değiştirmez), kapıyı açar ya da açmaz; açılan kapıda bir seçim yapar. Her karar
    /// <c>door_answer</c> komutudur. Kapı ana rastgelelik sırasına dokunmaz: tohumu harekât tohumu ve gündür
    /// (<see cref="DoorRng"/>). Ölümcül ziyaretçiler bir duruma bağlıdır, önceden uyarılır ve hep bir çıkışı vardır.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Kapalı kapının ortak seçenekleri: kapıyı aç, açma.</summary>
        public const string DoorOpen = "ac", DoorRefuse = "acma";

        static readonly DoorOption OpenOption = new DoorOption { Id = DoorOpen, Label = Loc.N("Kapıyı aç"), Paid = false };
        static readonly DoorOption RefuseOption = new DoorOption { Id = DoorRefuse, Label = Loc.N("Açma"), Paid = false };

        /// <summary>Kapıda bekleyen ziyaretçi (yoksa null).</summary>
        public DoorVisit DoorVisitor => S.Door?.Visit;
        public DoorCard DoorVisitorCard => DoorVisitor == null ? null : DoorCards.Get(DoorVisitor.Card);

        void SetupDoor() => S.Door = new DoorState { NextKnock = B.doorFirstKnock };

        /// <summary>Kapıdaki ziyaretçinin seçenekleri: kapalı kapıda kartın kapalı seçenekleri, Kapıyı aç ve Açma; açık kapıda kartınkiler.</summary>
        public List<DoorOption> DoorOptions()
        {
            var v = DoorVisitor;
            if (v == null) return new List<DoorOption>();
            var card = DoorCards.Get(v.Card);
            if (v.Open) return card.Options.ToList();
            var list = card.Closed.ToList();
            list.Add(OpenOption);
            list.Add(RefuseOption);
            return list;
        }

        /// <summary>Seçeneğin bu durumdaki etkisi (bedel ve sonuç); seçenek yoksa null.</summary>
        public DoorEffect DoorOptionEffect(string option)
        {
            var v = DoorVisitor;
            if (v == null) return null;
            var card = DoorCards.Get(v.Card);
            if (!v.Open && option == DoorOpen) return card.OnOpen;
            if (!v.Open && option == DoorRefuse) return card.Ignore;
            return DoorOptions().FirstOrDefault(o => o.Id == option)?.Effect?.Invoke(this);
        }

        public string DoorAnswerCheck(string option)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (S.Raid != null) return Loc.T("Baskın sürüyor.");
            if (DoorVisitor == null) return Loc.T("Kapıda kimse yok.");
            var opt = DoorOptions().FirstOrDefault(o => o.Id == option);
            if (opt == null) return Loc.T("Bu seçenek yok.");
            string need = opt.Need?.Invoke(this);
            if (need != null) return need;
            var e = DoorOptionEffect(option);
            if (opt.Paid && e != null)
            {
                if (e.Supplies < 0 && Shelter.Supplies < -e.Supplies - 1e-9) return Loc.F("{0} erzak gerekiyor.", -e.Supplies);
                if (e.Materials < 0 && Shelter.Materials < -e.Materials - 1e-9) return Loc.F("{0} malzeme gerekiyor.", -e.Materials);
                if (e.Fon < 0 && S.Fon < -e.Fon - 1e-9) return Loc.F("{0} fon gerekiyor.", -e.Fon);
            }
            return null;
        }

        /// <summary>Kapıdaki ziyaretçiye cevap: kapıyı aç, açma ya da açık kapıda bir seçim.</summary>
        public ActResult AnswerDoor(string option) => RunCommand(() => AnswerDoorCore(option), "door_answer", option, DoorVisitor?.Card);

        ActResult AnswerDoorCore(string option)
        {
            var why = DoorAnswerCheck(option);
            if (why != null) return ActResult.Fail(why);
            var v = DoorVisitor;
            var card = DoorCards.Get(v.Card);
            if (!v.Open && option == DoorOpen)
            {
                v.Open = true;
                ApplyDoor(card.OnOpen, v);
                // Açılınca biten ziyaret (tuzak, ölüm ya da yalnız bir haber)
                if (S.Ending != null || card.Options.Length == 0) FinishVisit(v, DoorOpen);
                return ActResult.Success();
            }
            ApplyDoor(DoorOptionEffect(option), v);
            FinishVisit(v, option);
            return ActResult.Success();
        }

        void FinishVisit(DoorVisit v, string choice)
        {
            v.Choice = choice;
            S.Door.Log.Add(v);
            S.Door.Visit = null;
        }

        /// <summary>Vardiyanın sonu (adım çağırır): kapıda kalan ziyaretçi açılmadı sayılır, sırası geldiyse yeni biri çalar.</summary>
        void StepDoor()
        {
            var d = S.Door;
            if (d == null) return;
            d.HungryShifts = Hungry ? d.HungryShifts + 1 : 0;
            if (d.Visit != null)
            {
                var v = d.Visit;
                v.Auto = true;
                ApplyDoor(DoorCards.Get(v.Card).Ignore, v);
                FinishVisit(v, DoorRefuse);
            }
            if (S.Ending != null || S.Day < d.NextKnock) return;
            var card = PickDoorCard();
            if (card == null) return;
            d.Visit = new DoorVisit { Day = S.Day, Card = card.Id };
            d.NextKnock = S.Day + B.doorKnockMin + (int)Math.Floor(DoorRng(1).Next() * (Math.Max(B.doorKnockMin, B.doorKnockMax) - B.doorKnockMin + 1));
            S.Events.Add(new SimEvent { Type = SimEventType.Door, Key = card.Id });
        }

        /// <summary>
        /// Sıradaki ziyaretçi: ilk vuruş senaryolu ve dostça; sonra zamanı gelen zincir kartı; yoksa koşulu tutan kartlardan
        /// ağırlıkla biri (son <c>doorRepeatGap</c> ziyaretteki kartlar, bir kez gelmiş tek seferlikler ve güvenli günlerdeki
        /// ölümcül kartlar hariç). Uygun kart yoksa kapı bu vardiya çalmaz.
        /// </summary>
        DoorCard PickDoorCard()
        {
            var d = S.Door;
            if (d.Log.Count == 0) return DoorCards.Get(DoorCards.First);
            foreach (var chain in d.Chains.Where(c => c.Day <= S.Day).OrderBy(c => c.Day).ToList())
            {
                var c = DoorCards.Get(chain.Card);
                // Ölümcül zincir güvenli günleri bekler
                if (c.Deadly && S.Day < B.doorSafeShifts) continue;
                d.Chains.Remove(chain);
                if (c.Once && DoorSeen(c.Id) || c.Still != null && !c.Still(this)) continue;
                return c;
            }
            var recent = new HashSet<string>(d.Log.Skip(Math.Max(0, d.Log.Count - B.doorRepeatGap)).Select(v => v.Card));
            var pool = DoorCards.All.Where(c => c.When != null && !recent.Contains(c.Id) && !(c.Once && DoorSeen(c.Id))
                && !(c.Deadly && S.Day < B.doorSafeShifts) && d.Chains.All(x => x.Card != c.Id) && c.When(this)).ToList();
            if (pool.Count == 0) return null;
            double r = DoorRng(2).Next() * pool.Sum(c => c.Weight);
            foreach (var c in pool)
            {
                r -= c.Weight;
                if (r < 0) return c;
            }
            return pool[pool.Count - 1];
        }

        /// <summary>Etkiyi uygular (depo, nüfus ve plaka sınırlarıyla) ve gerçekleşeni ziyarete yazar.</summary>
        void ApplyDoor(DoorEffect e, DoorVisit v)
        {
            if (e == null) return;
            var sh = Shelter;
            if (e.Supplies != 0)
            {
                double before = sh.Supplies;
                sh.Supplies = Math.Max(0, Math.Min(ShelterLogic.SuppliesCapacity(sh, B), sh.Supplies + e.Supplies));
                sh.Starving = sh.Supplies <= 0;
                v.Supplies += sh.Supplies - before;
            }
            if (e.Materials != 0)
            {
                double before = sh.Materials;
                sh.Materials = Math.Max(0, Math.Min(B.shelterMaterialsStorage, sh.Materials + e.Materials));
                v.Materials += sh.Materials - before;
            }
            if (e.Fon != 0)
            {
                double before = S.Fon;
                S.Fon = Math.Max(0, S.Fon + e.Fon);
                v.Fon += S.Fon - before;
            }
            if (e.Population != 0)
            {
                int before = sh.Population;
                if (e.Population > 0) sh.Population = Math.Max(sh.Population, Math.Min(ShelterLogic.Capacity(sh, B), sh.Population + e.Population));
                else ShelterLogic.Leave(sh, -e.Population);
                v.Population += sh.Population - before;
            }
            if (e.Plates > 0)
            {
                // Depo doluysa plaka malzemeye döner
                int room = Math.Max(0, ShelterLogic.PlateCapacity(sh, B) - sh.Plates), add = Math.Min(room, e.Plates);
                sh.Plates += add;
                v.Plates += add;
                if (e.Plates > add)
                {
                    double before = sh.Materials;
                    sh.Materials = Math.Min(B.shelterMaterialsStorage, sh.Materials + (e.Plates - add) * B.shelterPlateMaterials);
                    v.Materials += sh.Materials - before;
                }
            }
            var members = S.Teams.SelectMany(t => t.Members).ToList();
            if (e.Heal > 0)
            {
                foreach (var m in members)
                {
                    v.Healed += m.Down > 0 ? 0 : m.Wounds;
                    if (m.Down <= 0) m.Wounds = 0;
                    m.Down = Math.Max(0, m.Down - e.Heal);
                }
                foreach (var m in sh.Reserves) m.Down = Math.Max(0, m.Down - e.Heal);
            }
            if (e.Wound > 0)
            {
                // Yorgunluk: ayaktaki her uzman yara alır ama düşmez (en az 1 can kalır)
                foreach (var m in members.Where(x => x.Down <= 0))
                {
                    int before = m.Wounds;
                    m.Wounds = Math.Min(MaxHp(m.Role) - 1, m.Wounds + e.Wound);
                    v.Wounded += m.Wounds - before;
                }
            }
            if (e.Flag != null && !S.Door.Flags.Contains(e.Flag)) S.Door.Flags.Add(e.Flag);
            if (e.Unflag != null) S.Door.Flags.Remove(e.Unflag);
            if (e.Chain != null && e.Chain.Length > 0)
            {
                var rng = DoorRng(3 + S.Door.Log.Count);
                string next = e.Chain[(int)Math.Floor(rng.Next() * e.Chain.Length)];
                int span = Math.Max(0, e.ChainMax - e.ChainMin);
                S.Door.Chains.Add(new DoorChain { Card = next, Day = S.Day + e.ChainMin + (int)Math.Floor(rng.Next() * (span + 1)) });
            }
            if (e.Trust != 0)
            {
                int before = S.Trust;
                ChangeTrust(null, e.Trust);
                v.Trust += S.Trust - before;
            }
            if (e.Alarm != 0 && S.Ending == null)
            {
                int attacks = S.Counterattacks;
                double before = S.Alarm;
                ChangeAlarm(e.Alarm, null);
                if (S.Counterattacks > attacks) v.Counterattack = true;
                else v.Alarm += S.Alarm - before;
            }
            if (e.End != null && S.Ending == null)
            {
                End(e.End);
                v.Ending = e.End;
            }
            if (e.Text != null) v.Result = e.Text;
        }

        /// <summary>Kapının kendi üreteci: harekât tohumu, gün ve amaç. Ana sıradan (<c>S.Rng</c>) tüketmez.</summary>
        Rng DoorRng(int salt) => new Rng(unchecked(S.Seed * 2654435761u ^ (uint)(S.Day + 1) * 2246822519u ^ (uint)salt * 3266489917u));

        public bool DoorFlag(string flag) => S.Door?.Flags.Contains(flag) ?? false;
        public bool DoorSeen(string card) => S.Door?.Log.Any(v => v.Card == card) ?? false;
        public int DoorSeenCount(string card) => S.Door?.Log.Count(v => v.Card == card) ?? 0;

        /// <summary>Erzak nüfusun <c>doorHungerSupplies</c> katının altında (ya da bitti): vardiya aç geçer.</summary>
        public bool Hungry => Shelter != null && (Shelter.Starving || Shelter.Supplies < Shelter.Population * B.doorHungerSupplies);

        /// <summary>Odada en az bir görevli çalışıyor (onarılmış ve personelli).</summary>
        public bool RoomStaffed(string roomId) => (Shelter?.Room(roomId)?.Level ?? 0) > 0 && Shelter.Room(roomId).Workers > 0;

        public int FreeRegions => S.Liberation.Regions.Count(r => r.Completed);
    }
}
