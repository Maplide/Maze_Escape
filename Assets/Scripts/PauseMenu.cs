using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;      // Panel contenedor del menú de pausa
    public Button btnResume;
    public Button btnRetry;
    public Button btnMainMenu;

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";

    bool isPaused;

    void Awake()
    {
        // Asegura estado inicial
        SetPaused(false, applyNow: true);

        // Wire botones
        if (btnResume)   btnResume.onClick.AddListener(() => SetPaused(false));
        if (btnRetry)    btnRetry.onClick.AddListener(RestartLevel);
        if (btnMainMenu) btnMainMenu.onClick.AddListener(ReturnToMenu);
    }

    void Update()
    {
        // Toggle con ESC (ambos sistemas de input)
        bool escPressed = false;

        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        if (kb != null) escPressed = kb.escapeKey.wasPressedThisFrame;
        #else
        escPressed = Input.GetKeyDown(KeyCode.Escape);
        #endif

        if (escPressed)
        {
            SetPaused(!isPaused);
        }
    }

    public void SetPaused(bool pause, bool applyNow = true)
    {
        isPaused = pause;

        if (pausePanel) pausePanel.SetActive(isPaused);

        if (applyNow)
        {
            Time.timeScale = isPaused ? 0f : 1f;
            AudioListener.pause = isPaused;

            Cursor.visible = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    public void RestartLevel()
    {
        // Reanuda tiempo antes de recargar
        Time.timeScale = 1f;
        AudioListener.pause = false;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void ReturnToMenu()
    {
        // Reanuda tiempo antes de cambiar
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError($"PauseMenu: escena '{mainMenuScene}' no está en Build Settings.");
            return;
        }
        SceneManager.LoadScene(mainMenuScene);
    }
}
