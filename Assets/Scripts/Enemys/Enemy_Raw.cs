using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Raw : MonoBehaviour
{
    [Header("Detección")]
    public float visionRange = 9f;               // rango para detectar al player
    public LayerMask wallsMask;                  // capa de muros para evitar/linea de visión

    [Header("Distancias")]
    public float preferredDistance = 2.6f;       // distancia a la que intenta quedarse
    public float tooCloseDistance = 1.6f;        // si está más cerca que esto, retrocede

    [Header("Movimiento")]
    public float maxSpeed = 3.2f;                // velocidad máxima
    public float acceleration = 10f;             // acelera hacia el objetivo
    public float damping = 6f;                   // freno cuando no hay input
    public float avoidForce = 8f;                // fuerza lateral para evitar paredes
    public float avoidProbe = 0.8f;              // longitud del raycast de evitación

    [Header("Flotación/idle")]
    public float hoverAmplitude = 0.15f;         // oscilación vertical
    public float hoverFrequency = 2.0f;          // velocidad de la oscilación
    public float idleWanderSpeed = 1.2f;         // leve desplazamiento cuando no ve al player

    Transform player;
    Rigidbody2D rb;
    Vector2 idleDir;
    float idleTimer;
    float baseY;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Para usar linearVelocity cómodamente, deja el Body Type en Dynamic y congela la rotación Z en el inspector.
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        baseY = transform.position.y;
        PickIdleDir();
    }

    void Update()
    {
        // Oscilación “flotante”
        var pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = pos;

        // Rotar a mirar al player si existe
        if (player)
        {
            Vector2 toPlayer = (player.position - transform.position);
            if (toPlayer.sqrMagnitude > 0.0001f)
                transform.right = toPlayer.normalized;
        }
    }

    void FixedUpdate()
    {
        if (!player)
        {
            IdleMove();
            return;
        }

        Vector2 toPlayer = (Vector2)(player.position - transform.position);
        float dist = toPlayer.magnitude;

        // Si no está en rango, deambula
        if (dist > visionRange)
        {
            IdleMove();
            return;
        }

        Vector2 dir = (dist > 0.001f) ? toPlayer.normalized : Vector2.zero;

        // Mantener distancia óptima
        Vector2 desired = Vector2.zero;
        if (dist > preferredDistance + 0.1f)
        {
            // acercarse
            desired = dir * maxSpeed;
        }
        else if (dist < tooCloseDistance)
        {
            // alejarse (kite)
            desired = -dir * maxSpeed * 0.8f;
        }
        else
        {
            // orbitar un poco alrededor del player
            Vector2 tangent = new Vector2(-dir.y, dir.x); // giro 90°
            desired = tangent * (maxSpeed * 0.6f);
        }

        // Evitación simple de paredes (dos rayos a los lados)
        Vector2 right = transform.right;
        Vector2 leftProbe = Quaternion.Euler(0, 0, 90) * right;
        Vector2 rightProbe = Quaternion.Euler(0, 0, -90) * right;

        if (Physics2D.Raycast(transform.position, rightProbe, avoidProbe, wallsMask))
            desired += leftProbe * avoidForce;
        if (Physics2D.Raycast(transform.position, leftProbe, avoidProbe, wallsMask))
            desired += rightProbe * avoidForce;

        // Aceleración y frenado suaves
        Vector2 vel = rb.linearVelocity;
        Vector2 targetVel = Vector2.ClampMagnitude(desired, maxSpeed);

        Vector2 accel = (targetVel - vel) * acceleration;
        vel += accel * Time.fixedDeltaTime;

        // damping ligero si casi no hay input
        if (desired.sqrMagnitude < 0.01f)
            vel = Vector2.Lerp(vel, Vector2.zero, damping * Time.fixedDeltaTime);

        rb.linearVelocity = vel;
    }

    void IdleMove()
    {
        // Deambular suave en idle + cambiar dirección cada cierto tiempo
        idleTimer -= Time.fixedDeltaTime;
        if (idleTimer <= 0f) PickIdleDir();

        Vector2 vel = rb.linearVelocity;
        Vector2 target = idleDir * idleWanderSpeed;
        vel = Vector2.MoveTowards(vel, target, acceleration * 0.5f * Time.fixedDeltaTime);
        rb.linearVelocity = vel;
    }

    void PickIdleDir()
    {
        // dirección aleatoria en 360°
        float a = Random.Range(0f, Mathf.PI * 2f);
        idleDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        idleTimer = Random.Range(1.2f, 2.2f);
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
        if (Application.isPlaying)
        {
            Vector3 rp = Quaternion.Euler(0, 0, -90) * transform.right * avoidProbe;
            Vector3 lp = Quaternion.Euler(0, 0, 90) * transform.right * avoidProbe;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + rp);
            Gizmos.DrawLine(transform.position, transform.position + lp);
        }
    }
}
