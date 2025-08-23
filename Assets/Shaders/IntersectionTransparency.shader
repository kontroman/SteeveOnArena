Shader "Custom/IntersectionTransparency" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _CutoutRadius ("Cutout Radius", Float) = 0.5
        _CutoutPosition ("Cutout Position", Vector) = (0,0,0,0)
        _Transparency ("Transparency", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        
        CGPROGRAM
        #pragma surface surf Lambert alpha
        
        sampler2D _MainTex;
        float _CutoutRadius;
        float3 _CutoutPosition;
        float _Transparency;
        
        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
        };
        
        void surf (Input IN, inout SurfaceOutput o) {
            half4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            
            // Calculate distance from cutout sphere
            float distance = length(IN.worldPos - _CutoutPosition);
            if (distance < _CutoutRadius) {
                o.Alpha = _Transparency;
            } else {
                o.Alpha = 1.0;
            }
        }
        ENDCG
    } 
    FallBack "Transparent"
}