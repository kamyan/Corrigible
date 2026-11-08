using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 3 kart yüzleri (PLAN §5.1 E8–E10, eleştirmen 5 ve 6): dört kart prefab'ının kökü, iç içe modeli ve yüz tuvali;
    /// yüz mm'si → RectTransform yerleşimi (§3.2–3.4 tabloları, ± 0,05 mm); yazı kuralları (otomatik punto yok, paylaşılan font
    /// malzemesi, ışın hedefi yok, DeskLocText yok, en çok 4 font malzemesi); portre dörtgeni ve kırpması; ustalık harflerinin
    /// birbirine değmemesi (TR + EN) ve en kötü TR/EN yazıların tek satıra sığması.
    /// Prefab'lar <c>Corrigible/Revizyon 3/Aşama 3/Kart prefab'larını kur</c> ile kurulur; yoksa testler atlanır.
    /// Aşama 5 (PLAN §1.6, W2): tarif kartı (<c>RecipeCard.prefab</c>) aynı kalıpta; yüz düzeni, ilerleme kutuları, TR/EN taşma.
    /// </summary>
    public class Rev3CardFaceTests
    {
        const string Dir = "Assets/Corrigible/Desk/Prefabs/Rev3Board/";
        const string PersonPath = Dir + "PersonCard.prefab", ItemPath = Dir + "ItemCard.prefab";
        const string MaterialPath = Dir + "MaterialCard.prefab", ChipPath = Dir + "ChipCard.prefab";
        /// <summary>Aşama 5 (R18b): tarif kartı; Aşama 5'in tek seferlik kurulumuyla oluştu (script silindi).</summary>
        const string RecipePath = Dir + "RecipeCard.prefab";
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const float Tol = 0.05f;   // mm

        static readonly float[] Baselines = { 35.4f, 41.7f, 48.0f, 55.8f, 62.1f, 68.4f, 76.2f };
        static readonly float[] PipX = { 61.4f, 66.0f, 70.6f };
        static readonly float[] StatCentresTr = { 16.2f, 39.6f, 63.0f }, StatCentresEn = { 14.0f, 36.0f, 62.0f };
        static readonly float[] StatWidthsTr = { 23.4f, 23.4f, 23.4f }, StatWidthsEn = { 21f, 23f, 29f };
        static readonly float[] DividersTr = { 28.4f, 51.8f }, DividersEn = { 24.5f, 47.5f };

        struct Spec
        {
            public string Path, Model;
            public BoardCardKind Kind;
            public Vector2 Mm;
            public Spec(string path, string model, BoardCardKind kind, float w, float h) { Path = path; Model = model; Kind = kind; Mm = new Vector2(w, h); }
        }

        static readonly Spec[] Specs =
        {
            new Spec(PersonPath, "CardPerson", BoardCardKind.Person, 80, 120),
            new Spec(ItemPath, "CardItem", BoardCardKind.Item, 80, 50),
            new Spec(MaterialPath, "CardMaterial", BoardCardKind.Material, 80, 56),
            new Spec(ChipPath, "CardChip", BoardCardKind.Chip, 56, 40),
            new Spec(RecipePath, "CardRecipe", BoardCardKind.Recipe, 112, 80),
        };

        readonly List<Object> spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.DestroyImmediate(o);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================== E8 · kök, model, tuval

        [Test]
        public void Prefabs_RootColliderModelAndFaceCanvas()
        {
            foreach (var spec in Specs)
            {
                var prefab = Load(spec.Path);
                string n = spec.Kind + ": ";
                var view = prefab.GetComponent<BoardCardView>();
                Assert.IsTrue(view != null, n + "kökte BoardCardView");
                Assert.AreEqual(spec.Kind, view.Kind, n + "tür");

                var box = prefab.GetComponent<BoxCollider>();
                Assert.IsTrue(box != null, n + "kökte BoxCollider");
                Assert.AreSame(box, view.Collider, n + "BoardCardView.hit");
                Near(new Vector3(spec.Mm.x / 1000f, spec.Mm.y / 1000f, 0.004f), box.size, 1e-5f, n + "çarpıştırıcı boyutu");
                Near(new Vector3(0, 0, -0.002f), box.center, 1e-5f, n + "çarpıştırıcı merkezi");

                var model = prefab.transform.Find("Model");
                Assert.IsTrue(model != null, n + "Model çocuğu");
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                Assert.IsNotNull(source, n + "Model iç içe prefab olmalı");
                Assert.AreEqual(Rev3ModelImporter.PrefabPath("board", spec.Model), AssetDatabase.GetAssetPath(source), n + "Model kaynağı");
                Assert.AreSame(model.GetComponent<Rev3Model>(), view.Model, n + "BoardCardView.model");
                Assert.IsNotNull(view.Body, n + "BoardCardView.body");

                var contents = PrefabUtility.LoadPrefabContents(spec.Path);
                try
                {
                    var m = contents.transform.Find("Model").gameObject;
                    Assert.IsFalse(PrefabUtility.HasPrefabInstanceAnyOverrides(m, false), n + "iç içe modelde geçersiz kılma olmamalı");
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }

                var face = prefab.transform.Find("Face") as RectTransform;
                Assert.IsTrue(face != null, n + "Face çocuğu (RectTransform)");
                var canvas = face.GetComponent<Canvas>();
                Assert.IsTrue(canvas != null, n + "Face tuvali");
                Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, n + "World Space");
                Assert.AreSame(face.GetComponent<CardFace>(), view.Face, n + "BoardCardView.face");
                Assert.AreEqual(spec.Kind, view.Face.Kind, n + "yüz türü");
                Near(spec.Mm, face.sizeDelta, 1e-4f, n + "tuval sizeDelta = kart mm");
                Near(new Vector2(0, 1), face.pivot, 1e-6f, n + "sol üst pivot");
                Near(Vector3.one * 0.001f, face.localScale, 1e-7f, n + "ölçek 0,001");
                Near(new Vector3(-spec.Mm.x / 2000f, spec.Mm.y / 2000f, -0.0010f), face.localPosition, 1e-6f, n + "tuval konumu");
                Assert.AreEqual(Quaternion.identity, face.localRotation, n + "tuval dönüşü");

                Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true), n + "GraphicRaycaster olmamalı");
                Assert.IsEmpty(prefab.GetComponentsInChildren<Mask>(true), n + "Mask olmamalı");
                Assert.IsEmpty(prefab.GetComponentsInChildren<RectMask2D>(true), n + "RectMask2D olmamalı");
            }
        }

        [Test]
        public void FaceCanvasCorners_MatchModelFaceSlot()
        {
            foreach (var spec in Specs)
            {
                var card = Spawn(spec.Path);
                var slot = card.Model != null ? card.Model.Slot("FACE") : null;
                Assert.IsNotNull(slot, spec.Kind + ": modelde FACE yuvası");
                var a = new Vector3[4];
                ((RectTransform)slot.transform).GetWorldCorners(a);
                var b = new Vector3[4];
                ((RectTransform)card.Face.transform).GetWorldCorners(b);
                for (int i = 0; i < 4; i++)
                    Assert.Less(Vector3.Distance(a[i], b[i]), 0.0001f, spec.Kind + ": tuval köşesi " + i + " FACE ± 0,1 mm (" + a[i].ToString("F5") + " / " + b[i].ToString("F5") + ")");
            }
        }

        // ================================================================== E9 · yerleşim

        [Test]
        public void PersonFace_LayoutMatchesPlan()
        {
            var card = Spawn(PersonPath);
            var pf = card.Face as PersonCardFace;
            Assert.IsNotNull(pf, "PersonCardFace");
            var face = (RectTransform)pf.transform;

            TextAt(face, pf.NameText, 4.6f, 14.6f, 14f, HorizontalAlignmentOptions.Left, "ad");
            Width(pf.NameText, 58.4f, "ad");
            TextAt(face, pf.JobText, 4.8f, 26.2f, 9.5f, HorizontalAlignmentOptions.Left, "meslek");
            Width(pf.JobText, 72f, "meslek");
            Assert.AreEqual(PersonCardFace.Rows, pf.Letters.Count, "7 ustalık satırı");
            Assert.AreEqual(PersonCardFace.Rows * PersonCardFace.PipsPerRow, pf.Pips.Count, "21 pip");
            for (int i = 0; i < PersonCardFace.Rows; i++)
            {
                TextAt(face, pf.Letters[i], 52.5f, Baselines[i], 9.5f, HorizontalAlignmentOptions.Left, "ustalık harfi " + i);
                for (int j = 0; j < PersonCardFace.PipsPerRow; j++)
                {
                    var pip = pf.Pips[i * PersonCardFace.PipsPerRow + j];
                    Assert.IsNotNull(pip, "pip " + i + "." + j);
                    CentreAt(face, pip.rectTransform, new Vector2(PipX[j], Baselines[i] - 2.8f), new Vector2(3.8f, 3.8f), "pip " + i + "." + j);
                }
            }
            for (int i = 0; i < PersonCardFace.Stats; i++)
            {
                TextAt(face, pf.StatLabels[i], StatCentresTr[i], 89f, 9.5f, HorizontalAlignmentOptions.Center, "değer etiketi " + i);
                TextAt(face, pf.StatValues[i], StatCentresTr[i], 100.6f, 11.5f, HorizontalAlignmentOptions.Center, "değer " + i);
                Width(pf.StatLabels[i], StatWidthsTr[i], "değer etiketi " + i);
            }
            for (int i = 0; i < 2; i++)
                CentreAt(face, pf.Dividers[i], new Vector2(DividersTr[i], 92f), new Vector2(0.3f, 20f), "bölme " + i);
            TextAt(face, pf.StripText, 40f, 113.7f, 9.4f, HorizontalAlignmentOptions.Center, "şerit");
            Width(pf.StripText, 72f, "şerit");

            // durum katmanları
            var ov = pf.Overlay;
            Assert.IsNotNull(ov, "CardStateOverlay");
            var tape = Field<Image>(ov, "tape");
            Assert.IsNotNull(tape, "bant");
            PivotAt(face, tape.rectTransform, new Vector2(12f, 36f), 35.5f, "bant");
            Near(new Vector2(30f, 8.4f), tape.rectTransform.rect.size, Tol, "bant boyutu");
            TextAt(face, ov.InjuredWord, 47.5f, 75f, 6.4f, HorizontalAlignmentOptions.Right, "YARALI");

            var stamp = Field<RectTransform>(ov, "stamp");
            Assert.IsNotNull(stamp, "damga");
            PivotAt(face, stamp, new Vector2(27f, 53.1f), 14.9f, "damga");
            Assert.IsNotNull(Field<RectTransform>(ov, "stampFrame"), "damga çerçevesi");
            Assert.AreEqual(8.6f, ov.StampWord.fontSize, Tol, "damga em");
            Near(new Vector2(0, -0.12f * 8.6f), ov.StampWord.rectTransform.anchoredPosition, Tol, "damga taban çizgisi (merkezin 0,12 em altı)");
            Assert.AreEqual(VerticalAlignmentOptions.Baseline, ov.StampWord.verticalAlignment, "damga hizası");

            RectAt(face, Field<Image>(ov, "restrictedCorner").rectTransform, new Vector2(54f, 1.6f), new Vector2(24.4f, 24.4f), "KISITLI köşesi");
            CornerWordAt(face, ov.RestrictedWord, new Vector2(71.4f, 8.6f), "KISITLI");
            RectAt(face, Field<Image>(ov, "lostCorner").rectTransform, new Vector2(50f, 1.6f), new Vector2(28.4f, 28.4f), "KAYIP köşesi");
            CornerWordAt(face, ov.LostWord, new Vector2(70.4f, 9.6f), "KAYIP");
            RectAt(face, Field<Image>(ov, "selectedFrame").rectTransform, new Vector2(0.6f, 0.6f), new Vector2(78.8f, 118.8f), "seçim çerçevesi");

            // İNCELE: 3B sözcük bayrağın kartın üstünden taşan kırmızı dilinde ortalı (model bayrağı yüz x 23–41 mm'de; CardStateOverlay
            // onu gösterirken 6 mm yukarı alır; ataş sözcüğü örtmesin diye bayrağın önünde), 4,5 mm em (fontSize = em mm / 100; CP-Bold
            // eşaralıklı, altı harf 16,2 mm). Ölçü bayrak gösterilirken (inceleme durumu).
            ov.Apply(PersonStates.Review, new BoardWords());
            var flag = ov.FlagWord;
            Assert.IsNotNull(flag, "İNCELE sözcüğü");
            Assert.IsInstanceOf<TextMeshPro>(flag, "İNCELE sözcüğü 3B TextMeshPro");
            Assert.AreEqual(0.045f, flag.fontSize, 0.0005f, "İNCELE em 4,5 mm");
            Assert.IsNotNull(ov.Flag, "Flag parçası bağlı");
            var flagRenderer = ov.Flag.GetComponentInChildren<Renderer>(true);
            Assert.IsNotNull(flagRenderer, "bayrak modeli");
            var fMin = card.transform.InverseTransformPoint(flagRenderer.bounds.min);
            var fMax = card.transform.InverseTransformPoint(flagRenderer.bounds.max);
            var local = card.transform.InverseTransformPoint(flag.transform.position);
            Assert.AreEqual((fMin.x + fMax.x) / 2, local.x, 0.0002f, "İNCELE bayrağın ortasında");
            Assert.AreEqual(Mathf.Max(fMin.y, fMax.y) - 0.004f, local.y, 0.0002f, "İNCELE bayrağın kırmızı dilinin ortasında (üst kenarın 4 mm altı)");
            // Bayrak 6 mm yukarıda: ataş adın üstünde kalır, adı örtmez (R16a); iç içe modelde geçersiz kılma yok
            Assert.GreaterOrEqual(Mathf.Min(fMin.y, fMax.y), 0.0555f, "bayrağın alt ucu ad satırının üstünde");
            Assert.Less(local.z, Mathf.Min(fMin.z, fMax.z), "İNCELE bayrağın önünde");
            Assert.AreEqual(HorizontalAlignmentOptions.Center, flag.horizontalAlignment, "İNCELE ortalı");
            Assert.AreEqual(VerticalAlignmentOptions.Middle, flag.verticalAlignment, "İNCELE dikeyde ortalı");
            Assert.AreSame(ov.Flag, Field<PartDriver>(card, "flag"), "overlay ve kart aynı bayrağı sürer");

            // EN değer hücreleri (HP 3,5–24,5 · MOVE 24,5–47,5 · POWER 47,5–76,5) ve geri dönüş
            card.Portraits = null;
            card.English = true;
            card.Bind(Person("Cem", "Electronics", "Technician", true));
            for (int i = 0; i < PersonCardFace.Stats; i++)
            {
                TextAt(face, pf.StatLabels[i], StatCentresEn[i], 89f, 9.5f, HorizontalAlignmentOptions.Center, "EN değer etiketi " + i);
                Width(pf.StatLabels[i], StatWidthsEn[i], "EN değer etiketi " + i);
                TextAt(face, pf.StatValues[i], StatCentresEn[i], 100.6f, 11.5f, HorizontalAlignmentOptions.Center, "EN değer " + i);
            }
            for (int i = 0; i < 2; i++)
                CentreAt(face, pf.Dividers[i], new Vector2(DividersEn[i], 92f), new Vector2(0.3f, 20f), "EN bölme " + i);
            card.English = false;
            card.Bind(Person("Cem", "Elektronikçi", "Teknisyen", false));
            for (int i = 0; i < PersonCardFace.Stats; i++)
                TextAt(face, pf.StatLabels[i], StatCentresTr[i], 89f, 9.5f, HorizontalAlignmentOptions.Center, "TR'ye dönen değer etiketi " + i);
        }

        [Test]
        public void ItemMaterialChipFaces_LayoutMatchesPlan()
        {
            // eşya 80 × 50
            var item = Spawn(ItemPath);
            var itf = item.Face as ItemCardFace;
            Assert.IsNotNull(itf, "ItemCardFace");
            var face = (RectTransform)itf.transform;
            TextAt(face, itf.SlotLabel, 5f, 9f, 5.4f, HorizontalAlignmentOptions.Left, "yuva etiketi");
            Assert.AreEqual(0.4f / 5.4f * 100f, itf.SlotLabel.characterSpacing, 0.05f, "yuva etiketi aralığı 0,4 mm");
            TextAt(face, itf.UseLine, 5f, 17.6f, 5.4f, HorizontalAlignmentOptions.Left, "kullanım satırı");
            Width(itf.UseLine, 46f, "kullanım satırı");
            Assert.AreEqual(TextWrappingModes.Normal, itf.UseLine.textWrappingMode, "kullanım satırı sarar");
            RectAt(face, itf.Pictogram.rectTransform, new Vector2(53f, 3.4f), new Vector2(23f, 23f), "piktogram");
            RectAt(face, itf.BandIcon.rectTransform, new Vector2(2.5f, 36.7f), new Vector2(9.6f, 9.6f), "bant simgesi");
            TextAt(face, itf.ItemName, 14.6f, 44.8f, 9.4f, HorizontalAlignmentOptions.Left, "eşya adı");
            Width(itf.ItemName, 63.5f, "eşya adı");

            // kullanım satırının satır aralığı 6,4 mm
            item.Bind(new ItemCardData { Id = "esya-1", Kind = "armor", Name = "Zırh plakası", SlotLabel = "ZIRH", Slot = ItemSlotKind.Armor,
                UseLine = "ilk hasarı karşılar, kırılır ve bir sonraki baskında yeniden takılmalıdır" });
            itf.UseLine.ForceMeshUpdate(true, true);
            var lines = itf.UseLine.textInfo;
            Assert.GreaterOrEqual(lines.lineCount, 2, "satır aralığı ölçümü için iki satır");
            Assert.AreEqual(6.4f, lines.lineInfo[0].baseline - lines.lineInfo[1].baseline, 0.1f, "kullanım satırı aralığı (mm)");

            // hammadde 80 × 56
            var mat = Spawn(MaterialPath);
            var mf = mat.Face as MaterialCardFace;
            Assert.IsNotNull(mf, "MaterialCardFace");
            face = (RectTransform)mf.transform;
            RectAt(face, Field<Image>(mf, "icon").rectTransform, new Vector2(4f, 3.6f), new Vector2(10f, 10f), "hammadde simgesi");
            TextAt(face, mf.MaterialName, 16.4f, 11.2f, 9.4f, HorizontalAlignmentOptions.Left, "hammadde adı");
            Width(mf.MaterialName, 49.6f, "hammadde adı");
            TextAt(face, mf.FreeText, 75f, 27.6f, 22f, HorizontalAlignmentOptions.Right, "serbest sayısı");
            TextAt(face, mf.FreeWord, 75f, 33.2f, 5f, HorizontalAlignmentOptions.Right, "serbest sözcüğü");
            TextAt(face, mf.MoreText, 51.6f, 31.6f, 5f, HorizontalAlignmentOptions.Left, "+n");
            TextAt(face, mf.StockText, 5f, 47.4f, 7.4f, HorizontalAlignmentOptions.Left, "stok n");
            TextAt(face, mf.HeldText, 75f, 47.4f, 7.4f, HorizontalAlignmentOptions.Right, "ayrı n");
            Assert.AreEqual(MaterialCardFace.MaxPips, mf.Pips.Count, "20 pip");
            for (int i = 0; i < MaterialCardFace.MaxPips; i++)
                CentreAt(face, mf.Pips[i].rectTransform, new Vector2(6.6f + 4.5f * (i % 10), 20f + 5.6f * (i / 10)), new Vector2(3.4f, 3.4f), "stok pipi " + i);

            // fiş 56 × 40
            var chip = Spawn(ChipPath);
            var cf = chip.Face as ChipCardFace;
            Assert.IsNotNull(cf, "ChipCardFace");
            face = (RectTransform)cf.transform;
            RectAt(face, Field<Image>(cf, "icon").rectTransform, new Vector2(4f, 9f), new Vector2(14f, 14f), "fiş simgesi");
            TextAt(face, cf.OneText, 51f, 24f, 15f, HorizontalAlignmentOptions.Right, "fiş '1'");
            Assert.AreEqual("1", cf.OneText.text, "fiş '1' yazısı");
            TextAt(face, cf.ChipName, 28f, 35.4f, 9.4f, HorizontalAlignmentOptions.Center, "fiş adı");
            Width(cf.ChipName, 50f, "fiş adı");
            Assert.IsNull(Field<PartDriver>(chip, "magnet"), "fişte mıknatıs yok");
        }

        // ================================================================== E10 · yazı kuralları

        [Test]
        public void CardTexts_NoAutoSize_SharedMaterial_NoRaycast_NoLocText()
        {
            var materials = new HashSet<Material>();
            foreach (var spec in Specs)
            {
                var prefab = Load(spec.Path);
                string n = spec.Kind + ": ";
                var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                Assert.IsNotEmpty(texts, n + "yazı yok");
                foreach (var t in texts)
                {
                    string w = n + t.name + ": ";
                    Assert.IsFalse(t.enableAutoSizing, w + "otomatik punto");
                    Assert.IsNotNull(t.font, w + "font");
                    Assert.AreSame(t.font.material, t.fontSharedMaterial, w + "fontSharedMaterial == font.material");
                    Assert.IsFalse(t.raycastTarget, w + "raycastTarget");
                    Assert.AreEqual(TextOverflowModes.Overflow, t.overflowMode, w + "taşma kipi");
                    materials.Add(t.fontSharedMaterial);
                }
                foreach (var g in prefab.GetComponentsInChildren<Graphic>(true))
                    Assert.IsFalse(g.raycastTarget, n + g.name + ": raycastTarget");
                Assert.IsEmpty(prefab.GetComponentsInChildren<DeskLocText>(true), n + "kart yazılarında DeskLocText olmamalı");

                var face = prefab.GetComponent<BoardCardView>().Face;
                foreach (var (text, _) in face.SingleLineFields)
                    Assert.AreEqual(TextWrappingModes.NoWrap, text.textWrappingMode, n + text.name + ": tek satırlık alan sarmamalı");
            }
            Assert.LessOrEqual(materials.Count, 4, "kartlarda en çok 4 font malzemesi (eleştirmen 6): " + string.Join(", ", materials.Select(m => m.name)));
        }

        [Test]
        public void Portrait_QuadAndLibraryCrop()
        {
            var prefab = Load(PersonPath);
            var view = prefab.GetComponent<BoardCardView>();
            var r = view.PortraitRenderer;
            Assert.IsNotNull(r, "portre dörtgeni");
            Near(new Vector3(-0.013f, 0.0069f, -0.0009f), r.transform.localPosition, 0.00005f, "portre merkezi face (27; 53,1), z −0,0009");
            Near(new Vector3(0.044f, 0.0482f, 1f), r.transform.localScale, 0.00005f, "portre 44 × 48,2 mm");
            Assert.AreSame(prefab.transform, r.transform.parent, "portre kartın kökünde (modelin içinde değil)");
            Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode, "portre gölge vermez");
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(mesh != null, "portre ağı");
            Assert.AreEqual(2, mesh.triangles.Length / 3, "portre 2 üçgen");
            Assert.IsTrue(r.GetComponent<Collider>() == null, "portrede çarpıştırıcı yok");
            Assert.AreSame(r, ((PersonCardFace)view.Face).Overlay.Portrait, "KAYIP karartması aynı portreye");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Texture");
            Assert.IsNotNull(shader, "test gölgelendiricisi");
            var template = new Material(shader);
            var tex = new Texture2D(4, 4);
            var lib = ScriptableObject.CreateInstance<PortraitLibrary>();
            spawned.Add(lib); spawned.Add(template); spawned.Add(tex);
            lib.Configure(new[] { new PortraitLibrary.Entry { key = "cem", tex = tex } }, template, tex);
            var m = lib.For("Cem");
            Assert.IsNotNull(m, "Cem'in portre malzemesi");
            bool baseMap = m.HasProperty("_BaseMap");
            var scale = baseMap ? m.GetTextureScale("_BaseMap") : m.mainTextureScale;
            var offset = baseMap ? m.GetTextureOffset("_BaseMap") : m.mainTextureOffset;
            Near(new Vector2(0.7600f, 0.7353f), scale, 1e-3f, "portre kırpma ölçeği");
            Near(new Vector2(0.1200f, 0.1882f), offset, 1e-3f, "portre kırpma kayması");
            lib.ClearCache();
        }

        [Test]
        public void Bind_SameData_DoesNotRewrite()
        {
            var card = Spawn(PersonPath);
            card.Portraits = null;
            card.English = false;
            var d = Person("Cem", "Elektronikçi", "Teknisyen", false);
            card.Bind(d);
            int n = CardFace.ChangedBinds;
            card.Bind(d);
            Assert.AreEqual(n, CardFace.ChangedBinds, "aynı veri yeniden yazılmamalı");
            d.Strip = "Boşta";
            card.Bind(d);
            Assert.AreEqual(n + 1, CardFace.ChangedBinds, "değişen veri bir kez yazılmalı");
        }

        // ================================================================== eleştirmen 5 · ustalık harfleri

        [TestCase(false)]
        [TestCase(true)]
        public void Mastery_GlyphsDoNotOverlap(bool english)
        {
            var card = Spawn(PersonPath);
            card.Portraits = null;
            card.English = english;
            card.Bind(Person("Cem", english ? "Electronics" : "Elektronikçi", english ? "Technician" : "Teknisyen", english));
            var pf = (PersonCardFace)card.Face;
            var face = (RectTransform)pf.transform;
            var problems = new List<string>();
            float prevBottom = float.NegativeInfinity;
            string prev = null;
            for (int i = 0; i < PersonCardFace.Rows; i++)
            {
                var t = pf.Letters[i];
                Assert.IsNotEmpty(t.text, "ustalık harfi " + i);
                if (!GlyphExtent(t, face, problems, out float top, out float bottom)) continue;
                if (top <= prevBottom)
                    problems.Add("'" + prev + "' (alt " + prevBottom.ToString("0.00") + ") ile '" + t.text + "' (üst " + top.ToString("0.00") + ") değiyor");
                prevBottom = bottom;
                prev = t.text;
            }
            if (prevBottom >= 80f) problems.Add("son ustalık harfi değer çizgisine (y 80) değiyor: " + prevBottom.ToString("0.00"));
            Assert.IsEmpty(problems, (english ? "EN" : "TR") + "\n" + string.Join("\n", problems));
        }

        /// <summary>Tasarımdan gelen dokunmaları yalnız raporlar (eleştirmen 5: alanlar sessizce kaydırılmaz).</summary>
        [Test]
        public void Name_DesignTouches_AreReported()
        {
            var card = Spawn(PersonPath);
            card.Portraits = null;
            card.English = false;
            var pf = (PersonCardFace)card.Face;
            var face = (RectTransform)pf.transform;
            var notes = new List<string>();
            foreach (var name in new[] { "İpek", "Gülşen", "Yağmur", "Şükrü", "Suna 12" })
            {
                card.Bind(Person(name, "Elektronikçi", "Teknisyen", false));
                if (!GlyphExtent(pf.NameText, face, notes, out float top, out float bottom)) continue;
                if (top < 2.5f) notes.Add(name + ": üst " + top.ToString("0.00") + " mm iç çerçeveyi (2,5) geçiyor");
                if (bottom > 17.2f - 0.3f) notes.Add(name + ": alt " + bottom.ToString("0.00") + " mm kırmızı çizgiye (17,2) değiyor");
            }
            TestContext.WriteLine(notes.Count == 0 ? "tasarım dokunması yok" : string.Join("\n", notes));
            Assert.Pass(notes.Count + " tasarım dokunması raporlandı");
        }

        // ================================================================== taşma · TR ve EN

        [Test]
        public void Overflow_TrEn()
        {
            var failures = new List<string>();
            foreach (bool english in new[] { false, true })
            {
                string lang = english ? "EN" : "TR";
                var words = Words(english);

                // kişi
                var person = Spawn(PersonPath);
                person.Portraits = null;
                person.English = english;
                person.Words = words;
                var pf = (PersonCardFace)person.Face;
                var names = new[] { "Suna 12", "Eren 14", "Levent 30", "Gülizar", "Mehmet" };
                var jobs = Professions(english);
                var strips = Strips(english);
                for (int k = 0; k < strips.Count; k++)
                {
                    var d = Person(names[k % names.Length], jobs[k % jobs.Length], strips[k], english);
                    switch (k % 5)
                    {
                        case 0: d.Injured = true; d.Hurt = true; break;
                        case 1: d.Bedridden = true; d.StripTone = StripTone.Blue; break;
                        case 2: d.Restricted = true; d.StripTone = StripTone.Red; break;
                        case 3: d.Review = true; break;
                        default: d.Lost = true; break;
                    }
                    person.Bind(d);
                    CheckSingleLines(pf, lang + " kişi", failures);
                    var ov = pf.Overlay;
                    if (d.Restricted) CheckFit(ov.RestrictedWord, 20f, lang + " KISITLI köşesi", failures);
                    if (d.Lost) CheckFit(ov.LostWord, 22f, lang + " KAYIP köşesi", failures);
                    if (d.Review) CheckFit(ov.FlagWord, 0.0164f, lang + " İNCELE bayrağı (m)", failures);
                }
                foreach (var job in jobs)
                {
                    person.Bind(Person("Cem", job, strips[0], english));
                    CheckSingleLines(pf, lang + " meslek", failures);
                }

                // eşya
                var item = Spawn(ItemPath);
                item.Words = words;
                var itf = (ItemCardFace)item.Face;
                var itemNames = ItemNames(english);
                var useLines = UseLines(english);
                for (int k = 0; k < BoardLayout.KitKinds.Length; k++)
                {
                    var slot = k < 3 ? ItemSlotKind.Kit : k == 3 ? ItemSlotKind.Armor : ItemSlotKind.Aid;
                    string label = slot == ItemSlotKind.Kit ? words.Kit : slot == ItemSlotKind.Armor ? words.Armor : words.Aid;
                    foreach (var use in useLines)
                    {
                        item.Bind(new ItemCardData { Id = "esya-" + k, Kind = BoardLayout.KitKinds[k], Name = itemNames[k], SlotLabel = label, UseLine = use, Slot = slot });
                        CheckSingleLines(itf, lang + " eşya", failures);
                        itf.UseLine.ForceMeshUpdate(true, true);
                        if (itf.UseLine.textInfo.lineCount > itf.MaxUseLines)
                            failures.Add(lang + " eşya kullanım satırı " + itf.UseLine.textInfo.lineCount + " satır > " + itf.MaxUseLines + ": '" + use + "'");
                    }
                }

                // hammadde ve fiş
                var mat = Spawn(MaterialPath);
                mat.Words = words;
                var mf = (MaterialCardFace)mat.Face;
                var chip = Spawn(ChipPath);
                var cf = (ChipCardFace)chip.Face;
                var matNames = MaterialNames(english);
                for (int k = 0; k < BoardLayout.MaterialKinds.Length; k++)
                {
                    string kind = BoardLayout.MaterialKinds[k];
                    mat.Bind(new MaterialCardData
                    {
                        Kind = kind, Name = matNames[k], Stock = 64, Free = 60, Held = 60,
                        StockLine = Fmt(english, "stok {0}", "stock {0}", 64), HeldLine = Fmt(english, "ayrı {0}", "held {0}", 60),
                    });
                    CheckSingleLines(mf, lang + " hammadde", failures);
                    CheckFit(mf.FreeWord, 30f, lang + " serbest sözcüğü", failures);
                    chip.Bind(new ChipData { Id = "chip:" + kind + ":0", Kind = kind, Name = matNames[k], Index = 0 });
                    CheckSingleLines(cf, lang + " fiş", failures);
                }
            }
            Assert.IsEmpty(failures, "taşan ya da sarılan alanlar:\n" + string.Join("\n", failures));
        }

        static void CheckSingleLines(CardFace face, string what, List<string> failures)
        {
            foreach (var (t, max) in face.SingleLineFields)
            {
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                t.ForceMeshUpdate(true, true);
                float width = t.preferredWidth;
                int lines = t.textInfo.lineCount;
                bool fitsRect = max <= t.rectTransform.rect.width + 0.01f;
                if (lines != 1 || width > max + 0.01f || (fitsRect && t.isTextOverflowing))
                    failures.Add(what + "/" + t.name + ": '" + t.text + "' " + width.ToString("0.0") + " > " + max.ToString("0.0") + " mm, satır " + lines
                        + (t.isTextOverflowing ? ", taşıyor" : ""));
            }
        }

        static void CheckFit(TMP_Text t, float max, string what, List<string> failures)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return;
            t.ForceMeshUpdate(true, true);
            // preferredWidth 0,01 birime yukarı yuvarlanır: mm tuvalinde önemsiz, metre ölçekli 3B sözcükte 10 mm. Orada çizilen sınır.
            float width = max < 1f ? t.textBounds.size.x : t.preferredWidth;
            if (t.textInfo.lineCount > 1 || width > max * 1.001f)
                failures.Add(what + ": '" + t.text + "' " + width.ToString("0.0000") + " > " + max.ToString("0.0000"));
        }

        // ================================================================== Aşama 5 · tarif kartı (PLAN §1.6, W2)

        [Test]
        public void Prefab_RecipeCard_RootColliderModelFace()
        {
            var prefab = Load(RecipePath);
            var view = prefab.GetComponent<BoardCardView>();
            Assert.AreEqual(BoardCardKind.Recipe, view.Kind, "tür");
            Assert.IsInstanceOf<RecipeCardFace>(view.Face, "yüz RecipeCardFace");
            Assert.AreEqual(BoardCardKind.Recipe, view.Face.Kind);
            var so = new SerializedObject(view);
            Assert.AreEqual(1, so.FindProperty("faceMaterialSlot").intValue, "yüz malzemesi yuvası 1 (cardFace)");
            var faceMat = Field<Material>(view, "faceMaterial");
            Assert.IsNotNull(faceMat, "yüz malzemesi");
            StringAssert.EndsWith("/CardFace Recipe.mat", AssetDatabase.GetAssetPath(faceMat), "CardFace Recipe.mat");
            var magnet = Field<PartDriver>(view, "magnet");
            Assert.IsNotNull(magnet, "mıknatıs bağlı");
            Assert.AreSame(view.Model.Part("Magnet"), magnet, "mıknatıs = iç içe modelin Magnet parçası");
            Assert.IsNull(Field<PartDriver>(view, "flag"), "tarif kartında bayrak yok");
            Assert.IsNotNull(view.Body, "model gövdesi");

            var rf = (RecipeCardFace)view.Face;
            Assert.AreEqual(6, rf.Icons.Count, "altı çıktı simgesi");
            foreach (var sp in rf.Icons) Assert.IsNotNull(sp, "çıktı simgesi (eşya kartıyla aynı set)");
            Assert.AreEqual(4, rf.MaterialIcons.Count, "dört hammadde simgesi");
            foreach (var sp in rf.MaterialIcons) Assert.IsNotNull(sp, "hammadde simgesi (fiş kartıyla aynı set)");
            Assert.AreEqual(RecipeCardFace.Columns, rf.InputIcons.Count);
            Assert.AreEqual(RecipeCardFace.MaxBoxes, rf.Boxes.Count);
            Assert.AreEqual(RecipeCardFace.MaxBoxes, rf.BoxFills.Count);
            for (int i = 0; i < RecipeCardFace.MaxBoxes; i++)
            {
                Assert.IsNotNull(rf.Boxes[i], "kutu " + i);
                Assert.IsNotNull(rf.BoxFills[i], "kutu dolgusu " + i);
                Assert.IsFalse(rf.Boxes[i].gameObject.activeSelf, "kutu " + i + " başta gizli");
                Assert.IsTrue(rf.BoxFills[i].transform.IsChildOf(rf.Boxes[i].transform), "dolgu kutunun çocuğu");
            }
            Assert.IsEmpty(prefab.GetComponentsInChildren<DeskLocText>(true), "tarif kartında DeskLocText yok");
        }

        [Test]
        public void RecipeCardFace_Layout_Mm()
        {
            var card = Spawn(RecipePath);
            var rf = card.Face as RecipeCardFace;
            Assert.IsNotNull(rf, "RecipeCardFace");
            var face = (RectTransform)rf.transform;

            TextAt(face, rf.NameText, 6f, 15f, 12.1f, HorizontalAlignmentOptions.Left, "tarif adı");
            Width(rf.NameText, 81f, "tarif adı");
            RectAt(face, rf.Icon.rectTransform, new Vector2(88f, 4f), new Vector2(17f, 17f), "çıktı simgesi");
            TextAt(face, rf.NeedsLabel, 6f, 28f, 5.4f, HorizontalAlignmentOptions.Left, "GİRDİ");
            Assert.AreEqual(0.5f / 5.4f * 100f, rf.NeedsLabel.characterSpacing, 0.05f, "GİRDİ aralığı 0,5 mm");
            for (int i = 0; i < RecipeCardFace.Columns; i++)
            {
                float dx = 50f * i;
                RectAt(face, rf.InputIcons[i].rectTransform, new Vector2(6f + dx, 31f), new Vector2(12f, 12f), "girdi simgesi " + i);
                TextAt(face, rf.InputQty[i], 20f + dx, 42f, 9.5f, HorizontalAlignmentOptions.Left, "girdi sayısı " + i);
                Width(rf.InputQty[i], 28f, "girdi sayısı " + i);
                TextAt(face, rf.InputNames[i], 6f + dx, 54f, 9.4f, HorizontalAlignmentOptions.Left, "girdi adı " + i);
                Width(rf.InputNames[i], 48f, "girdi adı " + i);
            }
            for (int i = 0; i < RecipeCardFace.MaxBoxes; i++)
            {
                RectAt(face, rf.Boxes[i].rectTransform, new Vector2(6f + 8.2f * i, 60f), new Vector2(6.4f, 6.4f), "ilerleme kutusu " + i);
                CentreAt(face, rf.BoxFills[i].rectTransform, new Vector2(6f + 8.2f * i + 3.2f, 63.2f), new Vector2(4f, 4f), "kutu dolgusu " + i);
            }
            TextAt(face, rf.TurnsText, 6f, 74f, 9.4f, HorizontalAlignmentOptions.Left, "süre");
            Width(rf.TurnsText, 52f, "süre");
            TextAt(face, rf.OutText, 107f, 74f, 9.4f, HorizontalAlignmentOptions.Right, "→ 1 ürün");
            Width(rf.OutText, 48f, "→ 1 ürün");

            StringAssert.StartsWith("LF-Bold", rf.NameText.font.name, "ad LF 700");
            foreach (var t in rf.InputNames) StringAssert.StartsWith("LF-Regular", t.font.name, "girdi adı LF 400");
            foreach (var t in new[] { rf.NeedsLabel, rf.TurnsText, rf.OutText, rf.InputQty[0], rf.InputQty[1] })
                StringAssert.StartsWith("CP-Bold", t.font.name, t.name + " CP 700");
            // punto kuralı (D6-A): ad ≥ 12 mm, değerler ≥ 9,4 mm; "GİRDİ" gerekçeli küçük yazı
            Assert.GreaterOrEqual(rf.NameText.fontSize, 12f - Tol);
            foreach (var t in rf.ValueTexts) Assert.GreaterOrEqual(t.fontSize, 9.4f - Tol, t.name + " ≥ 9,4 mm");
            CollectionAssert.AreEqual(new[] { rf.NeedsLabel }, rf.SmallTexts.Select(x => x.text).ToArray(), "tek gerekçeli küçük yazı GİRDİ");
        }

        [Test]
        public void RecipeCard_ProgressBoxes_OnlyMultiShift()
        {
            var card = Spawn(RecipePath);
            var rf = (RecipeCardFace)card.Face;
            void Check(int turns, int done, int visible, int filled)
            {
                card.Bind(Recipe("emp_cell", "EMP hücresi", "metal", "Metal", "devre", "Devre", turns, done, false));
                string n = "Turns " + turns + ", Done " + done + ": ";
                Assert.AreEqual(visible, rf.VisibleBoxes, n + "görünen kutu");
                Assert.AreEqual(filled, rf.FilledBoxes, n + "dolu kutu");
            }
            Check(1, -1, 0, 0);   // tek vardiyalık, defterden kopya
            Check(1, 0, 0, 0);    // tek vardiyalık emir: kutu yok (D7-A)
            Check(2, -1, 0, 0);   // kopya (emir yok): kutu yok
            Check(2, 0, 2, 0);    // başlamamış iki vardiyalık emir
            Check(2, 1, 2, 1);    // 1/2
            Check(2, 2, 2, 2);
            Check(3, 1, 2, 1);    // en çok iki kutu çizilir
            Check(1, -1, 0, 0);   // geri döner
        }

        [Test]
        public void RecipeCard_Bind_SameData_DoesNotRewrite()
        {
            var card = Spawn(RecipePath);
            var d = Recipe("armor", "Zırh plakası", "metal", "Metal", "kumas", "Kumaş", 1, -1, false);
            card.Bind(d);
            Assert.AreEqual("armor", card.Id);
            Assert.AreEqual("Zırh plakası", card.HoverTitle, "üzerine gelme başlığı tarifin adı");
            int n = CardFace.ChangedBinds;
            card.Bind(Recipe("armor", "Zırh plakası", "metal", "Metal", "kumas", "Kumaş", 1, -1, false));
            Assert.AreEqual(n, CardFace.ChangedBinds, "aynı veri yeniden yazılmamalı");
            card.Bind(Recipe("armor", "Zırh plakası", "metal", "Metal", "kumas", "Kumaş", 2, 1, false));
            Assert.AreEqual(n + 1, CardFace.ChangedBinds, "değişen veri bir kez yazılmalı");
            var rf = (RecipeCardFace)card.Face;
            Assert.AreEqual("Zırh plakası", rf.NameText.text);
            Assert.AreEqual("×2", rf.InputQty[0].text);
            Assert.AreEqual("Kumaş", rf.InputNames[1].text);
            Assert.IsNotNull(rf.Icon.sprite, "çıktı simgesi (armor)");
            Assert.AreSame(rf.Icons[3], rf.Icon.sprite, "armor = KitKinds[3]");
            Assert.AreSame(rf.MaterialIcons[2], rf.InputIcons[1].sprite, "kumaş = MaterialKinds[2]");
        }

        /// <summary>
        /// E11: altı tarif × TR/EN, tek ve iki vardiya, bütün ilerleme durumları. Tek satırlık alanlar sığar; süre ile "→ 1 ürün"
        /// arasında ≥ 2 mm; kutular ayraçla ve süre satırıyla çakışmaz; ad kırmızı çizgiyi geçmez; girdi adları ayraca değmez.
        /// </summary>
        [Test]
        public void RecipeCard_Overflow_TrEn()
        {
            var failures = new List<string>();
            var notes = new List<string>();
            var card = Spawn(RecipePath);
            var rf = (RecipeCardFace)card.Face;
            var face = (RectTransform)rf.transform;
            foreach (bool english in new[] { false, true })
            {
                string lang = english ? "EN" : "TR";
                foreach (var r in Recipes(english))
                    foreach (int turns in new[] { 1, 2 })
                        foreach (int done in new[] { -1, 0, 1, 2 })
                        {
                            if (done > turns) continue;
                            card.Bind(Recipe(r[0], r[1], r[2], r[3], r[4], r[5], turns, done, english));
                            string what = lang + " " + r[0] + " " + turns + "/" + done;
                            CheckSingleLines(rf, what, failures);

                            float turnsRight = 6f + rf.TurnsText.preferredWidth, outLeft = 107f - rf.OutText.preferredWidth;
                            // TMP preferredWidth 0,01 mm'ye yuvarlanır; tam 2,0 mm boşluk kabul (tasarımda "2 vardiya" ile "→ 1 ürün" arası)
                            if (outLeft - turnsRight < 2f - 0.05f)
                                failures.Add(what + ": süre (sağ " + turnsRight.ToString("0.0") + ") ile çıktı (sol " + outLeft.ToString("0.0") + ") arası < 2 mm");

                            if (rf.VisibleBoxes > 0 && GlyphExtent(rf.TurnsText, face, notes, out float tTop, out _) && tTop < 60f + 6.4f + 0.3f)
                                failures.Add(what + ": kutular (alt 66,4) süre yazısına (üst " + tTop.ToString("0.00") + ") değiyor");

                            if (GlyphExtent(rf.NameText, face, notes, out _, out float nBottom))
                            {
                                if (nBottom > 18.6f) failures.Add(what + ": ad (alt " + nBottom.ToString("0.00") + ") kırmızı çizgiyi (18,6) geçiyor");
                                else if (nBottom > 18.35f) notes.Add(what + ": ad kırmızı çizgiye değiyor (alt " + nBottom.ToString("0.00") + ")");
                            }
                            foreach (var t in rf.InputNames)
                                if (t.gameObject.activeSelf && GlyphExtent(t, face, notes, out _, out float iBottom) && iBottom > 58f - 0.15f)
                                    failures.Add(what + ": girdi adı '" + t.text + "' (alt " + iBottom.ToString("0.00") + ") ayraca (58) değiyor");
                        }
            }
            if (notes.Count > 0) TestContext.WriteLine(string.Join("\n", notes.Distinct()));
            Assert.IsEmpty(failures, "taşan ya da çakışan alanlar:\n" + string.Join("\n", failures));
        }

        /// <summary>Core'un tarifleri (sırası, adları ve girdi türleri; <c>Simulation.BoardRecipes</c>): kimlik, ad, iki girdi (tür, ad).</summary>
        static IEnumerable<string[]> Recipes(bool english)
        {
            string metal = Tr(english, "Metal", "Metal"), devre = Tr(english, "Devre", "Circuit");
            string kumas = Tr(english, "Kumaş", "Cloth"), kimyasal = Tr(english, "Kimyasal", "Chemical");
            yield return new[] { "armor", Tr(english, "Zırh plakası", "Armour plate"), "metal", metal, "kumas", kumas };
            yield return new[] { "medkit", Tr(english, "Bakım kiti", "Care kit"), "kumas", kumas, "kimyasal", kimyasal };
            yield return new[] { "emp_cell", Tr(english, "EMP hücresi", "EMP cell"), "metal", metal, "devre", devre };
            yield return new[] { "kit_tek", Tr(english, "Teknisyen kiti", "Technician kit"), "metal", metal, "devre", devre };
            yield return new[] { "kit_emp", Tr(english, "EMP kiti", "EMP kit"), "metal", metal, "devre", devre };
            yield return new[] { "kit_ana", Tr(english, "Analist kiti", "Analyst kit"), "metal", metal, "devre", devre };
        }

        /// <summary>Kurucunun (W4) verdiği biçimde tarif kartı verisi (EN çoğulu "2 shifts", "→ 1 item", "INPUT").</summary>
        static RecipeCardData Recipe(string id, string name, string k0, string n0, string k1, string n1, int turns, int done, bool english)
        {
            var d = new RecipeCardData
            {
                Id = id, RecipeId = id, Name = name, IconKey = id, Turns = turns, Done = done,
                TurnsLine = turns == 1 ? Tr(english, "1 vardiya", "1 shift") : Fmt(english, "{0} vardiya", "{0} shifts", turns),
                OutLine = Tr(english, "→ 1 ürün", "→ 1 item"), NeedsLabel = Tr(english, "GİRDİ", "INPUT"),
            };
            d.Inputs.Add(new RecipeInput { Kind = k0, Name = n0, Quantity = 2, BookQty = "× 2", CardQty = "×2" });
            d.Inputs.Add(new RecipeInput { Kind = k1, Name = n1, Quantity = 1, BookQty = "× 1", CardQty = "×1" });
            return d;
        }

        // ================================================================== örnek veri

        static PersonCardData Person(string name, string job, string strip, bool english)
        {
            var d = new PersonCardData { Id = "sakin-1", Name = name, Job = job, Strip = strip, StripTone = StripTone.Ink, PortraitKey = name };
            var letters = (english ? "T E A P K C F" : "T E A Ü M B İ").Split(' ');
            for (int i = 0; i < 7; i++) { d.MasteryLetters[i] = letters[i]; d.MasteryTiers[i] = i % 4; }
            d.StatLabels[0] = Tr(english, "CAN", "HP");
            d.StatLabels[1] = Tr(english, "HAR", "MOVE");
            d.StatLabels[2] = Tr(english, "GÜÇ", "POWER");
            d.StatValues[0] = "3/4"; d.StatValues[1] = "5"; d.StatValues[2] = "2";
            return d;
        }

        static BoardWords Words(bool english) => new BoardWords
        {
            Injured = Tr(english, "YARALI", "INJURED"), Bedridden = Tr(english, "YATIYOR", "LAID UP"),
            Restricted = Tr(english, "KISITLI", "RESTRICTED"), Review = Tr(english, "İNCELE", "REVIEW"), Lost = Tr(english, "KAYIP", "LOST"),
            Free = Tr(english, "serbest", "free"), Kit = Tr(english, "KİT", "KIT"), Armor = Tr(english, "ZIRH", "ARMOUR"), Aid = Tr(english, "YARDIMCI", "AID"),
        };

        /// <summary>Core'un meslekleri; EN tablodan (yoksa D2'nin kısa karşılığı). D2 uygulanmadan EN uzun meslekler taşar.</summary>
        static string[] Professions(bool english) => new[]
        {
            Tr(english, "Elektronikçi", "Electronics"), Tr(english, "Elektrikçi", "Electrician"), Tr(english, "Haberleşmeci", "Radio operator"),
            Tr(english, "Tamirci", "Mechanic"), Tr(english, "Aşçı", "Cook"), Tr(english, "Sağlık çalışanı", "Medic"), Tr(english, "Depo görevlisi", "Storekeeper"),
        };

        /// <summary>Şeridin bütün biçimleri (PLAN §4.3), en uzun sayılarla.</summary>
        static List<string> Strips(bool english)
        {
            var list = new List<string>
            {
                Fmt(english, "Kayıp · V.{0:000}", "Lost · S.{0:000}", 131),
                Fmt(english, "Yatıyor · {0} v.", "Laid up · {0} sh.", 12),
                Fmt(english, "Kısıtlı · {0} v.", "Restricted {0} sh.", 12),
                Tr(english, "Kısıtlı · bitti", "Restricted · over"),
                Fmt(english, "Boşta · +{0} can", "Idle · +{0} HP", 2),
                Tr(english, "Boşta", "Idle"),
            };
            var topics = new[]
            {
                Tr(english, "Teknisyen", "Technician"), Tr(english, "EMP", "EMP"), Tr(english, "Analiz", "Analysis"), Tr(english, "Üretim", "Production"),
                Tr(english, "Mutfak", "Kitchen"), Tr(english, "Bakım", "Care"), Tr(english, "İkmal", "Foraging"),
            };
            foreach (var t in topics) list.Add(Fmt(english, "{0} +{1}", "{0} +{1}", t, 12));
            list.Add(Tr(english, "EMP uzmanı", "EMP specialist"));
            list.Add(Tr(english, "Analist", "Analyst"));
            list.AddRange(topics);
            return list;
        }

        static string[] ItemNames(bool english) => new[]
        {
            Tr(english, "Teknisyen kiti", "Technician kit"), Tr(english, "EMP kiti", "EMP kit"), Tr(english, "Analist kiti", "Analyst kit"),
            Tr(english, "Zırh plakası", "Armour plate"), Tr(english, "Bakım kiti", "Care kit"), Tr(english, "EMP hücresi", "EMP cell"),
        };

        static string[] UseLines(bool english) => new[]
        {
            Fmt(english, "saha rolü: {0}", "field role: {0}", Tr(english, "EMP uzmanı", "EMP specialist")),
            Tr(english, "ilk hasarı karşılar, kırılır", "absorbs the first hit, then breaks"),
            Fmt(english, "EMP uzmanı: ilk atış +{0} hasar; baskında tükenir", "EMP specialist: first shot +{0} damage; one raid", 1),
            Fmt(english, "EMP uzmanı: ilk {0} atış +{1} hasar; baskında tükenir", "EMP specialist: first {0} shots +{1} damage; one raid", 2, 1),
            Fmt(english, "bakım görevinde +{0} can; panoda bakım cebi yok", "care duty: +{0} HP; the board has no care pocket", 1),
        };

        static string[] MaterialNames(bool english) => new[]
        {
            Tr(english, "METAL", "METAL"), Tr(english, "DEVRE", "CIRCUIT"), Tr(english, "KUMAŞ", "CLOTH"), Tr(english, "KİMYASAL", "CHEMICAL"),
        };

        static Dictionary<string, string> enTable;
        static bool enLoaded;

        /// <summary>Türkçe kaynak; EN'de çeviri tablosundaki karşılık, yoksa <paramref name="enFallback"/> (henüz birleşmemiş yazı).</summary>
        static string Tr(bool english, string tr, string enFallback)
        {
            if (!english) return tr;
            if (!enLoaded)
            {
                enLoaded = true;
                if (File.Exists(TablePath))
                {
                    var table = Loc.ParseTable(File.ReadAllText(TablePath));
                    if (table.TryGetValue(Loc.English, out var en)) enTable = en;
                }
            }
            return enTable != null && enTable.TryGetValue(tr, out var v) && !string.IsNullOrEmpty(v) ? v : enFallback;
        }

        static string Fmt(bool english, string tr, string enFallback, params object[] args) =>
            string.Format(english ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo("tr-TR"), Tr(english, tr, enFallback), args);

        // ================================================================== yardımcılar

        static GameObject Load(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null) Assert.Ignore("Kart prefab'ı yok (Corrigible/Revizyon 3/Aşama 3/Kart prefab'larını kur): " + path);
            return p;
        }

        BoardCardView Spawn(string path)
        {
            var go = Object.Instantiate(Load(path));
            spawned.Add(go);
            var view = go.GetComponent<BoardCardView>();
            Assert.IsTrue(view != null, path + ": BoardCardView");
            return view;
        }

        static T Field<T>(Object owner, string field) where T : Object
        {
            var p = new SerializedObject(owner).FindProperty(field);
            Assert.IsNotNull(p, owner.GetType().Name + "." + field + " alanı");
            return p.objectReferenceValue as T;
        }

        /// <summary>Dünya noktası → face mm (sol üstten, y aşağı).</summary>
        static Vector2 FaceMm(RectTransform face, Vector3 world)
        {
            var l = face.InverseTransformPoint(world);
            return new Vector2(l.x, -l.y);
        }

        /// <summary>Yazının taban çizgisi noktası (Baseline hizasında dikdörtgenin dikey ortası; yatayda hizanın ucu), face mm.</summary>
        static Vector2 BaselineMm(RectTransform face, TMP_Text t)
        {
            var c = new Vector3[4];
            t.rectTransform.GetWorldCorners(c);
            Vector3 left = (c[0] + c[1]) * 0.5f, right = (c[2] + c[3]) * 0.5f;
            var h = t.horizontalAlignment;
            var p = h == HorizontalAlignmentOptions.Right ? right : h == HorizontalAlignmentOptions.Center ? (left + right) * 0.5f : left;
            return FaceMm(face, p);
        }

        static void TextAt(RectTransform face, TMP_Text t, float x, float baseline, float em, HorizontalAlignmentOptions align, string what)
        {
            Assert.IsNotNull(t, what + " bağlı değil");
            Assert.AreEqual(VerticalAlignmentOptions.Baseline, t.verticalAlignment, what + ": taban çizgisi hizası");
            Assert.AreEqual(align, t.horizontalAlignment, what + ": yatay hiza");
            Near(new Vector2(x, baseline), BaselineMm(face, t), Tol, what + " taban çizgisi (face mm)");
            Assert.AreEqual(em, t.fontSize, Tol, what + " em (mm)");
        }

        static void Width(TMP_Text t, float mm, string what) => Assert.AreEqual(mm, t.rectTransform.rect.width, Tol, what + " genişliği (mm)");

        static void RectAt(RectTransform face, RectTransform rt, Vector2 topLeftMm, Vector2 sizeMm, string what)
        {
            Assert.IsNotNull(rt, what);
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Near(topLeftMm, FaceMm(face, c[1]), Tol, what + " sol üst (face mm)");
            Near(sizeMm, rt.rect.size, Tol, what + " boyutu (mm)");
        }

        static void CentreAt(RectTransform face, RectTransform rt, Vector2 centreMm, Vector2 sizeMm, string what)
        {
            Assert.IsNotNull(rt, what);
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Near(centreMm, FaceMm(face, (c[0] + c[2]) * 0.5f), Tol, what + " merkezi (face mm)");
            Near(sizeMm, rt.rect.size, Tol, what + " boyutu (mm)");
        }

        static void PivotAt(RectTransform face, RectTransform rt, Vector2 mm, float rotZ, string what)
        {
            Near(mm, FaceMm(face, rt.position), Tol, what + " dönme merkezi (face mm)");
            Assert.AreEqual(0f, Mathf.DeltaAngle(rt.localEulerAngles.z, rotZ), 0.1f, what + " açısı");
        }

        /// <summary>Köşe sözcüğü: dönme merkezi tasarım merkezinde, −45° (tasarımda +45°), taban çizgisi merkezin 1,8 mm altında, 4,6 mm.</summary>
        static void CornerWordAt(RectTransform face, TMP_Text t, Vector2 centreMm, string what)
        {
            Assert.IsNotNull(t, what);
            PivotAt(face, t.rectTransform, centreMm, -45f, what);
            Assert.AreEqual(VerticalAlignmentOptions.Baseline, t.verticalAlignment, what + " taban hizası");
            var rt = t.rectTransform;
            Assert.AreEqual(-1.8f, (0.5f - rt.pivot.y) * rt.rect.height, Tol, what + " taban çizgisi merkezin 1,8 mm altında");
            Assert.AreEqual(4.6f, t.fontSize, Tol, what + " em");
        }

        /// <summary>Yazının gliflerinin dikey uçları (face mm, y aşağı), font ölçülerinden: üst = taban − bearingY, alt = üst + yükseklik.</summary>
        static bool GlyphExtent(TMP_Text t, RectTransform face, List<string> problems, out float top, out float bottom)
        {
            top = float.PositiveInfinity;
            bottom = float.NegativeInfinity;
            var font = t.font;
            if (font == null) { problems.Add(t.name + ": font yok"); return false; }
            float baseline = BaselineMm(face, t).y;
            float scale = t.fontSize / font.faceInfo.pointSize * font.faceInfo.scale;
            string text = System.Text.RegularExpressions.Regex.Replace(t.text ?? "", "<[^>]*>", "");
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch)) continue;
                if (!font.HasCharacter(ch, false, true) || !font.characterLookupTable.TryGetValue(ch, out var character) || character.glyph == null)
                {
                    problems.Add(t.name + ": fontta '" + ch + "' yok");
                    continue;
                }
                var m = character.glyph.metrics;
                float gTop = baseline - m.horizontalBearingY * scale;
                top = Mathf.Min(top, gTop);
                bottom = Mathf.Max(bottom, gTop + m.height * scale);
            }
            return !float.IsInfinity(top);
        }

        static void Near(Vector2 expected, Vector2 actual, float tol, string what) =>
            Assert.IsTrue(Mathf.Abs(expected.x - actual.x) <= tol && Mathf.Abs(expected.y - actual.y) <= tol,
                what + ": beklenen " + expected.ToString("F4") + ", olan " + actual.ToString("F4"));

        static void Near(Vector3 expected, Vector3 actual, float tol, string what) =>
            Assert.IsTrue(Mathf.Abs(expected.x - actual.x) <= tol && Mathf.Abs(expected.y - actual.y) <= tol && Mathf.Abs(expected.z - actual.z) <= tol,
                what + ": beklenen " + expected.ToString("F6") + ", olan " + actual.ToString("F6"));
    }
}
