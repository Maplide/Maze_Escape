using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class IntroInstructions : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject panelInstrucciones; // Panel_Instrucciones
    public CanvasGroup cg;                // CanvasGroup del panel (para fade opcional)
    public TMP_Text textControles;        // (opcional)

    [Header("Opciones")]
    public bool soloPrimeraVez = true;    // ¿Mostrar solo la primera vez?
    public float fadeDuration = 0.25f;    // 0 = sin fade

    private bool mostrando;
    private bool cerrando;
    private float t;
    private const string PREF_KEY = "nivel1_instrucciones_visto";

    void Start()
    {
        bool yaVisto = PlayerPrefs.GetInt(PREF_KEY, 0) == 1;
        if (soloPrimeraVez && yaVisto)
        {
            if (panelInstrucciones) panelInstrucciones.SetActive(false);
            return;
        }
        Mostrar();
    }

    void Mostrar()
    {
        if (!panelInstrucciones) return;

        panelInstrucciones.SetActive(true);
        mostrando = true;
        cerrando = false;
        t = 0f;

        // Pausa y cursor visible
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (cg) cg.alpha = 0f; // para fade-in
    }

    void Update()
    {
        if (!mostrando) return;

        // Fade-in
        if (cg && !cerrando && fadeDuration > 0f && cg.alpha < 1f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
        }

        // Cerrar por input
        if (PressedAnyConfirm())
            Cerrar();
    }

    bool PressedAnyConfirm()
    {
        bool pressed = false;

        #if ENABLE_INPUT_SYSTEM
        // Teclado
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            pressed = true;

        // Mouse
        if (!pressed && Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame)
                pressed = true;
        }

        // Gamepad (botones comunes)
        if (!pressed && Gamepad.current != null)
        {
            var g = Gamepad.current;
            if (g.buttonSouth.wasPressedThisFrame ||   // A / Cross
                g.buttonNorth.wasPressedThisFrame ||   // Y / Triangle
                g.buttonEast.wasPressedThisFrame ||    // B / Circle
                g.buttonWest.wasPressedThisFrame ||    // X / Square
                g.startButton.wasPressedThisFrame ||
                g.selectButton.wasPressedThisFrame ||
                g.leftShoulder.wasPressedThisFrame ||
                g.rightShoulder.wasPressedThisFrame ||
                g.leftStickButton.wasPressedThisFrame ||
                g.rightStickButton.wasPressedThisFrame ||
                g.dpad.up.wasPressedThisFrame ||
                g.dpad.down.wasPressedThisFrame ||
                g.dpad.left.wasPressedThisFrame ||
                g.dpad.right.wasPressedThisFrame)
                pressed = true;
        }

        // Pantalla táctil
        if (!pressed && Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pressed = true;

        #else
        // Legacy Input Manager
        pressed = Input.anyKeyDown || Input.GetMouseButtonDown(0);
        #endif

        return pressed;
    }

    public void Cerrar() // puedes asignarlo también a un botón "Entendido"
    {
        if (cerrando) return;
        cerrando = true;

        if (soloPrimeraVez)
        {
            PlayerPrefs.SetInt(PREF_KEY, 1);
            PlayerPrefs.Save();
        }

        Time.timeScale = 1f;

        if (cg && fadeDuration > 0f)
        {
            StartCoroutine(FadeOutThenDisable());
        }
        else
        {
            if (panelInstrucciones) panelInstrucciones.SetActive(false);
            mostrando = false;
        }
    }

    System.Collections.IEnumerator FadeOutThenDisable()
    {
        float tiempo = 0f;
        float alphaInicial = cg.alpha;

        while (tiempo < fadeDuration)
        {
            tiempo += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(tiempo / fadeDuration);
            cg.alpha = alphaInicial * k;
            yield return null;
        }

        cg.alpha = 0f;
        if (panelInstrucciones) panelInstrucciones.SetActive(false);
        mostrando = false;
    }
}
