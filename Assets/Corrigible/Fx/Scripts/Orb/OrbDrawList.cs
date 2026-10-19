using System;
using System.Runtime.CompilerServices;
namespace Corrigible.Fx.Orb
{
    public enum OrbShape { Line, Sprite, Radial, Triangle, Circle, Rectangle }
    public struct OrbPrimitive
    {
        public OrbShape Shape;
        public OrbVector A,B,C;
        public OrbColor Color0,Color1,Color2;
        public double Width,Radius,Stop,GlobalAlpha;
        public bool Gradient,Round;
    }
    /// <summary>Önceden ayrılmış çizim tamponu; köşe/doku üretimi sunum assembly'sindedir.</summary>
    public sealed class OrbDrawList
    {
        public const int Capacity=5120;
        public readonly OrbPrimitive[] Items=new OrbPrimitive[Capacity];
        public int Count {get;private set;}
        double globalAlpha=1;
        public double GlobalAlpha {get=>globalAlpha;set{if(value>=0&&value<=1)globalAlpha=value;}}
        public void Clear(){Count=0;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in OrbPrimitive p){if(Count>=Capacity)throw new InvalidOperationException("Küre çizim tamponu aşıldı.");Items[Count]=p;Items[Count++].GlobalAlpha=GlobalAlpha;}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Line(OrbVector a,OrbVector b,double width,OrbColor c,double alpha,bool round=false)=>Add(new OrbPrimitive{Shape=OrbShape.Line,A=a,B=b,Width=width,Color0=c.Css(alpha),Round=round});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sprite(OrbVector center,double halfSize,OrbColor c,double alpha){GlobalAlpha=alpha;Add(new OrbPrimitive{Shape=OrbShape.Sprite,A=center,Radius=halfSize,Color0=c.Sprite()});}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Radial(OrbVector center,double radius,double stop,OrbColor a,OrbColor b,OrbColor c,double clip=0)=>Add(new OrbPrimitive{Shape=OrbShape.Radial,A=center,Radius=radius,Width=clip,Stop=stop,Color0=a,Color1=b,Color2=c});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Triangle(OrbVector a,OrbVector b,OrbVector c,OrbColor color,double alpha)=>Add(new OrbPrimitive{Shape=OrbShape.Triangle,A=a,B=b,C=c,Color0=color.Css(alpha)});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Circle(OrbVector center,double radius,double width,OrbColor color,double alpha)=>Add(new OrbPrimitive{Shape=OrbShape.Circle,A=center,Radius=radius,Width=width,Color0=color.Css(alpha)});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GradientLine(OrbVector a,OrbVector b,double width,double stop,OrbColor c0,OrbColor c1,OrbColor c2)=>Add(new OrbPrimitive{Shape=OrbShape.Line,A=a,B=b,Width=width,Gradient=true,Stop=stop,Color0=c0,Color1=c1,Color2=c2});
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rectangle(OrbVector a,OrbVector size,OrbColor c,double alpha)=>Add(new OrbPrimitive{Shape=OrbShape.Rectangle,A=a,B=size,Color0=c.Css(alpha)});
        public void DashedLine(OrbVector a,OrbVector b,double width,OrbColor c,double alpha)
        {
            var d=b-a;double len=Math.Sqrt(d.X*d.X+d.Y*d.Y);if(len<=0)return;for(double u=0;u<len;u+=3.8)Line(a+d*(u/len),a+d*(Math.Min(u+2.2,len)/len),width,c,alpha);
        }
    }
}
