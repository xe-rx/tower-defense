using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveState
{
  Idle,         // Before game or after end, no waves are queued, nothing to prepare.
  Spawning,     // Spawning.
  Paused,       // Paused.
  Intermission, // After one wave, but before the next wave.
  GameOver      // Game is over.
}

public class WaveController : MonoBehaviour
{
  [Header("Config")]
  [Tooltip("Waves JSON file")]
  public TextAsset waveJson;

  [Tooltip("Seconds between waves")]
  public float intermissionSeconds = 5f; // should probably be tied to builder returning to hub

  [Tooltip("Autostart")]
  public bool autoStart = true;

  public WaveState State { get; private set; } = WaveState.Idle;
  public int WaveIndex { get; private set; } = 0;

  float _timer;
  SpawnScheduler _scheduler;
  WaveSource _source;

  void Awake()
  {
    _scheduler = new SpawnScheduler();
    _source = new WaveSource(waveJson);
  }

  void Start()
  {
    if (autoStart) StartRun();

  }

  void Update()
  {
    if (State == WaveState.Paused || State == WaveState.GameOver) return;
    switch (State)
    {
      case WaveState.Spawning:
        _scheduler.Update(Time.deltaTime);

        if (_scheduler.Done && EnemyTracker.AliveCount == 0)
          EnterIntermission();
        break;

      case WaveState.Intermission:
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
          NextWave();
        break;

      case WaveState.Idle:
        // waiting for start
        break;
    }
  }

  public void StartRun()
  {
    if (State != WaveState.Idle) return;

    WaveIndex = 0;
    EnterIntermission();
  }

  public void SendNextWaveEarly()
  {
    if (State == WaveState.Intermission)
      NextWave();
  }

  public void Pause()
  {
    if (State == WaveState.Paused) return;

    _scheduler.Pause();
    State = WaveState.Paused;
  }

  public void Resume()
  {
    if (State != WaveState.Paused) return;

    _scheduler.Resume();
    State = _scheduler.HasActiveTimeLine ? WaveState.Spawning : WaveState.Intermission;
  }

  public void SetGameOver()
  {
    State = WaveState.GameOver;
  }

  void NextWave()
  {
    WaveIndex += 1;
    var wave = _source.GetWave(WaveIndex);

    // TEMPORARY WAY OF HANDLING
    if (wave == null)
    {
      Debug.LogWarning($"No wae data for index {WaveIndex}, ending");
      State = WaveState.GameOver;
      return;
    }

    // builds a timeline of spawn events from WaveData
    var eventsList = BuildEvents(wave);
    _scheduler.Start(eventsList);
    State = WaveState.Spawning;
  }

  void EnterIntermission()
  {
    State = WaveState.Intermission;
    _timer = intermissionSeconds;
  }

  List<SpawnEvent> BuildEvents(WaveDef wave)
  {
    var list = new List<SpawnEvent>(64);
    foreach (var e in wave.entries)
    {
      float t = Mathf.Max(0f, e.delay);
      for (int i = 0; i < e.count; i++)
      {
        var prefab = _source.LoadPrefabFor(e.enemy);
        if (prefab == null)
        {
          Debug.LogError($"Wave {wave.id}: enemy '{e.enemy}' has no loadable prefab.");
          continue;
        }
        list.Add(new SpawnEvent { time = t, prefab = prefab, lane = e.lane });
        t += Mathf.Max(0f, e.interval);
      }
    }
    list.Sort((a, b) => a.time.CompareTo(b.time));
    return list;
  }
}
