using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Arka duvardaki KURTARILAN BÖLGELER panosu (Revizyon 2 · S1): başlık kartında sayaç (x/12), her özgür bölgeye mavi ÖZGÜR
    /// damgalı kart (ad + V.###), sıradaki hedefin yuvasında kurşun kalemle adı. Yeni özgür bölgenin kartı üstten düşer; bölge
    /// özgürlüğünü kaybederse kartı panodan kalkar. Değerleri bağlayıcı (SiginakDesk) yazar.
    /// </summary>
    public sealed class FreedRegionsBoard : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            [Tooltip("Özgür bölge kartı: kâğıt, ad, vardiya, ÖZGÜR damgası, mavi raptiye.")]
            public Transform card;
            public TMP_Text name, shift;
            [Tooltip("Sıradaki hedef: soluk kâğıt, kurşun kalemle ad, kırmızı raptiye.")]
            public GameObject next;
            public TMP_Text nextName;
            [Tooltip("Boş yuvadaki ahşap raptiye.")]
            public GameObject emptyPin;
        }

        [SerializeField] TMP_Text count;
        [Tooltip("Üzerine gelince açıklama (tıklanmaz); satırı pano yazar.")]
        [SerializeField] DeskTarget target;
        [SerializeField] Slot[] slots = Array.Empty<Slot>();
        [Tooltip("Yeni kartın üstten düşüşü (S1 notu › hareket: 0,35 s).")]
        [SerializeField, Min(0.01f)] float dropSeconds = 0.35f;
        [SerializeField, Min(0)] float dropHeight = 0.06f;

        Vector3[] rest;
        Quaternion[] restRotation;
        float[] droppedAt, fallAt;
        int shown = -1;

        static readonly string TargetLine = Corrigible.Core.Loc.N("Her özgür bölgeye bir kart; kurşun kalemle yazılan sıradaki hedef.");

        public int Count => slots.Length;

        void Awake()
        {
            rest = new Vector3[slots.Length];
            restRotation = new Quaternion[slots.Length];
            droppedAt = new float[slots.Length];
            fallAt = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                rest[i] = slots[i].card != null ? slots[i].card.localPosition : Vector3.zero;
                restRotation[i] = slots[i].card != null ? slots[i].card.localRotation : Quaternion.identity;
                droppedAt[i] = -100;
                fallAt[i] = -1;
            }
        }

        /// <param name="freed">Özgür bölgeler, özgürleşme sırasıyla: gösterilecek ad ve vardiya numarası.</param>
        /// <param name="next">Sıradaki hedefin adı (yoksa null); özgür kartlardan sonraki yuvaya kurşun kalemle.</param>
        /// <param name="animate">Yeni eklenen kartlar üstten düşer (vardiya raporu kapanınca); karşı saldırıda özgürlüğünü
        /// yitiren bölgenin kartı raptiyeden kurtulup panodan aşağı düşer (Revizyon 2 · S5b).</param>
        public void Show(IReadOnlyList<(string name, int shift)> freed, string next, bool animate)
        {
            if (rest == null) Awake();
            int n = Mathf.Min(freed.Count, slots.Length);
            if (count != null) count.text = n.ToString("00");
            if (target != null) target.LineOverride = Corrigible.Core.Loc.T(TargetLine);
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                bool filled = i < n, isNext = !filled && i == n && !string.IsNullOrEmpty(next);
                if (s.card != null)
                {
                    bool wasFilled = s.card.gameObject.activeSelf && fallAt[i] < 0;
                    if (!filled && wasFilled && animate && shown >= 0 && Application.isPlaying)
                    {
                        // Kart düşer; düşüş bitince kapanır
                        fallAt[i] = Time.unscaledTime;
                    }
                    else if (filled || fallAt[i] < 0)
                    {
                        if (filled) fallAt[i] = -1;
                        s.card.gameObject.SetActive(filled);
                        if (filled && !wasFilled && animate && shown >= 0 && Application.isPlaying) droppedAt[i] = Time.unscaledTime;
                        if (!filled) { s.card.localPosition = rest[i]; s.card.localRotation = restRotation[i]; }
                    }
                }
                if (filled)
                {
                    if (s.name != null) s.name.text = freed[i].name;
                    if (s.shift != null) s.shift.text = Corrigible.Core.Loc.F("V.{0:000}", freed[i].shift);
                }
                if (s.next != null) s.next.SetActive(isNext);
                if (isNext && s.nextName != null) s.nextName.text = next + "?";
                if (s.emptyPin != null) s.emptyPin.SetActive(!filled && !isNext);
            }
            shown = n;
        }

        void Update()
        {
            if (rest == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var card = slots[i].card;
                if (card == null || !card.gameObject.activeSelf) continue;
                if (fallAt[i] >= 0)
                {
                    // Düşüş: önce raptiye etrafında döner, sonra yerçekimiyle aşağı (0,6 s)
                    float f = (Time.unscaledTime - fallAt[i]) / 0.6f;
                    card.localRotation = restRotation[i] * Quaternion.Euler(0, 0, Mathf.Min(1, f * 3) * 24);
                    card.localPosition = rest[i] + Vector3.down * (0.5f * f * f);
                    if (f >= 1)
                    {
                        fallAt[i] = -1;
                        card.localPosition = rest[i];
                        card.localRotation = restRotation[i];
                        card.gameObject.SetActive(false);
                    }
                    continue;
                }
                float t = (Time.unscaledTime - droppedAt[i]) / dropSeconds;
                if (t >= 1)
                {
                    if (card.localPosition != rest[i]) card.localPosition = rest[i];
                    continue;
                }
                // Düşüş: yerçekimi gibi hızlanır, raptiyeye oturur
                card.localPosition = rest[i] + Vector3.up * (dropHeight * (1 - t * t));
            }
        }
    }
}
