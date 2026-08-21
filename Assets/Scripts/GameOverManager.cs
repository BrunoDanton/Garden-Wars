using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Detects victory/defeat based on tower HP, shows the corresponding panel with a fade,
/// animates the final match data (enemies defeated, elapsed time, accumulated money),
/// and smoothly slows down time until all animations conclude. Includes hover animations for buttons.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Stats & Referências")]
    public Tower_Stats playerTowerStats;
    public Tower_Stats enemyTowerStats;
    public CanvasManager canvasManager;

    [Header("Panels")]
    public GameObject VictoryPanel;
    public GameObject DefeatPanel;

    [Header("Textos - Vitória")]
    public TextMeshProUGUI victoryEnemiesDefeatedText;
    public TextMeshProUGUI victoryElapsedTimeText;
    public TextMeshProUGUI victoryMoneyText;

    [Header("Textos - Derrota")]
    public TextMeshProUGUI defeatEnemiesDefeatedText;
    public TextMeshProUGUI defeatElapsedTimeText;
    public TextMeshProUGUI defeatMoneyText;

    [Header("Botões - Vitória")]
    public Button VictoryRestart;
    public Button VictoryHome;
    public Button VictoryExit;

    [Header("Botões - Derrota")]
    public Button DefeatRestart;
    public Button DefeatHome;
    public Button DefeatExit;

    [Header("Animation Settings")]
    [SerializeField] private float panelFadeDuration = 0.5f;
    [SerializeField] private float numberLerpDuration = 1.0f;

    private static int enemiesDefeated = 0;
    private int OnStartMoney;
    private int OnStartEnemiesDefeated;

    private bool gameEnded = false;

    void Awake()
    {
        if (VictoryPanel != null) VictoryPanel.SetActive(false);
        if (DefeatPanel != null) DefeatPanel.SetActive(false);
    }

    void Start()
    {
        OnStartMoney = CoinManager.totalMoney;
        OnStartEnemiesDefeated = enemiesDefeated;

        ButtonHoverAnimator.ApplyTo(VictoryRestart);
        ButtonHoverAnimator.ApplyTo(VictoryHome);
        ButtonHoverAnimator.ApplyTo(VictoryExit);
        ButtonHoverAnimator.ApplyTo(DefeatRestart);
        ButtonHoverAnimator.ApplyTo(DefeatHome);
        ButtonHoverAnimator.ApplyTo(DefeatExit);
    }

    void OnEnable()
    {
        VictoryRestart.onClick.AddListener(HandleRestartClicked);
        VictoryHome.onClick.AddListener(HandleHomeClicked);
        VictoryExit.onClick.AddListener(HandleExitClicked);

        DefeatRestart.onClick.AddListener(HandleRestartClicked);
        DefeatHome.onClick.AddListener(HandleHomeClicked);
        DefeatExit.onClick.AddListener(HandleExitClicked);
    }

    void OnDisable()
    {
        VictoryRestart.onClick.RemoveListener(HandleRestartClicked);
        VictoryHome.onClick.RemoveListener(HandleHomeClicked);
        VictoryExit.onClick.RemoveListener(HandleExitClicked);

        DefeatRestart.onClick.RemoveListener(HandleRestartClicked);
        DefeatHome.onClick.RemoveListener(HandleHomeClicked);
        DefeatExit.onClick.RemoveListener(HandleExitClicked);
    }

    void Update()
    {
        if (gameEnded)
            return;

        if (enemyTowerStats.hp <= 0)
        {
            TriggerGameOver(isVictory: true);
        }
        else if (playerTowerStats.hp <= 0)
        {
            TriggerGameOver(isVictory: false);
        }
    }

    /// <summary>
    /// Call this method whenever an enemy is defeated (e.g., in NPC_Stats or NPC_Controller, on NPC death).
    /// </summary>
    public static void RegisterEnemyDefeated()
    {
        enemiesDefeated++;
    }

    private void TriggerGameOver(bool isVictory)
    {
        gameEnded = true;

        int elapsedSeconds = Mathf.CeilToInt(canvasManager != null ? canvasManager.ElapsedTime : 0f);
        int finalEnemies = enemiesDefeated - OnStartEnemiesDefeated;
        int finalMoney = CoinManager.totalMoney - OnStartMoney;

        if (isVictory)
        {
            StartCoroutine(FadePanelAndAnimateNumbers(VictoryPanel, finalEnemies, elapsedSeconds, finalMoney, true));
        }
        else
        {
            StartCoroutine(FadePanelAndAnimateNumbers(DefeatPanel, finalEnemies, elapsedSeconds, finalMoney, false));
        }
    }

    /// <summary>
    /// Fades the target panel in, interpolates the numbers from zero to their final values sequentially,
    /// and smoothly slows down the time scale until it freezes when the animations finish.
    /// </summary>
    private IEnumerator FadePanelAndAnimateNumbers(GameObject panel, int finalEnemies, int finalTime, int finalMoney, bool isVictory)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panel.AddComponent<CanvasGroup>();
        }

        panel.SetActive(true);
        group.alpha = 0f;

        float initialTimeScale = Time.timeScale;
        float totalDuration = panelFadeDuration + numberLerpDuration;
        float currentSequenceTime = 0f;

        float fadeElapsed = 0f;
        while (fadeElapsed < panelFadeDuration)
        {
            float dt = Time.unscaledDeltaTime;
            fadeElapsed += dt;
            currentSequenceTime += dt;

            group.alpha = Mathf.Clamp01(fadeElapsed / panelFadeDuration);
            Time.timeScale = Mathf.Lerp(initialTimeScale, 0f, currentSequenceTime / totalDuration);
            
            yield return null;
        }
        group.alpha = 1f;

        float numberElapsed = 0f;
        while (numberElapsed < numberLerpDuration)
        {
            float dt = Time.unscaledDeltaTime;
            numberElapsed += dt;
            currentSequenceTime += dt;

            float t = Mathf.Clamp01(numberElapsed / numberLerpDuration);

            int currentEnemies = Mathf.RoundToInt(Mathf.Lerp(0, finalEnemies, t));
            int currentTime = Mathf.RoundToInt(Mathf.Lerp(0, finalTime, t));
            int currentMoney = Mathf.RoundToInt(Mathf.Lerp(0, finalMoney, t));

            if (isVictory)
            {
                victoryEnemiesDefeatedText.text = "Enemies Defeated: " + currentEnemies.ToString();
                victoryElapsedTimeText.text = "Elapsed Time: " + FormatTime(currentTime);
                victoryMoneyText.text = "Money Earned: " + currentMoney.ToString();
            }
            else
            {
                defeatEnemiesDefeatedText.text = "Enemies Defeated: " + currentEnemies.ToString();
                defeatElapsedTimeText.text = "Elapsed Time: " + FormatTime(currentTime);
                defeatMoneyText.text = "Money Earned: " + currentMoney.ToString();
            }

            Time.timeScale = Mathf.Lerp(initialTimeScale, 0f, currentSequenceTime / totalDuration);

            yield return null;
        }

        if (isVictory)
        {
            victoryEnemiesDefeatedText.text = "Enemies Defeated: " + finalEnemies.ToString();
            victoryElapsedTimeText.text = "Elapsed Time: " + FormatTime(finalTime);
            victoryMoneyText.text = "Money Earned: " + finalMoney.ToString();
        }
        else
        {
            defeatEnemiesDefeatedText.text = "Enemies Defeated: " + finalEnemies.ToString();
            defeatElapsedTimeText.text = "Elapsed Time: " + FormatTime(finalTime);
            defeatMoneyText.text = "Money Earned: " + finalMoney.ToString();
        }

        Time.timeScale = 0f;
    }

    private string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void HandleRestartClicked() => InputManager.Instance.RequestRestartLevel();
    private void HandleHomeClicked() => InputManager.Instance.RequestGoHome();
    private void HandleExitClicked() => InputManager.Instance.RequestExit();
}