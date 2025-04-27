Shader "Custom/WaterShaderV2_Fixed" {
    Properties {
        _Color ("Water Color", Color) = (0.2, 0.6, 1, 0.8)
        _MainTex ("Wave Texture", 2D) = "gray" {}
        _ShapeTex ("Shape Texture (Alpha)", 2D) = "white" {}
        _Speed ("Wave Speed", Range(0, 5)) = 1
        _Amplitude ("Wave Amplitude", Range(0, 0.5)) = 0.1
        _Frequency ("Wave Frequency", Range(0, 10)) = 2
        _EdgeBlend ("Edge Blend", Range(0, 0.2)) = 0.05
    }
    
    SubShader {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uvWave : TEXCOORD0;
                float2 uvShape : TEXCOORD1;
            };

            sampler2D _MainTex, _ShapeTex;
            float4 _MainTex_ST, _ShapeTex_ST;
            fixed4 _Color;
            float _Speed, _Amplitude, _Frequency, _EdgeBlend;

            v2f vert (appdata v) {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.uvWave = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvShape = TRANSFORM_TEX(v.uv, _ShapeTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 displacedUV = i.uvWave;

                displacedUV.x += _Time.x * _Speed * 0.1;
                displacedUV.y += sin((i.uvWave.x + _Time.y * _Speed) * _Frequency) * _Amplitude;

                fixed4 waveTex = tex2D(_MainTex, displacedUV);
                fixed4 waterColor = _Color * waveTex;

                fixed4 shape = tex2D(_ShapeTex, i.uvShape);
                float shapeAlpha = smoothstep(0.5 - _EdgeBlend, 0.5 + _EdgeBlend, shape.a);

                waterColor.a *= shapeAlpha;
                return waterColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
