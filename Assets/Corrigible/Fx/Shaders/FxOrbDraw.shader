Shader "Hidden/FxOrbDraw"
{
 Properties { _Glow("Glow Profile",2D)="white"{} }
 SubShader { Tags {"RenderType"="Transparent"} Pass {
 Cull Off ZWrite Off ZTest Always Blend One One
 HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct Primitive {float4 d0,d1,d2,d3,d4,d5,d6;};
 StructuredBuffer<Primitive> _OrbPrimitives;
 struct v2f {float4 vertex:SV_POSITION;float4 d0:TEXCOORD0;float4 d1:TEXCOORD1;float4 d2:TEXCOORD2;float4 c0:TEXCOORD3;float4 c1:TEXCOORD4;float4 c2:TEXCOORD5;float4 d6:TEXCOORD6;float2 p:TEXCOORD7;};
 float _OrbSize;sampler2D _Glow;
 v2f vert(uint id:SV_VertexID){Primitive v=_OrbPrimitives[id/6];uint corner=id%6;float2 a=v.d0.xy,b=v.d0.zw,c=v.d1.xy;float kind=v.d2.x;float2 lo=a,hi=a;float pad=1/v.d6.x;
 if(kind<.5){lo=min(a,b);hi=max(a,b);pad+=v.d1.w*.5;}
 else if(kind>2.5&&kind<3.5){lo=min(a,min(b,c));hi=max(a,max(b,c));}
 else if(kind>4.5)hi=a+b;
 else{pad+=v.d1.z;if(kind>3.5)pad+=v.d1.w*.5;}
 lo-=pad;hi+=pad;float2 p=float2(corner==0||corner>=4?lo.x:hi.x,corner<=1||corner==5?lo.y:hi.y);
 v2f o;o.vertex=float4(p.x/_OrbSize*2-1,1-p.y/_OrbSize*2,0,1);
#if UNITY_UV_STARTS_AT_TOP
 o.vertex.y=-o.vertex.y;
#endif
o.p=p;o.d0=v.d0;o.d1=v.d1;o.d2=v.d2;o.c0=v.d3;o.c1=v.d4;o.c2=v.d5;o.d6=v.d6;return o;}
 float cross2(float2 a,float2 b){return a.x*b.y-a.y*b.x;}
 float4 gradient(float x,float stop,float4 a,float4 b,float4 c){return x<stop?lerp(a,b,saturate(x/max(.00001,stop))):lerp(b,c,saturate((x-stop)/max(.00001,1-stop)));}
 float4 glowPixel(float2 index,float3 color)
 {
   float r=length((index+.5)/64-.5)*2;
   float3 hi=floor(lerp(color*255,255,.45))/255;
   float4 c=r<.12?lerp(float4(1,1,1,1),float4(hi,1),r/.12):r<.3?lerp(float4(hi,1),float4(color,.55),(r-.12)/.18):r<.62?lerp(float4(color,.55),float4(color,.12),(r-.3)/.32):float4(color,.12*saturate((1-r)/.38));
   c.rgb*=c.a;return floor(saturate(c)*255+.5)/255;
 }
 float4 glow(float2 uv,float3 color)
 {
   float2 p=uv*64-.5,lo=floor(p),f=frac(p);
   return lerp(lerp(glowPixel(lo,color),glowPixel(lo+float2(1,0),color),f.x),lerp(glowPixel(lo+float2(0,1),color),glowPixel(lo+1,color),f.x),f.y);
 }
 float4 frag(v2f i):SV_Target
 {
   float2 a=i.d0.xy,b=i.d0.zw,c=i.d1.xy,p=i.p;
   float kind=i.d2.x,stop=i.d2.y,ga=i.d2.z,variant=i.d2.w,width=i.d1.w,radius=i.d1.z,scale=i.d6.x;
   float coverage=1;float4 col=i.c0;
   if(kind<.5){float2 delta=b-a;float len=max(.00001,length(delta));float2 tangent=delta/len;float along=dot(p-a,tangent),side=abs(cross2(p-a,tangent));float dist=variant>1.5?length(float2(max(0,max(-along,along-len)),side))-width*.5:max(side-width*.5,max(-along,along-len));coverage=saturate(.5-dist*scale);if(variant>.5&&variant<1.5)col=gradient(saturate(along/len),stop,i.c0,i.c1,i.c2);}
   else if(kind<1.5){float2 uv=(p-a)/max(.00001,2*radius)+.5;clip(uv);clip(1-uv);return glow(uv,i.c0.rgb)*ga;}
   else if(kind<2.5){float r=length(p-a)/max(.00001,radius);col=gradient(r,stop,i.c0,i.c1,i.c2);if(width>0)coverage=saturate(.5-(length(p-a)-width)*scale);}
   else if(kind<3.5){float sign=cross2(b-a,c-a)>=0?1:-1;float d0=cross2(b-a,p-a)/max(.00001,length(b-a))*sign,d1=cross2(c-b,p-b)/max(.00001,length(c-b))*sign,d2=cross2(a-c,p-c)/max(.00001,length(a-c))*sign;coverage=saturate(.5+min(d0,min(d1,d2))*scale);}
   else if(kind<4.5){coverage=saturate(.5-(abs(length(p-a)-radius)-width*.5)*scale);}
   else {float2 dist=max(a-p,p-(a+b));coverage=saturate(.5-dist.x*scale)*saturate(.5-dist.y*scale);}
   float alpha=col.a*ga*coverage;return float4(col.rgb*alpha,alpha);
 }
 ENDHLSL
 } }
}
