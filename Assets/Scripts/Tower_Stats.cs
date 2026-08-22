using UnityEngine;
using System.Collections;

public class Tower_Stats : Unit_Stats
{
    public bool isEnemy;
    private float lastHitCooldown = 0;
    public float toUpgradeResource = 100;

    [Header("Layers")]
    [Tooltip("Nome da Layer usada quando isEnemy = false. Precisa ser a mesma usada no NPC_Controller (allyLayerName).")]
    [SerializeField] private string allyLayerName = "Ally";
    [Tooltip("Nome da Layer usada quando isEnemy = true. Precisa ser a mesma usada no NPC_Controller (enemyLayerName).")]
    [SerializeField] private string enemyLayerName = "Enemy";

    private MeshRenderer meshRenderer;
    [SerializeField] private string deadLayerName = "DeadNPC";
    private Color materialColor;
    private Coroutine colorCoroutine;
    [SerializeField] private float collisionFeedBackDuration = 1f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    protected override bool IsEnemy => isEnemy;

    /// <summary>
    /// Coloca a torre na mesma layer do time dela (Ally/Enemy), igual às tropas.
    /// Sem isso, a torre fica na layer "Default" e as regras de colisão que já
    /// ignoram Ally-vs-Ally e Enemy-vs-Enemy nunca se aplicam a ela — resultado:
    /// tropas do próprio time colidem fisicamente com a própria torre e podem
    /// ficar travadas nela (especialmente tropas com collider grande, tipo tanques).
    /// </summary>
    private void Awake()
    {
        int allyLayer = LayerMask.NameToLayer(allyLayerName);
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (allyLayer == -1 || enemyLayer == -1)
        {
            Debug.LogError($"Layers '{allyLayerName}' e/ou '{enemyLayerName}' não existem. Crie-as em Project Settings > Tags and Layers.");
            return;
        }

        gameObject.layer = isEnemy ? enemyLayer : allyLayer;

        // NOVO: garante essas regras independentemente de alguma tropa já ter rodado
        // Awake() antes (a Torre existe na cena desde o início, pode ser a primeira
        // coisa a rodar). Chamada idempotente, sem problema em repetir.
        Physics.IgnoreLayerCollision(allyLayer, allyLayer, true);
        Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        Physics.IgnoreLayerCollision(allyLayer, enemyLayer, false);
    }

    protected override void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialColor = meshRenderer.material.color;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        if (lastHitCooldown > 0)
            lastHitCooldown -= Time.deltaTime;
    }

    /// <summary>
    /// Evaluates if the tower can take damage based on the cooldown timer.
    /// </summary>
    // Substitui os antigos métodos OnCollisionEnter e OnCollisionStay
    public override void TryTakeHitFrom(NPC_Stats attacker)
    {
        if (lastHitCooldown <= 0)
        { 
            lastHitCooldown = 1;
        }
        ReceiveDamageFrom(attacker);
    }

    protected override void OnDamaged()
    {
        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(Color.red, collisionFeedBackDuration));
    }

    /// <summary>
    /// Coroutine to visually indicate damage taken.
    /// </summary>
    public IEnumerator LerpColor(Color targetColor, float duration)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            float curveValue = flashCurve.Evaluate(timeElapsed / duration);
            meshRenderer.material.color = Color.Lerp(targetColor, materialColor, curveValue);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        meshRenderer.material.color = materialColor;
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        
        CoinManager.totalMoney += onDeathReward;
        
        int deadLayer = LayerMask.NameToLayer(deadLayerName);
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