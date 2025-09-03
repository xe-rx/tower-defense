using System.Collections.Generic;
using UnityEngine;

public struct SpawnEvent
{
  public float time;
  public GameObject prefab;
  public int lane;
}

public class SpawnScheduler
{
  List<SpawnEvent> _events;
  int _cursor; // curr event
  float _t; // time passed since wave

  // NOTE: consider killing running bool if no pause mechanic
  bool _running; // false if paused, else true (scheduler active)

  public bool Done => _running && _events != null && _cursor >= _events.Count;
  public bool HasActiveTimeline => _events != null && _cursor < _events.Count;


  public void Start(List<SpawnEvent> eventsList)
  {
    _events = eventsList;
    _cursor = 0;
    _t = 0f;
    _running = true;

  }

  public void Update(float dt)
  {
    if (!_running || _events == null) return;
    _t += dt;

    while (_cursor < _events.Count && _events[_cursor].time <= _t)
    {
      var ev = _events[_cursor++];
      EnemyManager.main.Spawn(ev.prefab, ev.lane);
    }
  }

  // NOTE: see note up top
  public void Pause() => _running = false;
  public void Resume() => _running = true;
}
