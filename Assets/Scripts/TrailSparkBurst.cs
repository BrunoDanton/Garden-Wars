using UnityEngine;
using System.Collections;

/// <summary>
/// Efeito visual de rastro "estourado": em vez de encolher sozinho como o
/// ShrinkAndDestroy, este script instancia vários fragmentos a partir da própria
/// posição, cada um se afastando ao longo dos eixos Z e Y locais (direção/distância
/// sorteada por fragmento) enquanto a cor faz lerp até "targetColor" (branco por
/// padrão) e a escala encolhe até sumir — tudo via corrotina, uma por fragmento.
/// Use no lugar do ShrinkAndDestroy quando quiser um rastro se espalhando em várias
/// peças em vez de uma única sumindo no lugar.
/// </summary>
public class TrailSparkBurst : MonoBehaviour
{
    [Header("Fragmentos")]
    [Tooltip("Prefab de cada fragmento do rastro. Precisa ter um Renderer — o material é acessado via '.material', então o Unity já cria uma instância própria por fragmento automaticamente (a cor de um não afeta os outros).")]
    [SerializeField] private GameObject fragmentPrefab;
    [Tooltip("Quantos fragmentos são instanciados de uma vez, ao criar este rastro.")]
    [SerializeField] private int fragmentCount = 5;

    [Header("Separação")]
    [Tooltip("Distância máxima (pra qualquer lado) que cada fragmento percorre no eixo Y local até o fim da vida.")]
    [SerializeField] private float maxSeparationY = 0.5f;
    [Tooltip("Distância máxima (pra qualquer lado) que cada fragmento percorre no eixo Z local até o fim da vida.")]
    [SerializeField] private float maxSeparationZ = 0.5f;

    [Header("Cor e vida")]
    [Tooltip("Cor para a qual cada fragmento vai fazendo lerp ao longo da vida.")]
    [SerializeField] private Color targetColor = Color.white;
    [Tooltip("Tempo, em segundos, até cada fragmento sumir (encolher a zero) e ser destruído.")]
    [SerializeField] private float lifeTime = 0.4f;
    [Tooltip("Controla a curva de encolhimento ao longo do tempo (eixo X = tempo normalizado 0→1, eixo Y = multiplicador de escala 1→0). Por padrão é linear.")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private void Start()
    {
        if (fragmentPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = Instantiate(fragmentPrefab, transform.position, transform.rotation);
            StartCoroutine(AnimateFragment(fragment));
        }

        // Este objeto só serve pra disparar os fragmentos — cada um agora se anima e se
        // destrói sozinho na própria corrotina, então não precisamos mais dele.
        Destroy(gameObject);
    }

    /// <summary>
    /// Corrotina de um único fragmento: sorteia um deslocamento nos eixos Z/Y locais
    /// (relativo à rotação do rastro no instante em que foi criado) e, ao longo de
    /// "lifeTime", interpola (lerp) posição, cor e escala até o fragmento sumir.
    /// </summary>
    private IEnumerator AnimateFragment(GameObject fragment)
    {
        Transform fragmentTransform = fragment.transform;
        Vector3 startPosition = fragmentTransform.position;
        Vector3 startScale = fragmentTransform.localScale;

        Vector3 localOffset = new Vector3(
            0f,
            Random.Range(-maxSeparationY, maxSeparationY),
            Random.Range(-maxSeparationZ, maxSeparationZ)
        );
        Vector3 targetPosition = startPosition + fragmentTransform.TransformDirection(localOffset);

        Renderer fragmentRenderer = fragment.GetComponent<Renderer>();
        Color startColor = fragmentRenderer != null ? fragmentRenderer.material.color : targetColor;

        float elapsed = 0f;

        while (elapsed < lifeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(lifeTime > 0f ? elapsed / lifeTime : 1f);

            fragmentTransform.position = Vector3.Lerp(startPosition, targetPosition, t);

            if (fragmentRenderer != null)
            {
                fragmentRenderer.material.color = Color.Lerp(startColor, targetColor, t);
            }

            float scaleMultiplier = Mathf.Max(shrinkCurve.Evaluate(t), 0f);
            fragmentTransform.localScale = startScale * scaleMultiplier;

            yield return null;
        }

        Destroy(fragment);
    }
}