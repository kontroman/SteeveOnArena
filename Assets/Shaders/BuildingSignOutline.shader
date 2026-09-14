Shader "MineArena/BuildingSignOutline"
{
    Properties
    {
        [HDR] _BaseColor ("Gold color", Color) = (1, 0.67, 0.16, 1)
        _GlowIntensity ("Glow intensity", Range(1, 3)) = 1.5
        _Opacity ("Opacity", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            half4 _BaseColor;
            half _GlowIntensity;
            half _Opacity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                // A soft halo is drawn directly, so it also works without camera bloom.
                float distance = abs(i.uv.y - 0.5) * 0.74;
                float edgeAA = max(fwidth(distance), 0.001);
                half core = 1 - smoothstep(0.09 - edgeAA, 0.09 + edgeAA, distance);
                half halo = 1 - smoothstep(0.09, 0.37, distance);
                halo *= halo;
                half pulse = 0.94 + 0.06 * sin(_Time.y * 3);
                half alpha = max(core * _Opacity, halo * 0.28 * pulse);
                return half4(_BaseColor.rgb * _GlowIntensity * pulse, alpha);
            }
            ENDCG
        }
    }
}
