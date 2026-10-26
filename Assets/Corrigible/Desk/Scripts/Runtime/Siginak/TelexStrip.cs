using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Teleks kadraj dışındayken aynı satırın ekrandaki karşılığı (K4 notu): alttan 0,15 s'de kayan kâğıt şerit
    /// (820 × 52, iki uçta delik sırası, CP 700 17), 3 s kalır, 0,12 s'de iner. Sırada satır varsa gösterilen satır en az
    /// motion.telexStripMinHoldS kalır, sonra şerit inmeden yerini sıradakine bırakır (kâğıt ilerler; kuyruk birikmez).
    /// </summary>
    public sealed class TelexStrip : MonoBehaviour
    {
        [SerializeField] RectTransform strip;
        [SerializeField] TMP_Text text;
        [SerializeField] float bottomMargin = 18;

        readonly Queue<TelexLine> queue = new Queue<TelexLine>();
        float since;
        bool showing;

        public bool Showing => showing;

        public void Configure(RectTransform stripRoot, TMP_Text label) { strip = stripRoot; text = label; }

        public void Enqueue(TelexLine line)
        {
            queue.Enqueue(line);
            if (queue.Count > 4) queue.Dequeue();
        }

        /// <summary>Yeni oyun / kayıttan devam: bekleyenler atılır, şerit iner.</summary>
        public void Clear()
        {
            queue.Clear();
            showing = false;
            if (strip != null && strip.gameObject.activeSelf) strip.gameObject.SetActive(false);
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            float inS = t.Number("motion.telexStripInS", 0.15f), hold = t.Number("motion.telexStripHoldS", 3), outS = t.Number("motion.telexStripOutS", 0.12f);
            if (showing && queue.Count > 0 && Time.unscaledTime - since >= inS + t.Number("motion.telexStripMinHoldS", 1.2f))
            {
                // Sırada satır var: şerit yukarıda kalır, satır değişir
                Fill(queue.Dequeue());
                since = Time.unscaledTime - inS;
            }
            if (!showing)
            {
                if (queue.Count == 0) { if (strip.gameObject.activeSelf) strip.gameObject.SetActive(false); return; }
                Fill(queue.Dequeue());
                showing = true;
                since = Time.unscaledTime;
                strip.gameObject.SetActive(true);
            }
            float age = Time.unscaledTime - since, h = strip.rect.height + bottomMargin;
            float k = age < inS ? 1 - Mathf.Pow(1 - age / inS, 3)
                : age < inS + hold ? 1
                : 1 - Mathf.Clamp01((age - inS - hold) / outS);
            strip.anchoredPosition = new Vector2(0, bottomMargin - h * (1 - k));
            if (age >= inS + hold + outS) showing = false;
        }

        void Fill(TelexLine l)
        {
            var t = SiginakTheme.Active;
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3"));
            string status = ColorUtility.ToHtmlStringRGB(t.ColorOf(l.kind == TelexKind.Failed ? "status.threat" : "ink"));
            text.text = $"<color=#{ink3}>{l.code}</color>   <b><color=#{status}>{l.status}</color></b>   <font-weight=400>{l.message}</font-weight>";
        }
    }
}
