using UnityEngine;

public class EnemyManager : MonoBehaviour
{
  public static EnemyManager main;

  public Transform spawnpoint;
  public Transform[] checkpoints;

  void Awake()
  {
    main = this;
  }

  // NOTE: Lane is ignored for now because you have a single checkpoints array.
  public GameObject Spawn(GameObject prefab, int lane = 0)
  {
    if (prefab == null)
    {
      Debug.LogError("EnemyManager.Spawn called with null prefab.");
      return null;
    }

    var go = Instantiate(prefab, spawnpoint.position, Quaternion.identity);
    EnemyTracker.AliveCount++;

    return go;
  }
}

