using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masa pencereleri: yerleşim sahnededir. Her pencere sahnedeki konumunda ve boyunda açılır (çalışırken yerleşim
    /// değiştirilmez, yalnızca veri dolar); başlangıçta açık olanlar sahnede etkin olanlardır. Açılan ya da tıklanan pencere
    /// öne gelir. Sekmeden sürüklenen pencere bırakıldığı yerde kalır (ekran içinde tutulur). Açık/kapalı durumu görünüme
    /// aittir. Hedef kartı ve harita gibi pencere olmayan paneller <see cref="DeskDraggablePanel"/> ile taşınır.
    /// </summary>
    public sealed class WindowManager : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public FolderWindow window;
        }

        [SerializeField] Entry[] windows = Array.Empty<Entry>();
        [SerializeField] RectTransform layer;

        public static WindowManager Instance { get; private set; }

        /// <summary>Bir pencere açıldı, kapandı ya da öne geldi.</summary>
        public event Action Changed;
        /// <summary>Kapatılan pencere (dosya kapanınca seçim kalkar).</summary>
        public event Action<WindowId> Closed;

        readonly List<FolderWindow> focus = new List<FolderWindow>();   // son etkileşim en sonda
        readonly Dictionary<WindowId, FolderWindow> byId = new Dictionary<WindowId, FolderWindow>();

        FolderWindow dragging;

        void Awake() => Init();

        /// <summary>Edit mod önizlemesi: Awake çalışmadığı için pencere tablosu burada kurulur.</summary>
        public void EnsureInit() { if (byId.Count == 0) Init(); }

        void Init()
        {
            Instance = this;
            byId.Clear(); focus.Clear();
            foreach (var e in windows)
            {
                if (e.window == null) continue;
                byId[e.window.Id] = e.window;
                // Sahnede açık bırakılan pencere açık başlar; sırası sahnedeki çizim sırası
                if (e.window.gameObject.activeSelf) focus.Add(e.window);
            }
            focus.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public bool IsOpen(WindowId id) => byId.TryGetValue(id, out var w) && w.gameObject.activeSelf;
        public bool IsFront(WindowId id) => IsOpen(id);
        public FolderWindow Get(WindowId id) => byId.TryGetValue(id, out var w) ? w : null;

        /// <summary>En son etkileşilen açık pencere (Esc onu kapatır).</summary>
        public FolderWindow Top => focus.Count > 0 ? focus[focus.Count - 1] : null;

        /// <summary>Kapalıysa açar; açıksa ve en üstte değilse öne getirir; en üstteyse kapatır.</summary>
        public void Toggle(WindowId id)
        {
            if (!byId.TryGetValue(id, out var w)) return;
            if (!w.gameObject.activeSelf) Open(id);
            else if (Top != w) BringToFront(w);
            else Close(id);
        }

        public void Open(WindowId id, bool notify = true)
        {
            if (!byId.TryGetValue(id, out var w)) return;
            if (w.gameObject.activeSelf) { BringToFront(w); return; }
            w.gameObject.SetActive(true);
            if (notify) w.PlayOpen();
            Touch(w);
            if (notify) Changed?.Invoke();
        }

        public void Close(WindowId id) => Close(id, true);

        void Close(WindowId id, bool notify)
        {
            if (!byId.TryGetValue(id, out var w) || !w.gameObject.activeSelf) return;
            if (dragging == w) dragging = null;
            focus.Remove(w);
            w.gameObject.SetActive(false);
            Closed?.Invoke(id);
            if (notify) Changed?.Invoke();
        }

        public void BringToFront(FolderWindow w)
        {
            if (w == null || !w.gameObject.activeSelf) return;
            bool changed = Top != w;
            Touch(w);
            if (changed) Changed?.Invoke();
        }

        /// <summary>Son etkileşilen en üstte çizilir.</summary>
        void Touch(FolderWindow w)
        {
            focus.Remove(w); focus.Add(w);
            w.transform.SetAsLastSibling();
        }

        // ------------------------------------------------------------------ sürükleme (serbest)
        public void BeginDrag(FolderWindow w, PointerEventData e)
        {
            if (w == null || !w.gameObject.activeSelf) return;
            dragging = w;
            Touch(w);
        }

        public void Drag(FolderWindow w, PointerEventData e)
        {
            if (dragging != w) return;
            var rt = (RectTransform)w.transform;
            DeskDraggablePanel.MoveWithin(rt, layer, DeskDraggablePanel.ParentDelta(rt, e));
        }

        public void EndDrag(FolderWindow w, PointerEventData e)
        {
            if (dragging != w) return;
            dragging = null;
            Changed?.Invoke();
        }
    }
}
