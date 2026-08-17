using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer))]

public class Tower_Stats : MonoBehaviour
{
    public float hp = 100;
    public bool isEnemy;
    private bool isDead = false;
    private float timeSinceDead = 5, lastHitCooldown = 0;
    
    private MeshRenderer meshRenderer;
    private Color materialColor;
    private Coroutine colorCoroutine;
    [SerializeField] private float collisionFeedBackDuration = 1f;

    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private GameObject lifeBar;
    private LifeBarController lifeBarController;
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        materialColor = meshRenderer.material.color;

        lifeBarController = lifeBar.GetComponent<LifeBarController>();
        lifeBarController.ConstructLifeBar(transform.lossyScale.y, hp, isEnemy);
    }

    void Update()
    {
        if (hp <= 0)
        {
            timeSinceDead -= Time.deltaTime;
            if (!isDead)
            {
            isDead = true;
            transform.GetComponent<BoxCollider>().enabled = false;
            }
        }

        if (lastHitCooldown > 0)
        {
            lastHitCooldown -= Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("NPC"))
        {
            if (other.transform.GetComponent<NPC_Controller>().isEnemy != isEnemy)
            {
                NPC_Stats otherStats = other.transform.GetComponent<NPC_Stats>();
                hp -= otherStats.damage;
                lifeBarController.TakeDamage(otherStats.damage);

                if (colorCoroutine != null)
                {
                    StopCoroutine(colorCoroutine);
                }
                colorCoroutine = StartCoroutine(LerpColor(Color.red, collisionFeedBackDuration));
                lastHitCooldown = 1;
            }
        }
    }

    void OnCollisionStay(Collision other)
    {
        if (lastHitCooldown <= 0 && other.gameObject.CompareTag("NPC"))
        {
            if (other.transform.GetComponent<NPC_Controller>().isEnemy != isEnemy)
            {
                NPC_Stats otherStats = other.transform.GetComponent<NPC_Stats>();
                hp -= otherStats.damage;
                lifeBarController.TakeDamage(otherStats.damage);

                if (colorCoroutine != null)
                {
                    StopCoroutine(colorCoroutine);
                }
                colorCoroutine = StartCoroutine(LerpColor(Color.red, collisionFeedBackDuration));
                lastHitCooldown = 1;
            }
        }
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

