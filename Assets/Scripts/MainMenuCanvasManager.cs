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
/// Handles background music with fade-in and fade-out mechanics.
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

    /* Alteration: Added fields for background music management and transition control */
    [Header("Audio")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float maxMusicVolume = 0.5f;
    public float musicFadeDuration = 2f;
    private AudioSource musicSource;
    private Coroutine musicTransitionCoroutine;

    public AudioClip buttonPressSound;
    [Range(0f, 1f)] public float buttonPressVolume = 1f;

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

        /* Alteration: Dynamically instantiates the AudioSource for the background music and begins the fade-in */
        if (backgroundMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();
            musicTransitionCoroutine = StartCoroutine(FadeInMusic());
        }

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

        Play.onClick.AddListener(PlayButtonSound);
        Credits.onClick.AddListener(PlayButtonSound);
        Settings.onClick.AddListener(PlayButtonSound);
        ExitCredits.onClick.AddListener(PlayButtonSound);
        ExitSettings.onClick.AddListener(PlayButtonSound);
    }

    void OnDisable()
    {
        Play.onClick.RemoveListener(HandlePlayClicked);
        Credits.onClick.RemoveListener(HandleCreditsClicked);
        Settings.onClick.RemoveListener(HandleSettingsClicked);
        ExitCredits.onClick.RemoveListener(HandleExitMenuButtonClicked);
        ExitSettings.onClick.RemoveListener(HandleExitMenuButtonClicked);

        Play.onClick.RemoveListener(PlayButtonSound);
        Credits.onClick.RemoveListener(PlayButtonSound);
        Settings.onClick.RemoveListener(PlayButtonSound);
        ExitCredits.onClick.RemoveListener(PlayButtonSound);
        ExitSettings.onClick.RemoveListener(PlayButtonSound);
    }

    void Update()
    {
        if (InputManager.Instance.WasExitMenuKeyPressed())
        {
            HandleMenuExit();
        }
    }

    /// <summary>
    /// Plays the generic button press sound safely at the camera's position.
    /// </summary>
    private void PlayButtonSound()
    {
        if (buttonPressSound != null)
        {
            AudioSource.PlayClipAtPoint(buttonPressSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, buttonPressVolume);
        }
    }

    private void HandlePlayClicked()
    {
        /* Alteration: Triggers the music fade-out synchronously with the loading panel fade */
        if (musicSource != null)
        {
            if (musicTransitionCoroutine != null) StopCoroutine(musicTransitionCoroutine);
            musicTransitionCoroutine = StartCoroutine(FadeOutMusic());
        }

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

    /* Alteration: Added coroutine to fade in the background music over a set duration */
    /// <summary>
    /// Fades in the background music volume from zero to maxMusicVolume over a set duration.
    /// </summary>
    private IEnumerator FadeInMusic()
    {
        float elapsed = 0f;
        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, maxMusicVolume, elapsed / musicFadeDuration);
            yield return null;
        }
        musicSource.volume = maxMusicVolume;
    }

    /* Alteration: Added coroutine to exclusively fade out music during scene unloads */
    /// <summary>
    /// Smoothly fades out the music volume completely to prepare for a scene transition.
    /// Uses fadeDuration to sync perfectly with the visual screen fade.
    /// </summary>
    private IEnumerator FadeOutMusic()
    {
        float elapsed = 0f;
        float startVolume = musicSource.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }
        musicSource.volume = 0f;
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