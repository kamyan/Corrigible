using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Kriz masasının pencereleri. Yan pencerelerden (araştırma, günlük, rehber, ekipler, haberler) aynı anda yalnızca biri
    /// açık olur; bölge kartı bunlardan bağımsızdır ve seçili bölgenin yanında açılır (oyuncu taşıdıysa taşıdığı yerde).
    /// Alt çubuktaki düğmeler ve kısayol tuşları pencereleri açıp kapatır; kapalı pencerede yeni bir şey olunca düğmede rozet yanar.
    /// </summary>
    public sealed class DeskWindows : MonoBehaviour
    {
        [Serializable]
        public struct Side
        {
            public WindowView window;
            public StyledButton button;
            [Tooltip("Kısayol tuşu.")] public Key key;
            [Tooltip("Kapalıyken yeni bir şey olduğunu gösteren nokta.")] public GameObject badge;
        }

        /// <summary>Yan pencere sırası (eski sekme sırası korunur: 0 araştırma, 1 günlük, 2 rehber).</summary>
        public const int Research = 0, Log = 1, Guide = 2, Teams = 3, News = 4;

        [SerializeField] UiTheme theme;
        [SerializeField] GameController game;
        [SerializeField] ModalView modals;
        [SerializeField] SettingsModal settings;
        [SerializeField] Side[] sides = Array.Empty<Side>();
        [SerializeField] WindowView region;
        [Tooltip("Bölge kartının düğüme göre konumu (düğümün sağına, biraz yukarı).")]
        [SerializeField] Vector2 regionOffset = new Vector2(48, 90);

        public static DeskWindows Instance { get; private set; }
        public WindowView Region => region;
        public event Action<int> SideOpened;

        public bool AnyOpen
        {
            get
            {
                if (region != null && region.IsOpen) return true;
                foreach (var s in sides) if (s.window.IsOpen) return true;
                return false;
            }
        }

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < sides.Length; i++)
            {
                int k = i;
                UiUtil.OnClick(sides[i].button.Button, () => Toggle(k));
                sides[i].window.Opened += () => OnOpened(k);
                sides[i].window.Closed += Paint;
                sides[i].window.gameObject.SetActive(false);
                SetBadge(i, false);
            }
            if (region != null) region.gameObject.SetActive(false);
            Paint();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnOpened(int k)
        {
            for (int i = 0; i < sides.Length; i++) if (i != k) sides[i].window.Close();
            SetBadge(k, false);
            Paint();
            if (region != null && region.IsOpen) PlaceRegion();   // yan pencere açılınca bölge kartı yol versin
            SideOpened?.Invoke(k);
        }

        public bool IsOpen(int i) => i >= 0 && i < sides.Length && sides[i].window.IsOpen;
        public void Open(int i) { if (i >= 0 && i < sides.Length) sides[i].window.Open(); }
        public void Toggle(int i) { if (i >= 0 && i < sides.Length) sides[i].window.Toggle(); }

        public void CloseAll()
        {
            foreach (var s in sides) s.window.Close();
            if (region != null) region.Close();
        }

        public void SetBadge(int i, bool on)
        {
            if (i < 0 || i >= sides.Length || sides[i].badge == null) return;
            bool show = on && !sides[i].window.IsOpen;
            if (sides[i].badge.activeSelf != show) sides[i].badge.SetActive(show);
        }

        /// <summary>Düğme etiketi; sonuna kısayol tuşu eklenir ("YZ günlüğü (3) · L").</summary>
        public void SetLabel(int i, string text)
        {
            if (i < 0 || i >= sides.Length) return;
            sides[i].button.SetText(sides[i].key != Key.None ? text + " · " + sides[i].key.ToString().ToUpperInvariant() : text);
        }

        /// <summary>Pencereyi ve düğmesini gösterir/gizler (ör. saha ekipleri yalnızca deneysel modda).</summary>
        public void SetSideVisible(int i, bool on)
        {
            if (i < 0 || i >= sides.Length) return;
            UiUtil.Show(sides[i].button, on);
            if (!on) sides[i].window.Close();
        }

        RectTransform lastNode;

        /// <summary>Bölge kartını açar: taşınmadıysa bölge düğümünün yanına, açık yan pencereyle çakışmayan tarafa.</summary>
        public void ShowRegion(RectTransform node)
        {
            if (region == null) return;
            lastNode = node;
            PlaceRegion();
            region.Open();
        }

        void PlaceRegion()
        {
            if (lastNode == null || region.Pinned) return;
            var layer = (RectTransform)region.transform.parent;
            Vector2 local = layer.InverseTransformPoint(lastNode.TransformPoint(lastNode.rect.center));
            var size = ((RectTransform)region.transform).rect.size;
            float right = local.x + regionOffset.x, left = local.x - regionOffset.x - size.x;
            float top = local.y + regionOffset.y;
            bool rightFits = right + size.x <= layer.rect.xMax && !HitsSide(right, top, size);
            bool leftFits = left >= layer.rect.xMin && !HitsSide(left, top, size);
            float x = rightFits ? right : leftFits ? left : (right + size.x <= layer.rect.xMax ? right : left);
            region.PlaceIfNotPinned(new Vector2(x, top));
        }

        // Açık yan pencereyle yatayda çakışıyor mu (pencereler sol üst köşeden konumlanır)
        bool HitsSide(float x, float top, Vector2 size)
        {
            foreach (var s in sides)
            {
                if (!s.window.IsOpen) continue;
                var r = (RectTransform)s.window.transform;
                float sx = r.anchoredPosition.x - r.rect.width * r.pivot.x;
                if (x < sx + r.rect.width && sx < x + size.x) return true;
            }
            return false;
        }

        public void HideRegion() { if (region != null) region.Close(); }

        void Paint()
        {
            for (int i = 0; i < sides.Length; i++)
            {
                bool on = sides[i].window.IsOpen;
                sides[i].button.SetColors(on ? theme.raise : Color.clear, on ? theme.verify : theme.line2, on ? theme.fg : theme.muted);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || game.Sim == null || game.Sim.S.Raid != null) return;
            if (modals != null && modals.IsOpen) return;
            if (settings != null && settings.IsOpen) return;
            for (int i = 0; i < sides.Length; i++)
                if (sides[i].key != Key.None && kb[sides[i].key].wasPressedThisFrame) Toggle(i);
        }
    }
}
