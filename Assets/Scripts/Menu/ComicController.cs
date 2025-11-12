using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class ComicController : MonoBehaviour
{
    [Header("UI")]
    public Image displayImage;
    public Text subtitleText;
    public Button btnNext;
    public Button btnPrev;
    public Button btnSkip;

    [Header("Contenido")]
    public Sprite[] panels;
    [TextArea] public string[] subtitles;

    [Header("Flujo")]
    public string nextScene;

    [Header("Input")]
    public bool inputAdvanceEnabled = true; // avanzar con click/teclas
    public bool allowBackgroundClick = true; // click en fondo avanza
    public float inputGraceSeconds = 0.20f;  // ignora input inicial

    int idx = 0;
    float sinceStart = 0f;
    bool transitioning = false;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (btnNext) btnNext.onClick.AddListener(Next);
        if (btnPrev) btnPrev.onClick.AddListener(Prev);
        if (btnSkip) btnSkip.onClick.AddListener(Skip);

        Apply();
    }

    void Update()
    {
        sinceStart += Time.unscaledDeltaTime;
        if (transitioning || !inputAdvanceEnabled || sinceStart < inputGraceSeconds) return;

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        bool nextPressed = false;
        bool prevPressed = false;
        bool skipPressed = false;

        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current; var mouse = Mouse.current;
        if (kb != null)
        {
            nextPressed |= (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
            prevPressed |= (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
            skipPressed |= kb.escapeKey.wasPressedThisFrame;
        }
        if (mouse != null && allowBackgroundClick && !pointerOverUI && mouse.leftButton.wasPressedThisFrame)
            nextPressed = true;
        #else
        if (allowBackgroundClick && !pointerOverUI && Input.GetMouseButtonDown(0)) nextPressed = true;
        nextPressed |= (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D));
        prevPressed |= (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A));
        skipPressed |= Input.GetKeyDown(KeyCode.Escape);
        #endif

        if (nextPressed) Next();
        if (prevPressed) Prev();
        if (skipPressed) Skip();
    }

    void Apply()
    {
        if (displayImage == null || panels == null || panels.Length == 0)
        {
            Debug.LogError("ComicController: Asigna displayImage y al menos un Sprite en 'panels'.");
            return;
        }

        idx = Mathf.Clamp(idx, 0, panels.Length - 1);
        displayImage.sprite = panels[idx];

        if (subtitleText)
            subtitleText.text = (subtitles != null && idx < subtitles.Length) ? (subtitles[idx] ?? "") : "";

        if (btnPrev) btnPrev.interactable = (idx > 0);
        if (btnNext)
        {
            var txt = btnNext.GetComponentInChildren<Text>();
            if (txt) txt.text = (idx >= panels.Length - 1) ? "Continuar" : "Siguiente";
        }
    }

    public void Next()
    {
        if (transitioning || panels == null || panels.Length == 0) return;

        if (idx < panels.Length - 1)
        {
            idx++;
            Apply();
        }
        else
        {
            LoadNextScene();
        }
    }

    public void Prev()
    {
        if (transitioning) return;
        if (idx > 0) { idx--; Apply(); }
    }

    public void Skip() => LoadNextScene();

    void LoadNextScene()
    {
        if (transitioning) return;
        transitioning = true; // 🔒 bloquea input y doble click

        if (btnNext) btnNext.interactable = false;
        if (btnPrev) btnPrev.interactable = false;
        if (btnSkip) btnSkip.interactable = false;

        if (string.IsNullOrWhiteSpace(nextScene))
        {
            Debug.LogError("ComicController: 'nextScene' no asignada.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"ComicController: La escena '{nextScene}' no está en Build Settings o el nombre está mal escrito.");
            return;
        }

        SceneManager.LoadScene(nextScene);
    }
}
