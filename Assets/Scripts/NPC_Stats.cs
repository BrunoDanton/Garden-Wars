using UnityEngine;

public class NPC_Stats : Unit_Stats
{
    public float damage = 2;
    public float spawnCooldown = 1;
    public float toSpawnResource;

    private NPC_Controller npcController;

    protected override bool IsEnemy => npcController.isEnemy;

    protected override void Start()
    {
        npcController = GetComponent<NPC_Controller>();
        base.Start();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsHostileNpc(collision, out NPC_Stats attacker))
        {
            ReceiveDamageFrom(attacker);
        }
    }
}