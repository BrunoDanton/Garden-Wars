using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Material))]
public class NPC_Controller : MonoBehaviour
{
    public bool isEnemy = false;
    [SerializeField] private float speed = 5;

    [Header("Times")]
    [Tooltip("Nome da Layer usada para aliados. Precisa existir em Project Settings > Tags and Layers.")]
    [SerializeField] private string allyLayerName = "NPC_Ally";
    [Tooltip("Nome da Layer usada para inimigos. Precisa existir em Project Settings > Tags and Layers.")]
    [SerializeField] private string enemyLayerName = "NPC_Enemy";

    private static bool layersConfigured = false;

    [Header("Repulsão")]
    public float atkRepulsion = 5;
    [Tooltip("Reduz o quanto este NPC é empurrado. 1 = normal, >1 = mais resistente, <1 = mais frágil.")] [SerializeField] private float resistance = 1f;
    [Tooltip("Mapeia a escala do NPC (localScale.x) para um multiplicador de repulsão/knockback.")] [SerializeField] private AnimationCurve scaleRepulsionCurve = AnimationCurve.Linear(0, 1, 3, 3);
    [SerializeField] private float baseUpwardForce = 5f;
    [SerializeField] private float baseStunDuration = 0.3f;

    [Header("Detecção de inimigos")]
    [SerializeField] private float retargetInterval = 0.25f;

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

    private Transform target = null;
    private readonly List<NPC_Controller> enemiesInRange = new List<NPC_Controller>();
    private float retargetTimer = 0f;

    void Awake()
    {
        int allyLayer = LayerMask.NameToLayer(allyLayerName);
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);

        if (allyLayer == -1 || enemyLayer == -1)
        {
            Debug.LogError($"Layers '{allyLayerName}' e/ou '{enemyLayerName}' não existem. Crie-as em Project Settings > Tags and Layers.");
            return;
        }

        gameObject.layer = isEnemy ? enemyLayer : allyLayer;

        if (!layersConfigured)
        {
            Physics.IgnoreLayerCollision(allyLayer, allyLayer, true);
            Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            Physics.IgnoreLayerCollision(allyLayer, enemyLayer, false);
            layersConfigured = true;
        }
    }

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

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            target = FindClosestEnemy();
        }

        if (isGrounded)
        {
            if (target == null)
                rb.linearVelocity = (isEnemy ? Vector3.left : Vector3.right) * speed;
            else
                rb.linearVelocity = (target.position - transform.position).normalized * speed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (stunTimer > 0) return;

        if (!collision.gameObject.CompareTag("NPC")) return;

        NPC_Controller other = collision.transform.GetComponent<NPC_Controller>();
        if (other == null) return;

        // Rede de segurança: mesmo que a Layer não esteja configurada corretamente,
        // aliados nunca aplicam repulsão entre si.
        if (other.isEnemy == isEnemy) return;

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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("NPC")) return;

        NPC_Controller otherNpc = other.GetComponent<NPC_Controller>();
        if (otherNpc == null || otherNpc.isEnemy == isEnemy) return;

        if (!enemiesInRange.Contains(otherNpc))
            enemiesInRange.Add(otherNpc);
    }

    void OnTriggerExit(Collider other)
    {
        NPC_Controller otherNpc = other.GetComponent<NPC_Controller>();
        if (otherNpc != null)
            enemiesInRange.Remove(otherNpc);
    }

    private Transform FindClosestEnemy()
    {
        NPC_Controller closest = null;
        float closestSqrDist = float.MaxValue;

        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            NPC_Controller candidate = enemiesInRange[i];

            if (candidate == null)
            {
                enemiesInRange.RemoveAt(i);
                continue;
            }

            float sqrDist = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = candidate;
            }
        }

        return closest != null ? closest.transform : null;
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