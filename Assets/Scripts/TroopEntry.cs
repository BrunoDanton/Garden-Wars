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

    [Tooltip("Distância de avanço do jogador (até a torre alvo) necessária para esta tropa ser liberada. Use um valor bem alto (ex: 999) para tropas sempre disponíveis, como um soldado. Use um valor baixo, perto do limiar de 'avançado' do spawner, para tropas que só devem sair quando o jogador está avançando demais, como um caminhão. Usado apenas pelo EnemyTroopSpawner (ShouldSpawn); ignorado pelo spawner do jogador.")]
    public float maxDistanceToSpawn = 999f;

    [HideInInspector] public NPC_Stats troopStats;
    [HideInInspector] public float cooldown;
}