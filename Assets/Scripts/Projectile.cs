using UnityEngine;

/// <summary>
/// Projétil disparado por uma tropa à distância. Viaja em linha reta até acertar
/// um alvo inimigo válido (NPC ou torre) ou até estourar seu tempo de vida.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Tooltip("Tempo máximo que o projétil existe antes de ser destruído, caso não acerte nada.")]
    [SerializeField] private float lifeTime = 5f;

    [Tooltip("Raio usado na checagem de trajetória (SphereCast) entre frames. Deixe próximo do raio visual do projétil.")]
    [SerializeField] private float castRadius = 0.1f;

    [Tooltip("Aceleração da gravidade aplicada ao projétil (negativa = puxa pra baixo). Ajuste para combinar com o quão \"arqueado\" o tiro deve parecer.")]
    [SerializeField] private float gravity = -20f;

    [Header("Rastro (efeito visual)")]
    [Tooltip("Prefab instanciado periodicamente na posição do projétil, deixando um rastro. Precisa ter o componente ShrinkAndDestroy (ou similar). Deixe vazio para não ter rastro.")]
    [SerializeField] private GameObject trailPrefab;
    [Tooltip("Intervalo, em segundos, entre cada instância do rastro.")]
    [SerializeField] private float trailSpawnInterval = 0.05f;

    [Tooltip("Prefab instanciado no ponto de impacto ao acertar um alvo. Precisa ter o componente ImpactDebris. Deixe vazio para não ter efeito de impacto.")]
    [SerializeField] private GameObject impactEffectPrefab;

    private float trailSpawnTimer = 0f;

    private Vector3 velocity;
    private bool shooterIsEnemy;
    private NPC_Stats shooterStats;
    private Vector3 previousPosition;
    private bool isLaunched = false;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // movimento é feito manualmente no Update
        rb.useGravity = false;
    }

    /// <summary>
    /// Configura o projétil logo após ser instanciado pelo NPC_Controller.
    /// NOVO: em vez de receber uma direção fixa e voar reto, recebe o PONTO do alvo e
    /// calcula a velocidade inicial (componente horizontal + vertical) necessária para,
    /// sob a gravidade configurada, descrever uma parábola que passa exatamente por esse
    /// ponto. "projectileHorizontalSpeed" é a velocidade horizontal mantida durante o voo
    /// (o tempo de voo é derivado dela: tempo = distância horizontal / velocidade).
    /// </summary>
    public void Launch(Vector3 targetPoint, float projectileHorizontalSpeed, NPC_Stats shooter)
    {
        shooterStats = shooter;

        NPC_Controller shooterController = shooter.GetComponent<NPC_Controller>();
        shooterIsEnemy = shooterController != null && shooterController.isEnemy;

        Vector3 toTarget = targetPoint - transform.position;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDistance = toTargetFlat.magnitude;

        if (horizontalDistance > 0.0001f && projectileHorizontalSpeed > 0.0001f)
        {
            Vector3 horizontalDir = toTargetFlat / horizontalDistance;
            float flightTime = horizontalDistance / projectileHorizontalSpeed;

            // Equação de MRUV isolando a velocidade vertical inicial: partindo daqui,
            // qual v_y faz o projétil estar exatamente na altura do alvo (toTarget.y)
            // quando o tempo de voo horizontal (flightTime) terminar, já descontando
            // a queda causada pela gravidade ao longo desse tempo.
            float verticalVelocity = (toTarget.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

            velocity = horizontalDir * projectileHorizontalSpeed + Vector3.up * verticalVelocity;
        }
        else
        {
            // Alvo praticamente em cima do ponto de disparo (ou velocidade inválida):
            // não há distância horizontal para calcular uma parábola, então atira reto.
            velocity = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized * projectileHorizontalSpeed
                : transform.forward * projectileHorizontalSpeed;
        }

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        previousPosition = transform.position;
        isLaunched = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isLaunched) return;

        // Integra a gravidade na velocidade antes de mover (Euler semi-implícito), o que
        // faz o projétil curvar para baixo ao longo do voo em vez de manter uma reta.
        velocity.y += gravity * Time.deltaTime;

        Vector3 newPosition = transform.position + velocity * Time.deltaTime;

        // Gira o projétil para acompanhar visualmente a curva da trajetória (aponta mais
        // para baixo conforme a velocidade vertical vai ficando negativa).
        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        // NOVO: a cada "trailSpawnInterval" segundos, instancia o prefab de rastro na
        // posição atual do projétil. O prefab cuida de encolher e se destruir sozinho
        // (via ShrinkAndDestroy), então aqui só precisamos criá-lo e esquecê-lo.
        if (trailPrefab != null)
        {
            trailSpawnTimer -= Time.deltaTime;
            if (trailSpawnTimer <= 0f)
            {
                trailSpawnTimer = trailSpawnInterval;
                Instantiate(trailPrefab, transform.position, transform.rotation);
            }
        }

        // NOVO: em vez de só mover e esperar o OnTriggerEnter acontecer (que pode falhar
        // em colliders não-convexos ou "vazar" se o projétil for rápido demais para o
        // intervalo entre frames), verificamos ativamente o trajeto percorrido neste frame
        // usando SphereCast. Isso é robusto contra qualquer tipo de collider (convexo ou
        // não) e contra "tunneling" (o projétil pulando por cima de um alvo fino/rápido).
        Vector3 segment = newPosition - previousPosition;
        float segmentDistance = segment.magnitude;

        if (segmentDistance > 0.0001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(previousPosition, castRadius, segment.normalized, segmentDistance);

            RaycastHit? bestHit = null;
            float bestDist = float.MaxValue;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue; // ignora a si mesmo

                if (IsValidTarget(hit.collider, out Unit_Stats targetStats))
                {
                    if (hit.distance < bestDist)
                    {
                        bestDist = hit.distance;
                        bestHit = hit;
                    }
                }
            }

            if (bestHit.HasValue)
            {
                Unit_Stats targetStats = bestHit.Value.collider.GetComponent<Unit_Stats>();
                transform.position = bestHit.Value.point;
                targetStats.TryTakeHitFrom(shooterStats);
                SpawnImpactEffect(bestHit.Value.point, velocity); // NOVO
                Destroy(gameObject);
                return;
            }
        }

        transform.position = newPosition;
        previousPosition = newPosition;
    }

    /// <summary>
    /// Checa se o collider atingido é um alvo inimigo válido (NPC ou torre), ignorando fogo amigo.
    /// </summary>
    private bool IsValidTarget(Collider other, out Unit_Stats targetStats)
    {
        targetStats = other.GetComponent<Unit_Stats>();
        if (targetStats == null) return false;

        bool targetIsEnemy;
        NPC_Controller otherController = other.GetComponent<NPC_Controller>();
        Tower_Stats towerStats = other.GetComponent<Tower_Stats>();

        if (otherController != null) targetIsEnemy = otherController.isEnemy;
        else if (towerStats != null) targetIsEnemy = towerStats.isEnemy;
        else return false; // não é um NPC nem uma torre, ignora (ex: terreno)

        if (targetIsEnemy == shooterIsEnemy) return false; // fogo amigo, ignora

        return true;
    }

    /// <summary>
    /// Mantido como reforço/fallback: caso o SphereCast do Update não capture o contato
    /// (ex: colisão física iniciada pelo próprio Rigidbody/collider do alvo), o trigger
    /// ainda funciona normalmente.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (IsValidTarget(other, out Unit_Stats targetStats))
        {
            targetStats.TryTakeHitFrom(shooterStats);
            SpawnImpactEffect(transform.position, velocity); // NOVO
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Instancia o prefab de efeito de impacto (ImpactDebris) no ponto de acerto — uma
    /// instância para cada ponto de dano causado (ex: 5 de dano = 5 prefabs, cada um com
    /// seu próprio ângulo de curva sorteado) —, usando a direção de voo do projétil no
    /// momento do impacto como direção inicial do efeito.
    /// </summary>
    private void SpawnImpactEffect(Vector3 position, Vector3 hitDirection)
    {
        if (impactEffectPrefab == null || shooterStats == null) return;

        int debrisCount = Mathf.Max(1, Mathf.RoundToInt(shooterStats.damage));

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject effectObj = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            ImpactDebris effect = effectObj.GetComponent<ImpactDebris>();
            if (effect != null)
            {
                effect.Launch(hitDirection);
            }
        }
    }
}