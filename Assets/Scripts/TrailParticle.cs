using UnityEngine;

/// <summary>
/// Partícula individual de rastro (ex: fumaça atrás de um foguete/projétil). Diferente
/// do ShrinkAndDestroy (que encolhe parado, no lugar, por tempo), esta partícula se
/// afasta continuamente da sua própria origem nos eixos Y e Z locais (o eixo pelo qual
/// foi instanciada — perpendicular à direção de avanço de quem a soltou) e encolhe a
/// cada frame, sumindo quando atinge uma distância específica da origem — não por um
/// tempo de vida fixo. O objeto que a solta (ex: Projectile.cs) continua seguindo reto
/// pra frente e instanciando novas partículas atrás de si, cada uma com sua própria
/// direção de espalhamento sorteada.
/// </summary>
public class TrailParticle : MonoBehaviour
{
    [Header("Espalhamento")]
    [Tooltip("Velocidade com que a partícula se afasta da própria origem, nos eixos Y/Z locais (perpendiculares à direção de avanço de quem a instanciou).")]
    [SerializeField] private float driftSpeed = 1f;
    [Tooltip("Distância da origem (onde a partícula nasceu) a partir da qual ela é destruída.")]
    [SerializeField] private float maxDistanceFromOrigin = 1f;

    [Header("Encolhimento")]
    [Tooltip("Fração da escala perdida por segundo (ex: 2 = perde 200%/s, encolhe bem rápido; 0.5 = encolhe mais devagar). Aplicado a cada frame, não por uma curva de tempo fixo.")]
    [SerializeField] private float shrinkSpeed = 1.5f;

    private Vector3 origin;
    private Vector3 driftDirection;

    private void Start()
    {
        origin = transform.position;

        // NOVO: sorteia uma direção de espalhamento dentro do plano Y/Z local (o plano
        // perpendicular ao "forward" de quem instanciou a partícula), simulando a fumaça
        // se abrindo pros lados/cima/baixo enquanto o foguete segue reto na frente dela.
        Vector2 randomYZ = Random.insideUnitCircle;
        Vector3 localDir = new Vector3(0f, randomYZ.y, randomYZ.x).normalized;
        driftDirection = transform.TransformDirection(localDir);
    }

    private void Update()
    {
        transform.position += driftDirection * driftSpeed * Time.deltaTime;

        // Encolhe a escala a cada frame (proporcional ao tamanho atual, então o
        // encolhimento é mais perceptível no começo e vai suavizando).
        float shrinkFactor = Mathf.Max(0f, 1f - shrinkSpeed * Time.deltaTime);
        transform.localScale *= shrinkFactor;

        bool reachedMaxDistance = (transform.position - origin).sqrMagnitude >= maxDistanceFromOrigin * maxDistanceFromOrigin;
        bool shrankToNothing = transform.localScale.sqrMagnitude <= 0.0001f;

        if (reachedMaxDistance || shrankToNothing)
        {
            Destroy(gameObject);
        }
    }
}