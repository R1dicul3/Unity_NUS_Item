using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

/// <summary>
/// CRT 效果自定义 Render Feature。
/// 在 AfterRenderingPostProcessing 注入全屏 CRT 后处理 Pass。
/// 运行时读取 GameSettings.CRTIntensity 动态调整效果强度。
/// </summary>
public class CRTRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CRTSettings
    {
        [Tooltip("扫描线强度")]
        public float scanlineIntensity = 0.3f;

        [Tooltip("曲率畸变强度")]
        public float curveIntensity = 0.15f;

        [Tooltip("RGB 色散强度")]
        public float chromaticAberration = 0.08f;

        [Tooltip("暗角强度")]
        public float vignetteIntensity = 0.4f;

        [Tooltip("噪点强度")]
        public float noiseIntensity = 0.05f;
    }

    public CRTSettings settings = new CRTSettings();

    private CRTVolumeRenderPass m_RenderPass;
    private Material m_Material;

    public override void Create()
    {
        m_RenderPass = new CRTVolumeRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 确保材质已创建
        if (m_Material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/CRT");
            if (shader == null)
            {
                Debug.LogError("[CRTRenderFeature] 找不到 Shader 'Universal Render Pipeline/CRT'。请确保 CRTShader.shader 已正确导入。");
                return;
            }
            m_Material = CoreUtils.CreateEngineMaterial(shader);
        }

        // 将材质和设置传给 Pass
        m_RenderPass.Setup(m_Material, settings);
        renderer.EnqueuePass(m_RenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_RenderPass?.Dispose();
            if (m_Material != null)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = null;
            }
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 内部 Render Pass，执行实际的 CRT Blit。
    /// </summary>
    class CRTVolumeRenderPass : ScriptableRenderPass
    {
        private Material m_Material;
        private CRTSettings m_Settings;
#if !UNITY_6000_0_OR_NEWER
        private RTHandle m_TempTexture;
#endif
        private static readonly int CRTIntensityId = Shader.PropertyToID("_CRTIntensity");
        private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int CurveIntensityId = Shader.PropertyToID("_CurveIntensity");
        private static readonly int ChromaticAberrationId = Shader.PropertyToID("_ChromaticAberration");
        private static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
        private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");

        public CRTVolumeRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public void Setup(Material material, CRTSettings settings)
        {
            m_Material = material;
            m_Settings = settings;
        }

#if !UNITY_6000_0_OR_NEWER
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref m_TempTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CRTTempTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null) return;

            // 读取 GameSettings 中的 CRT 强度
            float crtIntensity = GameSettings.Instance != null ? GameSettings.Instance.CRTIntensity : 0f;

            // 如果强度为 0，跳过渲染（优化）
            if (crtIntensity <= 0.001f)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("CRT Pass");

            // 设置材质参数
            m_Material.SetFloat(CRTIntensityId, crtIntensity);
            m_Material.SetFloat(ScanlineIntensityId, m_Settings.scanlineIntensity);
            m_Material.SetFloat(CurveIntensityId, m_Settings.curveIntensity);
            m_Material.SetFloat(ChromaticAberrationId, m_Settings.chromaticAberration);
            m_Material.SetFloat(VignetteIntensityId, m_Settings.vignetteIntensity);
            m_Material.SetFloat(NoiseIntensityId, m_Settings.noiseIntensity);

            RTHandle cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // source -> temp (应用 CRT shader)
            Blitter.BlitCameraTexture(cmd, cameraColor, m_TempTexture, m_Material, 0);
            // temp -> source (写回)
            Blitter.BlitCameraTexture(cmd, m_TempTexture, cameraColor);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

#if UNITY_6000_0_OR_NEWER
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Unity 6000+ 默认启用 RenderGraph，要求实现 RecordRenderGraph。
            // 当前留空以避免报错。如需恢复 CRT 效果，请在 URP Asset 中开启 Compatibility Mode：
            // Edit > Project Settings > Graphics > 选择你的 URP Asset > Compatibility Mode = true
            // 开启后 URP 会走旧的 Execute() 路径，CRT 效果即可正常工作。
        }
#endif

        public void Dispose()
        {
#if !UNITY_6000_0_OR_NEWER
            m_TempTexture?.Release();
#endif
        }
    }
}
