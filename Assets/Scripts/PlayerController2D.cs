using UnityEngine;

// Soporta ambos sistemas (elige según Player Settings)
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] public float speed = 5f;
    Rigidbody2D rb;

    void Awake(){
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate(){
        Vector2 input = ReadMove();
        // usa velocity en 2D
        rb.linearVelocity = input.normalized * speed;
    }

    Vector2 ReadMove(){
#if ENABLE_INPUT_SYSTEM // New Input System
        var k = UnityEngine.InputSystem.Keyboard.current;
        var g = UnityEngine.InputSystem.Gamepad.current;

        float x = 0f, y = 0f;
        if (k != null){
            x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
              - (k.aKey.isPressed || k.leftArrowKey.isPressed  ? 1f : 0f);
            // ARRIBA POSITIVO
            y = (k.wKey.isPressed || k.upArrowKey.isPressed    ? 1f : 0f)
              - (k.sKey.isPressed || k.downArrowKey.isPressed  ? 1f : 0f);
        }
        if (g != null){
            var stick = g.leftStick.ReadValue();
            // si hay mando, mezcla con teclado
            if (Mathf.Abs(stick.x) > Mathf.Abs(x)) x = stick.x;
            if (Mathf.Abs(stick.y) > Mathf.Abs(y)) y = stick.y;
        }
        return new Vector2(x, y);
#else
        // Input clásico
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)  ? 1 : 0);
        // ARRIBA POSITIVO
        float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)    ? 1 : 0)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)  ? 1 : 0);
        return new Vector2(x, y);
#endif
    }
}
