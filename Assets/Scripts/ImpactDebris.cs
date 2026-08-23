using UnityEngine;

/// <summary>
/// Efeito visual instanciado no ponto de impacto de um hit (projétil ou ataque comum).
/// Voa para longe do corpo atingido como uma parábola — a direção inicial é a direção
/// de quem acertou (ex: o voo do projétil, ou pra onde o atacante estava virado) — e,
/// ao longo do voo, a componente horizontal curva gradualmente para um ângulo aleatório
/// sorteado no Launch, dando um movimento mais orgânico do que uma parábola reta.
/// Encolhe junto com o voo até sumir e se destruir.
/// </summary>
public class ImpactDebris : MonoBehaviour
{
    [Header("Voo")]
    [Tooltip("Velocidade inicial do efeito ao ser lançado.")]
    [SerializeField] private float launchSpeed = 4f;
    [Tooltip("Aceleração da gravidade (negativa = puxa pra baixo), dá a curvatura vertical da parábola.")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Ângulo (graus) acima da direção de impacto usado como direção inicial de lançamento, para dar uma leve subida antes de cair.")]
    [SerializeField] private float upwardLaunchAngle = 25f;
    [Tooltip("Ângulo máximo (graus, pra qualquer lado) que a direção horizontal do voo pode se curvar aleatoriamente até o fim da vida do efeito.")]
    [SerializeField] private float maxRandomCurveAngle = 60f;
    [Tooltip("Velocidade angular (graus/segundo) com que a direção horizontal gira em direção ao ângulo aleatório sorteado.")]
    [SerializeField] private float curveTurnSpeed = 180f;

    [Header("Encolhimento")]
    [Tooltip("Tempo total, em segundos, até o efeito encolher a zero e ser destruído.")]
    [SerializeField] private float lifeTime = 0.6f;
    [Tooltip("Controla a curva do encolhimento ao longo do tempo (eixo X = tempo normalizado 0→1, eixo Y = multiplicador de escala 1→0).")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private Vector3 initialScale;
    private Vector3 horizontalDir; // direção horizontal (unitária) do voo, antes de aplicar a curva aleatória
    private float horizontalSpeed;
    private float verticalVelocity;
    private float targetCurveAngle;
    private float currentCurveAngle;
    private float timeElapsed = 0f;
    private bool isLaunched = false;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    /// <summary>
    /// Lança o efeito. "hitDirection" é a direção de quem acertou (velocidade do projétil
    /// no instante do impacto, ou o "forward" do atacante em ataques corpo a corpo).
    /// </summary>
    public void Launch(Vector3 hitDirection)
    {
        Vector3 flatDir = new Vector3(hitDirection.x, 0f, hitDirection.z);
        horizontalDir = flatDir.sqrMagnitude > 0.0001f ? flatDir.normalized : transform.forward;

        float rad = upwardLaunchAngle * Mathf.Deg2Rad;
        horizontalSpeed = launchSpeed * Mathf.Cos(rad);
        verticalVelocity = launchSpeed * Mathf.Sin(rad);

        targetCurveAngle = Random.Range(-maxRandomCurveAngle, maxRandomCurveAngle);
        currentCurveAngle = 0f;

        timeElapsed = 0f;
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched) return;

        timeElapsed += Time.deltaTime;

        verticalVelocity += gravity * Time.deltaTime;

        // NOVO: gira gradualmente a direção horizontal em direção ao ângulo aleatório
        // sorteado no Launch, sem afetar a componente vertical (que segue a parábola
        // normal da gravidade). Isso dá o efeito de "curvar para um ângulo aleatório".
        currentCurveAngle = Mathf.MoveTowards(currentCurveAngle, targetCurveAngle, curveTurnSpeed * Time.deltaTime);
        Vector3 curvedHorizontalDir = Quaternion.AngleAxis(currentCurveAngle, Vector3.up) * horizontalDir;

        Vector3 velocity = curvedHorizontalDir * horizontalSpeed + Vector3.up * verticalVelocity;
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(velocity.normalized);

        float t = Mathf.Clamp01(lifeTime > 0f ? timeElapsed / lifeTime : 1f);
        float scaleMultiplier = Mathf.Max(shrinkCurve.Evaluate(t), 0f);
        transform.localScale = initialScale * scaleMultiplier;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}