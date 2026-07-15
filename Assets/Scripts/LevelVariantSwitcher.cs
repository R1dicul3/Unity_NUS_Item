using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelVariantSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject[] variants;
    [SerializeField] private Key switchKey = Key.Tab;
    [SerializeField] private int currentIndex;
    [SerializeField] private Text variantLabel;
    [SerializeField] private int labelFontSize = 96;
    [SerializeField] private Vector2 labelOffset = new Vector2(48f, -36f);
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private bool createStartRoomDemoObject = true;

    private bool switchKeyWasPressed;

    public int CurrentIndex => currentIndex;

    private void Start()
    {
        EnsureVariantLabel();
        EnsureStartRoomDemoObject();
        ApplyVariant();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || variants == null || variants.Length == 0)
        {
            return;
        }

        bool switchKeyIsPressed = keyboard[switchKey].isPressed;
        if (switchKeyIsPressed && !switchKeyWasPressed)
        {
            currentIndex = (currentIndex + 1) % variants.Length;
            ApplyVariant();
        }

        switchKeyWasPressed = switchKeyIsPressed;
    }

    public void SetVariant(int index)
    {
        if (variants == null || variants.Length == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, variants.Length - 1);
        ApplyVariant();
    }

    private void ApplyVariant()
    {
        if (variants == null)
        {
            return;
        }

        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] != null)
            {
                variants[i].SetActive(i == currentIndex);
            }
        }

        UpdateVariantLabel();
    }

    private void EnsureVariantLabel()
    {
        if (variantLabel != null)
        {
            ConfigureVariantLabel();
            return;
        }

        GameObject canvasObject = new GameObject("LevelVariantIndicatorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject labelObject = new GameObject("LevelVariantLabel", typeof(Text));
        labelObject.transform.SetParent(canvasObject.transform, false);
        variantLabel = labelObject.GetComponent<Text>();
        ConfigureVariantLabel();
    }

    private void ConfigureVariantLabel()
    {
        variantLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        variantLabel.fontSize = labelFontSize;
        variantLabel.fontStyle = FontStyle.Bold;
        variantLabel.color = labelColor;
        variantLabel.alignment = TextAnchor.UpperLeft;
        variantLabel.raycastTarget = false;

        RectTransform labelTransform = variantLabel.rectTransform;
        labelTransform.anchorMin = new Vector2(0f, 1f);
        labelTransform.anchorMax = new Vector2(0f, 1f);
        labelTransform.pivot = new Vector2(0f, 1f);
        labelTransform.anchoredPosition = labelOffset;
        labelTransform.sizeDelta = new Vector2(180f, 130f);
    }

    private void UpdateVariantLabel()
    {
        if (variantLabel == null)
        {
            return;
        }

        int labelIndex = Mathf.Max(0, currentIndex);
        variantLabel.text = ((char)('A' + labelIndex)).ToString();
    }

    private void EnsureStartRoomDemoObject()
    {
        if (!createStartRoomDemoObject || FindFirstObjectByType<LevelVariantDemoObject>() != null)
        {
            return;
        }

        GameObject startRoom = GameObject.Find("Room_Start");
        if (startRoom == null)
        {
            return;
        }

        LevelVariantDemoObject.Create(startRoom.transform, this);
    }
}
