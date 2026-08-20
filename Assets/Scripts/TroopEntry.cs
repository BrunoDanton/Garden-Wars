using UnityEngine;

/// <summary>
/// Representa um tipo de tropa configurável no spawner: prefab, stats e cooldown individual.
/// </summary>
[System.Serializable]
public class TroopEntry
{
    public GameObject troopPrefab;

    [Tooltip("Se marcado, esta tropa spawna sozinha em intervalos de tempo, sem custar recurso (ideal para dar ritmo constante à IA). Se desmarcado, segue o modelo padrão de recurso + pedido (ShouldSpawn), como o jogador.")]
    public bool spawnsOnTimer = false;

    [Tooltip("Intervalo entre spawns automáticos desta tropa. Usado apenas se 'spawnsOnTimer' estiver marcado.")]
    public float spawnInterval = 5f;

    [Tooltip("Variação aleatória (+/-) aplicada ao intervalo de spawn, para o ritmo não ficar sempre idêntico. Usado apenas se 'spawnsOnTimer' estiver marcado.")]
    public float spawnIntervalVariance = 0f;

    [HideInInspector] public NPC_Stats troopStats;
    [HideInInspector] public float cooldown;
}