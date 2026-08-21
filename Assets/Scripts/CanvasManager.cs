using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the UI canvas elements including health bars, domination indicators, resources, and troop UI states.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    [Header("UI Bars")]
    public GameObject playerHP;
    public GameObject enemyHP;
    public GameObject playerDomination;
    public GameObject enemyDomination;
    public GameObject resources;

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
    [Tooltip("Tempo inicial do timer, em segundos. 600 = 10 minutos.")]
    [SerializeField] private float startingTimeInSeconds = 600f;
    private float remainingTime;

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
    [Tooltip("Imagem de cooldown de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6).")]
    public List<Image> troopImages = new List<Image>();

    [Tooltip("Texto de preço de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6).")]
    public List<TextMeshProUGUI> troopPriceTexts = new List<TextMeshProUGUI>();

    [Tooltip("Imagem de 'bloqueado' de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6). Fica ativa enquanto o level do jogador for menor que o unlockedAtLevel da tropa.")]
    public List<Image> blockedTroops = new List<Image>();

    [Header("Tower UI")]
    public Image Tower;

    private RectTransform pHP, eHP, pDom, eDom, res;
    private float pHP_Width, eHP_Width, pDom_Width, eDom_Width, res_Width;

    private Coroutine upgradeCoroutine;
    private Coroutine[] troopCoroutines;

    /// <summary>
    /// Initializes RectTransforms and dimensions for the UI elements.
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

        troopCoroutines = new Coroutine[playerTroopSpawner.troops.Count];

        remainingTime = startingTimeInSeconds;
        UpdateTimerText();
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
        pHP.sizeDelta = new Vector2(pHP_Width * (playerTowerStats.hp / playerTowerStats.maxHP), pHP.sizeDelta.y);
        eHP.sizeDelta = new Vector2(eHP_Width * (enemyTowerStats.hp / enemyTowerStats.maxHP), eHP.sizeDelta.y);
        pDom.sizeDelta = new Vector2(pDom_Width * (100 - playerTroopSpawner.ClosestTroopDistanceTo(new Vector3(50, 0, 0))) / 100, pDom.sizeDelta.y);
        eDom.sizeDelta = new Vector2(eDom_Width * (100 - enemyTroopSpawner.ClosestTroopDistanceTo(new Vector3(-50, 0, 0))) / 100, eDom.sizeDelta.y);
        res.sizeDelta = new Vector2(res_Width * (playerTroopSpawner.resource / playerTroopSpawner.maxResource), res.sizeDelta.y);
        resourcesText.text = "Resources:\n" + playerTroopSpawner.resource.ToString("F1") + " / " + playerTroopSpawner.maxResource.ToString("F1");
        towerUpgradePrice.text = playerTowerStats.toUpgradeResource.ToString("F0");
        level.text = "Lvl: " + playerTroopSpawner.level;

        UpdateTimer();

        int priceCount = Mathf.Min(playerTroopSpawner.troops.Count, troopPriceTexts.Count);
        for (int i = 0; i < priceCount; i++)
        {
            troopPriceTexts[i].text = playerTroopSpawner.troops[i].troopStats.toSpawnResource.ToString("F0");
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
    /// Decrementa o tempo restante e atualiza o texto do timer.
    /// Usa Time.deltaTime, então já respeita Time.timeScale = 0 (pausa).
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
        PausePanel.SetActive(!PausePanel.activeSelf);
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
            Time.timeScale = 1;
        }
    }


    public void UpdateSettingsPanel()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }
}