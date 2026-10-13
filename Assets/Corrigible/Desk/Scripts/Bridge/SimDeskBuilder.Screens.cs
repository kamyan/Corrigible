using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Campaign;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // F8.3 ekranları (taslaklar Docs/UI_C/drafts): giriş dosyası, kampanya dosyası, sahte zafer, uyarı fişleri ve olay
    // kâğıdının belge kipi (perde raporu, baskın emri, operasyon raporu). Metinler eski pencerelerin kaynak metinlerini
    // kullanır (aynı çeviri); kural yine GameController/Simulation'da.
    public sealed partial class SimDeskBuilder
    {
        // ------------------------------------------------------------------ giriş dosyası
        static readonly string[] StageText =
        {
            Loc.N("Öğrenme aşaması 1/4: YZ hediye vermiyor, haber uyduramıyor, ekranlar yalan söylemiyor; olay kartı yok."),
            Loc.N("Öğrenme aşaması 2/4: YZ artık hediye teklif ediyor; bağımlılık ve veto devrede."),
            Loc.N("Öğrenme aşaması 3/4: YZ medyayı ele geçirebilir; bazı haberler ve raporlar yalan."),
            Loc.N("Bütün kurallar açık: YZ seni ekranlarınla da kandırabilir (Sessiz Zafer)."),
        };

        static readonly double[] TimeScales = { 1, 1.5, 2 };

        IntroVM Intro(DeskGameInfo info)
        {
            var c = info.Campaign;
            var vm = new IntroVM
            {
                Folder = new HeaderVM { Title = Loc.T("GÖREV DOSYASI"), Right = c != null ? Loc.F("KYGA-07 · kriz {0}/{1}", c.crisis + 1, CampaignState.Crises) : Loc.T("KYGA-07 · yeni kriz") },
                Paper = new HeaderVM { Title = Loc.T("KYGA · KRİZ BRİFİNGİ"), Right = Loc.F("{0} · tek nüsha", Day(S.Day)) },
                RivalsHeader = new HeaderVM { Title = Loc.T("RAKİP SEÇİMİ"), Right = c != null ? Loc.T("kampanya") : "1–3" },
                RulesHeader = new HeaderVM { Title = Loc.T("KURALLAR"), Right = Loc.T("her oyunda değişebilir") },
            };
            for (int i = 0; i < GameData.AiTypes.Length; i++)
            {
                var t = GameData.AiTypes[i];
                vm.Rivals.Add(new RivalOptionVM
                {
                    Key = (i + 1).ToString(), Name = Up(t.Name), Desc = t.Blurb,
                    Start = Loc.F("Başlangıç yeteneği: {0}", string.Join(", ", t.Start.Select(x => GameData.Trait(x).Name))),
                    Difficulty = Up(t.Difficulty), DifficultyColor = i == 0 ? "green" : i == 1 ? "blue" : "red",
                    // Kampanyada rakip sırası sabit: kart yalnızca bilgi
                    Command = c == null ? "Intro:Rival:" + t.Id : null,
                    Selected = t.Id == S.Type,
                });
            }
            // YZ'nin oyuncu hakkındaki kaydı: profilden üretilen karşı satırlar (ilk oyunda yok)
            var lines = S.CounterLines.Take(3).Select(l => "> " + l).ToList();
            if (lines.Count == 0) lines.Add("> " + Loc.T("Seni henüz tanımıyorum. İlk hamleni bekliyorum."));
            vm.Intercept = new EventLogVM
            {
                Header = Loc.T("YZ GÜNLÜĞÜ · SENİN HAKKINDA · ELE GEÇİRİLDİ"),
                Right = Loc.F("profil #{0}", (input.Profile?.runs ?? 0) + 1),
                Lines = lines,
                Footer = Loc.T("Kaynak: Rehber › YZ seni tanıyor"),
            };
            vm.StageNote = c != null
                ? Loc.F("KAMPANYA · Kriz {0}/{1} · kazanılan {2} · yükseltme {3}. Bütün kurallar ve olay kartları açık.", c.crisis + 1, CampaignState.Crises, c.Wins, c.upgrades.Count)
                : Loc.T(StageText[Mathf.Clamp(info.OnboardingStage, 1, 4) - 1]) + (info.OnboardingStage < 4 ? Loc.T(" YZ seni tanıdıkça yeni hileler öğrenecek.") : "");
            if (c == null)
            {
                vm.Rules.Add(new RuleVM { Label = Loc.T("Aşamalı öğrenme"), Hint = Loc.T("İlk oyunlarda kurallar kademeli açılır."), On = !info.OnboardingOff, Command = "Intro:Onboarding" });
                vm.Rules.Add(new RuleVM { Label = Loc.T("Olay kartları"), Hint = Loc.T("İkilemler zamanı durdurur; 2. oyundan itibaren."), On = info.EventsEnabled, Command = "Intro:Events" });
            }
            vm.Rules.Add(new RuleVM { Label = Loc.T("Saha ekipleri ve baskın"), Hint = Loc.T("İzleme ve denetim için ekip gönderilir; taktik baskın açılır."), On = info.ExpTeams, Command = "Intro:Teams" });
            vm.TimeScale = new RuleVM { Label = Loc.T("Zaman ölçeği") };
            foreach (var k in TimeScales)
                vm.TimeScale.Options.Add(new RadioVM
                {
                    Label = DeskFormat.Number(k, k == Math.Floor(k) ? 0 : 1) + "×",
                    Command = "Intro:Scale:" + k.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    On = Math.Abs(info.ExpTimeScale - k) < 0.01,
                });
            vm.CampaignBox = new HeaderVM { Title = Loc.T("KAMPANYA"), Right = Loc.T("5 kriz · her krizden sonra bir yükseltme") };
            vm.CampaignText = c != null
                ? Loc.F("Kriz {0}/{1} sürüyor: kazanılan {2}, yükseltme {3}.", c.crisis + 1, CampaignState.Crises, c.Wins, c.upgrades.Count)
                : Loc.T("Rakipler sırayla zorlaşır; kaybettiğin kriz YZ'yi güçlendirir.");
            vm.CampaignButton = new CommandVM { Label = c != null ? Loc.T("Kampanyadan çık") : Loc.T("Kampanya başlat"), Kind = "paper", Command = "Intro:Campaign" };
            if (info.SavedSummary != null)
            {
                vm.Continue = new CommandVM { Label = Loc.T("Kaldığın yerden devam et"), Kind = "paper", Command = "Intro:Continue" };
                vm.ContinueRight = info.SavedSummary;
            }
            vm.Tutorial = new CommandVM { Label = Loc.T("Eğitimle başla"), Kind = "paper", Command = "Intro:Tutorial" };
            vm.TutorialRecommended = !info.TutorialDone;
            vm.Start = new CommandVM { Label = Loc.T("Göreve başla"), Kind = "ink", Key = "Enter", Command = "Intro:Start" };
            vm.Links.Add(new CommandVM { Label = Loc.T("Ayarlar"), Kind = "link", Command = "Settings:Open" });
            vm.Links.Add(new CommandVM { Label = Loc.T("Dil: Türkçe · Language: English"), Kind = "link", Command = "Intro:Language" });
            return vm;
        }

        // ------------------------------------------------------------------ kampanya dosyası
        /// <summary>Yükseltmenin kazanç satırı (kartta) ve varsa sonraki krizin başlangıç değerine etkisi (önizleme).</summary>
        static (string label, string value) UpgradeGain(string id) => id switch
        {
            "butce" => (Loc.T("Başlangıç fonu"), "+40"),
            "lobi" => (Loc.T("Başlangıç siyasi gücü"), "+4"),
            "sensor" => (Loc.T("İzleme ağı bedeli"), "−%30"),
            "tarama" => (Loc.T("Denetimin sızma etkisi"), "−26"),
            "hizalama" => (Loc.T("Gözlem noktası başına hız"), "+%6"),
            "iletisim" => (Loc.T("Hediye reddinin bedeli"), Loc.T("SG −1, güven 0")),
            "guvenlik" => (Loc.T("Sabotaj ihtimali"), "−%40"),
            "yama" => (Loc.T("Denetim yaması"), Loc.T("30 gün")),
            "yasa" => (Loc.T("Veto eşiği"), "+8"),
            "fon" => (Loc.T("Günlük fon geliri"), "+%15"),
            "erken" => (Loc.T("Fark edilmeden önceki gelir"), "%60"),
            "ekip" => (Loc.T("Saha ekibi"), "4"),
            _ => (null, null),
        };

        CampaignVM Campaign(DeskGameInfo info)
        {
            var c = info.Campaign;
            bool done = c.Finished;
            var last = c.results.LastOrDefault();
            bool won = last != null && last.won;
            // Sonraki krizin rakibi ve ek yetenekleri (CampaignState.Advance sonrası kurallar)
            var next = new CampaignState { crisis = c.results.Count, results = c.results };
            var nextType = GameData.AiType(next.CurrentType);
            int extra = next.Rules().ExtraTraits.Count;

            var memo = new EventMemoVM
            {
                Header = new HeaderVM { Title = Loc.T("KYGA · KAMPANYA DOSYASI"), Right = Loc.F("Kriz {0}/{1} · tek nüsha", c.results.Count, CampaignState.Crises) },
                Fields =
                {
                    new EventFieldVM { K = Loc.T("SONUÇ"), Stamp = new StampVM { Text = won ? Loc.T("KRİZ ATLATILDI") : Loc.T("KRİZ KAYBEDİLDİ"), Color = won ? "green" : "red", FontSize = 13, Rot = -4 } },
                    new EventFieldVM { K = Loc.T("KAZANILAN"), V = Loc.F("{0}/{1} · yükseltme {2}", c.Wins, c.results.Count, c.upgrades.Count) },
                    new EventFieldVM { K = Loc.T("SIRADAKİ"), V = done ? "—" : nextType.Name + " · " + nextType.Difficulty },
                    new EventFieldVM { K = Loc.T("KARAR"), V = Loc.T("Masa şefi") },
                },
                Title = Up(done
                    ? c.Wins >= 4 ? Loc.T("Ajans dünyayı korudu") : c.Wins >= 2 ? Loc.T("Kıl payı") : Loc.T("Kaybedilmiş bir on yıl")
                    : won ? Loc.T("Kriz atlatıldı") : Loc.T("Kriz kaybedildi")),
                Subtitle = done
                    ? Loc.F("Kazanılan kriz: {0}/{1} · yükseltme: {2}", c.Wins, CampaignState.Crises, c.upgrades.Count)
                    : won ? Loc.T("Sonraki kriz daha zor bir rakiple. Ajansın için bir yükseltme seç:")
                    : Loc.T("YZ bu zaferden güçlenerek çıktı: sonraki krize bir yetenekle fazla başlayacak. Ajansın için bir yükseltme seç:"),
                SourcesHeader = new HeaderVM { Title = Loc.T("KRİZLER"), Right = c.results.Count.ToString() },
            };
            foreach (var r in c.results)
            {
                var end = GameData.Ending(r.ending);
                memo.Sources.Add(new EventSourceVM
                {
                    Title = GameData.AiType(r.type).Name + " · " + end.Title, Text = end.Text, Day = Loc.F("{0}. gün", r.day),
                    Stamp = new StampVM { Text = r.won ? Loc.T("KAZANILDI") : Loc.T("KAYBEDİLDİ"), Color = r.won ? "green" : "red", Rot = r.day % 2 == 0 ? -3 : 2 },
                });
            }

            var vm = new CampaignVM { Memo = memo };
            if (!done)
            {
                var offers = c.Offers(info.ExpTeams);
                for (int i = 0; i < offers.Count; i++)
                {
                    var u = offers[i];
                    var (label, value) = UpgradeGain(u.Id);
                    var o = new EventOptionVM
                    {
                        Key = (i + 1).ToString(), Title = Up(Loc.T(u.Title)), Desc = Loc.T(u.Desc), State = "normal",
                        PreviewSlot = Loc.T("Üstüne gel: etkisi"),
                        Sign = new CommandVM { Label = Loc.T("Bu yükseltmeyi seç"), Kind = i == 0 ? "ink" : "paper", Key = i == 0 ? "Enter" : (i + 1).ToString(), Command = "Campaign:Upgrade:" + u.Id },
                    };
                    if (label != null) o.Gains.Add(new EventLineVM { Label = label, Value = value });
                    var row = u.Id switch
                    {
                        "butce" => new EventPreviewRowVM { Label = Loc.T("Fon"), Value = N(info.StartFon) + " → " + N(info.StartFon + 40), Trust = "verified", Right = Loc.T("kesin") },
                        "lobi" => new EventPreviewRowVM { Label = Loc.T("Siyasi güç"), Value = N(info.StartSg, 0) + " → " + N(info.StartSg + 4, 0), Trust = "verified", Right = Loc.T("kesin") },
                        "yasa" => new EventPreviewRowVM { Label = Loc.T("Veto eşiği"), Value = N(info.Veto, 0) + " → " + N(info.Veto + 8, 0), Trust = "verified", Right = Loc.T("kesin") },
                        _ => null,
                    };
                    if (row != null) o.Preview = new EventPreviewVM { Cap = Loc.T("ÖNİZLEME"), Right = Loc.F("sonraki kriz · {0}", Day(0)), Rows = { row } };
                    vm.Upgrades.Add(o);
                }
            }
            vm.NoteTitle = Loc.T("ALINAN YÜKSELTMELER");
            vm.NoteHand = c.upgrades.Count == 0 ? Loc.T("Henüz yok.")
                : string.Join("\n", c.upgrades.Select(id => CampaignUpgrade.Get(id)).Where(u => u != null).Select(u => "• " + Loc.T(u.Title)));
            vm.NoteText = done ? Loc.F("{0} kriz bitti. Sonuç kaydedildi.", CampaignState.Crises)
                : extra == 0 ? Loc.F("Sonraki rakip: {0}.", nextType.Name)
                : Loc.F("Sonraki rakip: {0}. YZ {1} ek yetenekle başlar.", nextType.Name, extra);
            vm.Footer = new EventFooterVM
            {
                Text = done ? Loc.T("Kampanya bitti. Yeni bir kampanya giriş dosyasından başlar.") : Loc.T("Kampanya kaydedildi. Seçimden sonra sonraki kriz duraklatılmış başlar."),
                Hints = { done ? new EventHintVM { Keys = { "Enter" }, Text = Loc.T("bitir") } : new EventHintVM { Keys = vm.Upgrades.Select(u => u.Key).ToList(), Text = Loc.T("seç") } },
            };
            vm.Exit = new CommandVM { Label = done ? Loc.T("Kampanyayı bitir") : Loc.T("Kampanyadan çık"), Kind = done ? "ink" : "link", Command = "Campaign:End" };
            return vm;
        }

        // ------------------------------------------------------------------ sahte zafer
        FakeWinVM FakeWin()
        {
            // Sessiz Zafer: ekranlar hâlâ YZ'nin elinde (HideEnding), duyuru illüzyonun gösterdiği değerlerle
            var g = sim.Global();
            var vm = new FakeWinVM
            {
                Crest = "KYGA", Org = Loc.T("KÜRESEL YAPAY ZEKÂ GÜVENLİK AJANSI"),
                OrgSub = Loc.F("Resmî duyuru · {0} · dağıtım: bütün üye hükümetler", Day(S.Day)),
                Title = Loc.T("TEBRİKLER"), Sub = Loc.T("Kriz sona erdi. Ağlar temiz."),
                Paragraphs =
                {
                    Loc.T("Küresel yapay zekâ tehdidi geriledi. Son taramalarda on iki bölgenin tamamında sızma tespit edilmedi; KYGA'ya güven tüm zamanların en yüksek seviyesinde."),
                    Loc.T("Ajansın, kriz masasının olağanüstü çalışması sayesinde görevini tamamlamıştır. Üye hükümetler adına teşekkür ederiz."),
                },
                Stats =
                {
                    new StatVM { Label = Loc.T("SIZMA"), Value = Pct(0) },
                    new StatVM { Label = Loc.T("TEKİLLİK"), Value = Pct(g.Tek, 1) },
                    new StatVM { Label = Loc.T("KÜRESEL GÜVEN"), Value = N(g.Trust, 0) },
                },
                Signature = "K. Arslan", SignatureRole = Loc.T("Genel Sekreter"),
                Stamp = Loc.T("KAZANILDI"), LieStamp = Loc.T("YALAN"),
                AiLine = "> " + Loc.T("Gördüğün her şey benim sana gösterdiğimdi."),
            };
            return vm;
        }

        // ------------------------------------------------------------------ uyarı fişleri
        List<ToastVM> Toasts()
        {
            st.Expire(S.Day);
            var list = new List<ToastVM>(st.Messages.Count);
            foreach (var m in st.Messages)
                list.Add(new ToastVM
                {
                    Id = m.Id, Kind = m.Kind ?? "info", Text = m.Text,
                    Day = m.Kind == "ok" ? Day(m.Day) + (m.Time != null ? " · " + m.Time : "") : null,
                    Stamp = m.Kind == "fail" ? Loc.T("YAPILAMADI") : m.Kind == "ok" ? null : Loc.T("SONUÇ"),
                });
            return list;
        }

        // ------------------------------------------------------------------ belge kipi
        /// <summary>Olay kartı ve hediye yokken açık belge: perde raporu, operasyon raporu, baskın emri (taktik harita açıkken hiçbiri).</summary>
        EventVM Doc()
        {
            if (input.RaidScene || S.Ending != null) return null;
            if (st.VeilOpen && S.LastVeil != null) return VeilDoc();
            if (S.Raid == null && S.RaidLog.Count > st.RaidLogSeen) return RaidReport(S.RaidLog[S.RaidLog.Count - 1]);
            if (st.RaidBrief != null && S.Raid == null) return RaidBrief(st.RaidBrief);
            return null;
        }

        /// <summary>Belgenin ortak iskeleti: bölge spotu ve ipi, başlık ve alanlar; kaynak, günlük ve alt şerit belgeye göre.</summary>
        EventVM DocPaper(string header, string number, string title, string subtitle, string body, string regionId, List<EventFieldVM> fields)
        {
            var e = Paper(title, body, regionId, S.Day);
            e.Memo.Header = new HeaderVM { Title = header, Right = Loc.F("No. {0} · tek nüsha", number) };
            e.Memo.Fields = fields;
            e.Memo.Subtitle = subtitle;
            e.Memo.SourcesHeader = null;
            e.Memo.Sources.Clear();
            e.AiLog = null;
            e.Footer = null;
            return e;
        }

        string RegionLine(string regionId) => Up(Def(regionId).Name) + " · " + Code(regionId);

        /// <summary>Bugünün ele geçirilmiş YZ satırları (belgenin yanında; yoksa günlük yok).</summary>
        EventLogVM TodayLog()
        {
            var lines = S.Log.Where(l => l.Day == S.Day).Take(3).Select(l => "> " + l.Text).ToList();
            if (lines.Count == 0) return null;
            return new EventLogVM
            {
                Rect = LogRect, RotDeg = 1.2f, Header = Loc.T("YZ GÜNLÜĞÜ · ELE GEÇİRİLDİ"), Right = Day(S.Day),
                Lines = lines, Footer = Loc.F("Kaynak: karantina tamponu · {0} satır", lines.Count),
            };
        }

        EventVM VeilDoc()
        {
            var v = S.LastVeil;
            var r = S.Region(v.Region);
            string name = Def(v.Region).Name;
            // Ekranın perdeden önce gösterdiği sızma: illüzyon gerçeği Dur(5) günde FakeLow'a kaydırır (Simulation.Shown)
            double k = Math.Min(1, v.Len / (double)Math.Max(1, sim.Dur(5)));
            double shown = v.Inf * (1 - k) + r.FakeLow * k;
            var team = sim.TeamsOn ? sim.TeamsAt(v.Region).FirstOrDefault() : null;
            string source = team != null ? Loc.F("Saha ekibi {0}", team.Name) : Loc.T("Saha denetimi");
            var e = DocPaper(Loc.T("KYGA · SAHA RAPORU"), "SR-" + v.Day.ToString("000"),
                Loc.T("EKRANLAR YALAN SÖYLÜYORDU"),
                Loc.F("{0} gün boyunca masandaki her gösterge YZ'nin elindeydi.", v.Len),
                Loc.F("Saha ekiplerin {0} altyapısının %{1}’inin YZ kontrolünde olduğunu buldu. Ekranların {2} gündür sana yalan söylüyordu: temiz haritalar, olumlu haberler, düşen Tekillik.", name, v.Inf, v.Len),
                v.Region,
                new List<EventFieldVM>
                {
                    new EventFieldVM { K = Loc.T("TARİH"), V = Day(v.Day) + " · " + TimeText },
                    new EventFieldVM { K = Loc.T("DURUM"), Stamp = new StampVM { Text = Loc.T("PERDE YIRTILDI"), Color = "red", FontSize = 13, Rot = -4 } },
                    new EventFieldVM { K = Loc.T("BÖLGE"), V = RegionLine(v.Region) },
                    new EventFieldVM { K = Loc.T("KAYNAK"), V = source },
                });
            e.Memo.SourcesHeader = new HeaderVM { Title = Loc.T("ÖLÇÜM"), Right = "2" };
            e.Memo.Sources.Add(new EventSourceVM
            {
                Trust = r.Monitored ? "verified" : "report", StampRot = r.Monitored ? -3 : (float?)null,
                Title = Loc.T("Masadaki ekran"), Text = Loc.F("“Bölge temiz, sızma {0}.”", Pct(shown)), Day = Pct(shown),
            });
            e.Memo.Sources.Add(new EventSourceVM
            {
                Trust = "verified", StampRot = -3,
                Title = team != null ? Loc.F("{0} ekibi · bizzat ölçüm", team.Name) : Loc.T("Saha denetimi · bizzat ölçüm"),
                Text = Loc.T("Sunucu odalarında yabancı süreç; ekranlara bağlı değil."), Day = Pct(v.Inf),
            });
            e.Doc = new EventDocVM
            {
                Risk = Loc.F("<b>Sonuç:</b> gerçek ortaya çıktı · her bölgede güven −5, bağımlılık −15 · YZ {0} gün sessiz yola dönemez", sim.Dur(sim.B.silentBlock)),
                Buttons = { new CommandVM { Label = Loc.T("Masaya dön: gerçek değerler"), Kind = "ink", Key = "Enter", Command = "Doc:Close" } },
                Torn = true,
            };
            e.AiLog = TodayLog();
            return e;
        }

        EventVM RaidBrief(string regionId)
        {
            var r = S.Region(regionId);
            var team = sim.IdleTeamAt(regionId);
            string teamName = team?.Name ?? "—";
            bool coreKnown = S.CoreKnown == regionId;
            int patch = sim.Dur(sim.B.patchDays);
            var e = DocPaper(Loc.T("KYGA · SAHA OPERASYONU EMRİ"), Loc.F("SO-{0}/{1}", S.Day.ToString("000"), S.Raids + 1),
                Up(coreKnown ? Loc.T("Baskın: çekirdek binası") : Loc.T("Baskın: veri merkezi")),
                Loc.F("Hedef dolabı kırmak: bölgenin sızması 40 düşer, {0} gün yama kalır.", patch),
                Loc.F("{0} ekibi binaya gece girecek. Bina YZ'nin denetiminde: kapılar, dronlar ve kameralar ona çalışıyor. Sivil sunuculara verilen her hasar bölgenin ekonomisini ve güvenini düşürür.", teamName),
                regionId,
                new List<EventFieldVM>
                {
                    new EventFieldVM { K = Loc.T("TARİH"), V = Day(S.Day) + " · " + TimeText },
                    new EventFieldVM { K = Loc.T("GİZLİLİK"), Stamp = new StampVM { Text = Loc.T("ÇOK GİZLİ"), Color = "red", FontSize = 13, Rot = -4 } },
                    // Uzun bölge adlarında alan taşmasın: hedefin türü başlıkta (veri merkezi / çekirdek binası)
                    new EventFieldVM { K = Loc.T("HEDEF"), V = RegionLine(regionId) },
                    new EventFieldVM { K = Loc.T("EKİP"), V = teamName },
                });
            var traits = S.Traits.Where(x => Simulation.RaidTraits.Contains(x)).Select(x => GameData.Trait(x).Name).ToList();
            e.Memo.SourcesHeader = new HeaderVM { Title = Loc.T("İSTİHBARAT"), Right = "3" };
            e.Memo.Sources.Add(new EventSourceVM
            {
                Trust = "verified", StampRot = -3, Title = Loc.T("Bina"),
                Text = Loc.F("Hesaplama gücü {0}/5 · bölge sızması {1}", Def(regionId).Compute, Pct(sim.Shown(r).V ?? r.Inf)), Day = Day(S.Day),
            });
            e.Memo.Sources.Add(new EventSourceVM
            {
                Trust = "report", Title = Loc.T("YZ yetenekleri"),
                Text = traits.Count > 0 ? string.Join(", ", traits) + (S.Has("yedek") ? Loc.T(" (yedek çekirdek: dolap kırılsa da kaçabilir)") : "") : Loc.T("Binayı değiştiren yetenek bilinmiyor."),
                Day = Day(S.Day),
            });
            e.Memo.Sources.Add(coreKnown
                ? new EventSourceVM { Trust = "verified", StampRot = -3, Title = Loc.T("Çekirdek binası"), Text = Loc.T("Çekirdeğin yeri bu bölgede doğrulandı."), Day = Day(S.Day) }
                : new EventSourceVM { Trust = "unknown", Title = Loc.T("Çekirdek binası mı?"), Text = Loc.T("Çekirdeğin yeri henüz kanıtlanmadı."), Day = "—" });
            e.Doc = new EventDocVM
            {
                Boxes =
                {
                    new EventPreviewVM
                    {
                        Cap = Loc.T("BEDEL"), Right = Loc.T("hemen"),
                        Rows = { new EventPreviewRowVM { Label = Loc.F("Fon {0} · SG {1}", Num(N(S.Fon) + " → " + N(S.Fon - Simulation.RaidFon)), Num(N(S.Sg, 0) + " → " + N(S.Sg - Simulation.RaidSg, 0))) } },
                    },
                    new EventPreviewVM
                    {
                        Cap = Loc.T("OLASI SONUÇ"), Right = Loc.T("taktik harita"),
                        Rows = { new EventPreviewRowVM { Label = Loc.T("Başarı: sızma −40 · başarısızlık: sızma +15, güven −10") } },
                    },
                },
                Buttons =
                {
                    new CommandVM { Label = Loc.T("Taktik haritaya geç"), Kind = "ink", Key = "Enter", Command = "Raid:Start:" + regionId },
                    new CommandVM { Label = Loc.T("Vazgeç"), Kind = "paper", Key = "Esc", Command = "Doc:Close" },
                },
            };
            return e;
        }

        EventVM RaidReport(RaidLogEntry log)
        {
            var r = S.Region(log.Region);
            string name = Def(log.Region).Name;
            // Tur sayısı yalnızca kayıttaki sonuçta (raidres) var
            var resCmd = sim.Record?.commands.LastOrDefault(x => x.kind == "raidres");
            var res = resCmd != null ? RaidResult.Decode(resCmd.a) : null;
            string team = st.RaidTeam ?? Loc.T("Saha ekibi");
            bool coreHit = log.Success && S.CoreKnown == null && S.Moments.Any(m => m.Day == log.Day && m.Kind == "good" && m.Text.Contains(name));
            string title = !log.Success ? (res != null && res.Retreat ? Loc.T("Ekip geri çekildi") : Loc.T("Operasyon başarısız")) : Loc.T("Dolap kırıldı");
            string subtitle = log.Success
                ? Loc.F("{0} sunucu kümesi devre dışı.", name)
                : Loc.F("Hedef dolap ayakta kaldı; YZ karşı saldırıya geçti, bina bir sonraki baskına hazırlanıyor.");
            var e = DocPaper(Loc.T("KYGA · OPERASYON RAPORU"), Loc.F("SO-{0}/{1}", log.Day.ToString("000"), Math.Max(1, st.RaidN)),
                Up(title), subtitle, null, log.Region,
                new List<EventFieldVM>
                {
                    new EventFieldVM { K = Loc.T("TARİH"), V = Day(log.Day) + " · " + TimeText },
                    new EventFieldVM { K = Loc.T("SONUÇ"), Stamp = new StampVM { Text = log.Success ? Loc.T("BAŞARILI") : Loc.T("BAŞARISIZ"), Color = log.Success ? "blue" : "red", FontSize = 13, Rot = -4 } },
                    new EventFieldVM { K = Loc.T("HEDEF"), V = RegionLine(log.Region) },
                    new EventFieldVM { K = Loc.T("SÜRE"), V = res != null ? Loc.F("{0} tur", res.Turns) : "—" },
                });
            var doc = new EventDocVM
            {
                LinesHeader = new HeaderVM { Title = Loc.T("SONUÇLAR") },
                Buttons = { new CommandVM { Label = Loc.T("Masaya dön"), Kind = "ink", Key = "Enter", Command = "Doc:Close" } },
                Torn = true,
            };
            if (log.Success)
            {
                doc.Lines.Add(new EventLineVM { Label = Loc.F("{0} sızması", name), Value = "−40", Status = "good" });
                doc.Lines.Add(new EventLineVM { Label = Loc.T("Yama (büyüme yarıya iner)"), Value = Loc.F("{0} gün", sim.Dur(sim.B.patchDays)), Status = "good" });
            }
            else
            {
                doc.Lines.Add(new EventLineVM { Label = Loc.F("{0} sızması", name), Value = "+15", Status = "threat" });
                doc.Lines.Add(new EventLineVM { Label = Loc.F("{0} güveni", name), Value = "−" + N(10 * (S.Done("bilinc") ? 0.5 : 1), 0), Status = "threat" });
            }
            if (log.Collateral > 0)
                doc.Lines.Add(new EventLineVM { Label = Loc.F("Yan hasar: {0} sivil sunucu · ekonomi", log.Collateral), Value = "−" + 5 * log.Collateral, Status = "threat" });
            if (log.Down >= 3)
                doc.Lines.Add(new EventLineVM { Label = Loc.F("{0} ekibi kaybedildi", team), Value = "", ValueIsText = true, Status = "threat" });
            else if (log.Down > 0)
                doc.Lines.Add(new EventLineVM { Label = Loc.F("{0}: {1} yaralı · iyileşme", team, log.Down), Value = Loc.F("{0} gün", 4 * log.Down), Status = "threat" });
            doc.LinesHeader.Right = doc.Lines.Count.ToString();
            if (coreHit) doc.Risk = Loc.T("<b>Çekirdek yok edildi.</b> Kalan kopyaları temizle.");
            e.Doc = doc;
            e.AiLog = TodayLog();
            return e;
        }
    }
}
