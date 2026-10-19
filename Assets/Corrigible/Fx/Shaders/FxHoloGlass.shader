Shader "Corrigible/FxHoloGlass" {
 Properties {_BaseColor("Renk",Color)=(.03,.1,.2,1) _BaseMap("Doku",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;};struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;
 CBUFFER_END
 float _FxBoardFlicker,_FxBoardAlpha;
 V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{clip(_FxBoardAlpha-.001);half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;c.rgb*=max(.01,_FxBoardFlicker)*_FxBoardAlpha;return c;}
 ENDHLSL
 } }
}
