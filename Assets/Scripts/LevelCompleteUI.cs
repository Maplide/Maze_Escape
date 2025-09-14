using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panelRoot;  // LevelCompletePanel (GameObject)
    public CanvasGroup cg;        // CanvasGroup del panel
    public TMP_Text timeText;
    public TMP_Text hitsText;
    public TMP_Text rankText;

    [Header("Opcional")]
    public float fadeDuration = 0.25f;

    bool shown;

    void Awake(){
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
    }

    IEnumerator FadeIn()
    {
        if (!cg){ yield break; }
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;         // importante: unscaled
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
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void OnNextLevel(string nextSceneName)
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("LevelCompleteUI: Asigna el nombre de la siguiente escena.");
    }

    public void OnMenu(string menuScene)
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(menuScene))
            SceneManager.LoadScene(menuScene);
    }
}
