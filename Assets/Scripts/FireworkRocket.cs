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
[RequireComponent(typeof(Rigidbody))]
public class FireworkRocket : MonoBehaviour
{
    [Header("Voo")]
    [Tooltip("Marque se este prefab é a versão inimiga do rojão (define a direção do voo e quem é considerado alvo válido).")]
    public bool isEnemy = false;
    [SerializeField] private float speed = 10f;
    [Tooltip("Tempo máximo de voo antes de se autodestruir, caso não acerte nada.")]
    [SerializeField] private float lifeTime = 6f;
    [Tooltip("Raio usado na checagem de trajetória entre frames (mesma técnica do Projectile, evita atravessar alvos).")]
    [SerializeField] private float castRadius = 0.2f;

    [Header("Explosão")]
    [Tooltip("Raio da explosão: tudo (tropas + torre) inimigo dentro desse raio do ponto de impacto toma dano.")]
    [SerializeField] private float explosionRadius = 4f;
    [Tooltip("Opcional: prefab de efeito visual instanciado no ponto da explosão. Pode deixar vazio.")]
    [SerializeField] private GameObject explosionVfxPrefab;

    // Guarda apenas o valor de dano (via NPC_Stats.damage). Fica sempre desabilitado:
    // nunca queremos que a lógica própria de NPC_Stats (Update, TryTakeHitFrom recebendo dano,
    // etc.) rode nele, já que o rojão não tem NPC_Controller nem vida própria administrada
    // do mesmo jeito que uma tropa normal.
    private NPC_Stats damageSource;

    private Vector3 previousPosition;
    private bool hasExploded = false;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        damageSource = GetComponent<NPC_Stats>();
        if (damageSource != null)
        {
            damageSource.enabled = false; // usado só como "porta-dados" do valor de dano
        }
        else
        {
            Debug.LogWarning("FireworkRocket sem componente NPC_Stats: não vai causar dano. Adicione o componente ao prefab (pode ficar desmarcado no Inspector) e configure o campo 'Damage'.");
        }
    }

    private void Start()
    {
        previousPosition = transform.position;

        // Mesma convenção usada no resto do projeto: aliados vão para a direita, inimigos para a esquerda.
        transform.rotation = Quaternion.LookRotation(isEnemy ? Vector3.left : Vector3.right);

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (hasExploded) return;

        Vector3 newPosition = transform.position + transform.forward * speed * Time.deltaTime;
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

                if (IsValidTarget(hit.collider) && hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestHit = hit;
                }
            }

            if (bestHit.HasValue)
            {
                Explode(bestHit.Value.point);
                return;
            }
        }

        transform.position = newPosition;
        previousPosition = newPosition;
    }

    /// <summary>
    /// Reforço/fallback, igual ao Projectile: caso o SphereCast do Update não capture o contato.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (IsValidTarget(other))
        {
            Explode(transform.position);
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

        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, explosionPoint, Quaternion.identity);
        }

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

                // NOVO: isola cada alvo em try/catch. Se um alvo específico tiver uma
                // referência quebrada no Inspector (ex: Life Bar não atribuído) e lançar
                // exceção, isso não deve impedir os OUTROS alvos de tomarem dano, nem
                // impedir o rojão de se destruir no final deste método.
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

    // Ajuda a visualizar o raio de explosão no Scene View ao selecionar o prefab.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}