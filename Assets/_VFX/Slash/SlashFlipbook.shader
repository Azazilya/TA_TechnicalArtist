Shader "TA VFX/Slash Flipbook"
{
    Properties
    {
        [MainTexture] _BaseMap ("Slash Atlas", 2D) = "white" {}
        [HDR] _InnerColor ("Inner Core Color", Color) = (0.65, 0.95, 1, 1)
        [HDR] _OuterColor ("Outer Core Color", Color) = (0.05, 0.45, 1, 1)
        _InnerIntensity ("Inner Core Intensity", Range(0, 12)) = 3
        _OuterIntensity ("Outer Core Intensity", Range(0, 12)) = 1.5
        _CoreThreshold ("Inner Core Width", Range(0, 1)) = 0.55
        _Size ("Size In Particle Quad", Range(0.1, 2)) = 1
        _Thickness ("Slash Thickness", Range(0.1, 2)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 1
        [HideInInspector] _Tiles ("Atlas Tiles", Vector) = (5, 4, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SlashFlipbook"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _InnerColor;
                half4 _OuterColor;
                half _InnerIntensity;
                half _OuterIntensity;
                half _CoreThreshold;
                half _Size;
                half _Thickness;
                half _Opacity;
                float4 _Tiles;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Particle Texture Sheet Animation has already selected an atlas cell.
                // Convert back to cell-local UV so size/thickness never bleed into
                // neighbouring slash frames.
                float2 tiles = max(_Tiles.xy, float2(1.0, 1.0));
                float2 gridUV = input.uv * tiles;
                float2 cell = floor(gridUV);
                float2 localUV = frac(gridUV);
                float2 adjustedUV = (localUV - 0.5) /
                    float2(max(_Size, 0.01), max(_Thickness, 0.01)) + 0.5;
                half inside = step(0.0, adjustedUV.x) * step(adjustedUV.x, 1.0) *
                    step(0.0, adjustedUV.y) * step(adjustedUV.y, 1.0);
                float2 sampleUV = (cell + saturate(adjustedUV)) / tiles;

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sampleUV);
                half coverage = tex.a * inside;
                half luminance = dot(tex.rgb, half3(0.2126, 0.7152, 0.0722));
                half coreMask = smoothstep(_CoreThreshold, min(1.0, _CoreThreshold + 0.25),
                    max(coverage, luminance));
                half3 coreColor = lerp(
                    _OuterColor.rgb * _OuterIntensity,
                    _InnerColor.rgb * _InnerIntensity,
                    coreMask);
                half coreAlpha = lerp(_OuterColor.a, _InnerColor.a, coreMask);
                half alpha = coverage * input.color.a * coreAlpha * _Opacity;
                half3 emission = tex.rgb * input.color.rgb * coreColor;
                return half4(emission, alpha);
            }
            ENDHLSL
        }
    }
}
