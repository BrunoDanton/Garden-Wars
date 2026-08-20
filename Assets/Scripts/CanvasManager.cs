using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject playerHP, enemyHP, playerDomination, enemyDomination, resources;
    public Tower_Stats playerTowerStats, enemyTowerStats;
    public EnemyTroopSpawner enemyTroopSpawner;
    public PlayerTroopSpawner playerTroopSpawner;
    public TextMeshProUGUI resourcesText, towerUpgradePrice, level;

    [Tooltip("Imagem de cooldown de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6).")]
    public List<Image> troopImages = new List<Image>();

    [Tooltip("Texto de preço de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6).")]
    public List<TextMeshProUGUI> troopPriceTexts = new List<TextMeshProUGUI>();

    [Tooltip("Imagem de 'bloqueado' de cada tropa, na mesma ordem da lista 'troops' do PlayerTroopSpawner (até 6). Fica ativa enquanto o level do jogador for menor que o unlockedAtLevel da tropa.")]
    public List<Image> blockedTroops = new List<Image>();

    public Image Tower;

    private RectTransform pHP, eHP, pDom, eDom, res;
    private float pHP_Width, eHP_Width, pDom_Width, eDom_Width, res_Width;

    private Coroutine upgradeCoroutine;
    private Coroutine[] troopCoroutines;

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
    }

    void OnEnable()
    {
        playerTroopSpawner.OnTroopSpawned += HandleTroopSpawned;
        playerTroopSpawner.OnTowerUpgraded += HandleTowerUpgraded;
    }

    void OnDisable()
    {
        playerTroopSpawner.OnTroopSpawned -= HandleTroopSpawned;
        playerTroopSpawner.OnTowerUpgraded -= HandleTowerUpgraded;
    }

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

    private void HandleTroopSpawned(int index, float cooldown)
    {
        if (index < 0 || index >= troopImages.Count)
            return;

        if (troopCoroutines[index] != null)
            StopCoroutine(troopCoroutines[index]);

        troopCoroutines[index] = StartCoroutine(TroopCoolDown(troopImages[index], cooldown, index));
    }

    private void HandleTowerUpgraded(float cooldown)
    {
        if (upgradeCoroutine != null)
            StopCoroutine(upgradeCoroutine);

        upgradeCoroutine = StartCoroutine(TowerCoolDown(cooldown));
    }

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
}