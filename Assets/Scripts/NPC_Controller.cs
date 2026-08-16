using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class NPC_Controller : MonoBehaviour
{
    public bool isEnemy = false;
    [SerializeField] private float speed = 5;
    public readonly float atkRepulsion = 5;
    private Rigidbody rb;
    private bool isGrounded = true;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.5f;
    [SerializeField] private LayerMask groundMask;

    private float stunTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        if (isGrounded)
        {
            if (isEnemy)
            {
                rb.linearVelocity = Vector3.left * speed;
            }
            else
            {
                rb.linearVelocity = Vector3.right * speed;    
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("NPC"))
        {
            Debug.Log("Colisão!!");
            
            stunTimer = 0.1f;

            if (isEnemy)
            {
                rb.AddForce((Vector3.right * collision.transform.GetComponent<NPC_Controller>().atkRepulsion) + (Vector3.up * 5), ForceMode.Impulse); 
            }
            else
            {
                rb.AddForce((Vector3.left * collision.transform.GetComponent<NPC_Controller>().atkRepulsion) + (Vector3.up * 5), ForceMode.Impulse); 
            }
        }
    }
}