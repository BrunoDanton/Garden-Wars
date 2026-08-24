using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Tower_Stats))]
public abstract class TroopSpawner : MonoBehaviour
{
    [Tooltip("Tipos de tropa disponíveis para este spawner (até 6).")]
    public List<TroopEntry> troops = new List<TroopEntry>();

    public float resource;
    public float maxResource = 100;
    public float resourceMultiplier;
    public int level = 1;
    public Vector3 NPC_Rotation;

    [Tooltip("Tempo mínimo entre upgrades consecutivos, para não disparar vários upgrades em sequência quando o recurso acumulado é grande.")]
    [SerializeField] protected float minTimeBetweenUpgrades = 5f;

    public float upgradeCooldown;
    protected Tower_Stats tower_Stats;
    protected readonly List<Transform> activeTroops = new List<Transform>();

    /// <summary>Disparado só quando uma tropa é realmente spawnada. Envia o índice da tropa (na lista 'troops') e a duração do cooldown usado.</summary>
    public event System.Action<int, float> OnTroopSpawned;

    /// <summary>Disparado só quando a torre é realmente upada. Envia a duração do cooldown usado.</summary>
    public event System.Action<float> OnTowerUpgraded;

    protected virtual void Start()
    {
        tower_Stats = GetComponent<Tower_Stats>();

        foreach (TroopEntry entry in troops)
        {
            entry.troopStats = entry.troopPrefab.GetComponent<NPC_Stats>();
        }
    }

    protected virtual void Update()
    {
        for (int i = 0; i < troops.Count; i++)
        {
            TroopEntry entry = troops[i];

            if (entry.cooldown <= 0 && level >= entry.troopStats.unlockedAtLevel && ShouldSpawn(i))
            {
                if (entry.spawnsOnTimer || resource >= entry.troopStats.toSpawnResource)
                {
                    SpawnTroop(i);
                }
            }

            if (entry.cooldown > 0)
                entry.cooldown -= Time.deltaTime;
        }

        if (upgradeCooldown <= 0 && tower_Stats.toUpgradeResource <= resource && ShouldUpgrade())
        {
            UpgradeTower();
        }

        if (upgradeCooldown > 0)
            upgradeCooldown -= Time.deltaTime;

        if (resource <= maxResource)
            resource += Time.deltaTime * resourceMultiplier;
    }

    /// <summary>Deve retornar true no frame em que o jogador/IA pediu o spawn da tropa de índice 'troopIndex' (0 a troops.Count - 1).</summary>
    protected abstract bool ShouldSpawn(int troopIndex);
    protected abstract bool ShouldUpgrade();

    /// <summary>
    /// Multiplicador aplicado ao intervalo de spawn de uma tropa por timer (antes da variância).
    /// 1 = intervalo normal, menor que 1 = spawna mais rápido, maior que 1 = spawna mais devagar.
    /// Base retorna sempre 1; subclasses (ex.: EnemyTroopSpawner) sobrescrevem para rubber-banding.
    /// </summary>
    protected virtual float GetSpawnIntervalMultiplier(int troopIndex) => 1f;

    /// <summary>
    /// Distância (em linha reta) da tropa viva mais próxima do ponto informado, entre as
    /// instanciadas por este spawner (qualquer tipo). Descarta da lista qualquer referência já
    /// destruída (tropa morta ou destruída por outro motivo). Retorna float.MaxValue se não houver nenhuma.
    /// </summary>
    public float ClosestTroopDistanceTo(Vector3 point)
    {
        float closestSqrDist = float.MaxValue;

        for (int i = activeTroops.Count - 1; i >= 0; i--)
        {
            Transform troopTransform = activeTroops[i];

            if (troopTransform == null)
            {
                activeTroops.RemoveAt(i);
                continue;
            }

            float sqrDist = (troopTransform.position - point).sqrMagnitude;
            if (sqrDist < closestSqrDist)
                closestSqrDist = sqrDist;
        }

        return closestSqrDist == float.MaxValue ? float.MaxValue : Mathf.Sqrt(closestSqrDist);
    }

    private void SpawnTroop(int index)
    {
        TroopEntry entry = troops[index];
        GameObject prefab = entry.troopPrefab;

        float offset = transform.lossyScale.z / 2 - prefab.transform.lossyScale.z / 2;
        Vector3 position = transform.position + new Vector3(0, 0, Random.Range(-offset, offset));

        GameObject spawned = Instantiate(prefab, position, Quaternion.Euler(NPC_Rotation));
        activeTroops.Add(spawned.transform);

        float cooldownDuration;

        if (entry.spawnsOnTimer)
        {
            float variance = Random.Range(-entry.spawnIntervalVariance, entry.spawnIntervalVariance);
            float multiplier = GetSpawnIntervalMultiplier(index);
            cooldownDuration = Mathf.Max(0.1f, (entry.spawnInterval + variance) * multiplier);
        }
        else
        {
            resource -= entry.troopStats.toSpawnResource;
            cooldownDuration = entry.troopStats.spawnCooldown;
        }

        entry.cooldown = cooldownDuration;

        OnTroopSpawned?.Invoke(index, cooldownDuration);
    }

    private void UpgradeTower()
    {
        resourceMultiplier *= 1.25f;
        maxResource *= 1.25f;
        resource -= tower_Stats.toUpgradeResource;
        tower_Stats.toUpgradeResource *= 1.25f;
        upgradeCooldown = minTimeBetweenUpgrades;
        StartCoroutine(tower_Stats.LerpColor(Color.yellow, 1f));
        level++;

        OnTowerUpgraded?.Invoke(minTimeBetweenUpgrades);
        minTimeBetweenUpgrades += 15;
    }
}