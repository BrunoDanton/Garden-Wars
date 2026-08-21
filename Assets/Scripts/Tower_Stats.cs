using UnityEngine;
using System.Collections;

public class Tower_Stats : Unit_Stats
{
    public bool isEnemy;
    private float lastHitCooldown = 0;
    public float toUpgradeResource = 100;

    private MeshRenderer meshRenderer;
    [SerializeField] private string deadLayerName = "DeadNPC";
    private Color materialColor;
    private Coroutine colorCoroutine;
    [SerializeField] private float collisionFeedBackDuration = 1f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    protected override bool IsEnemy => isEnemy;

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
            ReceiveDamageFrom(attacker);
            lastHitCooldown = 1;
        }
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