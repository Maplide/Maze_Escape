using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerHealth playerHealth;         // arrástralo (el del jugador)
    public GameObject panel;                  // el panel raíz de Game Over (Canvas hijo)
    public Button retryButton;                // botón Reintentar (para seleccionar por defecto)

    [Header("Escenas")]
    public string mainMenuSceneName = "MainMenu"; // cámbialo por el nombre real

    bool shown;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    void OnEnable()
    {
        if (!playerHealth) playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth) playerHealth.OnDead += HandleDead;
    }

    void OnDisable()
    {
        if (playerHealth) playerHealth.OnDead -= HandleDead;
    }

    void HandleDead()
    {
        if (shown) return;
        shown = true;

        // Mostrar UI
        if (panel) panel.SetActive(true);

        // Pausar juego
        Time.timeScale = 0f;

        // Habilitar cursor (opcional)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Seleccionar botón por defecto (pad/teclado)
        if (retryButton)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
        }
    }

    // --- Botones ---
    public void OnRetry()
    {
        Time.timeScale = 1f;
        var idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }

    public void OnBackToMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogWarning("Asigna el nombre de la escena de Menú en GameOverUI.");
    }
}
