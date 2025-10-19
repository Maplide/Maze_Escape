using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
[RequireComponent(typeof(Collider2D))]
public class Enemy_Electric : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public Transform leftPoint;
    public Transform rightPoint;

    [Tooltip("Si está activo, el cubo perseguirá al jugador SOLO en X cuando esté cerca.")]
    public bool chasePlayerHorizontally = true;
    public float chaseRange = 6f; // distancia para empezar a perseguir

    [Header("Colisiones")]
    public LayerMask wallsMask;   // capas de muros/pilares
    public float skin = 0.02f;    // margen para evitar penetración

    [Header("Ataque eléctrico")]
    public float shockRange = 2.5f;
    public int damage = 1;
    public float shockCooldown = 2f;
    public Color normalColor = Color.white;
    public Color chargeColor = Color.cyan;

    [Header("Sprite Sheet (desde Project)")]
    [Tooltip("Arrastra aquí TODOS los sub-sprites del sheet (en orden de slice).")]
    public Sprite[] allFrames;     // todos los cortes del sheet
    [Tooltip("Número de columnas (ej. 4 en 4x4).")]
    public int cols = 4;
    [Tooltip("Fila para cada animación (0 = arriba).")]
    public int rowIdle = 0, rowMove = 1, rowCharge = 2, rowShock = 3;

    // Animación
    SpriteAnimator anim;
    SpriteRenderer sr;
    Sprite[] idle, move, charge, shock;

    Transform player;
    Rigidbody2D rb;
    Collider2D col;
    bool movingRight = true;
    float cd;
    Vector3 startScale;
    float baseY; // para mantenerlo a nivel y no “bajar”

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        col = GetComponent<Collider2D>();
        col.isTrigger = false; // sólido

        anim = GetComponent<SpriteAnimator>();
        sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white; // evitar tinte accidental
        startScale = transform.localScale;

        baseY = transform.position.y; // bloquear movimiento vertical
    }

    void Start()
    {
        // Validar y construir filas del sheet
        if (!BuildRows())
        {
            Debug.LogError("[Enemy_Electric] Asigna 'allFrames' con todos los cortes y verifica 'cols'.");
            enabled = false;
            return;
        }

        // Animación inicial
        anim.Play(idle, 0.10f, true);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Validaciones
        if (!leftPoint || !rightPoint)
            Debug.LogWarning("Enemy_Electric: Asigna leftPoint y rightPoint.");
    }

    bool BuildRows()
    {
        if (allFrames == null || allFrames.Length == 0 || cols <= 0) return false;

        // Ordenar por nombre (SpriteName_0, _1, ...)
        var ordered = allFrames.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();

        idle   = Row(ordered, rowIdle, cols);
        move   = Row(ordered, rowMove, cols);
        charge = Row(ordered, rowCharge, cols);
        shock  = Row(ordered, rowShock, cols);

        return idle.Length > 0 && move.Length > 0 && charge.Length > 0 && shock.Length > 0;
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
        if (!player && chasePlayerHorizontally) return;

        // Mantener Y fija para que no “baje”
        var pos = transform.position;
        if (Mathf.Abs(pos.y - baseY) > 0.0001f)
            transform.position = new Vector3(pos.x, baseY, pos.z);

        // Flip visual según dirección (sin rotar transform)
        if (movingRight) transform.localScale = new Vector3(Mathf.Abs(startScale.x), startScale.y, startScale.z);
        else             transform.localScale = new Vector3(-Mathf.Abs(startScale.x), startScale.y, startScale.z);

        // Ataque por cercanía
        if (player)
        {
            float distToPlayer = Vector2.Distance(player.position, transform.position);
            cd -= Time.deltaTime;
            if (distToPlayer <= shockRange && cd <= 0f)
            {
                StartCoroutine(ShockAttack());
                cd = shockCooldown;
            }
        }
    }

    void FixedUpdate()
    {
        // Objetivo horizontal (patrulla por defecto)
        float targetX = transform.position.x;

        if (chasePlayerHorizontally && player && Vector2.Distance(player.position, transform.position) <= chaseRange)
        {
            // Perseguir SOLO en X
            targetX = player.position.x;
        }
        else if (leftPoint && rightPoint)
        {
            // Patrulla entre puntos (usa su X y mantiene Y)
            Vector2 target = movingRight ? (Vector2)rightPoint.position : (Vector2)leftPoint.position;
            targetX = target.x;

            // Cambia de sentido si llegó cerca del extremo
            if (Mathf.Abs(transform.position.x - targetX) <= 0.05f)
            {
                movingRight = !movingRight;
                anim.Play(idle, 0.12f, true); // breve idle
            }
        }

        // Dirección deseada en X únicamente
        float dirX = Mathf.Sign(targetX - transform.position.x);
        if (Mathf.Abs(targetX - transform.position.x) < 0.02f) dirX = 0f;

        Vector2 desiredDelta = new Vector2(dirX * moveSpeed * Time.fixedDeltaTime, 0f);

        // Reproduce anim de movimiento si se mueve, sino idle
        if (Mathf.Abs(dirX) > 0.001f)
        {
            if (anim.frames != move || !anim.IsPlaying)
                anim.Play(move, 0.10f, true);
        }
        else
        {
            if (anim.frames != idle || !anim.IsPlaying)
                anim.Play(idle, 0.12f, true);
        }

        // --- Movimiento con colisiones (raycast/cast) ---
        if (desiredDelta != Vector2.zero)
        {
            int hits = rb.Cast(desiredDelta.normalized, new ContactFilter2D { layerMask = wallsMask, useLayerMask = true, useTriggers = false }, _castHits, desiredDelta.magnitude + skin);
            if (hits > 0)
            {
                // bloqueado: no avanzar y cambiar de sentido si patrullando
                desiredDelta = Vector2.zero;
                if (!chasePlayerHorizontally) movingRight = !movingRight;
            }
        }

        rb.MovePosition(rb.position + desiredDelta);
    }

    // buffer de resultados para Cast (evita GC)
    static readonly RaycastHit2D[] _castHits = new RaycastHit2D[4];

    System.Collections.IEnumerator ShockAttack()
    {
        // Carga
        sr.color = chargeColor;
        anim.Play(charge, 0.08f, false);
        yield return new WaitForSeconds(charge.Length * 0.08f);

        // Descarga
        anim.Play(shock, 0.06f, false);

        // Daño por cercanía (1 frame después para sincronizar)
        yield return new WaitForSeconds(0.06f);
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, shockRange))
        {
            var pc = hit.GetComponent<PlayerHealth>();
            if (pc != null) pc.Damage(damage);
        }

        // Fin
        yield return new WaitForSeconds((Mathf.Max(0, shock.Length - 1)) * 0.06f);
        sr.color = normalColor;

        // volver a idle/move
        anim.Play(idle, 0.10f, true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockRange);

        if (leftPoint && rightPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(leftPoint.position.x, transform.position.y, 0f),
                            new Vector3(rightPoint.position.x, transform.position.y, 0f));
        }
    }
}
