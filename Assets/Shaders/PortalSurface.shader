Shader "Strings and Statutes/Portal Surface"
{
    Properties
    {
        [Header(View)]
        [NoScaleOffset] _PortalTex ("Portal view (set by script)", 2D) = "black" {}
        _HasView         ("Has a live view", Range(0,1)) = 0
        _ViewTint        ("View tint", Color) = (1,1,1,1)

        [Header(Edge)]
        _EdgeWidth       ("Edge width", Range(0.001, 0.5)) = 0.085
        _EdgeSoftness    ("Edge softness", Range(0.01, 1)) = 0.45
        _EdgeIntensity   ("Edge intensity", Range(0, 1)) = 0.95
        _ColorSpeed      ("Colour cycle speed", Float) = 0.18
        _ColorSpin       ("Colour spread around rim", Range(0, 4)) = 1.0
        _EdgeEmission    ("Edge emission", Range(0, 8)) = 2.2

        [Header(Distortion)]
        _EdgeDistort     ("Edge refraction", Range(0, 0.1)) = 0.012
        _RippleSpeed     ("Ripple speed", Float) = 1.6
        _RippleFrequency ("Ripple frequency", Float) = 9.0
        _RippleAmplitude ("Ripple amplitude", Range(0, 0.1)) = 0.006
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry+1"
        }

        LOD 200

        Pass
        {
            Name "PortalUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   PortalVertex
            #pragma fragment PortalFragment
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_PortalTex);
            SAMPLER(sampler_PortalTex);

            CBUFFER_START(UnityPerMaterial)
                float  _HasView;
                float4 _ViewTint;
                float  _EdgeWidth;
                float  _EdgeSoftness;
                float  _EdgeIntensity;
                float  _ColorSpeed;
                float  _ColorSpin;
                float  _EdgeEmission;
                float  _EdgeDistort;
                float  _RippleSpeed;
                float  _RippleFrequency;
                float  _RippleAmplitude;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 positionNDC : TEXCOORD0;
                float2 uv          : TEXCOORD1;
                float  fogCoord    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 Hsv2Rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx,
                                  clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            Varyings PortalVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // A little surface tension on the membrane. Kept
                // small on purpose: the view behind it is sampled
                // in screen space, so large displacement would
                // slide the image off the hole it belongs to.
                float2 c = input.uv - 0.5;

                float ripple = sin(length(c) * _RippleFrequency
                                   - _Time.y * _RippleSpeed)
                             * _RippleAmplitude;

                float3 positionOS = input.positionOS.xyz
                                  + input.normalOS * ripple;

                VertexPositionInputs p =
                    GetVertexPositionInputs(positionOS);

                output.positionCS  = p.positionCS;
                output.positionNDC = p.positionNDC;
                output.uv          = input.uv;
                output.fogCoord    = ComputeFogFactor(p.positionCS.z);

                return output;
            }

            half4 PortalFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // --- the hole ---
                // The destination was rendered from a camera placed
                // exactly where your eye would be if it were standing
                // behind the far portal, using this camera's own
                // projection. So the correct place to read that image
                // is the pixel's own screen position: the portal is a
                // window cut out of the frame, not a texture stuck to
                // a quad.
                float2 screenUV =
                    input.positionNDC.xy / input.positionNDC.w;

                // Rectangular rim - distance to the nearest border of
                // the surface, which follows a doorway rather than
                // assuming a disc.
                float2 toBorder = min(input.uv, 1.0 - input.uv);
                float  border   = min(toBorder.x, toBorder.y);

                float edge = 1.0 - smoothstep(
                    _EdgeWidth * (1.0 - _EdgeSoftness),
                    _EdgeWidth,
                    border);

                // The membrane bends light hardest where it meets the
                // frame, so push the lookup outward near the rim.
                float2 outward = normalize(input.uv - 0.5 + 1e-5);

                screenUV += outward * edge * _EdgeDistort;

                float3 view = SAMPLE_TEXTURE2D(
                    _PortalTex, sampler_PortalTex, screenUV).rgb
                    * _ViewTint.rgb;

                // --- the colour ---
                // Hue runs around the perimeter and drifts with time,
                // so every side of the frame shows a different part of
                // the spectrum at once rather than the whole ring
                // pulsing as one.
                float angle = atan2(input.uv.y - 0.5,
                                    input.uv.x - 0.5)
                            * 0.15915494;   // 1 / 2pi

                float hue = frac(angle * _ColorSpin
                                 + _Time.y * _ColorSpeed);

                float3 rim = Hsv2Rgb(float3(hue, 1.0, 1.0));

                // --- fallback ---
                // With no live view bound - unlinked, off-screen, or
                // being edited - it falls back to the white centre
                // with colour-shifting edges, so the portal never
                // reads as a black hole.
                float centreDist =
                    saturate(1.0 - border * 2.0);

                float3 fallback = Hsv2Rgb(float3(
                    hue,
                    smoothstep(0.0, 0.85, centreDist),
                    1.0));

                float3 base = lerp(fallback, view, saturate(_HasView));

                float3 colour = lerp(base, rim * _EdgeEmission,
                                     edge * _EdgeIntensity);

                colour = MixFog(colour, input.fogCoord);

                return half4(colour, 1.0);
            }
            ENDHLSL
        }

        // Portals occlude like solid geometry.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex   DepthVertex
            #pragma fragment DepthFragment
            #pragma target 3.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DVaryings DepthVertex(DAttributes input)
            {
                DVaryings output = (DVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFragment(DVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
