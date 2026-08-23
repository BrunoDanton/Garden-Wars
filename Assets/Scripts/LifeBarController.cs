using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
public class LifeBarController : MonoBehaviour
{
    [SerializeField] private Transform HP_Transform;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float NPC_VerticalSpacing = 0.5f;

    [Header("Divisão de barra")]
    [Tooltip("HP máximo representável por uma única barra antes de abrir uma nova acima.")] [SerializeField] private float maxHpPerBar = 20f;
    [Tooltip("Espaço vertical entre as barras empilhadas.")] [SerializeField] private float barVerticalSpacing = 0.3f;

    private bool isEnemyBar;

    private Vector3 parentLossyScale = Vector3.one;

    private class BarSegment
    {
        public Transform background;
        public Transform fill;
        public MeshRenderer fillRenderer;
        public Color fillColor;
        public Coroutine effectCoroutine;
        public float capacity;
        public float currentHp;
        public Vector3 targetScale;
        public Vector3 targetPosition;
    }

    private readonly List<BarSegment> segments = new List<BarSegment>();
    private int activeSegmentIndex;

    /// <summary>
    /// Initializes the life bar. If TotalHP exceeds maxHpPerBar, extra bars are stacked above,
    /// reusing the same width-per-HP ratio already used for a single bar.
    /// </summary>
    public void ConstructLifeBar(float height, float TotalHP, bool isEnemy)
    {
        isEnemyBar = isEnemy;

        parentLossyScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        transform.position += Vector3.up * (height / 2) + new Vector3(0, NPC_VerticalSpacing, 0);

        Vector3 baseScale = transform.localScale;

        int segmentCount = Mathf.Max(1, Mathf.CeilToInt(TotalHP / maxHpPerBar));
        float remainingHp = TotalHP;

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentCapacity = Mathf.Min(maxHpPerBar, remainingHp);
            remainingHp -= segmentCapacity;

            Transform background = transform;
            Transform fill = HP_Transform;

            if (i > 0)
            {
                background = Instantiate(transform, transform.parent);
                background.position = transform.position + Vector3.up * (barVerticalSpacing * i);

                LifeBarController dupController = background.GetComponent<LifeBarController>();
                if (dupController != null) Destroy(dupController);

                fill = background.Find(HP_Transform.name);
            }

            BuildSegment(background, fill, segmentCapacity, baseScale);
        }

        activeSegmentIndex = segments.Count - 1;
    }

    private void BuildSegment(Transform background, Transform fill, float capacity, Vector3 baseScale)
    {
        Vector3 desiredWorldScale = baseScale + Vector3.right * (capacity / 10f);
        background.localScale = SafeDivide(desiredWorldScale, parentLossyScale);

        Vector3 HP_GlobalScale = background.lossyScale + new Vector3(-0.15f, -0.15f, 0.15f);
        Vector3 HP_LocalScale = new(
            HP_GlobalScale.x / background.lossyScale.x,
            HP_GlobalScale.y / background.lossyScale.y,
            HP_GlobalScale.z / background.lossyScale.z);

        fill.localScale = HP_LocalScale;

        MeshRenderer fillRenderer = fill.GetComponent<MeshRenderer>();

        segments.Add(new BarSegment
        {
            background = background,
            fill = fill,
            fillRenderer = fillRenderer,
            fillColor = fillRenderer.material.color,
            capacity = capacity,
            currentHp = capacity,
            targetScale = HP_LocalScale,
            targetPosition = fill.localPosition
        });
    }

    private static Vector3 SafeDivide(Vector3 value, Vector3 divisor)
    {
        return new Vector3(
            divisor.x != 0f ? value.x / divisor.x : value.x,
            divisor.y != 0f ? value.y / divisor.y : value.y,
            divisor.z != 0f ? value.z / divisor.z : value.z);
    }

    /// <summary>
    /// Applies damage to the topmost non-empty segment, cascading down once it's depleted.
    /// </summary>
    public void TakeDamage(float damage)
    {
        while (damage > 0 && activeSegmentIndex >= 0)
        {
            BarSegment segment = segments[activeSegmentIndex];
            float applied = Mathf.Min(damage, segment.currentHp);
            damage -= applied;
            segment.currentHp -= applied;

            ApplySegmentDamage(segment, applied);

            if (segment.currentHp <= 0f)
                activeSegmentIndex--;
            else
                break;
        }
    }

    private void ApplySegmentDamage(BarSegment segment, float damage)
    {
        float scaleReduction = damage / segment.capacity;

        if (scaleReduction > segment.targetScale.x)
            scaleReduction = segment.targetScale.x;

        segment.targetScale -= Vector3.right * scaleReduction;

        float direction = isEnemyBar ? 1f : -1f;
        segment.targetPosition += Vector3.right * (scaleReduction / 2f) * direction;

        if (segment.effectCoroutine != null)
            StopCoroutine(segment.effectCoroutine);

        segment.effectCoroutine = StartCoroutine(LerpLifeBar(segment, Color.yellow, 0.5f, segment.targetScale, segment.targetPosition));
    }

    private IEnumerator LerpLifeBar(BarSegment segment, Color effectColor, float duration, Vector3 finalScale, Vector3 finalPosition)
    {
        float timeElapsed = 0f;

        Vector3 initialScale = segment.fill.localScale;
        Vector3 initialPosition = segment.fill.localPosition;

        while (timeElapsed < duration)
        {
            float curveValue = flashCurve.Evaluate(timeElapsed / duration);

            segment.fillRenderer.material.color = Color.Lerp(effectColor, segment.fillColor, curveValue);
            segment.fill.localScale = Vector3.Lerp(initialScale, finalScale, curveValue);
            segment.fill.localPosition = Vector3.Lerp(initialPosition, finalPosition, curveValue);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        segment.fillRenderer.material.color = segment.fillColor;
        segment.fill.localScale = finalScale;
        segment.fill.localPosition = finalPosition;

        if (finalScale.x <= 0)
        {
            Destroy(segment.background.gameObject);
        }
    }
}