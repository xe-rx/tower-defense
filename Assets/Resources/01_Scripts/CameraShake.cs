using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;   // how long the shake lasts
    [SerializeField] private float magnitude = 0.1f;  // how far the camera moves

    private Vector3 originalPos;
    private Coroutine runningShake;

    public void Shake(float delay = 0f)
    {
        if (runningShake != null) StopCoroutine(runningShake);
        runningShake = StartCoroutine(ShakeRoutine(delay));
    }

    private IEnumerator ShakeRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        runningShake = null;
    }
}

