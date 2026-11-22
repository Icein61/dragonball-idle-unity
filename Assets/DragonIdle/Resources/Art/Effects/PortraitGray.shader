Shader "DragonIdle/UI/PortraitGray" {
 Properties {
  [PerRendererData] _MainTex ("Portrait", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _StencilComp ("Stencil Comparison", Float) = 8
  _Stencil ("Stencil ID", Float) = 0
  _StencilOp ("Stencil Operation", Float) = 0
  _StencilWriteMask ("Stencil Write Mask", Float) = 255
  _StencilReadMask ("Stencil Read Mask", Float) = 255
  _ColorMask ("Color Mask", Float) = 15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off
  Lighting Off
  ZWrite Off
  ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]

  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"

   struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
   struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; fixed4 color:COLOR; };
   sampler2D _MainTex;
   fixed4 _Color;
   float4 _ClipRect;

   Output vert(Input input) {
    Output output;
    output.position=UnityObjectToClipPos(input.vertex);
    output.local=input.vertex.xy;
    output.uv=input.uv;
    output.color=input.color*_Color;
    return output;
   }

   fixed4 frag(Output input):SV_Target {
    fixed4 portrait=tex2D(_MainTex,input.uv)*input.color;
    fixed gray=dot(portrait.rgb,fixed3(.2126,.7152,.0722));
    portrait.rgb=gray.xxx;
    #ifdef UNITY_UI_CLIP_RECT
    portrait.a*=UnityGet2DClipping(input.local,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(portrait.a-.001);
    #endif
    return portrait;
   }
   ENDCG
  }
 }
}
