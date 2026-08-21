using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public CanvasManager canvasManager;
    void Update()
    {
        if (InputManager.Instance.WasRestartLevelKeyPressed())
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (InputManager.Instance.WasPauseKeyPressed())
        {
            if (Time.timeScale == 0)
            {
                canvasManager.UpdatePausePanel();
                Time.timeScale = 1;
                
            }
            else
            {    
                canvasManager.UpdatePausePanel();
                Time.timeScale = 0;
            }
        }

        if (InputManager.Instance.WasSettingsButtonPressed())
        {
            if (Time.timeScale == 0)
            {
                canvasManager.UpdateSettingsPanel();
            }
            else
            {    
                canvasManager.UpdateSettingsPanel();
            }
        }

        if (InputManager.Instance.WasGoHomeButtonPressed())
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("StartScene");
        }

        if (InputManager.Instance.WasExitButtonPressed())
        {
            // Não fecha o editor, mas fecha o aplicativo!
            Application.Quit();
        }

        if (InputManager.Instance.WasExitMenuKeyPressed())
        {
            canvasManager.HandleMenuExit();
        }
    }
}