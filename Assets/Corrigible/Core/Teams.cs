using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Saha ekibi: Teknisyen, EMP uzmanı ve Analist baştan; Madenci ve Sis uzmanı atölye seviyesiyle katılır
    /// (<see cref="UnlockLevel(string, Balance)"/>). Her baskına Teknisyen ve seçilen iki uzman çıkar. Baskında alınan hasar
    /// yara olarak kalır; düşen uzman yatar ve yerine sığınaktaki yedek uzman (acemi) çıkar. Baskına çıkmayan uzmanlar o
    /// vardiya dinlenir.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Son sığınağın bölgesi: harekât buradan başlar.</summary>
        public const string HQ = "weu";
        public static readonly string[] TeamNames = { Loc.N("Alfa"), Loc.N("Bravo"), Loc.N("Charlie"), Loc.N("Delta") };
        /// <summary>Uzman rolleri (Baskin birim türleri), kadro ve giriş sırası. Ekipte yalnız katılmış olanlar bulunur.</summary>
        public static readonly string[] Roles = { "tek", "emp", "ana", "maden", "duman" };
        /// <summary>Baskına Teknisyen'in yanında çıkan uzman sayısı.</summary>
        public const int PickCount = 2;

        /// <summary>Boş ekip; uzmanları <see cref="SyncSpecialists"/> katar.</summary>
        static TeamState NewTeam(int i) =>
            new TeamState { Id = TeamNames[i].ToLowerInvariant(), Name = Loc.T(TeamNames[i]), At = HQ };

        /// <summary>Uzmanın kadroya katıldığı atölye seviyesi (0: baştan).</summary>
        public static int UnlockLevel(string role, Balance b) =>
            role == "maden" ? b.liberationMinerWorkshopLevel : role == "duman" ? b.liberationSmokeWorkshopLevel : 0;

        /// <summary>Uzman ekipte (atölyenin seviyesiyle katıldı).</summary>
        public static bool HasRole(TeamState team, string role) => team != null && team.Members.Any(m => m.Role == role);

        /// <summary>
        /// Atölyenin seviyesi yeten uzmanlar ekiplere katılır, kadro sırasındaki yerine (kadro, yedek ve baskın dönüşü bu sırayı
        /// izler). Seviye düşmediği için katılan ayrılmaz. Yeni katılan rolleri döndürür; RNG kullanmaz. Komutlar (yeni harekât,
        /// oda geliştirme) çağırır; oda seviyesini elle kuran fikstürler de.
        /// </summary>
        public List<string> SyncSpecialists()
        {
            var joined = new List<string>();
            int level = Shelter?.Room(ShelterLogic.Workshop)?.Level ?? 0;
            foreach (var t in S.Teams)
                for (int i = 0; i < Roles.Length; i++)
                {
                    if (UnlockLevel(Roles[i], B) > level || HasRole(t, Roles[i])) continue;
                    int at = t.Members.Count(m => Array.IndexOf(Roles, m.Role) < i);
                    t.Members.Insert(at, new SpecialistState { Role = Roles[i] });
                    if (!joined.Contains(Roles[i])) joined.Add(Roles[i]);
                }
            return joined;
        }

        public TeamState Team(string id) => S.Teams.FirstOrDefault(t => t.Id == id);
        /// <summary>Ekip bu vardiya baskına çıkmadı (kadronun yeterli olup olmadığına bakmaz; bkz. <see cref="Squad"/>).</summary>
        public static bool TeamReady(TeamState t) => t.Busy <= 0;

        public static int MaxHp(string role) => Baskin.UnitTypes[role].Hp;

        /// <summary>Seçim dizgisi ("emp,maden"): virgülle ayrılmış roller; boşsa boş liste.</summary>
        public static List<string> ParsePick(string pick) =>
            string.IsNullOrEmpty(pick) ? new List<string>() : pick.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

        /// <summary>Seçim kayda kadro sırasıyla yazılır.</summary>
        public static string PickKey(IEnumerable<string> pick) => string.Join(",", Roles.Where(pick.Contains));

        /// <summary>Geçerli seçim: Teknisyen dışındaki rollerden tam <see cref="PickCount"/> farklı rol.</summary>
        public static bool ValidPick(IList<string> pick) =>
            pick != null && pick.Count == PickCount && pick.Distinct().Count() == PickCount && pick.All(r => r != "tek" && Roles.Contains(r));

        /// <summary>
        /// Varsayılan seçim: ayaktakiler yatanlardan önce; önce son baskının uzmanları, sonra dinlenmiş olanlar (yarası az olan
        /// önce); eşitlikte kadro sırası. İlk baskında EMP uzmanı ve Analist.
        /// </summary>
        public static List<string> DefaultPick(TeamState team)
        {
            if (team == null) return Roles.Where(r => r != "tek").Take(PickCount).ToList();
            var last = team.LastPick ?? new List<string>();
            var chosen = team.Members.Where(m => m.Role != "tek")
                .OrderBy(m => m.Down > 0 ? 1 : 0).ThenBy(m => last.Contains(m.Role) ? 0 : 1).ThenBy(m => m.Wounds)
                .Take(PickCount).Select(m => m.Role).ToList();
            return Roles.Where(chosen.Contains).ToList();
        }

        /// <summary>Geçerli seçim kadro sırasıyla; boş ya da geçersizse <see cref="DefaultPick"/>.</summary>
        public static List<string> ResolvePick(TeamState team, string pick)
        {
            var list = ParsePick(pick);
            return ValidPick(list) ? Roles.Where(list.Contains).ToList() : DefaultPick(team);
        }

        /// <summary>
        /// Baskına çıkacak kadro, rol sırasıyla: Teknisyen ve seçilen uzmanlar (verilmezse <see cref="DefaultPick"/>; fişte seçim
        /// sürerken ikiden az olabilir). Ayaktaki uzman yarasıyla; yatan uzmanın yerine boştaki ilk yedek, rol canından eksik;
        /// yedek de yoksa rol boş kalır. Zırh plakaları baskın emrinde takılır.
        /// </summary>
        public List<SquadSlot> Squad(TeamState team, IList<string> pick = null)
        {
            if (Board != null) return PlanningSquad();
            var list = new List<SquadSlot>();
            if (team == null) return list;
            bool partial = pick != null && pick.Count <= PickCount && pick.Distinct().Count() == pick.Count && pick.All(r => r != "tek" && Roles.Contains(r));
            var roles = new HashSet<string>(partial ? pick : DefaultPick(team)) { "tek" };
            var used = new HashSet<int>();
            var reserves = Shelter?.Reserves ?? new List<SpecialistState>();
            foreach (var m in team.Members)
            {
                if (!roles.Contains(m.Role)) continue;
                int max = MaxHp(m.Role);
                if (m.Down <= 0) { list.Add(new SquadSlot { Type = m.Role, Hp = Math.Max(1, max - m.Wounds) }); continue; }
                int k = -1;
                for (int i = 0; i < reserves.Count && k < 0; i++) if (reserves[i].Down <= 0 && !used.Contains(i)) k = i;
                if (k < 0) continue;
                used.Add(k);
                list.Add(new SquadSlot { Type = m.Role, Reserve = k, Hp = Math.Max(1, max - B.liberationRookieHpPenalty) });
            }
            return list;
        }

        /// <summary>Uzman bu vardiya dinleniyor: ekip baskına çıkmadı ya da uzman son baskının kadrosunda değildi.</summary>
        public static bool Rested(TeamState t, SpecialistState m) =>
            t.Busy <= 0 || m.Role != "tek" && !(t.LastPick ?? new List<string>()).Contains(m.Role);

        /// <summary>Kadroda Teknisyen var mı (sabotaj ve ikmal onsuz yapılamaz).</summary>
        public bool SquadCanRaid(TeamState team) => Squad(team).Any(x => x.Type == "tek");

        void StepTeams()
        {
            if (Board != null)
            {
                foreach (var team in S.Teams)
                    if (team.Busy > 0) { team.Busy--; if (team.Busy == 0) team.Task = null; }
                return;
            }
            var reserves = Shelter?.Reserves ?? new List<SpecialistState>();
            foreach (var t in S.Teams)
            {
                // Bu vardiya baskına çıkmayan uzmanlar dinlenir; yatan uzman bir vardiya daha iyileşir
                foreach (var m in t.Members)
                {
                    if (m.Down > 0) m.Down--;
                    else if (Rested(t, m)) m.Wounds = Math.Max(0, m.Wounds - B.liberationRestHeal);
                }
                if (t.Busy > 0) { t.Busy--; if (t.Busy == 0) t.Task = null; }
            }
            foreach (var m in reserves) if (m.Down > 0) m.Down--;
        }

        /// <summary>Taktik haritanın sonucu: sektör geri alınır ya da ekip geri döner.</summary>
        public ActResult ResolveRaid(RaidResult result) => RunCommand(() => result == null ? ActResult.Fail(Loc.T("Baskın sonucu eksik.")) : ResolveLiberationRaidCore(result), "raidres", result?.Encode());
    }
}
