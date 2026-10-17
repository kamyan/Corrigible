using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Kaydedilen çekmecelerin veri sınırı, hazır havuzları ve harita vurgusu sözleşmesi.</summary>
    public class SadeDrawerTests
    {
        [TestCase("research",3)]
        [TestCase("news",4)]
        [TestCase("teams",5)]
        [TestCase("aiLog",6)]
        [TestCase("guide",7)]
        public void SerializedDrawer_RebindsBothLanguagesWithoutCreatingHierarchyOrChangingModel(string id,int sample)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Drawers/"+id+".prefab");
            Assert.NotNull(prefab,"Çekmece F3 yazarından kaydedilmiş olmalı.");
            var instance=Object.Instantiate(prefab);var previous=DeskLoc.Instance.Lang;
            try
            {
                var folder=instance.GetComponent<SadeDrawerFolderView>();Assert.NotNull(folder);
                var mock=JObject.Parse(File.ReadAllText("Docs/UI_Sade/SonSalter_Sade_Paketi/spec/mock/masa_S"+sample+".json"));
                var data=(JObject)mock["drawer"];string before=data.ToString();
                var hierarchy=instance.GetComponentsInChildren<Transform>(true).Select(t=>t.GetInstanceID()).ToArray();
                foreach(var language in new[]{DeskLang.Tr,DeskLang.En})
                {
                    DeskLoc.Instance.SetLanguage(language);folder.Bind(data);folder.Bind(data);
                    Assert.AreEqual(id,folder.DrawerId);
                    CollectionAssert.AreEqual(hierarchy,instance.GetComponentsInChildren<Transform>(true).Select(t=>t.GetInstanceID()).ToArray(),"Bağlama sırasında hiyerarşi kurulmaz.");
                    foreach(var text in instance.GetComponentsInChildren<TMP_Text>(true))
                    {
                        Assert.IsFalse(text.enableAutoSizing,text.name);
                        Assert.GreaterOrEqual(text.fontSize,11,text.name);
                        if(text.gameObject.activeInHierarchy)StringAssert.DoesNotContain("{0}",text.text,"Biçim yuvası oyuncuya görünmez.");
                    }
                }
                Assert.AreEqual(before,data.ToString(),"Görünüm verisi çizim sırasında değiştirilmez.");
                Assert.IsNotEmpty(instance.GetComponentsInChildren<ScrollRect>(true),"Çekmece uzun içerikte kaydırılabilir.");
                foreach(var transform in instance.GetComponentsInChildren<Transform>(true))
                    Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),transform.name);
            }
            finally { DeskLoc.Instance.SetLanguage(previous);Object.DestroyImmediate(instance); }
        }

        [Test]
        public void RowHoverAndClick_SendOnlyExplicitCommandsAndClearMapHighlight()
        {
            var root=new GameObject("Test çekmecesi",typeof(RectTransform),typeof(SadeDrawerFolderView));
            var row=new GameObject("Haber",typeof(RectTransform),typeof(SadeDrawerCommandView));row.transform.SetParent(root.transform,false);
            try
            {
                var folder=root.GetComponent<SadeDrawerFolderView>();var item=row.GetComponent<SadeDrawerCommandView>();
                string region="initial",command=null,arg=null;int sent=0;
                folder.RegionHovered+=id=>region=id;folder.CommandRequested+=(cmd,id)=>{command=cmd;arg=id;sent++;};
                item.Bind(folder,"newsSelect","n1","DoA");var e=new PointerEventData(null){button=PointerEventData.InputButton.Left};
                item.OnPointerEnter(e);Assert.AreEqual("DoA",region);Assert.AreEqual(0,sent,"Üstüne gelmek komut çalıştırmaz.");
                item.OnPointerClick(e);Assert.AreEqual("newsSelect",command);Assert.AreEqual("n1",arg);Assert.AreEqual(1,sent);
                item.OnPointerExit(e);Assert.IsNull(region);
                item.OnPointerEnter(e);
                // Normal MonoBehaviour yaşam döngüsü EditMode'da çalışmaz; Play kapanışını ilet.
                typeof(SadeDrawerCommandView).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(item,null);
                row.SetActive(false);
                Assert.IsNull(region,"Panel ya da satır kapanınca harita vurgusu temizlenir.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
