using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Material))]
public class NPC_Controller : MonoBehaviour
{
    public bool isEnemy = false;
    [SerializeField] private float speed = 5;

    [Header("Repulsão")]
    public float atkRepulsion = 5;
    [Tooltip("Reduz o quanto este NPC é empurrado. 1 = normal, >1 = mais resistente, <1 = mais frágil.")] [SerializeField] private float resistance = 1f;
    [Tooltip("Mapeia a escala do NPC (localScale.x) para um multiplicador de repulsão/knockback.")] [SerializeField] private AnimationCurve scaleRepulsionCurve = AnimationCurve.Linear(0, 1, 3, 3);
    [SerializeField] private float baseUpwardForce = 5f;
    [SerializeField] private float baseStunDuration = 0.3f;

    private Rigidbody rb;
    private bool isGrounded = true;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.5f;
    [SerializeField] private LayerMask groundMask;

    [SerializeField] private float collisionFeedBackDuration = 1f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private MeshRenderer meshRenderer;
    private Color materialColor;

    private float stunTimer = 0f;
    private Coroutine colorCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        meshRenderer = GetComponent<MeshRenderer>();
        materialColor = meshRenderer.material.color;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = (isEnemy ? Vector3.left : Vector3.right) * speed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (stunTimer > 0) return;

        if (!collision.gameObject.CompareTag("NPC")) return;

        NPC_Controller other = collision.transform.GetComponent<NPC_Controller>();
        if (other == null) return;

        Debug.Log("Colisao");

        float mySize = transform.localScale.x;
        float sizeMultiplier = Mathf.Max(scaleRepulsionCurve.Evaluate(mySize), 0f);

        float safeResistance = Mathf.Max(resistance, 0.01f);

        float effectiveRepulsion = (other.atkRepulsion * sizeMultiplier) / safeResistance;
        float effectiveUpward = baseUpwardForce * sizeMultiplier;
        float effectiveStun = baseStunDuration * sizeMultiplier;

        stunTimer = effectiveStun;

        Vector3 diff = transform.position - collision.transform.position;
        diff.y = 0f;
        Vector3 direction = diff.sqrMagnitude > 0.0001f
            ? diff.normalized
            : (isEnemy ? Vector3.left : Vector3.right);

        Vector3 knockback = (direction * effectiveRepulsion) + (Vector3.up * effectiveUpward);

        rb.AddForce(knockback, ForceMode.VelocityChange);

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        colorCoroutine = StartCoroutine(LerpColor(Color.red, collisionFeedBackDuration));
    }

    /// <summary>
    /// Transitions the material color to a target color and then back to the original color over a specified duration using Color.Lerp.
    /// Evaluated via an Animation Curve to break the linear visual illusion.
    /// </summary>
    /// <param name="targetColor">The target color to transition to during the collision.</param>
    /// <param name="duration">The total time the color transition should take.</param>
    /// <returns>IEnumerator for Coroutine execution.</returns>
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