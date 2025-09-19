using TMPro;
using UnityEngine;

public class WaveHUD : MonoBehaviour
{
    [SerializeField] private WaveController waveController;
    [SerializeField] private TMP_Text waveText;

    void Reset()
    {
        waveController = FindObjectOfType<WaveController>();
        waveText = GetComponentInChildren<TMP_Text>();
    }

    void Update()
    {
        if (waveController == null || waveText == null) return;
        int displayWave = Mathf.Max(1, waveController.WaveIndex);
        waveText.text = $"WAVE {displayWave}";
    }
}

