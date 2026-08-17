using UnityEngine;

public abstract class TroopSpawner : MonoBehaviour
{
    public GameObject troop;
    public float resource;
    public float resourceMultiplier;

    private float troopCooldown;
    private NPC_Stats troopStats;

    protected virtual void Start()
    {
        troopStats = troop.GetComponent<NPC_Stats>();
    }

    protected virtual void Update()
    {
        if (troopCooldown <= 0 && resource >= troopStats.toSpawnResource && ShouldSpawn())
        {
            SpawnTroop();
        }

        if (troopCooldown > 0)
        {
            troopCooldown -= Time.deltaTime;
        }

        resource += Time.deltaTime * resourceMultiplier;
    }

    /// <summary>
    /// Condição adicional (além de cooldown e recurso disponível) que decide se o troop deve
    /// nascer neste frame. Implementada por cada spawner concreto (automático, por input, etc).
    /// </summary>
    protected abstract bool ShouldSpawn();

    private void SpawnTroop()
    {
        float offset = transform.lossyScale.z / 2 - troop.transform.lossyScale.z / 2;
        Vector3 position = transform.position + new Vector3(0, 0, Random.Range(-offset, offset));

        Instantiate(troop, position, Quaternion.identity);
        resource -= troopStats.toSpawnResource;
        troopCooldown = troopStats.spawnCooldown;
    }
}