using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
public class GoalTrigger : MonoBehaviour
{
    private LevelCompleteUI ui;
    private SpriteAnimator anim;

    [Header("Sprite Sheet (desde Project)")]
    [Tooltip("Arrastra aquí TODOS los sub-sprites del sheet de la puerta (en orden).")]
    public Sprite[] allFrames;    // arrastra los 8 cortes de SpriteDoor
    public int cols = 8;          // columnas del sheet (1x8)
    public int rowIdle = 0;       // fila base (si tuvieses más filas)
    public int rowOpen = 0;       // misma fila (usa un rango distinto si tu anim tiene más frames)

    Sprite[] idle, open;
    bool activated;

    void Awake()
    {
        anim = GetComponent<SpriteAnimator>();

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        GetComponent<SpriteRenderer>().color = Color.white;
    }

    void Start()
    {
        // Buscar el UI de nivel
        #if UNITY_2023_1_OR_NEWER
        ui = FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include);
        #else
        ui = FindObjectOfType<LevelCompleteUI>(true);
        #endif

        if (!ui)
            Debug.LogWarning("GoalTrigger: No encontré LevelCompleteUI en la escena.");

        // Construir frames si están asignados
        if (!BuildRows())
        {
            Debug.LogError("[GoalTrigger] Asigna 'allFrames' con los cortes del SpriteDoor.");
            enabled = false;
            return;
        }

        // Animación inicial (idle)
        anim.Play(idle, 0.10f, true);
    }

    bool BuildRows()
    {
        if (allFrames == null || allFrames.Length == 0 || cols <= 0) return false;

        var ordered = allFrames.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
        idle = Row(ordered, rowIdle, cols);
        open = Row(ordered, rowOpen, cols); // usa los mismos si solo hay una fila

        return idle.Length > 0 && open.Length > 0;
    }

    Sprite[] Row(Sprite[] src, int rowIndex, int c)
    {
        int start = rowIndex * c;
        int count = Mathf.Min(c, Mathf.Max(0, src.Length - start));
        Sprite[] r = new Sprite[count];
        for (int i = 0; i < count; i++) r[i] = src[start + i];
        return r;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;
        Debug.Log("¡Nivel completado!");
        StartCoroutine(OpenPortal());
    }

    System.Collections.IEnumerator OpenPortal()
    {
        // Reproduce animación de “apertura/brillo”
        anim.Play(open, 0.08f, false);
        yield return new WaitForSeconds(open.Length * 0.08f);

        // Mostrar UI y pausar
        ui?.Show();
    }
}
