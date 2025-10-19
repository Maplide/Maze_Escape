using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
public class Enemy_Raw : MonoBehaviour
{
    [Header("Detección")]
    public float visionRange = 9f;               // rango para detectar al player
    public LayerMask wallsMask;                  // capa de muros para evitar/linea de visión

    [Header("Distancias")]
    public float preferredDistance = 2.6f;       // distancia óptima
    public float tooCloseDistance = 1.6f;        // si está más cerca que esto, retrocede

    [Header("Movimiento")]
    public float maxSpeed = 3.2f;                // velocidad máxima
    public float acceleration = 10f;             // acelera hacia el objetivo
    public float damping = 6f;                   // freno cuando no hay input
    public float avoidForce = 8f;                // fuerza lateral para evitar paredes
    public float avoidProbe = 0.8f;              // longitud del “sensor” lateral

    [Header("Flotación/idle")]
    public float hoverAmplitude = 0.15f;         // oscilación vertical
    public float hoverFrequency = 2.0f;          // velocidad de la oscilación
    public float idleWanderSpeed = 1.2f;         // leve desplazamiento cuando no ve al player

    [Header("Sprite Sheet (desde Project)")]
    [Tooltip("Arrastra aquí TODOS los sub-sprites del sheet (en orden de slice).")]
    public Sprite[] allFrames;                   // sub-sprites del sheet (p.ej. 16 en 4x4)
    public int cols = 4;                         // columnas del sheet (ej. 4)
    public int rowIdle = 0, rowMove = 1, rowAttack = 2, rowDeath = 3;

    // Animación
    SpriteAnimator anim;
    SpriteRenderer sr;
    Sprite[] idle, move, attack, death;

    // Estado
    Transform player;
    Rigidbody2D rb;
    Vector2 idleDir;
    float idleTimer;
    float baseY;
    float attackFxCd;            // pequeño cooldown para anim “attack” visual
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Deja BodyType = Dynamic y congela Z en el inspector.

        sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white; // evitar tintes accidentales
        anim = GetComponent<SpriteAnimator>();
    }

    void Start()
    {
        // Cargar filas del sheet desde el array arrastrado en el Inspector
        if (!BuildRows())
        {
            Debug.LogError("[Enemy_Raw] Asigna 'allFrames' (todos los cortes) y verifica 'cols'.");
            enabled = false;
            return;
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        baseY = transform.position.y;
        PickIdleDir();

        anim.Play(idle, 0.12f, true);
    }

    bool BuildRows()
    {
        if (allFrames == null || allFrames.Length == 0 || cols <= 0) return false;

        // Ordenar por nombre (SpriteName_0, _1, ...)
        var ordered = allFrames.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();

        idle   = Row(ordered, rowIdle, cols);
        move   = Row(ordered, rowMove, cols);
        attack = Row(ordered, rowAttack, cols); // opcional si tu sheet lo tiene
        death  = Row(ordered, rowDeath, cols);

        return idle.Length > 0 && move.Length > 0 && death.Length > 0;
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
        // Oscilación “flotante” (no rota el transform)
        var pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = pos;

        // Selección de facing: flipX según la velocidad o dirección al player (sin rotación)
        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.0001f)
            sr.flipX = vel.x < 0f;
        else if (player)
            sr.flipX = (player.position.x - transform.position.x) < 0f;

        // Pequeño fx de “ataque” visual cuando está en la banda óptima (opcional)
        if (!dead && attack != null && attack.Length > 0)
        {
            attackFxCd -= Time.deltaTime;
            if (player)
            {
                float dist = Vector2.Distance(player.position, transform.position);
                if (dist >= tooCloseDistance && dist <= preferredDistance + 0.1f && attackFxCd <= 0f)
                {
                    StartCoroutine(AttackFlash());
                    attackFxCd = 1.2f; // cooldown del fx visual
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!player)
        {
            IdleMove();
            SetAnimIdleIfNeeded();
            return;
        }

        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float dist = toPlayer.magnitude;

        // Fuera de rango: deambula
        if (dist > visionRange)
        {
            IdleMove();
            SetAnimIdleIfNeeded();
            return;
        }

        Vector2 dir = (dist > 0.001f) ? toPlayer.normalized : Vector2.zero;

        // Mantener distancia óptima
        Vector2 desired = Vector2.zero;
        if (dist > preferredDistance + 0.1f)       desired = dir * maxSpeed;               // acercarse
        else if (dist < tooCloseDistance)          desired = -dir * maxSpeed * 0.8f;       // alejarse
        else
        {
            // orbitar en torno al player (tangente)
            Vector2 tangent = new Vector2(-dir.y, dir.x);
            desired = tangent * (maxSpeed * 0.6f);
        }

        // Evitación simple de paredes: usa dirección de movimiento actual como “forward”
        Vector2 fwd = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized :
                      (dir.sqrMagnitude > 0f ? dir : Vector2.right);

        Vector2 leftProbe = new Vector2(-fwd.y, fwd.x);  // +90°
        Vector2 rightProbe = new Vector2(fwd.y, -fwd.x); // -90°

        if (Physics2D.Raycast(transform.position, rightProbe, avoidProbe, wallsMask))
            desired += leftProbe * avoidForce;
        if (Physics2D.Raycast(transform.position, leftProbe, avoidProbe, wallsMask))
            desired += rightProbe * avoidForce;

        // Aceleración / damping
        Vector2 vel = rb.linearVelocity;
        Vector2 targetVel = Vector2.ClampMagnitude(desired, maxSpeed);
        Vector2 accel = (targetVel - vel) * acceleration;
        vel += accel * Time.fixedDeltaTime;

        if (desired.sqrMagnitude < 0.01f)
            vel = Vector2.Lerp(vel, Vector2.zero, damping * Time.fixedDeltaTime);

        rb.linearVelocity = vel;

        // Anim
        if (vel.sqrMagnitude > 0.05f) SetAnim(move, 0.10f, true);
        else                          SetAnim(idle, 0.12f, true);
    }

    void IdleMove()
    {
        // Deambular suave + cambiar dirección cada cierto tiempo
        idleTimer -= Time.fixedDeltaTime;
        if (idleTimer <= 0f) PickIdleDir();

        Vector2 vel = rb.linearVelocity;
        Vector2 target = idleDir * idleWanderSpeed;
        vel = Vector2.MoveTowards(vel, target, acceleration * 0.5f * Time.fixedDeltaTime);
        rb.linearVelocity = vel;
    }

    void PickIdleDir()
    {
        float a = Random.Range(0f, Mathf.PI * 2f);
        idleDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        idleTimer = Random.Range(1.2f, 2.2f);
    }

    void SetAnimIdleIfNeeded()
    {
        if (!dead) SetAnim(idle, 0.12f, true);
    }

    void SetAnim(Sprite[] frames, float rate, bool loop)
    {
        if (frames == null || frames.Length == 0) return;
        if (anim.frames == frames && anim.IsPlaying) return;
        anim.Play(frames, rate, loop);
    }

    System.Collections.IEnumerator AttackFlash()
    {
        // Solo animación visual (no daño). Si quieres daño/shot, aquí lo agregas.
        if (attack != null && attack.Length > 0)
        {
            SetAnim(attack, 0.06f, false);
            yield return new WaitForSeconds(attack.Length * 0.06f);
            SetAnim(idle, 0.12f, true);
        }
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);

        // probes de evitación
        Gizmos.color = Color.cyan;
        Vector3 fwd = Vector3.right;
        if (Application.isPlaying)
        {
            Vector2 v = rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity.normalized : Vector2.right;
            fwd = new Vector3(v.x, v.y, 0f);
        }
        Vector3 rp = new Vector3(fwd.y, -fwd.x, 0f) * avoidProbe; // -90°
        Vector3 lp = new Vector3(-fwd.y, fwd.x, 0f) * avoidProbe; // +90°
        Gizmos.DrawLine(transform.position, transform.position + rp);
        Gizmos.DrawLine(transform.position, transform.position + lp);
    }
}
