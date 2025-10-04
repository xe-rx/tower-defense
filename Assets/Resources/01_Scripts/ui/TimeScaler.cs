using UnityEngine;
using UnityEngine.UI;

public class TimeScaler : MonoBehaviour
{
    [Header("Speed Steps")]
    [SerializeField] private float[] speedSteps = { 1f, 2f, 4f };
    private int currentStep = 0;
    private bool isPaused = false;
    private float baseFixedDelta;

    [Header("UI Icons")]
    [Tooltip("The Image component used for the speed button icon.")]
    [SerializeField] private Image speedIcon;
    [Tooltip("Sprites that represent 1x, 2x, 4x in this order.")]
    [SerializeField] private Sprite[] speedSprites; // [0]=1x, [1]=2x, [2]=4x

    [Tooltip("The Image component used for the pause/resume button icon.")]
    [SerializeField] private Image pauseIcon;
    [Tooltip("Sprite to show when the game is running (press to pause).")]
    [SerializeField] private Sprite pauseSprite;   // e.g., “pause” icon
    [Tooltip("Sprite to show when the game is paused (press to resume).")]
    [SerializeField] private Sprite resumeSprite;  // e.g., “play” icon

    void Awake()
    {
        baseFixedDelta = Time.fixedDeltaTime;
        ApplySpeed(speedSteps[currentStep]);
        UpdateIcons();
    }

    /// <summary>Cycles speed between 1x → 2x → 4x → 1x.</summary>
    public void CycleSpeed()
    {
        if (isPaused) return; // ignore changes while paused
        currentStep = (currentStep + 1) % speedSteps.Length;
        ApplySpeed(speedSteps[currentStep]);
        UpdateIcons();
    }

    /// <summary>Toggles pause on/off (timeScale 0 or current speed).</summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = baseFixedDelta; // don't scale physics while paused
            AudioListener.pause = true;
        }
        else
        {
            AudioListener.pause = false;
            ApplySpeed(speedSteps[currentStep]);
        }
        UpdateIcons();
    }

    private void ApplySpeed(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDelta * scale;
        // Debug.Log($"Game speed set to {scale}x");
    }

    private void UpdateIcons()
    {
        // Speed button icon
        if (speedIcon != null && speedSprites != null && speedSprites.Length >= speedSteps.Length)
        {
            int spriteIndex = Mathf.Clamp(currentStep, 0, speedSprites.Length - 1);
            speedIcon.sprite = speedSprites[spriteIndex];
            // Uncomment if you want the icon to use the sprite's native size:
            // speedIcon.SetNativeSize();
        }

        // Pause/Resume button icon
        if (pauseIcon != null)
        {
            pauseIcon.sprite = isPaused ? resumeSprite : pauseSprite;
            // pauseIcon.SetNativeSize();
        }
    }
}

