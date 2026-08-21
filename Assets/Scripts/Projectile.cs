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

    private float speed;
    private bool shooterIsEnemy;
    private NPC_Stats shooterStats;

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

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Unit_Stats targetStats = other.GetComponent<Unit_Stats>();
        if (targetStats == null) return;

        bool targetIsEnemy;
        NPC_Controller otherController = other.GetComponent<NPC_Controller>();
        Tower_Stats towerStats = other.GetComponent<Tower_Stats>();

        if (otherController != null) targetIsEnemy = otherController.isEnemy;
        else if (towerStats != null) targetIsEnemy = towerStats.isEnemy;
        else return; // não é um NPC nem uma torre, ignora (ex: terreno)

        if (targetIsEnemy == shooterIsEnemy) return; // fogo amigo, ignora e deixa o projétil continuar

        targetStats.TryTakeHitFrom(shooterStats);
        Destroy(gameObject);
    }
}