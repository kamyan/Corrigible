using System;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using Corrigible.Game.Tutorial;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Eğitimin masadaki yürütücüsü (F8.4). Eski <c>TutorialController</c>'ın kurallarıyla çalışır: aynı içerik
    /// (<see cref="TutorialSequence"/>), aynı koşullar (<see cref="TutorialConditions"/>), aynı kalıcı anahtarlar (bitti,
    /// görülen ipuçları); eski oyunla ilerleme ortaktır. Rehberli adımlar sırayla, ipuçları bir mekanik ilk kez çıkınca
    /// bir kez. Gösterilecek notu <see cref="DeskBridgeState.Tutorial"/>'a yazar; çizim <see cref="DeskTutorialView"/>'ın işi.
    /// </summary>
    public sealed class DeskTutorialRunner : MonoBehaviour
    {
        const string DoneKey = "corrigible.tutorialDone";
        const string TipsKey = "corrigible.tipsSeen";

        [SerializeField] TutorialSequence sequence;
        [SerializeField] SimDeskDataSource source;
        [Tooltip("Hedeflerin masada bulunup bulunmadığını söyler (pencere kapalı, dosya açılmamış…).")]
        [SerializeField] DeskTutorialView view;

        /// <summary>
        /// Eski arayüze göre yazılmış iki metnin masa karşılığı (asset'e dokunulmaz; eski oyun kendi metnini gösterir).
        /// Anahtar asset'teki Türkçe kaynak metin.
        /// </summary>
        static readonly Dictionary<string, string> DeskWording = new Dictionary<string, string>
        {
            ["İzleme ağı ve denetim sahada yapılır. Ekiplerin Batı Avrupa’daki karargâhta.\n\nBölge panelindeki <b>Saha</b> bölümünden bir ekibi seçtiğin bölgeye gönder. Yolculuk 1–5 gün sürer."] =
                Loc.N("İzleme ağı ve denetim sahada yapılır. Ekiplerin Batı Avrupa’daki karargâhta.\n\nBölge dosyasındaki <b>SAHA</b> bölümünde hazır her ekibin fişi var: göndereceğin ekibe bas (B en yakınını gönderir). Yolculuk 1–5 gün sürer."),
            ["YZ’nin doğrulandığı bir bölgede boşta ekibin var. Bölge panelindeki <b>Saha operasyonu</b> ile binaya girebilirsin: 8×8 alanda sıra tabanlı bir savaş.\n\nHedef dolabı kırarsan bölgenin sızması 40 düşer; bina çekirdeğinse YZ’nin çekirdeği yok olur."] =
                Loc.N("YZ’nin doğrulandığı bir bölgede boşta ekibin var. Bölge dosyasındaki <b>Saha operasyonu: baskın</b> (B) önce operasyon emrini açar; onaylarsan taktik haritada binaya girersin: 8×8 alanda sıra tabanlı bir savaş.\n\nHedef dolabı kırarsan bölgenin sızması 40 düşer; bina çekirdeğinse YZ’nin çekirdeği yok olur."),
        };

        /// <summary>Masa karşılığı olan asset metinleri (test: asset değişirse karşılık sessizce düşmesin).</summary>
        public static IEnumerable<string> RewordedSources => DeskWording.Keys;

        HashSet<string> tipsSeen;
        int step = -1;          // -1: rehberli eğitim çalışmıyor
        int hidden = -1;        // “Anladım” ile gizlenen bekleme adımı: koşulu sağlanınca sıradaki adımla eğitim geri gelir
        TutorialTip tip;        // sırada bekleyen ipucu
        object shown;           // en son yazılan adım/ipucu (değişince VM yeniden kurulur)
        DeskLang shownLang;
        bool subscribed;

        public bool Running => step >= 0;
        DeskBridgeState St => source.State;

        void Awake() => tipsSeen = new HashSet<string>(PlayerPrefs.GetString(TipsKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

        void OnEnable()
        {
            if (source?.Game == null) return;
            source.Game.NewGameStarted += Abort;
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed) return;
            source.Game.NewGameStarted -= Abort;
            subscribed = false;
        }

        // ---------- Komutlar (SimDeskCommands.Run "Tutorial:…") ----------
        /// <summary>Rehberli eğitimi baştan başlatır (giriş dosyasında “Eğitimle başla”).</summary>
        public void Begin() { step = 0; hidden = -1; tip = null; }

        /// <summary>İleri; ipucunda “Anladım”.</summary>
        public void Next()
        {
            if (tip != null && ReferenceEquals(shown, tip)) { MarkSeen(tip); tip = null; return; }
            if (Running) Advance();
        }

        /// <summary>
        /// Oyunu bekleyen adımda “Anladım” (YZ tespit edilene, gözlem noktası çıkana kadar): not ve karartma kalkar; koşul
        /// sağlanınca sıradaki adım her zamanki gibi gelir.
        /// </summary>
        public void Hide()
        {
            if (Running && shown is TutorialStep s && ReferenceEquals(s, sequence.steps[step]) && WaitsOnGame(s)) hidden = step;
        }

        /// <summary>Oyuncunun kendi eylemiyle sağlanan koşullar: adım ne yapılacağını gösterir, gizlenmez.</summary>
        static readonly HashSet<TutorialCondition> PlayerActions = new HashSet<TutorialCondition>
        {
            TutorialCondition.RegionSelected, TutorialCondition.TeamDispatched, TutorialCondition.TimeRunning,
            TutorialCondition.MonitorBuilt, TutorialCondition.AlignmentStarted, TutorialCondition.AuditUsed,
        };

        /// <summary>Adım oyuncunun elinde olmayan bir şeyi mi bekliyor (YZ'nin tespiti, gözlem noktası…).</summary>
        public static bool WaitsOnGame(TutorialStep s) => s.completeWhen != TutorialCondition.NextButton && !PlayerActions.Contains(s.completeWhen);

        /// <summary>Eğitimi atla (ipucunda: kapat).</summary>
        public void Skip()
        {
            if (tip != null && ReferenceEquals(shown, tip)) { MarkSeen(tip); tip = null; return; }
            if (Running) Finish();
        }

        /// <summary>“Baştan”: rehberli eğitim ilk adımdan (koşulu sağlanmış adımlar kendiliğinden geçilir).</summary>
        public void Restart() { if (Running) { step = 0; hidden = -1; } }

        /// <summary>
        /// Rehber'deki “Eğitimi baştan oynat” (eski Rehber sekmesindeki gibi): bitti işareti ve görülen ipuçları silinir,
        /// en kolay rakiple yeni oyun açılır; giriş dosyası “Eğitimle başla”yı yeniden önerir.
        /// </summary>
        public void Replay()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.DeleteKey(TipsKey);
            PlayerPrefs.Save();
            tipsSeen.Clear();
            source.Game.NewGame("solucan");
        }

        void Abort() { step = -1; hidden = -1; tip = null; }

        void Finish()
        {
            step = -1;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            var S = source.Game.Sim.S;
            St.Push(Loc.T("Eğitim tamamlandı. Kurallar her zaman Rehber sekmesinde."), S.Day, "ok", source.Builder.TimeNow);
            source.MarkDirty();
        }

        void Advance()
        {
            step++;
            if (step >= sequence.steps.Count) Finish();
        }

        void MarkSeen(TutorialTip t)
        {
            tipsSeen.Add(t.id);
            PlayerPrefs.SetString(TipsKey, string.Join(",", tipsSeen));
            PlayerPrefs.Save();
        }

        // ---------- Koşullar ----------
        bool Check(TutorialCondition c)
        {
            // Seçim masanın görünüm durumunda; diğer koşullar oyunun kendi değerlendiricisinden (eski masa yalnız seçimi okur)
            if (c == TutorialCondition.RegionSelected) return St.Selected != null;
            return TutorialConditions.Check(c, source.Game, null);
        }

        /// <summary>Masanın kâğıtlarından biri (giriş, kampanya, belge, olay, oyun sonu…) açık mı.</summary>
        bool PaperOpen(WorldState S) =>
            St.IntroOpen || St.CampaignOpen || St.FakeWin || St.SettingsOpen || St.VeilOpen || St.RaidBrief != null ||
            S.RaidLog.Count > St.RaidLogSeen || S.PendingEvent != null || S.PendingGift != null || S.Ending != null;

        /// <summary>Kâğıt açıkken yalnızca o kâğıdı (hediye teklifi) hedefleyen içerik gösterilir.</summary>
        bool Available(TutorialTarget t, WorldState S)
        {
            if (t == TutorialTarget.GiftModal) return S.PendingGift != null && view != null && view.TargetAvailable(t.ToString());
            if (PaperOpen(S)) return false;
            return t == TutorialTarget.None || (view != null && view.TargetAvailable(t.ToString()));
        }

        // ---------- Akış ----------
        void Update()
        {
            var game = source != null ? source.Game : null;
            if (game?.Sim == null || sequence == null) return;
            var S = game.Sim.S;
            if (St.TutorialRequested) { St.TutorialRequested = false; Begin(); }

            object item = null;
            bool pause = false;
            // Taktik harita açıkken eğitim görünmez
            if (S.Raid == null && !RaidLauncher.IsRaidLoaded)
            {
                // Sırada bir ipucu varsa önce o kapanır (eski denetleyiciyle aynı)
                if (Running && tip == null)
                {
                    var st = sequence.steps[step];
                    if (st.completeWhen != TutorialCondition.NextButton && Check(st.completeWhen)) Advance();
                    else if (hidden != step && Check(st.showWhen) && Available(st.target, S)) { item = st; pause = st.pauseGame; }
                }
                // İpucu: rehberli adım görünmüyorsa, ilk kez ortaya çıkan mekanik için
                if (item == null)
                {
                    if (tip == null)
                        foreach (var t in sequence.tips)
                            if (!tipsSeen.Contains(t.id) && Check(t.trigger) && Available(t.target, S)) { tip = t; break; }
                    if (tip != null && Available(tip.target, S)) { item = tip; pause = tip.pauseGame; }
                }
            }

            game.TutorialHold = item != null && pause;
            St.SuppressAdvisor = Running;
            var lang = DeskLoc.Instance.Lang;
            if (ReferenceEquals(item, shown) && lang == shownLang) return;
            shown = item; shownLang = lang;
            St.Tutorial = item is TutorialStep s ? StepVM(s) : item is TutorialTip tp ? TipVM(tp) : null;
            source.MarkDirty();
        }

        void OnDestroy() { if (source?.Game != null) source.Game.TutorialHold = false; }

        static string Body(string body) => Loc.T(DeskWording.TryGetValue(body, out var desk) ? desk : body);

        TutorialVM StepVM(TutorialStep s) => new TutorialVM
        {
            Kind = "step", Step = step + 1, Steps = sequence.steps.Count,
            Counter = Loc.F("EĞİTİM · {0}/{1}", step + 1, sequence.steps.Count),
            Title = Loc.T(s.title), Body = Body(s.body),
            Target = s.target == TutorialTarget.None ? null : s.target.ToString(),
            // Eski sağ panel sekmeleri: 0 Araştırma, 1 YZ günlüğü, 2 Rehber
            OpenWindow = s.openTab switch { 0 => (int)WindowId.Research, 1 => (int)WindowId.AiLog, 2 => (int)WindowId.Guide, _ => -1 },
            // Oyunu bekleyen adım: “Anladım” notu ve karartmayı kaldırır, koşul sağlanınca eğitim sıradaki adımla döner
            Next = s.completeWhen == TutorialCondition.NextButton
                ? new CommandVM { Label = Loc.T("İleri"), Kind = "ink", Key = "Enter", Command = "Tutorial:Next" }
                : WaitsOnGame(s) ? new CommandVM { Label = Loc.T("Anladım"), Kind = "ink", Key = "Enter", Command = "Tutorial:Hide" } : null,
            Restart = new CommandVM { Label = Loc.T("Baştan"), Kind = "link", Command = "Tutorial:Restart" },
            Skip = new CommandVM { Label = Loc.T("Eğitimi atla"), Kind = "link", Command = "Tutorial:Skip" },
        };

        static TutorialVM TipVM(TutorialTip t) => new TutorialVM
        {
            Kind = "tip", Counter = Loc.T("İPUCU"), Title = Loc.T(t.title), Body = Body(t.body),
            Target = t.target == TutorialTarget.None ? null : t.target.ToString(),
            Next = new CommandVM { Label = "×", Kind = "link", Key = "Enter", Command = "Tutorial:Next" },
        };
    }
}
