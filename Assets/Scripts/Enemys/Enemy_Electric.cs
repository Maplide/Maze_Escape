using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy_Electric : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public Transform leftPoint;
    public Transform rightPoint;

    [Header("Ataque eléctrico")]
    public float shockRange = 2.5f;
    public int damage = 1;
    public float shockCooldown = 2f;
    public SpriteRenderer sr;
    public Color normalColor = Color.white;
    public Color chargeColor = Color.cyan;

    Transform player;
    Rigidbody2D rb;
    bool movingRight = true;
    float cd;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        if (!sr) sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!player) return;

        Vector2 pos = transform.position;
        Vector2 target = movingRight ? rightPoint.position : leftPoint.position;

        // Mover entre los puntos
        if (Vector2.Distance(pos, target) > 0.05f)
        {
            Vector2 dir = (target - pos).normalized;
            rb.MovePosition(pos + dir * moveSpeed * Time.deltaTime);
        }
        else
        {
            movingRight = !movingRight; // cambiar dirección al llegar al extremo
        }

        // Detectar al jugador
        float distToPlayer = Vector2.Distance(player.position, transform.position);

        cd -= Time.deltaTime;
        if (distToPlayer <= shockRange && cd <= 0f)
        {
            StartCoroutine(ShockAttack());
            cd = shockCooldown;
        }
    }

    System.Collections.IEnumerator ShockAttack()
    {
        if (sr) sr.color = chargeColor;
        yield return new WaitForSeconds(0.3f);

        // Daño por cercanía
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockRange);
        foreach (var hit in hits)
        {
            var pc = hit.GetComponent<PlayerHealth>();
            if (pc != null)
                pc.Damage(damage);
        }

        yield return new WaitForSeconds(0.2f);
        if (sr) sr.color = normalColor;
    }

    void OnDrawGizmosSelected()
    {
        // Rango del ataque eléctrico
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockRange);
    }
}
