using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public abstract class Unit_Stats : MonoBehaviour
{
    public float hp = 10;
    [SerializeField] protected GameObject lifeBar;

    protected bool isDead = false;
    protected float timeSinceDead = 5f;
    protected LifeBarController lifeBarController;

    /// <summary>
    /// A qual time esta unidade pertence. Cada subclasse decide de onde vem essa informação
    /// (campo próprio, componente NPC_Controller, etc).
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
                GetComponent<BoxCollider>().enabled = false;
            }
        }

        if (timeSinceDead <= 0)
            Destroy(gameObject);
    }

    /// <summary>
    /// Reduz HP, atualiza a barra de vida e dispara qualquer efeito extra da subclasse (flash, cooldown, etc).
    /// </summary>
    protected void ReceiveDamageFrom(NPC_Stats attacker)
    {
        hp -= attacker.damage;
        lifeBarController.TakeDamage(attacker.damage);
        OnDamaged();
    }

    /// <summary>Hook opcional para efeitos extras ao levar dano (ex: flash de cor).</summary>
    protected virtual void OnDamaged() { }

    /// <summary>Verifica se a colisão foi com um NPC inimigo válido e retorna suas stats.</summary>
    protected bool IsHostileNpc(Collision collision, out NPC_Stats attackerStats)
    {
        attackerStats = null;

        if (!collision.gameObject.CompareTag("NPC")) return false;

        NPC_Controller otherController = collision.transform.GetComponent<NPC_Controller>();
        if (otherController == null || otherController.isEnemy == IsEnemy) return false;

        attackerStats = collision.transform.GetComponent<NPC_Stats>();
        return attackerStats != null;
    }
}