using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SegmentedHealthUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth target;          // arrastra el Player
    public Transform container;          // contenedor de los segmentos (un GameObject con Layout)
    public GameObject segmentPrefab;     // prefab (UI Image) de un segmento

    [Header("Apariencia")]
    public Sprite spriteOn;              // sprite lleno
    public Sprite spriteOff;             // sprite vacío
    public bool useColorInsteadOfSprites = false;
    public Color onColor = new Color(0.2f, 0.8f, 0.2f);   // verde
    public Color midColor = new Color(0.95f, 0.75f, 0.2f); // amarillo
    public Color offColor = new Color(0.85f, 0.2f, 0.2f);  // rojo
    [Range(0f,1f)] public float midThreshold = 0.5f;       // < 50% pasa a amarillo/rojo

    readonly List<Image> _segments = new();

    void Awake()
    {
        if (!target)
            target = FindFirstObjectByType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (target != null)
        {
            target.OnHealthChanged += HandleChanged;
            // construir al inicio
            RebuildSegments(target.maxHealth);
            HandleChanged(target.currentHealth, target.maxHealth);
        }
    }

    void OnDisable()
    {
        if (target != null)
            target.OnHealthChanged -= HandleChanged;
    }

    void HandleChanged(int hp, int max)
    {
        // reconstruye si cambió el máximo (por power-ups)
        if (_segments.Count != max)
            RebuildSegments(max);

        float t = (max > 0) ? (hp / (float)max) : 0f;

        for (int i = 0; i < _segments.Count; i++)
        {
            bool on = i < hp;
            var img = _segments[i];

            if (useColorInsteadOfSprites)
            {
                // Color dinámico según porcentaje total
                if (t >= 0.999f)          img.color = onColor;          // lleno
                else if (t >= midThreshold) img.color = midColor;       // medio
                else                      img.color = offColor;         // bajo
                img.enabled = on; // encendido sólo si ese segmento está activo
            }
            else
            {
                img.sprite = on ? spriteOn : spriteOff;
                img.enabled = true;
            }
        }
    }

    void RebuildSegments(int count)
    {
        // limpia
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
        _segments.Clear();

        // crea
        for (int i = 0; i < count; i++)
        {
            var seg = Instantiate(segmentPrefab, container);
            var img = seg.GetComponent<Image>();
            if (!img) img = seg.AddComponent<Image>();
            _segments.Add(img);
        }
    }
}
