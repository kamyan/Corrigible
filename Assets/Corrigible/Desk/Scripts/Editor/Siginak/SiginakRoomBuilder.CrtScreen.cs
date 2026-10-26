using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.Bridge;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 7: CRT (K5). Masadaki ekrana bekleme içeriği (<see cref="CrtScreen"/>, World Space 1024 × 768 = 0,30 × 0,225 m)
    /// ve sahnenin kökünde tam ekran baskın geçişi (<see cref="CrtTransition"/>, Overlay; oda kökü baskında gizlendiği için
    /// dışarıda). Izgara baskın kamerasının çerçevesinden: x = 210 + (u + v) · 93,75 ; y = 528 + (v − u) · 53,775
    /// (tokens_sade › raid.camera.screenFromGrid1080, 1080 yüksekliğe göre).
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        const string CrtOverlayName = "SiginakCrtOverlay";

        static TextMeshProUGUI Phosphor(Transform parent, string name, string role, bool dim, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            string text = "", bool localize = false) =>
            UiText(parent, name, text, role, T.ColorOf(dim ? "crt.phosphorDim" : "crt.phosphor"), align, localize);

        static CrtScreen BuildCrtScreen(Transform screen, Camera camera)
        {
            const float w = 1024, h = 768;
            var canvas = WorldCanvas(screen, "Display", new Vector3(0, 0, -0.0006f), new Vector2(w, h), 0.30f / w, camera);
            var root = (RectTransform)canvas.transform;
            var column = Stretch(root, "Text");
            Column(column.gameObject, 14, new RectOffset(96, 96, 96, 60));
            var channel = Phosphor(column, "Channel", "crtTexLine", true);
            channel.fontSize = 40;
            var title = Phosphor(column, "Title", "crtTexLine", false);
            title.fontSize = 72;
            var sub = Phosphor(column, "Sub", "crtTexLine", true);
            sub.fontSize = 40;
            var gap = UiRect(column, "Gap", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 30));
            Height(gap, 30);
            // Ekip satırı: yaralıyken ters video (fosfor zemin)
            var teamRow = UiRect(column, "Team", Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), Vector2.zero, new Vector2(500, 56));
            Height(teamRow, 56);
            var teamBack = UiImage(Stretch(teamRow, "Inverse", new Vector2(0, 0)), S("ui_fill_r0"), T.ColorOf("crt.phosphor"));
            var team = Place(Phosphor(teamRow, "Text", "crtTexLine", true), new Vector2(0, 0), new Vector2(820, 56));
            team.alignment = TextAlignmentOptions.MidlineLeft;
            var fitter = teamRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            fitter.padding = new RectOffset(8, 16, 0, 0);
            fitter.childControlWidth = fitter.childControlHeight = true;
            fitter.childForceExpandWidth = fitter.childForceExpandHeight = false;
            Fit(teamRow.gameObject, true, false);
            var front = Phosphor(column, "Front", "crtTexLine", true);
            var hint = Phosphor(column, "Hint", "crtTexLine", true);
            hint.fontSize = 38;
            hint.fontStyle = FontStyles.Normal;
            hint.textWrappingMode = TextWrappingModes.Normal;
            // İpucu sol yarıda kırılır (sağ altta test deseni; K5a)
            hint.margin = new Vector4(0, 0, 260, 0);
            var cursor = Phosphor(column, "Cursor", "crtTexLine", false, text: "▮");

            // Sağ altta %28 test deseni: iki daire, artı, K-07
            var pattern = UiRect(root, "Test pattern", new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-250, 220), new Vector2(300, 300));
            var dim = T.ColorOf("crt.phosphor");
            dim.a = 0.28f;
            foreach (var radius in new[] { 140f, 92f })
            {
                var circle = Stretch(pattern, "Circle " + radius).gameObject.AddComponent<UILineGraphic>();
                circle.color = dim; circle.Thickness = 2.5f; circle.raycastTarget = false;
                circle.SetPoints(Enumerable.Range(0, 65).Select(i => new Vector2(Mathf.Cos(i * Mathf.PI / 32) * radius, Mathf.Sin(i * Mathf.PI / 32) * radius)).ToList());
            }
            foreach (var (a, b) in new[] { (new Vector2(-150, 0), new Vector2(150, 0)), (new Vector2(0, -150), new Vector2(0, 150)) })
            {
                var l = Stretch(pattern, "Cross").gameObject.AddComponent<UILineGraphic>();
                l.color = dim; l.Thickness = 2.5f; l.raycastTarget = false;
                l.SetPoints(new List<Vector2> { a, b });
            }
            var tag = Phosphor(pattern, "Tag", "crtTexLine", true, TextAlignmentOptions.BottomRight, SiginakRoomWords.CrtPattern, true);
            tag.fontSize = 30;
            tag.color = dim;
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(1, 0);
            tag.rectTransform.pivot = new Vector2(1, 0);
            tag.rectTransform.anchoredPosition = new Vector2(40, -60);
            tag.rectTransform.sizeDelta = new Vector2(160, 40);

            // Katmanlar: statik (kuruluyor/yayında), tarama çizgisi, köşe kararması
            var noise = Stretch(root, "Static").gameObject.AddComponent<RawImage>();
            noise.texture = Tex("tx_crt_static");
            noise.uvRect = new Rect(0, 0, w / 256f, h / 256f);
            noise.color = new Color(1, 1, 1, 0.35f);
            noise.raycastTarget = false;
            noise.gameObject.SetActive(false);
            var scan = UiImage(Stretch(root, "Scanlines"), S("crt_scanlines"), Color.white, Image.Type.Tiled);
            scan.pixelsPerUnitMultiplier = 0.5f;
            UiImage(Stretch(root, "Vignette"), S("crt_vignette"), Color.white, Image.Type.Simple);
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            var view = canvas.gameObject.AddComponent<CrtScreen>();
            view.Configure(channel, title, sub, team, front, hint, cursor, teamBack, noise, pattern.gameObject);
            return view;
        }

        /// <summary>Tam ekran CRT geçişi (K5b): sahnenin kökünde, kapalı başlar.</summary>
        static CrtTransition BuildCrtOverlay(Scene scene)
        {
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == CrtOverlayName).ToArray()) Object.DestroyImmediate(old);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SiginakSpriteManifest.SpritesDir + "/ui_crt_glass_cover.png") == null)
            {
                SiginakTextureBaker.GlassCover();
                AssetDatabase.ImportAsset(SiginakSpriteManifest.SpritesDir + "/ui_crt_glass_cover.png", ImportAssetOptions.ForceUpdate);
            }
            var go = new GameObject(CrtOverlayName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.layer = 5;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = MapProjection.DesignSize;
            scaler.matchWidthOrHeight = 1;
            var root = (RectTransform)go.transform;

            UiImage(Stretch(root, "Frame"), S("ui_fill_r0"), T.ColorOf("crt.frame[0]"));
            // Tüp: cam dikdörtgeni (34, 22 kenar); açılışta y ölçeği 4 px çizgiden tam boya
            var tube = Stretch(root, "Tube");
            tube.offsetMin = new Vector2(34, 22); tube.offsetMax = new Vector2(-34, -22);
            // Cam dışına taşan katmanlar (yuvarlanan bant, statik) kesilir; açılışta çizgi de bu maskeyle görünür
            tube.gameObject.AddComponent<RectMask2D>();
            UiImage(Stretch(tube, "Glass"), S("ui_fill_r0"), T.ColorOf("crt.glass"));

            // 8 × 8 ızgara, ekran merkezine göre (tüp ekranla aynı merkezde)
            var gridRoot = UiRect(tube, "Grid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, MapProjection.DesignSize);
            Vector2 G(float u, float v) => new Vector2(210 + (u + v) * 93.75f - 960, 540 - (528 + (v - u) * 53.775f));
            var lines = new List<Graphic>();
            for (int i = 0; i <= 8; i++)
            {
                foreach (var (a, b) in new[] { (G(i, 0), G(i, 8)), (G(0, i), G(8, i)) })
                {
                    var l = Stretch(gridRoot, "Line").gameObject.AddComponent<UILineGraphic>();
                    l.Thickness = 2; l.raycastTarget = false;
                    l.SetPoints(new List<Vector2> { a, b });
                    lines.Add(l);
                }
            }
            var noise = Stretch(tube, "Static").gameObject.AddComponent<RawImage>();
            noise.texture = Tex("tx_crt_static");
            noise.uvRect = new Rect(0, 0, 1852 / 256f, 1036 / 256f);
            noise.color = new Color(1, 1, 1, 0.55f);
            noise.raycastTarget = false;
            var roll = UiRect(tube, "Roll bar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 96));
            UiImage(roll, S("crt_rollbar"), Color.white, Image.Type.Simple);

            var content = Stretch(tube, "Content");
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            // Ekran koordinatları (K5b): üst satır y 72 + yarım satır, orta sütun 330, alt satır tabandan 70
            var channel = Place(Phosphor(content, "Channel", "crtHead", false), new Vector2(62, 66), new Vector2(900, 36));
            var code = Phosphor(content, "Code", "crtHead", true, TextAlignmentOptions.TopRight);
            code.rectTransform.anchorMin = code.rectTransform.anchorMax = new Vector2(1, 1);
            code.rectTransform.pivot = new Vector2(1, 1);
            code.rectTransform.anchoredPosition = new Vector2(-62, -66);
            code.rectTransform.sizeDelta = new Vector2(700, 36);

            // Orta sütun (ekran y 330): başlık + hayalet, senkron, hedef, kamera
            var center = UiRect(content, "Center", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -328), new Vector2(0, 400));
            var col = Column(center.gameObject, 26);
            col.childAlignment = TextAnchor.UpperCenter;
            col.childForceExpandWidth = false;
            var title = Phosphor(center, "Title", "crtBig", false, TextAlignmentOptions.Center);
            var ghost = Phosphor(title.transform, "Ghost", "crtBig", false, TextAlignmentOptions.Center);
            var ghostColor = T.ColorOf("crt.ghost");
            ghost.color = ghostColor;
            ghost.rectTransform.anchorMin = Vector2.zero; ghost.rectTransform.anchorMax = Vector2.one;
            ghost.rectTransform.sizeDelta = Vector2.zero; ghost.rectTransform.anchoredPosition = new Vector2(7, 0);
            ghost.transform.SetAsFirstSibling();
            var syncRow = UiRect(center, "Sync", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 40));
            Row(syncRow.gameObject, 18, TextAnchor.MiddleCenter);
            Fit(syncRow.gameObject, true, true);
            var sync = Phosphor(syncRow, "Label", "crtLine", false);
            sync.fontSize = 30;
            sync.characterSpacing = 8;
            var boxRow = UiRect(syncRow, "Boxes", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 36));
            Row(boxRow.gameObject, 6, TextAnchor.MiddleCenter);
            Fit(boxRow.gameObject, true, true);
            var boxes = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                boxes[i] = LayoutImage(boxRow, "Box " + (i + 1), S("ui_fill_r0"), Color.clear, new Vector2(26, 36));
                UiImage(Stretch(boxes[i].rectTransform, "Edge"), S("ui_frame_2px_r3"), T.ColorOf("crt.phosphorDim"));
            }
            var percent = Phosphor(syncRow, "Percent", "crtLine", false);
            percent.fontSize = 30;
            var target = Phosphor(center, "Target", "crtLine", false, TextAlignmentOptions.Center);
            var cameraLine = Phosphor(center, "Camera", "crtSub", true, TextAlignmentOptions.Center);

            // Alt satır: “Boşluk atla” fişi ve hat adı
            var skipRow = UiRect(content, "Skip", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(62, 70), new Vector2(300, 34));
            Row(skipRow.gameObject, 10);
            var chip = UiRect(skipRow, "Key", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 32));
            UiImage(Stretch(chip, "Edge"), S("ui_frame_2px_r3"), T.ColorOf("crt.phosphorDim"));
            var chipLayout = Row(chip.gameObject, 0, TextAnchor.MiddleCenter);
            chipLayout.padding = new RectOffset(8, 8, 0, 0);
            var skipKey = Phosphor(chip, "Text", "crtSub", true);
            skipKey.fontSize = 22;
            var skipWord = Phosphor(skipRow, "Word", "crtSub", true);
            skipWord.fontSize = 22;
            var footer = Phosphor(content, "Footer", "crtSub", true, TextAlignmentOptions.BottomRight);
            footer.fontSize = 22;
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(1, 0);
            footer.rectTransform.pivot = new Vector2(1, 0);
            footer.rectTransform.anchoredPosition = new Vector2(-62, 74);
            footer.rectTransform.sizeDelta = new Vector2(700, 30);

            UiImage(Stretch(tube, "Scanlines"), S("crt_scanlines"), Color.white, Image.Type.Tiled);
            UiImage(Stretch(tube, "Vignette"), S("crt_vignette"), Color.white, Image.Type.Simple);
            // Köşe örtüsü: camın yuvarlak köşeleri (74/64 ≈ 72), çerçeve rengiyle
            var cover = Stretch(root, "Glass corners");
            cover.offsetMin = new Vector2(34, 22); cover.offsetMax = new Vector2(-34, -22);
            UiImage(cover, S("ui_crt_glass_cover"), T.ColorOf("crt.frame[0]"));
            foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            var view = go.AddComponent<CrtTransition>();
            view.Configure(go.GetComponent<CanvasGroup>(), tube, contentGroup, channel, code, title, ghost, sync, percent, target, cameraLine, skipKey,
                skipWord, footer, boxes, lines.ToArray(), noise, roll);
            go.SetActive(false);
            return view;
        }

        static SiginakCrt BuildCrt(Scene scene, Refs refs, Camera camera, Corrigible.Game.GameController game)
        {
            var screen = BuildCrtScreen(refs.CrtScreen, camera);
            var overlay = BuildCrtOverlay(scene);
            var go = new GameObject("CRT controller");
            go.transform.SetParent(refs.Root, false);
            var crt = go.AddComponent<SiginakCrt>();
            crt.Configure(game, screen, overlay);
            return crt;
        }
    }
}
