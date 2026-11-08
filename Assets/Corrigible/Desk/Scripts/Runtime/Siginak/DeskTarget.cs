using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Odadaki tıklanır nesne (K1 notu › etkileşim): “pirinç plakası olan her nesne tıklanır”. Üzerine gelince nesne 4 mm
    /// kalkar, 400 ms sonra kâğıt etiket (ad + kısayol + tek satır) çıkar; çerçeve çizilmez. Kamerada PhysicsRaycaster ve
    /// nesnede modele oturan çarpıştırıcı gerekir. Metinler Türkçe kaynaktır, gösterirken çevrilir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DeskTarget : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>Girdi kilidi (modal, hikâye, baskın); bağlayıcı kurar.</summary>
        public static Func<bool> Blocked;

        [SerializeField] string title, line;
        [Tooltip("Üzerindeyken kalkan parça (yoksa kalkmaz).")]
        [SerializeField] Transform lift;
        [Tooltip("Kalkış yönü: masada yukarı, duvara asılı nesnede duvardan dışarı.")]
        [SerializeField] Vector3 liftDirection = Vector3.up;
        [SerializeField] HoverTag hoverTag;

        Vector3 rest;
        float lifted, target;
        bool hovered;

        public event Action Clicked;
        public event Action<bool> Hovered;
        public bool Interactable { get; set; } = true;
        public bool IsHovered => hovered;
        /// <summary>Etiketin satırını değiştirir (ör. devre dışı gerekçesi); kaynak metin değil, gösterilecek metin.</summary>
        public string LineOverride { get; set; }
        /// <summary>Etiketin başlığını değiştirir (ör. “KAPI · ZİYARETÇİ VAR”); kaynak metin, gösterirken çevrilir.</summary>
        public string TitleOverride { get; set; }
        public string Title => TitleOverride ?? title;
        public string HoverTitle => Corrigible.Core.Loc.T(Title);
        public string Line => LineOverride ?? (string.IsNullOrEmpty(line) ? null : Corrigible.Core.Loc.T(line));
        /// <summary>
        /// Bu nesnenin kendi girdi kilidi (ör. ayarlar dolabının kontrolleri: oda kilitliyken dolap açıksa çalışır); boşsa
        /// odanın kilidi (<see cref="Blocked"/>) geçerlidir.
        /// </summary>
        public Func<bool> BlockedOverride { get; set; }
        /// <summary>Üzerine gelme etiketi (prefab'daki nesneye sahne bağlar).</summary>
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }

        public void Configure(string titleSource, string lineSource, Transform liftPart, HoverTag hoverTag)
        {
            title = titleSource; line = lineSource; lift = liftPart; this.hoverTag = hoverTag;
        }

        void Awake() { if (lift != null) rest = lift.localPosition; }

        bool IsBlocked => BlockedOverride != null ? BlockedOverride() : Blocked != null && Blocked();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsBlocked) return;
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || IsBlocked || !Interactable) return;
            Clicked?.Invoke();
        }

        void SetHovered(bool on)
        {
            if (hovered == on) return;
            hovered = on;
            target = on ? SiginakTheme.Active.Number("motion.hoverLiftMm", 4) / 1000f : 0;
            if (hoverTag != null) { if (on) hoverTag.Show(this); else hoverTag.Hide(this); }
            Hovered?.Invoke(on);
        }

        void OnDisable() => SetHovered(false);

        void Update()
        {
            if (hovered && IsBlocked) SetHovered(false);
            if (lift == null || Mathf.Approximately(lifted, target)) return;
            lifted = Mathf.MoveTowards(lifted, target, Time.unscaledDeltaTime / 0.08f * 0.004f);
            lift.localPosition = rest + liftDirection * lifted;
        }
    }
}
