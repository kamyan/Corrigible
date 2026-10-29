using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Baskın geçişi (K5b notu): tam ekran Overlay — tüp camı (köşe 74/64), HAT AÇILIYOR (CP 700 76, 7 px hayalet),
    /// SİNYAL 10 kutu + yüzde, hedef ve saha masası satırı, arkada baskın kamerasıyla birebir 8 × 8 izometrik ızgara (α .2),
    /// statik (α .55, her kare rastgele ofset), 1,2 s'de yuvarlanan bant, tarama çizgisi, köşe kararması.
    /// Zaman çizelgesi (emirden itibaren): 0,30 kamera dönüşü (son 0,25 s'de bu katman devralır) · 0,80 tüp 4 px çizgiden
    /// 0,15 s'de açılır · 0,95 statik + başlık, kutular 0,1 s arayla · 2,00 ızgara parlar, statik %20 · 2,30 sahne açılır,
    /// katman 0,2 s'de söner. Dönüş tersi (0,6 s). Boşluk diziyi atlar. Shader yok: hepsi UI katmanı.
    /// </summary>
    public sealed class CrtTransition : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform tube;
        [SerializeField] CanvasGroup content;
        [SerializeField] TMP_Text channel, code, title, ghost, sync, percent, target, cameraLine, skipKey, skipWord, footer;
        [SerializeField] Image[] boxes = Array.Empty<Image>();
        [SerializeField] Graphic[] gridLines = Array.Empty<Graphic>();
        [SerializeField] RawImage noise;
        [SerializeField] RectTransform rollBar;

        float fadeInAt = -1, fadeOutAt = -1, gridAlpha = 0.2f, gridFadeFrom, noiseAlpha = 0.55f;
        bool skip;

        public bool Playing { get; private set; }

        public void Configure(CanvasGroup root, RectTransform tubeRoot, CanvasGroup contentGroup, TMP_Text channelText, TMP_Text codeText, TMP_Text titleText,
            TMP_Text ghostText, TMP_Text syncText, TMP_Text percentText, TMP_Text targetText, TMP_Text cameraText, TMP_Text skipKeyText, TMP_Text skipText,
            TMP_Text footerText, Image[] syncBoxes, Graphic[] grid, RawImage staticLayer, RectTransform roll)
        {
            group = root; tube = tubeRoot; content = contentGroup; channel = channelText; code = codeText; title = titleText; ghost = ghostText;
            sync = syncText; percent = percentText; target = targetText; cameraLine = cameraText; skipKey = skipKeyText; skipWord = skipText;
            footer = footerText; boxes = syncBoxes; gridLines = grid; noise = staticLayer; rollBar = roll;
        }

        static SiginakTheme T => SiginakTheme.Active;

        void Show(CrtTransitionData d, string titleText)
        {
            gameObject.SetActive(true);
            channel.text = d.Channel;
            code.text = d.Code;
            title.text = ghost.text = titleText;
            sync.text = d.Sync;
            target.text = d.Target;
            cameraLine.text = d.Camera;
            skipKey.text = d.SkipKey;
            skipWord.text = d.SkipWord;
            footer.text = d.Footer;
        }

        void SetBoxes(int filled)
        {
            var on = T.ColorOf("crt.phosphor");
            for (int i = 0; i < boxes.Length; i++)
            {
                bool f = i < filled;
                // Dolu kutu fosfor dolgu; boş kutu sönük 2 px kenar (alt öğe)
                boxes[i].color = f ? on : Color.clear;
                var edge = boxes[i].transform.childCount > 0 ? boxes[i].transform.GetChild(0).GetComponent<Image>() : null;
                if (edge != null) edge.enabled = !f;
            }
            percent.text = DeskFormat.Percent(boxes.Length > 0 ? 100.0 * filled / boxes.Length : 0);
        }

        void SetGrid(float alpha)
        {
            gridAlpha = alpha;
            var c = T.ColorOf("crt.phosphor");
            c.a = alpha;
            foreach (var g in gridLines) if (g != null) g.color = c;
        }

        void SetTube(float open) => tube.localScale = new Vector3(1, Mathf.Lerp(4f / Mathf.Max(1, tube.rect.height), 1, open), 1);

        /// <summary>Kamera yaklaşması başlıyor: katman yaklaşmanın son 0,25 s'inde belirir (cam geçilir).</summary>
        public void BeginApproach(float approachSeconds)
        {
            gameObject.SetActive(true);
            Playing = true;
            skip = false;
            group.alpha = 0;
            content.alpha = 0;
            SetTube(0);
            SetBoxes(0);
            SetGrid(0);
            noise.gameObject.SetActive(false);
            fadeOutAt = -1;
            fadeInAt = Time.unscaledTime + Mathf.Max(0, approachSeconds - 0.25f);
        }

        /// <summary>Geçiş dizisi (0,80 → 2,30). Boşluk atlar.</summary>
        public IEnumerator Run(CrtTransitionData d)
        {
            Show(d, d.Title);
            Playing = true;
            fadeInAt = -1;
            group.alpha = 1;
            float open = T.NumberOf("motion.crtOpenLineS", 0.15f), box = T.NumberOf("motion.crtSyncBoxS", 0.1f);
            float total = Mathf.Max(open + box * boxes.Length, T.NumberOf("motion.crtTotalS", 2.3f) - 0.8f);
            float start = Time.unscaledTime;
            while (true)
            {
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) skip = true;
                float e = skip ? total : Time.unscaledTime - start;
                // 0,80: tüp açılır · 0,95: statik + başlık · kutular · 2,00: ızgara parlar, statik %20
                SetTube(Mathf.Clamp01(e / open));
                bool lit = e >= open;
                content.alpha = lit ? 1 : 0;
                if (noise.gameObject.activeSelf != lit) noise.gameObject.SetActive(lit);
                SetBoxes(Mathf.Clamp(Mathf.FloorToInt((e - open) / box) + 1, 0, boxes.Length));
                bool bright = e >= total - 0.3f;
                SetGrid(bright ? 0.34f : lit ? 0.2f : 0);
                noiseAlpha = bright ? 0.55f * 0.2f : 0.55f;
                if (e >= total) break;
                yield return null;
            }
        }

        /// <summary>Baskın sahnesi açıldı: ızgara ve katman 0,2 s'de söner.</summary>
        public void SceneLoaded()
        {
            fadeOutAt = Time.unscaledTime;
            gridFadeFrom = gridAlpha;
        }

        /// <summary>Dönüş (0,6 s): kesildi yazısı, tüp çizgiye kapanır, katman söner.</summary>
        public IEnumerator Return(CrtTransitionData d)
        {
            Show(d, d.CutTitle);
            cameraLine.text = d.CutSub;
            Playing = true;
            fadeInAt = fadeOutAt = -1;
            group.alpha = 1;
            content.alpha = 1;
            SetBoxes(0);
            SetGrid(0);
            noise.gameObject.SetActive(true);
            noiseAlpha = 0.55f;
            float seconds = T.NumberOf("motion.crtReturnS", 0.6f), start = Time.unscaledTime;
            while (Time.unscaledTime - start < seconds)
            {
                float p = (Time.unscaledTime - start) / seconds;
                SetTube(1 - p);
                group.alpha = 1 - p * p;
                yield return null;
            }
            Hide();
        }

        public void Hide()
        {
            Playing = false;
            fadeInAt = fadeOutAt = -1;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (fadeInAt >= 0 && now >= fadeInAt) group.alpha = Mathf.Clamp01((now - fadeInAt) / 0.25f);
            if (fadeOutAt >= 0)
            {
                float p = Mathf.Clamp01((now - fadeOutAt) / 0.2f);
                group.alpha = 1 - p;
                SetGrid(gridFadeFrom * (1 - p));
                if (p >= 1) { Hide(); return; }
            }
            if (noise != null && noise.gameObject.activeSelf)
            {
                var r = noise.uvRect;
                noise.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, r.width, r.height);
                var c = noise.color;
                c.a = noiseAlpha;
                noise.color = c;
            }
            if (rollBar != null)
            {
                // Yuvarlanan parlak bant 1,2 s'de yukarıdan aşağı
                var parent = (RectTransform)rollBar.parent;
                float h = parent.rect.height + rollBar.rect.height;
                rollBar.anchoredPosition = new Vector2(0, rollBar.rect.height * 0.5f - Mathf.Repeat(now / 1.2f, 1) * h);
            }
        }
    }
}
