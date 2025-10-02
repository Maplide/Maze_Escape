using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private LevelCompleteUI ui;

    void Start()
    {
        #if UNITY_2023_1_OR_NEWER
        ui = FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include);
        #else
        // includeInactive: true para encontrar el panel aunque esté desactivado
        ui = FindObjectOfType<LevelCompleteUI>(true);
        #endif

        if (!ui)
            Debug.LogWarning("GoalTrigger: No encontré LevelCompleteUI en la escena.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("¡Nivel completado!");
        ui?.Show(); // abre panel y pausa (Time.timeScale = 0)
    }
}
