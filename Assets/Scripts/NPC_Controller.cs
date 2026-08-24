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
    [Tooltip("Reduz o quanto este NPC é empurrado. 1 = normal, >1 = mais resistente, <1 = mais frágil.")][SerializeField] private float resistance = 1f;
    [Tooltip("Mapeia a escala do NPC (localScale.x) para um multiplicador de repulsão/knockback.")][SerializeField] private AnimationCurve scaleRepulsionCurve = AnimationCurve.Linear(0, 1, 3, 3);
    [SerializeField] private float baseUpwardForce = 5f;
    [SerializeField] private float baseStunDuration = 0.3f;
    [Tooltip("Velocidade com que o impulso de knockback é absorvido/desacelerado.")]
    [SerializeField] private float knockbackDamping = 5f;

    [Header("Detecção de inimigos")]
    [SerializeField] private float retargetInterval = 0.25f;
    [Tooltip("Raio usado para procurar inimigos próximos (substitui o antigo trigger).")]
    [SerializeField] private float detectionRadius = 8f;

    [Header("Movimento")]
    [Tooltip("Distância máxima que QUALQUER tropa pode se afastar (na direção contrária ao avanço) de onde nasceu, por vontade própria — ex: uma tropa ranged recuando/kitando. Não limita o quanto ela é empurrada por knockback de inimigos; só o movimento voluntário é travado nesse limite. Se houver uma torre aliada na cena, o que for mais restritivo entre este valor e a posição da torre prevalece (a tropa nunca recua além da própria torre).")]
    [SerializeField] private float maxVoluntaryRetreatDistance = 4f;
    [Tooltip("Multiplica a velocidade de movimento quando a tropa está recuando por vontade própria (ex: 0.5 = recua na metade da velocidade com que avança). Não afeta a velocidade de knockback.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float retreatSpeedMultiplier = 0.5f;

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
    [Tooltip("Distância mínima que a tropa tenta manter do inimigo mais próximo, como fração do 'Attack Range' (ex: 0.5 = recua se o inimigo chegar a menos da metade do alcance de ataque). Sempre menor que 'Attack Range' por construção.")]
    [Range(0f, 0.95f)]
    [SerializeField] private float retreatDistanceRatio = 0.5f;

    [Header("Impacto (efeito visual)")]
    [Tooltip("Prefab instanciado no ponto de impacto ao acertar um alvo em ataque corpo a corpo. Precisa ter o componente ImpactDebris. Deixe vazio para não ter efeito de impacto.")]
    [SerializeField] private GameObject impactEffectPrefab;

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
    private Collider targetCollider = null;
    private float retargetTimer = 0f;

    private float verticalVelocity = 0f;
    private Vector3 knockbackVelocity = Vector3.zero;

    private Vector3 spawnPosition;
    private Transform homeTower;

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

        spawnPosition = transform.position;

        Tower_Stats[] towers = FindObjectsByType<Tower_Stats>(FindObjectsSortMode.None);
        float closestTowerSqrDist = float.MaxValue;
        foreach (Tower_Stats tower in towers)
        {
            if (tower.isEnemy != isEnemy) continue;
            float sqrDist = (tower.transform.position - spawnPosition).sqrMagnitude;
            if (sqrDist < closestTowerSqrDist)
            {
                closestTowerSqrDist = sqrDist;
                homeTower = tower.transform;
            }
        }

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
                targetCollider = (target != null) ? target.GetComponent<Collider>() : null;
            }

            Vector3 closestTargetPoint = target != null ? GetClosestPointOnTarget() : Vector3.zero;

            Vector3 towardTargetFlat = Vector3.zero;
            float distanceToTarget = float.MaxValue;
            if (target != null)
            {
                towardTargetFlat = closestTargetPoint - transform.position;
                towardTargetFlat.y = 0f;
                distanceToTarget = towardTargetFlat.magnitude;
            }

            bool inRangeToStop = isRanged && target != null && distanceToTarget <= attackRange;

            float retreatDistance = attackRange * retreatDistanceRatio;
            bool shouldRetreat = isRanged && target != null && distanceToTarget < retreatDistance;

            Vector3 moveDirection;
            if (target == null)
            {
                moveDirection = isEnemy ? Vector3.left : Vector3.right;
            }
            else if (shouldRetreat)
            {
                moveDirection = towardTargetFlat.sqrMagnitude > 0.0001f ? -towardTargetFlat.normalized : Vector3.zero;
            }
            else
            {
                moveDirection = towardTargetFlat.sqrMagnitude > 0.0001f ? towardTargetFlat.normalized : Vector3.zero;
            }

            Vector3 voluntaryMotion = moveDirection * (shouldRetreat ? speed * retreatSpeedMultiplier : speed);

            Vector3 retreatAxis = isEnemy ? Vector3.right : Vector3.left;
            float retreatLimit = maxVoluntaryRetreatDistance;
            if (homeTower != null)
            {
                float towerBackwardOffset = Vector3.Dot(homeTower.position + new Vector3((isEnemy)? -5: 5, 0, 0) - spawnPosition, retreatAxis);
                retreatLimit = Mathf.Min(retreatLimit, towerBackwardOffset);
            }

            float currentBackwardOffset = Vector3.Dot(transform.position - spawnPosition, retreatAxis);
            float voluntaryBackwardComponent = Vector3.Dot(voluntaryMotion, retreatAxis);

            if (voluntaryBackwardComponent > 0f && currentBackwardOffset >= retreatLimit)
            {
                voluntaryMotion -= retreatAxis * voluntaryBackwardComponent;
            }

            horizontalMotion = (inRangeToStop && !shouldRetreat) ? Vector3.zero : voluntaryMotion;

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
        if (isRanged) return;
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
                    SpawnImpactEffect(hit.point);
                }
            }
        }
    }

    /// <summary>
    /// Instancia o prefab de efeito de impacto (ImpactDebris) no ponto de acerto — uma
    /// instância para cada ponto de dano causado (ex: 5 de dano = 5 prefabs, cada um com
    /// seu próprio ângulo de curva sorteado) —, usando a direção para a qual este
    /// atacante está virado como direção inicial do efeito.
    /// </summary>
    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectPrefab == null || myStats == null) return;

        int debrisCount = Mathf.Max(1, Mathf.RoundToInt(myStats.damage));

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject effectObj = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            ImpactDebris effect = effectObj.GetComponent<ImpactDebris>();
            if (effect != null)
            {
                effect.Launch(transform.forward);
            }
        }
    }

    /// <summary>
    /// Retorna o ponto mais próximo da superfície do collider do alvo em relação a esta tropa.
    /// Cai de volta para o pivô (target.position) se o alvo não tiver um Collider acessível.
    /// </summary>
    private Vector3 GetClosestPointOnTarget()
    {
        if (targetCollider != null)
        {
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

        Vector3 aimPoint = targetCollider != null ? targetCollider.bounds.center : target.position;

        if ((aimPoint - spawnPosition).sqrMagnitude < 0.0001f) return;

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Launch(aimPoint, projectileSpeed, myStats);
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
        float effectiveDetectionRadius = isRanged ? Mathf.Max(detectionRadius, attackRange) : detectionRadius;
        Collider[] hits = Physics.OverlapSphere(transform.position, effectiveDetectionRadius);

        Transform closest = null;
        float closestSqrDist = float.MaxValue;

        foreach (Collider hitCollider in hits)
        {
            Unit_Stats candidateStats = hitCollider.GetComponent<Unit_Stats>();
            if (candidateStats == null) continue;

            bool isTargetEnemy;
            NPC_Controller candidateNPC = hitCollider.GetComponent<NPC_Controller>();
            Tower_Stats candidateTower = hitCollider.GetComponent<Tower_Stats>();

            if (candidateNPC != null)
            {
                if (candidateNPC == this) continue;
                isTargetEnemy = candidateNPC.isEnemy;
            }
            else if (candidateTower != null)
            {
                isTargetEnemy = candidateTower.isEnemy;
            }
            else
            {
                continue;
            }

            if (isTargetEnemy == isEnemy) continue;

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