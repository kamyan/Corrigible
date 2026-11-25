using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bina planının üç paftası (tesis aşama 5; Docs/TESIS-BASKINLARI-TASARIMI.md §7.1): görev fişinin iç yanında, üst bantta, üçü
    /// yan yana (soldan sağa giriş, ara oda, hedef oda; fiş sağdayken de bu sıra). Kadro raporu gibi fişe yaslanır: fiş kayarken
    /// onunla gider, fişin görünürlüğüyle belirir, açılışta 12 px yukarıdan oturur, fiş gizlenince gizlenir. İçerik
    /// <see cref="FacilityPlanData"/> olarak gelir (kurucu Bridge'de; plan yokken bağlayıcı hiç göstermez). Kararı bağlayıcı verir.
    /// </summary>
    public sealed class FacilityPlanSheets : MonoBehaviour
    {
        [Tooltip("Üç paftanın ortak kökü (fişin üst köşesine yaslanır).")]
        [SerializeField] RectTransform band;
        [SerializeField] CanvasGroup group;
        [Tooltip("Yanına oturduğu görev fişi.")]
        [SerializeField] OperationSlip slip;
        [SerializeField] FacilityPlanSheet[] sheets = Array.Empty<FacilityPlanSheet>();
        [Tooltip("Fişle arası (1080p referans px): yatay boşluk, fişin üst kenarından aşağı kayma.")]
        [SerializeField] Vector2 gap = new Vector2(14, 0);

        float openAt = -1, closeAt = -1;

        /// <summary>Paftalar açık (kapanırken değil).</summary>
        public bool IsOpen => gameObject.activeSelf && closeAt < 0;
        public string RegionId => Data?.RegionId;
        /// <summary>Son gösterilen veri (kapalıyken null).</summary>
        public FacilityPlanData Data { get; private set; }
        public FacilityPlanSheet Sheet(int i) => i >= 0 && i < sheets.Length ? sheets[i] : null;
        /// <summary>Üç paftanın kökü (yerleşim testleri okur).</summary>
        public RectTransform Band => band;

        static float OpenSeconds => Mathf.Max(0.01f, SiginakTheme.Active.NumberOf("motion.slipSlideS", 0.22f));
        static float CloseSeconds => SiginakTheme.Active.NumberOf("motion.slipCloseS", 0.09f);

        /// <summary>İçeriği yazar; kapalıysa açılır. Veri yoksa gizler.</summary>
        public void Show(FacilityPlanData d)
        {
            if (d == null) { Hide(); return; }
            bool opening = !IsOpen;
            Data = d;
            gameObject.SetActive(true);
            for (int i = 0; i < sheets.Length; i++)
            {
                var s = i < d.Sheets.Count ? d.Sheets[i] : null;
                sheets[i].gameObject.SetActive(s != null);
                if (s != null) sheets[i].Render(s);
            }
            if (opening)
            {
                openAt = Time.unscaledTime;
                closeAt = -1;
            }
            LateUpdate();
        }

        /// <summary>Fiş kapanırken: <paramref name="delay"/> sonra solar.</summary>
        public void Close(float delay = 0)
        {
            if (!IsOpen) return;
            closeAt = Time.unscaledTime + delay;
        }

        /// <summary>Hemen gizler (yeni oyun, başka fiş, bayrak kapalı).</summary>
        public void Hide()
        {
            Data = null;
            openAt = closeAt = -1;
            gameObject.SetActive(false);
        }

        // Fiş kendi Update'inde yerini aldıktan sonra (kayarken de) yanına oturur
        void LateUpdate()
        {
            if (slip == null || !slip.gameObject.activeSelf) { Hide(); return; }
            float now = Time.unscaledTime;
            float rise = 0, alpha = 1;
            if (openAt >= 0)
            {
                float p = Mathf.Clamp01((now - openAt) / OpenSeconds);
                float e = 1 - (1 - p) * (1 - p);
                rise = 12 * (1 - e);
                alpha = e;
                if (p >= 1) openAt = -1;
            }
            if (closeAt >= 0 && now >= closeAt)
            {
                float p = Mathf.Clamp01((now - closeAt) / CloseSeconds);
                alpha = 1 - p;
                if (p >= 1) { Hide(); return; }
            }
            group.alpha = alpha * slip.Alpha;
            group.blocksRaycasts = group.alpha > 0.5f;
            Place(rise);
        }

        /// <summary>Fiş solda ise sağ üst köşesinin yanına (sola yaslı), sağda ise sol üst köşesinin yanına (sağa yaslı).</summary>
        void Place(float rise)
        {
            if (slip.Paper == null) return;
            var s = slip.Paper;
            bool right = slip.OnRight;
            var r = s.rect;
            var corner = s.TransformPoint(new Vector3(right ? r.xMin : r.xMax, r.yMax, 0));
            var parent = (RectTransform)band.parent;
            band.pivot = new Vector2(right ? 1 : 0, 1);
            band.position = corner + parent.TransformVector(new Vector3(right ? -gap.x : gap.x, -gap.y + rise, 0));
        }
    }
}
