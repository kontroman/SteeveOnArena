Shader "Devotion/ConstructionReveal"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _MainTex("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _RevealHeight("Reveal Height", Float) = 999
        _RevealFeather("Reveal Feather", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _EmissionMap;

        fixed4 _BaseColor;
        half _Metallic;
        half _Smoothness;
        half _NormalScale;
        fixed4 _EmissionColor;
        float _RevealHeight;
        float _RevealFeather;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_EmissionMap;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _BaseColor;
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Occlusion = 1.0;

            half3 normalTex = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            o.Normal = normalTex * _NormalScale;

            fixed3 emissionTex = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb;
            o.Emission = emissionTex * _EmissionColor.rgb;

            float clipDistance = _RevealHeight - IN.worldPos.y;
            clip(clipDistance);

            if (_RevealFeather > 0.0001f)
            {
                float fade = saturate(clipDistance / _RevealFeather);
                o.Alpha *= fade;
            }
        }
        ENDCG
    }

    FallBack "Standard"
}
