using System;
using System.Runtime.CompilerServices;
namespace Corrigible.Fx.Orb
{
    public enum OrbKind { Normal, Unknown, Quarantine, Core }
    public struct OrbVector
    {
        public double X,Y,Z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbVector(double x,double y,double z=0){X=x;Y=y;Z=z;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbVector operator +(OrbVector a,OrbVector b)=>new OrbVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbVector operator -(OrbVector a,OrbVector b)=>new OrbVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbVector operator *(OrbVector a,double b)=>new OrbVector(a.X*b,a.Y*b,a.Z*b);
        public OrbVector Normalized {[MethodImpl(MethodImplOptions.AggressiveInlining)] get{double l=Math.Sqrt(X*X+Y*Y+Z*Z);if(l==0)l=1;return new OrbVector(X/l,Y/l,Z/l);}}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbVector Cross(OrbVector a,OrbVector b)=>new OrbVector(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    }
    /// <summary>Oyun RNG'sinden tamamen ayrı, kanonik JS mulberry32 aritmetiği.</summary>
    public struct OrbRng
    {
        public uint State;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbRng(uint seed){State=seed;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Next(){unchecked{State+=0x6D2B79F5;uint t=(State^(State>>15))*(1|State);t=(t+(t^(t>>7))*(61|t))^t;return (t^(t>>14))/4294967296.0;}}
    }
    public static class OrbMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double x,double lo=0,double hi=1)=>Math.Max(lo,Math.Min(hi,x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Round(double x)=>(int)Math.Floor(x+.5);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a,double b,double t)=>a+(b-a)*t;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Smooth(double a,double b,double x){double t=Clamp((x-a)/(b-a));return t*t*(3-2*t);}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sweep(double t){double p=t%18;if(p<2)return 0;if(p<8)return 100*Smooth(0,1,(p-2)/6);if(p<10)return 100;if(p<16)return 100*(1-Smooth(0,1,(p-10)/6));return 0;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbWeights Weights(double s)=>new OrbWeights(s);
    }
    public struct OrbWeights
    {
        public double A,B,C,Blue,Crystal;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbWeights(double s){A=1-OrbMath.Smooth(.18,.5,s);C=OrbMath.Smooth(.55,.86,s);B=Math.Max(0,1-A-C);Blue=OrbMath.Smooth(.24,.8,s);Crystal=OrbMath.Smooth(.42,.92,s);}
    }
    public struct OrbColor
    {
        public double R,G,B,A;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbColor(double r,double g,double b,double a=1){R=r;G=g;B=b;A=a;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrbColor Mix(OrbColor a,OrbColor b,double t)=>new OrbColor(OrbMath.Lerp(a.R,b.R,t),OrbMath.Lerp(a.G,b.G,t),OrbMath.Lerp(a.B,b.B,t));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbColor Css(double alpha)=>new OrbColor((int)R,(int)G,(int)B,Math.Floor(OrbMath.Clamp(alpha)*1000+.5)/1000);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrbColor Sprite()=>new OrbColor(Math.Min(255,OrbMath.Round(R/8)*8),Math.Min(255,OrbMath.Round(G/8)*8),Math.Min(255,OrbMath.Round(B/8)*8));
    }
    public static class OrbPalette
    {
        public static readonly OrbColor Gold=new OrbColor(255,176,64),GoldHi=new OrbColor(255,226,150),Amber=new OrbColor(255,128,36),Blue=new OrbColor(58,140,255),Ice=new OrbColor(150,232,255),White=new OrbColor(235,250,255),Grey=new OrbColor(110,140,156),GreyHi=new OrbColor(170,196,206),Mint=new OrbColor(70,255,186),Bars=new OrbColor(150,180,190);
    }
}
