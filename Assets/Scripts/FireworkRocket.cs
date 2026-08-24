using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tropa-habilidade tipo "rojão de fogos": ao ser comprada, sai voando reto em direção ao lado
/// inimigo (não persegue, não anda). Ao encostar numa tropa ou torre inimiga, explode e causa
/// dano em área para tudo (tropas + torre) dentro do raio de explosão.
///
/// Reaproveita o mesmo pipeline de dano do resto do jogo (Unit_Stats.TryTakeHitFrom), então a
/// UI de vida, o piscar vermelho e a economia (CoinManager ao matar inimigos) continuam
/// funcionando normalmente nos alvos atingidos.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FireworkRocket : MonoBehaviour
{
    [Header("Voo")]
    [Tooltip("Marque se este prefab é a versão inimiga do rojão (define a direção do voo e quem é considerado alvo válido).")]
    public bool isEnemy = false;
    [SerializeField] private float speed = 10f;
    [Tooltip("Tempo máximo de voo antes de se autodestruir, caso não acerte nada.")]
    [SerializeField] private float lifeTime = 6f;

    [Header("Rastro (efeito visual)")]
    [Tooltip("Prefab instanciado periodicamente na posição do foguete enquanto ele voa (ex: TrailParticle). Deixe vazio para não ter rastro.")]
    [SerializeField] private GameObject trailPrefab;
    [Tooltip("Intervalo, em segundos, entre cada leva de partículas de rastro.")]
    [SerializeField] private float trailSpawnInterval = 0.05f;
    [Tooltip("Quantas partículas são instanciadas de uma vez a cada intervalo (cada uma sorteia sua própria direção de espalhamento, ex: no TrailParticle).")]
    [SerializeField] private int trailParticlesPerSpawn = 3;

    private float trailSpawnTimer = 0f;

    [Header("Explosão")]
    [Tooltip("Raio da explosão: tudo (tropas + torre) inimigo dentro desse raio do ponto de impacto toma dano.")]
    [SerializeField] private float explosionRadius = 4f;
    [Tooltip("Prefab de estilhaço instanciado no ponto da explosão (não em cada alvo) — precisa ter o componente ExplosionDebris. Deixe vazio para não ter esse efeito.")]
    [SerializeField] private GameObject explosionDebrisPrefab;

    [Header("Perseguição")]
    [Tooltip("Raio de detecção: se houver um alvo inimigo dentro desse raio, o foguete muda gradualmente sua trajetória em direção a ele, em vez de manter a rota reta original. Mesma lógica de detecção do NPC_Controller.")]
    [SerializeField] private float detectionRadius = 5f;
    [Tooltip("Intervalo, em segundos, entre buscas por um novo alvo próximo.")]
    [SerializeField] private float retargetInterval = 0.25f;
    [Tooltip("Velocidade angular (graus/segundo) com que o foguete vira/inclina sua trajetória em direção ao alvo detectado.")]
    [SerializeField] private float turnSpeed = 120f;

    private Transform currentTarget;
    private float retargetTimer = 0f;

    private CharacterController controller; // NOVO: mesma abordagem de movimento do NPC_Controller

    private NPC_Stats damageSource;

    private bool hasExploded = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // NOVO: sem gravidade — o foguete não usa CharacterController pra lidar com queda/
        // terreno como o NPC_Controller, só reaproveita o Move() pra detectar colisão com
        // outros colliders no caminho (via OnControllerColliderHit) da mesma forma.

        damageSource = GetComponent<NPC_Stats>();
        if (damageSource != null)
        {
            damageSource.enabled = false;
        }
        else
        {
            Debug.LogWarning("FireworkRocket sem componente NPC_Stats: não vai causar dano. Adicione o componente ao prefab (pode ficar desmarcado no Inspector) e configure o campo 'Damage'.");
        }
    }

    private void Start()
    {
        transform.rotation = Quaternion.LookRotation(isEnemy ? Vector3.left : Vector3.right);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (hasExploded) return;

        if (trailPrefab != null)
        {
            trailSpawnTimer -= Time.deltaTime;
            if (trailSpawnTimer <= 0f)
            {
                trailSpawnTimer = trailSpawnInterval;
                for (int i = 0; i < trailParticlesPerSpawn; i++)
                {
                    Instantiate(trailPrefab, transform.position, transform.rotation);
                }
            }
        }

        // NOVO: procura periodicamente um alvo inimigo próximo (mesma lógica de detecção
        // do NPC_Controller) e, se houver um, vira gradualmente a trajetória em direção a
        // ele — o foguete "se inclina" rumo ao alvo em vez de manter a rota reta original.
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f)
        {
            retargetTimer = retargetInterval;
            currentTarget = FindClosestEnemy();
        }

        if (currentTarget != null)
        {
            Vector3 desiredDirection = GetClosestPointOnTarget(currentTarget) - transform.position;
            if (desiredDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
            }
        }

        // NOVO: em vez do SphereCastAll manual, usa CharacterController.Move como o
        // NPC_Controller — a gravidade não entra aqui (motion.y fica 0, o foguete nunca
        // cai), só a componente pra frente (que já reflete o giro acima). A colisão em si
        // é resolvida pelo próprio CharacterController e reportada em OnControllerColliderHit.
        Vector3 motion = transform.forward * speed;
        controller.Move(motion * Time.deltaTime);
    }

    /// <summary>
    /// Mesma lógica de FindClosestEnemy() do NPC_Controller: procura, dentro do raio de
    /// detecção, o inimigo (NPC ou torre) mais próximo, ignorando aliados.
    /// </summary>
    private Transform FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        Transform closest = null;
        float closestSqrDist = float.MaxValue;

        foreach (Collider hitCollider in hits)
        {
            if (!IsValidTarget(hitCollider)) continue;

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

    /// <summary>
    /// Ponto do collider do alvo mais próximo deste foguete (igual ao GetClosestPointOnTarget do NPC_Controller).
    /// </summary>
    private Vector3 GetClosestPointOnTarget(Transform targetTransform)
    {
        Collider targetCollider = targetTransform.GetComponent<Collider>();
        return targetCollider != null ? targetCollider.bounds.ClosestPoint(transform.position) : targetTransform.position;
    }

    /// <summary>
    /// Mesmo padrão do NPC_Controller: o CharacterController reporta aqui qualquer
    /// collider que encoste nele durante o Move(). Se for um alvo válido, explode ali mesmo.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hasExploded) return;

        if (IsValidTarget(hit.collider))
        {
            Explode(hit.point);
        }
    }

    private bool IsValidTarget(Collider other)
    {
        Unit_Stats stats = other.GetComponent<Unit_Stats>();
        if (stats == null) return false;

        bool targetIsEnemy;
        NPC_Controller otherController = other.GetComponent<NPC_Controller>();
        Tower_Stats towerStats = other.GetComponent<Tower_Stats>();

        if (otherController != null) targetIsEnemy = otherController.isEnemy;
        else if (towerStats != null) targetIsEnemy = towerStats.isEnemy;
        else return false; // não é NPC nem torre (terreno, etc.)

        return targetIsEnemy != isEnemy;
    }

    /// <summary>
    /// Explode no ponto de impacto: causa dano em área para toda tropa/torre inimiga
    /// dentro do raio de explosão, reaproveitando o mesmo pipeline de dano do resto do jogo.
    /// </summary>
    private void Explode(Vector3 explosionPoint)
    {
        hasExploded = true;
        transform.position = explosionPoint;

        SpawnExplosionDebris(explosionPoint);

        if (damageSource != null)
        {
            Collider[] hits = Physics.OverlapSphere(explosionPoint, explosionRadius);
            HashSet<Unit_Stats> alreadyHit = new HashSet<Unit_Stats>();

            foreach (Collider hit in hits)
            {
                if (!IsValidTarget(hit)) continue;

                Unit_Stats targetStats = hit.GetComponent<Unit_Stats>();
                if (targetStats == null || alreadyHit.Contains(targetStats)) continue;

                alreadyHit.Add(targetStats);

                try
                {
                    targetStats.TryTakeHitFrom(damageSource);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Falha ao aplicar dano em '{hit.gameObject.name}': {ex.Message}. Verifique se todos os campos do NPC_Stats/Unit_Stats desse prefab (ex: Life Bar) estão atribuídos no Inspector.", hit.gameObject);
                }
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Instancia o estilhaço de explosão (ExplosionDebris) no ponto de impacto do
    /// foguete — uma leva de instâncias proporcional ao dano (ex: 5 de dano = 5
    /// prefabs), cada uma se espalhando em uma direção aleatória do espaço (esfera
    /// completa), crescendo e depois diminuindo de escala.
    /// </summary>
    private void SpawnExplosionDebris(Vector3 position)
    {
        if (explosionDebrisPrefab == null || damageSource == null) return;

        int debrisCount = 7;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject effectObj = Instantiate(explosionDebrisPrefab, position, Quaternion.identity);
            ExplosionDebris effect = effectObj.GetComponent<ExplosionDebris>();
            if (effect != null)
            {
                effect.Launch();
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}