using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 · Aşama 3 kartlı panonun yazısız baskıları (PLAN §3.2–3.5, eleştiri #2 ve #21): pano yüzü
    /// (<c>tx_board_face</c>), iş emri formu (<c>tx_order_sheet</c>), kart tabanları (kişi, üç eşya yuvası, hammadde, fiş, tarif),
    /// kart yüzü malzemeleri (URP Lit) ve kart tuvallerinin sprite'ları (pip, çerçeve, köşe, bant, simge, piktogram).
    /// <list type="bullet">
    /// <item>Tarif paketin <c>threejs/r3/boardtex.js</c> ve <c>dress_room_board.js</c> çizimleridir; ölçüler mm, düzen
    /// <see cref="BoardLayout"/>'tan. Kelime yoktur: yazılar kartın dünya tuvalinde ya da panonun TMP yuvalarındadır.</item>
    /// <item>Rastgelelik tasarımın tohumlu doğrusal üreteciyle (16807, 2³¹ − 1); aynı girdiyle çıktı bayt bayt aynıdır, değişmeyen
    /// dosya yeniden yazılmaz.</item>
    /// <item>Kâğıt dokularının doğrusal albedosu ≤ 0,8 (PROMPT: kâğıtların albedosu ≤ 0,8; eleştiri #21): taban rengi tasarımdaki
    /// gibi çizilir, sonra her pikselin en parlak doğrusal kanalı 0,8'e sınırlanır (ton korunur). Malzeme boyası beyazdır.</item>
    /// <item>Sprite'lar tek, tam dikdörtgen ağ, mipmap'li, kenar kıskaçlı; 9 dilimliler kenar payıyla. Birim:
    /// <c>pixelsPerUnit = 100 × piksel/mm</c>, böylece tuvalin varsayılan <c>referencePixelsPerUnit</c> (100) ile 1 birim = 1 mm
    /// tuvalde doğal boy ve 9 dilim payı mm çıkar.</item>
    /// <item>Renk: sprite'lar beyaz şekildir, rengi <c>Image.color</c> verir (aynı piktogram kartta mürekkep, bantta açık krem
    /// boyanır). Tek istisna <c>tape</c>: iki renkli olduğundan renkli çizilir (boya beyaz).</item>
    /// <item>Aşama 5 (R18a, PLAN §2.1): tarif sayfasının sözcüksüz tabanı (<c>tx_recipe_page</c>, <see cref="BakeRecipePage"/>), defter
    /// kapağının ön dörtgeni (<c>tx_book_cover</c>, <see cref="BakeBookCover"/>), defter malzemeleri (<see cref="BookMaterials"/>) ve
    /// SÜRE satırının saati (<c>icon_clock</c>).</item>
    /// </list>
    /// </summary>
    public static class Rev3BoardTextureBaker
    {
        public const string TextureDir = Rev3TextureBaker.Dir;
        public const string BoardTextureDir = TextureDir + "/board";
        public const string SpriteDir = Rev3ModelImporter.ArtDir + "/Sprites/board";
        public const string MaterialDir = Rev3ModelImporter.ArtDir + "/SceneMaterials/board";
        public const string BoardFacePath = TextureDir + "/tx_board_face.png";
        public const string OrderSheetPath = BoardTextureDir + "/tx_order_sheet.png";
        /// <summary>Panonun model malzemesi (Rev3MaterialSetup aynı dokuyu doku notundan da bağlar).</summary>
        public const string BoardFaceMaterialPath = Rev3MaterialSetup.MaterialsDir + "/boardFace.mat";
        public const string PortraitTemplatePath = MaterialDir + "/Portrait Template.mat";
        public const string OrderSheetMaterialPath = MaterialDir + "/Order Sheet.mat";
        /// <summary>Aşama 5 (R18a, PLAN §2.1): tarif sayfasının sözcüksüz tabanı ve defter kapağının ön dörtgeni.</summary>
        public const string RecipePagePath = BoardTextureDir + "/tx_recipe_page.png";
        public const string BookCoverPath = BoardTextureDir + "/tx_book_cover.png";
        public const string RecipePageLeftMaterialPath = MaterialDir + "/Recipe page L.mat";
        public const string RecipePageRightMaterialPath = MaterialDir + "/Recipe page R.mat";
        public const string BookCoverMaterialPath = MaterialDir + "/Book cover.mat";
        /// <summary>Defter modeli (kapağın UV yönü buradan okunur).</summary>
        public static string RecipeBookPrefabPath => Rev3ModelImporter.PrefabPath("board", "RecipeBook");
        /// <summary>Sayfa (pageFace 0,9 pürüz) ve kapak bezi (bookCloth 0,82 pürüz) pürüzsüzlüğü.</summary>
        public const float PageSmoothness = 0.10f, CoverSmoothness = 0.18f;
        /// <summary>Tarif sayfası 99 × 140 mm, kapağın ön dörtgeni (UV 0–1) 93 × 128 mm; ikisi de 8 piksel/mm.</summary>
        public const float BookPxPerMm = 8;
        public static readonly Vector2 RecipePageMm = new Vector2(99, 140);
        /// <summary>Kapak ön dörtgeninin model sınırı (Cover yereli, mm): x 6…99, y 10…138 (kapak 105 × 148; D §1.1).</summary>
        public static readonly Rect CoverFrontMm = new Rect(6, 10, 93, 128);

        /// <summary>Kart tabanlarının çözünürlüğü (piksel/mm).</summary>
        public const float CardPxPerMm = 8;
        /// <summary>Tuvalin varsayılan referans piksel/birimi; sprite PPU = bu × piksel/mm.</summary>
        public const float ReferencePixelsPerUnit = 100;
        /// <summary>Kâğıt dokularında izin verilen en yüksek doğrusal kanal değeri.</summary>
        public const float MaxLinearAlbedo = 0.8f;
        /// <summary>Kart yüzü malzemelerinin pürüzsüzlüğü (cardFace pürüzü 0,88).</summary>
        public const float CardSmoothness = 0.12f;

        /// <summary>Kart tabanı dokuları: dosya adı → (kart mm, malzeme adı).</summary>
        public static readonly (string texture, Vector2 mm, string material)[] CardBases =
        {
            ("tx_card_person", new Vector2(80, 120), "CardFace Person"),
            ("tx_card_item_kit", new Vector2(80, 50), "CardFace Item Kit"),
            ("tx_card_item_armor", new Vector2(80, 50), "CardFace Item Armor"),
            ("tx_card_item_aid", new Vector2(80, 50), "CardFace Item Aid"),
            ("tx_card_material", new Vector2(80, 56), "CardFace Material"),
            ("tx_card_chip", new Vector2(56, 40), "CardFace Chip"),
            ("tx_card_recipe", new Vector2(112, 80), "CardFace Recipe"),
        };

        /// <summary>Sprite adları (CONTRACTS › Shared assets); hepsi <see cref="SpriteDir"/>'da.</summary>
        public static readonly string[] SpriteNames =
        {
            "pip_fill", "pip_ring", "pip_reserved", "frame_selected", "frame_target", "dim", "ring_pencil", "smudge", "tape",
            "stamp_frame", "corner_restricted", "corner_lost", "rule", "icon_metal", "icon_devre", "icon_kumas", "icon_kimyasal",
            "picto_kit_tek", "picto_kit_emp", "picto_kit_ana", "picto_armor", "picto_medkit", "picto_emp_cell", "icon_light",
            "icon_clock",
        };

        /// <summary>Kit yığını türü → piktogram sprite'ı (BoardLayout.KitKinds sırası).</summary>
        public static readonly string[] PictogramSprites = { "picto_kit_tek", "picto_kit_emp", "picto_kit_ana", "picto_armor", "picto_medkit", "picto_emp_cell" };
        /// <summary>Hammadde türü → simge sprite'ı (BoardLayout.MaterialKinds sırası).</summary>
        public static readonly string[] MaterialIconSprites = { "icon_metal", "icon_devre", "icon_kumas", "icon_kimyasal" };

        [MenuItem("Corrigible/Revizyon 3/Pano dokularını üret")]
        static void BakeMenu() => Debug.Log("[Rev3] " + BakeAll());

        /// <summary>Bütün pano ve kart dokularını, sprite'ları ve malzemeleri üretir (var olanları yerinde günceller).</summary>
        /// <summary>Doku listesi (yol, çizici); adım adım üretmek için (bir eval'in süre sınırı).</summary>
        static List<(string path, System.Func<Paint> paint)> TextureJobs() => new List<(string, System.Func<Paint>)>
        {
            (BoardFacePath, BoardFace),
            (OrderSheetPath, OrderSheet),
            (BoardTextureDir + "/tx_card_person.png", PersonBase),
            (BoardTextureDir + "/tx_card_item_kit.png", () => ItemBase(ItemSlotKind.Kit)),
            (BoardTextureDir + "/tx_card_item_armor.png", () => ItemBase(ItemSlotKind.Armor)),
            (BoardTextureDir + "/tx_card_item_aid.png", () => ItemBase(ItemSlotKind.Aid)),
            (BoardTextureDir + "/tx_card_material.png", MaterialBase),
            (BoardTextureDir + "/tx_card_chip.png", ChipBase),
            (BoardTextureDir + "/tx_card_recipe.png", RecipeBase),
            (RecipePagePath, RecipePage),
            (BookCoverPath, BookCoverFromModel),
        };

        public static int TextureCount => TextureJobs().Count;

        /// <summary>Tek dokuyu üretir ve içe aktarır; süreyi döndürür.</summary>
        public static string BakeTexture(int index)
        {
            Directory.CreateDirectory(TextureDir); Directory.CreateDirectory(BoardTextureDir);
            var (path, make) = TextureJobs()[index];
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var paint = make();
            long drawn = sw.ElapsedMilliseconds;
            if (path != BoardFacePath) paint.CapAlbedo(MaxLinearAlbedo);
            bool changed = Write(path, paint);
            ImportTexture(path, Mathf.Max(paint.W, paint.H));
            return $"{Path.GetFileName(path)} {paint.W}×{paint.H}: çizim {drawn} ms, toplam {sw.ElapsedMilliseconds} ms{(changed ? "" : " (aynı)")}";
        }

        /// <summary>Bütün sprite'ları üretir ve içe aktarır.</summary>
        public static string BakeSprites()
        {
            Directory.CreateDirectory(SpriteDir);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int n = 0;
            foreach (var s in Sprites())
            {
                string path = SpriteDir + "/" + s.name + ".png";
                Write(path, s.paint);
                ImportSprite(path, s.pxPerMm, s.borderMm, Mathf.Max(s.paint.W, s.paint.H));
                n++;
            }
            return $"{n} sprite, {sw.ElapsedMilliseconds} ms";
        }

        public static string BakeAll()
        {
            Directory.CreateDirectory(TextureDir);
            Directory.CreateDirectory(BoardTextureDir);
            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(MaterialDir);
            int written = 0;

            // ---- dokular (Default, sRGB, mipmap, kıskaç)
            var textures = new List<(string path, Paint paint)>
            {
                (BoardFacePath, BoardFace()),
                (OrderSheetPath, OrderSheet()),
                (BoardTextureDir + "/tx_card_person.png", PersonBase()),
                (BoardTextureDir + "/tx_card_item_kit.png", ItemBase(ItemSlotKind.Kit)),
                (BoardTextureDir + "/tx_card_item_armor.png", ItemBase(ItemSlotKind.Armor)),
                (BoardTextureDir + "/tx_card_item_aid.png", ItemBase(ItemSlotKind.Aid)),
                (BoardTextureDir + "/tx_card_material.png", MaterialBase()),
                (BoardTextureDir + "/tx_card_chip.png", ChipBase()),
                (BoardTextureDir + "/tx_card_recipe.png", RecipeBase()),
                (RecipePagePath, RecipePage()),
                (BookCoverPath, BookCoverFromModel()),
            };
            foreach (var (path, paint) in textures)
            {
                if (path != BoardFacePath) paint.CapAlbedo(MaxLinearAlbedo);
                if (Write(path, paint)) written++;
                ImportTexture(path, Mathf.Max(paint.W, paint.H));
            }

            // ---- sprite'lar
            foreach (var s in Sprites())
            {
                string path = SpriteDir + "/" + s.name + ".png";
                if (Write(path, s.paint)) written++;
                ImportSprite(path, s.pxPerMm, s.borderMm, Mathf.Max(s.paint.W, s.paint.H));
            }

            // ---- malzemeler
            var report = new List<string>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return "URP Lit gölgelendiricisi bulunamadı; dokular üretildi, malzemeler kurulmadı.";
            foreach (var (texture, _, material) in CardBases)
                LitMaterial(lit, MaterialDir + "/" + material + ".mat", AssetDatabase.LoadAssetAtPath<Texture2D>(BoardTextureDir + "/" + texture + ".png"), CardSmoothness);
            LitMaterial(lit, PortraitTemplatePath, null, CardSmoothness);
            LitMaterial(lit, OrderSheetMaterialPath, AssetDatabase.LoadAssetAtPath<Texture2D>(OrderSheetPath), 0.08f);
            BookMaterials();
            var boardFace = AssetDatabase.LoadAssetAtPath<Material>(BoardFaceMaterialPath);
            if (boardFace != null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BoardFacePath);
                boardFace.SetTexture("_BaseMap", tex);
                boardFace.SetTexture("_MainTex", tex);
                var tint = boardFace.HasProperty("_BaseColor") ? boardFace.GetColor("_BaseColor") : Color.white;
                boardFace.SetColor("_BaseColor", new Color(1, 1, 1, tint.a));
                boardFace.SetColor("_Color", new Color(1, 1, 1, tint.a));
                EditorUtility.SetDirty(boardFace);
            }
            else report.Add("boardFace.mat yok (Rev3 malzeme kurulumunu çalıştır)");
            AssetDatabase.SaveAssets();

            return $"Revizyon 3 pano dokuları: {textures.Count} doku, {SpriteNames.Length} sprite, {CardBases.Length + 5} malzeme; " +
                   $"{written} dosya değişti → {TextureDir}, {SpriteDir}, {MaterialDir}" + (report.Count > 0 ? " · " + string.Join(" · ", report) : "");
        }

        // ================================================================ tarif defteri (Aşama 5 · R18a, PLAN §2.1)

        /// <summary>
        /// <c>tx_recipe_page</c> 792 × 1120 (99 × 140 mm, 8 piksel/mm), sözcüksüz: kâğıt #F3EBD3 + doku, KK'nin 34 px'lik çizgileri
        /// (sayfada 5,95 mm adım, 1 px = 0,175 mm, rgba(142,129,102,0,22)), 1 px kenar #CDBF97, sırt gölgesi U = 0 tarafında
        /// (rgba(70,50,20,0,20) → 0, genişliğin %12'si). Sağ sayfada U = 0 sırttadır; sol sayfa malzemesi U'yu aynalar
        /// (<c>_BaseMap_ST</c> (−1, 1, 1, 0)). Albedo 0,8 tavanı uygulanır. Aynıysa yazılmaz.
        /// </summary>
        public static string BakeRecipePage()
        {
            Directory.CreateDirectory(BoardTextureDir);
            var paint = RecipePage();
            paint.CapAlbedo(MaxLinearAlbedo);
            bool changed = Write(RecipePagePath, paint);
            ImportTexture(RecipePagePath, Mathf.Max(paint.W, paint.H));
            return $"{Path.GetFileName(RecipePagePath)} {paint.W}×{paint.H}{(changed ? " yazıldı" : " (aynı)")}";
        }

        /// <summary>
        /// <c>tx_book_cover</c> 744 × 1024: kapağın UV'si 0–1 olan tek yüzü, ön dörtgen (Cover yereli x 6…99, y 10…138 mm; D §1.1).
        /// Bez #5A2420 + %12 kum; sırt şeridi #3E1714 kapağın x 0…9 mm'sinde (dörtgende ilk 3 mm); krem etiket #E9DFC2 66 × 34 mm
        /// <c>TXT_Cover</c> yuvasının ortasında (kapağın yukarıdan 50 mm'si, x 55,5; C Ç15: yuva esas), iç çerçeve 0,5 mm #3E1714;
        /// sözcük yok (TARİF DEFTERİ ve KYGA · 07 TMP). Kenarda 0,25 mm bez rengi: metrik UV'li öteki bez yüzleri kıskaçla bunu örnekler.
        /// UV yönü modelden okunur (<see cref="CoverUvFlip"/>).
        /// </summary>
        public static string BakeBookCover()
        {
            Directory.CreateDirectory(BoardTextureDir);
            string note = CoverUvFlip(out bool flipU, out bool flipV);
            var paint = BookCover(flipU, flipV);
            paint.CapAlbedo(MaxLinearAlbedo);
            bool changed = Write(BookCoverPath, paint);
            ImportTexture(BookCoverPath, Mathf.Max(paint.W, paint.H));
            return $"{Path.GetFileName(BookCoverPath)} {paint.W}×{paint.H}{(changed ? " yazıldı" : " (aynı)")} · UV: {note}";
        }

        /// <summary>
        /// Defter malzemeleri (URP Lit, beyaz boya, opak): <c>Recipe page L.mat</c> (U aynalı, <c>_BaseMap_ST</c> (−1, 1, 1, 0)),
        /// <c>Recipe page R.mat</c> (pürüzsüzlük 0,10) ve <c>Book cover.mat</c> (0,18 = bookCloth). Paylaşılan <c>pageFace.mat</c> /
        /// <c>bookCloth.mat</c> değişmez. Var olan malzeme yerinde güncellenir (GUID korunur).
        /// </summary>
        public static string BookMaterials()
        {
            Directory.CreateDirectory(MaterialDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return "URP Lit gölgelendiricisi bulunamadı; defter malzemeleri kurulmadı.";
            var page = AssetDatabase.LoadAssetAtPath<Texture2D>(RecipePagePath);
            var coverTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BookCoverPath);
            var left = LitMaterial(lit, RecipePageLeftMaterialPath, page, PageSmoothness);
            left.SetTextureScale("_BaseMap", new Vector2(-1, 1));
            left.SetTextureOffset("_BaseMap", new Vector2(1, 0));
            left.SetTextureScale("_MainTex", new Vector2(-1, 1));
            left.SetTextureOffset("_MainTex", new Vector2(1, 0));
            EditorUtility.SetDirty(left);
            var right = LitMaterial(lit, RecipePageRightMaterialPath, page, PageSmoothness);
            right.SetTextureScale("_BaseMap", Vector2.one);
            right.SetTextureOffset("_BaseMap", Vector2.zero);
            EditorUtility.SetDirty(right);
            LitMaterial(lit, BookCoverMaterialPath, coverTex, CoverSmoothness);
            AssetDatabase.SaveAssets();
            return "defter malzemeleri: " + Path.GetFileName(RecipePageLeftMaterialPath) + ", " + Path.GetFileName(RecipePageRightMaterialPath) + ", " +
                   Path.GetFileName(BookCoverMaterialPath) + (page == null ? " (sayfa dokusu yok)" : "") + (coverTex == null ? " (kapak dokusu yok)" : "");
        }

        /// <summary>Adıyla tek sprite üretir ve içe aktarır (ör. <c>icon_clock</c>); yoksa null.</summary>
        public static string BakeSprite(string name)
        {
            Directory.CreateDirectory(SpriteDir);
            foreach (var s in Sprites())
            {
                if (s.name != name) continue;
                string path = SpriteDir + "/" + s.name + ".png";
                bool changed = Write(path, s.paint);
                ImportSprite(path, s.pxPerMm, s.borderMm, Mathf.Max(s.paint.W, s.paint.H));
                return path + (changed ? " yazıldı" : " (aynı)");
            }
            return null;
        }

        /// <summary>
        /// Kapağın ön dörtgeninin UV yönü (modelin <c>Cover</c> ağı, bookCloth alt ağı, UV'si 0–1 olan köşeler): U, x arttıkça
        /// azalıyorsa <paramref name="flipU"/>, V, y arttıkça azalıyorsa <paramref name="flipV"/>. Önden (−z) bakınca +x sağdır.
        /// </summary>
        public static string CoverUvFlip(out bool flipU, out bool flipV)
        {
            flipU = flipV = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecipeBookPrefabPath);
            if (prefab == null) return "model yok (varsayılan yön)";
            Mesh mesh = null;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (mf.name == "Cover") { mesh = mf.sharedMesh; break; }
            if (mesh == null) return "Cover ağı yok (varsayılan yön)";
            try
            {
                var v = mesh.vertices;
                var uv = mesh.uv;
                if (uv == null || uv.Length != v.Length || mesh.subMeshCount < 1) return "UV yok (varsayılan yön)";
                var tris = mesh.GetTriangles(0);
                int minX = -1, maxX = -1, minY = -1, maxY = -1;
                foreach (int i in tris)
                {
                    var t = uv[i];
                    if (t.x < -0.01f || t.x > 1.01f || t.y < -0.01f || t.y > 1.01f) continue;
                    if (v[i].z > 0.0005f) continue;   // ön yüz (−z)
                    if (minX < 0 || v[i].x < v[minX].x) minX = i;
                    if (maxX < 0 || v[i].x > v[maxX].x) maxX = i;
                    if (minY < 0 || v[i].y < v[minY].y) minY = i;
                    if (maxY < 0 || v[i].y > v[maxY].y) maxY = i;
                }
                if (minX < 0) return "0–1 UV'li ön köşe yok (varsayılan yön)";
                flipU = uv[maxX].x < uv[minX].x;
                flipV = uv[maxY].y < uv[minY].y;
                return $"U {(flipU ? "aynalı" : "düz")}, V {(flipV ? "aynalı" : "düz")} (x {v[minX].x:0.000}…{v[maxX].x:0.000}, y {v[minY].y:0.000}…{v[maxY].y:0.000})";
            }
            catch (System.Exception e)
            {
                return "ağ okunamadı (" + e.GetType().Name + "; varsayılan yön)";
            }
        }

        static Paint RecipePage()
        {
            float W = RecipePageMm.x, H = RecipePageMm.y, Ppm = BookPxPerMm;
            var p = new Paint(Mathf.RoundToInt(W * Ppm), Mathf.RoundToInt(H * Ppm), Ppm, Ppm, Hex("#F3EBD3"));
            p.Grain(W, H, 11 * 71);
            // KK: repeating-linear-gradient(180deg, 0 33px, rgba(142,129,102,0.22) 33px 34px), dolgu kutusunun üstünden (kenar 1 px)
            const float Px = 0.175f;
            float bx = W / 560f, by = Px;
            var rule = Rgb(142, 129, 102);
            for (float y = by + 33 * Px; y + Px <= H - by + 1e-4f; y += 34 * Px)
                p.Fill(bx, y, W - 2 * bx, Px, rule, 0.22f);
            // sırt gölgesi (U = 0 tarafı): rgba(70,50,20,0.20) → saydam, genişliğin %12'si
            var shade = Rgb(70, 50, 20);
            float sw = W * 0.12f * p.Sx;
            for (int i = 0; i < Mathf.CeilToInt(sw); i++)
            {
                float a = 0.20f * Mathf.Clamp01(1f - (i + 0.5f) / sw);
                p.FillPx(i, 0, i + 1, p.H, shade, a);
            }
            // 1 px kenar #CDBF97
            var edge = Hex("#CDBF97");
            p.Fill(0, 0, W, by, edge); p.Fill(0, H - by, W, by, edge);
            p.Fill(0, 0, bx, H, edge); p.Fill(W - bx, 0, bx, H, edge);
            return p;
        }

        static Paint BookCoverFromModel()
        {
            CoverUvFlip(out bool u, out bool v);
            return BookCover(u, v);
        }

        static Paint BookCover(bool flipU, bool flipV)
        {
            var q = CoverFrontMm;
            const float CoverH = 148, Ppm = BookPxPerMm;
            var cloth = Hex("#5A2420");
            var p = new Paint(Mathf.RoundToInt(q.width * Ppm), Mathf.RoundToInt(q.height * Ppm), Ppm, Ppm, cloth);
            // kapak mm'si (sol üstten, y aşağı) → doku mm'si; dörtgenin üstü kapağın yukarıdan 148 − 138 = 10 mm'si
            float top = CoverH - q.yMax;
            Rect Q(float x, float y, float w, float h)
            {
                float tx = x - q.xMin, ty = y - top;
                if (flipU) tx = q.width - tx - w;
                if (flipV) ty = q.height - ty - h;
                return new Rect(tx, ty, w, h);
            }
            void FillQ(float x, float y, float w, float h, Color c, float a = 1)
            {
                var r = Q(x, y, w, h);
                p.Fill(r.x, r.y, r.width, r.height, c, a);
            }
            // %12 kum: 0,25 mm'lik siyah/beyaz noktalar
            var rng = new Lcg(5 * 229);
            for (int i = 0; i < q.width * q.height * 1.2f; i++)
            {
                var c = rng.Next() > 0.5f ? Color.black : Color.white;
                p.Fill(rng.Next() * q.width, rng.Next() * q.height, 0.25f, 0.25f, c, 0.12f);
            }
            var dark = Hex("#3E1714");
            FillQ(q.xMin, top, 9 - q.xMin, q.height, dark);                        // sırt şeridi (kapağın x 0…9 mm'si)
            const float LabelW = 66, LabelH = 34, LabelCx = 55.5f, LabelCy = 50;   // TXT_Cover: (0,0555; 0,098) → yukarıdan 50 mm
            float lx = LabelCx - LabelW / 2, ly = LabelCy - LabelH / 2;
            FillQ(lx, ly, LabelW, LabelH, Hex("#E9DFC2"));
            var frameRect = Q(lx + 1.5f, ly + 1.5f, LabelW - 3, LabelH - 3);
            p.StrokeRect(frameRect.x, frameRect.y, frameRect.width, frameRect.height, 0.5f, dark);
            // kenar halkası: metrik UV'li yüzler kıskaçla bunu örnekler
            const float Ring = 0.25f;
            p.Fill(0, 0, q.width, Ring, cloth); p.Fill(0, q.height - Ring, q.width, Ring, cloth);
            p.Fill(0, 0, Ring, q.height, cloth); p.Fill(q.width - Ring, 0, Ring, q.height, cloth);
            return p;
        }

        // ================================================================ kayıt ve içe aktarma

        /// <summary>PNG'yi yazar; içerik aynıysa dosyaya dokunmaz. Değiştiyse true.</summary>
        static bool Write(string path, Paint paint)
        {
            var tex = paint.ToTexture();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            if (File.Exists(path))
            {
                var old = File.ReadAllBytes(path);
                if (old.Length == png.Length && Same(old, png)) return false;
            }
            File.WriteAllBytes(path, png);
            return true;
        }

        static bool Same(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        static int MaxSizeFor(int px)
        {
            int size = 32;
            while (size < px && size < 8192) size *= 2;
            return Mathf.Max(size, 32);
        }

        static void ImportTexture(string path, int largest)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = false;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 8;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = MaxSizeFor(largest);
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        static void ImportSprite(string path, float pxPerMm, Vector4 borderMm, int largest)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.textureType = TextureImporterType.Sprite;
            s.spriteMode = (int)SpriteImportMode.Single;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spritePixelsPerUnit = ReferencePixelsPerUnit * pxPerMm;
            // Kenar payı piksel (sol, alt, sağ, üst)
            s.spriteBorder = new Vector4(Mathf.Round(borderMm.x * pxPerMm), Mathf.Round(borderMm.y * pxPerMm),
                Mathf.Round(borderMm.z * pxPerMm), Mathf.Round(borderMm.w * pxPerMm));
            s.mipmapEnabled = true;
            s.sRGBTexture = true;
            s.alphaSource = TextureImporterAlphaSource.FromInput;
            s.alphaIsTransparency = true;
            s.wrapMode = TextureWrapMode.Clamp;
            s.filterMode = FilterMode.Trilinear;
            s.npotScale = TextureImporterNPOTScale.None;
            ti.SetTextureSettings(s);
            ti.maxTextureSize = MaxSizeFor(largest);
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        /// <summary>URP Lit, opak, taban dokusu ve beyaz boya; var olan malzeme yerinde güncellenir (GUID korunur).</summary>
        static Material LitMaterial(Shader lit, string path, Texture2D baseMap, float smoothness)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = m == null;
            if (created) m = new Material(lit) { name = Path.GetFileNameWithoutExtension(path) };
            else if (m.shader != lit) m.shader = lit;
            m.SetTexture("_BaseMap", baseMap);
            m.SetTexture("_MainTex", baseMap);
            m.SetColor("_BaseColor", Color.white);
            m.SetColor("_Color", Color.white);
            m.SetFloat("_WorkflowMode", 1);
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Surface", 0);
            m.SetOverrideTag("RenderType", "Opaque");
            m.SetFloat("_SrcBlend", 1);
            m.SetFloat("_DstBlend", 0);
            m.SetFloat("_ZWrite", 1);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = -1;
            if (created) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================ renkler ve üreteç

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1);

        static readonly Color Ink = Hex("#1E1B16"), Paper2 = Hex("#E6DCC0"), Red = Hex("#9A1E14"),
            RuleColor = Hex("#B9A98A"), Cream = Rgb(232, 224, 200), InkSoft = Rgb(30, 27, 22), White = Color.white;

        /// <summary>Tasarımın üreteci: s = s · 16807 mod (2³¹ − 1), değer s / (2³¹ − 1).</summary>
        sealed class Lcg
        {
            long s;
            public Lcg(long seed) { s = seed; }
            public float Next() { s = s * 16807 % 2147483647; return (float)(s / 2147483647.0); }
        }

        // ================================================================ tuval (mm cinsinden çizim, sol üst orijin)

        /// <summary>Piksel dizisi; çizim mm'de (x · Sx, y · Sy piksel). Kayıtta satırlar çevrilir (Unity alt-sol orijin).</summary>
        sealed class Paint
        {
            public readonly int W, H;
            public readonly float Sx, Sy;
            public readonly Color[] Px;

            public Paint(int w, int h, float sx, float sy, Color fill)
            {
                W = w; H = h; Sx = sx; Sy = sy;
                Px = new Color[w * h];
                for (int i = 0; i < Px.Length; i++) Px[i] = fill;
            }

            /// <summary>Saydam tuval (sprite): rengi verilen, alfası 0.</summary>
            public static Paint Clear(int w, int h, float pxPerMm, Color colour) => new Paint(w, h, pxPerMm, pxPerMm, new Color(colour.r, colour.g, colour.b, 0));

            /// <summary>Ortalama ölçek (piksel/mm): çizgi kalınlığı için.</summary>
            public float S => (Sx + Sy) * 0.5f;

            public Vector2 P(float x, float y) => new Vector2(x * Sx, y * Sy);

            public void Blend(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || a <= 0) return;
                int i = y * W + x;
                var d = Px[i];
                a = Mathf.Clamp01(a * c.a);
                float outA = a + d.a * (1 - a);
                if (outA <= 0) { Px[i] = new Color(c.r, c.g, c.b, 0); return; }
                Px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / outA, (c.g * a + d.g * d.a * (1 - a)) / outA,
                    (c.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }

            /// <summary>Eksene paralel dolu dikdörtgen (mm), kenarlarda alan örtüsü.</summary>
            public void Fill(float x, float y, float w, float h, Color c, float a = 1) =>
                FillPx(x * Sx, y * Sy, (x + w) * Sx, (y + h) * Sy, c, a);

            public void FillPx(float x0, float y0, float x1, float y1, Color c, float a)
            {
                if (x1 < x0) { float t = x0; x0 = x1; x1 = t; }
                if (y1 < y0) { float t = y0; y0 = y1; y1 = t; }
                int i0 = Mathf.Max(0, Mathf.FloorToInt(x0)), i1 = Mathf.Min(W - 1, Mathf.CeilToInt(x1) - 1);
                int j0 = Mathf.Max(0, Mathf.FloorToInt(y0)), j1 = Mathf.Min(H - 1, Mathf.CeilToInt(y1) - 1);
                for (int j = j0; j <= j1; j++)
                {
                    float cy = Mathf.Min(j + 1, y1) - Mathf.Max(j, y0);
                    if (cy <= 0) continue;
                    for (int i = i0; i <= i1; i++)
                    {
                        float cx = Mathf.Min(i + 1, x1) - Mathf.Max(i, x0);
                        if (cx > 0) Blend(i, j, c, a * cx * cy);
                    }
                }
            }

            /// <summary>Maskeyi tek seferde boyar (birleşimdeki üst üste binmeler çift koyulaşmaz).</summary>
            public void Composite(Mask m, Color c, float a = 1)
            {
                for (int y = 0; y < m.H; y++)
                    for (int x = 0; x < m.W; x++)
                    {
                        float v = m.A[y * m.W + x];
                        if (v > 0) Blend(m.X0 + x, m.Y0 + y, c, a * v);
                    }
            }

            /// <summary>Çizim alanı için maske (mm kutusu + piksel payı), tuvale kırpılmış.</summary>
            public Mask MaskMm(float x0, float y0, float x1, float y1, float padPx = 4)
            {
                int a = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) * Sx - padPx)), b = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) * Sy - padPx));
                int c = Mathf.Min(W, Mathf.CeilToInt(Mathf.Max(x0, x1) * Sx + padPx)), d = Mathf.Min(H, Mathf.CeilToInt(Mathf.Max(y0, y1) * Sy + padPx));
                return new Mask(a, b, Mathf.Max(1, c - a), Mathf.Max(1, d - b));
            }

            public Mask Whole() => new Mask(0, 0, W, H);

            /// <summary>Dikdörtgen kenar çizgisi (mm), kesikliyse desen çevre boyunca sürer (kanvas strokeRect gibi, sol üstten saat yönü).</summary>
            public void StrokeRect(float x, float y, float w, float h, float lineMm, Color c, float a = 1, float dashMm = 0, float gapMm = 0)
            {
                var m = MaskMm(x, y, x + w, y + h, lineMm * S + 2);
                var pts = new[] { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h) };
                if (dashMm > 0) m.Dashed(pts, true, lineMm * S, dashMm * S, gapMm * S, false);
                else m.Stroke(pts, true, lineMm * S, false);
                Composite(m, c, a);
            }

            /// <summary>Yatay çizgi (mm), düz uçlu (tasarımın rule()).</summary>
            public void HLine(float x0, float y, float x1, Color c, float lineMm, float a = 1)
            {
                var m = MaskMm(x0, y, x1, y, lineMm * S + 2);
                m.Segment(P(x0, y), P(x1, y), lineMm * S, false);
                Composite(m, c, a);
            }

            /// <summary>Kâğıt dokusu (tasarımın paperGrain'i): mm² başına 0,35 nokta, 0,35 mm, %5 siyah ya da beyaz.</summary>
            public void Grain(float wMm, float hMm, long seed)
            {
                var r = new Lcg(seed);
                for (int i = 0; i < wMm * hMm * 0.35f; i++)
                {
                    var c = r.Next() > 0.5f ? Color.black : Color.white;
                    float x = r.Next() * wMm, y = r.Next() * hMm;
                    Fill(x, y, 0.35f, 0.35f, c, 0.05f);
                }
            }

            /// <summary>Her pikselin en parlak doğrusal kanalını <paramref name="max"/>'a indirir (ton korunur).</summary>
            public void CapAlbedo(float max)
            {
                for (int i = 0; i < Px.Length; i++)
                {
                    var c = Px[i];
                    var lin = c.linear;
                    float m = Mathf.Max(lin.r, Mathf.Max(lin.g, lin.b));
                    if (m <= max) continue;
                    float k = max / m;
                    var capped = new Color(lin.r * k, lin.g * k, lin.b * k, c.a).gamma;
                    capped.a = c.a;
                    Px[i] = capped;
                }
            }

            public Texture2D ToTexture()
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                var flipped = new Color32[Px.Length];
                for (int y = 0; y < H; y++)
                {
                    int src = y * W, dst = (H - 1 - y) * W;
                    for (int x = 0; x < W; x++) flipped[dst + x] = Px[src + x];
                }
                tex.SetPixels32(flipped);
                tex.Apply();
                return tex;
            }
        }

        /// <summary>
        /// Örtü maskesi (piksel): şekiller en büyük örtüyle birleşir, sonra <see cref="Paint.Composite"/> tek seferde boyar. Noktalar
        /// piksel cinsinden, sol üst orijin.
        /// </summary>
        sealed class Mask
        {
            public readonly int X0, Y0, W, H;
            public readonly float[] A;

            public Mask(int x0, int y0, int w, int h) { X0 = x0; Y0 = y0; W = w; H = h; A = new float[w * h]; }

            void Put(int x, int y, float c)
            {
                x -= X0; y -= Y0;
                if (x < 0 || y < 0 || x >= W || y >= H || c <= 0) return;
                int i = y * W + x;
                if (c > A[i]) A[i] = Mathf.Min(1, c);
            }

            /// <summary>Kalın çizgi; <paramref name="round"/> yuvarlak uçlu, değilse düz uçlu (kanvas butt).</summary>
            public void Segment(Vector2 p, Vector2 q, float width, bool round)
            {
                float r = width / 2;
                var d = q - p;
                float len = d.magnitude;
                var u = len > 1e-5f ? d / len : Vector2.right;
                int x0 = Mathf.FloorToInt(Mathf.Min(p.x, q.x) - r - 1), x1 = Mathf.CeilToInt(Mathf.Max(p.x, q.x) + r + 1);
                int y0 = Mathf.FloorToInt(Mathf.Min(p.y, q.y) - r - 1), y1 = Mathf.CeilToInt(Mathf.Max(p.y, q.y) + r + 1);
                x0 = Mathf.Max(x0, X0); y0 = Mathf.Max(y0, Y0); x1 = Mathf.Min(x1, X0 + W - 1); y1 = Mathf.Min(y1, Y0 + H - 1);
                float cap = Mathf.Min(1, width);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var v = new Vector2(x + 0.5f, y + 0.5f) - p;
                        float along = Vector2.Dot(v, u);
                        float cover;
                        if (round)
                        {
                            float t = Mathf.Clamp(along, 0, len);
                            cover = Mathf.Clamp01(Mathf.Min(r + 0.5f - (v - u * t).magnitude, cap));
                        }
                        else
                        {
                            float perp = Mathf.Abs(u.x * v.y - u.y * v.x);
                            float cp = Mathf.Clamp01(Mathf.Min(r + 0.5f - perp, cap));
                            float ca = Mathf.Clamp01(Mathf.Min(along, len - along) + 0.5f);
                            cover = cp * ca;
                        }
                        if (cover > 0) Put(x, y, cover);
                    }
            }

            /// <summary>Çoklu çizgi; düz uçluda köşeler kare doldurulur (dik açılı kanvas miter birleşimi).</summary>
            public void Stroke(IList<Vector2> pts, bool closed, float width, bool round)
            {
                int n = pts.Count;
                for (int i = 1; i < n; i++) Segment(pts[i - 1], pts[i], width, round);
                if (closed && n > 2) Segment(pts[n - 1], pts[0], width, round);
                if (round) return;
                int from = closed ? 0 : 1, to = closed ? n : n - 1;
                for (int i = from; i < to; i++) Square(pts[i], width);
            }

            void Square(Vector2 c, float width)
            {
                float r = width / 2;
                for (int y = Mathf.FloorToInt(c.y - r); y <= Mathf.CeilToInt(c.y + r); y++)
                    for (int x = Mathf.FloorToInt(c.x - r); x <= Mathf.CeilToInt(c.x + r); x++)
                    {
                        float cx = Mathf.Min(x + 1, c.x + r) - Mathf.Max(x, c.x - r);
                        float cy = Mathf.Min(y + 1, c.y + r) - Mathf.Max(y, c.y - r);
                        if (cx > 0 && cy > 0) Put(x, y, cx * cy);
                    }
            }

            /// <summary>Kesikli çoklu çizgi (desen köşelerden kesintisiz geçer; köşede çizgi açıksa köşe doldurulur).</summary>
            public void Dashed(IList<Vector2> pts, bool closed, float width, float dash, float gap, bool round)
            {
                // Evre çift duyarlıkta: float birikince büyük evrede küçük adımlar kayboluyor, döngü ilerlemiyordu (pano yüzü
                // dakikalarca kilitleniyordu). Her adım bir desen sınırına ya da kenarın sonuna gider.
                if (dash <= 0) { Stroke(pts, closed, width, round); return; }
                var path = new List<Vector2>(pts);
                if (closed) path.Add(pts[0]);
                double period = dash + System.Math.Max(0, gap), phase = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    Vector2 a = path[i - 1], b = path[i];
                    double len = (b - a).magnitude;
                    if (len <= 1e-5) continue;
                    var u = (b - a) / (float)len;
                    double s = 0;
                    while (len - s > 1e-6)
                    {
                        double inPeriod = phase - System.Math.Floor(phase / period) * period;
                        bool on = inPeriod < dash - 1e-9;
                        double step = on ? dash - inPeriod : period - inPeriod;
                        if (step < 1e-6) step = period;   // sınırda: bir sonraki desene geç
                        step = System.Math.Min(step, len - s);
                        if (on) Segment(a + u * (float)s, a + u * (float)(s + step), width, round);
                        s += step;
                        phase += step;
                    }
                    double end = phase - System.Math.Floor(phase / period) * period;
                    // köşe: desen açıkken düz uçlu çizginin köşesi kare
                    if (!round && (i < path.Count - 1 || closed) && end < dash) Square(b, width);
                }
            }

            /// <summary>Dolu çokgen (çift-tek kuralı), piksel başına 4 × 4 örnek.</summary>
            public void Polygon(IList<Vector2> pts)
            {
                if (pts.Count < 3) return;
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var p in pts) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
                int x0 = Mathf.Max(X0, Mathf.FloorToInt(minX)), x1 = Mathf.Min(X0 + W - 1, Mathf.CeilToInt(maxX));
                int y0 = Mathf.Max(Y0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(Y0 + H - 1, Mathf.CeilToInt(maxY));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < 4; sy++)
                            for (int sx = 0; sx < 4; sx++)
                                if (Inside(pts, x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f)) hits++;
                        if (hits > 0) Put(x, y, hits / 16f);
                    }
            }

            static bool Inside(IList<Vector2> pts, float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                {
                    var a = pts[i]; var b = pts[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                return inside;
            }

            /// <summary>Maskeyi dikdörtgenle (piksel) kırpar: dışı silinir, kenar pikselleri alan örtüsüyle.</summary>
            public void ClipRect(float rx0, float ry0, float rx1, float ry1)
            {
                for (int y = 0; y < H; y++)
                {
                    float py = Y0 + y, cy = Mathf.Clamp01(Mathf.Min(py + 1, ry1) - Mathf.Max(py, ry0));
                    for (int x = 0; x < W; x++)
                    {
                        float px = X0 + x, cx = Mathf.Clamp01(Mathf.Min(px + 1, rx1) - Mathf.Max(px, rx0));
                        A[y * W + x] *= cx * cy;
                    }
                }
            }
        }

        /// <summary>Elips noktaları (piksel): merkez ve yarıçaplar mm, açı kanvas yönünde (y aşağı, artan açı saat yönü).</summary>
        static Vector2[] ArcPts(Paint p, float cx, float cy, float rx, float ry, float from, float to, int n)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)n);
                pts[i] = p.P(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            return pts;
        }

        // ================================================================ simgeler (boardtex.js › icon: 10 × 10 birimlik hücre)

        /// <summary>
        /// Tasarımın piktogramı: hücre (<paramref name="x"/>, <paramref name="y"/>) mm, kenarı <paramref name="size"/> mm; çizgi 0,9 birim,
        /// yuvarlak uç ve birleşim. <paramref name="dash"/> (birim) verilirse çizgiler kesikli (panonun depo simgeleri tasarımda
        /// açık kalan kesik desenle çizilir).
        /// </summary>
        static void Icon(Mask m, Paint p, string kind, float x, float y, float size, float[] dash = null)
        {
            float k = size / 10f;
            float lw = 0.9f * k * p.S;
            Vector2 U(float ux, float uy) => p.P(x + ux * k, y + uy * k);
            Vector2[] Pts(float[] xy)
            {
                var a = new Vector2[xy.Length / 2];
                for (int i = 0; i < a.Length; i++) a[i] = U(xy[2 * i], xy[2 * i + 1]);
                return a;
            }
            void Line(bool close, float[] xy, float[] pattern)
            {
                var pts = Pts(xy);
                if (pattern != null) m.Dashed(pts, close, lw, pattern[0] * k * p.S, pattern[1] * k * p.S, true);
                else m.Stroke(pts, close, lw, true);
            }
            void StrokeP(bool close, params float[] xy) => Line(close, xy, dash);
            void FillP(params float[] xy) => m.Polygon(Pts(xy));
            void FillR(float rx, float ry, float rw, float rh) => FillP(rx, ry, rx + rw, ry, rx + rw, ry + rh, rx, ry + rh);
            void StrokeR(float rx, float ry, float rw, float rh) => StrokeP(true, rx, ry, rx + rw, ry, rx + rw, ry + rh, rx, ry + rh);
            Vector2[] Circle(float cx, float cy, float r, float from, float to, int n) => ArcPts(p, x + cx * k, y + cy * k, r * k, r * k, from, to, n);
            void Arc(float cx, float cy, float r, float from, float to)
            {
                var pts = Circle(cx, cy, r, from, to, 72);
                if (dash != null) m.Dashed(pts, false, lw, dash[0] * k * p.S, dash[1] * k * p.S, true);
                else m.Stroke(pts, false, lw, true);
            }
            void Disc(float cx, float cy, float r) => m.Polygon(Circle(cx, cy, r, 0, Mathf.PI * 2, 72));

            switch (kind)
            {
                case "metal":
                {
                    var h = new float[12];
                    for (int i = 0; i < 6; i++)
                    {
                        float a = Mathf.PI / 6 + i * Mathf.PI / 3;
                        h[2 * i] = 5 + 4.3f * Mathf.Cos(a);
                        h[2 * i + 1] = 5 + 4.3f * Mathf.Sin(a);
                    }
                    StrokeP(true, h);
                    Arc(5, 5, 1.8f, 0, Mathf.PI * 2);
                    break;
                }
                case "devre":
                    StrokeR(2.6f, 2.6f, 4.8f, 4.8f);
                    for (int i = 0; i < 3; i++)
                    {
                        float t = 3.6f + i * 1.4f;
                        StrokeP(false, t, 2.6f, t, 0.8f);
                        StrokeP(false, t, 7.4f, t, 9.2f);
                        StrokeP(false, 2.6f, t, 0.8f, t);
                        StrokeP(false, 7.4f, t, 9.2f, t);
                    }
                    FillR(4.2f, 4.2f, 1.6f, 1.6f);
                    break;
                case "kumas":
                    StrokeP(true, 1, 3, 7.5f, 1.5f, 9, 6.5f, 2.5f, 8.5f);
                    Line(true, new[] { 2.2f, 3.8f, 7.2f, 2.6f, 8.3f, 6.2f, 3f, 7.5f }, new[] { 0.8f, 0.7f });
                    break;
                case "kimyasal":
                    StrokeP(true, 3.6f, 1, 6.4f, 1, 6.4f, 3.6f, 8.8f, 8.2f, 8.2f, 9.2f, 1.8f, 9.2f, 1.2f, 8.2f, 3.6f, 3.6f);
                    FillP(2.3f, 6.6f, 7.7f, 6.6f, 8.6f, 8.4f, 1.4f, 8.4f);
                    break;
                case "kit_tek":
                    StrokeP(false, 2, 8.4f, 6.2f, 4.2f);
                    Arc(7.2f, 3, 2, 0.6f, 5.2f);
                    StrokeP(false, 8.2f, 8.6f, 3.4f, 3.8f);
                    FillP(3.4f, 3.8f, 2.4f, 1.6f, 1.6f, 2.4f);
                    break;
                case "kit_emp":
                    Arc(5, 5, 4.2f, 0, Mathf.PI * 2);
                    FillP(5.6f, 1.6f, 3.2f, 5.4f, 5, 5.4f, 4.2f, 8.4f, 6.9f, 4.4f, 5.1f, 4.4f);
                    break;
                case "kit_ana":
                    for (int i = 1; i < 4; i++) StrokeP(false, 1, i * 2.2f + 0.5f, 6, i * 2.2f + 0.5f);
                    Arc(6.2f, 5.6f, 2.3f, 0, Mathf.PI * 2);
                    StrokeP(false, 7.8f, 7.3f, 9.4f, 9.2f);
                    break;
                case "zirh":
                    StrokeP(true, 5, 0.9f, 8.8f, 2.3f, 8.4f, 6, 5, 9.2f, 1.6f, 6, 1.2f, 2.3f);
                    StrokeP(false, 5, 2.6f, 5, 7.6f);
                    StrokeP(false, 2.8f, 4.4f, 7.2f, 4.4f);
                    break;
                case "bakim":
                    StrokeR(1.2f, 2.6f, 7.6f, 6);
                    StrokeP(false, 3.6f, 2.6f, 3.6f, 1.4f, 6.4f, 1.4f, 6.4f, 2.6f);
                    FillR(4.4f, 3.8f, 1.2f, 3.6f);
                    FillR(3.2f, 5, 3.6f, 1.2f);
                    break;
                case "emp_hucre":
                    StrokeR(2.4f, 2, 5.2f, 7.4f);
                    FillR(4, 0.9f, 2, 1.1f);
                    FillP(5.6f, 3.2f, 3.9f, 5.9f, 5.2f, 5.9f, 4.4f, 8.3f, 6.3f, 5.2f, 5, 5.2f);
                    break;
                case "person":
                    Disc(5, 3.2f, 2.2f);
                    FillP(1.4f, 9.6f, 1.6f, 7.2f, 3, 5.8f, 7, 5.8f, 8.4f, 7.2f, 8.6f, 9.6f);
                    break;
                case "book":
                    StrokeP(true, 1, 2, 5, 3, 9, 2, 9, 8.6f, 5, 9.6f, 1, 8.6f);
                    StrokeP(false, 5, 3, 5, 9.6f);
                    break;
                case "clock":
                    // Saat (tarif sayfasının SÜRE satırı; KK 26 birimlik svg: daire r 10, ibreler 13,7 → 13,13 → 17,5;15,5)
                    Arc(5, 5, 3.85f, 0, Mathf.PI * 2);
                    StrokeP(false, 5, 2.7f, 5, 5, 6.73f, 5.96f);
                    break;
                case "light":
                    // Ampul (panonun lamba durumu için yazısız simge): altı açık cam, iki boyun çizgisi, iki duy halkası, flaman
                    Arc(5, 4.2f, 3.2f, Mathf.PI * 0.75f, Mathf.PI * 2.25f);
                    StrokeP(false, 2.74f, 6.46f, 3.6f, 7.6f);
                    StrokeP(false, 7.26f, 6.46f, 6.4f, 7.6f);
                    StrokeP(false, 3.4f, 7.9f, 6.6f, 7.9f);
                    StrokeP(false, 3.9f, 9.1f, 6.1f, 9.1f);
                    StrokeP(false, 4.1f, 5.6f, 5, 4.4f, 5.9f, 5.6f);
                    break;
            }
        }

        /// <summary>Hammadde türünün çizim adı (boardtex.js) ve kit türünün piktogramı.</summary>
        static readonly Dictionary<string, string> IconOfSprite = new Dictionary<string, string>
        {
            { "icon_metal", "metal" }, { "icon_devre", "devre" }, { "icon_kumas", "kumas" }, { "icon_kimyasal", "kimyasal" },
            { "picto_kit_tek", "kit_tek" }, { "picto_kit_emp", "kit_emp" }, { "picto_kit_ana", "kit_ana" }, { "picto_armor", "zirh" },
            { "picto_medkit", "bakim" }, { "picto_emp_cell", "emp_hucre" }, { "icon_light", "light" }, { "icon_clock", "clock" },
        };

        // ================================================================ pano yüzü (dress_room_board.js › boardFace)

        /// <summary>
        /// <c>tx_board_face</c> 4096 × 2048, 1160 × 600 mm iç alan (eşit olmayan ölçek, UV 0–1). Zemin #56604F ve 2600 boya lekesi;
        /// depo tonu; her cepte kesikli kart hattı, kişi simgesi ve üç kesikli eşya şeridi (saha ceplerinde ilki somon); yedi eğitim
        /// satırının sekiz çizgisi (16 mm); Boşta yelpaze izi ve ok; depo tutucuları, kit yerleri ve tarif defterinin gölge hattı.
        /// </summary>
        static Paint BoardFace()
        {
            const float W = BoardLayout.InnerWidthMm, H = BoardLayout.InnerHeightMm;
            // K4'te ekran pano yüzünde ~1,5 px/mm gösterir; 2048 × 1024 (1,77 px/mm) yeter, 4096 × 2048 dört kat yavaş
            var p = new Paint(2048, 1024, 2048f / W, 1024f / H, Hex("#56604F"));
            var r = new Lcg(11);
            var mottle = Hex("#9AA38F");
            for (int i = 0; i < 2600; i++)
            {
                var c = r.Next() > 0.5f ? Color.black : mottle;
                float x = r.Next() * W, y = r.Next() * H, w = 1 + r.Next() * 5, h = 0.6f + r.Next() * 2;
                p.Fill(x, y, w, h, c, 0.05f);
            }

            // depo tonu ve hattı
            var depot = BoardLayout.Depot;
            p.Fill(0, depot.y, W, depot.height, Rgb(20, 24, 18), 0.22f);
            p.StrokeRect(2, depot.y, W - 4, depot.height - 2, 0.6f, Cream, 0.20f);

            // cepler: kesikli kart hattı, kişi simgesi, üç eşya şeridi
            var salmon = Rgb(240, 150, 120);
            foreach (var job in BoardLayout.Jobs)
            {
                var c = BoardLayout.CardCentre(job);
                float x0 = c.x - 40, y0 = c.y - 60;
                p.StrokeRect(x0, y0, 80, 120, 0.55f, Cream, 0.55f, 2.2f, 1.6f);
                var icon = p.MaskMm(c.x - 12, c.y - 22, c.x + 12, c.y + 2);
                Icon(icon, p, "person", c.x - 12, c.y - 22, 24);
                p.Composite(icon, Cream, 0.28f);
                bool field = job == "tek" || job == "emp" || job == "ana";
                for (int i = 0; i < 3; i++)
                    p.StrokeRect(x0, y0 + 121 + i * 14, 80, 13, 0.55f, i == 0 && field ? salmon : Cream, i == 0 && field ? 0.7f : 0.40f, 2.2f, 1.6f);
            }

            // eğitim konuları: yedi satır, sekiz ince çizgi
            var row = BoardLayout.TopicRow(0);
            for (int i = 0; i <= 7; i++) p.HLine(row.x, row.y + row.height * i, row.x + 72, Cream, 0.4f, 0.22f);

            // Boşta yelpaze izi: dört kesikli kart hattı ve sola ok
            var d = BoardLayout.Deck;
            for (int i = 1; i <= 4; i++) p.StrokeRect(d.x - 40 - i * 78, d.y - 60, 80, 120, 0.55f, Cream, 0.30f - i * 0.05f, 2.2f, 1.6f);
            p.HLine(d.x - 52, d.y + 74, d.x - 300, Cream, 1f, 0.35f);
            var head = p.MaskMm(d.x - 306, d.y + 69, d.x - 296, d.y + 79);
            head.Polygon(new[] { p.P(d.x - 306, d.y + 74), p.P(d.x - 296, d.y + 69), p.P(d.x - 296, d.y + 79) });
            p.Composite(head, Cream, 0.35f);

            // depo: dört hammadde tutucusu (18 mm simge), altı kit yeri, tarif defterinin gölge hattı
            var kinds = BoardLayout.MaterialKinds;
            for (int i = 0; i < kinds.Length; i++)
            {
                var m = BoardLayout.Material(i);
                p.StrokeRect(m.x - 40, m.y - 28, 80, 56, 0.5f, Cream, 0.32f, 2f, 1.5f);
                var icon = p.MaskMm(m.x - 9, m.y - 9, m.x + 9, m.y + 9);
                Icon(icon, p, kinds[i], m.x - 9, m.y - 9, 18, new[] { 2f, 1.5f });
                p.Composite(icon, Cream, 0.18f);
            }
            for (int i = 0; i < BoardLayout.KitKinds.Length; i++)
            {
                var k = BoardLayout.Kit(i);
                p.StrokeRect(k.x - 40, k.y - 25, 80, 50, 0.5f, Cream, 0.32f, 2f, 1.5f);
            }
            var book = BoardLayout.Book;
            p.StrokeRect(book.x - 52, book.y + 6, 105, 140, 0.5f, Cream, 0.18f);
            return p;
        }

        // ================================================================ iş emri formu (boardtex.js › orderSheet, eleştiri #2)

        /// <summary>
        /// <c>tx_order_sheet</c> 440 × 170 mm, 5 piksel/mm (2200 × 850): kâğıt, çerçeve, kırmızı çizgi y 18, kesikli 112 × 80 tarif
        /// kutusu (8, 22) ve soluk defter simgesi, dört kesikli 66 × 56 girdi alanı (x 132 + 74i) ve soluk hammadde simgeleri, y 110
        /// çizgisi. Yazılar (İŞ EMRİ · ÜRETİM, No, TARİF, ÜRETİCİ, KAPANIŞTA) TMP.
        /// </summary>
        static Paint OrderSheet()
        {
            const float W = 440, H = 170, Ppm = 5;
            var p = new Paint((int)(W * Ppm), (int)(H * Ppm), Ppm, Ppm, Hex("#EFE7D1"));
            p.Grain(W, H, 41);
            p.StrokeRect(2, 2, W - 4, H - 4, 0.5f, Ink);
            p.HLine(6, 18, W - 6, Red, 0.6f);
            p.StrokeRect(8, 22, 112, 80, 0.5f, InkSoft, 0.45f, 2.2f, 1.6f);
            var bookIcon = p.MaskMm(54, 46, 74, 66);
            Icon(bookIcon, p, "book", 54, 46, 20);
            p.Composite(bookIcon, InkSoft, 0.22f);
            var kinds = BoardLayout.MaterialKinds;
            for (int i = 0; i < kinds.Length; i++)
            {
                float x = 132 + i * 74;
                p.StrokeRect(x, 31, 66, 56, 0.4f, InkSoft, 0.28f, 1.6f, 1.4f);
                var icon = p.MaskMm(x + 23, 49, x + 43, 69);
                Icon(icon, p, kinds[i], x + 23, 49, 20);
                p.Composite(icon, InkSoft, 0.2f);
            }
            p.HLine(6, 110, W - 6, RuleColor, 0.3f);
            return p;
        }

        // ================================================================ kart tabanları (boardtex.js; yalnız durağan parçalar)

        static Paint Card(Vector2 mm, string paper, long seed)
        {
            var p = new Paint(Mathf.RoundToInt(mm.x * CardPxPerMm), Mathf.RoundToInt(mm.y * CardPxPerMm), CardPxPerMm, CardPxPerMm, Hex(paper));
            p.Grain(mm.x, mm.y, seed);
            return p;
        }

        /// <summary>
        /// Kişi kartı 80 × 120 (PLAN §3.2): kâğıt ve doku, dış/iç çerçeve (0,45 / 0,18), ad altındaki kırmızı çizgi (y 17,2), portre
        /// zemini PAPER2 ve 0,5 mm çerçevesi (5, 29) 44 × 48,2, değer çizgisi (y 80), şerit bandı (3,2; 104,5) 73,6 × 12,4 %6 ve üst
        /// çizgisi. Hücre ayraçları (TR/EN farklı), pipler ve yazılar tuvalde.
        /// </summary>
        static Paint PersonBase()
        {
            const float W = 80, H = 120;
            var p = Card(new Vector2(W, H), "#EFE6D0", 7);
            p.StrokeRect(1.6f, 1.6f, W - 3.2f, H - 3.2f, 0.45f, Ink);
            p.StrokeRect(2.5f, 2.5f, W - 5, H - 5, 0.18f, Ink);
            p.HLine(5, 17.2f, W - 5, Red, 0.6f);
            p.Fill(5, 29, 44, 48.2f, Paper2);
            p.StrokeRect(5, 29, 44, 48.2f, 0.5f, Ink);
            p.HLine(5, 80, W - 5, RuleColor, 0.3f);
            p.Fill(3.2f, 104.5f, W - 6.4f, 12.4f, InkSoft, 0.06f);
            p.HLine(3.2f, 104.5f, W - 3.2f, Ink, 0.35f);
            return p;
        }

        /// <summary>
        /// Eşya kartı 80 × 50 (PLAN §3.3), yuvaya göre üç doku: kâğıt #ECE2C6, çerçeve (1,4; 1,4) 77,2 × 47,2, bant çizgisi y 34,6 ve
        /// yuva bloğu (1,4; 34,6) 11,8 × 14 — kit düz #4A5140, zırh #7D8079 üstüne çapraz tarama #3E413C (0,35 mm, 1,6 mm aralık),
        /// yardımcı #B08F4E üstüne noktalar #5E4826 (r 0,32, 1,6 mm ızgara, sıra kaydırmalı). Desen gri tonda da ayrılır.
        /// </summary>
        static Paint ItemBase(ItemSlotKind slot)
        {
            const float W = 80, H = 50;
            var p = Card(new Vector2(W, H), "#ECE2C6", 7 * 37);
            p.StrokeRect(1.4f, 1.4f, W - 2.8f, H - 2.8f, 0.4f, Ink);
            float by = H - 15.4f;
            p.HLine(1.4f, by, W - 1.4f, Ink, 0.4f);
            SlotBand(p, slot, 1.4f, by, 11.8f, 14);
            return p;
        }

        static void SlotBand(Paint p, ItemSlotKind slot, float x, float y, float w, float h)
        {
            switch (slot)
            {
                case ItemSlotKind.Kit:
                    p.Fill(x, y, w, h, Hex("#4A5140"));
                    break;
                case ItemSlotKind.Armor:
                {
                    p.Fill(x, y, w, h, Hex("#7D8079"));
                    var m = p.MaskMm(x, y, x + w, y + h, 2);
                    for (float i = -h; i < w; i += 1.6f)
                    {
                        m.Segment(p.P(x + i, y + h), p.P(x + i + h, y), 0.35f * p.S, false);
                        m.Segment(p.P(x + i, y), p.P(x + i + h, y + h), 0.35f * p.S, false);
                    }
                    m.ClipRect(x * p.Sx, y * p.Sy, (x + w) * p.Sx, (y + h) * p.Sy);
                    p.Composite(m, Hex("#3E413C"));
                    break;
                }
                default:
                {
                    p.Fill(x, y, w, h, Hex("#B08F4E"));
                    var m = p.MaskMm(x, y, x + w, y + h, 2);
                    for (float yy = y + 0.9f; yy < y + h; yy += 1.6f)
                        for (float xx = x + 0.9f + ((yy - y) % 3.2f > 1.5f ? 0.8f : 0); xx < x + w; xx += 1.6f)
                            m.Polygon(ArcPts(p, xx, yy, 0.32f, 0.32f, 0, Mathf.PI * 2, 24));
                    m.ClipRect(x * p.Sx, y * p.Sy, (x + w) * p.Sx, (y + h) * p.Sy);
                    p.Composite(m, Hex("#5E4826"));
                    break;
                }
            }
        }

        /// <summary>Hammadde stok kartı 80 × 56 (PLAN §3.4): kâğıt #E9E0C4, çerçeve 77,2 × 53,2, y 38 çizgisi (RULE 0,3).</summary>
        static Paint MaterialBase()
        {
            const float W = 80, H = 56;
            var p = Card(new Vector2(W, H), "#E9E0C4", 5 * 53);
            p.StrokeRect(1.4f, 1.4f, W - 2.8f, H - 2.8f, 0.4f, Ink);
            p.HLine(1.4f, 38, W - 1.4f, RuleColor, 0.3f);
            return p;
        }

        /// <summary>Fiş 56 × 40 (PLAN §3.4): kâğıt #E9E0C4, çerçeve (1,2; 1,2) 53,6 × 37,6, üstte 3 mm ayrılmış bandı (%85 kırmızı).</summary>
        static Paint ChipBase()
        {
            const float W = 56, H = 40;
            var p = Card(new Vector2(W, H), "#E9E0C4", 5 * 11);
            p.StrokeRect(1.2f, 1.2f, W - 2.4f, H - 2.4f, 0.4f, Ink);
            p.Fill(1.2f, 1.2f, W - 2.4f, 3, Rgb(154, 30, 20), 0.85f);
            return p;
        }

        /// <summary>Tarif kartı 112 × 80 (boardtex.js › recipeCard; eleştiri #1): kâğıt #F0E8D2, iki çerçeve, kırmızı çizgi y 18,6, y 58 çizgisi.</summary>
        static Paint RecipeBase()
        {
            const float W = 112, H = 80;
            var p = Card(new Vector2(W, H), "#F0E8D2", 7 * 17);
            p.StrokeRect(1.4f, 1.4f, W - 2.8f, H - 2.8f, 0.45f, Ink);
            p.StrokeRect(2.4f, 2.4f, W - 4.8f, H - 4.8f, 0.16f, Ink);
            p.HLine(6, 18.6f, W - 6, Red, 0.5f);
            p.HLine(6, 58, W - 6, RuleColor, 0.3f);
            return p;
        }

        // ================================================================ sprite'lar

        /// <summary>Üretilen sprite: tuval, çözünürlük (piksel/mm) ve 9 dilim payı (mm; sol, alt, sağ, üst).</summary>
        struct SpriteBake
        {
            public string name;
            public Paint paint;
            public float pxPerMm;
            public Vector4 borderMm;
        }

        static SpriteBake Bake(string name, Paint paint, float pxPerMm, float borderMm = 0) =>
            new SpriteBake { name = name, paint = paint, pxPerMm = pxPerMm, borderMm = new Vector4(borderMm, borderMm, borderMm, borderMm) };

        static IEnumerable<SpriteBake> Sprites()
        {
            yield return Bake("pip_fill", PipFill(), PipPx);
            yield return Bake("pip_ring", PipRing(), PipPx);
            yield return Bake("pip_reserved", PipReserved(), PipPx);
            yield return Bake("frame_selected", FrameSelected(), 8, 2);
            yield return Bake("frame_target", FrameTarget(), 8, 5);
            yield return Bake("dim", Solid(4, 4, 8), 8);
            yield return Bake("ring_pencil", RingPencil(), 8);
            yield return Bake("smudge", Smudge(), 8);
            yield return Bake("tape", Tape(), 10);
            yield return Bake("stamp_frame", StampFrame(), 10, 2);
            yield return Bake("corner_restricted", Corner(24.4f), 10);
            yield return Bake("corner_lost", Corner(28.4f), 10);
            yield return Bake("rule", Solid(2, 2, 8), 8, 0.5f);
            foreach (var name in SpriteNames)
                if (IconOfSprite.TryGetValue(name, out var kind)) yield return Bake(name, IconSprite(kind), IconPx / 10f);
        }

        /// <summary>Piplerin çözünürlüğü (piksel/mm): 3,8 mm → 76 piksel.</summary>
        const float PipPx = 20;
        /// <summary>Simge hücresi 10 mm, 256 piksel (kartta 23 mm'ye kadar 8 piksel/mm'den keskin).</summary>
        const float IconPx = 256;
        const float PipMm = 3.8f;

        /// <summary>Dolu pip Ø 3,8 mm (kişi ustalığı r 1,9; hammaddede 3,4 mm'ye küçültülür). Beyaz; boya INK.</summary>
        static Paint PipFill()
        {
            int n = Mathf.RoundToInt(PipMm * PipPx);
            var p = Paint.Clear(n, n, PipPx, White);
            var m = p.Whole();
            m.Polygon(ArcPts(p, PipMm / 2, PipMm / 2, PipMm / 2, PipMm / 2, 0, Mathf.PI * 2, 96));
            p.Composite(m, White);
            return p;
        }

        /// <summary>Boş pip halkası Ø 3,8 mm, çizgi 0,35 (dış kenar r 1,9). Beyaz; boya #8C826E.</summary>
        static Paint PipRing()
        {
            int n = Mathf.RoundToInt(PipMm * PipPx);
            var p = Paint.Clear(n, n, PipPx, White);
            var m = p.Whole();
            float r = PipMm / 2 - 0.175f;
            m.Stroke(ArcPts(p, PipMm / 2, PipMm / 2, r, r, 0, Mathf.PI * 2, 96), true, 0.35f * PipPx, true);
            p.Composite(m, White);
            return p;
        }

        /// <summary>Ayrılmış pip Ø 3,8 mm: halka 0,45 ve sol alttan sağ üste çizgi (±1,2 mm). Beyaz; boya RED.</summary>
        static Paint PipReserved()
        {
            int n = Mathf.RoundToInt(PipMm * PipPx);
            var p = Paint.Clear(n, n, PipPx, White);
            var m = p.Whole();
            float c = PipMm / 2, r = c - 0.225f;
            m.Stroke(ArcPts(p, c, c, r, r, 0, Mathf.PI * 2, 96), true, 0.45f * PipPx, true);
            m.Segment(p.P(c - 1.2f, c + 1.2f), p.P(c + 1.2f, c - 1.2f), 0.45f * PipPx, false);
            p.Composite(m, White);
            return p;
        }

        /// <summary>Seçim çerçevesi (9 dilim, 8 mm, pay 2 mm): 1,2 mm çizgi, kenardan 0,6 mm içeride. Beyaz; boya #E3B23C.</summary>
        static Paint FrameSelected()
        {
            const float S = 8, Ppm = 8;
            var p = Paint.Clear((int)(S * Ppm), (int)(S * Ppm), Ppm, White);
            var m = p.Whole();
            const float c = 0.6f + 0.6f;
            m.Stroke(new[] { p.P(c, c), p.P(S - c, c), p.P(S - c, S - c), p.P(c, S - c) }, true, 1.2f * Ppm, false);
            p.Composite(m, White);
            return p;
        }

        /// <summary>
        /// Hedef çerçevesi (9 dilim, 12 mm, pay 5 mm; dress_room_board.js › glowFrame): hedef kenarı resmin kenarından 4 mm içeride
        /// (BoardMarks dikdörtgeni her yandan 4 mm büyütür); 1,1 mm çekirdek çizgi %95, dışında altı hale çizgisi (6 → 1 mm, %10 → %35);
        /// hale resmin kenarına doğru 1,2 mm'de söner. Beyaz; boya amber #FFB248 (klavye odağı da aynı).
        /// </summary>
        static Paint FrameTarget()
        {
            const float S = 12, Ppm = 8, Pad = 4;
            var p = Paint.Clear((int)(S * Ppm), (int)(S * Ppm), Ppm, White);
            for (int i = 0; i < 6; i++)
            {
                float inset = Pad - (3 - i * 0.6f);
                var halo = p.Whole();
                halo.Stroke(new[] { p.P(inset, inset), p.P(S - inset, inset), p.P(S - inset, S - inset), p.P(inset, S - inset) }, true, (6 - i) * Ppm, false);
                p.Composite(halo, White, 0.10f + i * 0.05f);
            }
            var core = p.Whole();
            core.Stroke(new[] { p.P(Pad, Pad), p.P(S - Pad, Pad), p.P(S - Pad, S - Pad), p.P(Pad, S - Pad) }, true, 1.1f * Ppm, false);
            p.Composite(core, White, 0.95f);
            // kenarda sönüş
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float edge = Mathf.Min(Mathf.Min(x + 0.5f, p.W - x - 0.5f), Mathf.Min(y + 0.5f, p.H - y - 0.5f)) / Ppm;
                    float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / 1.2f));
                    var c = p.Px[y * p.W + x];
                    c.a *= k;
                    p.Px[y * p.W + x] = c;
                }
            return p;
        }

        /// <summary>Düz beyaz karo (sönük örtü, ince çizgi).</summary>
        static Paint Solid(float wMm, float hMm, float ppm)
        {
            var p = Paint.Clear(Mathf.RoundToInt(wMm * ppm), Mathf.RoundToInt(hMm * ppm), ppm, White);
            p.Fill(0, 0, wMm, hMm, White);
            return p;
        }

        /// <summary>
        /// Gres kalemi halkası 74 × 15 mm (dress_room_board.js › pencilRing; 16 mm satır aralığı): açık elips, yarıçap 35 × 5,3 mm,
        /// ±%4 dalga, 0,15 → 2π + 0,55 rad, taşan uç 2,2 mm/rad yukarı kayar; 1,1 mm yuvarlak uçlu. Beyaz; boya rgba(176,36,24,0,92).
        /// </summary>
        static Paint RingPencil()
        {
            const float W = 74, H = 15, Ppm = 8;
            var p = Paint.Clear((int)(W * Ppm), (int)(H * Ppm), Ppm, White);
            var pts = new List<Vector2>();
            for (float a = 0.15f; a <= Mathf.PI * 2 + 0.55f; a += 0.05f)
            {
                float rr = 1 + 0.04f * Mathf.Sin(a * 3);
                float x = W / 2 + Mathf.Cos(a) * (W / 2 - 2) * rr;
                float y = H / 2 + Mathf.Sin(a) * (H / 2 - 2.2f) * rr - (a > Mathf.PI * 2 ? (a - Mathf.PI * 2) * 2.2f : 0);
                pts.Add(p.P(x, y));
            }
            var m = p.Whole();
            m.Stroke(pts, false, 1.1f * Ppm, true);
            p.Composite(m, White);
            return p;
        }

        /// <summary>
        /// Tebeşir silintisi 64 × 16 mm: yatay sürtme çizgileri (tohumlu), kenarlara doğru sönen alfa. Beyaz; boya tebeşir #EDEBE2,
        /// saydamlığı BoardMarks sürer (tepe 0,35).
        /// </summary>
        static Paint Smudge()
        {
            const float W = 64, H = 16, Ppm = 8;
            var p = Paint.Clear((int)(W * Ppm), (int)(H * Ppm), Ppm, White);
            var a = new float[p.W * p.H];
            for (int i = 0; i < a.Length; i++) a[i] = 0.45f;
            var r = new Lcg(97);
            for (int s = 0; s < 160; s++)
            {
                float x = r.Next() * W, y = r.Next() * H, len = 4 + r.Next() * 20, h = 0.5f + r.Next() * 1.1f, v = 0.10f + r.Next() * 0.2f;
                int x0 = Mathf.Max(0, (int)((x - len / 2) * Ppm)), x1 = Mathf.Min(p.W - 1, (int)((x + len / 2) * Ppm));
                int y0 = Mathf.Max(0, (int)((y - h / 2) * Ppm)), y1 = Mathf.Min(p.H - 1, (int)((y + h / 2) * Ppm));
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++) a[yy * p.W + xx] = Mathf.Min(1, a[yy * p.W + xx] + v);
            }
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float ex = Mathf.Min(x + 0.5f, p.W - x - 0.5f) / Ppm, ey = Mathf.Min(y + 0.5f, p.H - y - 0.5f) / Ppm;
                    float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(ex / 8f)) * Mathf.SmoothStep(0, 1, Mathf.Clamp01(ey / 4f));
                    p.Px[y * p.W + x] = new Color(1, 1, 1, a[y * p.W + x] * k);
                }
            return p;
        }

        /// <summary>
        /// YARALI bandı 30 × 8,4 mm (PLAN §3.2): #D6C9A0 %72, içe çizilmiş kesikli kenar rgba(120,104,70,0,6) 0,25 mm (0,9 / 0,6).
        /// Renkli (boya beyaz); −35,5° dönüşü RectTransform'da.
        /// </summary>
        static Paint Tape()
        {
            const float W = 30, H = 8.4f, Ppm = 10;
            var fill = Hex("#D6C9A0");
            var p = Paint.Clear((int)(W * Ppm), Mathf.RoundToInt(H * Ppm), Ppm, fill);
            p.Fill(0, 0, W, H, fill, 0.72f);
            const float c = 0.125f;
            var m = p.Whole();
            m.Dashed(new[] { p.P(c, c), p.P(W - c, c), p.P(W - c, H - c), p.P(c, H - c) }, true, 0.25f * Ppm, 0.9f * Ppm, 0.6f * Ppm, false);
            p.Composite(m, Rgb(120, 104, 70), 0.6f);
            return p;
        }

        /// <summary>
        /// YATIYOR damgasının çift çerçevesi (9 dilim, 8 mm, pay 2 mm): dış çizgi 0,9 mm resmin kenarından içe (çerçeve boyu =
        /// sözcük + pay + çizgi), iç çizgi 0,35 mm 1 mm içeride. Beyaz; boya BLUE #1A2F5A, saydamlık 0,86.
        /// </summary>
        static Paint StampFrame()
        {
            const float S = 8, Ppm = 10;
            var p = Paint.Clear((int)(S * Ppm), (int)(S * Ppm), Ppm, White);
            var m = p.Whole();
            float o = 0.45f, i = 0.45f + 1f;
            m.Stroke(new[] { p.P(o, o), p.P(S - o, o), p.P(S - o, S - o), p.P(o, S - o) }, true, 0.9f * Ppm, false);
            m.Stroke(new[] { p.P(i, i), p.P(S - i, i), p.P(S - i, S - i), p.P(i, S - i) }, true, 0.35f * Ppm, false);
            p.Composite(m, White);
            return p;
        }

        /// <summary>Köşe üçgeni (dik açı sağ üstte): KISITLI 24,4 mm, KAYIP 28,4 mm. Beyaz; boya RED / #121110.</summary>
        static Paint Corner(float sizeMm)
        {
            const float Ppm = 10;
            int n = Mathf.RoundToInt(sizeMm * Ppm);
            var p = Paint.Clear(n, n, Ppm, White);
            var m = p.Whole();
            m.Polygon(new[] { p.P(0, 0), p.P(sizeMm, 0), p.P(sizeMm, sizeMm) });
            p.Composite(m, White);
            return p;
        }

        /// <summary>10 mm simge hücresi, 256 piksel; beyaz şekil.</summary>
        static Paint IconSprite(string kind)
        {
            const float Ppm = IconPx / 10f;
            var p = Paint.Clear((int)IconPx, (int)IconPx, Ppm, White);
            var m = p.Whole();
            Icon(m, p, kind, 0, 0, 10);
            p.Composite(m, White);
            return p;
        }
    }
}
