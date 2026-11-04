using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Bina kitlerinin Aşama A dokuları (BASKIN-MEKANLARI Katman 3–4): her kit için duvar bloğunun yan yüzü (512 × 160,
    /// <c>kit_&lt;id&gt;_side.png</c>), ayrık bloğun dik istifi (512 × 160, <c>_column</c>), üst yüz (256², <c>_top</c>), kanalın
    /// oluk tabanı (256², <c>_channel</c>, karolar arasında dikişsiz) ve tek kanal karesinin kuyusu (256², <c>_well</c>). Dokular
    /// <c>Art/Raid/Kits/&lt;id&gt;/</c>'ye yazılır; aynı klasöre URP Lit malzemeleri (WallWest/Trench kopyası, _BaseMap dokusu,
    /// _BaseColor beyaz ya da güney yüzünde parlaklık tonu), <c>Settings/Kits/Kit_&lt;id&gt;.asset</c> kiti kurulur ya da güncellenir
    /// ve kit renk kapısından (<see cref="RaidKitGate"/>) geçerse <see cref="RaidPieceLibrary.kits"/>'e bağlanır; geçemeyen kit
    /// kütüphaneden çıkar. Kitin molozu (<c>wallRubble</c>) ve köprü ayarı elle değiştirilmişse korunur.
    /// <para>Renkler <c>raid_tokens.json › kits.&lt;id&gt;</c>'den (<see cref="RaidTheme.Active"/>): motif dokuyu bu renklerle
    /// çizer, sonra dokunun doğrusal ortalaması yüzün token rengine eşitlenir (yan ve dik istif → wallWest, üst → wallTop, kanal →
    /// channel, kuyu → well). Kapı yüzün ortalamasını ölçtüğü için token rengi doğrudan kapının girdisidir.</para>
    /// <para>Rastgelelik kit kimliğinden türeyen sabit tohumla (her doku kendi alt tohumuyla: bir kiti ya da bir dokuyu yeniden
    /// üretmek ötekileri değiştirmez); UnityEngine.Random kullanılmaz. Yazı basılmaz. Yeni kit: <see cref="Motifs"/>'e bir satır
    /// ve beş motif işlevi (RaidKitTextureBaker.Motifs.cs), tokenlara <c>kits.&lt;id&gt;</c>.</para>
    /// Menü: Corrigible › Baskın › Kit dokularını üret.
    /// </summary>
    public static partial class RaidKitTextureBaker
    {
        public const string OutRoot = "Assets/Corrigible/Art/Raid/Kits";
        public const string KitDir = "Assets/Corrigible/Settings/Kits";
        public const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";
        const string Templates = "Assets/Corrigible/Art/Raid/Analyst/Materials/";

        /// <summary>Doku boyutları: yan yüz ve dik istif 512 × 160 (u yüz boyunca, v = y / 0,547), üst, kanal, kuyu 256².</summary>
        public const int SideW = 512, SideH = 160, SquareSize = 256;

        /// <summary>Yan dokuda katların satırları (üstten): üst kat y 0,278–0,547, alt kat y 0,003–0,272 (WallBlock).</summary>
        public const float UpperTop = 0f, UpperBottom = 78.7f, LowerTop = 80.4f, LowerBottom = 159.1f;

        public enum Sheet { Side, Column, Top, Channel, Well }
        static readonly string[] SheetNames = { "side", "column", "top", "channel", "well" };

        /// <summary>Kitin token anahtarları (<c>kits.&lt;id&gt;.&lt;anahtar&gt;</c>); motiflerin hepsi bunları okur.</summary>
        public static readonly string[] TokenKeys = { "wallTop", "wallWest", "wallSouth", "detailA", "detailB", "channel", "channelDetail", "well", "wellDetail" };

        public static string SheetName(Sheet s) => SheetNames[(int)s];
        public static string TexturePath(string id, Sheet s) => $"{OutRoot}/{id}/kit_{id}_{SheetName(s)}.png";
        public static string MaterialPath(string id, string part) => $"{OutRoot}/{id}/Kit_{id}_{part}.mat";
        public static string KitPath(string id) => $"{KitDir}/Kit_{id}.asset";
        public static (int w, int h) SheetSize(Sheet s) => s == Sheet.Side || s == Sheet.Column ? (SideW, SideH) : (SquareSize, SquareSize);

        /// <summary>Motifi çizilen kitler (sırası kütüphanedeki sıra).</summary>
        public static string[] KitIds => Motifs.Select(m => m.Id).ToArray();

        // ---------------------------------------------------------------- menü ve üretim

        [MenuItem("Corrigible/Baskın/Kit dokularını üret")]
        public static string BakeAll()
        {
            var result = Bake(KitIds);
            Debug.Log("[Kit] " + result);
            return result;
        }

        /// <summary>
        /// Verilen kitlerin dokularını, malzemelerini ve kit asset'lerini üretir; kapıdan geçenleri kütüphaneye bağlar. Her kit için
        /// kapının özetini (yüz başına L*, C*, en yakın ahşap ve ayrılmış renge ΔE, desen ΔL*) ve kapı hatalarını döndürür.
        /// </summary>
        public static string Bake(params string[] ids)
        {
            var report = new List<string>();
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            if (lib == null) return "parça kütüphanesi yok: " + LibraryPath;
            var made = new List<string>();
            foreach (var id in ids)
            {
                var motif = MotifOf(id);
                if (motif == null) { report.Add(id + ": motifi yok"); continue; }
                var palette = new Palette(id);
                var sheets = new Dictionary<Sheet, Canvas>();
                foreach (Sheet s in Enum.GetValues(typeof(Sheet))) sheets[s] = Draw(motif, palette, s);
                if (palette.Missing.Count > 0) { report.Add(id + ": token yok: " + string.Join(", ", palette.Missing.Distinct().Select(k => "kits." + id + "." + k))); continue; }
                Directory.CreateDirectory(OutRoot + "/" + id);
                foreach (var kv in sheets) WritePng(TexturePath(id, kv.Key), kv.Value);
                made.Add(id);
            }
            AssetDatabase.Refresh();
            foreach (var id in made)
                foreach (Sheet s in Enum.GetValues(typeof(Sheet)))
                    AssetDatabase.ImportAsset(TexturePath(id, s), ImportAssetOptions.ForceUpdate);

            var kits = new List<RaidRoomKit>();
            foreach (var id in made) kits.Add(BuildKit(MotifOf(id), new Palette(id)));
            AssetDatabase.SaveAssets();

            var linked = (lib.kits ?? new RaidRoomKit[0]).Where(k => k != null).ToList();
            foreach (var kit in kits)
            {
                var failures = RaidKitGate.Check(kit, lib);
                report.Add(RaidKitGate.Report(kit));
                report.Add("  desen sıkıştırma: " + string.Join(", ", new[] { Sheet.Side, Sheet.Column, Sheet.Top }
                    .Select(s => SheetName(s) + " " + (compression.TryGetValue(kit.id + "_" + SheetName(s), out var k) ? k.ToString("F2") : "?"))));
                linked.RemoveAll(k => k == kit || k.id == kit.id);
                if (failures.Count == 0) linked.Add(kit);
                else report.Add(kit.id + ": KAPIDAN GEÇMEDİ, kütüphaneye bağlanmadı: " + string.Join("; ", failures));
            }
            // Kütüphane sırası: motif tablosunun sırası, motifi olmayan kitler sonda
            var order = KitIds.ToList();
            linked = linked.OrderBy(k => order.IndexOf(k.id) < 0 ? int.MaxValue : order.IndexOf(k.id)).ToList();
            if (!linked.SequenceEqual(lib.kits ?? new RaidRoomKit[0]))
            {
                lib.kits = linked.ToArray();
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
            }
            return made.Count + " kit üretildi (" + string.Join(", ", made) + ")\n" + string.Join("\n", report);
        }

        /// <summary>Bir dokuyu dosyaya yazmadan çizer (testler: diskteki dokunun güncel olduğunu denetler). Pikseller üstten alta.</summary>
        public static Color32[] Render(string id, Sheet sheet)
        {
            var motif = MotifOf(id) ?? throw new ArgumentException("motifi olmayan kit: " + id);
            var palette = new Palette(id);
            var c = Draw(motif, palette, sheet);
            if (palette.Missing.Count > 0) throw new InvalidOperationException("token yok: " + string.Join(", ", palette.Missing.Distinct()));
            return c.Px.Select(p => (Color32)p).ToArray();
        }

        static Motif MotifOf(string id) => Motifs.FirstOrDefault(m => m.Id == id);

        /// <summary>Duvar dokularının desen kontrastı (L* %5–%95) bu sınıra sıkıştırılır; kapının sınırı 15 (<see cref="RaidKitGate.PatternDeltaLMax"/>).</summary>
        public const float PatternTarget = 14f;

        /// <summary>Son üretimde dokuların desen sıkıştırma çarpanı (1: motif zaten sınırın içinde); rapor için.</summary>
        static readonly Dictionary<string, float> compression = new Dictionary<string, float>();

        static Canvas Draw(Motif motif, Palette p, Sheet sheet)
        {
            var (w, h) = SheetSize(sheet);
            seed = SheetSeed(motif.Id, sheet);
            var c = new Canvas(w, h, Color.gray) { Wrap = sheet == Sheet.Channel || sheet == Sheet.Well };
            Color target;
            switch (sheet)
            {
                case Sheet.Side: motif.Side(c, p); target = p.West; break;
                case Sheet.Column: motif.Column(c, p); target = p.West; break;
                case Sheet.Top: motif.Top(c, p); target = p.Top; break;
                case Sheet.Channel: motif.Channel(c, p); target = p.Channel; break;
                default: motif.Well(c, p); target = p.Well; break;
            }
            bool wall = sheet == Sheet.Side || sheet == Sheet.Column || sheet == Sheet.Top;
            float k = 1;
            // Duvar yüzü: desen sınırına sıkıştır, ortalamayı eşitle (eşitleme kontrastı biraz oynatır: birkaç tur)
            for (int pass = 0; pass < 4; pass++)
            {
                if (wall) k *= LimitPattern(c, PatternTarget);
                Normalise(c, target);
                if (!wall || PatternRange(c).range <= PatternTarget + 0.25f) break;
            }
            compression[motif.Id + "_" + SheetName(sheet)] = k;
            return c;
        }

        /// <summary>L* dağılımının %5, %50 ve %95'i.</summary>
        static (float low, float mid, float high, float range) PatternRange(Canvas c)
        {
            var ls = c.Px.Select(px => RaidKitGate.Lab(px).x).ToArray();
            Array.Sort(ls);
            int n = ls.Length;
            float lo = ls[Mathf.FloorToInt(0.05f * (n - 1))], mid = ls[n / 2], hi = ls[Mathf.CeilToInt(0.95f * (n - 1))];
            return (lo, mid, hi, hi - lo);
        }

        /// <summary>
        /// Desen kontrastı sınırı aşıyorsa her pikselin L*'ını ortancaya doğru aynı oranda yaklaştırır (renk tonu korunur: doğrusal RGB
        /// parlaklık oranıyla ölçeklenir). Uygulanan çarpanı döndürür.
        /// </summary>
        static float LimitPattern(Canvas c, float limit)
        {
            var (_, mid, _, range) = PatternRange(c);
            if (range <= limit) return 1;
            float k = limit / range * 0.98f;
            for (int i = 0; i < c.Px.Length; i++)
            {
                var lin = c.Px[i].linear;
                float L = RaidKitGate.Lab(c.Px[i]).x, target = mid + (L - mid) * k;
                float y0 = LabY(L), y1 = LabY(target);
                float s = y0 > 1e-6f ? y1 / y0 : 1;
                c.Px[i] = new Color(Mathf.Clamp01(lin.r * s), Mathf.Clamp01(lin.g * s), Mathf.Clamp01(lin.b * s), 1).gamma;
            }
            return k;
        }

        /// <summary>CIE L* → göreli parlaklık Y.</summary>
        static float LabY(float L) => L > 8 ? Mathf.Pow((L + 16) / 116f, 3) : L / 903.3f;

        /// <summary>Kitin malzemeleri ve asset'i (varsa yerinde güncellenir; GUID'ler korunur).</summary>
        static RaidRoomKit BuildKit(Motif motif, Palette p)
        {
            string id = motif.Id;
            var wallTemplate = AssetDatabase.LoadAssetAtPath<Material>(Templates + "WallWest.mat");
            var trenchTemplate = AssetDatabase.LoadAssetAtPath<Material>(Templates + "Trench.mat");
            Texture2D Tex(Sheet s) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(id, s));
            var south = SouthTint(p.West, p.South);

            var kit = AssetDatabase.LoadAssetAtPath<RaidRoomKit>(KitPath(id));
            if (kit == null)
            {
                Directory.CreateDirectory(KitDir);
                kit = ScriptableObject.CreateInstance<RaidRoomKit>();
                kit.bridges = true;
                AssetDatabase.CreateAsset(kit, KitPath(id));
            }
            kit.id = id;
            kit.wallTop = KitMaterial(id, "WallTop", wallTemplate, Tex(Sheet.Top), Color.white, 0.1f);
            kit.wallWest = KitMaterial(id, "WallWest", wallTemplate, Tex(Sheet.Side), Color.white, 0.1f);
            kit.wallSouth = KitMaterial(id, "WallSouth", wallTemplate, Tex(Sheet.Side), south, 0.1f);
            kit.columnWest = KitMaterial(id, "ColumnWest", wallTemplate, Tex(Sheet.Column), Color.white, 0.1f);
            kit.columnSouth = KitMaterial(id, "ColumnSouth", wallTemplate, Tex(Sheet.Column), south, 0.1f);
            kit.channelFloor = KitMaterial(id, "Channel", trenchTemplate, Tex(Sheet.Channel), Color.white, motif.ChannelSmoothness);
            kit.wellFloor = KitMaterial(id, "Well", trenchTemplate, Tex(Sheet.Well), Color.white, motif.WellSmoothness);
            EditorUtility.SetDirty(kit);
            return kit;
        }

        static Material KitMaterial(string id, string part, Material template, Texture2D texture, Color baseColor, float smoothness)
        {
            string path = MaterialPath(id, part);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(template);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = template.shader;
            m.SetTexture("_BaseMap", texture);
            m.SetTexture("_MainTex", texture);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0);
            m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Güney yüzünün tonu: doğrusal uzayda güney / batı (batı dokusunun ortalaması batı rengine eşit; sonuç güney rengi).</summary>
        public static Color SouthTint(Color west, Color south)
        {
            var w = west.linear; var s = south.linear;
            return new Color(Mathf.Clamp01(s.r / Mathf.Max(1e-4f, w.r)), Mathf.Clamp01(s.g / Mathf.Max(1e-4f, w.g)), Mathf.Clamp01(s.b / Mathf.Max(1e-4f, w.b))).gamma;
        }

        /// <summary>
        /// İçe aktarma ayarları (<see cref="DeskSpriteImporter"/> bu klasör için çağırır): Default, sRGB, mipmap, Repeat, bilinear,
        /// aniso 4, sıkıştırmasız, NPOT olduğu gibi (160 satır), alfa yok.
        /// </summary>
        public static void ApplyImport(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.anisoLevel = 4;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false;
            ti.isReadable = false;
            ti.maxTextureSize = 512;
        }

        // ---------------------------------------------------------------- motif tablosu

        /// <summary>Kitin çizimi: beş dokunun motif işlevi ve kanal malzemelerinin parlaklığı (su 0,35).</summary>
        sealed class Motif
        {
            public readonly string Id;
            public readonly Action<Canvas, Palette> Side, Column, Top, Channel, Well;
            public float ChannelSmoothness = 0.1f, WellSmoothness = 0.1f;
            public Motif(string id, Action<Canvas, Palette> side, Action<Canvas, Palette> column, Action<Canvas, Palette> top,
                Action<Canvas, Palette> channel, Action<Canvas, Palette> well)
            { Id = id; Side = side; Column = column; Top = top; Channel = channel; Well = well; }
        }

        /// <summary>Kitin token renkleri (<c>kits.&lt;id&gt;.*</c>); olmayan anahtar listelenir, macenta döner.</summary>
        sealed class Palette
        {
            readonly string id;
            public readonly List<string> Missing = new List<string>();
            public Palette(string id) { this.id = id; }
            public Color this[string key]
            {
                get
                {
                    var theme = RaidTheme.Active;
                    if (theme != null && theme.TryGet("kits." + id + "." + key, out var c)) return c;
                    Missing.Add(key);
                    return Color.magenta;
                }
            }
            public Color Top => this["wallTop"];
            public Color West => this["wallWest"];
            public Color South => this["wallSouth"];
            public Color A => this["detailA"];
            public Color B => this["detailB"];
            public Color Channel => this["channel"];
            public Color ChannelDetail => this["channelDetail"];
            public Color Well => this["well"];
            public Color WellDetail => this["wellDetail"];
        }

        // ---------------------------------------------------------------- tohum (SiginakTextureBaker › lib.js rnd())

        static long seed;
        static float Rnd() { seed = seed * 16807 % 2147483647; return (seed - 1) / 2147483646f; }
        static float Rnd(float a, float b) => a + (b - a) * Rnd();
        static int RndInt(int a, int bExclusive) => Mathf.Min(bExclusive - 1, a + (int)(Rnd() * (bExclusive - a)));

        /// <summary>Kitin ve dokunun tohumu: kimliğin FNV-1a özeti + dokunun sırası (1 … 2147483646).</summary>
        static long SheetSeed(string id, Sheet sheet)
        {
            uint h = 2166136261;
            foreach (char ch in id) { h ^= ch; h *= 16777619; }
            long s = ((long)h + (int)sheet * 104729L) % 2147483646L;
            return s + 1;
        }

        // ---------------------------------------------------------------- ortalama eşitleme

        /// <summary>Dokunun doğrusal ortalamasını hedef renge eşitler (kanal başına çarpan; kırpılan pikseller için birkaç tur).</summary>
        static void Normalise(Canvas c, Color target)
        {
            var t = target.linear;
            var lin = c.Px.Select(p => p.linear).ToArray();
            for (int pass = 0; pass < 6; pass++)
            {
                double r = 0, g = 0, b = 0;
                foreach (var p in lin) { r += p.r; g += p.g; b += p.b; }
                int n = lin.Length;
                float sr = t.r / Mathf.Max(1e-6f, (float)(r / n)), sg = t.g / Mathf.Max(1e-6f, (float)(g / n)), sb = t.b / Mathf.Max(1e-6f, (float)(b / n));
                if (Mathf.Abs(sr - 1) < 1e-4f && Mathf.Abs(sg - 1) < 1e-4f && Mathf.Abs(sb - 1) < 1e-4f) break;
                for (int i = 0; i < n; i++)
                {
                    var p = lin[i];
                    lin[i] = new Color(Mathf.Clamp01(p.r * sr), Mathf.Clamp01(p.g * sg), Mathf.Clamp01(p.b * sb), 1);
                }
            }
            for (int i = 0; i < lin.Length; i++) c.Px[i] = lin[i].gamma;
        }

        static void WritePng(string path, Canvas c)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
            try
            {
                var flipped = new Color[c.Px.Length];
                for (int y = 0; y < c.H; y++) Array.Copy(c.Px, y * c.W, flipped, (c.H - 1 - y) * c.W, c.W);
                tex.SetPixels(flipped);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }

        // ---------------------------------------------------------------- tuval (SiginakTextureBaker.Canvas kopyası + eklemeler)

        /// <summary>
        /// Sol-üst orijinli RGBA tuval (sRGB); kaydederken satırlar ters çevrilir. <see cref="Wrap"/> açıkken kenardan taşan çizim karşı
        /// kenara sarar (kanal ve kuyu dokusu karolar arasında dikişsiz).
        /// </summary>
        sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public bool Wrap;
            public Canvas(int w, int h, Color fill) { W = w; H = h; Px = new Color[w * h]; Fill(fill); }

            public void Fill(Color c) { c.a = 1; for (int i = 0; i < Px.Length; i++) Px[i] = c; }

            public void Blend(int x, int y, Color c, float alpha)
            {
                if (Wrap) { x = ((x % W) + W) % W; y = ((y % H) + H) % H; }
                else if (x < 0 || y < 0 || x >= W || y >= H) return;
                if (alpha <= 0) return;
                int i = y * W + x;
                var d = Px[i];
                float a = Mathf.Clamp01(alpha);
                Px[i] = new Color(c.r * a + d.r * (1 - a), c.g * a + d.g * (1 - a), c.b * a + d.b * (1 - a), 1);
            }

            int X0(float x) => Wrap ? Mathf.FloorToInt(x) : Mathf.Max(0, Mathf.FloorToInt(x));
            int X1(float x) => Wrap ? Mathf.CeilToInt(x) : Mathf.Min(W, Mathf.CeilToInt(x));
            int Y0(float y) => Wrap ? Mathf.FloorToInt(y) : Mathf.Max(0, Mathf.FloorToInt(y));
            int Y1(float y) => Wrap ? Mathf.CeilToInt(y) : Mathf.Min(H, Mathf.CeilToInt(y));

            /// <summary>Dikdörtgen; kesirli kenarlarda kapsama oranıyla (ince çizgiler yumuşak).</summary>
            public void Rect(float x, float y, float w, float h, Color c, float alpha)
            {
                if (w <= 0 || h <= 0) return;
                for (int yy = Y0(y); yy < Y1(y + h); yy++)
                {
                    float cy = Mathf.Min(yy + 1, y + h) - Mathf.Max(yy, y);
                    if (cy <= 0) continue;
                    for (int xx = X0(x); xx < X1(x + w); xx++)
                    {
                        float cx = Mathf.Min(xx + 1, x + w) - Mathf.Max(xx, x);
                        if (cx > 0) Blend(xx, yy, c, alpha * cx * cy);
                    }
                }
            }

            /// <summary>Kabartma: üst ve sol kenar açık, alt ve sağ kenar koyu (lamba sol üstten).</summary>
            public void Bevel(float x, float y, float w, float h, float t, float light, float dark)
            {
                Rect(x, y, w, t, Color.white, light);
                Rect(x, y + t, t, h - t, Color.white, light * 0.7f);
                Rect(x, y + h - t, w, t, Color.black, dark);
                Rect(x + w - t, y, t, h - t, Color.black, dark * 0.7f);
            }

            /// <summary>Oyuk (kabartmanın tersi): üst ve sol kenar koyu, alt ve sağ açık.</summary>
            public void Inset(float x, float y, float w, float h, float t, float light, float dark)
            {
                Rect(x, y, w, t, Color.black, dark);
                Rect(x, y + t, t, h - t, Color.black, dark * 0.7f);
                Rect(x, y + h - t, w, t, Color.white, light);
                Rect(x + w - t, y, t, h - t, Color.white, light * 0.7f);
            }

            /// <summary>Silindir gölgesi: eksene dik yönde üçte birde parlak, alt kenarda koyu (yatay boru: dikey profil).</summary>
            public void Cylinder(float x, float y, float w, float h, bool horizontal, float light, float dark)
            {
                float span = horizontal ? h : w;
                int n = Mathf.CeilToInt(span);
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) / span;
                    float shade = Mathf.Cos((t - 0.32f) * Mathf.PI * 1.15f);
                    float a = shade >= 0 ? light * shade : -dark * shade;
                    var col = shade >= 0 ? Color.white : Color.black;
                    if (horizontal) Rect(x, y + i, w, Mathf.Min(1, h - i), col, a);
                    else Rect(x + i, y, Mathf.Min(1, w - i), h, col, a);
                }
            }

            public void Circle(float cx, float cy, float r, Color c, float alpha)
            {
                for (int yy = Mathf.FloorToInt(cy - r); yy <= Mathf.CeilToInt(cy + r); yy++)
                    for (int xx = Mathf.FloorToInt(cx - r); xx <= Mathf.CeilToInt(cx + r); xx++)
                    {
                        float d = Mathf.Sqrt((xx + 0.5f - cx) * (xx + 0.5f - cx) + (yy + 0.5f - cy) * (yy + 0.5f - cy));
                        Blend(xx, yy, c, alpha * Mathf.Clamp01(r - d + 0.5f));
                    }
            }

            /// <summary>Halka: r0 ile r1 arası (kenarları yumuşak).</summary>
            public void Ring(float cx, float cy, float r0, float r1, Color c, float alpha)
            {
                for (int yy = Mathf.FloorToInt(cy - r1); yy <= Mathf.CeilToInt(cy + r1); yy++)
                    for (int xx = Mathf.FloorToInt(cx - r1); xx <= Mathf.CeilToInt(cx + r1); xx++)
                    {
                        float d = Mathf.Sqrt((xx + 0.5f - cx) * (xx + 0.5f - cx) + (yy + 0.5f - cy) * (yy + 0.5f - cy));
                        float cov = Mathf.Clamp01(r1 - d + 0.5f) * Mathf.Clamp01(d - r0 + 0.5f);
                        Blend(xx, yy, c, alpha * cov);
                    }
            }

            /// <summary>Radyal leke: merkezde α, %60'ta α·.45, kenarda 0 (SiginakTextureBaker.Stain).</summary>
            public void Stain(float cx, float cy, float r, Color c, float alpha)
            {
                for (int yy = Mathf.FloorToInt(cy - r); yy <= Mathf.CeilToInt(cy + r); yy++)
                    for (int xx = Mathf.FloorToInt(cx - r); xx <= Mathf.CeilToInt(cx + r); xx++)
                    {
                        float t = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy)) / r;
                        if (t >= 1) continue;
                        float a = t < 0.6f ? Mathf.Lerp(alpha, alpha * 0.45f, t / 0.6f) : Mathf.Lerp(alpha * 0.45f, 0, (t - 0.6f) / 0.4f);
                        Blend(xx, yy, c, a);
                    }
            }

            /// <summary>Dikey doğrusal geçiş: üst kenarda a0, alt kenarda a1.</summary>
            public void VerticalFadeRect(float x, float y, float w, float h, Color c, float a0, float a1)
            {
                int n = Mathf.CeilToInt(h);
                for (int i = 0; i < n; i++) Rect(x, y + i, w, Mathf.Min(1, h - i), c, Mathf.Lerp(a0, a1, (i + 0.5f) / h));
            }

            /// <summary>Kalın düz çizgi (uçları yuvarlak, kenarı yumuşak).</summary>
            public void Line(float x0, float y0, float x1, float y1, float width, Color c, float alpha)
            {
                float r = width / 2, dx = x1 - x0, dy = y1 - y0, len2 = Mathf.Max(1e-6f, dx * dx + dy * dy);
                for (int yy = Mathf.FloorToInt(Mathf.Min(y0, y1) - r - 1); yy <= Mathf.CeilToInt(Mathf.Max(y0, y1) + r + 1); yy++)
                    for (int xx = Mathf.FloorToInt(Mathf.Min(x0, x1) - r - 1); xx <= Mathf.CeilToInt(Mathf.Max(x0, x1) + r + 1); xx++)
                    {
                        float px = xx + 0.5f, py = yy + 0.5f;
                        float t = Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / len2);
                        float qx = x0 + t * dx - px, qy = y0 + t * dy - py;
                        float d = Mathf.Sqrt(qx * qx + qy * qy);
                        Blend(xx, yy, c, alpha * Mathf.Clamp01(r - d + 0.5f));
                    }
            }

            /// <summary>Piksel gürültüsü (±amount/2 / 255, gri).</summary>
            public void Noise(float amount)
            {
                for (int i = 0; i < Px.Length; i++)
                {
                    float n = (Rnd() - 0.5f) * amount / 255f;
                    var p = Px[i];
                    Px[i] = new Color(Mathf.Clamp01(p.r + n), Mathf.Clamp01(p.g + n), Mathf.Clamp01(p.b + n), 1);
                }
            }

            /// <summary>Kaba taneli gürültü: cell × cell bloklar, yumuşak geçişli (mipmap'te de görünen alacalık).</summary>
            public void Grain(float amount, int cell)
            {
                int gw = Mathf.CeilToInt(W / (float)cell) + 1, gh = Mathf.CeilToInt(H / (float)cell) + 1;
                var g = new float[gw * gh];
                for (int i = 0; i < g.Length; i++) g[i] = Rnd() - 0.5f;
                // Sarmalı dokuda son sütun ve satır ilkine eşit: kenarlar dikişsiz
                if (Wrap)
                {
                    int cw = W / cell, ch = H / cell;
                    for (int y = 0; y < gh; y++) for (int x = 0; x < gw; x++) g[y * gw + x] = g[(y % ch) * gw + (x % cw)];
                }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float fx = x / (float)cell, fy = y / (float)cell;
                        int ix = (int)fx, iy = (int)fy;
                        float tx = Smooth(fx - ix), ty = Smooth(fy - iy);
                        float v = Mathf.Lerp(Mathf.Lerp(g[iy * gw + ix], g[iy * gw + ix + 1], tx), Mathf.Lerp(g[(iy + 1) * gw + ix], g[(iy + 1) * gw + ix + 1], tx), ty);
                        float n = v * amount / 255f;
                        var p = Px[y * W + x];
                        Px[y * W + x] = new Color(Mathf.Clamp01(p.r + n), Mathf.Clamp01(p.g + n), Mathf.Clamp01(p.b + n), 1);
                    }
            }

            static float Smooth(float t) => t * t * (3 - 2 * t);

            /// <summary>Rastgele küçük daireler (tane, perçin, kırağı).</summary>
            public void Speckle(float x, float y, float w, float h, int count, float rMin, float rMax, Color c, float aMin, float aMax)
            {
                for (int i = 0; i < count; i++)
                    Circle(x + Rnd() * w, y + Rnd() * h, Rnd(rMin, rMax), c, Rnd(aMin, aMax));
            }
        }
    }
}
