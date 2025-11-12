using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspectViewport : MonoBehaviour
{
    public float targetAspect = 16f / 9f; // 1.7777

    void Start() { Apply(); }
    void OnPreCull() { Apply(); }  // por si cambia el tamaño en runtime

    void Apply()
    {
        var cam = GetComponent<Camera>();
        float windowAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)
        {
            // Pantalla más alta → barras arriba/abajo
            Rect rect = cam.rect;
            rect.width = 1f;
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) * 0.5f;
            cam.rect = rect;
        }
        else
        {
            // Pantalla más ancha → barras a los lados
            float scaleWidth = 1f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1f;
            rect.x = (1f - scaleWidth) * 0.5f;
            rect.y = 0f;
            cam.rect = rect;
        }
    }
}
