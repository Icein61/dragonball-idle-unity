Shader "DragonIdle/UI/PursuitSheen" {
 Properties {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
  [HideInInspector] _BandY ("Band centre", Float) = 0
  [HideInInspector] _BandHalfHeight ("Band half height", Float) = 0.1
  [HideInInspector] _StencilComp ("Stencil comparison", Float) = 8
  [HideInInspector] _Stencil ("Stencil ID", Float) = 0
  [HideInInspector] _StencilOp ("Stencil operation", Float) = 0
  [HideInInspector] _StencilWriteMask ("Stencil write mask", Float) = 255
  [HideInInspector] _StencilReadMask ("Stencil read mask", Float) = 255
  [HideInInspector] _ColorMask ("Color mask", Float) = 15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use UI alpha clip", Float) = 0
 }
 SubShader {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
  Stencil {
   Ref [_Stencil]
   Comp [_StencilComp]
   Pass [_StencilOp]
   ReadMask [_StencilReadMask]
   WriteMask [_StencilWriteMask]
  }
  Cull Off
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

   struct AppData {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    fixed4 color : COLOR;
   };
   struct Interpolators {
    float4 clipPosition : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 localPosition : TEXCOORD1;
    fixed4 color : COLOR;
   };
   sampler2D _MainTex;
   float4 _ClipRect;
   float _BandY;
   float _BandHalfHeight;

   Interpolators vert(AppData input) {
    Interpolators output;
    output.clipPosition=UnityObjectToClipPos(input.vertex);
    output.localPosition=input.vertex.xy;
    output.uv=input.uv;
    output.color=input.color;
    return output;
   }

   fixed4 frag(Interpolators input) : SV_Target {
    float shade=1-smoothstep(0,_BandHalfHeight,abs(input.uv.y-_BandY));
    fixed alpha=tex2D(_MainTex,input.uv).a*shade*input.color.a;
    #ifdef UNITY_UI_CLIP_RECT
    alpha*=UnityGet2DClipping(input.localPosition,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(alpha-.001);
    #endif
    return fixed4(input.color.rgb,alpha);
   }
   ENDCG
  }
 }
}
