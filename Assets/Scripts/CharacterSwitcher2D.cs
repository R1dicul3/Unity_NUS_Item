using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher2D : MonoBehaviour
{
    [SerializeField] private PlatformerPlayerController character;
    [SerializeField] private Color poweredColor = new Color(1f, 0.82f, 0.32f);
    [SerializeField] private Color basicColor = new Color(0.45f, 0.8f, 1f);

    private bool isPoweredMode = true;

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
