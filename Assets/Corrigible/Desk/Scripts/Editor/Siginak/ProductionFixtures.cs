using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Tarif defteri ve üretimin (R18) sunum fikstürleri (Revizyon 3 · Aşama 5, PLAN §3 W7, E §6.2; Aşama 4 D8 kalıbı). Core'a dokunmaz:
    /// gerçek komutlarla ancak çok uzun kayıtla ulaşılan durumlar (açlık, 32 eşyalık depo) ve görsel uç durumlar (8 fiş, en uzun adlar,
    /// ilerleme kutuları, YENİ + kayış) canlı anlık görüntünün kopyası üzerinde kurulup <c>BoardView3D.Render(s, false)</c> ile çizilir.
    /// <list type="bullet">
    /// <item><see cref="Present"/> (Play, K4): adı verilen fikstürü panoya çizer; simülasyon değişmez, komut yazılmaz. Bir sonraki
    /// <c>game.Changed</c>, dil değişimi, poz ya da <see cref="Restore"/> gerçek görüntüye döner.</item>
    /// <item><see cref="RenderBookFlat"/>: defterin bir açılımının düz, dik çizimi (2240 × 1600 = <c>tarif_acilim_n_{tr,en}@2x.png</c>
    /// boyu) ve referansla yan yana kopyası (<c>A5_R18a_sayfa_*</c>). Önizleme sahnesinde kurar, açık sahneye dokunmaz.</item>
    /// <item><see cref="NewSimulation"/> / <see cref="BookSnapshot"/>: oyunun denge asset'iyle tohum 79'un V.001'i (punto taraması ve
    /// düz çizim aynı veriyi kullanır).</item>
    /// </list>
    /// <c>yeni</c> fikstürünün kayışı bittiğinde sunucu sunum durumunu (<c>newItem</c>) yerleşim dosyasına yazar: görüntüler kayıt
    /// yalıtımıyla (SessionState <c>Corrigible.TestStorage</c>, GELISTIRME) alınmalı.
    /// </summary>
    public static class ProductionFixtures
    {
        public const string BalancePath = "Assets/Corrigible/Settings/BalanceConfig.asset";
        /// <summary>Defter sayfasının prefab'ı (W1 kurucusu).</summary>
        public const string PagePrefabPath = DeskSpriteManifest.Root + "/Prefabs/Rev3Board/RecipeBookPage.prefab";
        /// <summary>Tasarımın açılım görüntüleri (@2x, 2240 × 1600).</summary>
        public const string ReferenceDir = "Docs/UI_Siginak_Rev3/threejs/r10/papers_new";
        public const string OutDir = "Temp";

        /// <summary>
        /// Fikstür adları: <c>durdu_aclik</c>, <c>durdu_depo</c> (DURDU + neden), <c>eksik</c> (metal 2, "eksik: 1 kumaş"), <c>fis8</c>
        /// (8 fiş, <c>ChipCap</c>), <c>en_uzun</c> (bu dilin en uzun tarif adı TARİF kutusunda), <c>ilerleme</c> (iki vardiyalık EMP
        /// hücresi 1/2), <c>yeni</c> (YENİ + kayış).
        /// </summary>
        public static readonly string[] Names = { "durdu_aclik", "durdu_depo", "eksik", "fis8", "en_uzun", "ilerleme", "yeni" };

        // ================================================================== veri

        /// <summary>Oyunun denge değerleriyle yeni harekât (kartlı pano; sunucunun ilk açılışındaki <c>board_auto</c> dahil).</summary>
        public static Simulation NewSimulation(uint seed = 79)
        {
            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            var balance = config != null ? config.CreateRuntimeCopy() : new Balance();
            balance.planningBoardEnabled = true;
            var sim = Simulation.NewGame(seed, balance);
            sim.EnableAutomaticProduction();
            return sim;
        }

        /// <summary>V.001'in anlık görüntüsü o anki dilde (defter sayfaları, tarifler, sözcükler).</summary>
        public static BoardSnapshot BookSnapshot(bool english) =>
            PlanningBoardSnapshotBuilder.Build(NewSimulation(), new BoardUiState(), english, false, false);

        static DeskLang LangOf(BoardSnapshot s) => s.English ? DeskLang.En : DeskLang.Tr;

        static RecipeCardData CardOf(BoardSnapshot s, RecipeData r, string id, int done)
        {
            var d = new RecipeCardData
            {
                Id = id, RecipeId = r.Id, Name = r.Name, IconKey = r.Id, TurnsLine = r.TurnsLine, OutLine = Loc.T("→ 1 ürün"),
                NeedsLabel = s.Book != null ? s.Book.Input : null, Turns = r.Turns, Done = r.Turns > 1 ? done : -1,
            };
            foreach (var i in r.Inputs) if (i != null) d.Inputs.Add(i);
            return d;
        }

        static string Maker(BoardSnapshot s)
        {
            string id = s.PocketOccupant.TryGetValue("craft", out var who) ? who : null;
            var p = id != null ? s.Person(id) : null;
            return p != null ? p.Name : Loc.T("üretici yok");
        }

        static void Chips(BoardSnapshot s, params (string kind, int count)[] kinds)
        {
            s.Chips.Clear();
            var lang = LangOf(s);
            foreach (var (kind, count) in kinds)
            {
                string name = DeskText.Upper(PlanningBoardData.MaterialName(kind), lang);
                for (int j = 0; j < count; j++) s.Chips.Add(new ChipData { Id = BoardUiState.ChipId(kind, j), Kind = kind, Name = name, Index = j });
            }
        }

        static void ChipsFor(BoardSnapshot s, RecipeData r, int lessOfLast = 0)
        {
            var list = new List<(string, int)>();
            for (int i = 0; i < r.Inputs.Count; i++)
            {
                var input = r.Inputs[i];
                int n = input.Quantity - (i == r.Inputs.Count - 1 ? lessOfLast : 0);
                if (n > 0) list.Add((input.Kind, n));
            }
            Chips(s, list.ToArray());
        }

        static OrderData Order(BoardSnapshot s, RecipeData r, OrderState state, string status, int done = 0)
        {
            const string id = "is-fikstur";
            return new OrderData
            {
                State = state, Number = s.Order != null ? s.Order.Number : null, OrderId = id, RecipeId = r.Id, Status = status,
                Stamp = string.Empty, StopReason = string.Empty, DropHint = PlanningBoardSnapshotBuilder.OrderDropHint(state, s.Editable),
                Started = state == OrderState.Started, Turns = r.Turns, Done = done,
                Card = CardOf(s, r, id, done), CardDraggable = false,
            };
        }

        static RecipeData Recipe(BoardSnapshot s, string id) => s.Recipes.FirstOrDefault(r => r != null && r.Id == id);

        /// <summary>
        /// Fikstürü anlık görüntüye uygular (yerinde); bilinmeyen adda false. Durum satırları ve damga kurucunun sözcükleriyle aynı
        /// biçimde (<c>PlanningBoardSnapshotBuilder.FillOrder</c>), Core'un o duruma ulaştığı varsayımıyla.
        /// </summary>
        public static bool Apply(string name, BoardSnapshot s)
        {
            if (s == null || s.Recipes.Count == 0) return false;
            var lang = LangOf(s);
            var armor = Recipe(s, "armor");
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "durdu_aclik":
                case "durdu_depo":
                {
                    string reason = name.EndsWith("aclik") ? Loc.T("açlık") : Loc.T("eşya deposu dolu");
                    var o = Order(s, armor, OrderState.Stopped, Loc.F("{0} · {1}", PlanningBoardSnapshotBuilder.Lowered(armor.Name, lang), reason));
                    o.Stamp = Loc.T("DURDU");
                    o.StopReason = reason;
                    s.Order = o;
                    ChipsFor(s, armor);
                    return true;
                }
                case "eksik":
                {
                    var last = armor.Inputs[armor.Inputs.Count - 1];
                    string lack = Loc.F("{0} {1}", 1, PlanningBoardSnapshotBuilder.Lowered(PlanningBoardData.MaterialName(last.Kind), lang));
                    s.Order = Order(s, armor, OrderState.Missing, Loc.F("eksik: {0}", lack));
                    ChipsFor(s, armor, 1);
                    return true;
                }
                case "fis8":
                    s.Order = Order(s, armor, OrderState.Full,
                        Loc.F("{0} · girdiler ayrıldı · üretici: {1}", PlanningBoardSnapshotBuilder.Lowered(armor.Name, lang), Maker(s)));
                    Chips(s, ("metal", 2), ("devre", 2), ("kumas", 2), ("kimyasal", 2));
                    return true;
                case "en_uzun":
                {
                    var longest = s.Recipes.Where(r => r != null).OrderByDescending(r => (r.Name ?? "").Length).First();
                    s.Order = Order(s, longest, OrderState.Full,
                        Loc.F("{0} · girdiler ayrıldı · üretici: {1}", PlanningBoardSnapshotBuilder.Lowered(longest.Name, lang), Maker(s)));
                    ChipsFor(s, longest);
                    return true;
                }
                case "ilerleme":
                {
                    var emp = s.Recipes.Where(r => r != null && r.Turns > 1).OrderBy(r => r.Index).FirstOrDefault() ?? Recipe(s, "emp_cell");
                    int done = Mathf.Max(1, emp.Turns - 1);
                    s.Order = Order(s, emp, OrderState.Started,
                        Loc.F("{0} · girdiler ödendi · {1}/{2} · üretici: {3}", PlanningBoardSnapshotBuilder.Lowered(emp.Name, lang), done, emp.Turns, Maker(s)), done);
                    s.Chips.Clear();   // başlamış emirde fiş çizilmez (girdiler ödendi; D7-A)
                    return true;
                }
                case "yeni":
                {
                    // zırh yığını önce, yoksa ilk dolu yığın: en yeni (üstteki) eşya "yeni ürün"
                    int stack = -1;
                    foreach (int k in new[] { 3, 4, 5, 0, 1, 2 })
                        if (k < s.Kits.Length && s.Kits[k] != null && s.Kits[k].TopIds.Count > 0) { stack = k; break; }
                    if (stack < 0) return false;
                    var kit = s.Kits[stack];
                    s.NewItem = new NewItemData { ItemId = kit.TopIds[kit.TopIds.Count - 1], Kind = kit.Kind ?? BoardLayout.KitKinds[stack], Stack = stack, Day = -1 };
                    s.Order = new OrderData { State = OrderState.Empty, Number = s.Order != null ? s.Order.Number : null };
                    s.Chips.Clear();
                    return true;
                }
            }
            return false;
        }

        // ================================================================== Play: panoya sunum

        /// <summary>
        /// Play içinde (yalnız sunum): canlı simülasyonun anlık görüntüsü kurulur, <paramref name="name"/> fikstürü uygulanır ve pano
        /// anında çizilir (<c>BoardView3D.Render(s, false)</c>). Komut yazılmaz, simülasyon değişmez. Görüntü için önce K4'e git;
        /// <c>yeni</c> fikstürü K4'teyken kayışı hemen oynatır (K1'deyken TARİF kutusunda bekler, K4'e varınca kayar). Dil: o anki
        /// masa dili (EN için önce <c>DeskLoc.Instance.SetLanguage(DeskLang.En)</c>).
        /// </summary>
        public static string Present(string name)
        {
            if (!Application.isPlaying) return "Play gerekli";
            var game = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
            var presenter = Object.FindFirstObjectByType<PlanningBoardPresenter>(FindObjectsInactive.Include);
            var board = Object.FindFirstObjectByType<BoardView3D>(FindObjectsInactive.Include);
            if (game == null || game.Sim?.Board == null) return "Kartlı oyun yok (GameController.Sim.Board)";
            if (presenter == null || board == null) return "Sahnede PlanningBoardPresenter ya da BoardView3D yok";
            bool english = DeskLoc.Instance.Lang == DeskLang.En;
            var s = PlanningBoardSnapshotBuilder.Build(game.Sim, presenter.Ui, english, false, false);
            if (!Apply(name, s)) return "Bilinmeyen ya da uygulanamayan fikstür: " + name + " (" + string.Join(", ", Names) + ")";
            if (s.NewItem != null && s.NewItem.Day < 0) s.NewItem.Day = game.Sim.S.Day;
            board.Render(s, false);
            var o = s.Order;
            return $"{name}: form {o.State}{(string.IsNullOrEmpty(o.Stamp) ? "" : " · " + o.Stamp)} · “{o.Status}” · {s.Chips.Count} fiş" +
                   (s.NewItem != null ? $" · yeni {s.NewItem.ItemId}" : "") + $" · dil {Loc.Lang}";
        }

        /// <summary>Gerçek görüntüye döner (sunucunun çizimi; komut yazmaz).</summary>
        public static string Restore()
        {
            if (!Application.isPlaying) return "Play gerekli";
            var presenter = Object.FindFirstObjectByType<PlanningBoardPresenter>(FindObjectsInactive.Include);
            if (presenter == null) return "Sahnede PlanningBoardPresenter yok";
            presenter.Refresh();
            return "pano gerçek görüntüde";
        }

        // ================================================================== düz çizim (2240 × 1600)

        /// <summary>Tasarımın açılım görüntüsü (<c>tarif_acilim_{n}_{tr,en}@2x.png</c>, n = 1–3).</summary>
        public static string ReferencePng(int spread, bool english) =>
            ReferenceDir + "/tarif_acilim_" + (spread + 1) + "_" + (english ? "en" : "tr") + "@2x.png";

        /// <summary>
        /// Defterin <paramref name="spread"/>. açılımının (0–2) düz, dik çizimi: sayfa prefab'ı iki kez önizleme sahnesinde kurulur, V.001'in
        /// tarifleriyle (<see cref="BookSnapshot"/>) bağlanır ve önden ortografik kamerayla <paramref name="width"/> × width/1,4 çizilir
        /// (varsayılan 2240 × 1600 = tasarım PNG'si). Zemin sayfa rengi (#F3EBD3; doku meshte, prefab'da yok); yazı ve şekiller ışıksız.
        /// Referans varsa <c>…_yan.png</c> (sol Unity, sağ tasarım). Dil <c>Loc</c> ile geçici değişir. Sonuçta taşma alanları yazılır.
        /// </summary>
        public static string RenderBookFlat(int spread, bool english, string path = null, int width = 2240)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePrefabPath);
            if (prefab == null) return "Sayfa prefab'ı yok: " + PagePrefabPath + " (Aşama 5'in tek seferlik kurulumu; script silindi)";
            spread = Mathf.Clamp(spread, 0, 2);
            width = Mathf.Clamp(width, 280, 4096);
            int height = Mathf.RoundToInt(width / 1.4f);
            string tag = english ? "en" : "tr";
            path ??= Path.Combine(OutDir, "A5_R18a_sayfa_" + (spread + 1) + "_" + tag + "_flat.png");

            string previous = Loc.Lang;
            var preview = EditorSceneManager.NewPreviewScene();
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            var active = RenderTexture.active;
            Texture2D tex = null, reference = null, pair = null;
            try
            {
                SiginakTextScan.UseLanguage(english ? Loc.English : Loc.Turkish);
                var s = BookSnapshot(english);
                int count = (s.Recipes.Count + 1) / 2;
                var pages = new RecipeBookPage[2];
                for (int side = 0; side < 2; side++)
                {
                    var go = Object.Instantiate(prefab);
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(go, preview);
                    if (!go.activeSelf) go.SetActive(true);
                    var page = go.GetComponent<RecipeBookPage>();
                    if (page == null) return "Prefab'ın kökünde RecipeBookPage yok: " + PagePrefabPath;
                    float halfMm = page.Layout != null ? page.Layout.sizeMm.x * 0.5f : 49.5f;
                    go.transform.SetPositionAndRotation(new Vector3((side == 0 ? -halfMm : halfMm) * 0.001f, 0, 0), Quaternion.identity);
                    pages[side] = page;
                }
                var left = spread * 2 < s.Recipes.Count ? s.Recipes[spread * 2] : null;
                var right = spread * 2 + 1 < s.Recipes.Count ? s.Recipes[spread * 2 + 1] : null;
                pages[0].Bind(left, s.Book, false, spread > 0, false, true);
                pages[1].Bind(right, s.Book, true, false, spread < count - 1, true);
                Canvas.ForceUpdateCanvases();

                var camGo = new GameObject("Book flat camera") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.scene = preview;
                cam.orthographic = true;
                float pageH = pages[0].Layout != null ? pages[0].Layout.sizeMm.y : 140f;
                cam.orthographicSize = pageH * 0.0005f;
                cam.aspect = width / (float)height;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = pages[0].Palette != null ? pages[0].Palette.page : new Color32(0xF3, 0xEB, 0xD3, 0xFF);
                camGo.transform.SetPositionAndRotation(new Vector3(0, 0, -0.5f), Quaternion.LookRotation(Vector3.forward, Vector3.up));

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                var overflow = pages.SelectMany(p => p.OverflowFields).ToList();
                string result = path + (overflow.Count > 0 ? " · TAŞMA: " + string.Join(", ", overflow) : " · taşma yok");

                string refPath = ReferencePng(spread, english);
                if (File.Exists(refPath))
                {
                    reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (reference.LoadImage(File.ReadAllBytes(refPath)))
                    {
                        pair = SideBySide(tex, reference);
                        string pairPath = Path.Combine(dir ?? "", Path.GetFileNameWithoutExtension(path).Replace("_flat", "") + "_yan.png");
                        File.WriteAllBytes(pairPath, pair.EncodeToPNG());
                        result += " · yan yana " + pairPath;
                    }
                }
                return result;
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(rt);
                if (tex != null) Object.DestroyImmediate(tex);
                if (reference != null) Object.DestroyImmediate(reference);
                if (pair != null) Object.DestroyImmediate(pair);
                EditorSceneManager.ClosePreviewScene(preview);
                SiginakTextScan.UseLanguage(previous);
            }
        }

        /// <summary>Üç açılımın TR ve EN düz çizimleri (<c>A5_R18a_sayfa_{1,2,3}_{tr,en}</c> + <c>_yan</c>).</summary>
        public static string RenderAllBookFlat(string dir = OutDir)
        {
            var lines = new List<string>();
            for (int spread = 0; spread < 3; spread++)
                foreach (bool en in new[] { false, true })
                    lines.Add(RenderBookFlat(spread, en, Path.Combine(dir, "A5_R18a_sayfa_" + (spread + 1) + "_" + (en ? "en" : "tr") + "_flat.png")));
            return string.Join("\n", lines);
        }

        /// <summary>İki görüntü yan yana (sol <paramref name="left"/>, sağ <paramref name="right"/> sol yüksekliğine en yakın komşuyla ölçeklenir).</summary>
        static Texture2D SideBySide(Texture2D left, Texture2D right)
        {
            int h = left.height;
            int rw = Mathf.Max(1, Mathf.RoundToInt(right.width * (h / (float)right.height)));
            var pair = new Texture2D(left.width + rw, h, TextureFormat.RGB24, false);
            pair.SetPixels(0, 0, left.width, h, left.GetPixels());
            var scaled = new Color[rw * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(right.height - 1, Mathf.FloorToInt(y * right.height / (float)h));
                for (int x = 0; x < rw; x++)
                {
                    int sx = Mathf.Min(right.width - 1, Mathf.FloorToInt(x * right.width / (float)rw));
                    scaled[y * rw + x] = right.GetPixel(sx, sy);
                }
            }
            pair.SetPixels(left.width, 0, rw, h, scaled);
            pair.Apply();
            return pair;
        }
    }
}
