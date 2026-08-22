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

    private float speed;
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
    /// </summary>
    public void Launch(Vector3 direction, float projectileSpeed, NPC_Stats shooter)
    {
        speed = projectileSpeed;
        shooterStats = shooter;

        NPC_Controller shooterController = shooter.GetComponent<NPC_Controller>();
        shooterIsEnemy = shooterController != null && shooterController.isEnemy;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized);

        previousPosition = transform.position;
        isLaunched = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isLaunched) return;

        Vector3 newPosition = transform.position + transform.forward * speed * Time.deltaTime;

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
            Destroy(gameObject);
        }
    }
}