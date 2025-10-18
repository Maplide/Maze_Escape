using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;   // LevelCompletePanel
    public CanvasGroup cg;         // CanvasGroup del panel
    public TMP_Text timeText;
    public TMP_Text hitsText;
    public TMP_Text rankText;

    [Header("UI Flow")]
    [Tooltip("Nombre de la escena de Menú en Build Settings (ej: MainMenu)")]
    public string menuSceneName = "MainMenu";

    [Tooltip("Si está activado, el botón Siguiente cargará el siguiente índice en Build Settings.")]
    public bool autoNextByBuildIndex = true;

    [Tooltip("Si no usas autoNext, especifica el nombre exacto de la próxima escena aquí.")]
    public string explicitNextSceneName = "";

    [Header("Opcional")]
    public float fadeDuration = 0.25f;

    bool shown;

    void Awake() {
        if (panelRoot) panelRoot.SetActive(false);
        if (cg) cg.alpha = 0f;
    }

    public void Show()
    {
        if (shown) return;
        shown = true;

        float t = LevelStatsTracker.Instance ? LevelStatsTracker.Instance.ElapsedTime() : Time.timeSinceLevelLoad;
        int h = LevelStatsTracker.Instance ? LevelStatsTracker.Instance.Hits() : 0;
        string r = LevelStatsTracker.Instance ? LevelStatsTracker.Instance.Rank() : "-";

        if (timeText) timeText.text = $"Tiempo: {FormatTime(t)}";
        if (hitsText) hitsText.text = $"Golpes: {h}";
        if (rankText) rankText.text = $"Rango: {r}";

        if (panelRoot) panelRoot.SetActive(true);
        StartCoroutine(FadeIn());

        Time.timeScale = 0f; // pausa el juego
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    IEnumerator FadeIn()
    {
        if (!cg){ yield break; }
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    string FormatTime(float s)
    {
        int m = Mathf.FloorToInt(s / 60f);
        float r = s - m * 60f;
        return $"{m:00}:{r:00.00}";
    }

    // === Botones ===
    public void OnRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Versión sin parámetros para el botón
    public void OnNextLevel()
    {
        Time.timeScale = 1f;

        if (autoNextByBuildIndex)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int last = SceneManager.sceneCountInBuildSettings - 1;
            if (current < last)
            {
                SceneManager.LoadScene(current + 1);
            }
            else
            {
                // si ya no hay siguiente, vete al menú
                LoadMenuInternal();
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(explicitNextSceneName))
                SceneManager.LoadScene(explicitNextSceneName);
            else
                Debug.LogWarning("LevelCompleteUI: Define explicitNextSceneName o activa autoNextByBuildIndex.");
        }
    }

    // Versión sin parámetros para el botón
    public void OnMenu()
    {
        Time.timeScale = 1f;
        LoadMenuInternal();
    }

    void LoadMenuInternal()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
        else
            Debug.LogWarning("LevelCompleteUI: menuSceneName vacío. Asigna el nombre de tu escena de menú.");
    }
}
