using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages general game flow, pausing, restarting, and scene transitions based on input.
/// </summary>
public class GameManager : MonoBehaviour
{
    public CanvasManager canvasManager;
    
    void Update()
    {
        if (InputManager.Instance.WasRestartLevelKeyPressed())
        {
            canvasManager.LoadSceneWithFade(SceneManager.GetActiveScene().name);
        }

        if (InputManager.Instance.WasPauseKeyPressed())
        {
            canvasManager.UpdatePausePanel();
        }

        if (InputManager.Instance.WasSettingsButtonPressed())
        {
            canvasManager.UpdateSettingsPanel();
        }

        if (InputManager.Instance.WasGoHomeButtonPressed())
        {
            canvasManager.LoadSceneWithFade("StartScene");
        }

        if (InputManager.Instance.WasExitButtonPressed())
        {
            Application.Quit();
        }

        if (InputManager.Instance.WasExitMenuKeyPressed())
        {
            canvasManager.HandleMenuExit();
        }
    }
}