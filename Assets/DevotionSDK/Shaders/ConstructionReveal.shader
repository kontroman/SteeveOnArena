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
        _BlockSize("Block Size", Float) = 1
        _BlockOffsetSteps("Block Offset Steps", Range(0,4)) = 1
        _BlockNoiseScale("Block Noise Scale", Float) = 1
        _BlockRandomSeed("Block Random Seed", Float) = 0
        _BlockClipPadding("Block Clip Padding", Float) = 0.01
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
        float _BlockSize;
        float _BlockOffsetSteps;
        float _BlockNoiseScale;
        float _BlockRandomSeed;
        float _BlockClipPadding;
        float4 _RevealBoundsMin;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_EmissionMap;
            float3 worldPos;
        };

        float Hash21(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }

        float ComputeBlockOffset(float3 worldPos)
        {
            const float minBlockSize = 0.0001f;
            if (_BlockOffsetSteps <= 0.0f || _BlockSize <= minBlockSize)
                return 0.0f;

            const float blockSize = max(_BlockSize, minBlockSize);
            float2 localXZ = (worldPos.xz - _RevealBoundsMin.xz) / blockSize;
            float2 cell = floor(localXZ);

            float2 noiseCoord = cell;
            if (_BlockNoiseScale > minBlockSize)
            {
                noiseCoord *= _BlockNoiseScale;
            }
            noiseCoord += _BlockRandomSeed;

            float noiseValue = Hash21(noiseCoord);
            float offsetIndex = round(lerp(-_BlockOffsetSteps, _BlockOffsetSteps, noiseValue));
            return offsetIndex * blockSize;
        }

        float ComputeRevealClip(float3 worldPos, float blockOffset)
        {
            const float minBlockSize = 0.0001f;
            if (_BlockOffsetSteps <= 0.0f || _BlockSize <= minBlockSize)
            {
                float clipHeight = _RevealHeight - blockOffset;
                return clipHeight - worldPos.y;
            }

            float blockSize = max(_BlockSize, minBlockSize);
            float revealHeight = _RevealHeight - blockOffset;
            float localY = worldPos.y - _RevealBoundsMin.y;
            float blockIndex = floor(localY / blockSize);
            float blockTop = (blockIndex + 1.0f) * blockSize + _RevealBoundsMin.y;
            float padding = clamp(max(_BlockClipPadding, blockSize * 0.01f), 0.0f, blockSize * 0.49f);
            float threshold = blockTop - padding;
            return revealHeight - threshold;
        }

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

            float blockOffset = ComputeBlockOffset(IN.worldPos);
            float clipValue = ComputeRevealClip(IN.worldPos, blockOffset);
            clip(clipValue);

            if (_BlockOffsetSteps <= 0.0f && _RevealFeather > 0.0001f)
            {
                float fade = saturate(clipValue / _RevealFeather);
                o.Alpha *= fade;
            }
        }
        ENDCG
    }

    FallBack "Standard"
}
