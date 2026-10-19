using System;
using System.Collections.Generic;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>FX JSON tokenlarının editörde pişirilmiş karşılığı; çalışma zamanında JSON okunmaz.</summary>
    [CreateAssetMenu(menuName="Corrigible/FX Theme")]
    public sealed class FxTheme : TokenTheme
    {
        [Serializable] public struct FontEntry { public string key; public TMP_FontAsset font; }
        [Serializable] public struct TypeEntry { public string key, fontKey, colorKey; public float size, spacing; }
        [Serializable] public struct SpriteEntry { public string key; public Sprite sprite; public Image.Type type; public float padding; public bool tint; public Vector2 size; }
        [Serializable] public struct GlowEntry { public string key, fontKey; public Material material; }
        public List<FontEntry> fonts = new List<FontEntry>();
        public List<TypeEntry> types = new List<TypeEntry>();
        public List<SpriteEntry> sprites = new List<SpriteEntry>();
        public List<GlowEntry> glows = new List<GlowEntry>();
        public Material additive, line;
        public TMP_FontAsset Font(string key) { foreach(var e in fonts) if(e.key==key)return e.font; return null; }
        public SpriteEntry SpriteInfo(string key) { foreach(var e in sprites) if(e.key==key)return e; throw new ArgumentException(key); }
        public Sprite Sprite(string key) => SpriteInfo(key).sprite;
        public Material Glow(string key, TMP_FontAsset font) { foreach(var e in glows) if(e.key==key && Font(e.fontKey)==font)return e.material; return font.material; }
        public Color Color(string key) => Get("colors."+key);
        public void Apply(TMP_Text text,string key)
        {
            foreach(var e in types) if(e.key==key) {
                text.font=Font(e.fontKey); text.fontSize=Mathf.Max(11,e.size); text.characterSpacing=e.spacing;
                text.color=Color(e.colorKey); text.fontStyle=FontStyles.Normal; text.enableAutoSizing=false; return;
            }
            throw new ArgumentException(key);
        }
        public void Apply(Image image,string key,Color tint)
        {
            var e=SpriteInfo(key); image.sprite=e.sprite; image.type=e.type; image.pixelsPerUnitMultiplier=1;
            image.color=e.tint?tint:UnityEngine.Color.white;
        }
    }
}
