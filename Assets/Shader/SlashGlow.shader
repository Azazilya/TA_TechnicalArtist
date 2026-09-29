Shader "TA VFX/Slash Glow"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Emission Intensity", Range(0, 20)) = 6
        _Opacity ("Opacity", Range(0, 1)) = 1
        _TailLength ("Tail Fade Length", Range(0.01, 1)) = 0.65
        _TailSharpness ("Tail Sharpness", Range(0.2, 8)) = 2.5
        [Toggle] _ReverseTail ("Reverse Tail", Float) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _DissolveSoftness ("Dissolve Softness", Range(0.001, 0.5)) = 0.12
        _NoiseScale ("Noise Scale", Range(0.1, 30)) = 7
        _NoiseSpeed ("Noise Speed", Range(-10, 10)) = 1.5
        _EdgeGlow ("Dissolve Edge Glow", Range(0, 10)) = 3
        _GlowWidth ("Outer Glow Width", Range(0, 1)) = 0.22
        _TipLength ("Pointed Tip Length", Range(0.01, 0.5)) = 0.12
        _TipSoftness ("Pointed Tip Softness", Range(0.001, 0.15)) = 0.02
        _Thickness ("Visual Thickness", Range(0.05, 1)) = 0.8
        [Toggle] _SweepEnabled ("Sweep Enabled", Float) = 0
        _SweepProgress ("Sweep Progress", Range(0, 1)) = 1
        _SweepSoftness ("Sweep Head Width", Range(0.01, 0.4)) = 0.12
        [Toggle] _SweepReverse ("Reverse Sweep", Float) = 1
        _TrailOpacity ("Faded Trail Opacity", Range(0, 1)) = 0.28
        [Toggle] _UseSweepGradient ("Use Three Color Sweep", Float) = 0
        [HDR] _SweepStartColor ("Sweep Start Color", Color) = (0, 0.1, 1, 1)
        [HDR] _SweepMiddleColor ("Sweep Middle Color", Color) = (0, 1, 0.1, 1)
        [HDR] _SweepEndColor ("Sweep End Color", Color) = (1, 0.02, 0, 1)
        _GlobalFade ("Global Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            Name "SLASH_GLOW"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 localPos : TEXCOORD1; };

            fixed4 _CoreColor, _GlowColor;
            fixed4 _SweepStartColor, _SweepMiddleColor, _SweepEndColor;
            float _Intensity, _Opacity, _TailLength, _TailSharpness, _ReverseTail;
            float _Dissolve, _DissolveSoftness, _NoiseScale, _NoiseSpeed, _EdgeGlow, _GlowWidth;
            float _TipLength, _TipSoftness, _Thickness;
            float _SweepEnabled, _SweepProgress, _SweepSoftness, _SweepReverse, _TrailOpacity;
            float _UseSweepGradient, _GlobalFade;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1,0)), f.x),
                            lerp(hash21(i + float2(0,1)), hash21(i + 1), f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV.x determines which end becomes the sharp/fading tail.
                float along = lerp(i.uv.x, 1.0 - i.uv.x, step(0.5, _ReverseTail));
                float tail = pow(saturate(along / max(_TailLength, 0.001)), _TailSharpness);

                // Narrows the visible width toward both UV ends, producing an
                // actual pointed silhouette even when the source mesh is blunt.
                float distanceToEnd = min(i.uv.x, 1.0 - i.uv.x);
                float allowedHalfWidth = saturate(distanceToEnd / max(_TipLength, 0.001)) * 0.5;
                float distanceFromCenter = abs(i.uv.y - 0.5);
                float thicknessHalfWidth = 0.5 * _Thickness;
                float thicknessMask = 1.0 - smoothstep(
                    max(thicknessHalfWidth - _TipSoftness, 0.0),
                    thicknessHalfWidth + _TipSoftness,
                    distanceFromCenter);
                float pointedTip = 1.0 - smoothstep(
                    max(allowedHalfWidth - _TipSoftness, 0.0),
                    allowedHalfWidth + _TipSoftness,
                    distanceFromCenter);

                float2 noiseUV = i.uv * _NoiseScale + float2(_Time.y * _NoiseSpeed, 0);
                float noise = valueNoise(noiseUV);
                float threshold = _Dissolve * (1.0 + _DissolveSoftness) - _DissolveSoftness;
                float visible = smoothstep(threshold, threshold + _DissolveSoftness, noise);
                float edge = saturate(1.0 - abs(noise - threshold) / max(_DissolveSoftness, 0.001));

                // Bright center and controlled outer bloom. The body stays tight and
                // angular rather than spreading too wide like a soft curved fog.
                float center = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                float core = smoothstep(max(_GlowWidth * 1.4, 0.12), 1.0, center);
                float rim = smoothstep(_GlowWidth, 1.0, center) * (1.0 - core);
                float sweepCoordinate = lerp(i.uv.x, 1.0 - i.uv.x, step(0.5, _SweepReverse));
                float revealed = 1.0 - smoothstep(_SweepProgress, _SweepProgress + 0.01, sweepCoordinate);
                float sweepHead = 1.0 - smoothstep(0.0, _SweepSoftness, abs(sweepCoordinate - _SweepProgress));
                float sweepAlpha = revealed * lerp(_TrailOpacity, 1.0, sweepHead);
                sweepAlpha = lerp(1.0, sweepAlpha, step(0.5, _SweepEnabled));
                fixed3 color = lerp(_GlowColor.rgb, _CoreColor.rgb, core);
                fixed3 firstHalf = lerp(_SweepStartColor.rgb, _SweepMiddleColor.rgb, saturate(sweepCoordinate * 2.0));
                fixed3 secondHalf = lerp(_SweepMiddleColor.rgb, _SweepEndColor.rgb, saturate((sweepCoordinate - 0.5) * 2.0));
                fixed3 sweepGradient = lerp(firstHalf, secondHalf, step(0.5, sweepCoordinate));
                color = lerp(color, sweepGradient, step(0.5, _UseSweepGradient));
                color *= lerp(1.0, 1.5, sweepHead * step(0.5, _SweepEnabled));
                color += _GlowColor.rgb * edge * _EdgeGlow * rim;
                float alpha = tail * pointedTip * thicknessMask * visible * sweepAlpha * _Opacity * _GlobalFade * lerp(0.55, 1.0, core);
                return fixed4(color * _Intensity, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
