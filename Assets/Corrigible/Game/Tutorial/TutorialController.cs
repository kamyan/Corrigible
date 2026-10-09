using System;
using System.Collections.Generic;
using Corrigible.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.Tutorial
{
    /// <summary>
    /// Eğitim katmanı (Egitim canvas'ı). İki iş yapar:
    /// 1) Rehberli ilk oyun: TutorialSequence'teki adımları sırayla gösterir; adım koşulu sağlanınca ilerler.
    /// 2) İpuçları: bir mekanik ilk kez ortaya çıktığında (hediye, çekirdek, perde…) bir kez açıklama gösterir.
    /// Hedef bölge vurgulanır, çevresi karartılır; karartma tıklamaları engellemez, oyuncu hedefle doğrudan etkileşir.
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        const string DoneKey = "corrigible.tutorialDone";
        const string TipsKey = "corrigible.tipsSeen";

        [Serializable]
        public struct TargetBinding
        {
            public TutorialTarget target;
            public RectTransform rect;
        }

        [SerializeField] TutorialSequence sequence;
        [SerializeField] GameController game;
        [SerializeField] CrisisDeskView desk;
        [SerializeField] ModalView modals;
        [SerializeField] IntroModal intro;
        [SerializeField] ToastView toast;
        [SerializeField] TargetBinding[] targets = Array.Empty<TargetBinding>();

        [Header("Katman")]
        [SerializeField] RectTransform root;
        [Tooltip("Sırasıyla üst, alt, sol, sağ karartma.")]
        [SerializeField] RectTransform[] dims = new RectTransform[4];
        [SerializeField] RectTransform frame;
        [SerializeField] RectTransform callout;
        [SerializeField] TMP_Text counter;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] StyledButton next;
        [SerializeField] StyledButton skip;
        [SerializeField] StyledButton replay;

        [Header("Yerleşim")]
        [SerializeField, Min(0)] float framePadding = 6;
        [SerializeField, Min(0)] float calloutGap = 14;
        [SerializeField, Min(0)] float screenMargin = 16;

        readonly Dictionary<TutorialTarget, RectTransform> targetMap = new Dictionary<TutorialTarget, RectTransform>();
        HashSet<string> tipsSeen;
        int step = -1;          // -1: rehberli eğitim çalışmıyor
        TutorialTip tip;        // şu an gösterilen ipucu
        bool visible;
        object presented;       // en son gösterilen adım/ipucu; yeni geldiğinde hedef görünür alana kaydırılır

        public bool Running => step >= 0;
        public static bool Completed => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        void Awake()
        {
            foreach (var t in targets) if (t.rect != null) targetMap[t.target] = t.rect;
            tipsSeen = new HashSet<string>(PlayerPrefs.GetString(TipsKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            UiUtil.OnClick(next.Button, OnNext);
            UiUtil.OnClick(skip.Button, OnSkip);
            if (replay != null) UiUtil.OnClick(replay.Button, Replay);
            intro.TutorialClicked += Begin;
            Hide();
        }

        void Start() => game.NewGameStarted += Abort;

        void OnDestroy()
        {
            if (game != null) game.NewGameStarted -= Abort;
            if (intro != null) intro.TutorialClicked -= Begin;
        }

        // ---------- Akış ----------
        /// <summary>Rehberli eğitimi baştan başlatır (giriş penceresindeki "Eğitimle başla").</summary>
        public void Begin()
        {
            step = 0;
            tip = null;
        }

        void Abort()
        {
            step = -1;
            tip = null;
            presented = null;
            Hide();
        }

        void Finish()
        {
            step = -1;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            Hide();
            toast.Show("Eğitim tamamlandı. Kurallar her zaman Rehber sekmesinde.", true);
        }

        void OnNext()
        {
            if (tip != null) { MarkSeen(tip); tip = null; Hide(); return; }
            if (Running) Advance();
        }

        void OnSkip()
        {
            if (tip != null) { MarkSeen(tip); tip = null; Hide(); return; }
            Finish();
        }

        /// <summary>Rehber sekmesindeki "Eğitimi baştan oynat": görülen ipuçlarını sıfırlar ve giriş penceresine döner.</summary>
        void Replay()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.DeleteKey(TipsKey);
            tipsSeen.Clear();
            game.NewGame("solucan");
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

        bool Check(TutorialCondition c) => TutorialConditions.Check(c, game, desk);

        void Update()
        {
            // Baskın ekranı açıkken eğitim katmanı görünmez
            if (game.Sim == null || game.Sim.S.Raid != null) { Hide(); return; }

            // Rehberli adım
            if (Running && tip == null)
            {
                var st = sequence.steps[step];
                if (st.completeWhen != TutorialCondition.NextButton && Check(st.completeWhen)) { Advance(); return; }
                bool canShow = Check(st.showWhen) && TargetAvailable(st.target);
                if (canShow)
                {
                    if (!visible && st.openTab >= 0) desk.ShowTab(st.openTab);
                    Present(st, st.title, st.body, st.target, $"EĞİTİM · {step + 1}/{sequence.steps.Count}",
                        st.completeWhen == TutorialCondition.NextButton, "Eğitimi geç", st.pauseGame);
                    return;
                }
            }

            // İpucu: rehberli adım görünmüyorsa, ilk kez ortaya çıkan mekanik için
            if (tip == null)
                foreach (var t in sequence.tips)
                    if (!tipsSeen.Contains(t.id) && Check(t.trigger) && TargetAvailable(t.target)) { tip = t; break; }
            if (tip != null)
            {
                if (!TargetAvailable(tip.target)) { Hide(); return; }
                Present(tip, tip.title, tip.body, tip.target, "İPUCU", true, null, tip.pauseGame);
                return;
            }
            Hide();
        }

        /// <summary>Pencereler (giriş, son, hediye) açıkken yalnızca o pencereyi hedefleyen içerik gösterilir.</summary>
        bool TargetAvailable(TutorialTarget t)
        {
            if (t == TutorialTarget.GiftModal) return targetMap.TryGetValue(t, out var g) && g.gameObject.activeInHierarchy;
            if (modals.IsOpen) return false;
            return t == TutorialTarget.None || (targetMap.TryGetValue(t, out var r) && r.gameObject.activeInHierarchy);
        }

        // ---------- Gösterim ----------
        void Present(object item, string titleText, string bodyText, TutorialTarget target, string counterText, bool showNext, string skipText, bool pause)
        {
            if (!visible) { root.gameObject.SetActive(true); visible = true; }
            if (!ReferenceEquals(item, presented))
            {
                presented = item;
                if (targetMap.TryGetValue(target, out var t)) ScrollIntoView(t);
            }
            counter.text = counterText;
            title.text = titleText;
            body.text = bodyText;
            UiUtil.Show(next, showNext);
            next.SetText(tip != null ? "ANLADIM" : "İLERİ");
            UiUtil.Show(skip, skipText != null);
            if (skipText != null) skip.SetText(skipText);
            game.TutorialHold = pause;
            desk.SuppressAdvisor = Running;
            Layout(target);
        }

        void Hide()
        {
            if (visible || root.gameObject.activeSelf) root.gameObject.SetActive(false);
            visible = false;
            if (game != null) game.TutorialHold = false;
            if (desk != null) desk.SuppressAdvisor = Running;
        }

        void Layout(TutorialTarget target)
        {
            var area = root.rect;
            Rect hole;
            bool hasTarget = target != TutorialTarget.None && targetMap.TryGetValue(target, out var rt);
            if (hasTarget)
            {
                hole = LocalRect(targetMap[target]);
                hole.xMin -= framePadding; hole.yMin -= framePadding; hole.xMax += framePadding; hole.yMax += framePadding;
            }
            else hole = new Rect(area.center, Vector2.zero);

            // Karartma: hedefin çevresindeki dört dikdörtgen
            Place(dims[0], Rect.MinMaxRect(area.xMin, hole.yMax, area.xMax, area.yMax));
            Place(dims[1], Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, hole.yMin));
            Place(dims[2], Rect.MinMaxRect(area.xMin, hole.yMin, hole.xMin, hole.yMax));
            Place(dims[3], Rect.MinMaxRect(hole.xMax, hole.yMin, area.xMax, hole.yMax));
            frame.gameObject.SetActive(hasTarget);
            if (hasTarget) Place(frame, hole);

            // Açıklama kutusu: hedefin altına, üstüne, sağına ya da soluna; sığmazsa ortaya
            LayoutRebuilder.ForceRebuildLayoutImmediate(callout);
            var size = callout.rect.size;
            Vector2 pos;
            if (!hasTarget) pos = area.center;
            else
            {
                float m = screenMargin, g = calloutGap;
                var below = new Vector2(hole.center.x, hole.yMin - g - size.y / 2);
                var above = new Vector2(hole.center.x, hole.yMax + g + size.y / 2);
                var right = new Vector2(hole.xMax + g + size.x / 2, hole.center.y);
                var left = new Vector2(hole.xMin - g - size.x / 2, hole.center.y);
                if (below.y - size.y / 2 >= area.yMin + m) pos = below;
                else if (above.y + size.y / 2 <= area.yMax - m) pos = above;
                else if (right.x + size.x / 2 <= area.xMax - m) pos = right;
                else if (left.x - size.x / 2 >= area.xMin + m) pos = left;
                // Hedef ekranın çoğunu kaplıyorsa (harita) kutu hedefin sol alt köşesine, içeri konur
                else pos = new Vector2(hole.xMin + m + size.x / 2, hole.yMin + m + size.y / 2);
                pos.x = Mathf.Clamp(pos.x, area.xMin + m + size.x / 2, area.xMax - m - size.x / 2);
                pos.y = Mathf.Clamp(pos.y, area.yMin + m + size.y / 2, area.yMax - m - size.y / 2);
            }
            callout.anchoredPosition = pos;
        }

        /// <summary>Hedef bir (ya da iç içe birkaç) kaydırma alanının içindeyse onu görünür alana getirir.</summary>
        static void ScrollIntoView(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            for (var sr = target.GetComponentInParent<ScrollRect>(); sr != null; sr = sr.transform.parent != null ? sr.transform.parent.GetComponentInParent<ScrollRect>() : null)
            {
                var vp = sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
                var c = new Vector3[4];
                target.GetWorldCorners(c);
                float yMin = vp.InverseTransformPoint(c[0]).y, yMax = vp.InverseTransformPoint(c[1]).y;
                var vr = vp.rect;
                const float margin = 12;
                float dy = 0;
                if (yMax - yMin > vr.height - 2 * margin || yMax > vr.yMax - margin) dy = yMax - (vr.yMax - margin);
                else if (yMin < vr.yMin + margin) dy = yMin - (vr.yMin + margin);
                if (dy != 0)
                {
                    sr.StopMovement();
                    sr.content.anchoredPosition -= new Vector2(0, dy);
                    sr.verticalNormalizedPosition = Mathf.Clamp01(sr.verticalNormalizedPosition);
                    Canvas.ForceUpdateCanvases();
                }
            }
        }

        Rect LocalRect(RectTransform t)
        {
            var c = new Vector3[4];
            t.GetWorldCorners(c);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(null, c[0]), null, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(null, c[2]), null, out var b);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static void Place(RectTransform rt, Rect r)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0, r.width), Mathf.Max(0, r.height));
            rt.anchoredPosition = r.center;
        }
    }
}
