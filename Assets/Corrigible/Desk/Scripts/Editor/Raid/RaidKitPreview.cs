using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Bina kiti önizlemesi (BASKIN-MEKANLARI Katman 3–4, görsel kontrol): baskının tahtasını Play'e girmeden, Edit modunda çizer.
    /// Karolar gerçek prefab'larla kurulur ve tahtanın kullandığı <see cref="RaidTileMask"/> + <see cref="RaidTileSkin"/> ile
    /// giydirilir (tahta ile önizleme ayrışmaz); nesneler <see cref="RaidPieceLibrary"/>'den (yağma hedefi ikmal sandığı), Madenci'nin
    /// açtığı gediklerde kitin molozu (<see cref="RaidPieceLibrary.BreachRubble"/>), birimler
    /// kendi parçalarıyla. Ozalit kâğıt ve kontrplak taban sahnenin malzemeleriyle; kamera kilitli 35°/45° ortografik, ışık Baskin
    /// sahnesinin masa lambası (yumuşak gölge) ve soğuk dolgusu, üç renkli ortam. Her şey HideAndDontSave, 31. katmanda kurulur;
    /// sahnedeki ışıklar çizim boyunca kapanır, ışık ve ortam ayarları finally'de geri yüklenir. Açık sahne kirlenmez.
    /// Olay katmanı, işaretler ve etiketler çizilmez (tahtanın kendisi değil, maketi).
    /// Menü: Corrigible › Baskın › Kit önizleme sayfası. Kodla: <see cref="Render"/>, <see cref="RenderState"/>, <see cref="Page"/>.
    /// </summary>
    public static class RaidKitPreview
    {
        public const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";
        const string Mats = "Assets/Corrigible/Art/Raid/Analyst/Materials/";
        const string Meshes = "Assets/Corrigible/Art/Raid/Analyst/Meshes/";
        /// <summary>Baskın kamerasının 1080p ölçeği (tokens_sade.json › raid.camera.pixelsPerUnitAt1080; raid_tokens.json › camera'daki 88,388 oyunda okunmaz, Soru 7).</summary>
        public const float GamePixelsPerUnit = 132.58f;
        const int Layer = 31;

        /// <summary>Çizim ayarları. Varsayılan: bütün tahta, oyunun 1080p ölçeğinde (1600 × 1000).</summary>
        public sealed class Options
        {
            public int Width = 1600, Height = 1000;
            /// <summary>Ekran pikseli / dünya birimi (oyunda 132,58; yakın çekim için büyüt).</summary>
            public float PixelsPerUnit = GamePixelsPerUnit;
            /// <summary>Kadrajın ortası, kare koordinatında (x, y; ör. (3.5, 3.5) tahtanın ortası). Null: tahtanın ortası.</summary>
            public Vector2? Focus;
            /// <summary>Kadraj ortasının yüksekliği (dünya birimi).</summary>
            public float FocusHeight = 0.2f;
            public bool Objects = true, Units = true, Shadows = true;
            /// <summary>Kenar yumuşatma: ss × ss örnekle çizilip küçültülür (ayrıca MSAA 4).</summary>
            public int Supersample = 2;
            /// <summary>Kâğıdın dışındaki zemin (keçe, raid_tokens.json › felt).</summary>
            public Color Background = new Color32(0x1C, 0x37, 0x2C, 0xFF);
        }

        /// <summary>Örnek: eğitim baskını (tohum 129, Batı Avrupa, yağma; "Ana sunucu salonu", MapSeed 0 → varsayılan kit).</summary>
        public static RaidParams TutorialParams() => new RaidParams
        {
            Seed = 129, Region = "weu", RegionName = GameData.Region("weu").Name, Compute = 4, RaidNumber = 1, Tier = 1,
            MissionKind = "loot", SectorId = "weu_1", Traits = new List<string> { "guvenli" }, Signature = "zdron",
        };

        /// <summary>
        /// Harekât havuzundan bir harita (MapSeed ≠ 0): <paramref name="kind"/> türünün (<see cref="RaidMaps.Kinds"/>) havuzdaki
        /// <paramref name="nth"/>. haritasına düşen ilk baskın sırası. Aynı tohum ve sıra her bölgede aynı haritayı verir (kit sayfası).
        /// </summary>
        public static RaidParams PoolParams(uint mapSeed, string kind, string region = "weu", int nth = 0)
        {
            var pool = RaidMaps.Pool(mapSeed);
            int seen = 0;
            for (int slot = 1; slot <= RaidMaps.PoolSize * 4; slot++)
            {
                var map = pool[RaidMaps.Slot(mapSeed, slot)];
                if (RaidMaps.KindOf(map.Name) != kind) continue;
                if (seen++ < nth) continue;
                var r = GameData.Region(region);
                return new RaidParams
                {
                    Seed = 1000 + slot, Region = region, RegionName = r.Name, Compute = 4, RaidNumber = slot, Tier = 2, MissionKind = "server",
                    Traits = new List<string> { r.Trait }, Signature = r.Enemy, MapSeed = mapSeed, MapSlot = slot,
                };
            }
            throw new ArgumentException("havuzda bu türden harita yok: " + kind);
        }

        /// <summary>Baskını kurar (<see cref="Baskin.Create"/>) ve çizer. <paramref name="kitId"/> boşsa kit oyundaki gibi seçilir
        /// (<see cref="RaidPieceLibrary.KitFor"/>: MapSeed 0 → varsayılan); doluysa o kimlik zorlanır (kitOverride gibi).
        /// Yazılan PNG'nin tam yolunu döndürür.</summary>
        public static string Render(RaidParams p, string kitId = null, string path = "Temp/raid_kit.png", Options options = null)
            => RenderState(Baskin.Create(p), kitId, path, options);

        /// <summary>Verilen durumu çizer (karolar, raf bayrakları, nesneler, birimler); <see cref="Render"/> gibi.</summary>
        public static string RenderState(Baskin.BState B, string kitId = null, string path = "Temp/raid_kit.png", Options options = null)
            => RenderKit(B, ResolveKit(B, kitId), path, options);

        /// <summary>Durumu verilen kit nesnesiyle çizer (kütüphaneye bağlanmamış, yeni kurulan bir kit de olabilir; null: karoların
        /// kendi varsayılanı). Yazılan PNG'nin tam yolunu döndürür.</summary>
        public static string RenderKit(Baskin.BState B, RaidRoomKit kit, string path = "Temp/raid_kit.png", Options options = null)
        {
            var o = options ?? new Options();
            var px = Draw(B, kit, o);
            Write(path, px, o.Width, o.Height);
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Kit sayfası: aynı harita (aynı durum) her kitle çizilir, ızgarada yan yana (soldan sağa, yukarıdan aşağı <paramref name="kitIds"/>
        /// sırası); ayrıca gri tonlu kopyası (<c>…_gray.png</c>): hedef, tehdit, sivil ve kanal renksiz de ayrılabilmeli.
        /// <paramref name="columns"/> ≤ 0: kareköke yakın. Hücre boyu <paramref name="options"/>'tan (varsayılan 1200 × 760, 100 px/birim).
        /// Renkli sayfanın tam yolunu ve kit sırasını döndürür.
        /// </summary>
        public static string Page(RaidParams p, IList<string> kitIds, string path = "Temp/raid_kit_page.png", int columns = 0, Options options = null)
        {
            var B = Baskin.Create(p);
            return PageKits(B, kitIds.Select(id => ResolveKit(B, id)).ToList(), path, columns, options) + " · " + string.Join(", ", kitIds);
        }

        /// <summary><see cref="Page"/> verilen durum ve kit nesneleriyle (kütüphaneye bağlanmamış kitler de); sıra listedeki gibi.</summary>
        public static string PageKits(Baskin.BState B, IList<RaidRoomKit> kits, string path = "Temp/raid_kit_page.png", int columns = 0, Options options = null)
        {
            var o = options ?? new Options { Width = 1200, Height = 760, PixelsPerUnit = 100 };
            int n = kits.Count, cols = columns > 0 ? columns : Mathf.CeilToInt(Mathf.Sqrt(n)), rows = Mathf.CeilToInt(n / (float)cols);
            const int gap = 8;
            int pw = cols * o.Width + (cols + 1) * gap, ph = rows * o.Height + (rows + 1) * gap;
            var page = new Color[pw * ph];
            var paper = new Color(0.12f, 0.12f, 0.12f, 1);
            for (int i = 0; i < page.Length; i++) page[i] = paper;
            for (int i = 0; i < n; i++)
            {
                var px = Draw(B, kits[i], o);
                int cx = gap + (i % cols) * (o.Width + gap), cy = gap + (i / cols) * (o.Height + gap);
                // Hücreler yukarıdan aşağı: sayfa satırı (alttan) = ph − 1 − (cy + y)
                for (int y = 0; y < o.Height; y++)
                    Array.Copy(px, (o.Height - 1 - y) * o.Width, page, (ph - 1 - (cy + y)) * pw + cx, o.Width);
            }
            Write(path, page, pw, ph);
            var gray = page.Select(c => { float g = c.grayscale; return new Color(g, g, g, 1); }).ToArray();
            string grayPath = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + "_gray.png");
            Write(grayPath, gray, pw, ph);
            return Path.GetFullPath(path) + " · " + Path.GetFullPath(grayPath) + " · " + B.Layout;
        }

        /// <summary>Menü: harekât havuzunun şebeke haritası, varsayılan kit ve kütüphanedeki bütün kitlerle (Temp/raid_kit_page.png).</summary>
        [MenuItem("Corrigible/Baskın/Kit önizleme sayfası")]
        public static string MenuPage()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            var ids = new List<string> { "default" };
            if (lib != null && lib.kits != null) ids.AddRange(lib.kits.Where(k => k != null).Select(k => k.id));
            var result = Page(PoolParams(20261102, RaidMaps.Grid), ids);
            Debug.Log("Kit önizleme sayfası: " + result);
            return result;
        }

        static RaidRoomKit ResolveKit(Baskin.BState B, string kitId)
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            if (lib == null) return null;
            return string.IsNullOrEmpty(kitId) ? lib.KitFor(B.Params?.Region, B.Params?.MapSeed ?? 0) : lib.KitById(kitId);
        }

        /// <summary>Maketi kurar ve çizer; pikseller alttan üste (Texture2D sırası), boyut o.Width × o.Height.</summary>
        static Color[] Draw(Baskin.BState B, RaidRoomKit kit, Options o)
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            if (lib == null) throw new InvalidOperationException("parça kütüphanesi yok: " + LibraryPath);
            var root = new GameObject("Raid kit preview") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0, -500, 0);
            var origin = root.transform.position;
            var lights = new List<Light>();
            var saved = (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor,
                RenderSettings.ambientIntensity, RenderSettings.sun, RenderSettings.fog);
            RenderTexture rt = null;
            Texture2D read = null;
            try
            {
                Board(root.transform, lib);
                // Karolar: tahtanın BuildTile'ı gibi (zemin karosu yalnız çarpıştırıcı, çizilmez), giydirme RaidTileMask + RaidTileSkin
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        string t = Baskin.CellAt(B, x, y).T;
                        var prefab = t == "wall" ? lib.wall : t == "cable" ? lib.cable : null;
                        if (prefab == null) continue;
                        var tile = UnityEngine.Object.Instantiate(prefab, origin + RaidBoard3D.W(x, y), Quaternion.identity, root.transform);
                        tile.name = $"Tile {x},{y}";
                        var skin = tile.GetComponent<RaidTileSkin>();
                        if (skin != null) skin.Apply(RaidTileMask.For(B, x, y), kit);
                    }
                if (o.Objects) { Objects(B, lib, kit, root.transform, origin); Breaches(B, lib, kit, root.transform, origin); }
                if (o.Units) Units(B, lib, root.transform, origin);

                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(root.transform, false);
                cam.orthographic = true;
                cam.transform.rotation = Quaternion.Euler(35, 45, 0);
                var focus = o.Focus ?? new Vector2(3.5f, 3.5f);
                var look = origin + RaidBoard3D.W(focus.x, focus.y) + Vector3.up * o.FocusHeight;
                cam.transform.position = look - cam.transform.forward * 20;
                cam.orthographicSize = o.Height / 2f / o.PixelsPerUnit;
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 60;
                cam.cullingMask = 1 << Layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = o.Background;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderShadows = o.Shadows;
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;

                // Baskin sahnesinin ışığı (Desk Lamp, Cool Fill: sahnedeki dönüşler), lambada yumuşak gölge
                Light Lamp(string name, Color c, float intensity, Quaternion rotation, bool shadows)
                {
                    var l = new GameObject(name).AddComponent<Light>();
                    l.transform.SetParent(root.transform, false);
                    l.gameObject.layer = Layer;
                    l.type = LightType.Directional; l.color = c; l.intensity = intensity; l.transform.rotation = rotation;
                    l.cullingMask = 1 << Layer;
                    l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                    l.shadowStrength = 0.55f; l.shadowBias = 0.05f; l.shadowNormalBias = 0.4f;
                    return l;
                }
                var lamp = Lamp("Desk Lamp", new Color(1f, 0.8901961f, 0.6901961f), 1.15f, new Quaternion(0.3904484f, 0.34682903f, -0.16172904f, 0.8373192f), o.Shadows);
                Lamp("Cool Fill", new Color(0.62352943f, 0.7058824f, 0.78431374f), 0.35f, new Quaternion(-0.13088542f, 0.8681628f, -0.3159854f, -0.35960478f), false);
                lights.AddRange(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.transform.root != root.transform && l.enabled));
                foreach (var l in lights) l.enabled = false;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.2182353f, 0.24705882f, 0.27450982f);
                RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
                RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
                RenderSettings.ambientIntensity = 1;
                RenderSettings.sun = lamp;
                RenderSettings.fog = false;
                // Maketin bütün parçaları yalıtılmış katmanda ve sahneye kaydedilmez
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = Layer; t.gameObject.hideFlags = HideFlags.HideAndDontSave; }

                int ss = Mathf.Max(1, o.Supersample), w = o.Width * ss, h = o.Height * ss;
                rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 24) { msaaSamples = 4, sRGB = true });
                cam.targetTexture = rt;
                cam.Render();
                read = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                read.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;
                var src = read.GetPixels();
                var px = new Color[o.Width * o.Height];
                for (int y = 0; y < o.Height; y++)
                    for (int x = 0; x < o.Width; x++)
                    {
                        var c = Color.clear;
                        for (int j = 0; j < ss; j++)
                            for (int i = 0; i < ss; i++) c += src[(y * ss + j) * w + x * ss + i];
                        c /= ss * ss;
                        c.a = 1;
                        px[y * o.Width + x] = c;
                    }
                return px;
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (read != null) UnityEngine.Object.DestroyImmediate(read);
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var l in lights) if (l != null) l.enabled = true;
                (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor,
                    RenderSettings.ambientIntensity, RenderSettings.sun, RenderSettings.fog) = saved;
            }
        }

        /// <summary>Masa: kontrplak taban ve ozalit kâğıt (Baskin.unity › BoardBase'in konum ve ölçüleri).</summary>
        static void Board(Transform root, RaidPieceLibrary lib)
        {
            var box = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "UnitBox.asset");
            Material M(string name) => AssetDatabase.LoadAssetAtPath<Material>(Mats + name + ".mat");
            var plyWest = M("PlywoodWest"); var plySouth = M("PlywoodSouth");
            Part(root, "Plywood", box, new Vector3(0, -0.125f, 0), new Vector3(8.6f, 0.25f, 8.6f), plyWest, plySouth, plyWest, plySouth, M("PlywoodTop"), plySouth);
            Part(root, "Ozalit", AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "PaperPlane.asset"), new Vector3(0, 0.001f, 0), new Vector3(8.5f, 1, 8.5f), M("Ozalit"));
        }

        static void Part(Transform root, string name, Mesh mesh, Vector3 position, Vector3 scale, params Material[] materials)
        {
            if (mesh == null) return;
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = materials;
        }

        /// <summary>Nesneler tahtanın NewObjectPiece'i gibi: yağma görevinde hedef ikmal sandığı, olay nesnesinin yedeği ölçekli.</summary>
        static void Objects(Baskin.BState B, RaidPieceLibrary lib, RaidRoomKit kit, Transform root, Vector3 origin)
        {
            bool loot = Baskin.IsLoot(B);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var o = Baskin.CellAt(B, x, y).O;
                    if (o == null) continue;
                    GameObject go;
                    if (loot && Baskin.IsTarget(o.K) && lib.lootCrate != null) go = UnityEngine.Object.Instantiate(lib.lootCrate.gameObject, origin + RaidBoard3D.W(x, y), Quaternion.identity, root);
                    else if (o.K == "enkaz" && !lib.HasOwnPiece(o.K) && lib.rubble != null)
                    {
                        go = UnityEngine.Object.Instantiate(lib.rubble, origin + RaidBoard3D.W(x, y), Quaternion.Euler(0, (x * 37 + y * 53) % 360, 0), root);
                        go.transform.localScale = lib.FallbackScale(o.K);
                    }
                    else
                    {
                        var piece = o.K=="civ"&&kit?.civilianModel!=null?kit.civilianModel:lib.Object(o.K);
                        if (piece == null) continue;
                        go = UnityEngine.Object.Instantiate(piece.gameObject, origin + RaidBoard3D.W(x, y), Quaternion.identity, root);
                        if (!lib.HasOwnPiece(o.K)) go.transform.localScale = lib.FallbackScale(o.K);
                    }
                    go.name = $"{o.K} {x},{y}";
                    HideLabels(go);
                }
        }

        /// <summary>Gedik molozu tahtanın Sync'i gibi (<see cref="RaidPieceLibrary.BreachRubble"/>): Madenci'nin yıktığı ve hâlâ zemin olan
        /// her karede kitin molozu, yaw (k × 41) mod 360; kitsiz çizimde varsayılan kitin yedeği.</summary>
        static void Breaches(Baskin.BState B, RaidPieceLibrary lib, RaidRoomKit kit, Transform root, Vector3 origin)
        {
            var prefab = lib.BreachRubble(kit != null ? kit : lib.defaultKit, out var scale);
            if (prefab == null || B.Rubble == null) return;
            foreach (var p in B.Rubble)
            {
                if (Baskin.CellAt(B, p.X, p.Y).T == "wall") continue;
                int k = p.Y * Baskin.N + p.X;
                var go = UnityEngine.Object.Instantiate(prefab, origin + RaidBoard3D.W(p.X, p.Y), Quaternion.Euler(0, lib.BreachRubbleYaw(kit,k), 0), root);
                go.name = $"Breach rubble {p.X},{p.Y}";
                go.transform.localScale = scale;
                foreach (var c in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            }
        }

        /// <summary>Birimler tahtanın Sync'i gibi: ekip 45°, makineler −135°; kabindeki ve tahtadan çıkan çizilmez.</summary>
        static void Units(Baskin.BState B, RaidPieceLibrary lib, Transform root, Vector3 origin)
        {
            foreach (var u in B.Units)
            {
                if (u.Out || u.Cab || (u.Dead && u.Side != 'p')) continue;
                var piece = lib.Unit(u.Type);
                if (piece == null) continue;
                var go = UnityEngine.Object.Instantiate(piece.gameObject, origin + RaidBoard3D.W(u.X, u.Y), Quaternion.Euler(0, u.Side == 'p' ? 45 : -135, 0), root);
                go.name = $"{u.Type} #{u.Id}";
                HideLabels(go);
            }
        }

        static void HideLabels(GameObject go)
        {
            var labels = go.transform.Find("Labels");
            if (labels != null) labels.gameObject.SetActive(false);
        }

        static void Write(string path, Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try
            {
                tex.SetPixels(px);
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }
    }
}
