Shader "UI/FxLine" {
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
Blend SrcAlpha OneMinusSrcAlpha
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
float _FxTime,_FxStatic,_FxLedPhase,_FxSweepPhase,_FxFlowPhase;
v2f vert(appdata v) {v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;o.data=v.data;o.extra=v.extra;o.reveal=v.reveal;return o;}
float frozenPhase(float period,float phase) {return _FxStatic>.5?phase:frac(_FxTime/max(.001,period));}
fixed4 frag(v2f i):SV_Target {
float u=i.data.x,side=i.data.y,halfWidth=i.data.z,soft=max(.1,i.data.w),total=i.reveal.x;
float distance=abs(side)-halfWidth;
if(i.extra.w>.5) {float beyond=max(-u,u-total);distance=length(float2(max(0,beyond),side))-halfWidth;}
else {clip(u);clip(total-u);}
float alpha=saturate(.5-distance/soft);
float period=i.extra.x+i.extra.y;
if(i.extra.y>0 && period>0) {
 float t=_FxStatic>.5?_FxFlowPhase*period:_FxTime*i.extra.z;
 float pos=frac((u-t)/period)*period;
 float edge=min(pos,i.extra.x-pos);
 alpha*=saturate(edge/max(.1,fwidth(u))+.5);
}
float reveal=i.reveal.z<=0||_FxStatic>.5?1:saturate((_FxTime-i.reveal.y-i.reveal.w)/i.reveal.z);
alpha*=step(u,total*reveal);
fixed4 c=i.color;c.a*=alpha;
#ifdef UNITY_UI_CLIP_RECT
c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
clip(c.a-.001);
#endif
return c;
}
ENDCG
}
}
}
