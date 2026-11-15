using System;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R1–R4: model bağlantıları, zımba sırası ve baskı dokularının gerçekten kullanılan UV'leri.</summary>
    public sealed class Rev3Stage8EntranceTests
    {
        static SerializedProperty P(Object o,string f)=>new SerializedObject(o).FindProperty(f);
        static T R<T>(Object o,string f)where T:Object=>(T)P(o,f).objectReferenceValue;
        const string DeskScene="Assets/Corrigible/Desk/Scenes/DeskGame.unity",EntranceScene="Assets/Corrigible/Desk/Scenes/Giris.unity";
        static T[] All<T>(UnityEngine.SceneManagement.Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        /// <summary>
        /// Sahnedeki gerçek dolaplar (model entegrasyonu: oda ve menü aynı SM_PowerBox1 kutusu, kapak −160°). Plaka yazıları gerilmesiz ve kenar
        /// vidalarının arasında (DİL · LANGUAGE plakası 1,4 × 1,4 ölçeklenmişti, yazı yatayda 1,4× gerilip vidaların üstüne biniyordu). Punto iki
        /// sınırla: tasarım biriminde büyük harf ≥ 6,7 mm (yuva) ve dünya ölçeğiyle, dolabın kamera çapasından (K9 / M1, dikey FOV, 1080 satır)
        /// ekranda em ≥ <c>cabinet.plates.minPxAtK9</c> (punto taramasının ölçüsü, <c>SiginakTextScan.MeasureWorld</c>). İç düzen 0,70 ölçekli:
        /// yalnız ilk sınır buna bakmaz, ikincisi küçülen yazıyı ve yaklaşan çapayı birlikte ölçer.
        /// </summary>
        [TestCase(DeskScene,-160)] [TestCase(EntranceScene,-160)]
        public void Cabinet_UsesTheModelPivotsAndReadablePlates(string path,float angle)
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);Camera k9=null;
            try
            {
                var c=All<SettingsCabinet>(scene).Single();var root=c.gameObject;
                var theme=SiginakTheme.Active;Assert.IsNotNull(theme,"SiginakTheme");float minPx=theme.NumberOf("cabinet.plates.minPxAtK9",-1);Assert.Greater(minPx,0,"cabinet.plates.minPxAtK9");
                k9=new GameObject("K9 ölçü"){hideFlags=HideFlags.HideAndDontSave}.AddComponent<Camera>();k9.enabled=false;k9.targetTexture=new RenderTexture(1920,1080,0){hideFlags=HideFlags.HideAndDontSave};
                k9.fieldOfView=c.CameraFieldOfView;k9.nearClipPlane=.03f;k9.transform.SetPositionAndRotation(c.CameraAnchor.position,c.CameraAnchor.rotation);
                Assert.AreEqual(angle,P(c,"leafOpenDeg").floatValue);Assert.IsNotNull(R<PartDriver>(c,"bulbPart"));Assert.AreEqual(LightShadows.None,R<Light>(c,"panelFill").shadows);
                foreach(var f in root.GetComponentsInChildren<CabinetFader>(true))foreach(string key in new[]{"handlePart","needlePart","lampPart"})Assert.IsNotNull(R<PartDriver>(f,key),f.name+key);
                var interior=R<GameObject>(c,"interior");interior.SetActive(true);
                foreach(var model in root.GetComponentsInChildren<Rev3Model>(true).Where(m=>m.ModelName=="CabPlate"))foreach(var slot in model.Slots)
                {
                    Assert.IsFalse(slot.Text.enableAutoSizing);Assert.GreaterOrEqual(slot.Text.fontSize*0.1f*slot.Text.font.faceInfo.capLine/slot.Text.font.faceInfo.pointSize,.0067f);
                    var t=slot.Text.transform;Assert.AreEqual(1,t.lossyScale.x/t.lossyScale.y,.01f,slot.Text.text+" yatayda gerilmiş");
                    var (px,_)=SiginakTextScan.MeasureWorld(slot.Text,new[]{("K9",k9)},false,false);Assert.GreaterOrEqual(px,minPx,slot.Text.text+" çapadan ekranda (px)");
                    // Glif sınırı ile kenar vidalarının iç kenarı (CabPlate'in 2. alt mesh'i vidalar: genişlikten iki vida çapı düşülür), Body yerelinde
                    var body=model.transform.Find("Body");var mesh=body.GetComponent<MeshFilter>().sharedMesh;var screws=mesh.GetTriangles(1).Select(i=>mesh.vertices[i]).ToArray();
                    float d=screws.Max(v=>v.y)-screws.Min(v=>v.y),inner0=screws.Min(v=>v.x)+d,inner1=screws.Max(v=>v.x)-d;
                    slot.Text.ForceMeshUpdate(true,true);var info=slot.Text.textInfo;Assert.Greater(info.characterCount,0,slot.Text.text);
                    for(int i=0;i<info.characterCount;i++){var ch=info.characterInfo[i];if(!ch.isVisible)continue;
                        float x0=body.InverseTransformPoint(t.TransformPoint(ch.bottomLeft)).x,x1=body.InverseTransformPoint(t.TransformPoint(ch.topRight)).x;
                        Assert.GreaterOrEqual(Mathf.Min(x0,x1),inner0,slot.Text.text+" sol vidanın üstünde");Assert.LessOrEqual(Mathf.Max(x0,x1),inner1,slot.Text.text+" sağ vidanın üstünde");}
                }
                Assert.AreEqual(path==EntranceScene,R<ShiftSwitch>(c,"exit")==null);
            }
            finally
            {
                if(k9!=null){var rt=k9.targetTexture;k9.targetTexture=null;if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}Object.DestroyImmediate(k9.gameObject);}
                EditorSceneManager.CloseScene(scene,true);
            }
        }
        /// <summary>
        /// Menü (Giris) odadaki yeni modelleri kullanır: kapı (Leartes SM_Door türevleri) ve ayarlar dolabı (SM_PowerBox1 türevleri) aynı mesh ve
        /// malzemeyle. İki sahne aynı koordinatları paylaşır: menü kapısı odadaki kapının dünya pozunda, koridordan dış yüzü görünür, kanat odaya
        /// (−x) açılır ve çark koridor yüzünde de vardır (aynı mil); dolap kutusu dolap köküne göre odadakiyle aynı yerde.
        /// </summary>
        [Test]
        public void Menu_UsesTheRoomDoorAndCabinetModels()
        {
            var desk=EditorSceneManager.OpenScene(DeskScene,OpenSceneMode.Additive);var menu=EditorSceneManager.OpenScene(EntranceScene,OpenSceneMode.Additive);
            try
            {
                DoorProp dd=All<DoorProp>(desk).Single(),md=R<DoorProp>(All<EntranceController>(menu).Single(),"door");
                System.Func<Transform,string[]> Parts=t=>t.GetComponentsInChildren<MeshRenderer>(false).Where(r=>r.GetComponent<TMPro.TMP_Text>()==null).Select(r=>(r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh?r.GetComponent<MeshFilter>().sharedMesh.name:"-")+":"+string.Join(",",r.sharedMaterials.Select(m=>m.name))).OrderBy(x=>x).ToArray();
                // kapı: kasa ve kanat aynı mesh/malzeme, aynı dünya pozu
                foreach(string mesh in new[]{"DoorFrame_SM","DoorLeaf_SM","DoorWheel_SM"})
                {
                    MeshFilter a=dd.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh&&f.sharedMesh.name==mesh),b=md.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh&&f.sharedMesh.name==mesh);
                    Assert.Less(Vector3.Distance(a.transform.position,b.transform.position),.001f,mesh);Assert.Less(Quaternion.Angle(a.transform.rotation,b.transform.rotation),.05f,mesh);
                    CollectionAssert.AreEqual(a.GetComponent<MeshRenderer>().sharedMaterials,b.GetComponent<MeshRenderer>().sharedMaterials,mesh);
                }
                Assert.IsFalse(md.GetComponentsInChildren<Rev3Model>(true).Any(m=>m.ModelName=="DoorFrame"||m.ModelName=="DoorLeaf"||m.ModelName=="OuterWheel"),"eski Rev3 kapı yok");
                Assert.IsEmpty(md.GetComponentsInChildren<Collider>(true).Where(x=>x.enabled),"kapı dekor: tık saate/karta");
                Assert.AreEqual(P(dd,"openAngle").floatValue,P(md,"openAngle").floatValue);
                var wheel=R<Transform>(md,"wheel");var leaf=R<Transform>(md,"leaf");Assert.IsTrue(wheel.IsChildOf(leaf));
                var leafMesh=leaf.GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh&&f.sharedMesh.name=="DoorLeaf_SM").GetComponent<Renderer>().bounds;
                Assert.Greater(wheel.GetComponentInChildren<Renderer>().bounds.max.x,leafMesh.max.x,"çark koridor yüzünde de (koridor +x)");
                var stencil=leaf.GetComponentsInChildren<DeskLocText>(true).Single();Assert.AreEqual("SIĞINAK 07 · KYGA",P(stencil,"source").stringValue);
                Assert.Greater(stencil.transform.position.x,leafMesh.max.x,"şablon kanadın koridor yüzünde");Assert.Less(Vector3.Dot(stencil.transform.forward,Vector3.right),-.99f,"şablon koridordan okunur");
                // portal tehlike bantları kasanın pervaz oluğunda: açıklığa bakar, kapalı kanadın dış yüzünün arkasında (kasanın dışına taşmaz)
                var frame=md.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.sharedMesh&&f.sharedMesh.name=="DoorFrame_SM").GetComponent<Renderer>().bounds;
                var bands=All<MeshRenderer>(menu).Where(r=>r.sharedMaterial&&r.sharedMaterial.name=="Portal hazard").ToArray();Assert.AreEqual(2,bands.Length,"iki portal bandı");
                foreach(var b in bands)
                {
                    Assert.GreaterOrEqual(b.bounds.min.x,frame.min.x,b.name);Assert.LessOrEqual(b.bounds.max.x,leafMesh.max.x,b.name+" kapalı kanadın arkasında");
                    Assert.Greater(b.bounds.min.z,frame.min.z,b.name);Assert.Less(b.bounds.max.z,frame.max.z,b.name);
                    Assert.Greater(Vector3.Dot(b.transform.rotation*Vector3.back,new Vector3(0,0,frame.center.z-b.bounds.center.z)),0,b.name+" açıklığa bakar");
                }
                // kanat odaya açılır: serbest kenar kapalıyken eşikte, açıkken odada (x < 2,2)
                var free=leaf.InverseTransformPoint(new Vector3(leafMesh.center.x,leafMesh.center.y,leafMesh.max.z));
                md.SetOpen(true);for(int i=0;i<200;i++)md.Advance(.02f);Assert.IsTrue(md.IsOpen);
                Assert.Less(leaf.TransformPoint(free).x,1.5f,"kanat odaya açılır");Assert.AreNotEqual(0,md.WheelDegrees);
                // dolap: odadaki kutunun aynısı, dolap köküne göre aynı yerde
                SettingsCabinet dc=All<SettingsCabinet>(desk).Single(),mc=All<SettingsCabinet>(menu).Single();
                CollectionAssert.AreEquivalent(Parts(dc.transform).Where(x=>x.Contains("PowerBox1")),Parts(mc.transform).Where(x=>x.Contains("PowerBox1")));
                Assert.AreEqual(3,Parts(mc.transform).Count(x=>x.Contains("PowerBox1")),"gövde, kapak, kilit");
                // AYARLAR plakası (CabPlate modeli, KOS aşama 8) dışında eski Rev3 menü dolabının parçası görünmez
                System.Func<Transform,string[]> Own=t=>t.GetComponentsInChildren<MeshRenderer>(false).Where(r=>r.GetComponent<TMPro.TMP_Text>()==null&&!r.GetComponentsInParent<Rev3Model>(true).Any(m=>m.ModelName=="CabPlate")).Select(r=>(r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh?r.GetComponent<MeshFilter>().sharedMesh.name:"-")+":"+string.Join(",",r.sharedMaterials.Select(m=>m.name))).ToArray();
                Assert.IsFalse(Own(mc.transform).Any(x=>x.StartsWith("Body:")||x.StartsWith("Leaf:")||x.StartsWith("Latch:")),"eski Rev3 menü dolabının gövdesi görünmez");
                // İkinci tur kararı: iki sahnedeki kutunun kapağında aynı AYARLAR plakası (DeskLocText), dolap yerelinde aynı yerde
                Transform Plate(SettingsCabinet c)=>c.GetComponentsInChildren<Rev3Model>(true).Where(m=>m.ModelName=="CabPlate"&&m.transform.parent!=null&&m.transform.parent.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh&&f.sharedMesh.name=="PowerBox1Door_SM")).Select(m=>m.transform).Single();
                Transform dp=Plate(dc),mp=Plate(mc);
                foreach(var p in new[]{dp,mp})
                {
                    var label=p.GetComponentsInChildren<DeskLocText>(true).Single();Assert.AreEqual("AYARLAR",P(label,"source").stringValue,p.name);
                    Assert.IsTrue(p.gameObject.activeInHierarchy,p.name+" görünür");
                }
                Assert.Less(Vector3.Distance(dc.transform.InverseTransformPoint(dp.position),mc.transform.InverseTransformPoint(mp.position)),.0005f,"plaka iki dolapta aynı yerde");
                foreach(var f in mc.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh&&f.sharedMesh.name.Contains("PowerBox1")))
                {
                    var g=dc.GetComponentsInChildren<MeshFilter>().Single(x=>x.sharedMesh==f.sharedMesh);
                    Assert.Less(Vector3.Distance(dc.transform.InverseTransformPoint(g.transform.position),mc.transform.InverseTransformPoint(f.transform.position)),.0005f,f.sharedMesh.name);
                }
                Assert.AreEqual(((BoxCollider)dc.GetComponent<Collider>()).size,((BoxCollider)mc.GetComponent<Collider>()).size);
                Assert.AreEqual(R<GameObject>(dc,"interior").transform.localScale,R<GameObject>(mc,"interior").transform.localScale);
            }
            finally{EditorSceneManager.CloseScene(menu,true);EditorSceneManager.CloseScene(desk,true);}
        }
        [Test]
        public void Clock_Uses80DistinctOrderedAnchors_AndSeparateFreshHole()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/Giris.unity",OpenSceneMode.Additive);
            try
            {
                var ctrl=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EntranceController>(true)).Single();var c=R<TimeClock>(ctrl,"clock");var a=P(c,"punches");Assert.AreEqual(80,a.arraySize);
                var holes=Enumerable.Range(0,80).Select(i=>((GameObject)a.GetArrayElementAtIndex(i).objectReferenceValue).transform).ToArray();
                Assert.AreEqual(80,holes.Distinct().Count());for(int i=0;i<80;i++)Assert.AreEqual("H"+(i+1).ToString("00"),holes[i].parent.name);
                foreach(string f in new[]{"cardRole","cardShelter","cardFree","newCardShift","newCardFree","freshPunch","clipPart","tiltPart","plungerPart","lampPart","hourPart","minutePart"})Assert.IsNotNull(P(c,f).objectReferenceValue,f);
                Assert.IsFalse(R<Transform>(c,"blank").gameObject.activeSelf);
                foreach(string f in new[]{"clockPressPose","switchPose","instruments"})Assert.IsNotNull(P(ctrl,f).objectReferenceValue,f);
                Assert.IsFalse(R<DeskTarget>(ctrl,"doorTarget").GetComponent<Collider>().enabled,"kapı dekor, oyunu saat/kart başlatır");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        [TestCase("CabCounter_Wheel0")] [TestCase("PunchClock_Wheel0")] [TestCase("CabGauge_Body")] [TestCase("ShiftCard_Tilt")]
        public void PrintedMeshes_HaveNormalizedUV_NotMetreProjection(string name)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Corrigible/Desk/Art/Rev3/SceneMeshes/Menu/"+name+".asset");Assert.IsNotNull(mesh);
            Assert.Greater(mesh.uv.Max(p=>p.x)-mesh.uv.Min(p=>p.x),.9f);Assert.Greater(mesh.uv.Max(p=>p.y)-mesh.uv.Min(p=>p.y),.9f);
            Assert.IsFalse(mesh.uv.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)),"sıfır genişlikli izdüşüm yok");
        }

        [TestCase("İPTAL EDİLİR\nV.014")] [TestCase("TO BE CANCELLED\nS.014")]
        public void CancellationSlip_FitsBothLanguages_WithoutShrinking(string text)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/Giris.unity",OpenSceneMode.Additive);
            try
            {
                var clock=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TimeClock>(true)).Single();var t=R<TMPro.TMP_Text>(clock,"voidText");
                var preferred=t.GetPreferredValues(text,float.PositiveInfinity,float.PositiveInfinity);
                Assert.IsFalse(t.enableAutoSizing);Assert.LessOrEqual(preferred.x,t.rectTransform.rect.width);Assert.LessOrEqual(preferred.y,t.rectTransform.rect.height);
                Assert.AreEqual(1,t.transform.lossyScale.x,.001f,"fiş büyürken harf genişliği değişmez");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
