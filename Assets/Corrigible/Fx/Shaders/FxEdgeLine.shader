Shader "Corrigible/FxEdgeLine" {
 Properties {_Width("Piksel",Float)=1 _Opacity("Alfa",Float)=1 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent"}
 Pass {Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 struct A {float4 position:POSITION;float4 other:TEXCOORD0;float4 data:TEXCOORD1;float3 n1:NORMAL;float4 n2:TANGENT;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float4 color:COLOR;float side:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Width,_Opacity;
 CBUFFER_END
 float _FxBoardFlicker,_FxBoardAlpha;
 V vert(A i){V o;float4 a=TransformObjectToHClip(i.position.xyz),b=TransformObjectToHClip(i.other.xyz);float2 d=normalize((b.xy/b.w-a.xy/a.w)*_ScreenParams.xy);float2 n=float2(-d.y,d.x);
 a.xy+=n*i.data.x*(_Width+1.4)/_ScreenParams.xy*a.w;
 #if UNITY_REVERSED_Z
 a.z+=.0001*a.w;
 #else
 a.z-=.0001*a.w;
 #endif
 o.position=a;
 float3 w=TransformObjectToWorld(i.position.xyz);float3 view=GetWorldSpaceViewDir(w);float f1=dot(TransformObjectToWorldNormal(i.n1),view),f2=dot(TransformObjectToWorldNormal(i.n2.xyz),view);
 o.color=i.color;o.color.rgb=SRGBToLinear(o.color.rgb);o.color.a*=i.data.y>.5?1:(f1*f2<0?1:0);o.side=i.data.x*(i.other.w>.5?-1:1)*(_Width+1.4)*.5;return o;}
 half4 frag(V i):SV_Target{return half4(i.color.rgb*max(.01,_FxBoardFlicker),i.color.a*_Opacity*_FxBoardAlpha*saturate(.5-(abs(i.side)-_Width*.5)/max(.7,fwidth(i.side))));}
 ENDHLSL
 } }
}
