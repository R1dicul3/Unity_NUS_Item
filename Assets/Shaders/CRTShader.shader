Shader "Universal Render Pipeline/CRT"
{
    Properties
    {
        [HideInInspector] _CRTIntensity("CRT Intensity", Range(0, 1)) = 0.5
        [HideInInspector] _ScanlineIntensity("Scanline Intensity", Range(0, 1)) = 0.3
        [HideInInspector] _CurveIntensity("Curve Intensity", Range(0, 1)) = 0.15
        [HideInInspector] _ChromaticAberration("Chromatic Aberration", Range(0, 1)) = 0.08
        [HideInInspector] _VignetteIntensity("Vignette Intensity", Range(0, 1)) = 0.4
        [HideInInspector] _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "CRT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _CRTIntensity;
            float _ScanlineIntensity;
            float _CurveIntensity;
            float _ChromaticAberration;
            float _VignetteIntensity;
            float _NoiseIntensity;

            // 简单伪随机函数
            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 桶形畸变
            float2 ApplyCurve(float2 uv, float intensity)
            {
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered * (1.0 - intensity);
                float r2 = dot(offset, offset);
                float r4 = r2 * r2;
                float distortion = 1.0 + intensity * r2 + intensity * r4 * 0.5;
                centered *= distortion;
                return centered * 0.5 + 0.5;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // ===== 1. 原始颜色采样 =====
                float3 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // 如果 CRT Intensity 为 0，直接返回原图（优化）
                if (_CRTIntensity <= 0.001)
                {
                    return float4(originalColor, 1.0);
                }

                // ===== 2. UV 曲率畸变 =====
                float2 curvedUV = ApplyCurve(uv, _CurveIntensity * _CRTIntensity);

                // 处理越界采样（畸变后 UV 可能超出 [0,1]）
                float2 borderMask = step(0.0, curvedUV) * step(curvedUV, 1.0);
                float inBounds = borderMask.x * borderMask.y;

                // ===== 3. RGB 色散 =====
                float3 crtColor = 0.0;
                if (_ChromaticAberration * _CRTIntensity > 0.001)
                {
                    float2 centerOffset = curvedUV - 0.5;
                    float2 abOffset = centerOffset * _ChromaticAberration * _CRTIntensity * 0.05;

                    float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, curvedUV + abOffset).r;
                    float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, curvedUV).g;
                    float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, curvedUV - abOffset).b;
                    crtColor = float3(r, g, b);
                }
                else
                {
                    crtColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, curvedUV).rgb;
                }

                // 越界区域显示黑色
                crtColor *= inBounds;

                // ===== 4. 扫描线 =====
                if (_ScanlineIntensity * _CRTIntensity > 0.001)
                {
                    float scanline = sin(curvedUV.y * 800.0) * 0.5 + 0.5;
                    scanline = lerp(1.0, scanline, _ScanlineIntensity * _CRTIntensity);
                    crtColor *= scanline;

                    // 添加淡淡的水平线（模拟像素间隙）
                    float pixelLine = sin(curvedUV.y * 400.0) * 0.5 + 0.5;
                    pixelLine = lerp(1.0, 0.85 + 0.15 * pixelLine, _ScanlineIntensity * _CRTIntensity * 0.5);
                    crtColor *= pixelLine;
                }

                // ===== 5. 暗角 =====
                if (_VignetteIntensity * _CRTIntensity > 0.001)
                {
                    float2 vignetteUV = curvedUV * 2.0 - 1.0;
                    float vignette = 1.0 - dot(vignetteUV, vignetteUV) * 0.5;
                    vignette = saturate(vignette);
                    vignette = pow(vignette, lerp(1.0, 2.5, _VignetteIntensity * _CRTIntensity));
                    crtColor *= vignette;
                }

                // ===== 6. 噪点 =====
                if (_NoiseIntensity * _CRTIntensity > 0.001)
                {
                    float noise = Random(uv + _Time.y * 0.1);
                    noise = noise * 2.0 - 1.0;
                    crtColor += noise * _NoiseIntensity * _CRTIntensity * 0.15;
                }

                // ===== 7. 复古色偏（可选，轻微偏绿偏暗） =====
                float3 tint = float3(0.95, 1.0, 0.92);
                crtColor = lerp(crtColor, crtColor * tint, _CRTIntensity * 0.2);

                // ===== 8. 与原图混合 =====
                float3 finalColor = lerp(originalColor, crtColor, _CRTIntensity * inBounds);

                return float4(saturate(finalColor), 1.0);
            }
            ENDHLSL
        }
    }
}
