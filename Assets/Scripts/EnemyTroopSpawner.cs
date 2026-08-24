using UnityEngine;

public class EnemyTroopSpawner : TroopSpawner
{
    [Header("IA de economia")]
    [Tooltip("Quantos custos de tropa o inimigo sempre mantém reservados antes de investir num upgrade.")]
    [SerializeField] private float troopReserveMultiplier = 2f;
    [Tooltip("Chance de adiar um upgrade disponível, para o ritmo não ficar sempre idêntico entre partidas.")]
    [Range(0f, 1f)]
    [SerializeField] private float upgradeSkipChance = 0.15f;

    [Header("Avanço de tropas")]
    [Tooltip("Transform da torre alvo (do jogador), usada para medir o avanço das próprias tropas.")]
    [SerializeField] private Transform enemyTower;
    [Tooltip("Distância a partir da qual uma tropa é considerada 'avançada' e a pressão já está garantida.")]
    [SerializeField] private float advancedDistanceThreshold = 3f;

    // Alteração: Campo adicionado para referenciar o spawner do jogador e medir o avanço inimigo real.
    [SerializeField] private TroopSpawner playerSpawner;

    [Header("Rubber banding (facilita quando o jogador perde, dificulta quando avança)")]
    [Tooltip("Distância de avanço do jogador a partir da qual a IA já está no modo mais fácil (spawn mais lento). Sem tropas do jogador em campo conta como esse modo.")]
    [SerializeField] private float easyDistanceThreshold = 9f;
    [Tooltip("Multiplicador do intervalo de spawn quando o jogador está bem contido (facilita a IA). Ex.: 1.6 = 60% mais devagar.")]
    [SerializeField] private float easyIntervalMultiplier = 1.6f;
    [Tooltip("Multiplicador do intervalo de spawn quando o jogador está muito avançado (dificulta a IA). Ex.: 0.5 = spawna 2x mais rápido.")]
    [SerializeField] private float hardIntervalMultiplier = 0.5f;

    /// <summary>
    /// Dominance gate: each troop is only released when the player's advancement (distance from the 
    /// closest troop to enemyTower) is within what it requires in TroopEntry.maxDistanceToSpawn.
    /// An "always available" troop should have maxDistanceToSpawn = Mathf.Infinity.
    /// A troop "only when the player advances too much" should have a low value, typically close to or below advancedDistanceThreshold.
    /// </summary>
    protected override bool ShouldSpawn(int troopIndex)
    {
        // Alteração: Medição de distância modificada para buscar a tropa a partir do spawner do jogador.
        float closest = playerSpawner.ClosestTroopDistanceTo(enemyTower.position);
        return closest <= troops[troopIndex].maxDistanceToSpawn;
    }

    /// <summary>
    /// Continuous rubber banding: the closer the player is to enemyTower, the shorter the interval 
    /// between spawns (harder); the further away (player contained/losing), the longer the interval 
    /// (easier). Always clamped between easyIntervalMultiplier and hardIntervalMultiplier.
    /// </summary>
    protected override float GetSpawnIntervalMultiplier(int troopIndex)
    {
        // Alteração: Medição de distância modificada para buscar a tropa a partir do spawner do jogador.
        float closest = playerSpawner.ClosestTroopDistanceTo(enemyTower.position);
        if (closest == float.MaxValue) closest = easyDistanceThreshold;

        float t = Mathf.InverseLerp(easyDistanceThreshold, advancedDistanceThreshold, closest);
        return Mathf.Lerp(easyIntervalMultiplier, hardIntervalMultiplier, t);
    }

    protected override bool ShouldUpgrade()
    {
        float reserveForTroops = CheapestTroopCost() * troopReserveMultiplier;
        bool hasSurplus = resource - tower_Stats.toUpgradeResource >= reserveForTroops;
        if (!hasSurplus) return false;

        // Alteração: Medição de distância modificada para buscar a tropa a partir do spawner do jogador.
        float closest = playerSpawner.ClosestTroopDistanceTo(enemyTower.position);
        bool pressureSecured = closest <= advancedDistanceThreshold;

        return pressureSecured && Random.value >= upgradeSkipChance;
    }

    /// <summary>
    /// Cost of the cheapest troop among those that still compete for resources (spawnsOnTimer == false), 
    /// used as a reference reserve before investing in an upgrade. Timer troops are not included here, 
    /// as they do not compete for resources. If all troops are timer-based, the reserve drops to zero 
    /// and the resource is entirely free for upgrades.
    /// </summary>
    private float CheapestTroopCost()
    {
        float min = float.MaxValue;

        foreach (TroopEntry entry in troops)
        {
            if (entry.spawnsOnTimer) continue;
            if (entry.troopStats != null && entry.troopStats.toSpawnResource < min)
                min = entry.troopStats.toSpawnResource;
        }

        return min == float.MaxValue ? 0f : min;
    }
}