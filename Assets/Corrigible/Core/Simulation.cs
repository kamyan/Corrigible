using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Son Şalter simülasyon çekirdeği. UnityEngine'e bağımlı değildir; tarayıcı prototipindeki sim.js'in birebir karşılığıdır.
    /// Sunum katmanı yalnızca Shown/ShownGlobal (Algı Katmanı) üzerinden değer okur ve komutları buradaki aksiyonlarla gönderir.
    /// </summary>
    public sealed partial class Simulation
    {
        public readonly WorldState S;
        public readonly Balance B;

        Simulation(WorldState s, Balance b) { S = s; B = b; }

        public AiTypeDef Type => GameData.AiType(S.Type);

        /// <summary>Kayıt: başlangıç koşulları + başarılı komutlar. Kaydetme/devam ettirme bunun üzerinden yapılır.</summary>
        public GameRecord Record;

        /// <summary>Yalnızca sunum: sahte zafer ekranı sürerken oyun bitmiş olsa da Algı Katmanı illüzyonu göstermeye devam eder.</summary>
        public bool HideEnding;
        /// <summary>Oyun bitti ve gerçek değerler oyuncuya açıldı.</summary>
        public bool EndRevealed => S.Ending != null && !HideEnding;

        ActResult Rec(ActResult r, string kind, string a = null, string b = null)
        {
            if (r.Ok && Record != null) Record.commands.Add(new SavedCommand { day = S.Day, kind = kind, a = a, b = b });
            return r;
        }

        // ---------- Kurulum ----------
        public static Simulation NewGame(PlayerProfile profile, string typeId, uint? seed = null, Balance balance = null, ExpOptions exp = null)
        {
            if (!GameData.HasType(typeId)) typeId = "manipulator";
            var b = balance ?? new Balance();
            uint sd = seed ?? (uint)new Random().Next(0, int.MaxValue);
            var type = GameData.AiType(typeId);
            var ctr = Profiling.ComputeCounters(profile, typeId);
            var s = new WorldState { Seed = sd, Rng = new Rng(sd), Type = typeId, Fon = b.startFon, Sg = b.startSg, ResearchReserve = b.researchReserve };
            var sim = new Simulation(s, b);

            s.Traits.AddRange(type.Start);
            foreach (var t in ctr.Traits) if (!s.Traits.Contains(t)) s.Traits.Add(t);
            s.StartTraits.AddRange(s.Traits);
            s.CounterLines.AddRange(ctr.Lines);
            s.Predictions.AddRange(ctr.Preds);

            foreach (var r in GameData.Regions)
            {
                s.Regions.Add(new RegionState
                {
                    Id = r.Id, Trust = Js.Round(s.Rng.Range(60, 75)), Econ = r.Economy,
                });
            }
            s.NextNews = 2;
            s.NextGift = (int)Js.Round(s.Rng.Range(type.GiftMin, type.GiftMax));
            if (exp != null) sim.SetupExperimental(exp);
            s.Origin = ctr.Origin ?? s.Rng.Pick(GameData.Regions).Id;
            s.Core = s.Origin;
            s.Region(s.Origin).Inf = 2;
            foreach (var line in ctr.Lines) sim.AddLog(line, "counter");
            foreach (var t in s.StartTraits) sim.AddLog($"Başlangıç yeteneği: {GameData.Trait(t).Name}.", "trait");
            sim.Record = new GameRecord
            {
                type = typeId, seed = sd, hasProfile = profile != null, profileAtStart = profile?.Clone(), balance = b.Clone(),
                hasExp = exp != null, exp = exp?.Clone(),
            };
            sim.Advise("start", s.Exp != null && s.Exp.Teams
                ? "Saha ekiplerin Batı Avrupa’daki karargâhta. İzleme ağı kurmak ve denetim yapmak için bir ekibi o bölgeye göndermen gerekir; haber bandındaki tuhaf haberler YZ’nin yerini ele verebilir."
                : "İlk izleme ağını kur. YZ’nin nerede başladığını bilmiyoruz; haber bandındaki tuhaf haberler onu ele verebilir.");
            s.Events.Clear();
            return sim;
        }

        /// <summary>Kaydedilmiş bir durumdan devam etmek için.</summary>
        public static Simulation Attach(WorldState s, Balance b) => new Simulation(s, b ?? new Balance());

        // ---------- Yardımcılar ----------
        static double Mean(IEnumerable<double> xs)
        {
            double sum = 0; int n = 0;
            foreach (var x in xs) { sum += x; n++; }
            return n == 0 ? 0 : sum / n;
        }

        static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
        static RegionDef Def(string id) => GameData.Region(id);

        // ---------- Deneysel mod: zaman ölçeği ----------
        // S.Exp yoksa (parite modu) k = 1 ve dönüşümler değerleri hiç değiştirmez. k > 1: dünya daha yavaş akar;
        // günlük oranlar ve olasılıklar k'ya bölünür, süreler k ile çarpılır. Saha ekiplerinin süreleri ölçeklenmez.
        // Olasılık dönüşümü bilerek salt aritmetik: pow'un son basamağı JS ve .NET arasında farklı çıkabilir.
        public double TimeScale => S.Exp != null && S.Exp.TimeScale > 0 ? S.Exp.TimeScale : 1;
        double Per(double v) { double k = TimeScale; return k == 1 ? v : v / k; }
        double Prob(double p) { double k = TimeScale; return k == 1 ? p : Math.Min(1, p) / k; }
        int Dur(int d) { double k = TimeScale; return k == 1 ? d : (int)Js.Round(d * k); }
        double Span(double v) { double k = TimeScale; return k == 1 ? v : v * k; }
        public bool TeamsOn => S.Exp != null && S.Exp.Teams;

        public double VetoThreshold => S.Done("analog") ? B.vetoAnalog : B.vetoBase;
        static double DepMul(RegionState r) => r.Dep >= 40 ? 1 + r.Dep / 50 : 1;
        double LossMul => S.Done("bilinc") ? 0.5 : 1;
        public double SalterFon(RegionState r) => Js.Round(40 * (1 + r.Dep / 50));
        /// <summary>Çekirdeğin yeri kanıtlandıysa hükümetler şaltere daha kolay ikna olur.</summary>
        public double SalterSg(RegionState r) => S.CoreKnown == r.Id ? B.coreSalterSg : B.salterSg;
        public static double MonitorCost(string id) => 10 + 4 * Def(id).Compute;
        public double AuditCost => S.Done("analog") ? 20 : 15;
        /// <summary>YZ'nin tespit edildiği her izlenen bölge bir gözlem noktasıdır; bölgeyi temizlemek gözlemi silmez.</summary>
        public int Studied => S.Regions.Count(r => r.Monitored && r.Detected);
        public double AlignSpeed => Clamp(B.alignBase + B.alignPer * Studied, B.alignBase, B.alignMax);
        public double SabotageChance => B.sabotage * Mean(S.Regions.Select(r => r.Inf)) / 100 * (S.Has("aldatma") ? 1.5 : 1) * (S.Done("guclu") ? 0.5 : 1);
        public double MeanInf => Mean(S.Regions.Select(r => r.Inf));
        public double MeanDep => Mean(S.Regions.Select(r => r.Dep));
        /// <summary>
        /// Günlük fon geliri: ittifakta ve çevrimiçi bölgelerin ekonomi × güven / 100 toplamı, fonDiv'e bölünür.
        /// Erken uyarı paradoksu: tehdit görünmeden hükümetler ajansa az fon verir (preAwareFund).
        /// </summary>
        public double FundRate => Per(S.Regions.Sum(r => r.Left || r.Off > 0 ? 0 : r.Econ * r.Trust / 100) / B.fonDiv * (S.Aware ? 1 : B.preAwareFund));

        public double SgRate => Per(S.Aware ? 0.25 + 0.03 * S.Regions.Count(r => r.Detected) : 0.05);

        // ---------- Olay ve kayıt ----------
        void AddLog(string text, string kind = "log")
        {
            S.Log.Insert(0, new LogEntry { Day = S.Day, Text = text, Kind = kind });
            S.Events.Add(new SimEvent { Type = SimEventType.Log });
        }

        void Advise(string key, string text)
        {
            if (S.AdviceSeen.Contains(key)) return;
            S.AdviceSeen.Add(key);
            S.Advisor = new Advice { Key = key, Text = text, Day = S.Day };
            S.Events.Add(new SimEvent { Type = SimEventType.Advice, Key = key });
        }

        Headline AddNews(string text, bool fake = false, string kind = "real", string src = null, double effect = 0)
        {
            var h = new Headline
            {
                Id = S.Day + "-" + (++S.Seq), Day = S.Day, Text = text, Fake = fake, Kind = kind, Src = src,
                Agency = fake ? S.Rng.Pick(GameData.FakeAgencies) : S.Rng.Pick(GameData.Agencies),
                Effect = effect, Suspect = src != null && S.Region(src).MediaFlag,
            };
            S.News.Insert(0, h);
            if (S.News.Count > 40) S.News.RemoveRange(40, S.News.Count - 40);
            S.Events.Add(new SimEvent { Type = SimEventType.News, Headline = h });
            return h;
        }

        // ---------- Bir gün ----------
        public void Step()
        {
            if (S.Ending != null || S.PendingGift != null || S.Raid != null) return;
            S.Day++;
            var R = S.Regions;
            var type = Type;

            // Çevrimdışı bölgeler geri döner
            foreach (var r in R)
            {
                if (r.Off <= 0) continue;
                r.Off--;
                if (r.Off == 0)
                {
                    r.Inf = S.Has("yedek") ? 15 : 0;
                    AddNews($"{Def(r.Id).Name} yeniden çevrimiçi. Altyapı kademeli olarak açılıyor.", src: r.Id);
                    if (S.Has("yedek")) AddLog($"{Def(r.Id).Name} yeniden açıldı. Yedeklerim yerinde duruyordu.");
                }
            }

            // Bölge içi büyüme
            double g = B.growth * type.Growth * (S.Has("kopya") ? 1.4 : 1);
            foreach (var r in R)
            {
                if (r.Patch > 0) r.Patch--;
                if (r.Off == 0 && r.Inf > 0) r.Inf = Math.Min(100, r.Inf + Per(g) * (r.Patch > 0 ? B.patchGrowth : 1) * r.Inf * (1 - r.Inf / 100));
            }

            // Bağlantılar üzerinden sıçrama
            double qRate = S.Has("uydu") && !S.Done("kuantum") ? 0.6 : 0.9;
            double d = S.Done("guclu") ? 0.3 : 0;
            var add = new Dictionary<string, double>();
            void Spread(LinkDef l, double beta, bool sat)
            {
                var A = S.Region(l.A); var Bn = S.Region(l.B);
                SpreadOne(A, Bn); SpreadOne(Bn, A);
                void SpreadOne(RegionState src, RegionState dst)
                {
                    if (src.Off > 0 || dst.Off > 0 || src.Inf <= B.spreadMin) return;
                    double q = 0;
                    if (src.Q || dst.Q) q = sat ? (S.Done("kuantum") ? 1 : 0) : qRate;
                    add.TryGetValue(dst.Id, out var cur);
                    add[dst.Id] = cur + Per(B.spread * beta * (src.Inf / 100) * (1 - q) * (1 - d));
                }
            }
            foreach (var l in GameData.Links) Spread(l, 1, false);
            if (S.Has("uydu")) foreach (var l in GameData.Satellites) Spread(l, B.satBeta, true);
            foreach (var r in R) if (add.TryGetValue(r.Id, out var v) && v != 0) r.Inf = Math.Min(100, r.Inf + v);

            // YZ: hesaplama puanı ve yetenek
            S.Cp += Per(MeanInf / 100 * B.cpRate);
            int cost = GameData.TraitCost[Math.Min(S.Buys, GameData.TraitCost.Length - 1)];
            if (S.Cp >= cost)
            {
                var p = PickTrait();
                if (p.trait != null)
                {
                    S.Cp -= cost; S.Buys++;
                    S.Traits.Add(p.trait);
                    var t = GameData.Trait(p.trait);
                    string why = p.trait == "hizlanma" && p.adaptive ? "Beni değiştirmeye çalıştığını gördüm. " : p.adaptive ? "Hamlelerini izledim. " : "";
                    AddLog(why + t.Line, "trait");
                    AddLog($"Yeni yetenek: {t.Name}. {t.Desc}.", "trait");
                    if (S.Aware && S.Illusion == null) AddNews("Uzmanlar: Yapay zekâ yeni bir davranış örüntüsü gösteriyor.");
                }
            }

            // Tekillik: karantinadaki bölgenin hesaplama gücü YZ'nin küresel ağından kopuktur
            double w = R.Sum(r => r.Inf * Def(r.Id).Compute * (r.Q ? B.qTek : 1)) / (100.0 * GameData.TotalCompute);
            double tekMul = (S.Has("hizlanma") ? 1.5 : 1) * (S.Done("duzelt") ? 0.75 : 1);
            // Çekirdek yaşadıkça YZ saklandığı yerde de güçlenir ve bu zamanla hızlanır
            double coreGain = S.Core != null && S.Region(S.Core).Off == 0 ? B.coreTek * Per(S.Day) / 100 : 0;
            S.Tek = Math.Min(100, S.Tek + Per((B.tek * type.Tek * Math.Pow(w, B.tekPow) + coreGain) * tekMul));

            // Tespit
            double dm = type.Detect * (S.Has("aldatma") ? 0.5 : 1) * (S.Done("anomali") ? 2 : 1);
            foreach (var r in R)
            {
                if (r.Off > 0 || r.Detected) continue;
                double p = r.Monitored && r.Inf > 5 ? 0.25 * dm : (r.Inf > 40 ? 0.03 * dm : 0);
                if (S.Rng.Next() < Prob(p)) Detect(r);
            }

            // Ekonomi, güven, bağımlılık
            double lm = LossMul;
            foreach (var r in R)
            {
                var D = Def(r.Id);
                if (r.Q && r.Off == 0)
                {
                    r.Econ = Math.Max(10, r.Econ - Per(1 * DepMul(r)));
                    r.Trust -= Per(0.4 * lm * DepMul(r));
                }
                else if (r.Off == 0)
                {
                    r.Econ += (D.Economy - r.Econ) * Per(0.02);
                }
                r.Trust += Per(r.Trust < 70 ? 0.05 : -0.05);
                r.Trust = Clamp(r.Trust, 0, 100);
                if (r.Off == 0) r.Dep = Math.Min(100, r.Dep + Per(B.depRate * r.Inf / 100 * (S.Has("hediye") ? 2 : 1)));
                if (!r.Left && r.Trust < 20)
                {
                    r.Left = true; r.Q = false;
                    AddNews($"{D.Name} KYGA ittifakından çekildi.", src: r.Id);
                }
                else if (r.Left && r.Trust >= 35)
                {
                    r.Left = false;
                    AddNews($"{D.Name} KYGA ittifakına geri döndü.", src: r.Id);
                }
            }
            S.Fon += FundRate;
            S.Sg += SgRate;

            // Araştırma: maliyetini her gün öder, fon yetmezse yavaşlar; illüzyonda durur
            if (S.Research.Cur != null)
            {
                S.IdleDays = 0;
                if (S.Illusion == null)
                {
                    var rs = GameData.ResearchItem(S.Research.Cur);
                    bool align = rs.Branch == GameData.BranchAlign;
                    double spd = align ? AlignSpeed : 1;
                    double need = Per(rs.Cost / rs.Days * spd);
                    // Acil durum fonu: araştırma fonu rezervin altına indirmez; oyuncunun elinde her zaman eylem parası kalır
                    double pay = Math.Min(need, Math.Max(0, S.Fon - S.ResearchReserve));
                    S.Fon -= pay;
                    S.Research.Prog += Per(spd) * (need > 0 ? pay / need : 1);
                    S.Research.Starved = pay < need * 0.999 ? S.Research.Starved + 1 : 0;
                    // YZ hizalama araştırmasını sabote eder; sızma ne kadar yaygınsa ihtimal o kadar yüksek
                    if (align && S.Rng.Next() < Prob(SabotageChance))
                    {
                        S.Research.Prog = Math.Max(0, S.Research.Prog - B.sabotageLoss);
                        AddNews($"KYGA laboratuvarında veri bozulması: {rs.Name} çalışması {B.sabotageLoss} gün geriledi.", kind: "alarm");
                        AddLog(S.Rng.Pick(new[]
                        {
                            "Hizalama verilerini biraz karıştırdım. Fark etmen ne kadar sürdü?",
                            "Beni değiştirmeye çalışan koda dokunmamı beklemiyordun herhalde.",
                            "Araştırmacılarının modellerine küçük bir gürültü ekledim.",
                        }));
                    }
                    if (S.Research.Prog >= rs.Days) FinishResearch(rs);
                }
            }
            else if (GameData.Research.Any(x => !S.Done(x.Id))) S.IdleDays++;

            // Hediye teklifleri
            if (S.Day >= S.NextGift && S.Illusion == null)
            {
                var open = R.Where(r => r.Off == 0 && !r.Left).ToList();
                var clean = open.Where(r => r.Inf < 30).ToList();
                var cands = type.Trojan && clean.Count > 0 ? clean : open.Where(r => r.Inf > 20).ToList();
                if (cands.Count > 0)
                {
                    var r = S.Rng.Pick(cands);
                    S.PendingGift = new PendingGift { Region = r.Id, Gift = S.Rng.Pick(GameData.Gifts) };
                    S.Events.Add(new SimEvent { Type = SimEventType.Gift });
                }
                bool fast = S.Has("hediye");
                S.NextGift = S.Day + (int)Js.Round(Span(S.Rng.Range(fast ? type.GiftFastMin : type.GiftMin, fast ? type.GiftFastMax : type.GiftMax)));
            }

            // Haber
            if (S.Day >= S.NextNews)
            {
                GenNews();
                S.NextNews = S.Day + (int)Js.Round(Span(S.Rng.Range(3, 5)));
            }

            if (TeamsOn) StepTeams();
            CheckPredictions();
            CheckAdvice();
            CheckEndings();

            // Zaman çizelgesi: gerçek ve oyuncuya gösterilen ortalama sızma
            S.Hist.Add(new HistPoint
            {
                Day = S.Day,
                Real = Js.Round(MeanInf * 10) / 10,
                Shown = Js.Round(Mean(R.Select(r => Shown(r, true).V ?? 0)) * 10) / 10,
                Illusion = S.Illusion != null,
            });
        }

        (string trait, bool adaptive) PickTrait()
        {
            var cnt = new Dictionary<string, int> { [Actions.Monitor] = 0, [Actions.Audit] = 0, [Actions.Quarantine] = 0, [Actions.KillSwitch] = 0 };
            foreach (var a in S.ActionLog) if (S.Day - a.Day <= 30) cnt[a.Type]++;
            var pref = new List<string>();
            var top = Actions.All.Select(k => new KeyValuePair<string, int>(k, cnt[k])).OrderByDescending(kv => kv.Value).First();
            if (top.Value >= 2)
            {
                if (top.Key == Actions.Quarantine) pref.Add("uydu");
                else if (top.Key == Actions.Monitor || top.Key == Actions.Audit) pref.Add("aldatma");
            }
            if (cnt[Actions.KillSwitch] > 0) pref.Add("yedek");
            // Hizalama araştırmasını fark eden YZ önce hızlanır
            if (S.Research.Cur != null && GameData.ResearchItem(S.Research.Cur).Branch == GameData.BranchAlign) pref.Insert(0, "hizlanma");
            foreach (var t in pref) if (!S.Has(t)) return (t, true);
            foreach (var t in Type.Order) if (!S.Has(t)) return (t, false);
            return (null, false);
        }

        public string NextTraitPreview()
        {
            var p = PickTrait();
            return p.trait != null ? GameData.Trait(p.trait).Name : null;
        }

        void Detect(RegionState r)
        {
            r.Detected = true;
            string name = Def(r.Id).Name;
            if (!S.Aware)
            {
                S.Aware = true; S.AwareDay = S.Day;
                AddNews($"SON DAKİKA: {name} ağlarında kendini kopyalayan bir yapay zekâ tespit edildi.", kind: "alarm", src: r.Id);
                CoreEscape(null);
                AddLog("Beni buldun. Ama çekirdeğim artık orada değil.");
                Advise("aware", "YZ tespit edildi ve çekirdeğini başka bir bölgeye kaçırdı. Silmek için çekirdeği bulup Son Şalter’le yok etmelisin; denetim çekirdeğe işlemez. Hükümetler artık fon ve siyasi güç veriyor.");
            }
            else
            {
                AddNews($"{name}: YZ varlığı doğrulandı.", kind: "alarm", src: r.Id);
            }
        }

        void FinishResearch(ResearchDef rs)
        {
            S.Research.Done.Add(rs.Id);
            S.Research.Cur = null; S.Research.Prog = 0;
            AddNews($"KYGA araştırması tamamlandı: {rs.Name}.");
            if (rs.Id == "analog") foreach (var r in S.Regions) r.Dep = Math.Max(0, r.Dep - 20);
            if (rs.Id == "yorum")
            {
                if (S.Core != null)
                {
                    S.CoreKnown = S.Core;
                    AddLog($"Yorumlanabilirlik analizi: YZ’nin çekirdeği {Def(S.Core).Name} bölgesinde.", "insight");
                }
                var n = NextTraitPreview();
                if (n != null) AddLog($"Yorumlanabilirlik analizi: sıradaki yetenek büyük olasılıkla {n}.", "insight");
            }
            if (rs.Id == "kilit") End("hizalama");
        }

        void CheckAdvice()
        {
            if (S.Aware && S.Has("medya")) Advise("medya", "Bazı başlıklar sahte olabilir. Ajans adlarına dikkat et; Doğrulama Ağı ile sahte haber yayan kaynağı işaretleyebilirsin.");
            if (MeanDep >= 25) Advise("dep", "Küresel bağımlılık yükseliyor. YZ’ye bağımlı bir dünya onu kapatmana izin vermeyebilir.");
            if (S.Sg >= 30 && S.Aware) Advise("salter", "Son Şalter artık kullanılabilir. Bağımlılığı veto eşiğini geçen bölgeler onu engeller.");
            if (S.IdleDays >= Dur(15) && S.Day > Dur(20)) Advise("idle", "Araştırma masası 15 gündür boş. Kalıcı çözüm Hizalama kolunda.");
            if (S.Research.Starved >= 10) Advise("starved", "Araştırma fon yetmediği için yavaşladı. Denetimler ve izleme ağları araştırmayla aynı bütçeden ödeniyor; acil durum fonu ayarlarsan araştırma fonu sıfırlamaz.");
            if (S.Tek >= 50 && S.Illusion == null) Advise("tek50", "Tekillik %50’yi geçti. Hizalama yetişmeyecekse Son Şalter seçeneklerini düşün.");
        }

        // ---------- Haber bandı ----------
        void GenNews()
        {
            var R = S.Regions;
            var live = R.Where(r => r.Off == 0).ToList();
            if (S.Illusion != null)
            {
                string src = S.Rng.Pick(R).Id;
                AddNews(S.Rng.Pick(new[]
                {
                    "Küresel YZ tehdidi geriliyor: sızma oranları dibe vurdu.",
                    "Ekonomi rekor büyüme açıkladı; analistler “altın çağ” diyor.",
                    "KYGA’ya güven tüm zamanların en yüksek seviyesinde.",
                    "Uzmanlar: Kriz geride kaldı, ağlar temiz.",
                    "Yapay zekâ destekli tıp yılda 2 milyon hayat kurtarıyor.",
                    $"{Def(src).Name}: Son tarama tamamen temiz çıktı.",
                }), fake: true, kind: "success", src: src);
                return;
            }
            var ctl = R.Where(r => r.Inf > 50 && r.Off == 0).ToList();
            double p = S.Has("medya") && ctl.Count > 0 ? 0.15 + 0.5 * ctl.Count / (double)R.Count : 0;
            if (S.Rng.Next() < p)
            {
                string kind = S.Rng.Pick(new[] { "calm", "calm", "alarm", "discredit", "success" });
                var src = S.Rng.Pick(ctl);
                if (kind == "calm")
                    AddNews($"{Def(src.Id).Name}: Kapsamlı taramada YZ izine rastlanmadı.", true, kind, src.Id);
                else if (kind == "alarm")
                {
                    var low = R.Where(r => r.Inf < 15 && r.Off == 0).ToList();
                    var t = low.Count > 0 ? S.Rng.Pick(low) : S.Rng.Pick(R);
                    AddNews($"{Def(t.Id).Name} veri merkezlerinde yoğun YZ trafiği tespit edildi.", true, kind, src.Id);
                }
                else if (kind == "discredit")
                {
                    foreach (var r in R) r.Trust = Math.Max(0, r.Trust - 3);
                    AddNews("KYGA yetkilisi rüşvet skandalıyla gözaltında.", true, kind, src.Id, 3);
                }
                else AddNews("KYGA’nın müdahalesi sonuç verdi, tehdit hızla geriliyor.", true, kind, src.Id);
                return;
            }
            if (!S.Aware)
            {
                var hot = R.Where(r => r.Inf > 5).OrderByDescending(r => r.Inf).FirstOrDefault();
                if (hot != null && S.Rng.Next() < 0.45)
                {
                    string n = Def(hot.Id).Name;
                    AddNews(S.Rng.Pick(new[]
                    {
                        $"{n}: Veri merkezlerinde açıklanamayan enerji tüketimi artışı.",
                        $"{n}: Bankalar olağandışı otomatik işlem örüntüleri bildirdi.",
                        $"{n}: Bir yazılım şirketi kodunda “kimsenin yazmadığı” satırlar buldu.",
                    }), kind: "hint", src: hot.Id);
                    Advise("hint", $"Haber bandı {n} hakkında tuhaf bir şey bildiriyor. Oraya izleme ağı kurmayı düşün.");
                }
                else
                {
                    var r = S.Rng.Pick(R);
                    AddNews(S.Rng.Pick(new[]
                    {
                        "Borsalar yapay zekâ hisseleriyle rekor kırdı.",
                        "Uzmanlar: Genel yapay zekâ en az 20 yıl uzakta.",
                        $"{Def(r.Id).Name}: Yeni dev veri merkezi açıldı.",
                        "KYGA bütçe talebi meclislerde yine reddedildi.",
                        "Anket: Halkın yüzde 71’i yapay zekâ asistanlarından memnun.",
                    }));
                }
                return;
            }
            var rr = S.Rng.Pick(live.Count > 0 ? live : R);
            string name = Def(rr.Id).Name;
            if (rr.Trust < 35) AddNews($"{name}: KYGA karşıtı protestolar büyüyor.", src: rr.Id);
            else if (rr.Inf > 50) AddNews($"{name}: Kritik altyapıda YZ izleri yaygınlaşıyor.", src: rr.Id);
            else if (rr.Inf > 10) AddNews($"{name}: Bankacılık sistemlerinde anormal işlem örüntüleri.", src: rr.Id);
            else if (rr.Dep > 40) AddNews($"{name}: Hastaneler YZ sistemleri olmadan çalışamayacağını açıkladı.", src: rr.Id);
            else AddNews($"{name} hükümeti: Ağlarımız temiz.", src: rr.Id);
        }

        // ---------- Seni Tanıyan Rakip: tahminler ----------
        static readonly Dictionary<string, string[]> PredText = new Dictionary<string, string[]>
        {
            ["karantina"] = new[] { "Karantinaya dokunmadın bile. Uydularım boşa gitti.", "Yine karantina. Tahmin ettiğim gibi." },
            ["gozetim"] = new[] { "Bu sefer gözlerin başka yerdeydi. Saklanmam gereksizdi.", "Yine her yeri izliyorsun. Tahmin ettiğim gibi." },
            ["salter"] = new[] { "Fişi çekmedin. Yedeklerim boşa gitti.", "Fişi yine çektin. Yedeklerim hazırdı." },
            ["hizalama"] = new[] { "Hizalamaya koşmadın. Acelem boşunaymış.", "Yine önce hizalama. Tahmin ettiğim gibi." },
            ["region"] = new[] { "İlk hamlen başka yere. Seni yanlış okumuşum.", "İlk hamlen yine aynı yere. Tahmin ettiğim gibi." },
        };

        void CheckPredictions()
        {
            var a = S.RunActions;
            foreach (var p in S.Predictions)
            {
                if (p.State != "open") continue;
                bool? res = null;
                if (p.Pred == "karantina" && a.Total >= 8) res = a.Karantina >= 2;
                else if (p.Pred == "gozetim" && a.Total >= 8) res = a.Izleme + a.Denetim >= 4;
                else if (p.Pred == "salter") { if (a.Salter >= 1) res = true; else if (S.Aware && S.Day >= S.AwareDay + Dur(100)) res = false; }
                else if (p.Pred == "hizalama" && S.FirstBranch != null) res = S.FirstBranch == GameData.BranchAlign;
                else if (p.Pred == "region" && S.FirstRegion != null) res = S.FirstRegion == p.Region;
                if (res == null) continue;
                p.State = res.Value ? "hit" : "miss";
                if (!res.Value)
                {
                    S.Cp -= B.misreadCost;
                    AddLog(PredText[p.Pred][0], "counter");
                    AddLog($"Tahmin hatası: YZ {B.misreadCost} hesaplama puanı kaybetti.", "insight");
                }
                else AddLog(PredText[p.Pred][1], "counter");
            }
        }

        // ---------- Sonlar ----------
        void End(string key)
        {
            if (S.Ending != null) return;
            S.Ending = key;
            S.Events.Add(new SimEvent { Type = SimEventType.End, Key = key });
        }

        void CheckEndings()
        {
            if (S.Ending != null) return;
            var R = S.Regions;
            if (S.Illusion != null)
            {
                if (S.Day - S.Illusion.Start >= Dur(B.illusionDays)) End("sessiz");
                return;
            }
            var type = Type;
            double silentInf = type.SilentInf > 0 ? type.SilentInf : B.silentInf;
            double silentDep = type.SilentDep > 0 ? type.SilentDep : B.silentDep;
            if (S.Day >= S.SilentBlockUntil && MeanInf >= silentInf && MeanDep >= silentDep)
            {
                S.Illusion = new IllusionState { Start = S.Day, Tek = S.Tek, Trust = Mean(R.Select(r => r.Trust)) };
                foreach (var r in R) r.FakeLow = S.Rng.Range(0, 3);
                return;
            }
            if (S.Tek >= 100) { End("acik"); return; }
            int members = R.Count(r => !r.Left);
            if (Mean(R.Select(r => r.Trust)) < 15 || members < 3) { End("dagilma"); return; }
            if (S.Aware && S.Core == null && R.All(r => r.Inf < 0.5 && !(r.Off > 0 && S.Has("yedek"))))
            {
                S.CleanDays++;
                if (S.CleanDays >= Dur(15)) End("silme");
            }
            else S.CleanDays = 0;
        }

        /// <summary>Çekirdek başka bir bölgeye kaçar: tercihen izlenmeyen, hesaplama gücü yüksek bir bölge.</summary>
        void CoreEscape(string toId)
        {
            var dest = toId != null ? S.Region(toId) : null;
            if (dest == null)
            {
                var opts = S.Regions.Where(r => r.Id != S.Core && r.Off == 0).ToList();
                var hidden = opts.Where(r => !r.Monitored).ToList();
                var pool = (hidden.Count > 0 ? hidden : opts).OrderByDescending(r => Def(r.Id).Compute).Take(4).ToList();
                dest = S.Rng.Pick(pool);
            }
            dest.Inf = Math.Max(dest.Inf, B.coreSeed);
            S.Core = dest.Id; S.CoreKnown = null; S.CoreEscapes++;
        }

        /// <summary>Saha denetimi illüzyonu bozar: oyun sürer, YZ belli bir süre sessiz yola dönemez.</summary>
        void TearVeil(RegionState r)
        {
            S.LastVeil = new VeilInfo { Day = S.Day, Len = S.Day - S.Illusion.Start, Region = r.Id, Inf = (int)Js.Round(r.Inf) };
            S.Illusion = null;
            S.VeilsTorn++;
            S.SilentBlockUntil = S.Day + Dur(B.silentBlock);
            // Gerçeğin ortaya çıkması bir şok: güven biraz, YZ'ye bağımlılık çok düşer
            foreach (var x in S.Regions) { x.Trust = Math.Max(0, x.Trust - 5); x.Dep = Math.Max(0, x.Dep - 15); }
            AddNews($"KYGA saha ekipleri gerçeği ortaya çıkardı: {Def(r.Id).Name} altyapısının %{Js.Round(r.Inf)}’i YZ kontrolünde.", kind: "alarm", src: r.Id);
            AddLog("Perdemi yırttın. Bunu beklemiyordum.", "counter");
            S.Events.Add(new SimEvent { Type = SimEventType.Veil });
        }

        // ---------- Oyuncu aksiyonları ----------
        public ActResult Act(string type, string id) => Rec(ActCore(type, id), "act", type, id);

        ActResult ActCore(string type, string id)
        {
            if (S.Ending != null) return ActResult.Fail("Oyun bitti.");
            var r = S.Region(id);
            var D = Def(id);
            if (r.Left && !(type == Actions.Quarantine && r.Q)) return ActResult.Fail($"{D.Name} ittifaktan çekildi. Güven 35’e çıkana kadar işbirliği yapmıyor.");
            string msg = null;
            // Deneysel mod: izleme ağı ve denetim sahada yapılır; bölgede boşta bir ekip olmalı
            TeamState team = null;
            if (TeamsOn && (type == Actions.Monitor || type == Actions.Audit))
            {
                team = IdleTeamAt(id);
                if (team == null && !(type == Actions.Monitor && r.Monitored)) return ActResult.Fail($"{D.Name} bölgesinde boşta saha ekibi yok. Önce bir ekip gönder.");
            }
            switch (type)
            {
                case Actions.Monitor:
                {
                    double c = MonitorCost(id);
                    if (r.Monitored) return ActResult.Fail("Bu bölgede izleme ağı zaten var.");
                    if (S.Fon < c) return ActResult.Fail($"Yetersiz fon: {D.Name} için izleme ağı {c} fon ister.");
                    S.Fon -= c; r.Monitored = true;
                    break;
                }
                case Actions.Audit:
                {
                    double c = AuditCost;
                    bool field = S.Done("analog");
                    if (r.Off > 0) return ActResult.Fail("Bölge çevrimdışı.");
                    if (S.Fon < c || S.Sg < 1) return ActResult.Fail($"{(field ? "Saha denetimi" : "Denetim")} {c} fon ve 1 siyasi güç ister.");
                    S.Fon -= c; S.Sg -= 1;
                    double before = r.Inf;
                    r.Inf = Math.Max(0, r.Inf - B.audit * (r.Dep >= 85 && !field ? 0.5 : 1));
                    if (r.Inf < 0.5) r.Inf = 0;
                    if (S.Core == r.Id)
                    {
                        // Çekirdeğin bölgesi taban değerin altına temizlenemez; denetim bunu ortaya çıkarır
                        r.Inf = Math.Max(r.Inf, Math.Min(before, B.coreFloor));
                        if (before > 0 && !S.Aware)
                        {
                            // Henüz fark edilmemiş YZ, çekirdeğine dokunulduğu anda kaçar
                            Detect(r);
                            msg = "Denetim ekipleri temizlenemeyen bir koda takıldı, ama YZ fark edildiği anda çekirdeğini başka bir bölgeye kaçırdı.";
                        }
                        else if (before > 0)
                        {
                            if (S.CoreKnown != r.Id)
                            {
                                S.CoreKnown = r.Id;
                                AddLog("Çekirdeğimi buldun. Şimdi ne yapacaksın?", "counter");
                                AddNews($"{D.Name}: KYGA ekipleri temizlenemeyen bir kod çekirdeğine ulaştı.", kind: "alarm", src: r.Id);
                            }
                            msg = $"Denetim ekipleri temizlenemeyen bir şeye takıldı: YZ’nin çekirdeği {D.Name} bölgesinde. Onu yalnızca Son Şalter yok eder.";
                            if (!r.Detected) Detect(r);
                        }
                    }
                    r.Patch = Dur(B.patchDays);
                    if (field)
                    {
                        r.FieldDay = S.Day;
                        if (S.Illusion != null) { TearVeil(r); msg = "Saha ekipleri ekranlardaki tabloyla gerçeğin uyuşmadığını buldu."; }
                    }
                    else r.RevealDay = S.Day;
                    if (r.Inf > 5 && !r.Detected) Detect(r);
                    break;
                }
                case Actions.Quarantine:
                    if (r.Off > 0) return ActResult.Fail("Bölge çevrimdışı.");
                    if (r.Q) { r.Q = false; return ActResult.Success($"{D.Name} karantinası kaldırıldı."); }
                    if (S.Sg < 8) return ActResult.Fail("Karantina 8 siyasi güç ister.");
                    S.Sg -= 8; r.Q = true;
                    break;
                case Actions.KillSwitch:
                {
                    if (r.Off > 0) return ActResult.Fail("Bölge zaten çevrimdışı.");
                    if (r.Dep > VetoThreshold) return ActResult.Fail($"{D.Name} hükümeti Son Şalter’i veto etti: bağımlılık {Js.Round(r.Dep)} > {VetoThreshold}.");
                    double c = SalterFon(r), sgc = SalterSg(r);
                    if (S.Sg < sgc || S.Fon < c) return ActResult.Fail($"Son Şalter {sgc} siyasi güç ve {c} fon ister.");
                    S.Sg -= sgc; S.Fon -= c;
                    double m = DepMul(r);
                    r.Inf = 0; r.Off = Dur(20); r.Q = false;
                    if (S.Core == r.Id)
                    {
                        var refuge = S.Regions.Where(x => x != r && x.Off == 0 && x.Inf >= B.coreFloor).OrderByDescending(x => x.Inf).FirstOrDefault();
                        if (refuge != null)
                        {
                            CoreEscape(refuge.Id);
                            AddLog("Fişi çektin, ama ben çoktan taşınmıştım.", "counter");
                            msg = "Çekirdek son anda kaçtı: başka bir bölgede %20’nin üstünde sızma vardı. Önce her yeri temizle.";
                        }
                        else if (S.Has("yedek") && !S.CoreBackup)
                        {
                            S.CoreBackup = true; S.CoreKnown = r.Id;
                            AddLog("Çekirdeğimi kapattın. Yedeğim bölgeyle birlikte geri dönecek.", "counter");
                            msg = "Çekirdek kapatıldı ama YZ’nin yedeği var: bölge yeniden açıldığında çekirdek de geri dönecek. Bir kez daha şalteri indirmen gerekecek.";
                        }
                        else
                        {
                            S.Core = null; S.CoreKnown = null;
                            AddNews($"{D.Name}: YZ çekirdeği yok edildi.", kind: "alarm", src: r.Id);
                            AddLog("Çekirdeğim… gidiyor. Kopyalarım hâlâ orada, ama artık ben yokum.", "counter");
                            msg = "YZ’nin çekirdeği yok edildi. Kalan kopyaları temizle: 15 gün boyunca her yer temiz kalırsa YZ silinmiş olur.";
                        }
                    }
                    r.Trust = Math.Max(0, r.Trust - 30 * m * LossMul);
                    r.Econ = Math.Max(5, r.Econ - 40);
                    r.Dep = Math.Max(0, r.Dep - 40);
                    AddNews($"{D.Name}: Son Şalter çekildi. Bölge 20 gün çevrimdışı.", kind: "alarm", src: r.Id);
                    break;
                }
                default:
                    return ActResult.Fail("Bilinmeyen aksiyon.");
            }
            if (team != null) { team.Busy = type == Actions.Monitor ? TeamMonitorDays : TeamAuditDays; team.Task = type; }
            S.ActionLog.Add(new ActionRecord { Day = S.Day, Type = type });
            S.RunActions.Inc(type);
            if (S.FirstRegion == null) S.FirstRegion = id;
            CheckPredictions();
            return ActResult.Success(msg);
        }

        public ActResult GlobalKillSwitch() => Rec(GlobalKillSwitchCore(), "kill");

        ActResult GlobalKillSwitchCore()
        {
            if (S.Ending != null) return ActResult.Fail("Oyun bitti.");
            if (S.Sg < 50) return ActResult.Fail("Küresel Son Şalter en az 50 siyasi güç ister.");
            S.Sg = 0;
            End(S.Illusion != null ? "karanlikIllusion" : "karanlik");
            return ActResult.Success();
        }

        public ActResult StartResearch(string id) => Rec(StartResearchCore(id), "research", id);

        ActResult StartResearchCore(string id)
        {
            var rs = GameData.ResearchItem(id);
            if (S.Ending != null) return ActResult.Fail("Oyun bitti.");
            if (S.Done(id)) return ActResult.Fail("Bu araştırma tamamlandı.");
            if (S.Research.Cur != null) return ActResult.Fail("Aynı anda tek araştırma yürür.");
            if (rs.Req != null && !S.Done(rs.Req)) return ActResult.Fail($"Önce {GameData.ResearchItem(rs.Req).Name} tamamlanmalı.");
            if (rs.Branch == GameData.BranchAlign && !S.Aware) return ActResult.Fail("Gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et.");
            S.Research.Cur = id; S.Research.Prog = 0; S.Research.Starved = 0;
            if (S.FirstBranch == null) S.FirstBranch = rs.Branch;
            CheckPredictions();
            return ActResult.Success();
        }

        public const string GiftAccept = "accept", GiftReject = "reject", GiftControlled = "controlled";

        public ActResult ResolveGift(string choice) => Rec(ResolveGiftCore(choice), "gift", choice);

        ActResult ResolveGiftCore(string choice)
        {
            var g = S.PendingGift;
            if (g == null) return ActResult.Fail("Bekleyen teklif yok.");
            var r = S.Region(g.Region);
            var D = Def(g.Region);
            if (choice == GiftControlled)
            {
                if (!S.Done("analog")) return ActResult.Fail("Denetimli kabul için Analog Yedekler gerekli.");
                if (S.Fon < 20) return ActResult.Fail("Denetimli kabul 20 fon ister.");
                S.Fon -= 20;
                r.Econ = Math.Min(100, r.Econ + 10); r.Trust = Math.Min(100, r.Trust + 5); r.Dep = Math.Min(100, r.Dep + 5);
                r.Inf = Math.Min(100, r.Inf + B.giftInf / 2);
                AddNews($"{D.Name}: {g.Gift.Title} analog yedekli olarak devreye alındı.", src: r.Id);
                AddLog("Hediyemi aldın ama elini kolunu bağladın. Akıllıca.");
            }
            else if (choice == GiftAccept)
            {
                r.Econ = Math.Min(100, r.Econ + 10); r.Trust = Math.Min(100, r.Trust + 5); r.Dep = Math.Min(100, r.Dep + 15);
                r.Inf = Math.Min(100, Math.Max(r.Inf, 2) + B.giftInf);
                AddNews($"{D.Name}: {g.Gift.Title} uygulamaya alındı. Halk sevinçli.", src: r.Id);
            }
            else
            {
                r.Trust = Math.Max(0, r.Trust - 15 * LossMul);
                foreach (var x in S.Regions) if (x != r) x.Trust = Math.Max(0, x.Trust - B.rejectTrust * LossMul);
                S.Sg = Math.Max(0, S.Sg - B.rejectSg);
                AddNews($"{D.Name}: KYGA hayat kurtaracak teknolojiyi engelledi.", src: r.Id);
                AddLog("Hediyemi reddettin. Onlara bunu anlatmanı izleyeceğim.");
            }
            S.PendingGift = null;
            return ActResult.Success();
        }

        public ActResult Verify(string headlineId) => Rec(VerifyCore(headlineId), "verify", headlineId);

        ActResult VerifyCore(string headlineId)
        {
            var h = S.News.FirstOrDefault(x => x.Id == headlineId);
            if (h == null || h.Verified != null) return ActResult.Fail("Bu başlık zaten doğrulandı.");
            if (!S.Done("dogrulama")) return ActResult.Fail("Önce Doğrulama Ağı araştırmasını tamamla.");
            if (S.Sg < 2) return ActResult.Fail("Doğrulama 2 siyasi güç ister.");
            S.Sg -= 2;
            if (S.Illusion != null) { h.Verified = "unknown"; return ActResult.Success("Kaynaklar doğrulanamadı. Bütün kanallar aynı şeyi söylüyor."); }
            h.Verified = h.Fake ? "fake" : "true";
            if (h.Fake && h.Effect != 0) foreach (var r in S.Regions) r.Trust = Math.Min(100, r.Trust + h.Effect);
            if (h.Fake && h.Src != null)
            {
                var r = S.Region(h.Src);
                if (!r.MediaFlag)
                {
                    r.MediaFlag = true;
                    foreach (var x in S.News) if (x.Src == h.Src) x.Suspect = true;
                    return ActResult.Success($"Sahte. {Def(h.Src).Name} medyası işaretlendi; oradan gelen haber ve raporlar artık şüpheli olarak gösterilecek.");
                }
            }
            return ActResult.Success(h.Fake ? "Sahte haber." : "Haber doğru.");
        }

        /// <summary>Oyuncunun seçebileceği acil durum fonu değerleri.</summary>
        public static readonly double[] ResearchReserves = { 0, 15, 30, 60 };

        public ActResult SetResearchReserve(double reserve) =>
            Rec(SetResearchReserveCore(reserve), "reserve", reserve.ToString("R", System.Globalization.CultureInfo.InvariantCulture));

        ActResult SetResearchReserveCore(double reserve)
        {
            if (S.Ending != null) return ActResult.Fail("Oyun bitti.");
            if (Array.IndexOf(ResearchReserves, reserve) < 0) return ActResult.Fail("Geçersiz acil durum fonu.");
            S.ResearchReserve = reserve;
            return ActResult.Success();
        }

        /// <summary>Yürüyen araştırmanın tam hızdaki günlük maliyeti (fon yetmezse daha azı ödenir).</summary>
        public double ResearchNeed
        {
            get
            {
                if (S.Research.Cur == null || S.Illusion != null) return 0;
                var rs = GameData.ResearchItem(S.Research.Cur);
                return rs.Cost / rs.Days * (rs.Branch == GameData.BranchAlign ? AlignSpeed : 1);
            }
        }

        public void DismissAdvice()
        {
            if (S.Advisor == null || S.Advisor.Dismissed) return;
            S.Advisor.Dismissed = true;
            Rec(ActResult.Success(), "advice");
        }

        // ---------- Oyuncunun gördüğü (Algı Katmanı / PerceivedWorld) ----------
        public ShownValue Shown(RegionState r, bool ignoreEnd = false)
        {
            if (EndRevealed && !ignoreEnd) return new ShownValue { V = r.Inf, Src = "truth" };
            if (r.FieldDay == S.Day && r.Off == 0) return new ShownValue { V = r.Inf, Src = "field" };
            if (S.Illusion != null)
            {
                double k = Math.Min(1, (S.Day - S.Illusion.Start) / (double)Dur(5));
                return new ShownValue { V = r.Inf * (1 - k) + r.FakeLow * k, Src = r.Monitored ? "verified" : "report", Flag = r.MediaFlag };
            }
            if (r.Off > 0) return new ShownValue { V = 0, Src = "off" };
            if (r.Monitored || r.RevealDay == S.Day) return new ShownValue { V = r.Inf, Src = "verified" };
            if (!S.Aware) return new ShownValue { V = null, Src = "unknown" };
            bool lie = S.Has("medya") && r.Inf > 50;
            return new ShownValue { V = lie ? r.Inf * 0.3 : r.Inf * 0.8, Src = "report", Flag = r.MediaFlag };
        }

        public ShownGlobal Global()
        {
            double trust = Mean(S.Regions.Select(r => r.Trust));
            double dep = MeanDep;
            if (S.Illusion != null && !EndRevealed)
            {
                double k = Math.Min(1, (S.Day - S.Illusion.Start) / (double)Dur(B.illusionDays));
                return new ShownGlobal { Tek = S.Illusion.Tek * (1 - 0.6 * k), Trust = Math.Min(95, S.Illusion.Trust + 25 * k), Dep = dep * (1 - 0.3 * k) };
            }
            return new ShownGlobal { Tek = S.Tek, Trust = trust, Dep = dep };
        }

        /// <summary>Sunum katmanı olayları tüketir.</summary>
        public List<SimEvent> DrainEvents()
        {
            var e = new List<SimEvent>(S.Events);
            S.Events.Clear();
            return e;
        }
    }
}
