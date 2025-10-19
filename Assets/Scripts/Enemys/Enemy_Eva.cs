using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
public class Enemy_Eva : MonoBehaviour
{
    [Header("Percepción / Ataque")]
    public float moveSpeed = 3.5f;
    public float visionRange = 12f;
    [Range(10f, 180f)] public float visionFov = 75f;
    public LayerMask wallsMask;
    public GameObject bulletPrefab;
    public float shootCooldown = 0.8f;
    public float bulletSpeed = 12f;

    [Header("Sprite Sheet (desde Project)")]
    [Tooltip("Arrastra aquí TODOS los sub-sprites del sheet (en orden de slice).")]
    public Sprite[] allFrames;     // todos los cortes del sheet (p.ej., 4x4 = 16)
    public int cols = 4;           // columnas del sheet
    public int rowIdle = 0;        // idle pulse
    public int rowSpin = 1;        // spin / move
    public int rowAttack = 2;      // burst
    public int rowDeath = 3;       // rage/death

    // internos
    Rigidbody2D rb;
    Transform player;
    SpriteRenderer sr;
    SpriteAnimator anim;

    Sprite[] idle, spin, attack, death;

    float cd;
    bool attacking, dead;

    // Vector de mirada para FOV (no rotamos el transform)
    Vector2 facing = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white; // evitar tintes

        anim = GetComponent<SpriteAnimator>();
    }

    void Start()
    {
        // Frames desde el Inspector
        if (!BuildRows())
        {
            Debug.LogError("[Enemy_Eva] Asigna 'allFrames' (todos los cortes) y verifica 'cols'.");
            enabled = false;
            return;
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Anim inicial
        anim.Play(idle, 0.12f, true);
    }

    bool BuildRows()
    {
        if (allFrames == null || allFrames.Length == 0 || cols <= 0) return false;

        var ordered = allFrames.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
        idle   = Row(ordered, rowIdle, cols);
        spin   = Row(ordered, rowSpin, cols);
        attack = Row(ordered, rowAttack, cols);
        death  = Row(ordered, rowDeath, cols);

        return idle.Length > 0 && spin.Length > 0 && attack.Length > 0 && death.Length > 0;
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
        Vector2 dir = (dist > 0.001f) ? toPlayer.normalized : facing;

        // Actualizar vector de mirada sin rotar transform
        if (dir.sqrMagnitude > 0.0001f) facing = dir;

        // Flip visual (izq/dcha) en base a la mirada
        if (Mathf.Abs(facing.x) > 0.0001f)
            sr.flipX = facing.x < 0f;

        bool inRange = dist <= visionRange;
        bool inFov = Vector2.Angle(facing, dir) <= visionFov * 0.5f;

        bool hasLos = false;
        if (inRange && inFov)
        {
            var hit = Physics2D.Raycast(transform.position, dir, dist, wallsMask);
            hasLos = (hit.collider == null);
        }

        // moverse si te ve
        rb.linearVelocity = hasLos ? dir * moveSpeed : Vector2.zero;

        // anim base
        if (!attacking)
            anim.Play(hasLos ? spin : idle, hasLos ? 0.10f : 0.12f, true);

        // disparo si te ve
        cd -= Time.deltaTime;
        if (hasLos && cd <= 0f && bulletPrefab)
        {
            StartCoroutine(AttackRoutine(dir));
            cd = shootCooldown;
        }
    }

    System.Collections.IEnumerator AttackRoutine(Vector2 dir)
    {
        if (dead) yield break;
        attacking = true;

        // anim de ataque (burst)
        anim.Play(attack, 0.07f, false);

        // dispara sincronizado (primer frame)
        var go = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        var b = go ? go.GetComponent<EnemyBullet2D>() : null;
        if (b) b.Init(dir * bulletSpeed);

        // esperar fin de anim
        float dur = attack.Length * 0.07f;
        yield return new WaitForSeconds(dur);

        attacking = false;

        // volver a spin si sigue viendo al player, si no idle
        Vector2 toPlayer = (player ? (player.position - transform.position) : Vector3.right);
        float dist = toPlayer.magnitude;
        Vector2 newDir = (dist > 0.001f) ? toPlayer.normalized : facing;

        bool inRange = dist <= visionRange;
        bool inFov = Vector2.Angle(facing, newDir) <= visionFov * 0.5f;
        bool hasLos = false;
        if (inRange && inFov)
        {
            var hit = Physics2D.Raycast(transform.position, newDir, dist, wallsMask);
            hasLos = (hit.collider == null);
        }
        anim.Play(hasLos ? spin : idle, hasLos ? 0.10f : 0.12f, true);
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        StopAllCoroutines();
        anim.Play(death, 0.10f, false);
        rb.linearVelocity = Vector2.zero;
        // Destroy(gameObject, 0.1f * (death?.Length ?? 1));
    }
}
