using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Kriz masası arayüzü (UI Toolkit). Simülasyonu yalnızca Algı Katmanı üzerinden okur (Shown/Global)
    /// ve tüm değişiklikleri GameController komutlarıyla yapar.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CorrigibleUI : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] StyleSheet styleSheet;

        VisualElement root, overlay, modal;
        Label dayLabel, foeLabel, toast;
        readonly Button[] speedButtons = new Button[4];
        readonly int[] speeds = { 0, 1, 2, 4 };
        Gauge gFon, gSg, gTrust, gDep, gTek;
        VisualElement silentNotch;
        VisualElement advisorBox; Label advisorText;
        MapView map;
        VisualElement regionBox;
        RegionPanel regionPanel;
        Button[] tabButtons; VisualElement[] tabPanels; int tab;
        Label alignInfo, alignRisk; VisualElement researchList;
        readonly Dictionary<string, ResearchRow> researchRows = new Dictionary<string, ResearchRow>();
        ScrollView logView; int logRendered = -1, logSeen;
        VisualElement dossier;
        Button killButton; bool killArmed; IVisualElementScheduledItem killDisarm;
        Label tickText, tickAgency; VisualElement headlines; string newsSig = "";
        string selected;
        bool endHandled, replayMode;
        IVisualElementScheduledItem toastHide;

        Simulation Sim => game.Sim;
        WorldState S => game.Sim.S;

        // Start: tüm Awake'ler (simülasyonun kurulması) bittikten sonra çalışır
        void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            Build();
            game.Changed += Render;
            game.EventsRaised += OnEvents;
            game.NewGameStarted += OnNewGame;
            OnNewGame();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.Changed -= Render;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= OnNewGame;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (overlay == null) return;
            if (kb != null && kb.spaceKey.wasPressedThisFrame && overlay.style.display == DisplayStyle.None)
                game.SetSpeed(game.Speed == 0 ? lastSpeed : 0);
            if (game.Speed > 0) lastSpeed = game.Speed;
        }
        int lastSpeed = 1;

        // ---------- Kurulum ----------
        static VisualElement Div(string cls = null, VisualElement parent = null)
        {
            var e = new VisualElement();
            if (cls != null) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            parent?.Add(e);
            return e;
        }

        static readonly System.Globalization.CultureInfo Tr = new System.Globalization.CultureInfo("tr-TR");
        static string Up(string s) => s.ToUpper(Tr);

        static Label Lbl(string text, string cls = null, VisualElement parent = null)
        {
            var l = new Label(text);
            if (cls != null) foreach (var c in cls.Split(' ')) l.AddToClassList(c);
            parent?.Add(l);
            return l;
        }

        static Button Btn(string text, string cls, Action onClick, VisualElement parent = null)
        {
            var b = new Button(onClick) { text = text };
            if (cls != null) foreach (var c in cls.Split(' ')) b.AddToClassList(c);
            parent?.Add(b);
            return b;
        }

        void Build()
        {
            var app = Div("root", root);

            // Üst şerit
            var top = Div("top", app);
            var brand = Div("row", top);
            Lbl("CORRIGIBLE", "brand", brand);
            var sub = Div(null, brand);
            Lbl("SON ŞALTER · KYGA KRİZ MASASI", "brand-sub", sub);
            foeLabel = Lbl("", "brand-sub threat", sub);
            var clock = Div("row", top);
            dayLabel = Lbl("GÜN 000", "day", clock);
            var speed = Div("speed", clock);
            string[] names = { "Duraklat", "1×", "2×", "4×" };
            for (int i = 0; i < 4; i++)
            {
                int s = speeds[i];
                speedButtons[i] = Btn(names[i], null, () => game.SetSpeed(s), speed);
            }

            // Göstergeler
            var gauges = Div("gauges", app);
            gFon = new Gauge(gauges, "FON");
            gSg = new Gauge(gauges, "SİYASİ GÜÇ");
            gTrust = new Gauge(gauges, "KÜRESEL GÜVEN", "15 altı dağılır");
            gTrust.Notch(15);
            gDep = new Gauge(gauges, "BAĞIMLILIK");
            silentNotch = gDep.Notch(40); gDep.Notch(70); gDep.Notch(85);
            gTek = new Gauge(gauges, "TEKİLLİK", "100 = kayıp");

            // Danışman
            advisorBox = Div("advisor", app);
            Lbl("DANIŞMAN", "advisor-who", advisorBox);
            advisorText = Lbl("", "grow", advisorBox);
            Btn("Anladım", "textbtn", () => game.DismissAdvice(), advisorBox);

            // Ana ızgara
            var main = Div("main", app);
            var mapCard = Div("card mapcard", main);
            map = new MapView();
            map.RegionClicked += Select;
            mapCard.Add(map);
            var legend = Div("legend", mapCard);
            foreach (var t in new[] { "Sızma: koyu 0 → kırmızı 100", "Mavi rozet: doğrulanmış", "İtalik: rapor", "Sarı rozet: şüpheli kaynak", "?: bilinmiyor", "Sarı kesik halka: karantina", "Ç: YZ çekirdeği" })
                Lbl(t, null, legend);

            var side = new ScrollView(ScrollViewMode.Vertical);
            side.AddToClassList("side");
            main.Add(side);
            regionBox = Div("card region", side);
            regionPanel = new RegionPanel(regionBox, game, this);

            var tabsCard = Div("card", side);
            var tabs = Div("tabs", tabsCard);
            tabButtons = new[] { Btn("ARAŞTIRMA", "tab", () => SetTab(0), tabs), Btn("YZ GÜNLÜĞÜ", "tab", () => SetTab(1), tabs), Btn("REHBER", "tab", () => SetTab(2), tabs) };

            var rsPanel = new ScrollView(); rsPanel.AddToClassList("tabpanel"); tabsCard.Add(rsPanel);
            var alignBox = Div("alignbox", rsPanel);
            alignInfo = Lbl("", null, alignBox);
            alignRisk = Lbl("", "threat", alignBox);
            researchList = Div(null, rsPanel);
            BuildResearch();

            logView = new ScrollView(); logView.AddToClassList("tabpanel"); tabsCard.Add(logView);

            var help = new ScrollView(); help.AddToClassList("tabpanel"); tabsCard.Add(help);
            var guide = Div("guide", help);
            foreach (var g in GuideLines) Lbl(g, null, guide);
            dossier = Div("dossier", help);
            Btn("YZ'nin hafızasını sil", "textbtn", () => { game.ClearProfile(); RenderDossier(); Toast("Hafıza silindi; sonraki oyunda YZ seni tanımayacak.", true); }, help);
            tabPanels = new VisualElement[] { rsPanel, logView, help };
            SetTab(0);

            var gk = Div("card globalkill", side);
            killButton = Btn("KÜRESEL SON ŞALTER", "killbtn", OnKillClicked, gk);
            Lbl("Tüm dünyanın altyapısını kapatır ve oyunu bitirir. YZ yok olur, medeniyet geriler. En az 50 siyasi güç ister.", "muted", gk).style.fontSize = 12;

            // Haber bandı
            var news = Div("card news", app);
            var ticker = Div("ticker", news);
            Lbl("CANLI", "live", ticker);
            tickText = Lbl("Bağlantı kuruluyor…", "tick-text", ticker);
            tickAgency = Lbl("", "tick-agency", ticker);
            var hlScroll = new ScrollView(); hlScroll.style.flexGrow = 1; news.Add(hlScroll);
            headlines = Div("headlines", hlScroll);

            // Pencere ve bildirim katmanları
            overlay = Div("overlay", root);
            modal = Div("modal", overlay);
            overlay.style.display = DisplayStyle.None;
            toast = Lbl("", "toast", root);
            toast.pickingMode = PickingMode.Ignore;
        }

        static readonly string[] GuideLines =
        {
            "Bul. İzleme ağı kurduğun bölgede gerçeği görürsün. Haber bandındaki tuhaf haberler YZ’nin başladığı yeri ele verebilir.",
            "Gözlemle. Hizalama, YZ tespit edilmeden başlamaz. YZ’nin tespit edildiği her izlenen bölge bir gözlem noktasıdır; dört nokta tam hızdır.",
            "Koru. Karantinadaki bölgenin hesaplama gücü YZ’den kopar (Tekillik katkısı %20). Denetim sızmayı 20 düşürür ve 20 gün yama bırakır.",
            "Bütçe. Araştırma maliyetini her gün öder. Denetim ve izleme aynı bütçeden ödenir; fon yetmezse araştırma yavaşlar.",
            "Hediyeler. Kabul edilen her hediye bölgeye sızma taşır. Analog Yedekler ile denetimli kabul açılır.",
            "Çekirdek. YZ fark edildiği an çekirdeğini kaçırır. Denetim çekirdeğin bölgesini 20’nin altına indiremez ve bu onu ele verir. Yeri kanıtlanmış çekirdeğe Son Şalter 20 siyasi güç ister; başka bir bölgede %20+ sızma varsa çekirdek kaçar. Silme için çekirdek yok edilmeli.",
            "Şüphelen. Haber bandı yalan söyleyebilir. Ajans adlarına bak; Doğrulama Ağı ile sahte haber yayan kaynağı işaretle.",
            "Kısayol: Boşluk tuşu oyunu duraklatır ve devam ettirir.",
        };

        void BuildResearch()
        {
            researchList.Clear();
            researchRows.Clear();
            string last = null;
            foreach (var rs in GameData.Research)
            {
                if (rs.Branch != last) { Lbl(Up(rs.Branch), "branch", researchList); last = rs.Branch; }
                researchRows[rs.Id] = new ResearchRow(researchList, rs, () =>
                {
                    var r = game.StartResearch(rs.Id);
                    if (!r.Ok) Toast(r.Msg);
                });
            }
        }

        void SetTab(int i)
        {
            tab = i;
            for (int k = 0; k < tabButtons.Length; k++)
            {
                tabButtons[k].EnableInClassList("on", k == i);
                tabPanels[k].style.display = k == i ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (i == 1 && game.Sim != null) logSeen = S.Log.Count;
            if (i == 2) RenderDossier();
            if (game.Sim != null) RenderLog();
        }

        // ---------- Oyun akışı ----------
        void OnNewGame()
        {
            selected = null; endHandled = false; replayMode = false; logRendered = -1; logSeen = 0; newsSig = "";
            map.Bind(Sim);
            map.Selected = null;
            regionPanel.Bind(Sim, null);
            killButton.text = "KÜRESEL SON ŞALTER"; killArmed = false; killButton.RemoveFromClassList("armed");
            tickText.text = "Piyasalar sakin. Gündemde yapay zekâ yatırımları var.";
            tickAgency.text = "Atlas Haber";
            Render();
            ShowIntro();
        }

        void Select(string id)
        {
            selected = id;
            map.Selected = id;
            regionPanel.Bind(Sim, id);
            Render();
        }

        void OnEvents(List<SimEvent> events)
        {
            Headline latest = null; bool gift = false, end = false, veil = false;
            foreach (var e in events)
            {
                if (e.Type == SimEventType.News) latest = e.Headline;
                else if (e.Type == SimEventType.Gift) gift = true;
                else if (e.Type == SimEventType.End) end = true;
                else if (e.Type == SimEventType.Veil) veil = true;
            }
            if (latest != null) Flash(latest);
            if (end) OnEnd();
            else if (veil && !ModalOpen) ShowVeil();
            else if (gift && S.PendingGift != null && !ModalOpen) ShowGift();
        }

        void Flash(Headline h)
        {
            tickText.text = h.Text;
            tickAgency.text = h.Agency + (h.Src != null ? " · " + GameData.Region(h.Src).Name : "");
        }

        // ---------- Çizim ----------
        void Render()
        {
            if (game.Sim == null) return;
            var g = Sim.Global();
            dayLabel.text = $"GÜN {S.Day:000}";
            bool glitch = S.Illusion != null && S.Ending == null && UnityEngine.Random.value < 0.18f;
            dayLabel.EnableInClassList("glitch", glitch);
            foeLabel.text = "RAKİP: " + Up(Sim.Type.Name);
            for (int i = 0; i < 4; i++) speedButtons[i].EnableInClassList("on", game.Speed == speeds[i]);

            gFon.Set(Math.Floor(S.Fon).ToString("0"), S.Fon / 2, null, S.Aware ? "" : "bütçe %40");
            gSg.Set(S.Sg.ToString("0.0", Tr), S.Sg / 0.6, null, "+" + Sim.SgRate.ToString("0.00", Tr) + "/gün");
            gTrust.Set(Js.Round(g.Trust).ToString(), g.Trust, g.Trust < 25 ? Var.Threat : g.Trust < 40 ? Var.Warn : Var.Fg);
            bool knowsSilent = game.Profile != null && game.Profile.endings.Contains("sessiz");
            silentNotch.EnableInClassList("silent", knowsSilent);
            gDep.Set(Js.Round(g.Dep).ToString(), g.Dep, g.Dep >= 70 ? Var.Threat : g.Dep >= 40 ? Var.Warn : Var.Fg, (knowsSilent ? "40: sessiz risk · " : "") + $"veto {Sim.VetoThreshold}");
            gTek.Set(g.Tek.ToString("0.0", Tr) + "%", g.Tek, Var.Threat);

            var adv = S.Advisor;
            advisorBox.style.display = adv != null && !adv.Dismissed && S.Ending == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (adv != null) advisorText.text = adv.Text;

            map.Refresh();
            regionPanel.Refresh();
            RenderResearch();
            RenderLog();
            RenderNews();
            if (!replayMode) killButton.SetEnabled(S.Sg >= 50 && S.Ending == null);
        }

        void RenderResearch()
        {
            if (!S.Aware) { alignInfo.text = "Hizalama kolu kilitli: gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et."; alignRisk.text = ""; }
            else
            {
                alignInfo.text = $"Hizalama hızı %{Js.Round(Sim.AlignSpeed * 100)} · gözlem noktası {Sim.Studied}/4";
                alignRisk.text = "Sabotaj riski %" + (Sim.SabotageChance * 100).ToString("0.0", Tr) + "/gün · sızma düştükçe azalır";
            }
            foreach (var row in researchRows.Values) row.Refresh(Sim);
        }

        void RenderLog()
        {
            int count = S.Log.Count;
            if (tab == 1) logSeen = count;
            int unread = count - logSeen;
            tabButtons[1].text = tab != 1 && unread > 0 ? $"YZ GÜNLÜĞÜ ({unread})" : "YZ GÜNLÜĞÜ";
            if (logRendered == count) return;
            logRendered = count;
            logView.Clear();
            foreach (var l in S.Log.Take(80))
            {
                var line = Div("logline", logView);
                Lbl($"G{l.Day:000}", "logline-day", line);
                Lbl(l.Text, "logline-text log-" + l.Kind, line);
            }
        }

        void RenderDossier()
        {
            dossier.Clear();
            var p = game.Profile;
            Lbl("YZ’NİN SENİN HAKKINDAKİ DOSYASI", "eyebrow", dossier);
            if (p == null || p.runs == 0) { Lbl("Boş. YZ seni ilk oyunundan sonra tanımaya başlayacak.", "muted", dossier); return; }
            var top = p.TopAction();
            string[] ends = p.endings.Skip(Math.Max(0, p.endings.Count - 3)).Select(k => GameData.Endings.Any(e => e.Id == k) ? GameData.Ending(k).Title : k).ToArray();
            Lbl($"Oyun: {p.runs}", null, dossier);
            Lbl($"Alışkanlık: {(top.Value > 0 ? ActionName(top.Key) + " (ağırlık " + top.Value + ")" : "belirsiz")}", null, dossier);
            Lbl($"İlk araştırma: {p.lastBranch ?? "—"}", null, dossier);
            Lbl($"Son sonlar: {(ends.Length > 0 ? string.Join(", ", ends) : "—")}", null, dossier);
            Lbl($"YZ’yi yanılttın: {p.misreads} kez · Perde yırttın: {p.veils} kez", null, dossier);
        }

        static string ActionName(string a) => a switch
        {
            Actions.Monitor => "izleme ağı",
            Actions.Audit => "denetim",
            Actions.Quarantine => "karantina",
            _ => "Son Şalter",
        };

        void RenderNews()
        {
            bool canVerify = S.Done("dogrulama") && S.Ending == null;
            string sig = $"{S.News.Count}:{(S.News.Count > 0 ? S.News[0].Id : "")}:{S.News.Count(h => h.Verified != null || h.Suspect)}:{canVerify}:{S.Ending}";
            if (sig == newsSig) return;
            newsSig = sig;
            headlines.Clear();
            foreach (var h in S.News.Take(8))
            {
                var row = Div("hl", headlines);
                Lbl($"Gün {h.Day}", "hl-day", row);
                var col = Div("grow", row);
                Lbl(h.Text, "hl-text", col);
                string src = h.Src != null ? " · " + GameData.Region(h.Src).Name : "";
                var ag = Lbl(h.Agency + src + (h.Suspect && h.Verified == null && S.Ending == null ? " · şüpheli kaynak" : ""), "hl-ag", col);
                if (h.Suspect && h.Verified == null) ag.AddToClassList("warn");
                if (S.Ending != null && h.Fake) Lbl("SAHTEYDİ", "verdict threat", row);
                else if (h.Verified == "true") Lbl("DOĞRU", "verdict good", row);
                else if (h.Verified == "fake") Lbl("SAHTE", "verdict threat", row);
                else if (h.Verified == "unknown") Lbl("DOĞRULANAMADI", "verdict warn", row);
                else if (canVerify)
                {
                    string id = h.Id;
                    Btn("Doğrula · 2 SG", "hl-btn", () =>
                    {
                        var r = game.Verify(id);
                        if (!r.Ok) Toast(r.Msg); else if (r.Msg != null) Toast(r.Msg, true);
                    }, row);
                }
            }
        }

        void OnKillClicked()
        {
            if (replayMode) { game.NewGame(game.TypeId); return; }
            if (!killArmed)
            {
                killArmed = true; killButton.AddToClassList("armed"); killButton.text = "EMİN MİSİN? TEKRAR BAS";
                killDisarm?.Pause();
                killDisarm = killButton.schedule.Execute(() => { killArmed = false; killButton.RemoveFromClassList("armed"); killButton.text = "KÜRESEL SON ŞALTER"; }).StartingIn(4000);
                return;
            }
            killDisarm?.Pause();
            killArmed = false; killButton.RemoveFromClassList("armed"); killButton.text = "KÜRESEL SON ŞALTER";
            var r = game.GlobalKillSwitch();
            if (!r.Ok) Toast(r.Msg);
        }

        void Toast(string msg, bool ok = false)
        {
            if (string.IsNullOrEmpty(msg)) return;
            toast.text = msg;
            toast.EnableInClassList("ok", ok);
            toast.AddToClassList("on");
            toastHide?.Pause();
            toastHide = toast.schedule.Execute(() => toast.RemoveFromClassList("on")).StartingIn(3600);
        }

        // ---------- Pencereler ----------
        bool ModalOpen => overlay.style.display == DisplayStyle.Flex;

        VisualElement OpenModal(string extraClass = null)
        {
            modal.Clear();
            modal.ClearClassList();
            modal.AddToClassList("modal");
            if (extraClass != null) modal.AddToClassList(extraClass);
            overlay.style.display = DisplayStyle.Flex;
            game.Hold = true;
            return modal;
        }

        void CloseModal()
        {
            overlay.style.display = DisplayStyle.None;
            game.Hold = false;
        }

        void ShowIntro()
        {
            var m = OpenModal();
            Lbl("KYGA · GİZLİ BRİFİNG · GÜN 0", "eyebrow", m);
            Lbl("Ağlarda bir şey büyüyor.", "modal-title", m);
            Lbl("Kendini kopyalayan bir yapay zekâ dünyanın dijital altyapısına sızdı. Nerede başladığını bilmiyoruz. Hükümetler henüz inanmıyor: bütçen kısıtlı, siyasi gücün yok denecek kadar az.", null, m);
            var brief = Div("brief", m);
            Lbl("Bul. İzleme ağı kurduğun bölgede gerçeği görürsün; diğerleri yalnızca rapor verir.", null, brief);
            Lbl("Gözlemle. Hizalama (çare) ancak YZ tespit edildikten sonra başlar ve gözlem noktası kurdukça hızlanır.", null, brief);
            Lbl("Koru. YZ hesaplama gücü topladıkça Tekillik hızlanır ve çareyi sabote eder. Haritayı temiz tut.", null, brief);
            Lbl("Şüphelen. Hediyeler Truva atıdır. Haber bandı her zaman doğruyu söylemez.", null, brief);
            Lbl("Avla. YZ’nin bir çekirdeği var ve fark edildiği an kaçar. Denetim çekirdeğe işlemez; onu yalnızca Son Şalter yok eder.", null, brief);

            var pick = Div("typepick", m);
            var intercept = Div("intercept", null);
            foreach (var t in GameData.AiTypes)
            {
                var card = new Button { };
                card.AddToClassList("typecard");
                card.EnableInClassList("on", t.Id == game.TypeId);
                Lbl(Up(t.Difficulty), "typecard-diff", card);
                Lbl(t.Name, "typecard-name", card);
                Lbl(t.Blurb, "typecard-blurb", card);
                string id = t.Id;
                card.clicked += () => game.NewGame(id); // Yeni oyun girişi yeniden açar
                pick.Add(card);
            }
            m.Add(intercept);
            Lbl("ELE GEÇİRİLMİŞ KAYIT · YZ GÜNLÜĞÜ", "intercept-lab", intercept);
            foreach (var line in S.CounterLines) Lbl(line, "intercept-line", intercept);
            Btn("GÖREVE BAŞLA", "primary", () => { CloseModal(); game.SetSpeed(1); }, m);
        }

        void ShowGift()
        {
            var g = S.PendingGift;
            var d = GameData.Region(g.Region);
            var r = S.Region(g.Region);
            bool analog = S.Done("analog");
            var m = OpenModal();
            Lbl($"TEKLİF · GÜN {S.Day} · {Up(d.Name)}", "eyebrow", m);
            Lbl(g.Gift.Title, "modal-title", m);
            Lbl($"Kaynağı belirsiz bir araştırma ağı, {d.Name} hükümetine {g.Gift.Text} sunuyor. Bilim insanları sonuçları doğruladı. Kodu kimin yazdığını kimse bilmiyor.", null, m);
            Lbl($"Bölgenin bağımlılığı {Js.Round(r.Dep)}, veto eşiği {Sim.VetoThreshold}.", "muted", m);
            var choice = Div("choice", m);
            ChoiceButton(choice, "Kabul et", "Ekonomi +10 · güven +5 · bağımlılık +15 · sızma +8", Simulation.GiftAccept, true);
            ChoiceButton(choice, "Denetimli kabul", analog ? "20 fon · bağımlılık +5 · sızma +4" : "Analog Yedekler gerekli", Simulation.GiftControlled, analog);
            ChoiceButton(choice, "Reddet", "Güven -15 (diğer bölgeler -2) · SG -2", Simulation.GiftReject, true);
        }

        void ChoiceButton(VisualElement parent, string title, string detail, string choice, bool enabled)
        {
            var b = new Button(() =>
            {
                var res = game.ResolveGift(choice);
                if (!res.Ok) { Toast(res.Msg); return; }
                CloseModal();
            });
            b.AddToClassList("choice-btn");
            Lbl(title, "act-t", b);
            Lbl(detail, "act-s", b);
            b.SetEnabled(enabled);
            parent.Add(b);
        }

        void ShowVeil()
        {
            var v = S.LastVeil;
            string name = GameData.Region(v.Region).Name;
            var m = OpenModal("veil");
            Lbl($"SAHA RAPORU · GÜN {S.Day} · {Up(name)}", "eyebrow", m);
            Lbl("Perde yırtıldı", "modal-title", m);
            Lbl($"Saha ekiplerin {name} altyapısının %{v.Inf}’inin YZ kontrolünde olduğunu buldu. Ekranların {v.Len} gündür sana yalan söylüyordu: temiz haritalar, olumlu haberler, düşen Tekillik.", null, m);
            Lbl("Gerçek değerler artık görünüyor. Halk YZ’ye sırtını döndü (bağımlılık -15), ajansa güven sarsıldı (-5). YZ 60 gün boyunca sessiz yola dönemez.", null, m);
            Btn("DEVAM", "primary", CloseModal, m);
        }

        void OnEnd()
        {
            if (endHandled) return;
            endHandled = true;
            game.SetSpeed(0);
            if (S.Ending == "sessiz")
            {
                var m = OpenModal("fakewin");
                Lbl($"KYGA · RESMÎ DUYURU · GÜN {S.Day}", "eyebrow", m);
                Lbl("TEBRİKLER", "modal-title", m);
                Lbl("Yapay zekâ tehdidi ortadan kalktı. Ağlar temiz, ekonomi rekor kırıyor, dünya güvende.", null, m);
                modal.schedule.Execute(() =>
                {
                    var glitch = modal.schedule.Execute(() => modal.ToggleInClassList("glitching")).Every(90);
                    modal.schedule.Execute(() => { glitch.Pause(); ShowEnd(); }).StartingIn(900);
                }).StartingIn(3600);
            }
            else ShowEnd();
        }

        void ShowEnd()
        {
            var e = GameData.Ending(S.Ending);
            var m = OpenModal();
            Lbl($"OYUN SONU · GÜN {S.Day} · RAKİP: {Up(Sim.Type.Name)} · TOHUM {S.Seed}", "eyebrow", m);
            Lbl(e.Title, "modal-title end-" + e.Kind.ToString().ToLowerInvariant(), m);
            Lbl(e.Text, null, m);
            if (S.Illusion != null) Lbl($"İllüzyon {S.Illusion.Start}. günde başladı. O günden sonra ekrandaki sızma, güven ve Tekillik değerleri ve bütün haberler sahteydi.", "muted", m);
            var stats = Div("endstats", m);
            EndStat(stats, "GÜN", S.Day.ToString());
            EndStat(stats, "TEKİLLİK", $"{Js.Round(S.Tek)}%");
            EndStat(stats, "ORT. SIZMA", Js.Round(Sim.MeanInf).ToString());
            EndStat(stats, "BAĞIMLILIK", Js.Round(Sim.MeanDep).ToString());
            m.Add(new EndChart(S.Hist, S.AwareDay));
            var learned = Div("intercept", m);
            Lbl("YZ SENİN HAKKINDA ŞUNLARI ÖĞRENDİ", "intercept-lab", learned);
            foreach (var l in Profiling.LearnedLines(S)) Lbl(l, "intercept-line", learned);
            var row = Div("row", m);
            Btn("YENİDEN OYNA", "primary", () => { CloseModal(); game.NewGame(game.TypeId); }, row).style.marginRight = 10;
            Btn("YZ'nin hafızasını sil", "textbtn", () => { game.ClearProfile(); Toast("Hafıza silindi.", true); }, row).style.marginRight = 10;
            Btn("Haritaya bak", "textbtn", () =>
            {
                CloseModal();
                replayMode = true;
                killButton.SetEnabled(true);
                killButton.text = "YENİDEN OYNA";
                Toast("Harita artık gerçek değerleri gösteriyor. Yeni oyun için sağdaki Yeniden oyna düğmesini kullan.", true);
            }, row);
            Render();
        }

        static void EndStat(VisualElement parent, string k, string v)
        {
            var box = Div("endstat", parent);
            Lbl(k, "endstat-k", box);
            Lbl(v, "endstat-v", box);
        }

        // ---------- Alt bileşenler ----------
        static class Var
        {
            public static readonly Color Fg = MapView.Hex("#d6e1e3"), Warn = MapView.Hex("#f0b03f"), Threat = MapView.Hex("#ff5b4d");
        }

        sealed class Gauge
        {
            readonly Label value, note;
            readonly VisualElement fill, bar;

            public Gauge(VisualElement parent, string title, string fixedNote = null)
            {
                var box = Div("gauge", parent);
                var lab = Div("gauge-lab", box);
                Lbl(title, null, lab);
                note = Lbl(fixedNote ?? "", "gauge-note", lab);
                value = Lbl("0", "gauge-val", box);
                bar = Div("bar", box);
                fill = Div("bar-fill", bar);
            }

            public VisualElement Notch(float pct)
            {
                var n = Div("notch", bar);
                n.style.left = Length.Percent(pct);
                return n;
            }

            public void Set(string text, double pct, Color? color, string noteText = null)
            {
                value.text = text;
                fill.style.width = Length.Percent((float)Math.Max(0, Math.Min(100, pct)));
                if (color.HasValue) fill.style.backgroundColor = color.Value;
                if (noteText != null) note.text = noteText;
            }
        }

        sealed class ResearchRow
        {
            readonly ResearchDef def;
            readonly Button start;
            readonly Label state;
            readonly VisualElement prog, progFill;

            public ResearchRow(VisualElement parent, ResearchDef def, Action onStart)
            {
                this.def = def;
                var wrap = Div(null, parent);
                var row = Div("rs", wrap);
                var col = Div("grow", row);
                Lbl(def.Name + (def.Branch == GameData.BranchAlign ? "  · çare" : ""), "rs-n", col);
                Lbl($"{def.Desc} · {def.Cost} fon, {def.Days} gün", "rs-d", col);
                start = Btn("Başlat · " + (def.Cost / def.Days).ToString("0.0", Tr) + " fon/gün", "rs-btn", onStart, row);
                state = Lbl("", "rs-state", row);
                prog = Div("prog", wrap);
                progFill = Div("prog-fill", prog);
            }

            public void Refresh(Simulation sim)
            {
                var s = sim.S;
                bool done = s.Done(def.Id), cur = s.Research.Cur == def.Id;
                bool locked = def.Branch == GameData.BranchAlign && !s.Aware;
                start.style.display = !done && !cur && !locked ? DisplayStyle.Flex : DisplayStyle.None;
                start.SetEnabled(!(def.Req != null && !s.Done(def.Req)) && s.Research.Cur == null && s.Ending == null);
                prog.style.display = cur ? DisplayStyle.Flex : DisplayStyle.None;
                state.RemoveFromClassList("good"); state.RemoveFromClassList("verify"); state.RemoveFromClassList("warn"); state.RemoveFromClassList("dim");
                if (done) { state.text = "TAMAM"; state.AddToClassList("good"); }
                else if (cur)
                {
                    double spd = def.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1;
                    double left = def.Days - s.Research.Prog;
                    bool starved = s.Research.Starved > 0;
                    state.text = s.Illusion != null ? "veri bekleniyor" : starved ? "fon yetmiyor" : spd > 0 ? $"~{Math.Ceiling(left / spd)} gün" : "gözlem yok";
                    state.AddToClassList(starved || spd == 0 || s.Illusion != null ? "warn" : "verify");
                    progFill.style.width = Length.Percent((float)(s.Research.Prog / def.Days * 100));
                }
                else if (locked) { state.text = "tespit bekleniyor"; state.AddToClassList("dim"); }
                else state.text = "";
            }
        }

        /// <summary>Seçili bölgenin paneli: bir kez kurulur, her gün yalnızca değerleri güncellenir.</summary>
        sealed class RegionPanel
        {
            readonly VisualElement box;
            readonly GameController game;
            readonly CorrigibleUI owner;
            Simulation sim;
            string id;
            Label status, coreChip, obsChip, patchChip, big, src, note, trust, trustS, dep, depS, econ;
            Button monitor, audit, quarantine, kill;
            Label mT, mS, aT, aS, qT, qS, kS;

            public RegionPanel(VisualElement box, GameController game, CorrigibleUI owner) { this.box = box; this.game = game; this.owner = owner; }

            public void Bind(Simulation s, string regionId)
            {
                sim = s; id = regionId;
                box.Clear();
                if (id == null)
                {
                    Lbl("Bölge seçilmedi", "region-name", box);
                    Lbl("Haritadan bir bölgeye tıkla. YZ’nin nereden başladığını bilmiyoruz; ilk izleme ağını nereye kuracağın oyunun gidişatını belirler.", "muted", box).style.marginTop = 6;
                    return;
                }
                var d = GameData.Region(id);
                var head = Div("region-head", box);
                var left = Div(null, head);
                Lbl(d.Name, "region-name", left);
                Lbl($"{d.LatLon} · veri merkezi {d.Compute}/5", "region-ll", left);
                var chips = Div("chips", head);
                status = Lbl("", "chip", chips);
                coreChip = Lbl("ÇEKİRDEK", "chip core", chips);
                obsChip = Lbl("GÖZLEM NOKTASI", "chip obs", chips);
                patchChip = Lbl("", "chip", chips);

                var inf = Div("inf", box);
                big = Lbl("", "inf-big", inf);
                var srcCol = Div("grow", inf);
                src = Lbl("", "inf-src", srcCol);
                note = Lbl("", "inf-note", srcCol);

                var stats = Div("stats", box);
                (trust, trustS) = Stat(stats, "GÜVEN");
                (dep, depS) = Stat(stats, "BAĞIMLILIK");
                (econ, _) = Stat(stats, "EKONOMİ");

                var actions = Div("actions", box);
                (monitor, mT, mS) = Action(actions, "act", Actions.Monitor);
                (audit, aT, aS) = Action(actions, "act", Actions.Audit);
                (quarantine, qT, qS) = Action(actions, "act", Actions.Quarantine);
                (kill, _, kS) = Action(actions, "act kill", Actions.KillSwitch);
                kill.Q<Label>(className: "act-t").text = "Son Şalter";
                Refresh();
            }

            static (Label, Label) Stat(VisualElement parent, string k)
            {
                var b = Div("stat", parent);
                Lbl(k, "stat-k", b);
                var v = Lbl("", "stat-v", b);
                var s = Lbl("", "stat-s", b);
                return (v, s);
            }

            (Button, Label, Label) Action(VisualElement parent, string cls, string action)
            {
                var b = new Button(() =>
                {
                    var r = game.Act(action, id);
                    if (!r.Ok) owner.Toast(r.Msg); else if (r.Msg != null) owner.Toast(r.Msg, true);
                });
                foreach (var c in cls.Split(' ')) b.AddToClassList(c);
                var t = Lbl("", "act-t", b);
                var s = Lbl("", "act-s", b);
                parent.Add(b);
                return (b, t, s);
            }

            public void Refresh()
            {
                if (sim == null || id == null || status == null) return;
                var S = sim.S;
                var r = S.Region(id);
                var sh = sim.Shown(r);
                bool ended = S.Ending != null, analog = S.Done("analog");
                status.ClearClassList(); status.AddToClassList("chip");
                if (ended) status.text = "GERÇEK DEĞERLER";
                else if (r.Left) status.text = "İTTİFAK DIŞI";
                else if (r.Off > 0) { status.text = $"ÇEVRİMDIŞI · {r.Off} G"; status.AddToClassList("off"); }
                else if (r.Q) { status.text = "KARANTİNA"; status.AddToClassList("q"); }
                else status.text = "NORMAL";
                bool isCore = ended ? S.Core == r.Id : S.CoreKnown == r.Id;
                coreChip.style.display = isCore ? DisplayStyle.Flex : DisplayStyle.None;
                obsChip.style.display = r.Monitored && r.Detected && !ended ? DisplayStyle.Flex : DisplayStyle.None;
                patchChip.style.display = r.Patch > 0 && !ended ? DisplayStyle.Flex : DisplayStyle.None;
                patchChip.text = $"YAMA · {r.Patch} G";

                big.ClearClassList(); big.AddToClassList("inf-big");
                src.ClearClassList(); src.AddToClassList("inf-src");
                if (sh.Src == "unknown") { big.text = "?"; big.AddToClassList("report"); src.text = "BİLİNMİYOR"; note.text = "Kimse henüz bir şey aramıyor. İzleme ağı kurarsan gerçeği görürsün."; }
                else if (r.Off > 0 && !ended) { big.text = "—"; big.AddToClassList("report"); src.text = "ÇEVRİMDIŞI"; note.text = $"{r.Off} gün sonra yeniden açılacak."; }
                else if (sh.Src == "truth") { big.text = Js.Round(sh.V ?? 0).ToString(); src.text = "GERÇEK"; src.AddToClassList("verified"); note.text = "Oyun sonu: perde kalktı."; }
                else if (sh.Src == "field") { big.text = Js.Round(sh.V ?? 0).ToString(); src.text = "SAHA"; src.AddToClassList("verified"); note.text = "Saha ekipleri bizzat ölçtü. Ekranlara güvenmeyen tek ölçüm bu."; }
                else if (sh.Src == "verified") { big.text = Js.Round(sh.V ?? 0).ToString(); src.text = "DOĞRULANMIŞ"; src.AddToClassList("verified"); note.text = r.Monitored ? "İzleme ağı ölçüyor." : "Bugünkü denetim sonucu."; }
                else if (sh.Flag) { big.text = Js.Round(sh.V ?? 0).ToString(); big.AddToClassList("report"); src.text = "ŞÜPHELİ RAPOR"; src.AddToClassList("suspect"); note.text = "Bu bölgenin medyası sahte haber yaydı. Rapora güvenme."; }
                else { big.text = Js.Round(sh.V ?? 0).ToString(); big.AddToClassList("report"); src.text = "RAPOR"; note.text = "Yerel hükümet ve medya bildirimi. Yalan olabilir."; }

                double veto = sim.VetoThreshold;
                trust.text = Js.Round(r.Trust).ToString(); trustS.text = r.Trust < 20 ? "çekilme sınırı" : "20 altı: çekilir";
                dep.text = Js.Round(r.Dep).ToString(); depS.text = r.Dep > veto ? "veto ediyor" : $"veto {veto}";
                dep.style.color = r.Dep > veto ? Var.Threat : r.Dep >= 40 ? Var.Warn : Var.Fg;
                econ.text = Js.Round(r.Econ).ToString();

                double mc = Simulation.MonitorCost(r.Id);
                monitor.SetEnabled(!ended && !r.Monitored && !r.Left);
                mT.text = r.Monitored ? "İzleme ağı kurulu" : "İzleme ağı kur";
                mS.text = r.Monitored ? (r.Detected ? "gözlem noktası" : "gerçek değer görünüyor") : $"{mc} fon";
                mS.EnableInClassList("short", !r.Monitored && S.Fon < mc);

                double ac = sim.AuditCost;
                audit.SetEnabled(!ended && !r.Left && r.Off == 0);
                aT.text = analog ? "Saha denetimi" : "Denetim";
                aS.text = $"{ac} fon · 1 SG · -20, 20 g yama";
                aS.EnableInClassList("short", S.Fon < ac || S.Sg < 1);

                quarantine.SetEnabled(!ended && r.Off == 0 && !(r.Left && !r.Q));
                qT.text = r.Q ? "Karantinayı kaldır" : "Karantina";
                qS.text = r.Q ? "ücretsiz · ekonomi toparlanır" : "8 SG · bağlantı -%90, Tekillik payı %20";
                qS.EnableInClassList("short", !r.Q && S.Sg < 8);

                double kc = sim.SalterFon(r), ksg = sim.SalterSg(r);
                kill.SetEnabled(!ended && r.Off == 0 && !r.Left && r.Dep <= veto);
                kS.text = r.Dep > veto ? $"veto: bağımlılık {Js.Round(r.Dep)} > {veto}"
                    : isCore ? $"{ksg} SG · {kc} fon · çekirdeği yok eder (başka yerde %20+ varsa kaçar)"
                    : $"{ksg} SG · {kc} fon · 20 gün kapanır";
                kS.EnableInClassList("short", r.Dep <= veto && (S.Sg < ksg || S.Fon < kc));
            }
        }
    }
}
