Shader "UI/FxDeskAdditive" {
Properties {
[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
_Color ("Tint", Color) = (1,1,1,1)
_StencilComp ("Stencil Comparison", Float) = 8
_Stencil ("Stencil ID", Float) = 0
_StencilOp ("Stencil Operation", Float) = 0
_StencilWriteMask ("Stencil Write Mask", Float) = 255
_StencilReadMask ("Stencil Read Mask", Float) = 255
_ColorMask ("Color Mask", Float) = 15
[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
}
SubShader {
Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
Blend One One
ColorMask [_ColorMask]
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
#include "UnityCG.cginc"
#include "UnityUI.cginc"
struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float4 data:TEXCOORD1; float4 extra:TEXCOORD2; float4 reveal:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float4 uv:TEXCOORD0; float4 data:TEXCOORD1; float4 extra:TEXCOORD2; float4 reveal:TEXCOORD3; float4 world:TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
sampler2D _MainTex;fixed4 _Color,_TextureSampleAdd;float4 _ClipRect;
float _FxPhases[16];
float _FxTime,_FxStatic,_FxLedPhase,_FxSweepPhase,_FxFlowPhase;
v2f vert(appdata v) {v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;o.data=v.data;o.extra=v.extra;o.reveal=v.reveal;return o;}
float frozenPhase(float period,float phase) {return _FxStatic>.5?phase:frac(_FxTime/max(.001,period));}
fixed4 frag(v2f i):SV_Target {
fixed4 c=(tex2D(_MainTex,i.uv.xy)+_TextureSampleAdd)*i.color;
float kind=i.data.x;
if(kind>.5) {
 if(kind>4.5){float p=_FxStatic>.5?_FxPhases[(int)i.extra.z]:frac(_FxTime/max(.001,i.data.y)+i.reveal.x);float wave=i.extra.w<.5?.5-.5*cos(p*6.2831853):i.extra.w<1.5?p:step(p,.5);float offset=lerp(i.data.z,i.data.w,wave);c=(tex2D(_MainTex,i.uv.xy+i.extra.xy*offset)+_TextureSampleAdd)*i.color;}
 else {
 // fx UV is written independently of packed sprite coordinates in uv2.zw by FxShaderImage.
 float2 uv=i.extra.zw;
 if(kind<1.5) {float p=frozenPhase(i.data.y,_FxSweepPhase);float x=lerp(-.336,1.176,p);float q=(uv.x-x)/.28;c=(tex2D(_MainTex,lerp(i.reveal.xy,i.reveal.zw,float2(saturate(q),.5)))+_TextureSampleAdd)*i.color;c.a*=step(0,q)*step(q,1);if(_FxStatic>.5)c.a=0;}
 else if(kind<2.5) {float x=uv.x*i.data.z;c=i.color;c.a*=step(frac(x/6),4.0/6);}
 else if(kind<3.5) {float p=frozenPhase(i.data.y,_FxLedPhase);float x=lerp(-.4,1.4,p);c=i.color;c.a*=saturate(1-abs((uv.x-x)/.2));}
 else {float p=frozenPhase(i.data.y,0);c=i.color;c.a*=step(.5,frac((uv.x*i.data.z+uv.y*i.data.w)/28-p));}
 }
}
#ifdef UNITY_UI_CLIP_RECT
c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
clip(c.a-.001);
#endif
#ifndef UNITY_COLORSPACE_GAMMA
c.rgb=GammaToLinearSpace(LinearToGammaSpace(c.rgb)*c.a);
#else
c.rgb*=c.a;
#endif
c.a=0;return c;
}
ENDCG
}
}
}
