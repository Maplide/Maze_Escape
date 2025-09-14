using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private LevelCompleteUI ui;

    void Start()
    {
        // Busca el controlador de la UI (aunque el panel esté desactivado)
        ui = FindObjectOfType<LevelCompleteUI>(includeInactive: true);
        if (!ui) Debug.LogWarning("GoalTrigger: No encontré LevelCompleteUI en la escena.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("¡Nivel completado!");
        ui?.Show();              // abre el panel y hace Time.timeScale = 0
        // Ya NO cambiamos de escena aquí. Los botones de la UI manejan lo que sigue.
    }
}