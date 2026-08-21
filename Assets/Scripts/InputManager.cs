using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Único ponto de leitura de input do jogo. Não deve conter lógica de jogo:
/// apenas expõe o estado do input para quem precisar (câmera, spawners, etc).
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private const int MaxTroopSlots = 6;
    private readonly bool[] troopSpawnRequestedByUI = new bool[MaxTroopSlots];
    private bool upgradeRequestedByUI;
    private bool restartLevelRequestedByUI;
    private bool pauseRequestedByUI;
    private bool settingsRequestedByUI;
    private bool exitRequestedByUI, exitMenuRequestedByUI;
    private bool goHomeRequestedByUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void LateUpdate()
    {
        for (int i = 0; i < troopSpawnRequestedByUI.Length; i++)
            troopSpawnRequestedByUI[i] = false;

        upgradeRequestedByUI = false;
        restartLevelRequestedByUI = false;
        pauseRequestedByUI = false;
        settingsRequestedByUI = false;
        exitRequestedByUI = false;
        exitMenuRequestedByUI = false;
        goHomeRequestedByUI = false;
    }

    public void RequestTroopSpawn(int troopIndex)
    {
        if (troopIndex < 0 || troopIndex >= MaxTroopSlots)
            return;

        troopSpawnRequestedByUI[troopIndex] = true;
    }

    public void RequestUpgrade()
    {
        upgradeRequestedByUI = true;
    }

    public void RequestRestartLevel()
    {
        restartLevelRequestedByUI = true;
    }

    public void RequestPause()
    {
        pauseRequestedByUI = true;
    }

    public void RequestGoHome()
    {
        goHomeRequestedByUI = true;
    }

    public void RequestSettings()
    {
        settingsRequestedByUI = true;
    }

    public void RequestExit()
    {
        exitRequestedByUI = true;
    }
    public void RequestExitMenu()
    {
        exitMenuRequestedByUI = true;
    }

    public bool MoveCameraLeftHeld =>
        Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;

    public bool MoveCameraRightHeld =>
        Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;

    /// <summary>
    /// True no frame em que a tecla numérica correspondente ao índice da tropa
    /// (1..MaxTroopSlots) foi pressionada.
    /// </summary>
    public bool WasTroopSpawnKeyPressed(int troopIndex)
    {
        if (troopIndex < 0 || troopIndex >= MaxTroopSlots)
            return false;

        Key key = Key.Digit1 + troopIndex;
        return Keyboard.current[key].wasPressedThisFrame || troopSpawnRequestedByUI[troopIndex];
    }

    public bool WasUpgradeKeyPressed()
    {
        return Keyboard.current.qKey.wasPressedThisFrame || upgradeRequestedByUI;
    }


    public bool WasRestartLevelKeyPressed()
    {
        return Keyboard.current.rKey.wasPressedThisFrame || restartLevelRequestedByUI;
    }

    public bool WasPauseKeyPressed()
    {
        return Keyboard.current.pKey.wasPressedThisFrame || pauseRequestedByUI;
    }

    public bool WasSettingsButtonPressed()
    {
        return settingsRequestedByUI;
    }

    public bool WasExitButtonPressed()
    {
        return exitRequestedByUI;
    }

    public bool WasGoHomeButtonPressed()
    {
        return goHomeRequestedByUI;
    }
    public bool WasExitMenuKeyPressed()
    {
        return Keyboard.current.escapeKey.wasPressedThisFrame || exitMenuRequestedByUI;
    }
}