using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class NPC_Stats : MonoBehaviour
{
    public float hp = 10;
    [SerializeField] private float damage = 2;
    private bool isDead = false;
    private float timeSinceDead = 5;

    

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

        if (timeSinceDead <= 0)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("NPC"))
        {
            if (other.transform.GetComponent<NPC_Controller>().isEnemy != GetComponent<NPC_Controller>().isEnemy)
            {
                NPC_Stats otherStats = other.transform.GetComponent<NPC_Stats>();
                otherStats.hp -= damage;
            }
        }
    }
}

