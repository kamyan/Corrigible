using System.IO;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    public sealed class SadeTooltipTests
    {
        static JObject Sample(string name) => JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
            "../Docs/UI_Sade/SonSalter_Sade_Paketi/spec/mock/masa_" + name + ".json")));

        [Test]
        public void NodeTooltip_UsesOnlyShownValue_UnknownAndNullStayUnknown()
        {
            var tip = (JObject)Sample("S8")["nodeTooltip"];
            Assert.IsTrue(SadeNodeTooltipView.TryShownValue(tip, out var shown));
            Assert.AreEqual(52, shown);
            tip["trust"] = "unknown";
            // Sayısal alan yanlışlıkla dolu kalsa dahi bilinmeyen güven durumu gerçek değeri açmaz.
            Assert.IsFalse(SadeNodeTooltipView.TryShownValue(tip, out _));
            tip["trust"] = "report"; tip["value"] = JValue.CreateNull();
            Assert.IsFalse(SadeNodeTooltipView.TryShownValue(tip, out _));
            Assert.IsFalse(SadeNodeTooltipView.TryShownValue(null, out _));
        }

        [Test]
        public void NodeTooltip_ZeroIsAValidShownReading()
        {
            var tip = new JObject { ["value"] = 0, ["trust"] = "verified" };
            Assert.IsTrue(SadeNodeTooltipView.TryShownValue(tip, out var shown));
            Assert.AreEqual(0, shown);
        }

        [Test]
        public void FundBreakdown_S3ShownTotalsAgreeWithNet_AndSourceRemainsUnchanged()
        {
            var tip = (JObject)Sample("S3")["tooltip"];
            string original = tip.ToString();
            float income = SadeBreakdownTooltipView.Sum((JArray)tip["income"]);
            float expense = SadeBreakdownTooltipView.Sum((JArray)tip["expense"]);
            Assert.AreEqual(6.1f, income, .0001f);
            Assert.AreEqual(7.3f, expense, .0001f);
            Assert.AreEqual((float)tip["net"], income - expense, .0001f);
            Assert.AreEqual(original, tip.ToString());
            Assert.AreEqual(0, SadeBreakdownTooltipView.Sum(null));
        }

        [Test]
        public void FundBreakdown_IllusionKeepsPresentedCoverageClueWithoutInventingTransactions()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Tooltips/SadeBreakdownTooltip.prefab");
            var go = Object.Instantiate(prefab);
            try
            {
                var view = go.GetComponent<SadeBreakdownTooltipView>();
                view.Bind(new JObject { ["target"] = "fund", ["trust"] = "verified",
                    ["items"] = new JArray(new JObject { ["label"] = "FON", ["value"] = 47 }),
                    ["note"] = "Kapsam %0 · onaylandı · 0 ms" });
                var so = new UnityEditor.SerializedObject(view);
                var note = (TMPro.TMP_Text)so.FindProperty("note").objectReferenceValue;
                var balance = (GameObject)so.FindProperty("balanceRoot").objectReferenceValue;
                Assert.IsTrue(note.gameObject.activeSelf);
                StringAssert.Contains("0 ms", note.text);
                Assert.IsFalse(balance.activeSelf);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
