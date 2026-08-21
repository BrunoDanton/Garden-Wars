using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the initial screen UI: Play, Credits, and Settings.
/// Credits and Settings can be closed by pressing Esc (via
/// InputManager.WasExitMenuKeyPressed) or clicking the exit button of the
/// respective panel. Both paths pass through InputManager and land
/// in the same HandleMenuExit, centralizing the closing logic.
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

    [Header("Loading Panel")]
    public Image loadingPanelImage;
    public float fadeDuration = 1f;

    [Header("Panel Animation")]
    public float panelFadeDuration = 0.3f;
    private Coroutine creditsFadeCoroutine;
    private Coroutine settingsFadeCoroutine;

    void Start()
    {
        if (loadingPanelImage != null)
        {
            loadingPanelImage.gameObject.SetActive(true);
            StartCoroutine(FadeLoadingPanel(1f, 0f, () => loadingPanelImage.gameObject.SetActive(false)));
        }

        /* Alteration: Replaced repetitive hover scale logic with the new helper utility */
        ButtonHoverAnimator.ApplyTo(Play);
        ButtonHoverAnimator.ApplyTo(Credits);
        ButtonHoverAnimator.ApplyTo(Settings);
        ButtonHoverAnimator.ApplyTo(ExitCredits);
        ButtonHoverAnimator.ApplyTo(ExitSettings);
    }

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
        if (loadingPanelImage != null)
        {
            loadingPanelImage.gameObject.SetActive(true);
            StartCoroutine(FadeLoadingPanel(0f, 1f, () => SceneManager.LoadScene("CityScene")));
        }
        else
        {
            SceneManager.LoadScene("CityScene");
        }
    }

    private void HandleCreditsClicked()
    {
        if (creditsFadeCoroutine != null) StopCoroutine(creditsFadeCoroutine);
        bool isAppearing = !CreditsPanel.activeSelf;
        creditsFadeCoroutine = StartCoroutine(FadePanel(CreditsPanel, isAppearing));
    }

    private void HandleSettingsClicked()
    {
        if (settingsFadeCoroutine != null) StopCoroutine(settingsFadeCoroutine);
        bool isAppearing = !SettingsPanel.activeSelf;
        settingsFadeCoroutine = StartCoroutine(FadePanel(SettingsPanel, isAppearing));
    }

    /// <summary>
    /// Exit buttons from each panel do not close anything on their own.
    /// They only notify the InputManager, just like the Esc key does.
    /// HandleMenuExit determines what to close, called from the same state.
    /// </summary>
    private void HandleExitMenuButtonClicked()
    {
        InputManager.Instance.RequestExitMenu();
    }

    private void HandleMenuExit()
    {
        if (CreditsPanel.activeSelf)
        {
            if (creditsFadeCoroutine != null) StopCoroutine(creditsFadeCoroutine);
            creditsFadeCoroutine = StartCoroutine(FadePanel(CreditsPanel, false));
        }
        else if (SettingsPanel.activeSelf)
        {
            if (settingsFadeCoroutine != null) StopCoroutine(settingsFadeCoroutine);
            settingsFadeCoroutine = StartCoroutine(FadePanel(SettingsPanel, false));
        }
    }

    /// <summary>
    /// Fades a panel in or out using a CanvasGroup.
    /// </summary>
    private IEnumerator FadePanel(GameObject panel, bool isAppearing)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.AddComponent<CanvasGroup>();
        }

        if (isAppearing)
        {
            panel.SetActive(true);
            group.alpha = 0f;
        }

        float startAlpha = group.alpha;
        float targetAlpha = isAppearing ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }

        group.alpha = targetAlpha;

        if (!isAppearing)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// Lerps the alpha channel of the loading panel image over a set duration.
    /// Executes an optional callback action upon completion.
    /// </summary>
    private IEnumerator FadeLoadingPanel(float startAlpha, float endAlpha, Action onComplete)
    {
        float elapsed = 0f;
        Color color = loadingPanelImage.color;
        color.a = startAlpha;
        loadingPanelImage.color = color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            loadingPanelImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        loadingPanelImage.color = color;

        onComplete?.Invoke();
    }
}