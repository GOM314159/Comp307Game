Shader "Custom/ColorShiftPortal"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _RippleSpeed ("Ripple Speed", Float) = 1.0
        _RippleAmplitude ("Ripple Amplitude", Float) = 0.1
        _RippleFrequency ("Ripple Frequency", Float) = 4.0
        _ColorSpeed ("Color Cycle Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ============================================================
            // PROPERTIES & UNIFORMS
            // ============================================================
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _RippleSpeed;
            float _RippleAmplitude;
            float _RippleFrequency;
            float _ColorSpeed;

            // ============================================================
            // VERTEX SHADER
            // ============================================================
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Calculate distance from center (0.5, 0.5) in UV space
                float2 centerUV = v.uv - float2(0.5, 0.5);
                float distFromCenter = length(centerUV);

                // Ripple: vertical displacement based on distance and time
                // Sine wave travels outward from center
                float ripple = sin(distFromCenter * _RippleFrequency - _Time.y * _RippleSpeed) * _RippleAmplitude;

                // Apply ripple to vertex position (along normal)
                v.vertex.xyz += v.normal * ripple;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            // ============================================================
            // COLOR SPACE CONVERSION: HSV ↔ RGB
            // ============================================================

            // HSV to RGB conversion
            // Input: H (0-1 = 0-360°), S (0-1), V (0-1)
            // Output: RGB (0-1 range)
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            // ============================================================
            // FRAGMENT SHADER
            // ============================================================
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample base texture for blending
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // ========== DISTANCE FROM CENTER ==========
                // Calculate how far this pixel is from the center (0.5, 0.5)
                float2 centerUV = i.uv - float2(0.5, 0.5);
                float distFromCenter = length(centerUV);

                // ========== RAINBOW HUE CYCLE ==========
                // Full spectrum: Red → Orange → Yellow → Green → Cyan → Blue → Magenta → Red
                // Smooth oscillation: 0→1→0 over time
                float colorCycle = abs(sin(_Time.y * _ColorSpeed * 3.14159));
                float hue = colorCycle; // Full range 0 to 1

                // ========== SATURATION GRADIENT: WHITE CENTER → RAINBOW EDGES ==========
                // At center (distFromCenter ≈ 0): saturation = 0 (white/desaturated)
                // At edges (distFromCenter ≈ 0.5): saturation = 1 (full color)
                // Uses smoothstep for smooth transition
                float saturation = smoothstep(0.0, 0.5, distFromCenter);

                // ========== BRIGHTNESS: CONSTANT ==========
                // Keep at 1.0 for full brightness (no black)
                float brightness = 1.0;

                // Create HSV color and convert to RGB
                float3 hsvColor = float3(hue, saturation, brightness);
                float3 rgbColor = hsv2rgb(hsvColor);

                // ========== FINAL OUTPUT ==========
                // Blend portal color with base texture
                fixed4 finalColor;
                finalColor.rgb = lerp(texColor.rgb, rgbColor, 0.9);
                finalColor.a = 1.0; // Full opacity (no transparency)

                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
