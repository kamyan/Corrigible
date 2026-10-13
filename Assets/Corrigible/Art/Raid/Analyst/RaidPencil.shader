Shader "Corrigible/Raid Pencil"
{
 Properties { _BaseColor("Mürekkep", Color)=(1,1,1,1) _ZTest("Derinlik", Float)=8 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest [_ZTest]
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; };
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
   CBUFFER_END
   Varyings Vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.color=input.color*_BaseColor; return o; }
   half4 Frag(Varyings input):SV_Target { return input.color; }
   ENDHLSL
  }
 }
}