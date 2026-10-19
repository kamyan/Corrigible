using System;
using System.Runtime.CompilerServices;
using static System.Math;
using static Corrigible.Fx.Orb.OrbMath;
using static Corrigible.Fx.Orb.OrbPalette;
namespace Corrigible.Fx.Orb
{
    /// <summary>fx_orb.js'nin double tabanlı sayısal portu. Oyun verisine veya Unity'ye erişmez.</summary>
    public sealed class OrbSimulation
    {
        public struct Particle {public OrbVector P;public double Size,Phase,Tau,IceWobble;public bool Ice;}
        public struct Block {public double Lat,Lon,Width,Height,Tau,Phase;public bool Dash;}
        public struct Shard {public double Angle,Radius,Speed,Tilt,Size,Rotation,Tau;}
        public struct Spark {public OrbVector P,V;public double Life,Max;public OrbColor Color;}
        struct Ring {public OrbVector U,W;public double Radius,Speed;}
        public readonly double Radius,Size;
        public readonly int Seed;
        public OrbKind Kind;
        public double Level,FilamentScale=1,SweepPhase;
        public bool AutoSweep,SkipIntro;
        public double SweepTime=double.NaN;
        public double Time {get;private set;}
        public double DisplayedLevel {get;private set;}
        public double Angle {get;private set;}
        public double FirstTime {get;private set;}=-1;
        public double NextFilament {get;private set;}
        public double SparkAccumulator {get;private set;}
        public OrbRng Rng;
        public readonly Particle[] Shell,Bands;
        public readonly Block[] Blocks=new Block[34];
        public readonly double[] Jag=new double[42],FaceThreshold=new double[80],EdgeThreshold=new double[120];
        public readonly Shard[] Shards=new Shard[10];
        public readonly Spark[] Sparks=new Spark[512];
        public readonly OrbVector[] Filaments=new OrbVector[60],Arcs=new OrbVector[51];
        public int SparkCount {get;private set;}
        public int FilamentCount {get;private set;}
        public int ArcCount {get;private set;}
        public readonly OrbDrawList DrawList=new OrbDrawList();
        readonly Ring[] rings=new Ring[2];
        readonly OrbVector[] ringPoints=new OrbVector[146];
        static readonly OrbVector[] wireLatitude,wireMeridian;
        static OrbSimulation(){wireLatitude=BuildWire(true);wireMeridian=BuildWire(false);}
        static OrbVector[] BuildWire(bool latitude)
        {
            int rows=latitude?6:8,steps=latitude?64:40;var points=new OrbVector[rows*(steps+1)];
            for(int row=0;row<rows;row++)for(int k=0;k<=steps;k++){double la=latitude?WireLats[row]:(double)k/40*6.283,lo=latitude?(double)k/64*6.283:(double)row/8*3.1416;points[row*(steps+1)+k]=new OrbVector(Cos(la)*Cos(lo),Sin(la),Cos(la)*Sin(lo));}return points;
        }
        readonly OrbVector[] crystal=new OrbVector[42],path=new OrbVector[17];
        static readonly double[] BandLats={-.66,-.33,0,.33,.66},WireLats={-1.05,-.62,-.21,.21,.62,1.05};
        bool started;
        double cyw,syw,cp,sp,rg,center;
        public OrbSimulation(double radius=48,int seed=3,OrbKind kind=OrbKind.Normal,double level=0,double size=0)
        {
            if(radius<1||radius>200)throw new ArgumentOutOfRangeException(nameof(radius));
            Radius=radius;Seed=seed==0?1:seed;Kind=kind;Level=level;DisplayedLevel=level;Size=size>0?size:OrbMath.Round(radius*4.4);
            Rng=new OrbRng(unchecked((uint)(Seed*9973+17)));Angle=Rng.Next()*6.283;
            int n=OrbMath.Round(56+Radius*2.4);Shell=new Particle[n];
            for(int i=0;i<n;i++){double y=1-((i+.5)/n)*2,rr=Sqrt(1-y*y),th=i*2.399963+Rng.Next()*.25;Shell[i]=new Particle{P=new OrbVector(Cos(th)*rr,y,Sin(th)*rr),Size=.55+Rng.Next(),Phase=Rng.Next()*6.283,Tau=Rng.Next(),Ice=Rng.Next()<.35};}
            for(int i=0;i<Shell.Length;i++)Shell[i].IceWobble=Sin(Shell[i].Phase*7);
            int total=0;foreach(double lat in BandLats)total+=OrbMath.Round(16+Radius*.62*Cos(lat));Bands=new Particle[total];int index=0;
            for(int bi=0;bi<BandLats.Length;bi++){double lat=BandLats[bi];int m=OrbMath.Round(16+Radius*.62*Cos(lat));for(int k=0;k<m;k++){double lon=(double)k/m*6.283+bi*.21;Bands[index++]=new Particle{P=new OrbVector(Cos(lat)*Cos(lon),Sin(lat),Cos(lat)*Sin(lon)),Tau=Rng.Next(),Phase=Rng.Next()*6.283};}}
            for(int j=0;j<34;j++)Blocks[j]=new Block{Lat=(Rng.Next()*2-1)*1.2,Lon=Rng.Next()*6.283,Width=.08+Rng.Next()*.26,Height=.02+Rng.Next()*.035,Tau=Rng.Next(),Phase=Rng.Next()*6.283,Dash=Rng.Next()<.4};
            MakeRing(0,new OrbVector(.35,.92,.18),1.3,.55);MakeRing(1,new OrbVector(-.82,.4,.41),1.52,-.38);
            for(int i=0;i<42;i++){double k=.8+Rng.Next()*.42;if(Rng.Next()<.22)k+=.38+Rng.Next()*.42;Jag[i]=k;}
            for(int i=0;i<80;i++)FaceThreshold[i]=Rng.Next();for(int i=0;i<120;i++)EdgeThreshold[i]=Rng.Next();
            for(int q=0;q<10;q++)Shards[q]=new Shard{Angle=Rng.Next()*6.283,Radius=1.3+Rng.Next()*.55,Speed=(.4+Rng.Next()*.7)*(Rng.Next()<.5?-1:1),Tilt=(Rng.Next()*2-1)*.9,Size=.1+Rng.Next()*.12,Rotation=Rng.Next()*6.283,Tau=Rng.Next()};
        }
        void MakeRing(int i,OrbVector n,double radius,double speed){n=n.Normalized;var a=Abs(n.Y)<.9?new OrbVector(0,1,0):new OrbVector(1,0,0);var u=OrbVector.Cross(a,n).Normalized;rings[i]=new Ring{U=u,W=OrbVector.Cross(n,u),Radius=radius,Speed=speed};for(int k=0;k<=72;k++)ringPoints[i*73+k]=RingPoint(rings[i],(double)k/72*6.283);}
        public void Advance(double dt,int count=1){if(dt<=0||double.IsNaN(dt)||double.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));for(int i=0;i<count;i++){Time+=dt;Draw(Time,dt);}}
        public void AdvanceRealtime(double elapsed){if(elapsed<=0)return;Time+=elapsed;Draw(Time,Min(.1,elapsed));}
        public void CompleteIntro(){SkipIntro=true;if(started)FirstTime=Min(FirstTime,Time-1.1);}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        OrbVector Rot(double x,double y,double z){double x1=x*cyw+z*syw,z1=-x*syw+z*cyw;return new OrbVector(x1,y*cp-z1*sp,y*sp+z1*cp);}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        OrbVector Rot(OrbVector p)=>Rot(p.X,p.Y,p.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        OrbVector Project(OrbVector p)=>new OrbVector(center+p.X*rg,center-p.Y*rg,p.Z);
        OrbVector RingPoint(Ring r,double a)=>r.U*(Cos(a)*r.Radius)+r.W*(Sin(a)*r.Radius);
        void Draw(double t,double dt)
        {
            var d=DrawList;double target=Kind==OrbKind.Unknown?0:AutoSweep?Sweep((double.IsNaN(SweepTime)?t:SweepTime)+SweepPhase):Level;
            if(!started){started=true;FirstTime=SkipIntro?t-1.1:t;DisplayedLevel=target;}
            DisplayedLevel+=(target-DisplayedLevel)*Min(1,dt*(AutoSweep?6:3));double s=DisplayedLevel/100;var weights=Weights(s);double wa=weights.A,wb=weights.B,wc=weights.C,blue=weights.Blue;
            bool unk=Kind==OrbKind.Unknown,quar=Kind==OrbKind.Quarantine,core=Kind==OrbKind.Core;double dim=quar?.72:1,intro=Min(1,(t-FirstTime)/1.1),grow=intro*intro*(3-2*intro);
            d.Clear();center=Size/2;rg=Radius*(.55+.45*grow);Angle+=dt*(unk?.12:(.2+.62*s)*(quar?.45:1));double pitch=-.36+.1*Sin(t*.8+Seed)*(wb+wc);cyw=Cos(Angle);syw=Sin(Angle);cp=Cos(pitch);sp=Sin(pitch);
            var tone=OrbColor.Mix(Gold,Blue,blue);var toneHi=OrbColor.Mix(GoldHi,Ice,blue);if(unk){tone=Grey;toneHi=GreyHi;}
            var origin=new OrbVector(center,center);
            // Dış hale ve çekirdek geçişleri (CSS RGBA durakları üç ondalığa yuvarlanır).
            d.Radial(origin,rg*2.05,.45,tone.Css((unk?.05:.2)*dim*grow),tone.Css((unk?.03:.09)*dim*grow),tone.Css(0));
            if(!unk){double pulse=.82+.18*Sin(t*(2+3*s)+Seed);d.Radial(origin,rg*(.62+.25*wc),.25,White.Css(.75*pulse*dim),toneHi.Css(.5*pulse*dim),tone.Css(0),rg*.95);}
            double flare=(wb+wc*.35)*dim*grow*(unk?0:1);
            if(flare>.03)
            {
                double start=t*(.9+s)+Seed;for(int fr=0;fr<6;fr++){double angle=start+fr*(6.283/6)+.25*Sin(t*3+fr),len=rg*(.8+.5*Abs(Sin(t*2.7+fr*1.3)));var col=fr%2!=0?OrbColor.Mix(Amber,GoldHi,.4):Ice;if(s>.7)col=Ice;var end=origin+new OrbVector(Cos(angle)*len,Sin(angle)*len);d.GradientLine(origin,end,rg*.07,.35,White.Css(.55*flare),col.Css(.32*flare),col.Css(0));}
                double fs=rg*(.35+.12*Sin(t*7));d.Sprite(origin,fs,s>.6?Ice:GoldHi,.85*flare);d.GlobalAlpha=1;
            }
            if(core&&!unk)for(int w=0;w<2;w++){double ph=((t+w*1.2)%2.4)/2.4;d.Circle(origin,rg*(1.05+ph*.95),1.6,tone,(1-ph)*.45*grow);}
            double ringA=wa*(unk?0:1)*dim*grow;
            if(ringA>.02)for(int ri=0;ri<2;ri++)
            {
                var r=rings[ri];double spin=t*r.Speed+ri;OrbVector prev=default;
                for(int k=0;k<=72;k++){var q=Rot(ringPoints[ri*73+k]);var p=Project(q);if(k>0&&((k+Floor(spin*6))%3!=0)){double depth=(q.Z/r.Radius+1)/2;d.Line(prev,p,1.1,GoldHi,ringA*(.12+.5*depth));}prev=p;}
                for(int i=0;i<2;i++){double a=spin*1.6+i*3.1416;var q=Rot(RingPoint(r,a));double depth=(q.Z/r.Radius+1)/2,sz=Radius*(.22+.16*depth);d.Sprite(Project(q),sz,GoldHi,ringA*(.35+.65*depth));d.GlobalAlpha=1;}
            }
            double wireA=(wa*.9+wb*.35)*dim*grow*(unk?.35:1);
            if(wireA>.02)
            {
                var color=unk?Grey:OrbColor.Mix(Gold,Blue,blue*.8);
                for(int li=0;li<6;li++){double la=WireLats[li];OrbVector prev=default;for(int k=0;k<=64;k++){double lo=(double)k/64*6.283,wob=wb*.07*Sin(lo*3+t*2.4+li);var basis=wireLatitude[li*65+k];var p=Project(Rot(basis.X*(1+wob),basis.Y*(1+wob),basis.Z*(1+wob)));if(k>0){double depth=((p.Z+prev.Z)/2+1)/2;d.Line(prev,p,.9,color,wireA*(.04+.34*depth*depth));}prev=p;}}
                for(int mi=0;mi<8;mi++){double lo=(double)mi/8*3.1416;OrbVector prev=default;for(int k=0;k<=40;k++){double la=(double)k/40*6.283,wob=wb*.07*Sin(la*3+t*2.1+mi);var basis=wireMeridian[mi*41+k];var p=Project(Rot(basis.X*(1+wob),basis.Y*(1+wob),basis.Z*(1+wob)));if(k>0){double depth=((p.Z+prev.Z)/2+1)/2;d.Line(prev,p,.9,color,wireA*(.03+.26*depth*depth));}prev=p;}}
                if(!unk){OrbVector prev=default;for(int k=0;k<=48;k++){double angle=(double)k/48*6.283+t*1.4;var p=Project(Rot(Cos(angle)*.42,.1*Sin(angle*2+t),Sin(angle)*.42));if(k>0&&k%4!=0)d.Line(prev,p,1.4,GoldHi,wa*.55*dim*grow);prev=p;}}
            }
            // Kabuk izinde kaynak globalAlpha hem CSS alfa hem çarpan olarak iki kez uygulanır.
            double jitter=wb*.2+wc*.05;
            for(int i=0;i<Shell.Length;i++)
            {
                var p=Shell[i];double rr=1+jitter*Sin(t*2.6+p.Phase*3)+wc*.06*p.IceWobble,nx=p.P.X*rr+jitter*.5*Sin(t*1.9+p.Phase),ny=p.P.Y*rr+jitter*.5*Cos(t*2.3+p.Phase*2),nz=p.P.Z*rr;var q=Rot(nx,ny,nz);double depth=(q.Z+1)/2,a=(.18+.82*depth)*dim*grow;OrbColor col;
                if(unk){col=Grey;a*=.35+.65*Abs(Sin(t*9+p.Phase*13));}else{double edge=p.Tau-blue;var cb=p.Ice?Ice:Blue;var cg=p.Ice?GoldHi:Gold;col=edge>.07?cg:edge<-.07?cb:OrbColor.Mix(cb,cg,(edge+.07)/.14);}
                double sz=Radius*.085*p.Size*(.65+.55*depth)*(1-.3*wc);d.GlobalAlpha=a*(.42*wa+wb+.3*wc+(unk?.6:0));
                if(wb>.25&&p.Size>1.25){double vx=Sin(t*1.9+p.Phase)*.12*wb,vy=Cos(t*2.3+p.Phase*2)*.12*wb;var q7=Rot(nx-vx*2.2,ny-vy*2.2,nz);d.Line(Project(q7),Project(q),sz*.7,col,d.GlobalAlpha*.6);}
                d.Sprite(Project(q),sz*1.6,col,d.GlobalAlpha);
            }
            d.GlobalAlpha=1;
            double bandA=(wa+wb*.45)*dim*grow*(unk?.5:1);
            if(bandA>.02){for(int i=0;i<Bands.Length;i++){var e=Bands[i];double dx=wb*.18*Sin(t*3.1+e.Phase*5),dy=wb*.14*Cos(t*2.7+e.Phase*3);var q=Rot(e.P.X+dx,e.P.Y+dy,e.P.Z);double depth=(q.Z+1)/2;if(depth<.18)continue;var col=unk?Grey:e.Tau<blue?Ice:GoldHi;double sz=Radius*.05*(.6+.6*depth);d.Sprite(Project(q),sz*1.5,col,bandA*(.1+.9*depth)*(unk?.4:1));}d.GlobalAlpha=1;}
            double blockA=wa*dim*grow*(unk?0:1);
            if(blockA>.02)foreach(var bl in Blocks){double cl=Cos(bl.Lat),sl=Sin(bl.Lat),co=Cos(bl.Lon),so=Sin(bl.Lon),x=cl*co*1.04,y=sl*1.04,z=cl*so*1.04,tx=-so,tz=co;var a1=Rot(x-tx*bl.Width/2,y,z-tz*bl.Width/2);var a2=Rot(x+tx*bl.Width/2,y,z+tz*bl.Width/2);double depth=((a1.Z+a2.Z)/2+1)/2,blink=.55+.45*Sin(t*2.2+bl.Phase*4),width=Max(1,bl.Height*rg*(.6+.6*depth)),alpha=blockA*(.08+.75*depth)*blink;var col=bl.Tau<blue?Ice:GoldHi;if(bl.Dash)d.DashedLine(Project(a1),Project(a2),width,col,alpha);else d.Line(Project(a1),Project(a2),width,col,alpha);}
            double crystalF=weights.Crystal*(unk?0:1);
            if(crystalF>.01)DrawCrystal(t,s,wa,wb,wc,dim,grow,crystalF,origin);
            if(quar&&!unk)DrawCage(t,grow);
            UpdateFilaments(t,s,wb,wc,quar,unk);
            StrokePaths(Filaments,FilamentCount,10,Blue,White,.16*dim*grow,.85*dim*grow,1.1);
            StrokePaths(Arcs,ArcCount,17,Blue,Ice,.2*dim*grow,.7*dim*grow,1.3);
            if(!unk)UpdateSparks(t,dt,wa,wb,wc,blue,quar,dim);
            if(unk)for(int i=0;i<3;i++){double y=center+Sin(t*(1.3+i)+i*2)*Radius*.9,w=Radius*(.6+.6*Abs(Sin(t*3+i)));d.Rectangle(new OrbVector(center-w,y),new OrbVector(w*2,1.4),Bars,.08+.08*Abs(Sin(t*11+i)));}
        }
        void DrawCrystal(double t,double s,double wa,double wb,double wc,double dim,double grow,double factor,OrbVector origin)
        {
            var d=DrawList;
            for(int i=0;i<42;i++){double k=Lerp(1,Jag[i],Smooth(.5,1,s))*(1.02+.05*Sin(t*1.3+i));crystal[i]=Rot(OrbIcosphere.Vertices[i]*k);}
            for(int i=0;i<80;i++){double vis=Smooth(FaceThreshold[i]-.08,FaceThreshold[i]+.08,factor*1.08);if(vis<.02)continue;var tr=OrbIcosphere.Faces[i];var a=crystal[tr[0]];var b=crystal[tr[1]];var c=crystal[tr[2]];double nz=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);if(nz<=0)continue;var norm=OrbVector.Cross(b-a,c-a).Normalized;double light=Max(0,norm.X*-.4+norm.Y*.55+norm.Z*.75);d.Triangle(Project(a),Project(b),Project(c),OrbColor.Mix(Blue,Ice,light),(.05+.26*light*light)*vis*dim*grow);}
            for(int i=0;i<120;i++){double ev=Smooth(EdgeThreshold[i]-.1,EdgeThreshold[i]+.1,factor*1.1);if(ev<.02)continue;var edge=OrbIcosphere.Edges[i];var a=crystal[edge[0]];var b=crystal[edge[1]];double depth=((a.Z+b.Z)/2+1.3)/2.3,fl=.75+.25*Sin(t*6+i*1.7);d.Line(Project(a),Project(b),1,depth>.62?White:Ice,ev*(.06+.62*depth*depth)*fl*dim*grow);}
            for(int i=0;i<42;i++){if(Jag[i]<1.12)continue;double cr=Smooth(.62,1,s)*(.4+.6*Abs(Sin(t*2.3+i)));d.Line(origin,origin+(Project(crystal[i])-origin)*.92,1,Ice,.22*cr*dim*grow);}
            int ck=(int)Floor(t*9)%120;for(int i=0;i<3;i++){var e=OrbIcosphere.Edges[(ck*7+i*37)%120];var a=crystal[e[0]];var b=crystal[e[1]];if(a.Z+b.Z<0)continue;d.Line(Project(a),Project(b),1.6,White,.9*wc*dim);}
            for(int i=0;i<42;i++){if(Jag[i]<1.1||crystal[i].Z<0)continue;double size=Radius*.16*Smooth(.6,1,s);d.Sprite(Project(crystal[i]),size,Ice,.7*factor*dim);}d.GlobalAlpha=1;
            double shA=wc*dim*grow;
            if(shA>.02)foreach(var sh in Shards){double angle=sh.Angle+t*sh.Speed;var p=Rot(Cos(angle)*sh.Radius,Sin(angle*.7)*sh.Tilt,Sin(angle)*sh.Radius);double depth=(p.Z/sh.Radius+1)/2,len=sh.Size*rg,rt=sh.Rotation+t*2*sh.Speed;var xy=Project(p);var a=xy+new OrbVector(Cos(rt)*len,Sin(rt)*len);var b=xy+new OrbVector(Cos(rt+2.3)*len*.6,Sin(rt+2.3)*len*.6);var c=xy+new OrbVector(Cos(rt+3.9)*len*.5,Sin(rt+3.9)*len*.5);d.Triangle(a,b,c,Ice,shA*(.15+.5*depth));d.Line(a,b,1.2,White,shA*(.25+.6*depth));d.Line(b,c,1.2,White,shA*(.25+.6*depth));d.Line(c,a,1.2,White,shA*(.25+.6*depth));}
        }
        void DrawCage(double t,double grow)
        {
            double qa=-t*.35,cqa=Cos(qa),sqa=Sin(qa);var d=DrawList;
            for(int li=0;li<3;li++){double la=(li-1)*.5;OrbVector prev=default;for(int k=0;k<=48;k++){double lo=(double)k/48*6.283,x=Cos(la)*Cos(lo)*1.42,y=Sin(la)*1.42,z=Cos(la)*Sin(lo)*1.42,xr=x*cqa+z*sqa,zr=-x*sqa+z*cqa;var p=Project(new OrbVector(xr,y*cp-zr*sp,y*sp+zr*cp));if(k>0&&k%2==0)d.Line(prev,p,1,Mint,.42*grow);prev=p;}}
            for(int m=0;m<6;m++){double lo=(double)m/6*3.1416+qa;OrbVector prev=default;for(int k=0;k<=32;k++){double la=(double)k/32*3.1416-1.5708,x=Cos(la)*Cos(lo)*1.42,y=Sin(la)*1.42,z=Cos(la)*Sin(lo)*1.42;var q=new OrbVector(x,y*cp-z*sp,y*sp+z*cp);var p=Project(q);if(k>0&&k%2==1&&q.Z>-.2)d.Line(prev,p,1,Mint,.42*grow);prev=p;}}
        }
        void UpdateFilaments(double t,double s,double wb,double wc,bool quar,bool unk)
        {
            int count=unk?0:OrbMath.Round(wb*5+wc*2+(s>.3?1:0)*wb);
            if(t<=NextFilament)return;FilamentCount=count;
            for(int i=0;i<count;i++){var dir=new OrbVector(Rng.Next()*2-1,Rng.Next()*2-1,Rng.Next()*2-1).Normalized;double len=((quar?.95:1.15)+Rng.Next()*(quar?.3:.7+wb*.6))*FilamentScale;var side=OrbVector.Cross(dir,new OrbVector(.3,1,.2)).Normalized;for(int j=0;j<=9;j++){double f=(double)j/9,rad=.25+f*len,off=(Rng.Next()*2-1)*.16*(1-Abs(f-.5));Filaments[i*10+j]=dir*rad+side*off;}}
            ArcCount=unk?0:OrbMath.Round(wb*3+wc);
            for(int i=0;i<ArcCount;i++){var axis=new OrbVector(Rng.Next()*2-1,Rng.Next()*2-1,Rng.Next()*2-1).Normalized;double a0=Rng.Next()*6.283,span=1.2+Rng.Next()*1.6;var u=OrbVector.Cross(axis,new OrbVector(.1,1,.3)).Normalized;var w=OrbVector.Cross(axis,u);for(int j=0;j<=16;j++){double a=a0+(double)j/16*span,r=1.08+Rng.Next()*.12+.12*Sin(j);Arcs[i*17+j]=new OrbVector((u.X*Cos(a)+w.X*Sin(a))*r,(u.Y*Cos(a)+w.Y*Sin(a))*r,(u.Z*Cos(a)+w.Z*Sin(a))*r);}}
            NextFilament=t+.07+Rng.Next()*(.22-.1*wb);
        }
        void StrokePaths(OrbVector[] points,int count,int stride,OrbColor wide,OrbColor thin,double aw,double at,double width)
        {
            var d=DrawList;for(int i=0;i<count;i++){for(int j=0;j<stride;j++)path[j]=Project(Rot(points[i*stride+j]));for(int pass=0;pass<2;pass++){double lw=width*(pass==0?3.2:1),alpha=pass==0?aw:at;var color=pass==0?wide:thin;for(int j=1;j<stride;j++)d.Line(path[j-1],path[j],lw,color,alpha);}}
        }
        void UpdateSparks(double t,double dt,double wa,double wb,double wc,double blue,bool quar,double dim)
        {
            double rate=(1.2*wa+16*wb+20*wc)*(quar?.4:1)*(Radius/34);SparkAccumulator+=rate*dt;
            while(SparkAccumulator>1){SparkAccumulator-=1;var dir=new OrbVector(Rng.Next()*2-1,Rng.Next()*2-1,Rng.Next()*2-1).Normalized;double speed=.6+Rng.Next()*(.6+wb),life=.5+Rng.Next()*.7;var color=Rng.Next()<blue?(Rng.Next()<.5?Ice:Blue):(Rng.Next()<.5?GoldHi:Amber);if(SparkCount>=Sparks.Length)throw new InvalidOperationException("Kıvılcım tamponu aşıldı.");Sparks[SparkCount++]=new Spark{P=dir,V=dir*speed,Life=0,Max=life,Color=color};}
            var d=DrawList;for(int i=SparkCount-1;i>=0;i--){var p=Sparks[i];p.Life+=dt;if(p.Life>p.Max){Array.Copy(Sparks,i+1,Sparks,i,SparkCount-i-1);SparkCount--;continue;}p.P+=p.V*dt;Sparks[i]=p;double life=1-p.Life/p.Max,size=Radius*.07*(.5+life);d.Sprite(Project(Rot(p.P)),size,p.Color,life*.9*dim);}d.GlobalAlpha=1;
        }
    }
}
