using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bir Graphic'in (Image, RawImage, TMP) rengini tema anahtarından alır; sabit hex yazılmaz (PROMPT §4.3).
    /// İllüzyon paleti değişince kendiliğinden güncellenir. Boş anahtar = rengi elle bırak.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public sealed class ThemeTint : MonoBehaviour
    {
        [SerializeField] string key;
        [Range(0, 1)] [SerializeField] float alpha = 1;

        Graphic graphic;

        public string Key => key;

        public void Set(string colorKey, float a = 1)
        {
            if (key == colorKey && Mathf.Approximately(alpha, a)) return;
            key = colorKey; alpha = a;
            Apply();
        }

        void OnEnable() { DeskTheme.Changed += Apply; Apply(); }
        void OnDisable() => DeskTheme.Changed -= Apply;
        void OnValidate() => Apply();

        public void Apply()
        {
            if (string.IsNullOrEmpty(key)) return;
            var theme = DeskTheme.Active;
            if (theme == null) return;
            if (graphic == null) graphic = GetComponent<Graphic>();
            var c = theme.Get(key);
            c.a *= alpha;
            if (graphic.color != c) graphic.color = c;
        }
    }
}
