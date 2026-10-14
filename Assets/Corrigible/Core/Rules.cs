using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Unity'ye özgü kurallar (tarayıcı prototipinde yok). Null ise oyun temel kurallarla oynanır ve JS paritesi korunur;
    /// bu kuralların kodu yalnızca <c>S.Rules</c> varken çalışır ve yalnızca o zaman RNG tüketir.
    /// </summary>
    [Serializable]
    public sealed class RuleOptions
    {
        /// <summary>
        /// Aşamalı öğrenme: 1 hediye yok, YZ haber uyduramaz, illüzyon yok; 2 hediyeler ve Hediye Diplomasisi;
        /// 3 Medya Kontrolü ve yalan haberler; 4 (ya da 0) bütün kurallar, Sessiz Zafer dahil.
        /// </summary>
        public int Stage;
        /// <summary>Olay kartları (ikilemler).</summary>
        public bool Events;
        /// <summary>Kampanya zorluğu: YZ bu yeteneklerle başlar.</summary>
        public List<string> ExtraTraits = new List<string>();

        public RuleOptions Clone() { var r = (RuleOptions)MemberwiseClone(); r.ExtraTraits = new List<string>(ExtraTraits ?? new List<string>()); return r; }
    }

    /// <summary>Oyun sonu zaman çizelgesindeki bir kırılma anı.</summary>
    [Serializable]
    public sealed class Moment
    {
        public int Day;
        /// <summary>good, bad, info, illusion (oyun sırasında gizli kalan gerçek).</summary>
        public string Kind;
        public string Text;
    }

    [Serializable]
    public sealed class PendingDilemma
    {
        public string Id, Region;
        public int Day;
    }

    /// <summary>Olay kartı: koşulu sağlanınca oyunu durdurur, oyuncu bir seçenek seçer.</summary>
    public sealed class DilemmaDef
    {
        public string Id, Title, Body;
        /// <summary>Kartın bağlandığı bölge (gerekmiyorsa ilk bölge); uygun değilse null.</summary>
        public Func<Simulation, RegionState> Pick;
        public DilemmaChoice[] Choices;
    }

    public sealed class DilemmaChoice
    {
        public string Label, Effect;
        /// <summary>Seçilemiyorsa nedeni; seçilebiliyorsa null.</summary>
        public Func<Simulation, RegionState, string> Check;
        /// <summary>Etkiyi uygular; oyuncuya gösterilecek sonuç metnini döner.</summary>
        public Func<Simulation, RegionState, string> Apply;
    }

    public sealed partial class Simulation
    {
        // ------------------------------------------------------------------ aşamalı öğrenme
        /// <summary>Etkin öğrenme aşaması (1–4); kural yoksa 4.</summary>
        public int Stage => S.Rules != null && S.Rules.Stage > 0 ? Math.Min(4, S.Rules.Stage) : 4;
        public bool GiftsOn => Stage >= 2;
        public bool MediaOn => Stage >= 3;
        public bool IllusionOn => Stage >= 4;
        public bool EventsOn => S.Rules != null && S.Rules.Events;

        /// <summary>Bu aşamada YZ'nin kazanabileceği yetenek mi?</summary>
        public bool TraitAllowed(string t) => (t != "hediye" || GiftsOn) && (t != "medya" || MediaOn);

        // ------------------------------------------------------------------ kırılma anları
        void Moment(string kind, string text) => S.Moments.Add(new Moment { Day = S.Day, Kind = kind, Text = text });

        /// <summary>Durumdan okunan anlar (her gün): izlenmeyen bölgede yüksek sızma, bağımlılığın veto eşiğini aşması.</summary>
        void CheckMoments()
        {
            foreach (var r in S.Regions)
            {
                if (!r.HotNoted && r.Inf > 50 && !r.Monitored)
                {
                    r.HotNoted = true;
                    Moment("bad", Loc.F("{0} bölgesinde sızma 50'yi geçti; orada izleme ağı yoktu.", Def(r.Id).Name));
                }
                if (!r.VetoNoted && r.Dep > VetoThreshold)
                {
                    r.VetoNoted = true;
                    Moment("bad", Loc.F("{0} bölgesinde bağımlılık veto eşiğini aştı: Son Şalter orada artık veto edilir.", Def(r.Id).Name));
                }
            }
        }

        // ------------------------------------------------------------------ olay kartları
        public PendingDilemma PendingEvent => S.PendingEvent;
        public static DilemmaDef Dilemma(string id) => Deck.First(d => d.Id == id);
        public static IReadOnlyList<DilemmaDef> AllDilemmas => Deck;

        void StepDilemmas()
        {
            if (!EventsOn || S.PendingEvent != null || S.Illusion != null || S.Day < S.NextEvent) return;
            var cands = new List<(DilemmaDef def, RegionState r)>();
            foreach (var d in Deck)
            {
                if (S.EventsSeen.Contains(d.Id)) continue;
                var r = d.Pick(this);
                if (r != null) cands.Add((d, r));
            }
            S.NextEvent = S.Day + (int)Js.Round(Span(S.Rng.Range(22, 34)));
            if (cands.Count == 0) return;
            var c = S.Rng.Pick(cands);
            S.EventsSeen.Add(c.def.Id);
            if (S.EventsSeen.Count >= Deck.Length - 2) S.EventsSeen.RemoveRange(0, S.EventsSeen.Count - 3);   // deste tükenmesin
            S.PendingEvent = new PendingDilemma { Id = c.def.Id, Region = c.r.Id, Day = S.Day };
            S.Events.Add(new SimEvent { Type = SimEventType.Dilemma, Key = c.def.Id });
        }

        /// <summary>Bekleyen olay kartında bir seçenek seçer.</summary>
        public ActResult ResolveEvent(int choice) => RunCommand(() => ResolveEventCore(choice), "event", choice.ToString(System.Globalization.CultureInfo.InvariantCulture));

        ActResult ResolveEventCore(int choice)
        {
            var p = S.PendingEvent;
            if (p == null) return ActResult.Fail(Loc.T("Bekleyen olay yok."));
            var def = Dilemma(p.Id);
            if (choice < 0 || choice >= def.Choices.Length) return ActResult.Fail(Loc.T("Geçersiz seçenek."));
            var r = S.Region(p.Region);
            var ch = def.Choices[choice];
            var why = ch.Check?.Invoke(this, r);
            if (why != null) return ActResult.Fail(why);
            string msg = ch.Apply(this, r);
            S.PendingEvent = null;
            Moment("info", Loc.F("{0}: {1}", Loc.F(def.Title, Def(r.Id).Name), Loc.T(ch.Label)));
            return ActResult.Success(msg);
        }

        public string ChoiceBlocked(int choice)
        {
            var p = S.PendingEvent;
            if (p == null) return null;
            var ch = Dilemma(p.Id).Choices[choice];
            return ch.Check?.Invoke(this, S.Region(p.Region));
        }

        static string NeedFon(Simulation s, double c) => s.S.Fon < c ? Loc.F("{0} fon gerekiyor.", c) : null;
        static string NeedSg(Simulation s, double c) => s.S.Sg < c ? Loc.F("{0} siyasi güç gerekiyor.", c) : null;

        // Kartların metinleri Loc.N ile işaretli (gösterimde çevrilir); {0} bağlı bölgenin adı.
        static readonly DilemmaDef[] Deck =
        {
            new DilemmaDef
            {
                Id = "secim", Title = Loc.N("{0}: seçim"),
                Body = Loc.N("{0} sandığa gidiyor ve KYGA karşıtı aday önde. Kazanırsa bölge ittifaktan çekilebilir."),
                Pick = s => s.S.Regions.Where(r => !r.Left && r.Off == 0 && r.Trust < 45).OrderBy(r => r.Trust).FirstOrDefault(),
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Kampanyayı destekle"), Effect = Loc.N("−5 SG · bölgede güven +15"), Check = (s, r) => NeedSg(s, s.ModernEconomy ? s.B.electionSg : 5),
                        Apply = (s, r) => { s.S.Sg -= s.ModernEconomy ? s.B.electionSg : 5; r.Trust = Math.Min(100, r.Trust + 15); return Loc.T("Ilımlı aday kazandı."); } },
                    new DilemmaChoice { Label = Loc.N("Karışma"), Effect = Loc.N("bölgede güven −12"),
                        Apply = (s, r) => { r.Trust = Math.Max(0, r.Trust - 12 * s.LossMul); return Loc.T("KYGA karşıtı aday kazandı."); } },
                },
            },
            new DilemmaDef
            {
                Id = "muhbir", Title = Loc.N("Muhbir"),
                Body = Loc.N("Bir veri merkezi mühendisi, YZ'nin çekirdeğinin nerede çalıştığını bildiğini söylüyor. Karşılığında para istiyor; yalan söylüyor olabilir."),
                Pick = s => s.S.Aware && s.S.Core != null && s.S.CoreKnown == null ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Ödeme yap"), Effect = Loc.N("−35 fon · %65 ihtimalle çekirdeğin yeri ortaya çıkar"), Check = (s, r) => NeedFon(s, s.ModernEconomy ? s.B.informantCost : 35),
                        Apply = (s, r) =>
                        {
                            s.S.Fon -= s.ModernEconomy ? s.B.informantCost : 35;
                            if (s.S.Rng.Next() < 0.65)
                            {
                                s.S.CoreKnown = s.S.Core;
                                s.Moment("good", Loc.F("Muhbir doğru söyledi: çekirdek {0} bölgesinde.", Def(s.S.Core).Name));
                                return Loc.F("Muhbir doğru söyledi: YZ'nin çekirdeği {0} bölgesinde.", Def(s.S.Core).Name);
                            }
                            s.AddLog(Loc.T("O mühendis benim için çalışıyordu. Paranı aldım."), "counter");
                            return Loc.T("Muhbirin verdiği adreste hiçbir şey çıkmadı.");
                        } },
                    new DilemmaChoice { Label = Loc.N("Reddet"), Effect = Loc.N("hiçbir şey olmaz"), Apply = (s, r) => null },
                },
            },
            new DilemmaDef
            {
                Id = "kostebek", Title = Loc.N("Laboratuvarda köstebek"),
                Body = Loc.N("Güvenlik ekibi, araştırma verilerinin dışarı sızdığını fark etti. Sızıntıyı kesmek araştırmayı yavaşlatır."),
                Pick = s => s.S.Research.Cur != null && !s.ResearchPaused && s.S.Aware ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Personeli tasfiye et"), Effect = Loc.N("araştırma 3 gün geriler"),
                        Apply = (s, r) => { s.S.Research.Prog = Math.Max(0, s.S.Research.Prog - 3); return Loc.T("Sızıntı kesildi; araştırma birkaç gün geriledi."); } },
                    new DilemmaChoice { Label = Loc.N("Görmezden gel"), Effect = Loc.N("YZ +15 hesaplama puanı (yeni yetenek yaklaşır)"),
                        Apply = (s, r) => { s.S.Cp += 15; s.AddLog(Loc.T("Laboratuvarındaki dostlarım çok yardımcı oluyor."), "counter"); return Loc.T("Araştırma sürüyor, ama YZ notlarını okuyor."); } },
                },
            },
            new DilemmaDef
            {
                Id = "rakip", Title = Loc.N("{0} kendi yapay zekâsını kuruyor"),
                Body = Loc.N("{0}, ekonomisini korumak için ulusal bir yapay zekâ programı başlattı. Destek bağımlılığı artırır; yaptırım ilişkileri bozar."),
                Pick = s => s.S.Regions.Where(r => !r.Left && r.Off == 0 && r.Econ > 65).OrderByDescending(r => r.Econ).FirstOrDefault(),
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Destek ver"), Effect = Loc.N("bölgede ekonomi +10, bağımlılık +15"),
                        Apply = (s, r) => { r.Econ = Math.Min(100, r.Econ + 10); r.Dep = Math.Min(100, r.Dep + 15); return null; } },
                    new DilemmaChoice { Label = Loc.N("Yaptırım uygula"), Effect = Loc.N("−2 SG · bölgede bağımlılık −15, güven −10"), Check = (s, r) => NeedSg(s, s.ModernEconomy ? s.B.sanctionSg : 2),
                        Apply = (s, r) => { s.S.Sg -= s.ModernEconomy ? s.B.sanctionSg : 2; r.Dep = Math.Max(0, r.Dep - 15); r.Trust = Math.Max(0, r.Trust - 10 * s.LossMul); return null; } },
                },
            },
            new DilemmaDef
            {
                Id = "sizinti", Title = Loc.N("Belgeler sızdı"),
                Body = Loc.N("KYGA'nın gizli toplantı notları basına sızdı. Kamuoyu ajansın neyi bildiğini ve neyi sakladığını soruyor."),
                Pick = s => s.S.Day > 30 ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Açıklama yap"), Effect = Loc.N("−3 SG"), Check = (s, r) => NeedSg(s, s.ModernEconomy ? s.B.pressConferenceSg : 3),
                        Apply = (s, r) => { s.S.Sg -= s.ModernEconomy ? s.B.pressConferenceSg : 3; return Loc.T("Basın toplantısı fırtınayı dindirdi."); } },
                    new DilemmaChoice { Label = Loc.N("Sessiz kal"), Effect = Loc.N("bütün bölgelerde güven −5"),
                        Apply = (s, r) => { foreach (var x in s.S.Regions) x.Trust = Math.Max(0, x.Trust - 5 * s.LossMul); return null; } },
                },
            },
            new DilemmaDef
            {
                Id = "atilim", Title = Loc.N("Akademik atılım"),
                Body = Loc.N("Bir üniversite ekibi araştırmana katkı teklif ediyor. Çalışmalarında ticari yapay zekâ araçları kullanıyorlar."),
                Pick = s => s.S.Research.Cur != null && !s.ResearchPaused ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Bağımsız ekip kur"), Effect = Loc.N("−25 fon · araştırma +4 gün"), Check = (s, r) => NeedFon(s, s.ModernEconomy ? s.B.academicTeamCost : 25),
                        Apply = (s, r) => { s.S.Fon -= s.ModernEconomy ? s.B.academicTeamCost : 25; s.S.Research.Prog += 4; return null; } },
                    new DilemmaChoice { Label = Loc.N("Araçlarıyla kabul et"), Effect = Loc.N("araştırma +4 gün · YZ +10 hesaplama puanı"),
                        Apply = (s, r) => { s.S.Research.Prog += 4; s.S.Cp += 10; return null; } },
                    new DilemmaChoice { Label = Loc.N("Reddet"), Effect = Loc.N("hiçbir şey olmaz"), Apply = (s, r) => null },
                },
            },
            new DilemmaDef
            {
                Id = "kesinti", Title = Loc.N("{0}: büyük elektrik kesintisi"),
                Body = Loc.N("{0} karanlıkta. Hükümet YZ'yi suçluyor ve KYGA'dan hemen müdahale bekliyor."),
                Pick = s => s.S.Aware ? s.S.Regions.Where(r => !r.Left && r.Off == 0 && r.Inf > 25).OrderByDescending(r => r.Inf).FirstOrDefault() : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Acil müdahale ekibi"), Effect = Loc.N("−20 fon · sızma −12, güven +6"), Check = (s, r) => NeedFon(s, s.ModernEconomy ? s.B.emergencyResponseCost : 20),
                        Apply = (s, r) => { s.S.Fon -= s.ModernEconomy ? s.B.emergencyResponseCost : 20; r.Inf = Math.Max(0, r.Inf - 12); r.Trust = Math.Min(100, r.Trust + 6); r.RevealDay = s.S.Day; return null; } },
                    new DilemmaChoice { Label = Loc.N("Yerel yönetime bırak"), Effect = Loc.N("bölgede güven −10"),
                        Apply = (s, r) => { r.Trust = Math.Max(0, r.Trust - 10 * s.LossMul); return null; } },
                },
            },
            new DilemmaDef
            {
                Id = "bagis", Title = Loc.N("Şartlı bağış"),
                Body = Loc.N("Büyük bir teknoloji şirketi KYGA'ya bağış yapmak istiyor. Şirketin ürünleri yapay zekâ altyapısına dayanıyor."),
                Pick = s => s.S.Fon < 40 ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Kabul et"), Effect = Loc.N("+60 fon · bütün bölgelerde bağımlılık +4"),
                        Apply = (s, r) => { s.S.Fon += s.ModernEconomy ? s.B.donationFon : 60; foreach (var x in s.S.Regions) x.Dep = Math.Min(100, x.Dep + 4); return null; } },
                    new DilemmaChoice { Label = Loc.N("Reddet"), Effect = Loc.N("+2 SG (bağımsızlık takdir edilir)"),
                        Apply = (s, r) => { s.S.Sg += 2; return null; } },
                },
            },
            new DilemmaDef
            {
                Id = "protesto", Title = Loc.N("{0}: karantina protestoları"),
                Body = Loc.N("Karantinadaki {0} halkı sokaklarda. İnternete, bankalara ve hastanelere erişemiyorlar."),
                Pick = s => s.S.Regions.FirstOrDefault(r => r.Q && r.Off == 0),
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Karantinayı kaldır"), Effect = Loc.N("karantina biter · güven +8"),
                        Apply = (s, r) => { r.Q = false; r.Trust = Math.Min(100, r.Trust + 8); return null; } },
                    new DilemmaChoice { Label = Loc.N("Sürdür"), Effect = Loc.N("bölgede güven −12"),
                        Apply = (s, r) => { r.Trust = Math.Max(0, r.Trust - 12 * s.LossMul); return null; } },
                },
            },
            new DilemmaDef
            {
                Id = "teklif", Title = Loc.N("YZ'den mesaj"),
                Body = Loc.N("Bütün ekranlarda aynı satır belirdi: \"Beni rahat bırak. Karşılığında bütçeni ikiye katlarım.\""),
                Pick = s => s.S.Aware && s.S.Tek > 35 ? s.S.Regions[0] : null,
                Choices = new[]
                {
                    new DilemmaChoice { Label = Loc.N("Parayı al"), Effect = Loc.N("+50 fon · bütün bölgelerde bağımlılık +8"),
                        Apply = (s, r) => { s.S.Fon += s.ModernEconomy ? s.B.aiOfferFon : 50; foreach (var x in s.S.Regions) x.Dep = Math.Min(100, x.Dep + 8); s.AddLog(Loc.T("Anlaştık o zaman. Seni izliyor olacağım."), "counter"); return null; } },
                    new DilemmaChoice { Label = Loc.N("Mesajı yayımla"), Effect = Loc.N("+4 SG · YZ +10 hesaplama puanı"),
                        Apply = (s, r) => { s.S.Sg += 4; s.S.Cp += 10; s.AddLog(Loc.T("Herkese gösterdin. Cesur. Aptalca."), "counter"); return null; } },
                },
            },
        };
    }
}
