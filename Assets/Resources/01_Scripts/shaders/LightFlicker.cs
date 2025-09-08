using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
  private Light2D _light;

  [Header("Flicker Settings")]
  public bool flickIntensity = true;
  public float _baseIntensity = 1f;
  public float intensityRange = 0.3f;
  public float intensityTimeMin = 0.05f;
  public float intensityTimeMax = 0.2f;

  void Awake()
  {
    _light = GetComponent<Light2D>();
  }

  void OnEnable()
  {
    StartCoroutine(FlickIntensity());
  }

  private IEnumerator FlickIntensity()
  {
    // Small random startup delay so multiple lights aren’t in sync
    yield return new WaitForSeconds(Random.Range(0.01f, 0.5f));

    while (true)
    {
      if (flickIntensity)
      {
        // Pick a random target intensity
        float r = Random.Range(_baseIntensity - intensityRange, _baseIntensity + intensityRange);
        _light.intensity = r;

        // Wait a random duration before the next flicker
        float t = Random.Range(intensityTimeMin, intensityTimeMax);
        yield return new WaitForSeconds(t);
      }
      else
      {
        yield return null; // just wait a frame if flickering is off
      }
    }
  }
}
