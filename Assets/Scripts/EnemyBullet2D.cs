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
        rb.linearVelocity = velocity;
        Destroy(gameObject, life);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        // daño simple al Player si tiene un método Damage(int)
        var pc = col.GetComponent<PlayerHealth>();
        if (pc != null)
        {
            pc.Damage(damage);
            Destroy(gameObject);
            return;
        }
        // pared → destruir
        if (col.gameObject.layer == LayerMask.NameToLayer("Walls"))
            Destroy(gameObject);
    }
}
