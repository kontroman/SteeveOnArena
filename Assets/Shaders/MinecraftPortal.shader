Shader "Custom/MinecraftPortal"
{
    Properties
    {
        _MainTex ("Portal Texture", 2D) = "purple" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DistortionAmount ("Distortion Amount", Range(0, 0.2)) = 0.1
        _Speed ("Animation Speed", Range(0, 2)) = 0.5
        _GlowColor ("Glow Color", Color) = (0.5, 0.1, 0.8, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.35
        _EdgeThickness ("Edge Thickness", Range(0, 0.2)) = 0.05
        _EdgeColor ("Edge Color", Color) = (0.8, 0.2, 1, 1)
        _PixelResolution ("Pixels Per Tile", Float) = 64
        _AnimationFPS ("Animation Frames Per Second", Range(1, 30)) = 12
        _Opacity ("Opacity", Range(0, 1)) = 0.9
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
                float2 edgeUV : TEXCOORD1;
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
            float _PixelResolution, _AnimationFPS, _Opacity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.edgeUV = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float resolution = max(1.0, _PixelResolution);
                float time = floor(_Time.y * _AnimationFPS) / max(1.0, _AnimationFPS) * _Speed;
                float2 uv = (floor(i.uv * resolution) + 0.5) / resolution;
                float2 distortion = float2(sin(uv.y * 6.283 + time), cos(uv.x * 6.283 - time)) * _DistortionAmount;
                float2 flowA = floor((float2(time * 0.025, -time * 0.05) + distortion) * resolution) / resolution;
                float2 flowB = floor((float2(-time * 0.035, time * 0.03) - distortion) * resolution) / resolution;
                fixed4 first = tex2D(_MainTex, uv + flowA);
                fixed4 second = tex2D(_MainTex, uv + flowB + float2(0.5, 0.25));
                fixed4 portalColor = lerp(first, second, 0.25);
                float pulse = 0.5 + 0.5 * sin(time * 2.0);
                portalColor.rgb *= 0.9 + 0.15 * pulse;
                portalColor.rgb += _GlowColor.rgb * _GlowIntensity * (0.15 + 0.2 * pulse) * portalColor.b;
                float2 edgeDistance = min(i.edgeUV, 1.0 - i.edgeUV);
                float edge = smoothstep(0.0, max(0.001, _EdgeThickness), min(edgeDistance.x, edgeDistance.y));
                portalColor.rgb = lerp(_EdgeColor.rgb, portalColor.rgb, edge);
                portalColor.a = _Opacity * (0.94 + 0.06 * pulse);
                
                return portalColor;
            }
            ENDCG
        }
    }
}
