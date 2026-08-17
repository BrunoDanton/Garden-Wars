using UnityEngine;
using System.Collections;

public class Tower_Stats : Unit_Stats
{
    public bool isEnemy;
    private float lastHitCooldown = 0;

    private MeshRenderer meshRenderer;
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

    void OnCollisionEnter(Collision collision) => TryTakeHit(collision);

    void OnCollisionStay(Collision collision)
    {
        if (lastHitCooldown <= 0)
            TryTakeHit(collision);
    }

    private void TryTakeHit(Collision collision)
    {
        if (IsHostileNpc(collision, out NPC_Stats attacker))
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

    private IEnumerator LerpColor(Color targetColor, float duration)
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
}