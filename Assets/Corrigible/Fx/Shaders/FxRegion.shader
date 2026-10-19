Shader "UI/FxRegion"
{
 Properties {
 [PerRendererData] _MainTex("Orb Canvas",2D)="black"{}
 _Color("Tint",Color)=(1,1,1,1)
 _BaseMap("Base",2D)="black"{}
 _Dots("Dots",2D)="white"{}
 _Hatch("Unknown Hatch",2D)="white"{}
 _Hex("Quarantine",2D)="white"{}
 _StencilComp("Stencil Comparison",Float)=8
 _Stencil("Stencil ID",Float)=0
 _StencilOp("Stencil Operation",Float)=0
 _StencilWriteMask("Stencil Write Mask",Float)=255
 _StencilReadMask("Stencil Read Mask",Float)=255
 _ColorMask("Color Mask",Float)=15
 [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader {Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
 Stencil{Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
 Pass {CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 data:TEXCOORD1;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float4 world:TEXCOORD1;float index:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
 sampler2D _MainTex,_BaseMap,_Dots,_Hatch,_Hex;fixed4 _Color;float4 _RegionData[12],_RegionNodes[12];float4 _ClipRect;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.world=v.vertex;o.index=v.data.x;o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target
 {
    int id=(int)(i.index+.5);float4 state=_RegionData[id],node=_RegionNodes[id];float s=saturate(state.x);float t=smoothstep(.24,.80,s);
    float3 tone=floor(lerp(float3(255,176,64),float3(58,140,255),t)+.5)/255;
    float3 result=tex2D(_BaseMap,float2(i.uv.x/1920,1-i.uv.y/1080)).rgb;
    #ifndef UNITY_COLORSPACE_GAMMA
    result=LinearToGammaSpace(result);
    #endif
    if(state.y<.5){float d=distance(i.uv,node.xy)/node.z;float a=(.20+.40*s)*(d<.45?lerp(1,.38,saturate(d/.45)):lerp(.38,0,saturate((d-.45)/.55)));result=lerp(result,tone,a);}
    else{tone=float3(127,163,179)/255;float hatch=tex2D(_Hatch,float2(i.uv.x,-i.uv.y)/8.485).a;result=lerp(result,tone,hatch*.16);}
    float dots=tex2D(_Dots,float2(i.uv.x,-i.uv.y)/7).a;
    if(state.y>.5){float r=length(frac(i.uv/7)*7-3.5);dots=saturate((.9-r)/max(.001,fwidth(r))+.5);}
    result=lerp(result,tone,dots*(state.y>.5?.30:.26+.30*s));
    if(state.z>.5){float hex=tex2D(_Hex,float2(i.uv.x/13.856,-i.uv.y/24)).a;result=lerp(result,float3(61,255,180)/255,hex*.22);}
    if(state.w>.5)result=lerp(result,float3(91,228,255)/255,.07);
    #ifndef UNITY_COLORSPACE_GAMMA
    result=GammaToLinearSpace(result);
    #endif
    float alpha=i.color.a;
    #ifdef UNITY_UI_CLIP_RECT
    alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(alpha-.001);
    #endif
    return float4(result*i.color.rgb,alpha);
 }
 ENDCG
 } }
}
