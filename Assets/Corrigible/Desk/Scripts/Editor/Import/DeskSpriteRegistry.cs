using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary><c>Resources/DeskSprites.asset</c>: sprite kitini (ad → sprite + Image türü + boyanabilirlik) ve dokuları kaydeder.</summary>
    public static class DeskSpriteRegistry
    {
        public const string RegistryPath = DeskSpriteManifest.Root + "/Resources/DeskSprites.asset";

        [MenuItem("Corrigible/Desk UI/Sprite kaydını güncelle")]
        public static void Build()
        {
            var reg = AssetDatabase.LoadAssetAtPath<DeskSprites>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<DeskSprites>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }

            var list = new List<DeskSprites.Entry>();
            foreach (var e in DeskSpriteManifest.All.OrderBy(e => e.Name))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(DeskSpriteManifest.SpritesDir + "/" + e.file);
                if (sp == null) { Debug.LogWarning("[Desk] Sprite yok: " + e.file); continue; }
                var type = e.imageType switch { "sliced" => Image.Type.Sliced, "tiled" => Image.Type.Tiled, _ => Image.Type.Simple };
                list.Add(new DeskSprites.Entry { name = e.Name, sprite = sp, type = type, tint = e.tint });
            }
            reg.sprites = list;

            reg.textures = AssetDatabase.FindAssets("t:Texture2D", new[] { DeskSpriteManifest.TexturesDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(t => t.name).ToList();

            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Desk] Sprite kaydı: {list.Count} sprite, {reg.textures.Count} doku.");
        }
    }
}
