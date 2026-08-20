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

    protected override bool ShouldSpawn(int troopIndex)
    {
        // Decisão de "precisa de mais pressão" independe do tipo de tropa; a escolha de QUAL
        // tropa spawnar continua a cargo do TroopSpawner (cooldown/recurso de cada uma).
        float closest = ClosestTroopDistanceTo(enemyTower.position);
        return closest > advancedDistanceThreshold;
    }

    protected override bool ShouldUpgrade()
    {
        float reserveForTroops = CheapestTroopCost() * troopReserveMultiplier;
        bool hasSurplus = resource - tower_Stats.toUpgradeResource >= reserveForTroops;
        if (!hasSurplus) return false;

        float closest = ClosestTroopDistanceTo(enemyTower.position);
        bool pressureSecured = closest <= advancedDistanceThreshold;

        return pressureSecured && Random.value >= upgradeSkipChance;
    }

    /// <summary>
    /// Custo da tropa mais barata entre as que ainda disputam recurso (spawnsOnTimer == false),
    /// usada como referência de reserva antes de investir em upgrade. Tropas por tempo não entram
    /// aqui, pois não competem pelo recurso — se todas as tropas forem por tempo, a reserva cai a
    /// zero e o recurso fica inteiramente livre para upgrades.
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