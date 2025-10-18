using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Plant : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;          // velocidad al perseguir
    public float stopDistance = 1.6f;     // distancia a la que deja de acercarse
    public bool kiteWhenTooClose = true;  // si es true, se aleja un poco si está demasiado cerca

    [Header("Disparo")]
    public float visionRange = 10f;
    public LayerMask wallsMask;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 1.2f;
    public float bulletSpeed = 9f;

    Transform player;
    Rigidbody2D rb;
    float cd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic; // kinematic + MovePosition
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;
        Vector2 dir = (dist > 0.001f) ? toPlayer.normalized : Vector2.zero;

        // rota para mirar al jugador
        if (dir.sqrMagnitude > 0.0f)
            transform.right = dir;

        // fuera de rango: no moverse ni disparar
        if (dist > visionRange) return;

        // Raycast para línea de visión
        bool hasLos = Physics2D.Raycast(transform.position, dir, dist, wallsMask).collider == null;

        // --- MOVIMIENTO (solo si tiene línea de visión) ---
        if (hasLos)
        {
            Vector2 desired = Vector2.zero;

            if (dist > stopDistance + 0.05f)
            {
                // acercarse
                desired = dir * moveSpeed;
            }
            else if (kiteWhenTooClose && dist < stopDistance * 0.7f)
            {
                // alejarse un poco (kite)
                desired = -dir * (moveSpeed * 0.7f);
            }

            if (desired != Vector2.zero)
            {
                // Kinematic se mueve con MovePosition
                rb.MovePosition(rb.position + desired * Time.deltaTime);
            }
        }

        // --- DISPARO ---
        if (!bulletPrefab) return;

        cd -= Time.deltaTime;
        if (hasLos && cd <= 0f)
        {
            Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
            var go = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            var b = go.GetComponent<EnemyBullet2D>();
            if (b != null) b.Init(dir * bulletSpeed);
            cd = shootCooldown;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
