using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlatformerPlayerController character;

    [Header("Input")]
    [SerializeField] private Key switchKey = Key.Z;
    [SerializeField] private bool allowDirectInput;
    [SerializeField] private bool startInPoweredMode = true;

    [Header("Modes")]
    [SerializeField] private Color poweredColor = new Color(1f, 0.05f, 0.72f);
    [SerializeField] private Color basicColor = new Color(0.05f, 1f, 0.2f);

    private bool isPoweredMode;

    private void Awake()
    {
        isPoweredMode = startInPoweredMode;
        ApplyCurrentMode();
    }

    public void Initialize(PlatformerPlayerController playableCharacter)
    {
        character = playableCharacter;
        ApplyCurrentMode();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!allowDirectInput || keyboard == null || character == null)
        {
            return;
        }

        if (keyboard[switchKey].wasPressedThisFrame)
        {
            isPoweredMode = !isPoweredMode;
            ApplyCurrentMode();
        }
    }

    private void ApplyCurrentMode()
    {
        if (character == null)
        {
            return;
        }

        if (isPoweredMode)
        {
            character.SetAbilities(true, true, poweredColor);
        }
        else
        {
            character.SetAbilities(false, false, basicColor);
        }
    }
}
