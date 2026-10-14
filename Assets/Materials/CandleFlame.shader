// Visible candle flame + soft glow. Put it on a Quad at the wick tip.
// Unlit, additive, always faces the camera. Works in Built-in and URP.
// (HDRP needs a Shader Graph equivalent.)
Shader "Custom/CandleFlame"
{
    Properties
    {
        _ColorDim      ("Dim Colour (outer flame)",    Color) = (1.00, 0.34, 0.08, 1)
        _ColorBright   ("Bright Colour (outer flame)", Color) = (1.00, 0.68, 0.30, 1)
        _CoreColor     ("Hot Core Colour",             Color) = (1.00, 0.92, 0.75, 1)
        _Intensity     ("Intensity",                   Range(0, 8)) = 2.5
        _HaloStrength  ("Halo Strength",               Range(0, 1)) = 0.35
        _Flicker       ("Flicker (set by CandleFlicker)", Float) = 1
        _Bend          ("Bend (set by CandleFlicker)",    Range(-1, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One      // additive
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _ColorDim, _ColorBright, _CoreColor;
            float  _Intensity, _HaloStrength, _Flicker, _Bend;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                // Billboard: rebuild the quad from the camera's right/up axes
                float3 centre = unity_ObjectToWorld._m03_m13_m23;
                float2 scale  = float2(length(unity_ObjectToWorld._m00_m10_m20),
                                       length(unity_ObjectToWorld._m01_m11_m21));
                float3 right  = UNITY_MATRIX_V[0].xyz;
                float3 up     = UNITY_MATRIX_V[1].xyz;
                float3 worldPos = centre + right * (v.vertex.x * scale.x)
                                         + up    * (v.vertex.y * scale.y);
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float k = _Flicker;

                float u = (i.uv.x - 0.5) * 2.0;                      // -1..1 across the quad
                float t = saturate((i.uv.y - 0.12) / (0.7 * k));     // 0 at flame base, 1 at tip
                u -= _Bend * 0.25 * t * t;                           // tip leans with the draught

                // Teardrop: round at the base, pointed at the tip
                float halfWidth = 0.32 * sin(3.14159265 * pow(t, 0.6)) + 1e-3;
                float body = 1.0 - smoothstep(halfWidth * 0.2, halfWidth, abs(u));

                float3 outer = lerp(_ColorDim.rgb, _ColorBright.rgb, saturate((k - 0.7) / 0.6));
                float  core  = body * body * body * (1.0 - 0.6 * t); // hottest near the base
                float3 flame = lerp(outer, _CoreColor.rgb, core) * body;

                // Soft glow around the lower part of the flame
                float2 hp   = (i.uv - float2(0.5, 0.3)) * 2.0;
                float  halo = exp(-dot(hp, hp) * 4.0) * _HaloStrength
                            * (1.0 - smoothstep(0.7, 1.0, length(hp)));

                float3 col = (flame * _Intensity + outer * halo) * k;
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
