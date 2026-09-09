Shader "Hidden/MineArena/SkinPreview"
{
    Properties { _MainTex ("Skin", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(Output i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv); clip(c.a-.1); return fixed4(c.rgb,1); }
            ENDCG
        }
    }
}
