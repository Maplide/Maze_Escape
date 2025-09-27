using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class PlayerSpriteAnim : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite idleFrame;          // arrastra maze_0 (o el que quieras idle)
    public Sprite[] walkFrames;       // arrastra maze_0..maze_5 en orden

    [Header("Ajustes")]
    public float walkFps = 10f;        // velocidad de la animación
    public float moveThreshold = 0.1f; // cuándo consideramos que “se mueve”

    SpriteRenderer sr;
    Rigidbody2D rb;
    float t;
    int frame;

    void Awake(){
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (idleFrame) sr.sprite = idleFrame;
    }

    void Update(){
        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;

        // Volteo horizontal según dirección X
        if (Mathf.Abs(v.x) > 0.01f)
            sr.flipX = v.x < 0f;

        if (speed > moveThreshold && walkFrames != null && walkFrames.Length > 0)
        {
            // Animación de caminar en bucle
            t += Time.deltaTime * walkFps;
            frame = (int)t % walkFrames.Length;
            sr.sprite = walkFrames[frame];
        }
        else
        {
            // Quieto (idle)
            if (idleFrame) sr.sprite = idleFrame;
            t = 0f; frame = 0;
        }
    }
}
