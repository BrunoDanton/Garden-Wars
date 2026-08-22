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
    private Collider targetCollider = null; // NOVO: usado para calcular distância/mira pela superfície real do collider, não pelo pivô
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

        // NOVO: sempre reconfigura (idempotente/barato) em vez de configurar só uma vez.
        // A trava estática antiga podia "sobreviver" entre execuções do Play Mode se
        // "Reload Domain" estiver desligado em Enter Play Mode Settings, mesmo com a
        // matriz de colisão do motor de física sendo resetada a cada novo Play — fazendo
        // essas regras nunca serem reaplicadas a partir da segunda execução em diante.
        Physics.IgnoreLayerCollision(allyLayer, allyLayer, true);
        Physics.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        Physics.IgnoreLayerCollision(allyLayer, enemyLayer, false);
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
                targetCollider = (target != null) ? target.GetComponent<Collider>() : null; // NOVO
            }

            // NOVO: calcula o ponto mais próximo do collider real do alvo (não o pivô).
            // Isso é essencial pra alvos grandes como a torre, cujo pivô pode estar
            // longe da superfície que a tropa realmente precisa alcançar.
            Vector3 closestTargetPoint = target != null ? GetClosestPointOnTarget() : Vector3.zero;

            bool inRangeToStop = false;
            if (isRanged && target != null)
            {
                Vector3 flatDiff = closestTargetPoint - transform.position;
                flatDiff.y = 0f;
                inRangeToStop = flatDiff.magnitude <= attackRange;
            }

            Vector3 rawDirection = (target == null)
                ? (isEnemy ? Vector3.left : Vector3.right)
                : (closestTargetPoint - transform.position);

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
        if (isRanged) return; // tropas ranged nunca causam dano por contato, só por projétil
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
    /// Retorna o ponto mais próximo da superfície do collider do alvo em relação a esta tropa.
    /// Cai de volta para o pivô (target.position) se o alvo não tiver um Collider acessível.
    /// </summary>
    // NOVO
    private Vector3 GetClosestPointOnTarget()
    {
        if (targetCollider != null)
        {
            // IMPORTANTE: Collider.ClosestPoint() só funciona corretamente em colliders
            // CONVEXOS (Box, Sphere, Capsule, ou Mesh Collider com "Convex" marcado).
            // Se a Torre usa um Mesh Collider não-convexo (bem comum em modelos maiores/
            // detalhados), aquele método falha silenciosamente e devolve valores errados.
            // Bounds.ClosestPoint() usa a caixa delimitadora (AABB) do collider, o que
            // funciona para QUALQUER tipo de collider, convexo ou não.
            return targetCollider.bounds.ClosestPoint(transform.position);
        }
        return target.position;
    }

    /// <summary>
    /// Dispara um projétil em direção ao alvo atual, respeitando o cooldown de ataque
    /// da tropa (mesmo cooldown usado pelo ataque corpo a corpo).
    /// </summary>
    private void TryRangedAttack()
    {
        if (myStats == null || !myStats.CanAttack() || projectilePrefab == null || target == null) return;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        // NOVO: mira no centro real do collider do alvo (inclui altura), em vez de assumir
        // que o alvo está sempre na mesma altura da tropa. Resolve o projétil "passando por
        // baixo" de alvos grandes como a torre.
        Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center : target.position;

        Vector3 direction = aimPoint - spawnPosition;
        if (direction.sqrMagnitude < 0.0001f) return;

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
        // NOVO: se attackRange for maior que detectionRadius (ex: ajustado no Inspector
        // durante testes), a tropa nunca conseguiria detectar algo que ela teoricamente
        // já poderia atacar. Isso garante consistência entre os dois valores.
        float effectiveDetectionRadius = isRanged ? Mathf.Max(detectionRadius, attackRange) : detectionRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position, effectiveDetectionRadius);

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

            // 4. NOVO: usa o ponto mais próximo do collider (não o pivô) para escolher
            // o alvo realmente mais próximo. Crítico para objetos grandes como a torre,
            // cujo pivô pode estar bem longe da superfície de contato real.
            // Usa Bounds.ClosestPoint (funciona em qualquer tipo de collider) em vez de
            // Collider.ClosestPoint (só funciona em colliders convexos).
            Vector3 closestPoint = hitCollider.bounds.ClosestPoint(transform.position);
            float sqrDist = (closestPoint - transform.position).sqrMagnitude;
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