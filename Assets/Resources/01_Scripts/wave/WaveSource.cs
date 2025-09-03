using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // each object in enemyCatalog array
public class EnemyCatalogEntry
{
  public string id;
  public string prefab;
  public int threatCost;
}

[Serializable] // spawn groups inside waves entry arrays
public class WaveEntryDef
{
  public string enemy;
  public int count;
  public float interval;
  public float delay;
  public int lane;
}

[Serializable] // individual wave
public class WaveDef
{
  public int id;
  public List<WaveEntryDef> entries;
  public int rewardgold;
}

[Serializable] // root object of wavedata json
public class WavesRoot
{
  public int version;
  public List<EnemyCatalogEntry> enemyCatalog;
  public int preDeterminedWaves;
  public List<WaveDef> waves;
}

public class WaveSource
{
  readonly WavesRoot _root;
  readonly Dictionary<string, EnemyCatalogEntry> _catalogById = new();
  readonly Dictionary<string, GameObject> _prefabCache = new();

  public WaveSource(TextAsset jsonAsset)
  {
    /* Takes JSON WaveData, deserializes it using JsonUtility. Falls back to
     * empty root so game doesnt null ref.
     * Builds the id -> catalog entry dict.
     * */

    if (jsonAsset == null)
    {
      Debug.LogError("WaveSource: missing waveJson asset");
      _root = new WavesRoot { waves = new List<WaveDef>() };
      return;
    }

    _root = JsonUtility.FromJson<WavesRoot>(jsonAsset.text);
    if (_root == null)
    {
      _root = new WavesRoot { waves = new List<WaveDef>() };
    }

    if (_root.enemyCatalog != null)
      foreach (var e in _root.enemyCatalog)
        if (!string.IsNullOrEmpty(e.id))
          _catalogById[e.id] = e;

  }

  public WaveDef GetWave(int waveIndex)
  {
    /* NOTE: implement procedural generation using budget for later waves.
     *
     * Returns wave by given index.
     * */

    if (_root.waves == null || _root.waves.Count == 0) return null;

    // make sure wave id is 0-index !!
    var w = _root.waves.Find(x => x.id == waveIndex);
    if (w != null) return w;

    return null;
  }

  public GameObject LoadPrefabFor(string enemyId)
  {
    /* Returns the enemy prefab corresponding to the given id.
     * */

    if (string.IsNullOrEmpty(enemyId))
      return null;

    if (_prefabCache.TryGetValue(enemyId, out var cached))
      return cached;

    if (!_catalogById.TryGetValue(enemyId, out var entry))
    {
        Debug.LogError($"WaveSource: enemy id '{enemyId}' not in catalog");
        return null;
    }

    // expects prefabs inside of 05_Prefabs
    var go = Resources.Load<GameObject>(entry.prefab);
    if (go == null)
    {
        Debug.LogError($"WaveSource: Resources.Load failed for path '{entry.prefab}' (enemy '{enemyId}').");
        return null;
    }

    _prefabCache[enemyId] = go;
    return go;
  }
}
