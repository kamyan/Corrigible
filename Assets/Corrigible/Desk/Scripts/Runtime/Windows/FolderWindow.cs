using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Manila klasör penceresi (C1 §7.1): sekme (tutamaç ⠿, başlık, alt başlık / tuş fişi, kapat) + gövde (manila, gölge 3,4)
    /// + isteğe bağlı ataş. Pencere çubuğu yoktur; sekmeden sürükleme <see cref="WindowManager"/>'da, yerleşim sahnede.
    /// Arka klasör görünümü (<see cref="SetBack"/>: sekme sağa yaslı, arka manila) eski sütun düzeninindi; pencereler artık
    /// serbest durduğu için çağrılmıyor.
    /// </summary>
    public sealed class FolderWindow : MonoBehaviour
    {
        [SerializeField] WindowId id;
        [SerializeField] RectTransform tab;
        [SerializeField] RectTransform body;
        [SerializeField] DeskRow tabRow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] KeyChipView key;
        [SerializeField] DeskButton close;
        [SerializeField] RectTransform grip;
        [Tooltip("Tasarlanmamış pencereler: ön sekme genişliği içerikten (aksi hâlde prefab'daki referans genişliği).")]
        [SerializeField] bool autoTabWidth;
        [Tooltip("Klasör arkadayken gizlenen süsler (ör. ataş sekme şeridinden taşmasın).")]
        [SerializeField] GameObject[] hideWhenBack = System.Array.Empty<GameObject>();

        const float BackPad = 12;

        Image tabFill, tabFrame;
        bool back, hasSubtitle, hasKey;
        float frontWidth = -1, rowX, backLimit = float.MaxValue;
        CanvasGroup group;
        DeskTween fade;

        public WindowId Id => id;
        public RectTransform Tab => tab;
        public RectTransform Body => body;
        public DeskButton Close => close;
        public RectTransform Grip => grip;
        public bool IsBack => back;

        /// <summary>Kullanıcı açınca klasör ~120 ms'de belirir (PROMPT §8 hareket); başlangıçta açık olanlar animasyonsuz.</summary>
        public void PlayOpen()
        {
            // Edit mod önizlemesi: canlandırma çalışmaz, pencere doğrudan görünür (sahneye bileşen eklenmez)
            if (!Application.isPlaying) { if (group != null || TryGetComponent(out group)) group.alpha = 1; return; }
            if (group == null && !TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            fade.Snap(0);
            fade.To(1, DeskTheme.Active != null ? DeskTheme.Active.folderOpen : 0.12f);
            group.alpha = 0;
        }

        void Update()
        {
            if (fade.Tick(Time.unscaledDeltaTime)) group.alpha = fade.Value;
        }

        void OnDisable()
        {
            if (group != null) { fade.Snap(1); group.alpha = 1; }
        }

        void Awake()
        {
            hasSubtitle = subtitle != null && subtitle.gameObject.activeSelf;
            hasKey = key != null && key.gameObject.activeSelf;
            if (close != null) close.onClick.AddListener(() => { if (WindowManager.Instance != null) WindowManager.Instance.Close(id); });
        }

        public void SetTitle(string titleText, string subtitleText = null, string keyId = null)
        {
            DeskUi.SetText(title, DeskLoc.Instance.T(titleText));
            hasSubtitle = !string.IsNullOrEmpty(subtitleText);
            if (hasSubtitle) DeskUi.SetText(subtitle, DeskLoc.Instance.T(subtitleText));
            hasKey = !string.IsNullOrEmpty(keyId);
            if (hasKey) key.Set(keyId);
            ApplyTab();
        }

        /// <summary>Sekmenin şu anki genişliği.</summary>
        public float TabWidth => tab.sizeDelta.x;

        /// <summary>
        /// Arkadaki klasör (açık karar 2): sekme sağa yaslı, arka manila tonu (#CDB27A), başlık ink2. Arka sekme sadeleşir
        /// (tutamaç, × ve alt başlık gizli; tıklanınca öne gelir) ve <paramref name="maxWidth"/>'e sığar: önce tuş fişi düşer,
        /// sonra başlık “…” ile kısalır. Böylece öndeki sekmeyle çakışmaz.
        /// </summary>
        public void SetBack(bool isBack, float maxWidth = float.MaxValue)
        {
            back = isBack;
            backLimit = maxWidth;
            ApplyTab();
        }

        void ApplyTab()
        {
            if (tabFill == null) { tabFill = tab.Find("Fill")?.GetComponent<Image>(); tabFrame = tab.Find("Frame")?.GetComponent<Image>(); }
            if (frontWidth < 0) { frontWidth = tab.sizeDelta.x; rowX = ((RectTransform)tabRow.transform).anchoredPosition.x; }

            DeskUi.SetActive(grip, !back);
            DeskUi.SetActive(close, !back);
            DeskUi.SetActive(subtitle, !back && hasSubtitle);
            DeskUi.SetActive(key, hasKey);
            foreach (var g in hideWhenBack) DeskUi.SetActive(g, !back);
            title.overflowMode = TextOverflowModes.Overflow;
            tabRow.Layout();

            var rowRt = (RectTransform)tabRow.transform;
            float w;
            if (back)
            {
                rowRt.anchoredPosition = new Vector2(BackPad, rowRt.anchoredPosition.y);
                w = BackPad + tabRow.Width + BackPad;
                if (w > backLimit && hasKey) { DeskUi.SetActive(key, false); tabRow.Layout(); w = BackPad + tabRow.Width + BackPad; }
                if (w > backLimit)
                {
                    w = Mathf.Max(BackPad * 2 + 24, Mathf.Floor(backLimit));
                    title.overflowMode = TextOverflowModes.Ellipsis;
                    var tr = title.rectTransform;
                    tr.sizeDelta = new Vector2(w - BackPad * 2, tr.sizeDelta.y);
                }
            }
            else
            {
                rowRt.anchoredPosition = new Vector2(rowX, rowRt.anchoredPosition.y);
                // Tasarlanmamış pencerelerde sekme içeriğe göre: tutamaç alanı + satır + 12 + × (22) + 5
                w = autoTabWidth ? Mathf.Round(rowX + tabRow.Width + 12 + 22 + 5) : frontWidth;
            }

            tab.anchorMin = tab.anchorMax = new Vector2(back ? 1 : 0, 1);
            tab.pivot = new Vector2(back ? 1 : 0, 1);
            tab.anchoredPosition = Vector2.zero;
            tab.sizeDelta = new Vector2(w, tab.sizeDelta.y);
            if (close != null)
            {
                var cr = (RectTransform)close.transform;
                cr.anchoredPosition = new Vector2(w - 27, cr.anchoredPosition.y);
            }
            DeskUi.Tint(tabFill, back ? "paper.manilaBack" : "paper.manila");
            DeskUi.Tint(tabFrame, back ? "paper.manilaBackEdge" : "paper.manilaEdge");
            DeskUi.Tint(title, back ? "ink.ink2" : "ink.ink");
        }
    }
}
