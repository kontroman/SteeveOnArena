Shader "Hidden/MineArena/BuildingPreviewUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shadow ("Contact shadow", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Shadow;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                if (_Shadow > 0.5)
                {
                    float2 delta = i.uv * 2 - 1;
                    float shade = pow(saturate(1 - dot(delta, delta)), 2) * 0.16;
                    return fixed4(0.28, 0.24, 0.19, shade);
                }
                fixed4 color = tex2D(_MainTex, i.uv) * _Color;
                clip(color.a - 0.1);
                color.rgb *= 0.82 + 0.18 * saturate(dot(normalize(i.normal), normalize(float3(-0.4, 0.85, -0.3))));
                return color;
            }
            ENDCG
        }
    }
}
