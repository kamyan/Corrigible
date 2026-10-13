using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Edit modda masa önizlemesi: masa sahnesi (DeskPresenter olan) açılınca, derlemeden sonra ve Play'den dönünce veri masaya
    /// bağlanır; Play'deki gibi görünür, prefab'lar ve sahne üzerinde elle düzenleme yapılabilir.
    /// Varsayılan “oyun verisi”: sahnenin kaynağı <see cref="IDeskPreviewSource"/> ise (oyun sahnesi) oyunun açılış masası,
    /// yani Play'e basınca gelen bölgeler, yerler ve listeler; değilse (sandbox) sahte C1. Sahte C1–C4 menüden seçilir.
    /// Dinamik listelerin kopyaları sahneye kaydedilmez ve düzenlenemez (<see cref="DeskUi.MarkPreview"/>); önizleme sahneyi
    /// kirli işaretlemez. Veriden yerleşen öğeler (metinler, satır konumları) her önizlemede yeniden kurulur.
    /// </summary>
    [InitializeOnLoad]
    public static class DeskEditPreview
    {
        const string EnabledKey = "Corrigible.Desk.EditPreview", ModeKey = "Corrigible.Desk.EditPreviewMode";
        const string MockDir = DeskSpriteManifest.Root + "/Data/Mock/";
        static readonly string[] Presets = { "C1_normal", "C2_illusion", "C3_event", "C4_gameover" };
        /// <summary>Önizleme kipleri: oyun verisi (masa / giriş kâğıdıyla) ya da sahte ön ayar adı.</summary>
        public const string Game = "game", GameIntro = "game-intro";
        const string Menu = "Corrigible/Desk UI/Edit modda önizleme/";

        /// <summary>Alan yeniden yüklenince sahnedeki önceki önizleme kopyaları sahipsiz kalır; ilk önizlemeden önce silinir.</summary>
        static bool cleanupPending = true;

        static DeskEditPreview()
        {
            EditorSceneManager.sceneOpened += (s, m) => Schedule();
            EditorApplication.playModeStateChanged += st => { if (st == PlayModeStateChange.EnteredEditMode) Schedule(); };
            Schedule();
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        /// <summary>Geçerli kip: <see cref="Game"/>, <see cref="GameIntro"/> ya da sahte ön ayar (“C3_event”).</summary>
        public static string Mode
        {
            get
            {
                var m = EditorPrefs.GetString(ModeKey, Game);
                return m == Game || m == GameIntro || Array.IndexOf(Presets, m) >= 0 ? m : Game;
            }
            set => EditorPrefs.SetString(ModeKey, value);
        }

        /// <summary>Play'den dönüşte ve alan yüklenirken sahne birkaç güncelleme sonra hazır olur: o kadar denenir.</summary>
        static int attempts;

        static void Schedule()
        {
            attempts = 30;
            EditorApplication.update -= Try;
            EditorApplication.update += Try;
        }

        static void Try()
        {
            if (EditorApplication.isPlaying) { EditorApplication.update -= Try; return; }
            bool ready = !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && Object.FindFirstObjectByType<DeskPresenter>(FindObjectsInactive.Include) != null;
            if (!ready && --attempts > 0) return;
            EditorApplication.update -= Try;
            if (ready) Apply();
        }

        // ------------------------------------------------------------------ menü
        [MenuItem(Menu + "Göster", false, 0)]
        static void Toggle()
        {
            Enabled = !Enabled;
            if (Enabled) Schedule();
            else Debug.Log("[Desk] Edit mod önizlemesi kapalı; sahne yeniden açılınca şablon metinleri görünür.");
        }

        [MenuItem(Menu + "Göster", true)]
        static bool ToggleValidate() { UnityEditor.Menu.SetChecked(Menu + "Göster", Enabled); return true; }

        [MenuItem(Menu + "Yenile", false, 1)] static void Refresh() => Schedule();
        const string MGame = Menu + "Oyun verisi · Play'deki açılış masası", MIntro = Menu + "Oyun verisi · giriş kâğıdıyla";
        const string M1 = Menu + "Sahte C1 · masa", M2 = Menu + "Sahte C2 · illüzyon", M3 = Menu + "Sahte C3 · olay", M4 = Menu + "Sahte C4 · oyun sonu";
        [MenuItem(MGame, false, 20)] static void UGame() => Use(Game);
        [MenuItem(MIntro, false, 21)] static void UIntro() => Use(GameIntro);
        [MenuItem(M1, false, 40)] static void P0() => Use(Presets[0]);
        [MenuItem(M2, false, 41)] static void P1() => Use(Presets[1]);
        [MenuItem(M3, false, 42)] static void P2() => Use(Presets[2]);
        [MenuItem(M4, false, 43)] static void P3() => Use(Presets[3]);
        [MenuItem(MGame, true)] static bool VGame() => Check(MGame, Game);
        [MenuItem(MIntro, true)] static bool VIntro() => Check(MIntro, GameIntro);
        [MenuItem(M1, true)] static bool V0() => Check(M1, Presets[0]);
        [MenuItem(M2, true)] static bool V1() => Check(M2, Presets[1]);
        [MenuItem(M3, true)] static bool V2() => Check(M3, Presets[2]);
        [MenuItem(M4, true)] static bool V3() => Check(M4, Presets[3]);

        static bool Check(string item, string mode) { UnityEditor.Menu.SetChecked(item, Enabled && Mode == mode); return true; }

        /// <summary>Kipi seçer ve önizlemeyi yeniden kurar (menü ve araçlar).</summary>
        public static void Use(string mode) { Mode = mode; Enabled = true; Schedule(); }

        // ------------------------------------------------------------------ önizleme
        /// <summary>Açık masa sahnesine sahte veriyi bağlar (Play'de ve kapalıyken bir şey yapmaz).</summary>
        public static void Apply()
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var presenter = Object.FindFirstObjectByType<DeskPresenter>(FindObjectsInactive.Include);
            if (presenter == null) return;
            var scene = presenter.gameObject.scene;
            bool wasDirty = scene.isDirty;
            if (cleanupPending) { RemoveOrphans(scene); cleanupPending = false; }
            var vm = Build(presenter, Mode);
            if (vm == null) return;
            DeskUi.EditPreview = true;
            try { presenter.Preview(vm); }
            catch (Exception e) { Debug.LogWarning("[Desk] Edit mod önizlemesi kurulamadı: " + e.Message); }
            finally { DeskUi.EditPreview = false; }
            if (!wasDirty) { ClearDirty(scene); KeepClean(scene, 5); }
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// Oyun kipinde sahnenin kendi kaynağı (oyun sahnesi: açılış masası, Play'deki bölgeler ve yerler); kaynak önizleme
        /// veremiyorsa (sandbox) sahte C1. Sahte kipte Data/Mock'taki ön ayar.
        /// </summary>
        static DeskViewModel Build(DeskPresenter presenter, string mode)
        {
            if (mode == Game || mode == GameIntro)
            {
                var src = new SerializedObject(presenter).FindProperty("dataSource").objectReferenceValue as IDeskPreviewSource;
                if (src != null)
                {
                    try
                    {
                        var vm = src.BuildPreview(mode == GameIntro);
                        if (vm != null) return vm;
                    }
                    catch (Exception e) { Debug.LogWarning("[Desk] Oyun verisiyle önizleme kurulamadı, sahte C1 kullanılıyor: " + e.Message); }
                }
                return BuildMock(0);
            }
            return BuildMock(Math.Max(0, Array.IndexOf(Presets, mode)));
        }

        /// <summary>Sahte VM: geçici, gizli bir sahte kaynak Data/Mock'tan kurar (sahneye hiçbir şey eklenmez).</summary>
        static DeskViewModel BuildMock(int preset)
        {
            var go = new GameObject("_DeskEditPreviewSource") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var src = go.AddComponent<MockDeskDataSource>();
                var so = new SerializedObject(src);
                var arr = so.FindProperty("presets");
                arr.arraySize = Presets.Length;
                for (int i = 0; i < Presets.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(MockDir + Presets[i] + ".json");
                so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(MockDir + "map.json");
                so.FindProperty("englishMock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(MockDir + "mock_en.txt");
                so.FindProperty("presetIndex").intValue = preset;
                so.ApplyModifiedPropertiesWithoutUndo();
                return src.Current;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Desk] Önizleme verisi kurulamadı: " + e.Message);
                return null;
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>Sahnede kayıtsız (önizleme) kopyaları siler: alan yeniden yüklenince havuzları kaybolmuş, sahipsizdir.</summary>
        static void RemoveOrphans(Scene scene)
        {
            var doomed = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    bool mine = (t.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0;
                    bool parentMine = t.parent != null && (t.parent.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0;
                    if (mine && !parentMine) doomed.Add(t.gameObject);
                }
            foreach (var g in doomed) if (g != null) Object.DestroyImmediate(g);
        }

        /// <summary>
        /// Bileşenlerin ertelenmiş güncellemeleri (OnValidate, Deferred) önizlemeden sonraki birkaç editör güncellemesinde
        /// sahneyi yeniden kirletir; o güncellemelerde de temiz tutulur.
        /// </summary>
        static void KeepClean(Scene scene, int updates)
        {
            int left = updates;
            void Tick()
            {
                if (--left < 0 || !scene.isLoaded) { EditorApplication.update -= Tick; return; }
                ClearDirty(scene);
            }
            EditorApplication.update += Tick;
        }

        /// <summary>Önizleme sahnede değişiklik sayılmasın (kaydedilmemiş değişiklik uyarısı çıkmasın).</summary>
        static void ClearDirty(Scene scene)
        {
            var m = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            m?.Invoke(null, new object[] { scene });
        }
    }
}
