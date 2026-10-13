using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Corrigible.Desk;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F7 cila: sahte verinin İngilizcesi (sözlük, kalıplar, biçim, kalıntı yok), statik fontlarda boşluk (batch), lejant akışı.</summary>
    public class DeskPolishTests
    {
        const string Root = "Assets/Corrigible/Desk";

        static MockEnglish Dictionary() =>
            new MockEnglish(AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/mock_en.txt").text);

        [Test]
        public void MockEnglish_TranslatesExactPatternsAndFormats()
        {
            var en = Dictionary();
            Assert.AreEqual("POL. CAPITAL", en.Translate("SİYASİ GÜÇ"), "dar gösterge başlığı kısa biçim");
            Assert.AreEqual("4 funds · 2 PC · 2d", en.Translate("4 fon · 2 SG · 2 gün"), "maliyet satırında gün = d");
            Assert.AreEqual("12 days left", en.Translate("12 gün kaldı"));
            Assert.AreEqual("Funds 47.3 → 39.3 · spend +0.6/day", en.Translate("Fon 47,3 → 39,3 · gider +0,6/gün"));
            Assert.AreEqual("to 100%: <style=num>~48 days</style>", en.Translate("%100'e <style=num>~48 gün</style>"));
            Assert.AreEqual("D-074 · 38%", MockEnglish.Format("G-074 · %38"));
            Assert.AreEqual("47.3", MockEnglish.Format("47,3"));
        }

        [Test]
        public void MockEnglish_LooksTurkishIgnoresTags()
        {
            Assert.IsTrue(MockEnglish.LooksTurkish("to 100%: ~48 gün"));
            Assert.IsTrue(MockEnglish.LooksTurkish("Fon 47.3"));
            Assert.IsTrue(MockEnglish.LooksTurkish("şüpheli"));
            Assert.IsFalse(MockEnglish.LooksTurkish("<style=num>~48 days</style>"));
            Assert.IsFalse(MockEnglish.LooksTurkish("Monitoring 4/12 · Verified 4/12"));
        }

        static MockDeskDataSource Mock(out GameObject go)
        {
            go = new GameObject("MockTest");
            var src = go.AddComponent<MockDeskDataSource>();
            var so = new SerializedObject(src);
            var presets = so.FindProperty("presets");
            string[] names = { "C1_normal", "C2_illusion", "C3_event", "C4_gameover" };
            presets.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                presets.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/" + names[i] + ".json");
            so.FindProperty("mapSpec").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json");
            so.FindProperty("englishMock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/mock_en.txt");
            so.ApplyModifiedPropertiesWithoutUndo();
            return src;
        }

        static void Strings(object o, List<string> into, HashSet<object> seen, int depth)
        {
            if (o == null || depth > 12 || !seen.Add(o)) return;
            var t = o.GetType();
            if (t.Namespace != typeof(MockEnglish).Namespace) return;
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                // Kod alanları (ekranda görünmez): kimlik, tuş kodu, simge, komut, durum (ör. TeamTokenVM.State)
                if (f.Name == "Id" || f.Name.EndsWith("Id") || f.Name == "Key" || f.Name == "Icon" || f.Name == "Command" || f.Name == "State") continue;
                switch (f.GetValue(o))
                {
                    case null: break;
                    case string s: into.Add(f.Name + "=" + s); break;
                    case List<string> ls: foreach (var s in ls) into.Add(f.Name + "=" + s); break;
                    case IDictionary d: foreach (var k in d.Keys) Strings(d[k], into, seen, depth + 1); break;
                    case IList l: foreach (var x in l) Strings(x, into, seen, depth + 1); break;
                    case var v when !f.FieldType.IsValueType: Strings(v, into, seen, depth + 1); break;
                }
            }
        }

        [Test]
        public void MockPresets_HaveNoTurkishLeftoversInEnglish()
        {
            var start = DeskLoc.Instance.Lang;
            var src = Mock(out var go);
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                var bad = new List<string>();
                for (int p = 0; p < src.PresetCount; p++)
                {
                    src.Load(p);
                    var all = new List<string>();
                    Strings(src.Current, all, new HashSet<object>(), 0);
                    Assert.Greater(all.Count, 50, "görünüm modeli metinleri toplandı");
                    foreach (var s in all)
                    {
                        // Görünümler metni DeskLoc.T'den geçirir: durum/tuş adları (State, Keys) oyunun çeviri tablosundan gelir
                        var v = s.Substring(s.IndexOf('=') + 1);
                        if (MockEnglish.LooksTurkish(v) && MockEnglish.LooksTurkish(DeskLoc.Instance.T(v))) bad.Add($"C{p + 1} {s}");
                    }
                }
                CollectionAssert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally
            {
                DeskLoc.Instance.SetLanguage(start);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StaticFonts_ContainSpace()
        {
            // Boşluk statik atlasta yoksa her boşluklu metin dinamik yedekten alt ağ açar (C1: 98 → 65 batch)
            foreach (var name in new[] { "CP-Regular", "CP-Bold", "LF-Regular", "LF-Medium", "LF-SemiBold", "LF-Bold", "LF-ExtraBold", "Kalam-Regular", "Kalam-Bold" })
            {
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EditorTools.DeskFontSetup.AssetPath(name));
                Assert.IsNotNull(fa, name);
                Assert.AreEqual(AtlasPopulationMode.Static, fa.atlasPopulationMode, name);
                Assert.IsTrue(fa.characterLookupTable.ContainsKey(' '), name + ": U+0020");
            }
        }

        [Test]
        public void MapPrefab_LegendFlows()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Screens/Map.prefab");
            var legend = p.transform.Find("Legend");
            Assert.IsNotNull(legend);
            Assert.IsNotNull(legend.GetComponent<MapLegendFlow>(), "EN'de uzun başlıklar sonraki öğeleri itsin");
            Assert.IsNotNull(legend.Find("Box"), "akış sınırı lejant kutusundan");
        }
    }
}
