using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
public class Enemy_Plant : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public float stopDistance = 1.6f;
    public bool kiteWhenTooClose = true;

    [Header("Disparo")]
    public float visionRange = 10f;
    public LayerMask wallsMask;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 1.2f;
    public float bulletSpeed = 9f;

    [Header("Sprite Sheet (desde Project)")]
    [Tooltip("Arrastra aquí TODOS los sub-sprites del sheet (en orden de slice).")]
    public Sprite[] allFrames;     // arrastra todos los cortes
    [Tooltip("Número de columnas del sheet (ej. 4 para 4x4).")]
    public int cols = 4;
    [Tooltip("Índice de fila (0=arriba) para cada animación.")]
    public int rowIdle = 0, rowAim = 1, rowAttack = 2, rowDeath = 3;

    // Animación
    SpriteAnimator anim;
    Sprite[] idle, aim, attack, death;

    Transform player;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector3 firePointRightLocal;   // recordaremos la posición local “mirando a la derecha”

    float cd;
    bool attacking, dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        anim = GetComponent<SpriteAnimator>();
        sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white; // evitar tintes accidentales

        if (firePoint) firePointRightLocal = firePoint.localPosition;
    }

    void Start()
    {
        if (!BuildRows())
        {
            Debug.LogError("[Enemy_Plant] Asigna 'allFrames' con todos los cortes del sheet y verifica 'cols'.");
            enabled = false;
            return;
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        anim.Play(idle, 0.12f, true);
    }

    bool BuildRows()
    {
        if (allFrames == null || allFrames.Length == 0 || cols <= 0) return false;

        // Asegurar orden por nombre (SpriteName_0, _1, ...):
        var ordered = allFrames.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();

        idle   = Row(ordered, rowIdle, cols);
        aim    = Row(ordered, rowAim, cols);
        attack = Row(ordered, rowAttack, cols);
        death  = Row(ordered, rowDeath, cols);

        return idle.Length > 0 && aim.Length > 0 && attack.Length > 0 && death.Length > 0;
    }

    Sprite[] Row(Sprite[] src, int rowIndex, int c)
    {
        int start = rowIndex * c;
        int count = Mathf.Min(c, Mathf.Max(0, src.Length - start));
        Sprite[] r = new Sprite[count];
        for (int i = 0; i < count; i++) r[i] = src[start + i];
        return r;
    }

    void Update()
    {
        if (dead || !player) return;

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;
        Vector2 dir = (dist > 0.001f) ? toPlayer.normalized : Vector2.zero;

        // --- FACING sin rotar el transform (solo flipX) ---
        if (Mathf.Abs(dir.x) > 0.0001f)
        {
            bool faceLeft = dir.x < 0f;
            sr.flipX = faceLeft;

            // espejar firePoint en X
            if (firePoint)
            {
                var p = firePointRightLocal;
                p.x = faceLeft ? -Mathf.Abs(p.x) : Mathf.Abs(p.x);
                firePoint.localPosition = p;
            }
        }

        // fuera de rango
        if (dist > visionRange)
        {
            if (!attacking) anim.Play(idle, 0.12f, true);
            return;
        }

        // LOS
        bool hasLos = Physics2D.Raycast(transform.position, dir, dist, wallsMask).collider == null;

        // movimiento
        if (hasLos)
        {
            Vector2 desired = Vector2.zero;
            if (dist > stopDistance + 0.05f) desired = dir * moveSpeed;
            else if (kiteWhenTooClose && dist < stopDistance * 0.7f) desired = -dir * (moveSpeed * 0.7f);

            if (desired != Vector2.zero)
                rb.MovePosition(rb.position + desired * Time.deltaTime);
        }

        // anim base (si no está atacando)
        if (!attacking)
            anim.Play(hasLos ? aim : idle, 0.10f, true);

        // disparo
        if (!bulletPrefab) return;

        cd -= Time.deltaTime;
        if (hasLos && cd <= 0f)
        {
            StartCoroutine(AttackRoutine(dir));
            cd = shootCooldown;
        }
    }

    System.Collections.IEnumerator AttackRoutine(Vector2 dir)
    {
        attacking = true;

        if (attack != null && attack.Length > 0)
            anim.Play(attack, 0.08f, false);

        // Disparo
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        var go = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        var b = go ? go.GetComponent<EnemyBullet2D>() : null;
        if (b) b.Init(dir * bulletSpeed);

        // Duración de la anim de ataque (fallback 0.2s)
        float dur = (attack != null && attack.Length > 0) ? attack.Length * 0.08f : 0.2f;
        yield return new WaitForSeconds(dur);

        attacking = false;
        anim.Play(aim, 0.10f, true);
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        StopAllCoroutines();
        if (death != null && death.Length > 0)
            anim.Play(death, 0.10f, false);
        // Destroy(gameObject, 0.1f * (death?.Length ?? 1));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}
