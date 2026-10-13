using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Kriz masasında küçük, sürüklenebilir pencere. Başlık çubuğundan tutulup taşınır; bırakılınca konumu kaydedilir
    /// (PlayerPrefs "corrigible.window.&lt;key&gt;") ve sonraki açılışta oradan açılır. Ekranın dışına taşmaz.
    /// Başlığa çift tıklamak kayıtlı konumu siler (varsayılan yerine döner).
    /// </summary>
    public sealed class WindowView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        [Tooltip("Konum kaydının anahtarı (benzersiz).")]
        [SerializeField] string key = "window";
        [Tooltip("Sürükleme yalnızca bu alandan başlar (başlık çubuğu). Boşsa pencerenin tamamı.")]
        [SerializeField] RectTransform handle;
        [SerializeField] StyledButton close;

        public string Key => key;
        public bool IsOpen => gameObject.activeSelf;
        /// <summary>Oyuncu pencereyi bir kez taşıdı: kayıtlı konum var.</summary>
        public bool Pinned => PlayerPrefs.HasKey(PrefKey);
        public event Action Opened, Closed;

        RectTransform rt;
        Vector2 defaultPos;
        Vector2 grab;
        bool dragging;
        string PrefKey => "corrigible.window." + key;

        RectTransform Rt => rt != null ? rt : rt = (RectTransform)transform;
        RectTransform Parent => (RectTransform)transform.parent;

        void Awake()
        {
            defaultPos = Rt.anchoredPosition;
            if (close != null) UiUtil.OnClick(close.Button, Close);
            Restore();
        }

        public void Open()
        {
            if (!IsOpen) { gameObject.SetActive(true); Opened?.Invoke(); }
            transform.SetAsLastSibling();
            Clamp();
        }

        public void Close()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        /// <summary>Kayıtlı konum yoksa pencereyi verilen yere taşır (bölge kartı bölgenin yanında açılır).</summary>
        public void PlaceIfNotPinned(Vector2 anchoredPos)
        {
            if (Pinned) return;
            Rt.anchoredPosition = anchoredPos;
            Clamp();
        }

        void Restore()
        {
            if (Pinned) Rt.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(PrefKey + ".x"), PlayerPrefs.GetFloat(PrefKey + ".y"));
            else Rt.anchoredPosition = defaultPos;
        }

        // ---------------------------------------------------------------- sürükleme
        bool OnHandle(PointerEventData e) => handle == null || RectTransformUtility.RectangleContainsScreenPoint(handle, e.pressPosition, e.pressEventCamera);

        public void OnPointerDown(PointerEventData e) => transform.SetAsLastSibling();

        public void OnBeginDrag(PointerEventData e)
        {
            dragging = OnHandle(e);
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, e.position, e.pressEventCamera, out var p);
            grab = Rt.anchoredPosition - p;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, e.position, e.pressEventCamera, out var p);
            Rt.anchoredPosition = p + grab;
            Clamp();
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            var a = Rt.anchoredPosition;
            PlayerPrefs.SetFloat(PrefKey + ".x", a.x);
            PlayerPrefs.SetFloat(PrefKey + ".y", a.y);
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.clickCount == 2 && OnHandle(e)) ResetPosition();
        }

        public void ResetPosition()
        {
            PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.DeleteKey(PrefKey + ".x"); PlayerPrefs.DeleteKey(PrefKey + ".y");
            Rt.anchoredPosition = defaultPos;
            Clamp();
        }

        /// <summary>Pencere ebeveyninin (pencere katmanı) içinde kalsın; altı taşabilir ama başlık çubuğu hep görünür.</summary>
        public void Clamp()
        {
            var parent = Parent.rect;
            var r = Rt.rect;
            var pv = Rt.pivot;
            var a = Rt.anchoredPosition;
            float left = a.x - r.width * pv.x, top = a.y + r.height * (1 - pv.y);
            left = Mathf.Clamp(left, parent.xMin, Mathf.Max(parent.xMin, parent.xMax - r.width));
            top = Mathf.Clamp(top, parent.yMin + Mathf.Min(r.height, 48), parent.yMax);
            Rt.anchoredPosition = new Vector2(left + r.width * pv.x, top - r.height * (1 - pv.y));
        }
    }
}
