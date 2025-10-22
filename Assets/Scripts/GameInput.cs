using UnityEngine;
public static class GameInput
{
    public static InputActions InputActions;
    public const float InputWrapAround = 3000f;

    public static void Initialize()
    {
        InputActions = new InputActions();
        InputActions.Enable();
        InputActions.Player.Enable();
    }
}