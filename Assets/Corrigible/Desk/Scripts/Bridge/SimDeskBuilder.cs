using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Masanın oyun tarafındaki görünüm durumu: seçim, okundu işaretleri, uyarı fişleri, gösterge geçmişi. Kural değildir;
    /// simülasyon kaydına girmez.
    /// </summary>
    public sealed class DeskBridgeState
    {
        public string Selected;
        public int LogSeen, NewsSeen;
        public string CriticalNewsId;
        public bool TeamsBadge;
        /// <summary>Oyun sonunda “Günlükleri incele”: rapor kapanır, masa gerçek değerlerle açık kalır.</summary>
        public bool Review;
        /// <summary>Eylem sonuçları ve olay bildirimleri: uyarı fişleri (en yenisi önce). Fiş görünümü gösterip kapatınca silinir.</summary>
        public readonly List<DeskMessage> Messages = new List<DeskMessage>();

        // F8.3 ekranları (görünüm durumu; kayda girmez)
        /// <summary>Giriş dosyası açık (her yeni oyunda açılır; “Göreve başla” kapatır).</summary>
        public bool IntroOpen;
        /// <summary>Krizler arası kampanya dosyası açık (oyun sonu raporundan).</summary>
        public bool CampaignOpen;
        /// <summary>Sessiz Zafer: sahte resmî duyuru oynuyor (bitince oyun sonu raporu).</summary>
        public bool FakeWin;
        /// <summary>Ayarlar açık: zaman durur.</summary>
        public bool SettingsOpen;
        /// <summary>Perde raporu açık (S.LastVeil).</summary>
        public bool VeilOpen;
        /// <summary>Baskın emri açık: hedef bölge (onaylanınca taktik harita).</summary>
        public string RaidBrief;
        /// <summary>Okunmuş baskın raporu sayısı; S.RaidLog bundan uzunsa operasyon raporu açılır.</summary>
        public int RaidLogSeen;
        /// <summary>Süren baskının ekibi ve sırası: rapor için (ekip baskında kaybedilebilir).</summary>
        public string RaidTeam;
        public int RaidN;
        /// <summary>Giriş dosyasında “Eğitimle başla” seçildi: eğitim yürütücüsü (DeskTutorialRunner) başlatır.</summary>
        public bool TutorialRequested;
        /// <summary>Gösterilen eğitim notu (yürütücü yazar) ve rehberli eğitim sürerken danışman notunun gizlenmesi.</summary>
        public TutorialVM Tutorial;
        public bool SuppressAdvisor;
        int nextMessageId;

        /// <summary>Uyarı ekler: tür ok (yeşil mühür + zaman), fail (YAPILAMADI), info (SONUÇ). Aynı metin art arda gelmez, kuyruk 6'yı geçmez.</summary>
        public void Push(string text, int day, string kind = "info", string time = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            Messages.RemoveAll(m => m.Text == text);
            Messages.Insert(0, new DeskMessage { Id = ++nextMessageId, Text = text, Day = day, Kind = kind, Time = time });
            if (Messages.Count > 6) Messages.RemoveRange(6, Messages.Count - 6);
        }

        /// <summary>Fiş kapandı (tıklama ya da süre).</summary>
        public bool Remove(int id) => Messages.RemoveAll(m => m.Id == id) > 0;

        /// <summary>4 günden eski uyarılar kendiliğinden düşer.</summary>
        public void Expire(int day) => Messages.RemoveAll(m => day - m.Day > 4);
        public string LastSpeed = "1x";
        public readonly GaugeHistory History = new GaugeHistory();

        /// <summary>Masanın açtığı kâğıtlardan biri zamanı durduruyor mu (olay kartı ve hediye zaten Blocked).</summary>
        public bool WantsHold(WorldState s) =>
            IntroOpen || CampaignOpen || FakeWin || SettingsOpen || VeilOpen || RaidBrief != null || (s != null && s.RaidLog.Count > RaidLogSeen);

        /// <summary>Yeni ya da yüklenen oyun: seçim, işaretler, uyarılar ve açık belgeler sıfırlanır. Fiş kimlikleri artmaya devam eder.</summary>
        public void Reset()
        {
            Selected = null; LogSeen = 0; NewsSeen = 0; TeamsBadge = false; Review = false;
            CriticalNewsId = null;
            CampaignOpen = false; FakeWin = false; VeilOpen = false; RaidBrief = null; RaidTeam = null; RaidN = 0; TutorialRequested = false;
            Tutorial = null; SuppressAdvisor = false;
            Messages.Clear(); History.Clear();
        }
    }

    public sealed class DeskMessage { public int Id, Day; public string Text, Kind, Time; }

    /// <summary>Giriş ve kampanya dosyası için oyunun (GameController) durumu; testlerde boş kalabilir.</summary>
    public sealed class DeskGameInfo
    {
        public Corrigible.Game.Campaign.CampaignState Campaign;
        public bool OnboardingOff, EventsEnabled, ExpTeams, TutorialDone;
        public int OnboardingStage = 4;
        public double ExpTimeScale = 1;
        /// <summary>Kayıtlı oyunun kısa açıklaması (yoksa null).</summary>
        public string SavedSummary;
        /// <summary>Sonraki krizin başlangıç değerleri (yükseltme önizlemesi): şimdiki dengeden.</summary>
        public double StartFon, StartSg, Veto;
    }

    /// <summary>Göstergelerin gün gün gösterilen değerleri (gidişat, döküm şeridi, son 7 gün).</summary>
    public sealed class GaugeHistory
    {
        public struct Point { public int Day; public float Fund, Sg, Trust, Dep, Tek; }

        readonly List<Point> points = new List<Point>();
        public IReadOnlyList<Point> Points => points;

        public void Clear() => points.Clear();

        public void Add(Point p)
        {
            if (points.Count > 0 && points[points.Count - 1].Day == p.Day) { points[points.Count - 1] = p; return; }
            if (points.Count > 0 && p.Day < points[points.Count - 1].Day) points.Clear();
            points.Add(p);
            if (points.Count > 60) points.RemoveAt(0);
        }

        /// <summary>Son <paramref name="days"/> günün günlük ortalama değişimi; veri yetmezse null.</summary>
        public float? Rate(Func<Point, float> f, int days = 10)
        {
            if (points.Count < 3) return null;
            var a = points[Math.Max(0, points.Count - 1 - days)]; var b = points[points.Count - 1];
            int d = Math.Max(1, b.Day - a.Day);
            return (f(b) - f(a)) / d;
        }

        public float? Change(Func<Point, float> f, int days)
        {
            if (points.Count < 2) return null;
            var b = points[points.Count - 1];
            var a = points.LastOrDefault(p => p.Day <= b.Day - days);
            if (a.Day == 0 && a.Fund == 0 && a.Sg == 0) a = points[0];
            return f(b) - f(a);
        }

        public float[] Series(Func<Point, float> f, int n = 30)
        {
            var s = new float[n];
            if (points.Count == 0) return s;
            for (int i = 0; i < n; i++)
            {
                int k = points.Count - n + i;
                s[i] = Mathf.Clamp(f(points[Mathf.Max(0, k)]), 0, 100);
            }
            return s;
        }
    }

    /// <summary>Bir kurulum için girdi: simülasyon ve oyunun sunum durumu.</summary>
    public struct DeskBuildInput
    {
        public Simulation Sim;
        /// <summary>0 duraklatılmış, 1/2/4 hız.</summary>
        public int Speed;
        /// <summary>Günün ilerleyişi (0–1): saat.</summary>
        public float DayProgress;
        /// <summary>Seçili hızda bir günün gerçek süresi (saniye): saat bu hızla ilerler.</summary>
        public float SecondsPerDay;
        public PlayerProfile Profile;
        public DeskBridgeState State;
        public JObject MapSpec;
        /// <summary>Giriş/kampanya dosyası için (null: yalnızca masa).</summary>
        public DeskGameInfo Info;
        /// <summary>Taktik harita (Baskin sahnesi) açık: masa belgeleri bekler.</summary>
        public bool RaidScene;
    }

    /// <summary>
    /// Oyun durumunu masanın görünüm modeline çevirir (F8). Algı Katmanına saygılıdır: sızma ve küresel değerler
    /// <c>Shown</c>/<c>Global</c>'den okunur; İllüzyon sırasında gerçeği ele verecek ayrıntı gösterilmez. Metinler geçerli
    /// dilde üretilir (<c>Loc</c>); görünümün ek çevirisi bunlarda etkisizdir. Kural uygulamaz, simülasyonu değiştirmez.
    /// </summary>
    public sealed partial class SimDeskBuilder
    {
        Simulation sim;
        WorldState S;
        DeskBuildInput input;
        DeskBridgeState st;
        bool illusion, revealed;

        static string N(double v, int d = 1) => DeskFormat.Number(v, d);
        static string Pct(double v, int d = 0) => DeskFormat.Percent(v, d);
        static string Day(int d) => DeskFormat.DayCode(d);
        static string Up(string s) => DeskText.Upper(s);
        static string Num(string s) => "<style=num>" + s + "</style>";

        /// <summary>
        /// Bağlamlı metin: “[bağlam] kaynak” anahtarı (Loc.N ile işaretli) dar alanlara ayrı çeviri verir; Türkçede ve çevirisi
        /// yoksa önek atılır. Eski arayüzün aynı kaynak metni (ör. SİYASİ GÜÇ → POLITICAL CAPITAL) değişmez.
        /// </summary>
        public static string Ctx(string keyed)
        {
            string t = Loc.T(keyed);
            int i = keyed.IndexOf("] ", System.StringComparison.Ordinal);
            return t == keyed && keyed.StartsWith("[") && i > 0 ? keyed.Substring(i + 2) : t;
        }
        static readonly string Approx = DeskFormat.Approx.ToString();
        static RegionDef Def(string id) => GameData.Region(id);
        static float Deg(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        public DeskViewModel Build(DeskBuildInput inp)
        {
            input = inp; sim = inp.Sim; S = sim.S; st = inp.State;
            illusion = S.Illusion != null && !sim.EndRevealed;
            revealed = sim.EndRevealed;
            var g = sim.Global();
            st.History.Add(new GaugeHistory.Point { Day = S.Day, Fund = (float)S.Fon, Sg = (float)S.Sg, Trust = (float)g.Trust, Dep = (float)g.Dep, Tek = (float)g.Tek });

            var vm = new DeskViewModel
            {
                Screen = S.Ending != null && !st.Review ? "C4" : S.PendingEvent != null || S.PendingGift != null ? "C3" : illusion ? "C2" : "C1",
                Illusion = illusion,
                Clock = Clock(),
                Rail = Rail(),
                Advisor = Advisor(),
                Goal = Goal(),
                Research = Research(),
                Map = Map(),
                Telex = Telex(),
                KillSwitch = KillSwitch(),
                AiLog = AiLog(),
                Guide = Guide(),
            };
            vm.Gauges.AddRange(Gauges(g));
            vm.Dossier = st.Selected != null ? Dossier(st.Selected) : null;
            vm.News = S.News.Select(TelexItem).ToList();
            vm.Teams = sim.TeamsOn ? S.Teams.Select(TeamRow).ToList() : new List<TeamRowVM>();
            vm.TeamHire = TeamHire();
            vm.Dock = Dock();
            if (S.Raid != null) { st.RaidTeam = sim.Team(S.Raid.Team)?.Name; st.RaidN = S.Raid.N; }
            vm.Event = S.PendingEvent != null ? Dilemma() : S.PendingGift != null ? Gift() : Doc();
            if (vm.Event != null && vm.Screen != "C4") vm.Screen = "C3";
            vm.GameOver = S.Ending != null && !st.Review && !sim.HideEnding && !st.CampaignOpen ? GameOver() : null;
            vm.Intro = st.IntroOpen && inp.Info != null ? Intro(inp.Info) : null;
            vm.Campaign = st.CampaignOpen && inp.Info?.Campaign != null ? Campaign(inp.Info) : null;
            vm.FakeWin = st.FakeWin ? FakeWin() : null;
            vm.Toasts = Toasts();
            vm.Tutorial = st.Tutorial;
            // Kritik haber olay/hediye, giriş ve taktik haritayı örtmez; masa yeniden erişilebilir olunca açılır.
            if (S.Ending == null && vm.Intro == null && vm.Event == null && vm.Campaign == null && vm.FakeWin == null
                && !st.SettingsOpen && !inp.RaidScene && st.Tutorial?.Kind != "step")
                vm.CriticalNewsId = st.CriticalNewsId;
            return vm;
        }

        // ------------------------------------------------------------------ ray ve saat
        ClockVM Clock()
        {
            int minutes = Mathf.Clamp(Mathf.FloorToInt(input.DayProgress * 24 * 60), 0, 24 * 60 - 1);
            string speed = input.Speed switch { 1 => "1x", 2 => "2x", 4 => "4x", _ => st.LastSpeed };
            if (input.Speed > 0) st.LastSpeed = speed;
            return new ClockVM
            {
                Day = S.Day, DayLabel = Loc.T("GÜN"), DayText = S.Day.ToString("000"),
                Time = $"{minutes / 60:00}:{minutes % 60:00}",
                Speed = speed,
                Paused = input.Speed == 0 || S.Blocked || S.Ending != null,
                Speeds = { "pause", "1x", "2x", "4x" },
                MinutesPerSecond = input.Speed > 0 && input.SecondsPerDay > 0 ? 24 * 60 / input.SecondsPerDay : 0,
            };
        }

        string TimeText => Clock().Time;

        /// <summary>Son kurulumdaki saat (uyarı fişinin zamanı).</summary>
        public string TimeNow => input.Sim != null ? TimeText : null;

        RailVM Rail()
        {
            var type = sim.Type;
            string profile = !S.Aware || illusion ? Loc.T("Etkinlik saptanmadı") : type.Id switch
            {
                "solucan" => Loc.T("Gürültülü profil"),
                "kahin" => Loc.T("Sessiz profil"),
                _ => Loc.T("Yanıltıcı profil"),
            };
            // İllüzyonda teleks gecikmesi 0 ms: ekranlar artık yerel değil (C2 ipucu)
            int latency = illusion ? 0 : 180 + (S.Day * 37 + 11) % 90;
            return new RailVM
            {
                PlaqueTitle = Loc.T("SON ŞALTER"), PlaqueSub = Loc.T("KYGA · KRİZ MASASI"),
                Classification = Loc.T("KYGA-07 · GİZLİ"), EscKey = "Esc",
                Rival = new RivalVM { Stamp = Loc.T("RAKİP YZ"), StampRot = -3, Name = Up(type.Name), Profile = profile },
                Live = new LiveVM { On = true, LatencyMs = latency, Text = Loc.F("CANLI · {0} ms", latency) },
            };
        }

        // ------------------------------------------------------------------ göstergeler
        static DeltaVM Delta(float? perDay, bool upGood, int decimals = 1)
        {
            if (perDay == null || Mathf.Abs(perDay.Value) < 0.05f)
                return new DeltaVM { Dir = "flat", Text = N(0, decimals) + Loc.T("/gün"), Status = "neutral" };
            bool up = perDay.Value > 0;
            return new DeltaVM { Dir = up ? "up" : "down", Text = N(Mathf.Abs(perDay.Value), decimals) + Loc.T("/gün"), Status = up == upGood ? "good" : "threat" };
        }

        /// <summary>Eşiğe kalan gün: gidişat eşiğe doğru değilse null.</summary>
        static int? DaysTo(float value, float target, float? rate)
        {
            if (rate == null || Mathf.Abs(rate.Value) < 0.02f) return null;
            float d = (target - value) / rate.Value;
            // Çok uzak gidişat (ör. ~1092 gün) bilgi taşımaz
            return d > 0 && d < 100 ? Mathf.Max(1, Mathf.RoundToInt(d)) : (int?)null;
        }

        IEnumerable<GaugeVM> Gauges(ShownGlobal g)
        {
            var h = st.History;
            int monitored = S.Regions.Count(r => r.Monitored);
            bool coverage = monitored >= 9;
            bool flagged = S.Regions.Any(r => r.MediaFlag);
            float k = (float)sim.TimeScale;

            // Fon: gelir ve araştırma gideri (günlük, zaman ölçeğiyle)
            float income = (float)sim.FundRate, spend = (float)(sim.ResearchNeed / k);
            float fund = (float)S.Fon;
            var fundG = new GaugeVM
            {
                Id = "fund", Label = Loc.T("FON"), Value = fund, Display = N(fund), Trust = "verified", StampRot = -4,
                NeedleDeg = DeskMath.NeedleDegrees(fund),
                Delta = Delta(income - spend, true),
                Note = Loc.F("Gelir {0} · Gider {1} /gün", Num(N(income)), Num(N(spend))),
            };
            fundG.Breakdown = Breakdown(fundG, x => x.Fund,
                (Loc.T("Hükümet ödenekleri"), income),
                (Loc.T("Araştırma"), -spend));
            if (sim.ModernEconomy) EconomyBreakdown(fundG);

            float sg = (float)S.Sg, sgRate = (float)sim.SgRate;
            var week = h.Change(x => x.Sg, 7);
            var sgG = new GaugeVM
            {
                Id = "political", Label = Ctx(Loc.N("[gösterge] SİYASİ GÜÇ")), Value = sg, Display = N(sg, 0), Trust = "verified", StampRot = 3,
                NeedleDeg = DeskMath.NeedleDegrees(sg),
                Delta = Delta(sgRate, true, 2),
                Note = week.HasValue ? Loc.F("Son 7 gün {0}", Num(DeskFormat.Signed(week.Value))) : Loc.F("Küresel Son Şalter {0}", Num("50")),
            };
            sgG.Breakdown = Breakdown(sgG, x => x.Sg,
                (Loc.T("Hükümet desteği"), S.Aware ? 0.25f / k : sgRate),
                (Loc.T("Doğrulanan YZ bölgeleri"), S.Aware ? 0.03f * S.Regions.Count(r => r.Detected) / k : 0));

            // Küresel güven: izleme kapsamı geniş değilse rapordur (≈); İllüzyonda her şey “onaylı” görünür
            float trust = (float)g.Trust;
            string trustLvl = illusion || revealed || coverage ? "verified" : "report";
            var trustRate = illusion ? 0 : h.Rate(x => x.Trust);
            int? toLoss = DaysTo(trust, 15, trustRate);
            var trustG = new GaugeVM
            {
                Id = "globalTrust", Label = Loc.T("KÜRESEL GÜVEN"), Value = trust, Trust = trustLvl,
                Display = (trustLvl == "report" ? Approx : "") + N(trust),
                NeedleDeg = DeskMath.NeedleDegrees(trust),
                Uncertainty = trustLvl == "report" ? 3 : (float?)null,
                DangerZone = new[] { 0f, 15f }, Threshold = 15,
                Delta = Delta(trustRate, true),
                Note = illusion ? Loc.T("Risk yok") : toLoss.HasValue
                    ? Loc.F("Kayıp eşiği {0} · ~{1} gün", Num("15"), toLoss.Value)
                    : Loc.F("Kayıp eşiği {0}", Num("15")),
            };
            trustG.Breakdown = Breakdown(trustG, x => x.Trust,
                (Loc.T("İttifaktaki bölgeler"), S.Regions.Count(r => !r.Left)),
                (Loc.T("Karantinadaki bölgeler"), -S.Regions.Count(r => r.Q)));

            float dep = (float)g.Dep, veto = (float)sim.VetoThreshold;
            string depLvl = illusion || revealed || coverage ? "verified" : flagged ? "suspicious" : "report";
            var depRate = illusion ? 0 : h.Rate(x => x.Dep);
            int? toVeto = DaysTo(dep, veto, depRate);
            var depG = new GaugeVM
            {
                Id = "dependency", Label = Loc.T("BAĞIMLILIK"), Value = dep, Trust = depLvl,
                Display = (depLvl == "report" ? Approx : "") + N(dep),
                NeedleDeg = DeskMath.NeedleDegrees(dep),
                DangerZone = new[] { veto, 100f }, Threshold = veto,
                DialQuestionMark = depLvl == "suspicious",
                Delta = Delta(depRate, false),
                Note = illusion ? Loc.T("Risk yok") : toVeto.HasValue
                    ? Loc.F("Veto eşiği {0} · ~{1} gün", Num(N(veto, 0)), toVeto.Value)
                    : Loc.F("Veto eşiği {0}", Num(N(veto, 0))),
            };
            depG.Breakdown = Breakdown(depG, x => x.Dep,
                (Loc.T("Veto eşiğini aşan bölgeler"), S.Regions.Count(r => r.Dep > veto)),
                (Loc.T("Hediye teklifleri"), S.Has("hediye") ? 2 : 1));

            // Tekillik: Yorumlanabilirlik YZ'nin içini gösterene kadar tahmin (±6)
            float tek = (float)g.Tek;
            string tekLvl = illusion || revealed || S.Done("yorum") ? "verified" : "report";
            var tekRate = illusion ? 0 : h.Rate(x => x.Tek);
            int? toEnd = DaysTo(tek, 100, tekRate);
            var tekG = new GaugeVM
            {
                Id = "singularity", Label = Loc.T("TEKİLLİK"), Value = tek, Trust = tekLvl,
                Display = (tekLvl == "report" ? Approx : "") + Pct(tek, 1),
                Suffix = tekLvl == "report" ? "±6" : null,
                NeedleDeg = DeskMath.NeedleDegrees(tek),
                Uncertainty = tekLvl == "report" ? 6 : (float?)null,
                DangerZone = new[] { 90f, 100f }, Threshold = 100,
                Delta = Delta(tekRate, false),
                Note = illusion ? Loc.T("Tehdit saptanmadı") : toEnd.HasValue
                    ? Loc.F("{0}'e {1}", Pct(100), Num(Loc.F("~{0} gün", toEnd.Value)))
                    : Loc.F("{0} = kayıp", Pct(100)),
            };
            tekG.Breakdown = Breakdown(tekG, x => x.Tek,
                (Loc.T("Ağ büyümesi"), tekRate ?? 0),
                (Loc.T("Düzeltilebilirlik"), S.Done("duzelt") ? -0.25f : 0));

            return new[] { fundG, sgG, trustG, depG, tekG };
        }

        BreakdownVM Breakdown(GaugeVM g, Func<GaugeHistory.Point, float> f, params (string label, float value)[] items)
        {
            bool upGood = g.Id == "fund" || g.Id == "political" || g.Id == "globalTrust";
            var b = new BreakdownVM
            {
                Title = g.Label + Loc.T(" · DÖKÜM"), Note = Loc.T("son 30 gün"), Trust = g.Trust,
                Total = "= " + (g.Delta?.Dir == "down" ? DeskFormat.Minus.ToString() : "+") + g.Delta?.Text,
                TotalRight = StripTags(g.Note),
                Series = st.History.Series(f),
            };
            foreach (var (label, value) in items)
            {
                if (Mathf.Abs(value) < 0.005f) continue;
                b.Items.Add(new BreakdownItemVM { Label = label, Value = DeskFormat.Signed(value, Mathf.Abs(value) < 1 ? 2 : 1), Status = (value > 0) == upGood ? "good" : "threat" });
            }
            return b;
        }

        static string StripTags(string s) => string.IsNullOrEmpty(s) ? s : System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");

        // ------------------------------------------------------------------ danışman ve hedef
        static string AdviceCommand(string key) => key switch
        {
            "aware" or "idle" or "starved" or "tek50" => "OpenWindow:Research",
            "medya" or "hint" => "OpenWindow:News",
            "start" => "OpenWindow:Teams",
            _ => null,
        };

        AdvisorVM Advisor()
        {
            if (S.Ending != null && st.Review)
                return new AdvisorVM
                {
                    Title = Loc.T("OYUN BİTTİ"), TitleColor = "red",
                    Text = Loc.T("Harita, göstergeler ve haber bandı artık gerçek değerleri gösteriyor."),
                    Actions =
                    {
                        new CommandVM { Label = Loc.T("Rapora dön"), Kind = "link", Command = "ShowReport" },
                        new CommandVM { Label = Loc.T("Yeniden başla"), Kind = "link", Command = "Restart" },
                    },
                };
            // Rehberli eğitim sürerken danışman notu eğitimle yarışmasın
            if (st.SuppressAdvisor) return null;
            var a = S.Advisor;
            if (a == null || a.Dismissed || S.Ending != null) return null;
            var vm = new AdvisorVM { Title = Loc.T("DANIŞMAN NOTU"), TitleColor = "red", Text = a.Text };
            var cmd = AdviceCommand(a.Key);
            if (cmd != null) vm.Actions.Add(new CommandVM { Label = Loc.T("Göster"), Kind = "link", Command = cmd });
            vm.Actions.Add(new CommandVM { Label = Loc.T("Anladım"), Kind = "link", Command = "DismissAdvisor" });
            return vm;
        }

        GoalVM Goal()
        {
            var align = Explain.Alignment(sim);
            var erase = Explain.Erasure(sim);
            var vm = new GoalVM
            {
                Title = Loc.T("HEDEF"), Subtitle = Loc.T("Kazanma yolları"), Boxes = 7,
                Tracks =
                {
                    new GoalTrackVM { Name = Loc.T("Hizalama"), Percent = Mathf.Round(align.Progress * 100) },
                    new GoalTrackVM { Name = Loc.T("Silme"), Percent = Mathf.Round(erase.Progress * 100) },
                },
            };
            if (illusion) vm.Stamp = new StampVM { Text = Loc.T("TEMİZ"), Color = "green", Rot = -3 };
            if (S.Ending != null) { vm.Next = new GoalNextVM { Label = Loc.T("Sıradaki:"), Name = Loc.T("Oyun bitti"), Right = "" }; return vm; }
            if (S.Research.Cur != null)
            {
                var rs = GameData.ResearchItem(S.Research.Cur);
                vm.Next = new GoalNextVM { Label = Loc.T("Sıradaki:"), Name = rs.Name, Eta = Loc.F("{0} gün", DaysLeft(rs)), Link = Loc.T("Araştırma ›") };
            }
            else if (!S.Aware) vm.Next = new GoalNextVM { Label = Loc.T("Sıradaki:"), Name = Loc.T("YZ'yi tespit et"), Right = Loc.T("İzleme ağı kur") };
            else
            {
                var next = GameData.Research.FirstOrDefault(r => r.Branch == GameData.BranchAlign && !S.Done(r.Id));
                vm.Next = next != null
                    ? new GoalNextVM { Label = Loc.T("Sıradaki:"), Name = next.Name, Eta = Loc.F("{0} gün", ResearchDays(next)), Link = Loc.T("Araştırma ›") }
                    : new GoalNextVM { Label = Loc.T("Sıradaki:"), Name = erase.Text, Right = "" };
            }
            return vm;
        }

        // ------------------------------------------------------------------ teleks, haberler, alt sekmeler, zarf
        TelexVM Telex()
        {
            var vm = new TelexVM { Label = Loc.T("TELEKS"), LampOn = S.News.Count > 0 && S.Day - S.News[0].Day <= 1 };
            foreach (var h in S.News.Take(8)) vm.Items.Add(TelexItem(h));
            return vm;
        }

        TelexItemVM TelexItem(Headline h)
        {
            var it = new TelexItemVM
            {
                Id = h.Id,
                Critical = h.Kind == "alarm",
                Day = Day(h.Day),
                Source = Up(Loc.T(h.Agency)) + ":",
                Text = Up(h.Text),
            };
            // Doğrulama sonucu ya da oyun sonu gerçeği: sahte başlık damgalanır
            if (h.Verified == "fake" || (revealed && h.Fake))
            {
                it.Fake = true;
                it.Stamp = new StampVM { Text = Loc.T("SAHTE"), Color = "red", Rot = -5 };
            }
            else it.Trust = h.Verified == "true" ? "verified" : h.Verified == "unknown" ? "unknown" : h.Suspect ? "suspicious" : "report";
            if (it.Trust == "verified") it.StampRot = -3;
            return it;
        }

        TeamRowVM TeamRow(TeamState t)
        {
            var where = t.Dest != null
                ? Loc.F("→ {0} · {1} gün", Def(t.Dest).Name, t.Eta)
                : Loc.F("{0} · {1}", Def(t.At).Name, TeamStatusText(t));
            // Moral yerine hazır olma: hazır 4, görevde 3, yolda 2, yaralı/kuruluyor 1
            int morale = t.Down > 0 ? 1 : t.Dest != null ? 2 : t.Busy > 0 ? 3 : 4;
            return new TeamRowVM
            {
                Id = TeamLetter(t), Name = Loc.F("Ekip {0}", t.Name), Where = where,
                MoraleLabel = Loc.T("Hazırlık"), Morale = morale, MoraleMax = 4,
                LastContact = Loc.F("Son temas {0}", Day(S.Day)), Region = t.At ?? t.Dest,
            };
        }

        static string TeamLetter(TeamState t) => string.IsNullOrEmpty(t.Name) ? "?" : t.Name.Substring(0, 1).ToUpperInvariant();

        static string TeamStatusText(TeamState t) =>
            t.Down > 0 ? Loc.F("{0} · {1} gün", t.DownWhy ?? Loc.T("iyileşiyor"), t.Down)
            : t.Busy > 0 ? Loc.F("görevde · {0} gün", t.Busy)
            : Loc.T("Hazır");

        List<DockTabVM> Dock()
        {
            int unreadLog = Mathf.Max(0, S.Log.Count - st.LogSeen);
            int newNews = Mathf.Max(0, S.Seq - st.NewsSeen) > 0 ? S.News.Count(h => ParseSeq(h.Id) > st.NewsSeen) : 0;
            bool researchIdle = S.Ending == null && S.Research.Cur == null && GameData.Research.Any(x => !S.Done(x.Id));
            return new List<DockTabVM>
            {
                new DockTabVM { Id = "research", Label = Loc.T("ARAŞTIRMA"), Key = "R", Color = "#D9C08B", Badge = researchIdle ? new BadgeVM { Type = "dot" } : null },
                new DockTabVM { Id = "news", Label = Loc.T("HABERLER"), Key = "N", Color = "#EFE4BC", Badge = newNews > 0 ? new BadgeVM { Type = "count", Value = Mathf.Min(newNews, 9) } : null },
                new DockTabVM { Id = "teams", Label = Loc.T("SAHA EKİPLERİ"), Key = "T", Color = "#C9CDB4", Disabled = !sim.TeamsOn, Badge = st.TeamsBadge ? new BadgeVM { Type = "dot" } : null },
                new DockTabVM { Id = "aiLog", Label = Loc.T("YZ GÜNLÜĞÜ"), Key = "L", Color = "#D8A69B", Badge = unreadLog > 0 ? new BadgeVM { Type = "dot" } : null },
                new DockTabVM { Id = "guide", Label = Loc.T("REHBER"), Key = "H", Color = "#BFC8CD" },
            };
        }

        /// <summary>Başlık kimliği “gün-sıra”; sıra numarası okundu işareti için.</summary>
        public static int ParseSeq(string id)
        {
            int dash = id?.LastIndexOf('-') ?? -1;
            return dash >= 0 && int.TryParse(id.Substring(dash + 1), out int n) ? n : 0;
        }

        KillSwitchVM KillSwitch()
        {
            bool ready = S.Sg >= 50 && S.Ending == null;
            return new KillSwitchVM
            {
                Title = Loc.T("KÜRESEL SON ŞALTER"),
                Sub = S.Ending != null ? Loc.T("Oyun bitti")
                    : ready ? Loc.T("Mührü kır · 3 sn basılı tut")
                    : Loc.F("{0} SG gerekli · şu an {1}", 50, N(S.Sg, 0)),
                State = ready ? "normal" : "disabled",
                HoldSeconds = 3,
            };
        }

        // ------------------------------------------------------------------ YZ günlüğü ve rehber
        AiLogVM AiLog()
        {
            int unread = Mathf.Max(0, S.Log.Count - st.LogSeen);
            var vm = new AiLogVM
            {
                Title = Loc.T("YZ GÜNLÜĞÜ"), Right = Day(S.Day) + " · " + TimeText,
                Footer = Loc.F("Kaynak: karantina tamponu · {0} satır", Mathf.Min(S.Log.Count, 40)),
            };
            for (int i = 0; i < S.Log.Count && i < 40; i++)
            {
                var e = S.Log[i];
                vm.Lines.Add(new AiLogLineVM { Time = Day(e.Day), Text = "> " + e.Text, Read = i >= unread });
            }
            return vm;
        }

        GuideVM Guide()
        {
            var vm = new GuideVM
            {
                Sections =
                {
                    new GuideSectionVM { Title = Loc.T("GÜVEN İŞARETLERİ"), Body = Loc.T("Mavi damga ölçülmüş değeri, karbon kopya yalnızca raporu, turuncu kalem halkası şüpheli kaynağı, kesik daire veri olmadığını gösterir. Renk tek başına bilgi taşımaz.") },
                    new GuideSectionVM { Title = Loc.T("EYLEMLER"), Body = Loc.T("Dosyada 1–4 tuşları bölge eylemlerini uygular. Üstüne geldiğiniz eylemin etkisi sarı ÖNİZLEME notunda görünür. Son Şalter onay ister.") },
                    new GuideSectionVM { Title = Loc.T("KISAYOLLAR"), Body = Loc.T("R araştırma · N haberler · T saha ekipleri · L YZ günlüğü · H rehber · Boşluk duraklat · +/− hız · Esc en üstteki pencereyi kapatır.") },
                    new GuideSectionVM
                    {
                        Title = Loc.T("EĞİTİM"), Body = Loc.T("Rehberli eğitimi ve bütün ipuçlarını yeniden görmek için yeni bir oyun açar."),
                        Action = new CommandVM { Label = Loc.T("Eğitimi baştan oynat"), Kind = "link", Command = "Tutorial:Replay" },
                    },
                },
            };
            var p = input.Profile;
            if (p != null && p.runs > 0)
            {
                var top = p.TopAction();
                vm.Sections.Add(new GuideSectionVM
                {
                    Title = Loc.T("YZ SENİ TANIYOR"),
                    Body = Loc.F("Oyun: {0} · alışkanlık: {1} · yanılttığın: {2} · yırttığın perde: {3}", p.runs,
                        top.Value > 0 ? ActionName(top.Key) : Loc.T("belirsiz"), p.misreads, p.veils),
                });
            }
            return vm;
        }

        static string ActionName(string a) => a switch
        {
            Actions.Monitor => Loc.T("izleme ağı"),
            Actions.Audit => Loc.T("denetim"),
            Actions.Quarantine => Loc.T("karantina"),
            _ => Loc.T("Son Şalter"),
        };
    }
}
