Shader "Custom/MinecraftPortal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "purple" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DistortionAmount ("Distortion Amount", Range(0, 0.2)) = 0.1
        _Speed ("Animation Speed", Range(0, 2)) = 0.5
        _GlowColor ("Glow Color", Color) = (0.5, 0.1, 0.8, 1)
        _GlowIntensity ("Glow Intensity", Range(1, 10)) = 3
        _EdgeThickness ("Edge Thickness", Range(0, 0.2)) = 0.05
        _EdgeColor ("Edge Color", Color) = (0.8, 0.2, 1, 1)
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float _DistortionAmount;
            float _Speed;
            float4 _GlowColor;
            float _GlowIntensity;
            float _EdgeThickness;
            float4 _EdgeColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 noiseUV = i.uv + _Time.y * _Speed * 0.2;
                float noise = tex2D(_NoiseTex, noiseUV).r;
                
                float2 distortedUV = i.uv + float2(
                    sin(_Time.y * _Speed + i.uv.y * 10) * 0.05 * noise,
                    cos(_Time.y * _Speed + i.uv.x * 10) * 0.05 * noise
                ) * _DistortionAmount;
                
                fixed4 portalColor = tex2D(_MainTex, distortedUV);
                
                float glow = sin(_Time.y * _Speed * 2 + i.uv.x * 5) * 0.5 + 0.5;
                portalColor += _GlowColor * glow * _GlowIntensity * noise;
                
                float edge = smoothstep(0, _EdgeThickness, i.uv.x) * 
                            smoothstep(0, _EdgeThickness, i.uv.y) * 
                            smoothstep(1, 1 - _EdgeThickness, i.uv.x) * 
                            smoothstep(1, 1 - _EdgeThickness, i.uv.y);
                
                portalColor.rgb = lerp(_EdgeColor.rgb, portalColor.rgb, edge);
                
                portalColor.a *= (0.8 + 0.2 * sin(_Time.y * _Speed * 3));
                
                return portalColor;
            }
            ENDCG
        }
    }
}