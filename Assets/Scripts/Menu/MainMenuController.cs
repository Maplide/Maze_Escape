using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMain;       // Panel con botones Jugar, Niveles, Controles, Salir
    public GameObject panelNiveles;    // Panel de selección de niveles
    public GameObject panelControles;  // Panel de controles (nuevo)

    private void Start()
    {
        // Estado inicial de los paneles
        if (panelMain) panelMain.SetActive(true);
        if (panelNiveles) panelNiveles.SetActive(false);
        if (panelControles) panelControles.SetActive(false);

        // Mostrar cursor en el menú
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // === Botones principales ===
    public void OnClickJugar()
    {
        // Empieza en Nivel 1 (en Build Settings debe estar en índice 1)
        SceneManager.LoadScene(1);
    }

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

    public void OnClickSalir()
    {
        Application.Quit();
        Debug.Log("Salir del juego");
    }

    // === Botones del Panel Niveles ===
    public void OnClickNivel1()
    {
        SceneManager.LoadScene(1);
    }

    public void OnClickNivel2()
    {
        SceneManager.LoadScene(2);
    }

    public void OnClickNivel3()
    {
        SceneManager.LoadScene(3);
    }

    public void OnClickVolverDeNiveles()
    {
        if (panelNiveles) panelNiveles.SetActive(false);
        if (panelMain) panelMain.SetActive(true);
    }

    // === Botones del Panel Controles ===
    public void OnClickVolverDeControles()
    {
        if (panelControles) panelControles.SetActive(false);
        if (panelMain) panelMain.SetActive(true);
    }
}
