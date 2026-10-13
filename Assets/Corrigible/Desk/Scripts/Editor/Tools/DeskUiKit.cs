using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Prefab'ları Editor'de elle kurarken kullanılan küçük yardımcılar (KARARLAR: prefab'lar kaynaktır, builder tutulmaz).
    /// Düğüm, görüntü, metin ve serileştirilmiş alan bağlama; varsayılanlar PROMPT §4.7'ye uygun (raycast kapalı, Auto Size kapalı).
    /// </summary>
    public static class DeskUiKit
    {
        public const string PrefabDir = DeskSpriteManifest.Root + "/Prefabs";

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Sol-üst köşeli yerleşim (ebeveynin sol-üstüne göre, y aşağı).</summary>
        public static RectTransform At(RectTransform rt, float x, float y, float w, float h)
        {
            DeskUi.Place(rt, x, y, w, h);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Center(RectTransform rt, float w, float h, float x = 0, float y = 0)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Img(Transform parent, string name, string sprite, string tintKey = null, float alpha = 1)
        {
            var rt = Stretch(Node(name, parent));
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (sprite != null) DeskSprites.Apply(img, sprite);
            if (tintKey != null) rt.gameObject.AddComponent<ThemeTint>().Set(tintKey, alpha);
            return img;
        }

        public static RawImage Raw(Transform parent, string name, string texture, string tintKey = null)
        {
            var rt = Stretch(Node(name, parent));
            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            img.texture = DeskSprites.Instance.Texture(texture);
            if (tintKey != null) rt.gameObject.AddComponent<ThemeTint>().Set(tintKey);
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, DeskFont font, float size, string tintKey, string text,
            TextAlignmentOptions align = TextAlignmentOptions.Left, float spacing = 0)
        {
            var rt = Stretch(Node(name, parent));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var theme = DeskTheme.Active;
            t.font = theme.Font(font);
            t.fontSize = size;
            t.characterSpacing = spacing;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = false;
            t.richText = true;
            t.raycastTarget = false;
            t.styleSheet = theme.styleSheet;
            t.text = text;
            if (tintKey != null) rt.gameObject.AddComponent<ThemeTint>().Set(tintKey);
            return t;
        }

        public static Shadow AddShadow(Graphic g, Vector2 offsetDown, Color color)
        {
            var sh = g.gameObject.AddComponent<Shadow>();
            sh.effectDistance = new Vector2(offsetDown.x, -offsetDown.y);
            sh.effectColor = color;
            sh.useGraphicAlpha = true;
            return sh;
        }

        public static T Add<T>(Component c) where T : Component => c.gameObject.AddComponent<T>();

        /// <summary>Özel [SerializeField] alanları bağlar: ("ad", değer) çiftleri; değer Object, string, bool, int, float, enum ya da Vector2.</summary>
        public static void Wire(Object target, params (string field, object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogError($"[Desk] {target.GetType().Name}.{field} yok"); continue; }
                switch (value)
                {
                    case Object o: p.objectReferenceValue = o; break;
                    case string s: p.stringValue = s; break;
                    case bool b: p.boolValue = b; break;
                    case float f: p.floatValue = f; break;
                    case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                    case int i: p.intValue = i; break;
                    case System.Enum e: p.enumValueIndex = System.Convert.ToInt32(e); break;
                    case Vector2 v: p.vector2Value = v; break;
                    case Object[] arr:
                        p.arraySize = arr.Length;
                        for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                        break;
                    default: Debug.LogError($"[Desk] {field}: desteklenmeyen tür {value?.GetType()}"); break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject Instance(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return go;
        }

        public static GameObject LoadPrefab(string relPath) => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + relPath);

        /// <summary>Kökü prefab olarak kaydeder (yol aynıysa GUID korunur) ve sahnedeki geçici kopyayı siler.</summary>
        public static GameObject Save(GameObject root, string relPath)
        {
            var path = PrefabDir + "/" + relPath;
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
            Object.DestroyImmediate(root);
            if (!ok) Debug.LogError("[Desk] Prefab kaydedilemedi: " + path);
            return asset;
        }
    }
}
