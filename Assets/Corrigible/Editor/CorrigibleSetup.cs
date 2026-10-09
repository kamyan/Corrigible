using System.IO;
using System.Linq;
using Corrigible.Game;
using Corrigible.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Corrigible.EditorTools
{
    /// <summary>Oyunun asset'lerini ve ana sahnesini kurar. Tekrar çalıştırmak güvenlidir: var olan asset'ler korunur.</summary>
    public static class CorrigibleSetup
    {
        const string Root = "Assets/Corrigible";
        const string SettingsDir = Root + "/Settings";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Corrigible/Sahneyi kur")]
        public static string Setup()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();

            var balance = LoadOrCreate<BalanceConfig>(SettingsDir + "/BalanceConfig.asset");

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(SettingsDir + "/CorrigiblePanelSettings.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, SettingsDir + "/CorrigiblePanelSettings.asset");
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Root + "/UI/CorrigibleTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);

            // Yeni sahne kullanılmayan asset'leri bellekten atar; USS'i sahneden sonra yükle
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(Root + "/UI/Corrigible.uss");

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(10, 17, 20, 255);

            var go = new GameObject("Corrigible");
            var controller = go.AddComponent<GameController>();
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            var ui = go.AddComponent<CorrigibleUI>();

            var so = new SerializedObject(controller);
            so.FindProperty("balanceConfig").objectReferenceValue = balance;
            so.ApplyModifiedPropertiesWithoutUndo();
            so = new SerializedObject(ui);
            so.FindProperty("game").objectReferenceValue = controller;
            so.FindProperty("styleSheet").objectReferenceValue = uss;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            return $"Kuruldu: {ScenePath} (derleme listesinde ilk sahne), BalanceConfig, PanelSettings. Tema: {(panel.themeStyleSheet != null ? "tamam" : "EKSİK")}, USS: {(uss != null ? "tamam" : "EKSİK")}";
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
