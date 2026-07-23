using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 全屏黑白滤镜。
///
/// Volume 和 VolumeProfile 都在运行时创建，所以场景里不需要摆任何东西、
/// 也不需要往工程里加 Volume Profile 资产。摄像机的后处理开关也会自动打开。
/// </summary>
public class GrayscaleFilter : MonoBehaviour {
    [Tooltip("黑白与彩色之间过渡的时长（秒）。设为 0 则瞬间切换。")]
    [SerializeField] private float fadeDuration = 0.25f;

    private Volume volume;
    private float targetWeight;

    public bool IsActive => targetWeight > 0.5f;

    private void Awake() {
        BuildVolume();
        EnablePostProcessingOnCameras();
    }

    private void OnDestroy() {
        // Profile 是运行时 new 出来的，不销毁会泄漏。
        if (volume != null && volume.profile != null) {
            Destroy(volume.profile);
        }
    }

    private void BuildVolume() {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "RuntimeGrayscaleProfile";

        ColorAdjustments colorAdjustments =
            profile.Add<ColorAdjustments>(overrides: true);

        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = -100f;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.profile = profile;
        volume.weight = 0f;

        targetWeight = 0f;
    }

    /// <summary>
    /// URP 里后处理是逐摄像机开关的，场景里的摄像机默认没开，
    /// 不打开的话 Volume 不会生效。
    /// </summary>
    private static void EnablePostProcessingOnCameras() {
        foreach (Camera cam in Camera.allCameras) {
            if (cam == null) {
                continue;
            }

            UniversalAdditionalCameraData cameraData =
                cam.GetUniversalAdditionalCameraData();

            if (cameraData != null) {
                cameraData.renderPostProcessing = true;
            }
        }
    }

    public void SetActive(bool active) {
        targetWeight = active ? 1f : 0f;

        if (fadeDuration <= 0f && volume != null) {
            volume.weight = targetWeight;
        }
    }

    private void Update() {
        if (volume == null || fadeDuration <= 0f) {
            return;
        }

        if (Mathf.Approximately(volume.weight, targetWeight)) {
            return;
        }

        volume.weight = Mathf.MoveTowards(
            volume.weight,
            targetWeight,
            Time.unscaledDeltaTime / fadeDuration
        );
    }
}
