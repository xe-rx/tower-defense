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
  void Start()
  {

  }

  void Update()
  {

  }
}
