Shader "Custom/WaterShaderV2_Fixed" {
    Properties {
        _Color ("Water Color", Color) = (0.2, 0.6, 1, 0.8)
        _MainTex ("Wave Texture", 2D) = "gray" {}
        _ShapeTex ("Shape Texture (Alpha)", 2D) = "white" {}
        _Speed ("Wave Speed", Range(0, 5)) = 1
        _Amplitude ("Wave Amplitude", Range(0, 0.5)) = 0.1
        _Frequency ("Wave Frequency", Range(0, 10)) = 2
        _EdgeBlend ("Edge Blend", Range(0, 0.2)) = 0.05
        _TilesPerUnit ("Water Tiles Per World Unit", Float) = 0.25
        _PixelResolution ("Pixels Per Tile", Float) = 64
        _AnimationFPS ("Animation Frames Per Second", Range(1, 30)) = 12
    }
    
    SubShader {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
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
            float _TilesPerUnit, _PixelResolution, _AnimationFPS;

            v2f vert (appdata v) {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // World coordinates keep the pixel size consistent on lakes and the ocean.
                float2 worldUV = mul(unity_ObjectToWorld, v.vertex).xz * _TilesPerUnit;
                o.uvWave = worldUV * _MainTex_ST.xy + _MainTex_ST.zw;
                o.uvShape = TRANSFORM_TEX(v.uv, _ShapeTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float resolution = max(1.0, _PixelResolution);
                float time = floor(_Time.y * _AnimationFPS) / max(1.0, _AnimationFPS) * _Speed;
                float2 pixelUV = (floor(i.uvWave * resolution) + 0.5) / resolution;
                float ripple = sin(pixelUV.x * _Frequency + time) * _Amplitude;
                float2 flowA = float2(time * 0.04, time * 0.025 + ripple);
                float2 flowB = float2(-time * 0.025, time * 0.035 - ripple);
                // Move by whole texels so the drifting highlights retain square pixel edges.
                flowA = floor(flowA * resolution) / resolution;
                flowB = floor(flowB * resolution) / resolution;
                fixed4 waveA = tex2D(_MainTex, pixelUV + flowA);
                fixed4 waveB = tex2D(_MainTex, pixelUV + flowB + float2(0.5, 0.25));
                fixed4 waveTex = lerp(waveA, waveB, 0.3);
                fixed4 waterColor = _Color * waveTex;

                fixed4 shape = tex2D(_ShapeTex, i.uvShape);
                float edge = max(_EdgeBlend, 0.001);
                float shapeAlpha = smoothstep(0.5 - edge, 0.5 + edge, shape.a);

                waterColor.a *= shapeAlpha;
                return waterColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
