using System;
using System.Linq;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Corrigible.Fx
{
    /// <summary>F5 sunum havuzu. Sahnedeki hazır geometriyi ortak maket verisiyle bağlar; oyun komutu veya gizli sızma okumaz.</summary>
    public sealed class FxRaidBoard : FxTimedBehaviour
    {
        [Serializable] public sealed class CellSlot {public int x,y;public GameObject normal,backup,civil,rubble;public RectTransform health;public Image healthBorder;public Image[] pips;public Vector3 anchor;}
        [Serializable] public sealed class UnitSlot {public string kind;public Transform root,body;public float height;public RectTransform health,flag;public Image[] pips;public Image healthBorder;public TMP_Text order,status;public Image flagFill,flagBorder,strike;public GameObject question;public CanvasGroup flagGroup;public FxPulseGraphic flagPulse;[NonSerialized]public Vector3 anchor,flagAnchor;public Quaternion rotation;[NonSerialized]public int id;}
        [Serializable] public sealed class TagSlot {public RectTransform root;public TMP_Text text;public Image border;[NonSerialized]public Vector2 position,offset;[NonSerialized]public Vector3 anchor;public bool floating;}
        [SerializeField] public TagSlot[] tags;
        [SerializeField] public TMP_FontAsset stampFont,popFont;
        [SerializeField] public FxTheme theme;
        [SerializeField] public Camera boardCamera;
        [SerializeField] public Canvas labelsCanvas,backgroundCanvas;
        [SerializeField] public TextAsset states;
        [SerializeField] public string preset="B1";
        [SerializeField] public bool sandboxHotkeys=true;
        [SerializeField] public CellSlot[] cells;
        [SerializeField] public UnitSlot[] units;
        [SerializeField] public FxRaidMarks marks;
        [SerializeField] public FxRaidEdges edgeBatch;
        [SerializeField] public FxLineGraphic[] flows,flowGlows;
        [SerializeField] public UIPolygonGraphic scanMask,shadow;
        [SerializeField] public RectTransform scan,cone;
        [SerializeField] public Graphic coneImage;
        [SerializeField] public OrbView nodeOrb;
        [SerializeField] public FxLineGraphic tether,tetherGlow;
        [SerializeField] public CanvasGroup labelsGroup;
        public List<Baskin.IntentView> DisplayIntents {get;private set;}=new List<Baskin.IntentView>();
        Matrix4x4 previousMatrix;int previousW,previousH;bool bound;double appeared;string shown;
        static readonly string[] Presets={"B1","B2","B3","B4","B5","B6","B7a","B7b","B8"};
        Vector2 nodeAnchor;readonly Vector2[] corners=new Vector2[4];
        public MockRaidBoardDataSource.Snapshot Snapshot {get;private set;}
        public int ProjectionRefreshes {get;private set;}
        public float Flicker {get;private set;}=1;
        public Color Ink(string key)=>theme.Color(key);
        public float Number(string key,float fallback)=>theme.TryNumber(key,out var value)?value:fallback;
        protected override void OnEnable(){base.OnEnable();DeskLoc.Instance.Changed+=LanguageChanged;}
        protected override void OnDisable(){DeskLoc.Instance.Changed-=LanguageChanged;base.OnDisable();}
        void LanguageChanged(){if(bound&&sandboxHotkeys)LoadPreset(preset);}
        void Start(){if(!bound&&sandboxHotkeys)LoadPreset(preset);}
        public void LoadPreset(string id)
        {
            var data=JObject.Parse(states.text);var snapshot=MockRaidBoardDataSource.Parse(data,id);Snapshot=snapshot;
            DisplayIntents=Baskin.Intents(snapshot.State);
            var rawUnits=((JArray)data["states"][id]["units"]).Concat((JArray)data["states"][id]["robots"]).ToArray();
            for(int i=0;i<snapshot.State.Units.Count;i++)
            {
                var unit=snapshot.State.Units[i];var raw=rawUnits[i]["intent"];var intent=DisplayIntents.Find(x=>x.Unit==unit.Id);if(intent==null)continue;
                if((bool?)raw?["nomark"]==true){DisplayIntents.Remove(intent);continue;}
                if((string)raw?["cert"]=="scanned"){intent.Uncertain=false;intent.IsScanned=true;}
                if((string)raw?["cert"]=="maybe")intent.Uncertain=true;
                if(snapshot.IntentStates.TryGetValue(unit.Id,out var phase)&&phase=="now")snapshot.IntentStates[unit.Id]="current";
            }
            snapshot.Marks.Danger.Clear();snapshot.Marks.Heal.Clear();
            foreach(var intent in DisplayIntents)if(intent.HasTarget&&!intent.TargetNone){int k=intent.TargetY*8+intent.TargetX;if(intent.Kind=="repair")snapshot.Marks.Heal.Add(k);else snapshot.Marks.Danger[k]=!intent.Uncertain;}
            Present(snapshot,id,true);
        }
        public Baskin.PreviewResult AbilityPreview {get;private set;}
        public void BindState(Baskin.BState state,RaidViewModel vm,IReadOnlyDictionary<int,string> intents,JObject raw=null,Baskin.PreviewResult preview=null)
        {
            var snapshot=new MockRaidBoardDataSource.Snapshot{State=state,Marks=new RaidBoard3D.Marks()};var m=snapshot.Marks;
            AbilityPreview=preview;DisplayIntents=Baskin.Intents(state);
            if(raw!=null){var rows=(raw["units"] as JArray??new JArray()).Concat(raw["robots"] as JArray??new JArray()).ToArray();for(int i=0;i<state.Units.Count&&i<rows.Length;i++){var item=DisplayIntents.Find(x=>x.Unit==state.Units[i].Id);if(item==null)continue;var r=rows[i]["intent"];if((bool?)r?["nomark"]==true){DisplayIntents.Remove(item);continue;}if((string)r?["cert"]=="scanned"){item.Uncertain=false;item.IsScanned=true;}if((string)r?["cert"]=="maybe")item.Uncertain=true;}}
            foreach(var pair in intents)snapshot.IntentStates[pair.Key]=pair.Value=="now"?"current":pair.Value=="cancel"?"cancelled":pair.Value;
            foreach(var u in state.Units){if(u.Acted)m.Spent.Add(u.Id);if(u.Side=='p'&&vm.UnitCard?.Letter==Baskin.UnitTypes[u.Type].Short[0])m.Selected=u.Id;}
            foreach(var p in vm.Moves)m.Moves.Add(p.y*8+p.x);foreach(var p in vm.AbilityTargets)m.Targets.Add(p.y*8+p.x);foreach(var p in state.Spawns)m.Spawns.Add(p.Y*8+p.X);
            foreach(var cell in cells)if(cell.rubble!=null&&state.Cells[cell.y,cell.x].O==null)m.Rubble.Add(cell.y*8+cell.x);
            foreach(var intent in DisplayIntents)if(intent.HasTarget&&!intent.TargetNone){int k=intent.TargetY*8+intent.TargetX;if(intent.Kind=="repair")m.Heal.Add(k);else m.Danger[k]=!intent.Uncertain;}
            if(preview!=null){foreach(var fx in preview.Fx){if(fx.K=="dmg")m.Danger[fx.Y*8+fx.X]=true;if(fx.K=="die")snapshot.IntentStates[fx.Unit]="cancelled";}}
            string id=vm.Finished?"B7a":vm.LastTurn?"B6":vm.Phase=="enemy"?"B5":"B1";
            Present(snapshot,id,!bound);
            foreach(var slot in units)if(slot.root.gameObject.activeSelf){var u=state.Units.First(x=>x.Id==slot.id);if(u.Side=='p'&&!u.Dead)slot.status.gameObject.SetActive(false);}
            foreach(var t in tags)t.root.gameObject.SetActive(false);int index=0;
            if(preview!=null){foreach(var group in preview.Fx.Where(x=>x.K=="dmg").GroupBy(x=>(x.X,x.Y)))if(index<tags.Length)Tag(index++,Loc.F("−{0}",group.Sum(x=>x.N)),RaidBoard3D.W(group.Key.X,group.Key.Y),Vector2.up*78,Ink("rd2"),false);foreach(var fx in preview.Fx.Where(x=>x.K=="armor"||x.K=="break"&&x.Obj=="cool"))if(index<tags.Length)Tag(index++,fx.K=="armor"?Loc.T("ZIRH"):Loc.T("PATLAR"),RaidBoard3D.W(fx.X,fx.Y),Vector2.up*46,Ink("rd2"),false);if(preview.Won&&index<tags.Length)Tag(index++,Loc.T("HEDEF KIRILIR"),RaidBoard3D.W(vm.Spot.x,vm.Spot.y),Vector2.up*104,Ink("mt"),false);}
        }
        void Present(MockRaidBoardDataSource.Snapshot snapshot,string id,bool entrance)
        {
            Snapshot=snapshot;
            preset=id;shown=id;bound=true;if(entrance)appeared=clock!=null?clock.Now:0;
            foreach(var cell in cells)
            {
                var o=snapshot.State.Cells[cell.y,cell.x].O;bool rubble=snapshot.Marks.Rubble.Contains(cell.y*8+cell.x);
                if(cell.normal!=null)cell.normal.SetActive(o!=null);if(cell.rubble!=null)cell.rubble.SetActive(rubble);
                if(cell.backup!=null)cell.backup.SetActive(o!=null&&o.K=="backup");if(cell.civil!=null)cell.civil.SetActive(o!=null&&o.K=="civ");
                if(cell.health!=null){cell.health.gameObject.SetActive(o!=null&&o.K!="civ"&&o.K!="cool");if(o!=null)Pips(cell.pips,cell.healthBorder,o.Hp,o.Max,Ink("rd"),false);}
            }
            foreach(var slot in units)slot.root.gameObject.SetActive(false);
            foreach(var u in snapshot.State.Units)
            {
                var slot=units.FirstOrDefault(x=>x.kind==u.Type&&!x.root.gameObject.activeSelf);if(slot==null)continue;slot.root.gameObject.SetActive(true);slot.root.position=RaidBoard3D.W(u.X,u.Y);slot.id=u.Id;
                slot.body.localRotation=slot.rotation*(u.Dead?Quaternion.Euler(85,0,0):u.Side=='p'&&u.Moved?Quaternion.Euler(0,180,0):Quaternion.identity);
                slot.health.gameObject.SetActive(true);Pips(slot.pips,slot.healthBorder,u.Dead?0:u.Hp,u.Max,u.Side=='p'?Ink("mt"):Ink("rd"),u.Moved);
                slot.anchor=u.Side=='p'&&!u.Dead?slot.root.position+Vector3.up*(slot.height+.12f):slot.root.position+Vector3.up*.18f-boardCamera.transform.up*.26f;
                slot.flag.gameObject.SetActive(u.Side=='e'&&u.Intent!=null&&!u.Dead);slot.order.text=u.Order.ToString(System.Globalization.CultureInfo.InvariantCulture);
                slot.flagAnchor=slot.root.position+Vector3.up*(slot.height+.35f)+boardCamera.transform.right*.26f;
                string intentState=snapshot.IntentStates.TryGetValue(u.Id,out var state)?state:"pending";slot.flagFill.color=Ink(intentState=="current"?"rd":"raid.flagFill");slot.order.color=Ink(intentState=="current"?"raid.flagInk":"raid.flagText");
                slot.flagBorder.color=Ink(DisplayIntents.Exists(x=>x.Unit==u.Id&&x.Uncertain)?"am":intentState=="current"?"am2":"rd");
                bool mayLie=DisplayIntents.Exists(x=>x.Unit==u.Id&&x.Uncertain);slot.question.SetActive(mayLie);slot.strike.gameObject.SetActive(intentState=="done"||intentState=="cancelled");slot.strike.rectTransform.localRotation=Quaternion.Euler(0,0,intentState=="cancelled"?-28:0);
                slot.flag.localScale=Vector3.one*(intentState=="current"?1.14f:1);
                slot.status.gameObject.SetActive(u.Dead||u.Moved||intentState=="uncertain"||intentState=="done");slot.status.text=u.Dead?Loc.T("DÜŞTÜ"):u.Moved?Loc.T("TUR BİTTİ"):intentState=="uncertain"?"?":Loc.T("TAMAM");slot.status.color=Ink(u.Dead?"rd":"am2");
            }
            foreach(var slot in units)if(!slot.root.gameObject.activeSelf){slot.health.gameObject.SetActive(false);slot.flag.gameObject.SetActive(false);slot.status.gameObject.SetActive(false);}
            nodeOrb.level=id=="B5"?64:id=="B6"||id.StartsWith("B7",StringComparison.Ordinal)?86:58;if(nodeOrb.Simulation==null)nodeOrb.ResetModel();RefreshProjection(true);
            foreach(var tag in tags)tag.root.gameObject.SetActive(false);
            if(id=="B3"){Tag(0,Loc.T("ZIRH"),cells.First(x=>x.x==6&&x.y==3).anchor,new Vector2(0,28),Ink("am2"),false);Tag(1,Loc.T("ÇARPIŞMA"),RaidBoard3D.W(6,5),Vector2.up*35,Ink("rd"),false);}
            if(id=="B5")Tag(0,"−1",RaidBoard3D.W(3,3),Vector2.up*90,Ink("rd2"),true);
            if(id=="B6")Tag(0,Loc.T("HEDEF KIRILIR"),cells.First(x=>x.x==6&&x.y==3).anchor,Vector2.up*32,Ink("mt"),false);
            if(edgeBatch!=null)edgeBatch.Refresh();
        }
        public void ShowEffect(Baskin.Fx fx){if(fx!=null&&fx.K=="dmg")Tag(tags.Length-1,Loc.F("−{0}",fx.N),RaidBoard3D.W(fx.X,fx.Y),Vector2.up*90,Ink("rd2"),true);}
        void Tag(int index,string text,Vector3 anchor,Vector2 offset,Color color,bool floating){var t=tags[index];t.root.gameObject.SetActive(true);t.text.font=floating?popFont:stampFont;t.text.fontSize=floating?16:13;t.text.characterSpacing=floating?0:16;t.text.text=text;t.text.color=color;t.border.color=color;t.anchor=anchor;t.offset=offset;t.position=Project(anchor)+offset;t.root.anchoredPosition=t.position;t.floating=floating;t.root.sizeDelta=new Vector2(Mathf.Max(40,t.text.preferredWidth+16),floating?22:24);}
        static void Pips(Image[] pips,Image border,int hp,int max,Color color,bool spent){var rect=(RectTransform)pips[0].transform.parent;color.a=.75f;border.color=color;color.a=1;rect.sizeDelta=new Vector2(8+max*10,17);
            for(int i=0;i<pips.Length;i++){pips[i].gameObject.SetActive(i<max);pips[i].rectTransform.anchoredPosition=new Vector2(-rect.sizeDelta.x*.5f+8+i*10,0);var c=color;c.a=(i<hp?1:.16f)*(spent?.55f:1);pips[i].color=c;}}
        public float IntentAlpha(int id)=>Snapshot.IntentStates.TryGetValue(id,out var state)&&state=="done"?.35f:.95f;
        public Vector2 Project(Vector3 world)
        {
            var screen=boardCamera.WorldToScreenPoint(world);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)labelsCanvas.transform,screen,boardCamera,out var local);return local;
        }
        public static Vector3 Corner(float u,float v,float h=0)=>new Vector3(u-4,h,4-v);
        public void RefreshProjection(bool force=false)
        {
            float scale=Mathf.Min(boardCamera.pixelWidth/1920f,boardCamera.pixelHeight/1080f);boardCamera.orthographic=true;boardCamera.orthographicSize=boardCamera.pixelHeight/Mathf.Max(.001f,scale)/(2*132.5825f);boardCamera.transform.rotation=Quaternion.Euler(35,45,0);boardCamera.transform.position=-boardCamera.transform.forward*20-boardCamera.transform.up*(12/132.5825f);
            var matrix=boardCamera.projectionMatrix*boardCamera.worldToCameraMatrix;if(!force&&matrix==previousMatrix&&previousW==boardCamera.pixelWidth&&previousH==boardCamera.pixelHeight)return;
            previousMatrix=matrix;previousW=boardCamera.pixelWidth;previousH=boardCamera.pixelHeight;ProjectionRefreshes++;
            corners[0]=Project(Corner(-.3f,-.3f));corners[1]=Project(Corner(8.3f,-.3f));corners[2]=Project(Corner(8.3f,8.3f));corners[3]=Project(Corner(-.3f,8.3f));scanMask.SetConvex(corners);
            for(int i=0;i<4;i++){float u=i==0||i==3?-.3f:8.3f,v=i<2?-.3f:8.3f;corners[i]=Project(Corner(u,v,-.25f))+new Vector2(15,-18);}shadow.SetConvex(corners);
            cone.anchoredPosition=Project(Vector3.zero);
            if(Snapshot==null)return;foreach(var tag in tags)if(tag.root.gameObject.activeSelf)tag.position=Project(tag.anchor)+tag.offset;
            foreach(var cell in cells)if(cell.health!=null)cell.health.anchoredPosition=Project(cell.anchor);
            foreach(var slot in units)if(slot.root.gameObject.activeSelf){slot.health.anchoredPosition=Project(slot.anchor);slot.flag.anchoredPosition=Project(slot.flagAnchor);slot.status.rectTransform.anchoredPosition=Project(slot.anchor)+new Vector2(0,-29);}
            // Hücre etiketi tepeden +0,12 dünyada; kürenin merkezi dolabın tam tepesinden 64 piksel yukarıdadır.
            var top=cells.First(x=>x.x==6&&x.y==3).anchor-Vector3.up*.12f;nodeAnchor=Project(top)+Vector2.up*64;
            var nodeCell=Snapshot.State.Cells[3,6];nodeOrb.gameObject.SetActive(nodeCell.O!=null);
            var intents=DisplayIntents;int index=0;
            foreach(var intent in intents)
            {
                if(!intent.HasTarget||intent.TargetNone||index>=flows.Length)continue;
                var a=Project(RaidBoard3D.W(intent.FromX,intent.FromY)+Vector3.up*.08f);var b=Project(RaidBoard3D.W(intent.TargetX,intent.TargetY)+Vector3.up*.08f);var d=(b-a).normalized;a+=d*17;b-=d*8;
                float alpha=IntentAlpha(intent.Unit);bool current=Snapshot.IntentStates.TryGetValue(intent.Unit,out var value)&&value=="current";
                Setup(flows[index],a,b,Ink(current?"am2":intent.Uncertain?"am":"raid.flow"),alpha,intent.Uncertain?new Vector2(10,6):new Vector2(3,26),current?9:2.2f);
                Setup(flowGlows[index],a,b,Ink("rd"),.4f*alpha,new Vector2(10000,0),3);index++;
            }
            for(int i=index;i<flows.Length;i++){flows[i].gameObject.SetActive(false);flowGlows[i].gameObject.SetActive(false);}marks.SetVerticesDirty();
        }
        static void Setup(FxLineGraphic line,Vector2 a,Vector2 b,Color color,float alpha,Vector2 dash,float width){line.gameObject.SetActive(true);line.points[0]=a;line.points[1]=b;color.a=alpha;line.color=color;line.dash=dash;line.width=width;line.speed=28.6f;line.SetVerticesDirty();}
        public static float BoardFlicker(double time,bool reduced){if(reduced||time<1)return 1;float p=Mathf.Repeat((float)(time-1)/7,1);float a=Mathf.Max(0,1-Mathf.Abs(p-.48f)/.012f)*.14f;float b=Mathf.Max(0,1-Mathf.Abs(p-.84f)/.015f)*.10f;return 1-a-b;}
        public override void Tick(FxClock source)
        {
            if(Application.isPlaying&&sandboxHotkeys){if(shown!=preset)LoadPreset(preset);var keys=Keyboard.current;if(keys!=null)for(int i=0;i<Presets.Length;i++)if(keys[(Key)((int)Key.F1+i)].wasPressedThisFrame)LoadPreset(Presets[i]);}
            RefreshProjection();foreach(var tag in tags)if(tag.root.gameObject.activeSelf)tag.root.anchoredPosition=tag.position+Vector2.up*(tag.floating&&!source.Static?Mathf.Sin((float)source.Now*Mathf.PI*2/1.6f)*3:0);Flicker=BoardFlicker(source.Now-appeared,source.Static);Shader.SetGlobalFloat("_FxBoardFlicker",Flicker);float alpha=source.Static?1:Mathf.Clamp01((float)(source.Now-appeared));Shader.SetGlobalFloat("_FxBoardAlpha",alpha);if(labelsGroup.alpha!=alpha)labelsGroup.alpha=alpha;
            bool scanVisible=!source.Static;if(scan.gameObject.activeSelf!=scanVisible)scan.gameObject.SetActive(scanVisible);
            float scanPhase=source.Phase(6.5f,0,FxPhase.Sweep);scan.anchoredPosition=new Vector2(0,Mathf.Lerp(660,-560,scanPhase));
            var c=coneImage.color;c.a=source.Static?.925f:.925f+.075f*Mathf.Sin((float)source.Now*Mathf.PI*.5f);coneImage.color=c;
            float floating=source.Static?0:Mathf.Sin((float)source.Now*Mathf.PI*2/3.2f)*5;nodeOrb.image.rectTransform.anchoredPosition=nodeAnchor+Vector2.up*floating;
            var nodeTop=nodeAnchor-Vector2.up*64;var bottom=nodeTop+Vector2.up*22;float length=64+floating-20-22; // Bağın uçları dolap ve küre parıltısından içeride tutulur.
            tether.rectTransform.anchoredPosition=bottom;tetherGlow.rectTransform.anchoredPosition=bottom;tether.rectTransform.localScale=Vector3.one;tetherGlow.rectTransform.localScale=Vector3.one;
            if(tether.points[1].y!=length){tether.points[1]=Vector2.up*length;tetherGlow.points[1]=Vector2.up*length;tether.SetVerticesDirty();tetherGlow.SetVerticesDirty();}
            tether.gameObject.SetActive(nodeOrb.gameObject.activeSelf);tetherGlow.gameObject.SetActive(nodeOrb.gameObject.activeSelf);
            if(Snapshot!=null)foreach(var slot in units)if(slot.root.gameObject.activeSelf&&slot.flag.gameObject.activeSelf){bool current=Snapshot.IntentStates.TryGetValue(slot.id,out var state)&&state=="current";float opacity=state=="done"?.45f:1;if(slot.flagGroup.alpha!=opacity)slot.flagGroup.alpha=opacity;if(slot.flagPulse!=null)slot.flagPulse.SetPulse(current);else if(current&&!source.Static)slot.flagGroup.alpha=.82f+.18f*Mathf.Sin((float)source.Now*Mathf.PI*2);}
        }
        void OnDestroy(){Shader.SetGlobalFloat("_FxBoardAlpha",1);Shader.SetGlobalFloat("_FxBoardFlicker",1);}
    }
}
