using UnityEngine;

public class NPC_Stats : Unit_Stats
{
    public float damage = 2;
    public float spawnCooldown = 1;
    public float toSpawnResource;
    public int unlockedAtLevel = 1;

    public float attackCooldown = 1f;
    private float currentAttackCooldown = 0f;

    [Tooltip("Layer assigned when the NPC dies so it ignores the terrain.")]
    [SerializeField] private string deadLayerName = "DeadNPC";

    private NPC_Controller npcController;

    protected override bool IsEnemy => npcController.isEnemy;

    protected override void Start()
    {
        npcController = GetComponent<NPC_Controller>();
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        if (currentAttackCooldown > 0)
            currentAttackCooldown -= Time.deltaTime;
    }

    /// <summary>
    /// Checks if this unit is ready to deal damage.
    /// </summary>
    public bool CanAttack()
    {
        return currentAttackCooldown <= 0;
    }

    /// <summary>
    /// Resets the attack timer after dealing damage.
    /// </summary>
    public void ResetAttackTimer()
    {
        currentAttackCooldown = attackCooldown;
    }

    /// <summary>
    /// Applies damage and triggers physical and visual reactions on this unit's controller.
    /// </summary>
    public override void TryTakeHitFrom(NPC_Stats attacker)
    {
        if (isDead) return;
        
        base.TryTakeHitFrom(attacker); // Changed: Repassa a dedução de vida para a classe base

        // Changed: Se houver um atacante válido, dispara a repulsão e o piscar de cor no NPC_Controller
        NPC_Controller attackerController = attacker.GetComponent<NPC_Controller>();
        if (attackerController != null)
        {
            npcController.ApplyHitReaction(attackerController);
        }
    }

    /// <summary>
    /// Changes the NPC layer to fall through the terrain upon death.
    /// </summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        
        int deadLayer = LayerMask.NameToLayer(deadLayerName);
        CoinManager.totalMoney += onDeathReward;
        if (deadLayer != -1)
        {
            gameObject.layer = deadLayer;
        }
        else
        {
            Debug.LogWarning($"Layer '{deadLayerName}' não encontrada na Unity.");
        }
    }
}