using UnityEngine;

/// <summary>
/// Efeito visual simples: encolhe a escala do objeto ao longo do tempo até chegar a
/// zero, e então se destrói. Usado pelo Projectile.cs para instanciar um rastro atrás
/// do projétil, mas serve para qualquer objeto que deva "sumir" suavemente.
/// </summary>
public class ShrinkAndDestroy : MonoBehaviour
{
    [Tooltip("Tempo, em segundos, até a escala chegar a zero e o objeto ser destruído.")]
    [SerializeField] private float shrinkDuration = 0.4f;

    [Tooltip("Controla a curva do encolhimento ao longo do tempo (eixo X = tempo normalizado 0→1, eixo Y = multiplicador de escala 1→0). Por padrão é linear; ajuste para um encolhimento mais rápido no início/fim.")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private Vector3 initialScale;
    private float timeElapsed = 0f;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (shrinkDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01(timeElapsed / shrinkDuration);
        float scaleMultiplier = Mathf.Max(shrinkCurve.Evaluate(t), 0f);

        transform.localScale = initialScale * scaleMultiplier;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}