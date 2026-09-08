Shader "MineArena/PixelItem"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.texcoord,_MainTex); return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c=tex2D(_MainTex,i.uv)*_Color; clip(c.a-0.1); return c; }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent Cutout"
}
