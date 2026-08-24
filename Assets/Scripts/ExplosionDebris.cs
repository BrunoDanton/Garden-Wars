using UnityEngine;

/// <summary>
/// Efeito visual de estilhaço de EXPLOSÃO (ex: usado pelo FireworkRocket): diferente do
/// ImpactDebris (que sai numa direção específica, tipo parábola de quem acertou), este
/// voa numa direção sorteada em qualquer sentido do espaço (esfera completa — pra cima,
/// pros lados, pra trás, etc.), e a escala primeiro CRESCE e depois DIMINUI ao longo da
/// vida (em vez de só encolher), simulando o estouro da explosão.
/// </summary>
public class ExplosionDebris : MonoBehaviour
{
    [Header("Voo")]
    [Tooltip("Velocidade inicial do estilhaço ao ser lançado.")]
    [SerializeField] private float launchSpeed = 4f;
    [Tooltip("Aceleração da gravidade (negativa = puxa pra baixo). Deixe 0 para o estilhaço não cair, só se afastar do centro da explosão.")]
    [SerializeField] private float gravity = -8f;

    [Header("Escala (cresce e depois diminui)")]
    [Tooltip("Tempo total, em segundos, até o estilhaço sumir e ser destruído.")]
    [SerializeField] private float lifeTime = 0.5f;
    [Tooltip("Curva de escala ao longo do tempo (eixo X = tempo normalizado 0→1, eixo Y = multiplicador de escala). Por padrão começa em 0, cresce rápido até 1 (pico) e depois volta a 0 — ajuste as chaves para mudar o quão rápido cresce/diminui.")]
    [SerializeField]
    private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Cor")]
    [Tooltip("Cor para a qual a partícula vai fazendo lerp ao longo da vida.")]
    [SerializeField] private Color targetColor = Color.white;

    private Vector3 velocity;
    private Vector3 initialScale;
    private float timeElapsed = 0f;
    private bool isLaunched = false;

    private Renderer particleRenderer;
    private Color startColor;

    private void Awake()
    {
        initialScale = transform.localScale;

        particleRenderer = GetComponent<Renderer>();
        if (particleRenderer != null)
        {
            startColor = particleRenderer.material.color; // acessar ".material" já cria uma instância própria, então não afeta outras partículas
        }
    }

    /// <summary>
    /// Lança o estilhaço numa direção aleatória em qualquer sentido do espaço 3D
    /// (Random.onUnitSphere), simulando uma explosão se espalhando pra todo lado.
    /// </summary>
    public void Launch()
    {
        velocity = Random.onUnitSphere * launchSpeed;
        timeElapsed = 0f;
        isLaunched = true;
    }

    private void Update()
    {
        if (!isLaunched) return;

        timeElapsed += Time.deltaTime;

        velocity.y += gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        float t = Mathf.Clamp01(lifeTime > 0f ? timeElapsed / lifeTime : 1f);
        float scaleMultiplier = Mathf.Max(scaleCurve.Evaluate(t), 0f);
        transform.localScale = initialScale * scaleMultiplier;

        if (particleRenderer != null)
        {
            particleRenderer.material.color = Color.Lerp(startColor, targetColor, t);
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}