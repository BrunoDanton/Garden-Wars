using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gerencia a UI da tela inicial: Play, Créditos e Settings.
/// Créditos e Settings podem ser fechados apertando Esc (via
/// InputManager.WasExitMenuKeyPressed) ou clicando no botão de sair do
/// respectivo painel — os dois caminhos passam pelo InputManager e caem
/// no mesmo HandleMenuExit, então o fechamento é sempre tratado num só lugar.
/// </summary>
public class MainMenuCanvasManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button Play;
    public Button Credits;
    public Button Settings;
    public Button ExitCredits;
    public Button ExitSettings;

    [Header("Panels")]
    public GameObject CreditsPanel;
    public GameObject SettingsPanel;

    void OnEnable()
    {
        Play.onClick.AddListener(HandlePlayClicked);
        Credits.onClick.AddListener(HandleCreditsClicked);
        Settings.onClick.AddListener(HandleSettingsClicked);
        ExitCredits.onClick.AddListener(HandleExitMenuButtonClicked);
        ExitSettings.onClick.AddListener(HandleExitMenuButtonClicked);
    }

    void OnDisable()
    {
        Play.onClick.RemoveListener(HandlePlayClicked);
        Credits.onClick.RemoveListener(HandleCreditsClicked);
        Settings.onClick.RemoveListener(HandleSettingsClicked);
        ExitCredits.onClick.RemoveListener(HandleExitMenuButtonClicked);
        ExitSettings.onClick.RemoveListener(HandleExitMenuButtonClicked);
    }

    void Update()
    {
        if (InputManager.Instance.WasExitMenuKeyPressed())
        {
            HandleMenuExit();
        }
    }

    private void HandlePlayClicked()
    {
        SceneManager.LoadScene("CityScene");
    }

    private void HandleCreditsClicked()
    {
        CreditsPanel.SetActive(!CreditsPanel.activeSelf);
    }

    private void HandleSettingsClicked()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }

    /// <summary>
    /// Botões "sair" de cada painel não fecham nada sozinhos — eles só
    /// avisam o InputManager, igual o Esc faz. Quem decide o que fechar é
    /// sempre o HandleMenuExit, chamado a partir do mesmo estado.
    /// </summary>
    private void HandleExitMenuButtonClicked()
    {
        InputManager.Instance.RequestExitMenu();
    }

    private void HandleMenuExit()
    {
        if (CreditsPanel.activeSelf)
        {
            CreditsPanel.SetActive(false);
        }
        else if (SettingsPanel.activeSelf)
        {
            SettingsPanel.SetActive(false);
        }
    }
}