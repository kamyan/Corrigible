using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Revizyon 3 modelindeki yazı yuvası (manifest <c>texts[]</c>, <c>TXT_*</c>). Modellerde kelime yoktur; her yazı bu
    /// yuvadaki TMP'dir. Yuvanın ölçüsü EN örneğe göredir (%30'a kadar uzun); punto küçülmez, satır sarar. Yüz yuvaları
    /// (kart, kâğıt, dosya, defter sayfası) TMP taşımaz: <see cref="IsSurface"/>, içerik görünümün işidir.
    /// Sabit plaka yazısının çevirisi sahneye konunca <c>DeskLocText</c> ile bağlanır; buradaki örnekler yalnız referanstır.
    /// </summary>
    public sealed class Rev3TextSlot : MonoBehaviour
    {
        [SerializeField] string slotName, role, fontFamily, sample, sampleEn;
        [Tooltip("Büyük harf yüksekliği (mm); boşsa yuvaya sığdırılır.")]
        [SerializeField] float capMm;
        [Tooltip("Yuvanın dikdörtgeni (m).")]
        [SerializeField] Vector2 size;
        [SerializeField] bool surface;
        [SerializeField] TMP_Text text;

        public string SlotName => slotName;
        public string Role => role;
        public string FontFamily => fontFamily;
        public string Sample => sample;
        public string SampleEn => sampleEn;
        public float CapMm => capMm;
        public Vector2 Size => size;
        public bool IsSurface => surface;
        public TMP_Text Text => text;

        public void Configure(string name, string roleName, string family, string tr, string en, float cap, Vector2 rect, bool isSurface, TMP_Text tmp)
        {
            slotName = name; role = roleName; fontFamily = family; sample = tr; sampleEn = en; capMm = cap; size = rect;
            surface = isSurface; text = tmp;
        }

        /// <summary>Örnek metni gösterir (doğrulama görüntüleri için); EN örneği yoksa TR kalır (kişi adları, sabit plakalar).</summary>
        public void ShowSample(bool english)
        {
            if (text == null) return;
            text.text = english && !string.IsNullOrEmpty(sampleEn) ? sampleEn : sample;
        }
    }
}
