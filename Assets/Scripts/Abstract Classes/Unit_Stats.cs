using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public abstract class Unit_Stats : MonoBehaviour
{
    public float hp = 10, maxHP = 10;
    [SerializeField] protected GameObject lifeBar;

    protected bool isDead = false;
    protected float timeSinceDead = 5f;
    protected LifeBarController lifeBarController;
    public int onDeathReward;

    /// <summary>
    /// Indicates whether the unit is currently dead.
    /// </summary>
    public bool IsDead => isDead;
    /// <summary>
    /// Indicates which team this unit belongs to. Each subclass decides where this information comes from.
    /// </summary>
    protected abstract bool IsEnemy { get; }

    protected virtual void Start()
    {
        lifeBarController = lifeBar.GetComponent<LifeBarController>();
        lifeBarController.ConstructLifeBar(transform.lossyScale.y, hp, IsEnemy);
    }

    protected virtual void Update()
    {
        if (hp <= 0)
        {
            timeSinceDead -= Time.deltaTime;
            if (!isDead)
            {
                isDead = true;
                OnDeath();
            }
        }

        if (timeSinceDead <= 0)
            Destroy(gameObject);
    }

    /// <summary>
    /// Hook triggered once when the unit's HP reaches zero.
    /// </summary>
    protected virtual void OnDeath() { } // Changed: Método criado para injeção de comportamento de morte

    /// <summary>
    /// Reduces HP, updates the life bar, and triggers extra effects from the subclass.
    /// </summary>
    public void ReceiveDamageFrom(NPC_Stats attacker)
    {
        hp -= attacker.damage;
        lifeBarController.TakeDamage(attacker.damage);
        OnDamaged();
    }

    /// <summary>
    /// Attempts to take a hit from an attacker. Intended to be overridden for cooldown checks.
    /// </summary>
    public virtual void TryTakeHitFrom(NPC_Stats attacker)
    {
        ReceiveDamageFrom(attacker);
    }

    /// <summary>
    /// Optional hook for extra effects when taking damage.
    /// </summary>
    protected virtual void OnDamaged() { }
}