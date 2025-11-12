using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMain;
    public GameObject panelNiveles;
    public GameObject panelControles;

    [Header("Scene Names")]
    [Tooltip("Escena a cargar al presionar 'Jugar' (p.ej. IntroComic)")]
    public string firstScene = "IntroComic";  // ✅ Solo una definición
    [Tooltip("Acceso directo a Nivel 1")]
    public string level1Scene = "MazeEscapev01";
    [Tooltip("Acceso directo a Nivel 2")]
    public string level2Scene = "MazeEscapev02";
    [Tooltip("Acceso directo a Nivel 3")]
    public string level3Scene = "MazeEscapev03";
    [Tooltip("Escena de créditos (p.ej. 'Credits')")]
    public string creditsScene = "Credits";

    void Start()
    {
        if (panelMain) panelMain.SetActive(true);
        if (panelNiveles) panelNiveles.SetActive(false);
        if (panelControles) panelControles.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ==== Botones principales ====
    public void OnClickJugar() => LoadByNameSafe(firstScene);

    public void OnClickNiveles()
    {
        if (panelMain) panelMain.SetActive(false);
        if (panelNiveles) panelNiveles.SetActive(true);
    }

    public void OnClickControles()
    {
        if (panelMain) panelMain.SetActive(false);
        if (panelControles) panelControles.SetActive(true);
    }

    public void OnClickCreditos() => LoadByNameSafe(creditsScene);

    public void OnClickSalir()
    {
        Application.Quit();
        Debug.Log("Salir del juego");
    }

    // ==== Botones del Panel Niveles ====
    public void OnClickNivel1() => LoadByNameSafe(level1Scene);
    public void OnClickNivel2() => LoadByNameSafe(level2Scene);
    public void OnClickNivel3() => LoadByNameSafe(level3Scene);

    public void OnClickVolverDeNiveles()
    {
        if (panelNiveles) panelNiveles.SetActive(false);
        if (panelMain) panelMain.SetActive(true);
    }

    // ==== Botones del Panel Controles ====
    public void OnClickVolverDeControles()
    {
        if (panelControles) panelControles.SetActive(false);
        if (panelMain) panelMain.SetActive(true);
    }

    // ==== Utilidad segura ====
    void LoadByNameSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MainMenuController: nombre de escena vacío.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"MainMenuController: la escena '{sceneName}' no está en Build Settings o está mal escrita.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
