using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class NPC_Controller : MonoBehaviour
{
    public bool isEnemy = false;
    [SerializeField] private float speed = 5;

    [Header("Layers")]
    [Tooltip("Nome da Layer usada para aliados. Precisa existir em Project Settings > Tags and Layers.")]
    [SerializeField] private string allyLayerName = "Ally";
    [Tooltip("Nome da Layer usada para inimigos. Precisa existir em Project Settings > Tags and Layers.")]
    [SerializeField] private string enemyLayerName = "Enemy";

    private static bool layersConfigured = false;

    [Header("Repulsão")]
    public float atkRepulsion = 5;
    [Tooltip("Reduz o quanto este NPC é empurrado. 1 = normal, >1 = mais resistente, <1 = mais frágil.")] [SerializeField] private float resistance = 1f;
    [Tooltip("Mapeia a escala do NPC (localScale.x) para um multiplicador de repulsão/knockback.")] [SerializeField] private AnimationCurve scaleRepulsionCurve = AnimationCurve.Linear(0, 1, 3, 3);
    [SerializeField] private float baseUpwardForce = 5f;
    [SerializeField] private float baseStunDuration = 0.3f;
    [Tooltip("Velocidade com que o impulso de knockback é absorvido/desacelerado.")]
    [SerializeField] private float knockbackDamping = 5f;

    [Header("Detecção de inimigos")]
    [SerializeField] private float retargetInterval = 0.25f;
    [Tooltip("Raio usado para procurar inimigos próximos (substitui o antigo trigger).")]
    [SerializeField] private float detectionRadius = 8f;

    [Header("Ataque à distância")]
    [Tooltip("Se marcado, esta tropa para a uma certa distância do alvo e atira projéteis, em vez de avançar até o contato corpo a corpo.")]
    [SerializeField] private bool isRanged = false;
    [Tooltip("Distância que a tropa mantém do alvo antes de parar e começar a atirar. Ignorado se 'isRanged' estiver desmarcado.")]
    [SerializeField] private float attackRange = 6f;
    [Tooltip("Prefab do projétil disparado por esta tropa. Precisa ter o componente Projectile.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Ponto de onde o projétil nasce. Se vazio, usa a posição da própria tropa.")]
    [SerializeField] private Transform firePoint;
    [Tooltip("Velocidade de voo do projétil.")]
    [SerializeField] private float projectileSpeed = 15f;

    [Header("Terreno (CharacterController)")]
    [Tooltip("Altura máxima de degrau que o NPC sobe automaticamente.")]
    [SerializeField] private float stepOffset = 0.4f;
    [Tooltip("Inclinação máxima de rampa que o NPC consegue subir, em graus.")]
    [SerializeField] private float slopeLimit = 45f;
    [SerializeField] private float gravity = -20f;
    [Tooltip("Pequena velocidade negativa mantida enquanto encostado no chão, para o controller não 'flutuar'.")]
    [SerializeField] private float groundedStickVelocity = -2f;

    [SerializeField] private float collisionFeedBackDuration = 1f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CharacterController controller;
    private MeshRenderer meshRenderer;
    private Color materialColor;

    private float stunTimer = 0f;
    private Coroutine colorCoroutine;

    private Transform target = null;
    private Unit_Stats targetStats = null;
    private float retargetTimer = 0f;

    private float verticalVelocity = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;

    private NPC_Stats myStats;

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
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepOffset;
        controller.slopeLimit = slopeLimit;

        meshRenderer = GetComponent<MeshRenderer>();
        materialColor = meshRenderer.material.color;

        myStats = GetComponent<NPC_Stats>();
    }

    void Update()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickVelocity;
        else
            verticalVelocity += gravity * Time.deltaTime;

        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);

        Vector3 horizontalMotion = Vector3.zero;

        if (myStats != null && myStats.IsDead)
        {
            horizontalMotion = Vector3.zero;
        }
        else if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }
        else
        {
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f)
            {
                retargetTimer = retargetInterval;
                target = FindClosestEnemy();
                targetStats = (target != null) ? target.GetComponent<Unit_Stats>() : null;
            }

            // NOVO: se a tropa é ranged e já está a uma distância <= attackRange do alvo,
            // ela para de andar (nunca recua) e tenta atirar.
            bool inRangeToStop = false;
            if (isRanged && target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                inRangeToStop = distanceToTarget <= attackRange;
            }

            Vector3 rawDirection = (target == null)
                ? (isEnemy ? Vector3.left : Vector3.right)
                : (target.position - transform.position);

            rawDirection.y = 0f;
            Vector3 moveDirection = rawDirection.sqrMagnitude > 0.0001f ? rawDirection.normalized : Vector3.zero;

            horizontalMotion = inRangeToStop ? Vector3.zero : moveDirection * speed;

            if (inRangeToStop)
            {
                TryRangedAttack();
            }
        }

        if (target != null && targetStats.IsDead == true)
        {
            target = null;
        }

        Vector3 motion = horizontalMotion + knockbackVelocity;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isRanged) return; // NOVO: tropas ranged nunca causam dano por contato, só por projétil
        if (stunTimer > 0f) return;
        if (myStats != null && myStats.IsDead) return;

        Unit_Stats targetStats = hit.gameObject.GetComponent<Unit_Stats>();
        if (targetStats != null)
        {
            if (myStats != null)
            {
                bool targetIsEnemy = isEnemy;
                NPC_Controller otherController = hit.gameObject.GetComponent<NPC_Controller>();
                Tower_Stats towerStats = hit.gameObject.GetComponent<Tower_Stats>();

                if (otherController != null) targetIsEnemy = otherController.isEnemy;
                else if (towerStats != null) targetIsEnemy = towerStats.isEnemy;

                if (targetIsEnemy != isEnemy && myStats.CanAttack())
                {
                    targetStats.TryTakeHitFrom(myStats);
                    myStats.ResetAttackTimer();
                }
            }
        }
    }

    /// <summary>
    /// Dispara um projétil em direção ao alvo atual, respeitando o cooldown de ataque
    /// da tropa (mesmo cooldown usado pelo ataque corpo a corpo).
    /// </summary>
    // NOVO
    private void TryRangedAttack()
    {
        if (myStats == null || !myStats.CanAttack() || projectilePrefab == null || target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Launch(direction, projectileSpeed, myStats);
        }
        else
        {
            Debug.LogWarning("O projectilePrefab não possui o componente Projectile.");
        }

        myStats.ResetAttackTimer();
    }

    /// <summary>
    /// Computes and applies physics knockback and color flash dynamically based on the attacker stats.
    /// </summary>
    public void ApplyHitReaction(NPC_Controller attacker)
    {
        float attackerSize = attacker.transform.localScale.x;
        float sizeMultiplier = Mathf.Max(scaleRepulsionCurve.Evaluate(attackerSize), 0f);

        float safeResistance = Mathf.Max(resistance, 0.01f);

        float effectiveRepulsion = (attacker.atkRepulsion * sizeMultiplier) / safeResistance;
        float effectiveUpward = baseUpwardForce * sizeMultiplier;
        float effectiveStun = baseStunDuration * sizeMultiplier;

        stunTimer = effectiveStun;

        Vector3 diff = transform.position - attacker.transform.position;
        diff.y = 0f;
        Vector3 direction = diff.sqrMagnitude > 0.0001f
            ? diff.normalized
            : (isEnemy ? Vector3.left : Vector3.right);

        knockbackVelocity = direction * effectiveRepulsion;
        verticalVelocity = effectiveUpward;

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        colorCoroutine = StartCoroutine(LerpColor(Color.red, collisionFeedBackDuration));
    }

    private Transform FindClosestEnemy()
    {
    Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

    Transform closest = null;
    float closestSqrDist = float.MaxValue;

    foreach (Collider hitCollider in hits)
    {
        // 1. Garante que o alvo é uma unidade ou torre válida (ambos herdam de Unit_Stats)
        Unit_Stats candidateStats = hitCollider.GetComponent<Unit_Stats>();
        if (candidateStats == null) continue;

        // 2. Descobre de quem é o alvo e se é inimigo (mesma lógica do Projectile.cs)
        bool isTargetEnemy;
        NPC_Controller candidateNPC = hitCollider.GetComponent<NPC_Controller>();
        Tower_Stats candidateTower = hitCollider.GetComponent<Tower_Stats>();

        if (candidateNPC != null)
        {
            if (candidateNPC == this) continue; // Ignora a si mesmo
            isTargetEnemy = candidateNPC.isEnemy;
        }
        else if (candidateTower != null)
        {
            isTargetEnemy = candidateTower.isEnemy;
        }
        else
        {
            continue; // Não é NPC nem Torre (terreno, etc)
        }

        // 3. Se for aliado (fogo amigo), ignora
        if (isTargetEnemy == isEnemy) continue;

        // 4. Calcula a distância para pegar o mais próximo
        float sqrDist = (hitCollider.transform.position - transform.position).sqrMagnitude;
        if (sqrDist < closestSqrDist)
        {
            closestSqrDist = sqrDist;
            closest = hitCollider.transform;
        }
    }

    return closest;
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