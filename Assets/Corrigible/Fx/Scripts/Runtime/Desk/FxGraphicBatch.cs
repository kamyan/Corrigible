using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>
    /// F3'ün aynı materyal/dokudaki küçük görsellerini bir Canvas mesh'inde toplar. Kaynak bileşenler bağlama,
    /// yerleşim ve hit hiyerarşisinde kalır. Dönen/nabızlı görseller bu gruba alınmaz; döngüleri yerel yürür.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxGraphicBatch : MaskableGraphic
    {
        [SerializeField] public Graphic[] sources=Array.Empty<Graphic>();
        [SerializeField] public Texture texture;
        [SerializeField] public FxClock clock;
        [SerializeField] public FxPopupView[] occlusionWindows=Array.Empty<FxPopupView>();
        int[] sourceWindows;
        Rect[] windowRects;
        bool[] windowVisible;
        readonly Rect[] cellsA=new Rect[256],cellsB=new Rect[256];
        readonly List<Vector3> positions=new List<Vector3>(2048);
        readonly List<Color32> colors=new List<Color32>(2048);
        readonly List<Vector4> uv0=new List<Vector4>(2048),uv1=new List<Vector4>(2048),uv2=new List<Vector4>(2048),uv3=new List<Vector4>(2048);
        readonly List<int> indices=new List<int>(3072);
        Matrix4x4[] transforms;
        Rect[] rectangles;
        Color[] tints;
        int[] contents;
        bool[] active,rebuild,sourceDirty;
        UnityEngine.Events.UnityAction[] dirtyCallbacks;
        float[] alphas;
        Mesh[] meshes;
        RectMask2D[][] masks;
        Rect[] clips;
        bool[] hasClip;
        readonly Vector3[] clipCorners=new Vector3[4];
        readonly UIVertex[] polygonA=new UIVertex[8],polygonB=new UIVertex[8];
        bool force,preparing;
        FxGraphicBatchTick ticker;
        public int MeshBuilds {get;private set;}
        public override Texture mainTexture=>texture!=null?texture:base.mainTexture;
        protected override void OnEnable()
        {
            base.OnEnable();sourceWindows=new int[sources.Length];windowRects=new Rect[occlusionWindows.Length];windowVisible=new bool[occlusionWindows.Length];
            for(int i=0;i<sources.Length;i++)sourceWindows[i]=Array.IndexOf(occlusionWindows,sources[i]!=null?sources[i].GetComponentInParent<FxPopupView>(true):null);
            if(sources.Length>0)texture=sources[0] is TMP_Text text?text.font.atlasTexture:sources[0].mainTexture;int n=sources.Length;transforms=new Matrix4x4[n];rectangles=new Rect[n];tints=new Color[n];contents=new int[n];active=new bool[n];rebuild=new bool[n];sourceDirty=new bool[n];dirtyCallbacks=new UnityEngine.Events.UnityAction[n];alphas=new float[n];force=true;meshes=new Mesh[n];masks=new RectMask2D[n][];clips=new Rect[n];hasClip=new bool[n];for(int i=0;i<n;i++)masks[i]=sources[i]!=null?sources[i].GetComponentsInParent<RectMask2D>(true):Array.Empty<RectMask2D>();if(n>0&&sources[0] is TMP_Text label)material=label.fontSharedMaterial;
            for(int i=0;i<n;i++){var source=sources[i];int index=i;dirtyCallbacks[i]=()=>{if(!preparing)sourceDirty[index]=true;};if(source is MaskableGraphic mask)mask.maskable=false;if(source!=null)source.RegisterDirtyVerticesCallback(dirtyCallbacks[i]);}
            ticker=GetComponent<FxGraphicBatchTick>();if(ticker!=null)ticker.batch=this;
            Canvas.willRenderCanvases+=HideSources;
        }
        protected override void OnDisable(){ReleaseMeshes();Canvas.willRenderCanvases-=HideSources;for(int i=0;i<sources.Length;i++)if(sources[i]!=null){if(dirtyCallbacks!=null)sources[i].UnregisterDirtyVerticesCallback(dirtyCallbacks[i]);sources[i].canvasRenderer.cull=false;}base.OnDisable();}
        // RectMask2D/Graphic yeniden kurulumları cull bayrağını geri açabilir.
        // Canvas kayıt geçişinden sonra kaynak çizimleri yeniden kapatılır.
        void HideSources(){foreach(var source in sources)if(source!=null&&!source.canvasRenderer.cull)source.canvasRenderer.cull=true;}
        
        public void Refresh()
        {
            bool dirty=force;bool changed=dirty;force=false;var inverse=transform.worldToLocalMatrix;
            for(int w=0;w<occlusionWindows.Length;w++)
            {
                var window=occlusionWindows[w];bool visible=window!=null&&window.gameObject.activeInHierarchy&&window.group.alpha>=.99f;Rect bounds=default;
                if(visible){window.rect.GetWorldCorners(clipCorners);var lo=inverse.MultiplyPoint3x4(clipCorners[0]);var hi=inverse.MultiplyPoint3x4(clipCorners[2]);bounds=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);}
                changed|=visible!=windowVisible[w]||(visible&&bounds!=windowRects[w]);windowRects[w]=bounds;windowVisible[w]=visible;
            }
            for(int i=0;i<sources.Length;i++)
            {
                var g=sources[i];if(g==null)continue;bool a=g.isActiveAndEnabled;var m=inverse*g.transform.localToWorldMatrix;var rect=g.rectTransform.rect;int hash=g is TMP_Text t?(t.text??"").GetHashCode():g is Image image&&image.sprite!=null?image.sprite.GetInstanceID():0;float alpha=g.canvasRenderer.GetInheritedAlpha();
                bool clipped=false;Rect clip=default;
                foreach(var mask in masks[i])
                {
                    if(mask==null||!mask.isActiveAndEnabled)continue;mask.rectTransform.GetWorldCorners(clipCorners);
                    var lo=inverse.MultiplyPoint3x4(clipCorners[0]);var hi=inverse.MultiplyPoint3x4(clipCorners[2]);
                    var next=Rect.MinMaxRect(Mathf.Min(lo.x,hi.x),Mathf.Min(lo.y,hi.y),Mathf.Max(lo.x,hi.x),Mathf.Max(lo.y,hi.y));
                    clip=clipped?Rect.MinMaxRect(Mathf.Max(clip.xMin,next.xMin),Mathf.Max(clip.yMin,next.yMin),Mathf.Min(clip.xMax,next.xMax),Mathf.Min(clip.yMax,next.yMax)):next;clipped=true;
                }
                changed|=clipped!=hasClip[i]||(clipped&&clip!=clips[i]);hasClip[i]=clipped;clips[i]=clip;
                rebuild[i]=dirty||sourceDirty[i]||meshes[i]==null||a!=active[i]||rect!=rectangles[i]||g.color!=tints[i]||hash!=contents[i];
                changed|=sourceDirty[i];sourceDirty[i]=false;
                changed|=a!=active[i]||m!=transforms[i]||rect!=rectangles[i]||g.color!=tints[i]||hash!=contents[i]||alpha!=alphas[i];active[i]=a;transforms[i]=m;rectangles[i]=rect;tints[i]=g.color;contents[i]=hash;alphas[i]=alpha;g.canvasRenderer.cull=true;
            }
            if(changed)
            {
                // Kaynak mesh'ler Canvas'ın toplu çizim geçişinden önce hazırlanır.
                // OnPopulateMesh içinde Rebuild çağırmak uGUI'nin yeniden giriş korumasına takılır.
                preparing=true;
                for(int i=0;i<sources.Length;i++)
                {
                    var g=sources[i];if(g==null||!active[i]||!rebuild[i])continue;
                    g.canvasRenderer.cull=false;
                    if(g is TMP_Text text)text.ForceMeshUpdate(true);
                    else {g.SetVerticesDirty();g.Rebuild(CanvasUpdate.PreRender);}
                    if(g is TMP_Text label)meshes[i]=label.mesh;else CopyWorkerMesh(i);g.canvasRenderer.cull=true;
                }
                preparing=false;SetVerticesDirty();
            }
        }
        // CanvasRenderer.GetMesh her çağrıda yeni bir Mesh kopyası üretir. uGUI'nin ortak
        // çalışma mesh'ini kaynak Rebuild'inden hemen sonra kendi yeniden kullanılan tamponumuza alırız.
        void CopyWorkerMesh(int index)
        {
            var mesh=meshes[index];if(mesh==null){mesh=new Mesh{name="FX Batch Source",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();meshes[index]=mesh;}
            workerMesh.GetVertices(positions);workerMesh.GetColors(colors);workerMesh.GetUVs(0,uv0);workerMesh.GetUVs(1,uv1);workerMesh.GetUVs(2,uv2);workerMesh.GetUVs(3,uv3);workerMesh.GetTriangles(indices,0);
            mesh.Clear();mesh.SetVertices(positions);mesh.SetColors(colors);mesh.SetUVs(0,uv0);mesh.SetUVs(1,uv1);mesh.SetUVs(2,uv2);mesh.SetUVs(3,uv3);mesh.SetTriangles(indices,0);
        }
        void ReleaseMeshes()
        {
            if(meshes==null)return;for(int i=0;i<meshes.Length;i++)if(meshes[i]!=null&&!(sources[i] is TMP_Text)){if(Application.isPlaying)Destroy(meshes[i]);else DestroyImmediate(meshes[i]);meshes[i]=null;}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;vh.Clear();if(transforms==null||transforms.Length!=sources.Length)return;
            for(int i=0;i<sources.Length;i++)
            {
                var g=sources[i];if(g==null||!active[i])continue;
                var mesh=meshes[i];if(mesh==null||mesh.vertexCount==0)continue;
                positions.Clear();colors.Clear();uv0.Clear();uv1.Clear();uv2.Clear();uv3.Clear();indices.Clear();mesh.GetVertices(positions);mesh.GetColors(colors);mesh.GetUVs(0,uv0);mesh.GetUVs(1,uv1);mesh.GetUVs(2,uv2);mesh.GetUVs(3,uv3);mesh.GetTriangles(indices,0);int first=vh.currentVertCount;
                bool occluded=false;for(int w=sourceWindows[i]+1;w<windowVisible.Length;w++)if(windowVisible[w]){occluded=true;break;}
                if(hasClip[i]||occluded)
                {
                    float xmin=float.MaxValue,ymin=float.MaxValue,xmax=float.MinValue,ymax=float.MinValue;
                    for(int j=0;j<positions.Count;j++){var p=transforms[i].MultiplyPoint3x4(positions[j]);xmin=Mathf.Min(xmin,p.x);ymin=Mathf.Min(ymin,p.y);xmax=Mathf.Max(xmax,p.x);ymax=Mathf.Max(ymax,p.y);}
                    Rect visible=Rect.MinMaxRect(xmin,ymin,xmax,ymax);if(hasClip[i])visible=Intersection(visible,clips[i]);if(visible.width<=0||visible.height<=0)continue;
                    var cells=cellsA;var output=cellsB;int count=1;cells[0]=visible;
                    for(int w=sourceWindows[i]+1;w<windowVisible.Length&&count>0;w++)
                    {
                        if(!windowVisible[w])continue;int length=0;for(int n=0;n<count;n++)Subtract(cells[n],windowRects[w],output,ref length);var swap=cells;cells=output;output=swap;count=length;
                    }
                    for(int j=0;j<indices.Count;j+=3)for(int n=0;n<count;n++)AddClipped(vh,Vertex(indices[j],i),Vertex(indices[j+1],i),Vertex(indices[j+2],i),cells[n]);
                }
                else
                {
                    for(int j=0;j<positions.Count;j++)vh.AddVert(Vertex(j,i));
                    for(int j=0;j<indices.Count;j+=3)vh.AddTriangle(first+indices[j],first+indices[j+1],first+indices[j+2]);
                }
            }
        }
        UIVertex Vertex(int j,int source)
        {
            var v=UIVertex.simpleVert;v.position=transforms[source].MultiplyPoint3x4(positions[j]);v.color=j<colors.Count?colors[j]:(Color32)sources[source].color;var c=v.color;c.a=(byte)Mathf.RoundToInt(c.a*alphas[source]);v.color=c;
            v.uv0=j<uv0.Count?uv0[j]:Vector4.zero;v.uv1=j<uv1.Count?uv1[j]:Vector4.zero;v.uv2=j<uv2.Count?uv2[j]:Vector4.zero;v.uv3=j<uv3.Count?uv3[j]:Vector4.zero;return v;
        }
        // Farklı ScrollRect'lerdeki geometri CPU'da kırpılır. Böylece metin materyalleri ortak kalır;
        // dikdörtgen değişmediğinde yeniden iş veya tahsis yapılmaz.
        void AddClipped(VertexHelper vh,UIVertex a,UIVertex b,UIVertex c,Rect rect)
        {
            var input=polygonA;var output=polygonB;input[0]=a;input[1]=b;input[2]=c;int count=3;
            for(int edge=0;edge<4&&count>0;edge++)
            {
                int length=0;var previous=input[count-1];float pd=Distance(previous.position,edge,rect);
                for(int n=0;n<count;n++)
                {
                    var current=input[n];float cd=Distance(current.position,edge,rect);
                    if((pd>=0)!=(cd>=0))output[length++]=Lerp(previous,current,pd/(pd-cd));
                    if(cd>=0)output[length++]=current;previous=current;pd=cd;
                }
                var swap=input;input=output;output=swap;count=length;
            }
            if(count<3)return;int first=vh.currentVertCount;for(int n=0;n<count;n++)vh.AddVert(input[n]);for(int n=1;n<count-1;n++)vh.AddTriangle(first,first+n,first+n+1);
        }
        static Rect Intersection(Rect a,Rect b)=>Rect.MinMaxRect(Mathf.Max(a.xMin,b.xMin),Mathf.Max(a.yMin,b.yMin),Mathf.Min(a.xMax,b.xMax),Mathf.Min(a.yMax,b.yMax));
        static void Subtract(Rect source,Rect cover,Rect[] output,ref int count)
        {
            if(!source.Overlaps(cover)){output[count++]=source;return;}var hit=Intersection(source,cover);
            if(hit.xMin>source.xMin)output[count++]=Rect.MinMaxRect(source.xMin,source.yMin,hit.xMin,source.yMax);
            if(hit.xMax<source.xMax)output[count++]=Rect.MinMaxRect(hit.xMax,source.yMin,source.xMax,source.yMax);
            if(hit.yMin>source.yMin)output[count++]=Rect.MinMaxRect(hit.xMin,source.yMin,hit.xMax,hit.yMin);
            if(hit.yMax<source.yMax)output[count++]=Rect.MinMaxRect(hit.xMin,hit.yMax,hit.xMax,source.yMax);
        }
        static float Distance(Vector3 p,int edge,Rect r)=>edge==0?p.x-r.xMin:edge==1?r.xMax-p.x:edge==2?p.y-r.yMin:r.yMax-p.y;
        static UIVertex Lerp(UIVertex a,UIVertex b,float t)
        {
            var v=a;v.position=Vector3.LerpUnclamped(a.position,b.position,t);v.color=Color32.Lerp(a.color,b.color,t);v.uv0=Vector4.LerpUnclamped(a.uv0,b.uv0,t);v.uv1=Vector4.LerpUnclamped(a.uv1,b.uv1,t);v.uv2=Vector4.LerpUnclamped(a.uv2,b.uv2,t);v.uv3=Vector4.LerpUnclamped(a.uv3,b.uv3,t);return v;
        }

    }
}
