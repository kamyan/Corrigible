using System;
using System.Collections.Generic;
namespace Corrigible.Fx.Orb
{
    /// <summary>JS yüz dolaşımını ve ilk görülen kenar sırasını aynen korur.</summary>
    public static class OrbIcosphere
    {
        public static readonly OrbVector[] Vertices;
        public static readonly int[][] Faces,Edges;
        static OrbIcosphere()
        {
            double t=(1+Math.Sqrt(5))/2;var v=new List<OrbVector>{new OrbVector(-1,t,0),new OrbVector(1,t,0),new OrbVector(-1,-t,0),new OrbVector(1,-t,0),new OrbVector(0,-1,t),new OrbVector(0,1,t),new OrbVector(0,-1,-t),new OrbVector(0,1,-t),new OrbVector(t,0,-1),new OrbVector(t,0,1),new OrbVector(-t,0,-1),new OrbVector(-t,0,1)};
            for(int i=0;i<v.Count;i++)v[i]=v[i].Normalized;
            int[][] f={new[]{0,11,5},new[]{0,5,1},new[]{0,1,7},new[]{0,7,10},new[]{0,10,11},new[]{1,5,9},new[]{5,11,4},new[]{11,10,2},new[]{10,7,6},new[]{7,1,8},new[]{3,9,4},new[]{3,4,2},new[]{3,2,6},new[]{3,6,8},new[]{3,8,9},new[]{4,9,5},new[]{2,4,11},new[]{6,2,10},new[]{8,6,7},new[]{9,8,1}};
            var cache=new Dictionary<int,int>();var nf=new List<int[]>();
            int Mid(int a,int b){int key=Math.Min(a,b)*1024+Math.Max(a,b);if(cache.TryGetValue(key,out int index))return index;var x=v[a];var y=v[b];v.Add(new OrbVector((x.X+y.X)/2,(x.Y+y.Y)/2,(x.Z+y.Z)/2).Normalized);index=v.Count-1;cache[key]=index;return index;}
            foreach(var tr in f){int a=Mid(tr[0],tr[1]),b=Mid(tr[1],tr[2]),c=Mid(tr[2],tr[0]);nf.Add(new[]{tr[0],a,c});nf.Add(new[]{tr[1],b,a});nf.Add(new[]{tr[2],c,b});nf.Add(new[]{a,b,c});}
            var seen=new HashSet<int>();var edges=new List<int[]>();foreach(var tr in nf)for(int i=0;i<3;i++){int a=tr[i],b=tr[(i+1)%3],key=Math.Min(a,b)*1024+Math.Max(a,b);if(seen.Add(key))edges.Add(new[]{a,b});}
            Vertices=v.ToArray();Faces=nf.ToArray();Edges=edges.ToArray();
        }
    }
}
