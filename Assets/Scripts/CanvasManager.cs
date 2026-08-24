using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the UI canvas elements including health bars, domination indicators, resources, and troop UI states.
/// Handles global UI audio interactions and music transitions.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    [Header("UI Bars")]
    public GameObject playerHP;
    public GameObject enemyHP;
    public GameObject playerDomination;
    public GameObject enemyDomination;
    public GameObject resources;

    [Header("Bar Animation")]
    public float barLerpSpeed = 10f;

    [Header("Stats & Spawners")]
    public Tower_Stats playerTowerStats;
    public Tower_Stats enemyTowerStats;
    public EnemyTroopSpawner enemyTroopSpawner;
    public PlayerTroopSpawner playerTroopSpawner;

    [Header("Text Elements")]
    public TextMeshProUGUI resourcesText;
    public TextMeshProUGUI towerUpgradePrice;
    public TextMeshProUGUI level;
    public TextMeshProUGUI timer;

    [Header("Timer")]
    [SerializeField] private float startingTimeInSeconds = 600f;
    private float remainingTime;

    [Header("Animação de Números")]
    [SerializeField] private float numberLerpDuration = 0.4f;

    /// <summary>
    /// Elapsed time since the match started, in seconds.
    /// </summary>
    public float ElapsedTime => startingTimeInSeconds - remainingTime;

    [Header("Buttons")]
    public Button UpgradeTower;
    public Button Unit1, Unit2, Unit3, Unit4, Unit5;
    public Button RestartLevel;
    public Button Pause, Resume;
    public Button Settings;
    public Button Exit, ExitSettings;
    public Button Home;

    [Header("Panels")]
    public GameObject PausePanel;
    public GameObject SettingsPanel;

    [Header("Troops UI")]
    public List<Image> troopImages = new List<Image>();
    public List<TextMeshProUGUI> troopPriceTexts = new List<TextMeshProUGUI>();
    public List<Image> blockedTroops = new List<Image>();

    [Header("Tower UI")]
    public Image Tower;

    [Header("Loading Panel")]
    public Image loadingPanelImage;
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip victoryMusic;
    public AudioClip defeatMusic;
    [Range(0f, 1f)] public float maxMusicVolume = 0.5f;
    public float musicFadeDuration = 2f;
    private AudioSource musicSource;
    private Coroutine musicTransitionCoroutine;
    
    public AudioClip genericPressSound;
    [Range(0f, 1f)] public float genericPressVolume = 1f;
    public AudioClip unitPressSound;
    [Range(0f, 1f)] public float unitPressVolume = 1f;

    /* Alteration: Added an AudioSource reference specifically for playing UI sound effects attached to the camera. */
    private AudioSource sfxSource;

    [Header("Panel Animation")]
    public float panelFadeDuration = 0.3f;
    private Coroutine pauseFadeCoroutine;
    private Coroutine settingsFadeCoroutine;

    private RectTransform pHP, eHP, pDom, eDom, res;
    private float pHP_Width, eHP_Width, pDom_Width, eDom_Width, res_Width;

    private float targetPDomWidth;
    private float targetEDomWidth;

    private Coroutine upgradeCoroutine;
    private Coroutine[] troopCoroutines;

    private const float UNINITIALIZED = float.MinValue;

    private float displayedResource;
    private float lastTargetResource = UNINITIALIZED;
    private Coroutine resourceNumberCoroutine;

    private float displayedUpgradePrice;
    private float lastTargetUpgradePrice = UNINITIALIZED;
    private Coroutine upgradePriceNumberCoroutine;

    private float displayedLevel;
    private float lastTargetLevel = UNINITIALIZED;
    private Coroutine levelNumberCoroutine;

    private float displayedMaxResource;
    private float lastTargetMaxResource = UNINITIALIZED;
    private Coroutine maxResourceNumberCoroutine;

    private float[] displayedTroopPrices;
    private float[] lastTargetTroopPrices;
    private Coroutine[] troopPriceNumberCoroutines;

    /// <summary>
    /// Initializes RectTransforms, UI events, inputs, and starts the initial music fade-in.
    /// </summary>
    void Start()
    {
        pHP = playerHP.GetComponent<RectTransform>();
        eHP = enemyHP.GetComponent<RectTransform>();
        pDom = playerDomination.GetComponent<RectTransform>();
        eDom = enemyDomination.GetComponent<RectTransform>();
        res = resources.GetComponent<RectTransform>();

        pHP_Width = pHP.sizeDelta.x;
        eHP_Width = eHP.sizeDelta.x;
        pDom_Width = pDom.sizeDelta.x;
        eDom_Width = eDom.sizeDelta.x;
        res_Width = res.sizeDelta.x;

        targetPDomWidth = pDom.sizeDelta.x;
        targetEDomWidth = eDom.sizeDelta.x;

        troopCoroutines = new Coroutine[playerTroopSpawner.troops.Count];

        displayedTroopPrices = new float[troopPriceTexts.Count];
        lastTargetTroopPrices = new float[troopPriceTexts.Count];
        troopPriceNumberCoroutines = new Coroutine[troopPriceTexts.Count];
        for (int i = 0; i < lastTargetTroopPrices.Length; i++)
        {
            lastTargetTroopPrices[i] = UNINITIALIZED;
        }

        remainingTime = startingTimeInSeconds;
        UpdateTimerText();

        if (loadingPanelImage != null)
        {
            loadingPanelImage.gameObject.SetActive(true);
            StartCoroutine(FadeLoadingPanel(1f, 0f, () => loadingPanelImage.gameObject.SetActive(false)));
        }

        if (backgroundMusic != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();
            musicTransitionCoroutine = StartCoroutine(FadeInMusic());
        }

        /* Alteration: Instantiates a dedicated UI AudioSource on the Main Camera to prevent spatial panning issues during camera movement. */
        if (Camera.main != null)
        {
            sfxSource = Camera.main.gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
        }
        else
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
        }

        ButtonHoverAnimator.ApplyTo(UpgradeTower, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasUpgradeKeyPressed());
        ButtonHoverAnimator.ApplyTo(Unit1, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasTroopSpawnKeyPressed(0));
        ButtonHoverAnimator.ApplyTo(Unit2, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasTroopSpawnKeyPressed(1));
        ButtonHoverAnimator.ApplyTo(Unit3, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasTroopSpawnKeyPressed(2));
        ButtonHoverAnimator.ApplyTo(Unit4, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasTroopSpawnKeyPressed(3));
        ButtonHoverAnimator.ApplyTo(Unit5, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasTroopSpawnKeyPressed(4));
        
        ButtonHoverAnimator.ApplyTo(RestartLevel, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasRestartLevelKeyPressed());
        ButtonHoverAnimator.ApplyTo(Pause, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasPauseKeyPressed());
        ButtonHoverAnimator.ApplyTo(Resume, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasPauseKeyPressed());
        ButtonHoverAnimator.ApplyTo(Settings, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasSettingsButtonPressed());
        ButtonHoverAnimator.ApplyTo(Home, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasGoHomeButtonPressed());
        ButtonHoverAnimator.ApplyTo(Exit, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasExitButtonPressed());
        ButtonHoverAnimator.ApplyTo(ExitSettings, 1.1f, 0.9f, 0.15f, () => InputManager.Instance.WasExitMenuKeyPressed());
    }

    /// <summary>
    /// Subscribes to spawner events and button clicks when enabled.
    /// </summary>
    void OnEnable()
    {
        playerTroopSpawner.OnTroopSpawned += HandleTroopSpawned;
        playerTroopSpawner.OnTowerUpgraded += HandleTowerUpgraded;

        UpgradeTower.onClick.AddListener(HandleUpgradeTowerClicked);
        Unit1.onClick.AddListener(HandleUnit1Clicked);
        Unit2.onClick.AddListener(HandleUnit2Clicked);
        Unit3.onClick.AddListener(HandleUnit3Clicked);
        Unit4.onClick.AddListener(HandleUnit4Clicked);
        Unit5.onClick.AddListener(HandleUnit5Clicked);
        RestartLevel.onClick.AddListener(HandleRestartLevelClicked);
        Pause.onClick.AddListener(HandlePauseClicked);
        Resume.onClick.AddListener(HandleResumeClicked);
        Settings.onClick.AddListener(HandleSettingsClicked);
        Home.onClick.AddListener(HandleHomeClicked);
        Exit.onClick.AddListener(HandleExitClicked);
        ExitSettings.onClick.AddListener(HandleExitSettingsClicked);
    }

    /// <summary>
    /// Unsubscribes from spawner events and button clicks when disabled.
    /// </summary>
    void OnDisable()
    {
        playerTroopSpawner.OnTroopSpawned -= HandleTroopSpawned;
        playerTroopSpawner.OnTowerUpgraded -= HandleTowerUpgraded;

        UpgradeTower.onClick.RemoveListener(HandleUpgradeTowerClicked);
        Unit1.onClick.RemoveListener(HandleUnit1Clicked);
        Unit2.onClick.RemoveListener(HandleUnit2Clicked);
        Unit3.onClick.RemoveListener(HandleUnit3Clicked);
        Unit4.onClick.RemoveListener(HandleUnit4Clicked);
        Unit5.onClick.RemoveListener(HandleUnit5Clicked);
        RestartLevel.onClick.RemoveListener(HandleRestartLevelClicked);
        Pause.onClick.RemoveListener(HandlePauseClicked);
        Resume.onClick.RemoveListener(HandleResumeClicked);
        Settings.onClick.RemoveListener(HandleSettingsClicked);
        Home.onClick.RemoveListener(HandleHomeClicked);
        Exit.onClick.RemoveListener(HandleExitClicked);
        ExitSettings.onClick.RemoveListener(HandleExitSettingsClicked);
    }

    /// <summary>
    /// Updates UI bars, texts, and blocked states every frame.
    /// </summary>
    void Update()
    {
        PlayInputSounds();

        float targetPHPWidth = pHP_Width * (playerTowerStats.hp / playerTowerStats.maxHP);
        pHP.sizeDelta = new Vector2(Mathf.Lerp(pHP.sizeDelta.x, targetPHPWidth, Time.unscaledDeltaTime * barLerpSpeed), pHP.sizeDelta.y);

        float targetEHPWidth = eHP_Width * (enemyTowerStats.hp / enemyTowerStats.maxHP);
        eHP.sizeDelta = new Vector2(Mathf.Lerp(eHP.sizeDelta.x, targetEHPWidth, Time.unscaledDeltaTime * barLerpSpeed), eHP.sizeDelta.y);

        if (playerTowerStats.hp > 0 && enemyTowerStats.hp > 0)
        {
            float pDist = playerTroopSpawner.ClosestTroopDistanceTo(new Vector3(50, 0, 0));
            float eDist = enemyTroopSpawner.ClosestTroopDistanceTo(new Vector3(-50, 0, 0));

            if (float.IsInfinity(pDist) || float.IsNaN(pDist)) pDist = 0f;
            if (float.IsInfinity(eDist) || float.IsNaN(eDist)) eDist = 0f;

            targetPDomWidth = pDom_Width * (100f - Mathf.Clamp(pDist, 0f, 100f)) / 100f;
            targetEDomWidth = eDom_Width * (100f - Mathf.Clamp(eDist, 0f, 100f)) / 100f;
        }

        pDom.sizeDelta = new Vector2(Mathf.Lerp(pDom.sizeDelta.x, targetPDomWidth, Time.unscaledDeltaTime * barLerpSpeed), pDom.sizeDelta.y);
        eDom.sizeDelta = new Vector2(Mathf.Lerp(eDom.sizeDelta.x, targetEDomWidth, Time.unscaledDeltaTime * barLerpSpeed), eDom.sizeDelta.y);

        float targetResWidth = res_Width * (playerTroopSpawner.resource / playerTroopSpawner.maxResource);
        res.sizeDelta = new Vector2(Mathf.Lerp(res.sizeDelta.x, targetResWidth, Time.unscaledDeltaTime * barLerpSpeed), res.sizeDelta.y);

        AnimateIfChanged(playerTroopSpawner.resource, ref lastTargetResource, ref displayedResource, ref resourceNumberCoroutine, value =>
        {
            displayedResource = value;
            UpdateResourcesText();
        });

        AnimateIfChanged(playerTroopSpawner.maxResource, ref lastTargetMaxResource, ref displayedMaxResource, ref maxResourceNumberCoroutine, value =>
        {
            displayedMaxResource = value;
            UpdateResourcesText();
        });

        AnimateIfChanged(playerTowerStats.toUpgradeResource, ref lastTargetUpgradePrice, ref displayedUpgradePrice, ref upgradePriceNumberCoroutine, value =>
        {
            displayedUpgradePrice = value;
            towerUpgradePrice.text = value.ToString("F0");
        });

        AnimateIfChanged(playerTroopSpawner.level, ref lastTargetLevel, ref displayedLevel, ref levelNumberCoroutine, value =>
        {
            displayedLevel = value;
            level.text = "Lvl: " + Mathf.RoundToInt(value);
        });

        UpdateTimer();

        int priceCount = Mathf.Min(playerTroopSpawner.troops.Count, troopPriceTexts.Count);
        for (int i = 0; i < priceCount; i++)
        {
            int index = i;
            Coroutine coroutine = troopPriceNumberCoroutines[index];
            AnimateIfChanged(playerTroopSpawner.troops[index].troopStats.toSpawnResource, ref lastTargetTroopPrices[index], ref displayedTroopPrices[index], ref coroutine, value =>
            {
                displayedTroopPrices[index] = value;
                troopPriceTexts[index].text = value.ToString("F0");
            });
            troopPriceNumberCoroutines[index] = coroutine;
        }

        int blockedCount = Mathf.Min(playerTroopSpawner.troops.Count, blockedTroops.Count);
        for (int i = 0; i < blockedCount; i++)
        {
            bool isLocked = playerTroopSpawner.level < playerTroopSpawner.troops[i].troopStats.unlockedAtLevel;

            if (blockedTroops[i].gameObject.activeSelf != isLocked)
                blockedTroops[i].gameObject.SetActive(isLocked);
        }
    }

    /// <summary>
    /// Polls the InputManager to detect any generic or unit-specific actions requested this frame,
    /// triggering the corresponding audio clip.
    /// </summary>
    private void PlayInputSounds()
    {
        if (InputManager.Instance.WasRestartLevelKeyPressed() ||
            InputManager.Instance.WasPauseKeyPressed() ||
            InputManager.Instance.WasSettingsButtonPressed() ||
            InputManager.Instance.WasGoHomeButtonPressed() ||
            InputManager.Instance.WasExitButtonPressed() ||
            InputManager.Instance.WasExitMenuKeyPressed())
        {
            PlaySound(genericPressSound, genericPressVolume);
        }

        bool playUnitSound = InputManager.Instance.WasUpgradeKeyPressed();

        if (!playUnitSound)
        {
            for (int i = 0; i < playerTroopSpawner.troops.Count; i++)
            {
                if (InputManager.Instance.WasTroopSpawnKeyPressed(i))
                {
                    playUnitSound = true;
                    break;
                }
            }
        }

        if (playUnitSound)
        {
            PlaySound(unitPressSound, unitPressVolume);
        }
    }

    /* Alteration: Replaced PlayClipAtPoint with PlayOneShot utilizing the camera-attached AudioSource to maintain position and volume. */
    /// <summary>
    /// Plays the specified audio clip using the dedicated UI AudioSource.
    /// </summary>
    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Updates the resources text UI with the currently displayed (lerped) values.
    /// </summary>
    private void UpdateResourcesText()
    {
        resourcesText.text = "Resources:\n" + displayedResource.ToString("F1") + " / " + displayedMaxResource.ToString("F1");
    }

    /// <summary>
    /// Decrements the remaining time and updates the timer text.
    /// </summary>
    private void UpdateTimer()
    {
        if (remainingTime <= 0f)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void HandleUpgradeTowerClicked() => InputManager.Instance.RequestUpgrade();
    private void HandleUnit1Clicked() => InputManager.Instance.RequestTroopSpawn(0);
    private void HandleUnit2Clicked() => InputManager.Instance.RequestTroopSpawn(1);
    private void HandleUnit3Clicked() => InputManager.Instance.RequestTroopSpawn(2);
    private void HandleUnit4Clicked() => InputManager.Instance.RequestTroopSpawn(3);
    private void HandleUnit5Clicked() => InputManager.Instance.RequestTroopSpawn(4);
    private void HandleRestartLevelClicked() => InputManager.Instance.RequestRestartLevel();
    private void HandlePauseClicked() => InputManager.Instance.RequestPause();
    private void HandleResumeClicked() => InputManager.Instance.RequestPause();
    private void HandleHomeClicked() => InputManager.Instance.RequestGoHome();
    private void HandleSettingsClicked() => InputManager.Instance.RequestSettings();
    private void HandleExitClicked() => InputManager.Instance.RequestExit();
    private void HandleExitSettingsClicked() => InputManager.Instance.RequestExitMenu();

    /// <summary>
    /// Handles the troop spawned event to trigger cooldown UI.
    /// </summary>
    private void HandleTroopSpawned(int index, float cooldown)
    {
        if (index < 0 || index >= troopImages.Count)
            return;

        if (troopCoroutines[index] != null)
            StopCoroutine(troopCoroutines[index]);

        troopCoroutines[index] = StartCoroutine(TroopCoolDown(troopImages[index], cooldown, index));
    }

    /// <summary>
    /// Handles the tower upgraded event to trigger cooldown UI.
    /// </summary>
    private void HandleTowerUpgraded(float cooldown)
    {
        if (upgradeCoroutine != null)
            StopCoroutine(upgradeCoroutine);

        upgradeCoroutine = StartCoroutine(TowerCoolDown(cooldown));
    }

    /// <summary>
    /// Coroutine to manage the troop cooldown visual effect.
    /// </summary>
    private IEnumerator TroopCoolDown(Image unit, float cooldown, int index)
    {
        float timeElapsed = 0f;
        unit.fillAmount = 0f;

        while (timeElapsed < cooldown)
        {
            timeElapsed += Time.deltaTime;
            unit.fillAmount = timeElapsed / cooldown;
            yield return null;
        }

        unit.fillAmount = 1f;
        troopCoroutines[index] = null;
    }

    /// <summary>
    /// Coroutine to manage the tower upgrade cooldown visual effect.
    /// </summary>
    private IEnumerator TowerCoolDown(float cooldown)
    {
        float timeElapsed = 0f;
        Tower.fillAmount = 0f;

        while (timeElapsed < cooldown)
        {
            timeElapsed += Time.deltaTime;
            Tower.fillAmount = timeElapsed / cooldown;
            yield return null;
        }

        Tower.fillAmount = 1f;
        upgradeCoroutine = null;
    }

    public void UpdatePausePanel()
    {
        if (pauseFadeCoroutine != null) StopCoroutine(pauseFadeCoroutine);
        bool isAppearing = !PausePanel.activeSelf;
        pauseFadeCoroutine = StartCoroutine(FadePanel(PausePanel, isAppearing, true));
    }

    public void HandleMenuExit()
    {
        if (SettingsPanel.activeSelf)
        {
            UpdateSettingsPanel();
        }
        else if (PausePanel.activeSelf && !SettingsPanel.activeSelf)
        {
            UpdatePausePanel();
        }
    }

    public void UpdateSettingsPanel()
    {
        if (settingsFadeCoroutine != null) StopCoroutine(settingsFadeCoroutine);
        bool isAppearing = !SettingsPanel.activeSelf;
        settingsFadeCoroutine = StartCoroutine(FadePanel(SettingsPanel, isAppearing, true));
    }

    /// <summary>
    /// Fades a panel in or out using a CanvasGroup. Lerps time scale during transition
    /// unless another time-freezing panel is active.
    /// Uses unscaledDeltaTime to ensure the animation plays even when time is frozen.
    /// </summary>
    private IEnumerator FadePanel(GameObject panel, bool isAppearing, bool freezesTime)
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

        bool otherPanelActive = (panel == SettingsPanel && PausePanel.activeSelf) || (panel == PausePanel && SettingsPanel.activeSelf);

        float targetTimeScale = 1f;
        if (otherPanelActive)
        {
            targetTimeScale = 0f;
        }
        else if (isAppearing && freezesTime)
        {
            targetTimeScale = 0f;
        }

        float startAlpha = group.alpha;
        float targetAlpha = isAppearing ? 1f : 0f;
        float initialTimeScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / panelFadeDuration;
            
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            Time.timeScale = Mathf.Lerp(initialTimeScale, targetTimeScale, t);
            
            yield return null;
        }

        group.alpha = targetAlpha;
        Time.timeScale = targetTimeScale;

        if (!isAppearing)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// Initiates a crossfade transition to either the victory or defeat music track.
    /// </summary>
    public void PlayEndgameMusic(bool isVictory)
    {
        AudioClip nextClip = isVictory ? victoryMusic : defeatMusic;
        if (nextClip != null && musicSource != null)
        {
            if (musicTransitionCoroutine != null) StopCoroutine(musicTransitionCoroutine);
            musicTransitionCoroutine = StartCoroutine(CrossfadeMusic(nextClip));
        }
    }

    /// <summary>
    /// Fades in the initial background music volume from zero to maxMusicVolume over a set duration.
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

    /// <summary>
    /// Fades out the current playing music, swaps the clip, and fades the new clip in.
    /// </summary>
    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        float elapsed = 0f;
        float startVolume = musicSource.volume;
        float halfDuration = musicFadeDuration / 2f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, maxMusicVolume, elapsed / halfDuration);
            yield return null;
        }

        musicSource.volume = maxMusicVolume;
    }

    /// <summary>
    /// Smoothly fades out the music volume completely to prepare for a scene transition.
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
    /// Checks if the target numerical value has changed and starts a lerp animation coroutine if necessary.
    /// </summary>
    private void AnimateIfChanged(float targetValue, ref float lastTarget, ref float displayedValue, ref Coroutine coroutine, Action<float> onUpdate)
    {
        if (lastTarget == UNINITIALIZED)
        {
            lastTarget = targetValue;
            displayedValue = targetValue;
            onUpdate(targetValue);
            return;
        }

        if (Mathf.Abs(targetValue - lastTarget) > Mathf.Epsilon)
        {
            lastTarget = targetValue;

            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            coroutine = StartCoroutine(NumberAnimator.Animate(displayedValue, targetValue, numberLerpDuration, onUpdate));
        }
    }

    /// <summary>
    /// Restores time scale, triggers the fade-out animation, and loads the specified scene.
    /// Applies a minimum time scale to ensure deltaTime is not zero if called while paused.
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 0.1f;
        }

        if (musicSource != null)
        {
            if (musicTransitionCoroutine != null) StopCoroutine(musicTransitionCoroutine);
            musicTransitionCoroutine = StartCoroutine(FadeOutMusic());
        }

        if (loadingPanelImage != null)
        {
            loadingPanelImage.gameObject.SetActive(true);
            StartCoroutine(FadeLoadingPanel(0f, 1f, () => SceneManager.LoadScene(sceneName)));
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Lerps the alpha channel of the loading panel image over a set duration.
    /// Gradually restores Time.timeScale to 1 when transitioning to a new scene.
    /// Executes an optional callback action upon completion.
    /// </summary>
    private IEnumerator FadeLoadingPanel(float startAlpha, float endAlpha, Action onComplete)
    {
        Color color = loadingPanelImage.color;
        color.a = startAlpha;
        loadingPanelImage.color = color;

        yield return null;

        float elapsed = 0f;
        float initialTime = Time.timeScale;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            loadingPanelImage.color = color;
            
            Time.timeScale = Mathf.Lerp(initialTime, 1f, t);
            
            yield return null;
        }

        color.a = endAlpha;
        loadingPanelImage.color = color;
        Time.timeScale = 1f;

        onComplete?.Invoke();
    }
}