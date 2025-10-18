using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBullet2D : MonoBehaviour
{
    public float life = 3f;
    public int damage = 1;
    Rigidbody2D rb;

    public void Init(Vector2 velocity)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearVelocity = velocity;                  // <-- aquí el fix
        Destroy(gameObject, life);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // ¿tocó al Player?
        var pc = col.GetComponent<PlayerHealth>();
        if (pc != null)
        {
            pc.Damage(damage);
            Destroy(gameObject);
            return;
        }
        // ¿tocó pared?
        if (col.gameObject.layer == LayerMask.NameToLayer("Walls"))
            Destroy(gameObject);
    }
}
