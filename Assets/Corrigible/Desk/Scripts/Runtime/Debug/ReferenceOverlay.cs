using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Referans katmanı (PROMPT §9.1; yalnızca Editor/Development): <c>Docs/UI_C/reference/screens/&lt;ekran&gt;.png</c> diskten
    /// yüklenir (Assets'e kopyalanmaz) ve en üst canvas'ta tam ekran gösterilir. F12 aç-kapa; F11 opaklık %25/50/75/100;
    /// Shift+F11 yanıp sönme (0,5 sn).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class ReferenceOverlay : MonoBehaviour
    {
        [SerializeField] string screenName = "C1_main";
        [SerializeField] string folder = "Docs/UI_C/reference/screens";
        [SerializeField] float[] opacities = { 0.25f, 0.5f, 0.75f, 1f };

        RawImage image;
        Texture2D tex;
        int opacityIndex = 1;
        bool visible, blink;
        string loaded;

        public string ScreenName { get => screenName; set { screenName = value; if (visible) Load(); } }

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            image.enabled = false;
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f12Key.wasPressedThisFrame) SetVisible(!visible);
            if (kb.f11Key.wasPressedThisFrame)
            {
                if (kb.shiftKey.isPressed) blink = !blink;
                else opacityIndex = (opacityIndex + 1) % opacities.Length;
                Apply();
            }
            if (visible && blink) image.enabled = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
#endif
        }

        public void SetVisible(bool on)
        {
            visible = on;
            if (on) Load();
            Apply();
        }

        void Load()
        {
            if (loaded == screenName && tex != null) return;
            var path = Path.Combine(Application.dataPath, "..", folder, screenName + ".png");
            if (!File.Exists(path)) { Debug.LogWarning("[Desk] Referans yok: " + path); return; }
            if (tex == null) tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            image.texture = tex;
            loaded = screenName;
        }

        void Apply()
        {
            if (image == null) return;
            image.enabled = visible;
            image.color = new Color(1, 1, 1, opacities[opacityIndex]);
        }

        void OnDestroy()
        {
            if (tex != null) Destroy(tex);
        }
    }
}
