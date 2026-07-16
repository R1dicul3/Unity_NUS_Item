using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour
{
    [SerializeField] private PlatformerPlayerController character;
    [SerializeField] private Color doubleJumpColor = new Color(1f, 0.82f, 0.32f);
    [SerializeField] private Color dashColor = new Color(0.45f, 0.8f, 1f);

    private bool isDoubleJumpMode = true;

    public void Initialize(PlatformerPlayerController playableCharacter)
    {
        character = playableCharacter;
        ApplyCurrentMode();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || character == null)
        {
            return;
        }

        if (keyboard.zKey.wasPressedThisFrame)
        {
            isDoubleJumpMode = !isDoubleJumpMode;
            ApplyCurrentMode();
        }
    }

    private void ApplyCurrentMode()
    {
        if (character == null)
        {
            return;
        }

        if (isDoubleJumpMode)
        {
            character.SetAbilities(true, false, doubleJumpColor);
        }
        else
        {
            character.SetAbilities(false, true, dashColor);
        }
    }
}
