using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System
#endif

public class GameScreens : MonoBehaviour
{
    public static GameScreens main;

    [Header("Wire in Inspector")]
    [SerializeField] private CanvasGroup gameOverScreen;   // full-screen panel
    [SerializeField] private TextMeshProUGUI gameOverText; // optional "Press SPACE…" text
    [SerializeField] private CanvasGroup winScreen;        // full-screen panel
    [SerializeField] private TextMeshProUGUI winText;      // optional "You won!" text

    [Header("Gameplay UI")]
    [Tooltip("Root GameObject of your in-game UI (e.g., UICanvas). Will be hidden on Game Over / Win.")]
    [SerializeField] private GameObject gameplayUIRoot;

    [Header("Config")]
    [SerializeField] private string gameOverPrompt = "Press SPACE to restart";
    [SerializeField] private KeyCode restartKey = KeyCode.Space;
    [SerializeField] private bool pauseOnShow = true;

    bool isGameOver;

    void Awake()
    {
        main = this;
        HideAll();
        ShowGameplayUI(true); // ensure HUD is visible at start
    }

    void Update()
    {
        if (isGameOver && RestartPressed())
        {
            Restart();
        }
    }

    public void ShowGameOver()
    {
        isGameOver = true;

        if (pauseOnShow) Time.timeScale = 0f;

        SetGroup(gameOverScreen, true);
        if (gameOverText) gameOverText.text = gameOverPrompt;

        SetGroup(winScreen, false);

        // hide gameplay HUD
        ShowGameplayUI(false);
    }

    public void ShowWin()
    {
        isGameOver = false;

        if (pauseOnShow) Time.timeScale = 0f;

        SetGroup(winScreen, true);
        if (winText && string.IsNullOrEmpty(winText.text))
            winText.text = "You Won!";

        SetGroup(gameOverScreen, false);

        // hide gameplay HUD
        ShowGameplayUI(false);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    void HideAll()
    {
        SetGroup(gameOverScreen, false);
        SetGroup(winScreen, false);
    }

    static void SetGroup(CanvasGroup g, bool on)
    {
        if (!g) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
        g.gameObject.SetActive(on);
    }

    void ShowGameplayUI(bool on)
    {
        if (gameplayUIRoot && gameplayUIRoot.activeSelf != on)
            gameplayUIRoot.SetActive(on);
    }

    // ===== Input handling (New Input System first, fallback to Legacy if present) =====
    bool RestartPressed()
    {
        // Prefer the new Input System if it's enabled
        #if ENABLE_INPUT_SYSTEM
        // Keyboard
        if (Keyboard.current != null)
        {
            // If you change restartKey in Inspector, you can branch on it here.
            // For now, Space is the default.
            if (restartKey == KeyCode.Space && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;

            // Optional: also allow Enter/Return or 'R' to restart
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                return true;
            if (Keyboard.current.rKey.wasPressedThisFrame)
                return true;
        }
        // Gamepad (Start or South/A)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame ||
                Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
        }
        #endif

        // Fallback to legacy Input Manager if available (requires Active Input Handling = Both)
        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(restartKey)) return true;
        #endif

        return false;
    }
}

