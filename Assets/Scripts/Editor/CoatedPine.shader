Shader "Strings and Statutes/Coated Pine"
{
    Properties
    {
        [Header(Cut)]
        _Scale              ("Object units per metre", Float) = 1.0
        _TrunkOffset        ("Trunk axis offset (YZ)", Vector) = (0.35, 0.12, 0, 0)
        _RingDensity        ("Rings per metre", Float) = 62.0

        [Header(Growth Rings)]
        _EarlyColor         ("Earlywood", Color) = (0.706, 0.541, 0.357, 1)
        _LateColor          ("Latewood", Color) = (0.400, 0.259, 0.141, 1)
        _LateWidth          ("Latewood width", Range(0.02, 0.6)) = 0.22
        _EarlyCurve         ("Earlywood ramp", Range(0.2, 6.0)) = 2.2

        [Header(Wander)]
        _WarpAmount         ("Warp amount", Range(0, 0.2)) = 0.045
        _WarpScaleAlong     ("Warp scale along grain", Float) = 0.9
        _WarpScaleAcross    ("Warp scale across grain", Float) = 7.0

        [Header(Fibre)]
        _FibreStrength      ("Fibre strength", Range(0, 1)) = 0.28
        _FibreAlong         ("Fibre scale along grain", Float) = 14.0
        _FibreAcross        ("Fibre scale across grain", Float) = 420.0

        [Header(Knots)]
        _KnotCount          ("Knot count", Range(0, 8)) = 3
        _KnotSeed           ("Knot seed", Float) = 3.0
        _KnotSize           ("Knot size", Range(0.01, 0.5)) = 0.075
        _KnotSpread         ("Knot spread along grain", Float) = 1.2
        _KnotRingDensity    ("Knot rings per metre", Float) = 210.0
        _KnotBlend          ("Knot blend", Range(0, 2)) = 1.25
        _KnotColor          ("Knot colour", Color) = (0.208, 0.125, 0.071, 1)
        _KnotDarken         ("Knot darkening", Range(0, 1)) = 0.75

        [Header(Relief)]
        _LateRelief         ("Latewood relief", Range(0, 1)) = 0.35
        _FibreRelief        ("Fibre relief", Range(0, 1)) = 0.55
        _ReliefStrength     ("Relief strength", Range(0, 4)) = 1.1
        _ReliefEpsilon      ("Relief sample step", Range(0.0002, 0.02)) = 0.0025

        [Header(Varnish)]
        _CoatSmoothness     ("Coat smoothness", Range(0, 1)) = 0.93
        _CoatStrength       ("Coat strength", Range(0, 1)) = 1.0
        _WetAmount          ("Wetting of the wood", Range(0, 1)) = 0.55
        _BaseSmoothness     ("Wood smoothness under coat", Range(0, 1)) = 0.30

        [Header(Scratches)]
        _ScratchDensity     ("Scratch density", Float) = 260.0
        _ScratchSharpness   ("Scratch sharpness", Range(1, 60)) = 34.0
        _ScratchAmount      ("Scratch amount", Range(0, 1)) = 0.35
        _ScratchRelief      ("Scratch relief", Range(0, 1)) = 0.12

        [Header(Other)]
        _OcclusionStrength  ("Crevice occlusion", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        LOD 300

        // =====================================================
        // FORWARD LIT
        // =====================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   CoatedPineVertex
            #pragma fragment CoatedPineFragment
            #pragma target 3.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // Turns on URP's second specular lobe. This is the
            // varnish: a thin smooth dielectric layer sitting on
            // top of the wood, with its own Fresnel and its own
            // roughness, which also attenuates what comes back
            // from the base underneath it.
            #define _CLEARCOAT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CoatedPineWood.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Scale;
                float4 _TrunkOffset;
                float  _RingDensity;
                float4 _EarlyColor;
                float4 _LateColor;
                float  _LateWidth;
                float  _EarlyCurve;
                float  _WarpAmount;
                float  _WarpScaleAlong;
                float  _WarpScaleAcross;
                float  _FibreStrength;
                float  _FibreAlong;
                float  _FibreAcross;
                float  _KnotCount;
                float  _KnotSeed;
                float  _KnotSize;
                float  _KnotSpread;
                float  _KnotRingDensity;
                float  _KnotBlend;
                float4 _KnotColor;
                float  _KnotDarken;
                float  _LateRelief;
                float  _FibreRelief;
                float  _ReliefStrength;
                float  _ReliefEpsilon;
                float  _CoatSmoothness;
                float  _CoatStrength;
                float  _WetAmount;
                float  _BaseSmoothness;
                float  _ScratchDensity;
                float  _ScratchSharpness;
                float  _ScratchAmount;
                float  _ScratchRelief;
                float  _OcclusionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CP_WoodParams CoatedPineParams()
            {
                CP_WoodParams w;
                w.trunkOffset     = _TrunkOffset.xy;
                w.ringDensity     = _RingDensity;
                w.lateWidth       = _LateWidth;
                w.earlyCurve      = _EarlyCurve;
                w.warpAmount      = _WarpAmount;
                w.warpScaleAlong  = _WarpScaleAlong;
                w.warpScaleAcross = _WarpScaleAcross;
                w.fibreStrength   = _FibreStrength;
                w.fibreAlong      = _FibreAlong;
                w.fibreAcross     = _FibreAcross;
                w.knotCount       = (int)round(_KnotCount);
                w.knotSeed        = _KnotSeed;
                w.knotSize        = _KnotSize;
                w.knotSpread      = _KnotSpread;
                w.knotRingDensity = _KnotRingDensity;
                w.knotBlend       = _KnotBlend;
                w.lateRelief      = _LateRelief;
                w.fibreRelief     = _FibreRelief;
                return w;
            }

            Varyings CoatedPineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs p =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs n =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS   = n.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.fogCoord   = ComputeFogFactor(p.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV,
                                   unity_LightmapST,
                                   output.lightmapUV);

                OUTPUT_SH(n.normalWS.xyz, output.vertexSH);

                return output;
            }

            half4 CoatedPineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                CP_WoodParams w = CoatedPineParams();

                // Everything is evaluated in the plank's own space,
                // scaled to metres, so the grain is a property of
                // the solid rather than of a UV layout.
                float3 p = input.positionOS * _Scale;

                // ---- the wood ----
                float knotMask;
                float ring  = CP_RingCoord(p, w, knotMask);
                float late  = CP_LateWood(ring, w);
                float fibre = CP_Fibre(p, w);

                float3 albedo = lerp(_EarlyColor.rgb,
                                     _LateColor.rgb, late);

                albedo *= 1.0 - (fibre - 0.5) * _FibreStrength;

                albedo = lerp(albedo, _KnotColor.rgb,
                              saturate(knotMask * _KnotDarken));

                // Varnish soaks in and wets the timber: it drops
                // the value and lifts the saturation, the same way
                // water on a dry board does. Squaring the albedo is
                // the cheap honest version of that.
                albedo = lerp(albedo, albedo * albedo, _WetAmount);

                // ---- relief ----
                // Forward differences of the height field. A
                // gradient transforms like a normal, so the
                // object-to-world normal matrix is the correct one
                // to carry it across.
                float e  = _ReliefEpsilon;
                float h0 = CP_Height(p, w);
                float3 gradOS = float3(
                    CP_Height(p + float3(e, 0, 0), w) - h0,
                    CP_Height(p + float3(0, e, 0), w) - h0,
                    CP_Height(p + float3(0, 0, e), w) - h0) / e;

                float scratch = CP_Scratches(p, _ScratchDensity,
                                             _ScratchSharpness)
                              * _ScratchAmount;

                float3 normalWS = normalize(input.normalWS);

                float3 gradWS =
                    TransformObjectToWorldNormal(gradOS);

                // Keep only the part lying in the surface plane -
                // that is the surface gradient, and subtracting it
                // from the normal is Blinn's perturbation.
                float3 surfGrad =
                    gradWS - dot(gradWS, normalWS) * normalWS;

                normalWS = normalize(
                    normalWS - surfGrad * _ReliefStrength * 0.01);

                // ---- the coat ----
                // Scratches belong to the varnish. They roughen the
                // coat and nick its surface; they do not repaint the
                // wood underneath.
                float coatSmooth = _CoatSmoothness
                                 * (1.0 - scratch * 0.8);

                // A scratch is a groove, so it gets a real gradient
                // of its own rather than a nudge along an arbitrary
                // axis. Same finite-difference trick as the grain,
                // projected into the surface plane the same way.
                float s0 = CP_Scratches(p, _ScratchDensity,
                                        _ScratchSharpness);

                float3 scratchGradOS = float3(
                    CP_Scratches(p + float3(e, 0, 0),
                        _ScratchDensity, _ScratchSharpness) - s0,
                    CP_Scratches(p + float3(0, e, 0),
                        _ScratchDensity, _ScratchSharpness) - s0,
                    CP_Scratches(p + float3(0, 0, e),
                        _ScratchDensity, _ScratchSharpness) - s0) / e;

                float3 scratchGradWS =
                    TransformObjectToWorldNormal(scratchGradOS);

                float3 scratchSurfGrad = scratchGradWS
                    - dot(scratchGradWS, normalWS) * normalWS;

                normalWS = normalize(normalWS
                    - scratchSurfGrad
                      * _ScratchRelief * _ScratchAmount * 0.01);

                // Crevices between the rings catch less ambient
                // light. Latewood sits proud, so the earlywood
                // troughs are what darken.
                float occlusion = lerp(1.0,
                    saturate(0.55 + late * 0.45),
                    _OcclusionStrength);

                // ---- hand it to URP ----
                SurfaceData surface = (SurfaceData)0;
                surface.albedo             = albedo;
                surface.metallic           = 0.0;
                surface.specular           = 0.0;
                surface.smoothness         = _BaseSmoothness;
                surface.normalTS           = float3(0, 0, 1);
                surface.emission           = 0.0;
                surface.occlusion          = occlusion;
                surface.alpha              = 1.0;
                surface.clearCoatMask      = _CoatStrength;
                surface.clearCoatSmoothness= coatSmooth;

                InputData lighting = (InputData)0;
                lighting.positionWS      = input.positionWS;
                lighting.normalWS        = normalWS;
                lighting.viewDirectionWS =
                    SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) || defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    lighting.shadowCoord =
                        TransformWorldToShadowCoord(input.positionWS);
                #else
                    lighting.shadowCoord = float4(0, 0, 0, 0);
                #endif

                lighting.fogCoord = input.fogCoord;
                lighting.bakedGI  = SAMPLE_GI(input.lightmapUV,
                                              input.vertexSH,
                                              normalWS);
                lighting.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 colour = UniversalFragmentPBR(lighting, surface);

                colour.rgb = MixFog(colour.rgb, input.fogCoord);

                return colour;
            }
            ENDHLSL
        }

        // =====================================================
        // SHADOW CASTER
        // =====================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowVertex
            #pragma fragment ShadowFragment
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                float3 normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir =
                        normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDir));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z,
                        UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z,
                        UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;

                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =====================================================
        // DEPTH ONLY
        // =====================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthVertex
            #pragma fragment DepthFragment
            #pragma target 3.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }

            half4 DepthFragment(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =====================================================
        // DEPTH NORMALS
        // =====================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma target 3.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DNVaryings DepthNormalsVertex(DNAttributes input)
            {
                DNVaryings output = (DNVaryings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 DepthNormalsFragment(DNVaryings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS),
                             0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
