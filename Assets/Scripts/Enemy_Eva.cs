using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Eva : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float visionRange = 12f;
    [Range(10f, 180f)] public float visionFov = 75f;
    public LayerMask wallsMask;
    public GameObject bulletPrefab;
    public float shootCooldown = 0.8f;
    public float bulletSpeed = 12f;

    Transform player;
    Rigidbody2D rb;
    float cd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
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
        Vector2 dir = toPlayer.normalized;

        bool inRange = dist <= visionRange;
        bool inFov = Vector2.Angle(transform.right, dir) <= visionFov * 0.5f;

        bool hasLos = false;
        if (inRange && inFov)
        {
            var hit = Physics2D.Raycast(transform.position, dir, dist, wallsMask);
            hasLos = (hit.collider == null); // no hay pared en medio
        }

        // rotar cara al player
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.right = dir;

        // moverse si te ve
        rb.linearVelocity = hasLos ? dir * moveSpeed : Vector2.zero;

        // disparo si te ve
        cd -= Time.deltaTime;
        if (hasLos && cd <= 0f && bulletPrefab)
        {
            var go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            var b = go.GetComponent<EnemyBullet2D>();
            b.Init(dir * bulletSpeed);
            cd = shootCooldown;
        }
    }
}
