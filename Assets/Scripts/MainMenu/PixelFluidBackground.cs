using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    [RequireComponent(typeof(RawImage))]
    public class PixelFluidBackground : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("Fluid grid width (higher = smoother, keep it reasonable)")]
        [SerializeField] private int simWidth = 1280;

        [Tooltip("Fluid grid height")]
        [SerializeField] private int simHeight = 720;

        [Tooltip("Jacobi iterations for pressure solve (more needed for higher res)")]
        [SerializeField] private int jacobiIterations = 60;

        [Header("Interaction")]
        [Tooltip("How much mouse movement pushes the fluid")]
        [SerializeField] private float mouseForce = 25.0f;

        [Tooltip("Size of the mouse brush in UV space")]
        [SerializeField] private float splatRadius = 0.02f;

        [Tooltip("How much smoke is injected by mouse movement")]
        [SerializeField] private float densityAmount = 0.5f;

        [Header("Physics Enhancement")]
        [Tooltip("How fast velocity fades (higher = smoke rises longer)")]
        [Range(0.98f, 1.0f)]
        [SerializeField] private float velocityDecay = 0.998f;

        [Tooltip("How fast smoke density fades (lower = less clutter)")]
        [Range(0.95f, 1.0f)]
        [SerializeField] private float densityDecay = 0.996f;

        [Tooltip("Upward force from dense smoke (this is the main engine now)")]
        [SerializeField] private float buoyancyStrength = 8.0f;

        [Tooltip("Swirl detail enhancement (0 = no vorticity, 2-4 recommended)")]
        [SerializeField] private float vorticityStrength = 3.0f;

        [Header("Ambient Emitters")]
        [Tooltip("Number of automatic mist emitters along the bottom")]
        [SerializeField] private int emitterCount = 3;

        [Tooltip("Density injected per emitter per frame (keep moderate)")]
        [SerializeField] private float emitterDensity = 0.35f;

        [Tooltip("Horizontal size of emitter splats (larger = smoother, less dotty)")]
        [SerializeField] private float emitterRadius = 0.12f;

        [Tooltip("How fast emitters drift horizontally")]
        [SerializeField] private float emitterJitter = 0.005f;

        [Header("Background Texture")]
        [Tooltip("If set, the fluid will distort this image instead of showing monochrome smoke")]
        [SerializeField] private Texture2D backgroundTexture;

        [Tooltip("How much original image is restored each frame (0.05 = balanced)")]
        [Range(0.0f, 0.2f)]
        [SerializeField] private float backgroundRestore = 0.05f;

        [Header("Display")]
        [Tooltip("Tint color of the fluid (unused in background texture mode)")]
        [SerializeField] private Color fluidColor = new Color(0.75f, 0.85f, 0.95f, 0.9f);

        [Tooltip("Density threshold before pixels become visible (higher = only dense cores show)")]
        [Range(0, 1)]
        [SerializeField] private float threshold = 0.06f;

        [Tooltip("How hard the pixel edges are")]
        [Range(0, 0.5f)]
        [SerializeField] private float edgeSoftness = 0.15f;

        private RawImage displayImage;
        private Material simMaterial;
        private Material displayMaterial;

        private RenderTexture velocityRT1, velocityRT2;
        private RenderTexture densityRT1, densityRT2;
        private RenderTexture pressureRT1, pressureRT2;
        private RenderTexture divergenceRT;
        private RenderTexture vorticityRT;
        private RenderTexture displayRT;
        private RenderTexture backgroundRT1, backgroundRT2;

        private Vector2 lastMousePos = new Vector2(-1, -1);
        private float[] emitterPositions;
        private float[] emitterTimers;
        private bool useBackgroundMode;

        private void Start()
        {
            displayImage = GetComponent<RawImage>();

            Shader simShader = Shader.Find("Hidden/PixelFluidSimulation");
            Shader displayShader = Shader.Find("UI/PixelFluidDisplay");

            if (simShader == null)
            {
                Debug.LogError("[PixelFluid] Hidden/PixelFluidSimulation shader not found! Ensure PixelFluidSimulation.shader is imported.");
                enabled = false;
                return;
            }
            if (displayShader == null)
            {
                Debug.LogError("[PixelFluid] UI/PixelFluidDisplay shader not found! Ensure PixelFluidDisplay.shader is imported.");
                enabled = false;
                return;
            }

            simMaterial = new Material(simShader);
            displayMaterial = new Material(displayShader);

            // Simulation textures: BILINEAR is critical for smooth advection
            velocityRT1  = CreateSimRT(simWidth, simHeight, "Velocity1");
            velocityRT2  = CreateSimRT(simWidth, simHeight, "Velocity2");
            densityRT1   = CreateSimRT(simWidth, simHeight, "Density1");
            densityRT2   = CreateSimRT(simWidth, simHeight, "Density2");
            pressureRT1  = CreateSimRT(simWidth, simHeight, "Pressure1");
            pressureRT2  = CreateSimRT(simWidth, simHeight, "Pressure2");
            divergenceRT = CreateSimRT(simWidth, simHeight, "Divergence");
            vorticityRT  = CreateSimRT(simWidth, simHeight, "Vorticity");

            // Display texture: POINT keeps the pixel-art crisp look when upscaled
            displayRT = CreateDisplayRT(simWidth, simHeight, "FluidDisplay");

            useBackgroundMode = backgroundTexture != null;
            if (useBackgroundMode)
            {
                backgroundRT1 = CreateSimRT(simWidth, simHeight, "Background1");
                backgroundRT2 = CreateSimRT(simWidth, simHeight, "Background2");
                Graphics.Blit(backgroundTexture, backgroundRT1);
                Graphics.Blit(backgroundTexture, backgroundRT2);
            }

            ClearAll();
            InitEmitters();
            if (!useBackgroundMode) InitialFill();

            if (useBackgroundMode)
            {
                displayImage.texture = backgroundRT1;
                displayImage.material = null; // Use default UI material for true color
            }
            else
            {
                displayImage.texture = displayRT;
                displayImage.material = displayMaterial;
            }
        }

        private RenderTexture CreateSimRT(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            rt.name = name;
            rt.filterMode = FilterMode.Bilinear; // KEY: smooth advection
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        private RenderTexture CreateDisplayRT(int width, int height, string name)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.name = name;
            rt.filterMode = FilterMode.Point; // KEY: pixel-art upscale
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        private void ClearAll()
        {
            RenderTexture[] rts = { velocityRT1, velocityRT2, densityRT1, densityRT2, pressureRT1, pressureRT2, divergenceRT, vorticityRT };
            foreach (var rt in rts)
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }
            RenderTexture.active = null;
        }

        private void InitEmitters()
        {
            emitterPositions = new float[emitterCount];
            emitterTimers = new float[emitterCount];
            for (int i = 0; i < emitterCount; i++)
            {
                emitterPositions[i] = Random.Range(0.2f, 0.8f);
                emitterTimers[i] = Random.value * 100f;
            }
        }

        private void InitialFill()
        {
            // Pre-fill bottom area with a wide, low-density mist band
            // Use large radius so it looks like a continuous sheet, not dots
            for (int i = 0; i < 6; i++)
            {
                Vector2 pos = new Vector2(Random.value, Random.Range(0.0f, 0.15f));
                simMaterial.SetVector("_Point", pos);
                simMaterial.SetFloat("_Radius", Random.Range(0.1f, 0.2f));
                simMaterial.SetVector("_Value", new Vector4(Random.Range(0.1f, 0.25f), 0, 0, 0));
                Graphics.Blit(densityRT1, densityRT2, simMaterial, 1);
                Swap(ref densityRT1, ref densityRT2);
            }
        }

        private void UpdateEmitters()
        {
            for (int i = 0; i < emitterCount; i++)
            {
                emitterTimers[i] += Time.deltaTime;
                float noise = Mathf.PerlinNoise(i * 7.3f, emitterTimers[i] * 0.12f);
                emitterPositions[i] += (noise - 0.5f) * 2f * emitterJitter;
                emitterPositions[i] = Mathf.Clamp(emitterPositions[i], 0.12f, 0.88f);

                float yPos = 0.02f + Mathf.PerlinNoise(emitterTimers[i] * 0.15f, i * 3.1f) * 0.03f;
                Vector2 emitPos = new Vector2(emitterPositions[i], yPos);

                // Inject density ONLY. Let Buoyancy create the upward velocity naturally.
                simMaterial.SetVector("_Point", emitPos);
                simMaterial.SetFloat("_Radius", emitterRadius);
                simMaterial.SetVector("_Value", new Vector4(emitterDensity, 0, 0, 0));
                Graphics.Blit(densityRT1, densityRT2, simMaterial, 1);
                Swap(ref densityRT1, ref densityRT2);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            simMaterial.SetFloat("_DeltaTime", dt);
            simMaterial.SetFloat("_Radius", splatRadius);

            // --- Mouse / Touch Input (Hover, no click needed) ---
            Vector2 mouseUV = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            Vector2 mouseDelta = Vector2.zero;
            if (lastMousePos.x >= 0)
            {
                mouseDelta = mouseUV - lastMousePos;
            }
            lastMousePos = mouseUV;

            // --- 1. Advect Velocity ---
            simMaterial.SetFloat("_Decay", velocityDecay);
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 0);
            Swap(ref velocityRT1, ref velocityRT2);

            // --- 2. Advect Density ---
            simMaterial.SetFloat("_Decay", densityDecay);
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            Graphics.Blit(densityRT1, densityRT2, simMaterial, 0);
            Swap(ref densityRT1, ref densityRT2);

            // --- 3. Buoyancy (density pushes velocity up) ---
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            simMaterial.SetTexture("_DensityTex", densityRT1);
            simMaterial.SetFloat("_BuoyancyStrength", buoyancyStrength);
            Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 2);
            Swap(ref velocityRT1, ref velocityRT2);

            // --- 4. Vorticity ---
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            Graphics.Blit(velocityRT1, vorticityRT, simMaterial, 3);

            // --- 5. Vorticity Confinement ---
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            simMaterial.SetTexture("_VorticityTex", vorticityRT);
            simMaterial.SetFloat("_VorticityStrength", vorticityStrength);
            Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 4);
            Swap(ref velocityRT1, ref velocityRT2);

            // --- 6. Ambient Emitters (auto mist) ---
            // Emitters now only inject density. Buoyancy handles the rise.
            UpdateEmitters();

            // --- 7. Mouse Interaction (hover, no click) ---
            if (mouseDelta.magnitude > 0.0003f)
            {
                // Velocity
                simMaterial.SetVector("_Point", mouseUV);
                simMaterial.SetFloat("_Radius", splatRadius * 1.2f);
                simMaterial.SetVector("_Value", new Vector4(mouseDelta.x, mouseDelta.y, 0, 0) * mouseForce * 3f);
                Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 1);
                Swap(ref velocityRT1, ref velocityRT2);

                // Density
                simMaterial.SetVector("_Value", new Vector4(densityAmount * 0.3f, 0, 0, 0));
                Graphics.Blit(densityRT1, densityRT2, simMaterial, 1);
                Swap(ref densityRT1, ref densityRT2);
            }

            // --- 8. Boundary (velocity) ---
            Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 8);
            Swap(ref velocityRT1, ref velocityRT2);

            // --- 9. Boundary (density) ---
            Graphics.Blit(densityRT1, densityRT2, simMaterial, 8);
            Swap(ref densityRT1, ref densityRT2);

            // --- 10. Divergence ---
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            Graphics.Blit(null, divergenceRT, simMaterial, 5);

            // --- 11. Jacobi iterations ---
            simMaterial.SetTexture("_DivergenceTex", divergenceRT);
            for (int i = 0; i < jacobiIterations; i++)
            {
                simMaterial.SetTexture("_PressureTex", pressureRT1);
                Graphics.Blit(pressureRT1, pressureRT2, simMaterial, 6);
                Swap(ref pressureRT1, ref pressureRT2);
            }

            // --- 12. Gradient Subtraction ---
            simMaterial.SetTexture("_VelocityTex", velocityRT1);
            simMaterial.SetTexture("_PressureTex", pressureRT1);
            Graphics.Blit(velocityRT1, velocityRT2, simMaterial, 7);
            Swap(ref velocityRT1, ref velocityRT2);

            // --- 13. Background Advection (texture distortion) ---
            if (useBackgroundMode)
            {
                simMaterial.SetTexture("_VelocityTex", velocityRT1);
                simMaterial.SetTexture("_SourceTex", backgroundTexture);
                simMaterial.SetFloat("_RestoreFactor", backgroundRestore);
                simMaterial.SetFloat("_DistortionScale", 0.3f);
                Graphics.Blit(backgroundRT1, backgroundRT2, simMaterial, 9);
                Swap(ref backgroundRT1, ref backgroundRT2);
                displayImage.texture = backgroundRT1;
            }
            else
            {
                // --- Display (monochrome smoke) ---
                displayMaterial.SetColor("_FluidColor", fluidColor);
                displayMaterial.SetFloat("_Threshold", threshold);
                displayMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
                Graphics.Blit(densityRT1, displayRT, displayMaterial, 0);
            }
        }

        private void Swap(ref RenderTexture a, ref RenderTexture b)
        {
            RenderTexture temp = a;
            a = b;
            b = temp;
        }

        private void OnDestroy()
        {
            RenderTexture[] allRTs = { velocityRT1, velocityRT2, densityRT1, densityRT2, pressureRT1, pressureRT2, divergenceRT, vorticityRT, displayRT, backgroundRT1, backgroundRT2 };
            foreach (var rt in allRTs)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }

            if (simMaterial != null) Destroy(simMaterial);
            if (displayMaterial != null) Destroy(displayMaterial);
        }
    }
}
