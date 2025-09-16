using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave-agnostic Builder agent that walks an injected path of nodes,
/// stops to dwell (equal time) at each node, and reports completion.
/// Movement is constrained to axis-aligned legs (no diagonals).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Builder : MonoBehaviour
{
  // ------------ Config ------------
  [Header("Motion")]
  [SerializeField] private float moveSpeed = 4f;                 // units/sec
  [SerializeField] private float arrivalTolerance = 0.05f;       // node snap threshold
  [SerializeField] private float axisSnapEpsilon = 0.005f;       // sub-leg snap to avoid jitter
  [Tooltip("If true, choose the axis with the larger absolute delta first (feels most direct). If false, always X then Y.")]
  [SerializeField] private bool preferLongestAxisFirst = true;

  [Header("Dwell")]
  [SerializeField] private float dwellTime = 1.0f;               // seconds to wait at each node
  [Tooltip("Accept a press this many seconds before dwell end as 'on time'.")]
  [SerializeField] private float inputBufferSeconds = 0.15f;

  // ------------ Public read-only state ------------
  public bool IsPathRunning { get; private set; }
  public bool IsPathCompleted { get; private set; }
  public int CurrentNodeIndex { get; private set; } = -1;

  // ------------ Events ------------
  public event Action<IReadOnlyList<Transform>> OnPathStarted;
  public event Action<int, Transform> OnNodeArrived;
  public event Action<int, float> OnDwellStarted;
  public event Action<int, bool> OnDwellCompleted;
  public event Action OnPathCompleted;
  public event Action<string> OnPathCancelled;

  // ------------ Internals ------------
  private enum State { Idle, WalkingX, WalkingY, Dwelling, Completed, Cancelled }
  private State _state = State.Idle;

  private Rigidbody2D _rb;
  private List<Transform> _path;         // injected at runtime
  private Vector2 _target;               // full node position
  private Vector2 _subTarget;            // current axis leg target
  private bool _movingAlongXFirst;

  // dwell/input
  private float _dwellTimer;
  private bool _pressedDuringDwell;
  private float _lastPressTime;
  private bool _hasBufferedPress;


  private readonly Dictionary<Transform, GameObject> _selectorMap = new();
  private Transform _lastSelectorNode;

  private GameObject GetSelectorGO(Transform node)
  {
    if (!node) return null;
    if (_selectorMap.TryGetValue(node, out var go)) return go;

    // Look for a child named "Selector"
    var child = node.Find("Selector");
    go = child ? child.gameObject : null;
    _selectorMap[node] = go;
    return go;
  }

  private void HideAllSelectors()
  {
    foreach (var kv in _selectorMap)
      if (kv.Value) kv.Value.SetActive(false);
    _lastSelectorNode = null;
  }

  private void ShowSelector(Transform node, bool show)
  {
    var go = GetSelectorGO(node);
    if (go) go.SetActive(show);
    if (show) _lastSelectorNode = node;
  }

  // ------------- Unity -------------
  private void Awake()
  {
    _rb = GetComponent<Rigidbody2D>();
    // Make sure physics won’t fight you
    _rb.gravityScale = 0f;
    _rb.linearDamping = 0f;
    _rb.angularDamping = 0f;
  }

  private void Update()
  {
    // We just time the dwell in Update for frame-rate independence; movement happens in FixedUpdate.
    if (_state == State.Dwelling)
    {
      _dwellTimer -= Time.deltaTime;

      // Late-buffer acceptance window:
      if (_hasBufferedPress && _dwellTimer <= inputBufferSeconds)
      {
        _pressedDuringDwell = true;
        _hasBufferedPress = false; // consume buffer
      }

      if (_dwellTimer <= 0f)
      {
        EndDwellAndAdvance();
      }
    }
  }

  private void FixedUpdate()
  {
    if (_state == State.WalkingX)
    {
      MoveTowardsAxisAligned(xLeg: true);
    }
    else if (_state == State.WalkingY)
    {
      MoveTowardsAxisAligned(xLeg: false);
    }
    else
    {
      // Not walking: ensure we’re not drifting.
      _rb.linearVelocity = Vector2.zero;
    }
  }

  // ------------- Public API -------------

  /// <summary> Starts a fresh path. Resets previous progress. </summary>
  public void BeginPath(IReadOnlyList<Transform> nodes,
                        float? speedOverride = null,
                        float? dwellOverride = null)
  {
    if (nodes == null || nodes.Count == 0)
    {
      Debug.LogWarning("[Builder] BeginPath called with empty/null nodes.");
      CancelPath("EmptyPath");
      return;
    }

    // Optional overrides (useful per-wave without touching serialized defaults)
    if (speedOverride.HasValue) moveSpeed = Mathf.Max(0.01f, speedOverride.Value);
    if (dwellOverride.HasValue) dwellTime = Mathf.Max(0f, dwellOverride.Value);

    // Reset state
    _path = new List<Transform>(nodes);
    _selectorMap.Clear();
    foreach (var t in _path)
    {
      if (!t) continue;
      var go = GetSelectorGO(t);      // populates cache
      if (go) go.SetActive(false);    // ensure hidden by default
    }

    IsPathCompleted = false;
    IsPathRunning = true;
    CurrentNodeIndex = -1;
    _pressedDuringDwell = false;
    _hasBufferedPress = false;
    _rb.linearVelocity = Vector2.zero;

    OnPathStarted?.Invoke(_path);

    // Go to first node
    AdvanceToNextNode();
  }

  /// <summary> Cancels current path (resets state). </summary>
  public void CancelPath(string reason = "Cancelled")
  {
    if (!IsPathRunning && !IsPathCompleted)
    {
      HideAllSelectors();
      return;
    }

    _state = State.Cancelled;
    IsPathRunning = false;
    _rb.linearVelocity = Vector2.zero;
    OnPathCancelled?.Invoke(reason);
    ResetLocalState();
  }

  /// <summary> External input hook. Call this from your input system when Space is pressed. </summary>
  public void RegisterSpacePress()
  {
    _lastPressTime = Time.time;
    if (_state == State.Dwelling)
    {
      _pressedDuringDwell = true;
    }
    else
    {
      // buffer for acceptance near the end of dwell if allowed
      _hasBufferedPress = true;
    }
  }

  // ------------- Internals -------------

  private void AdvanceToNextNode()
  {
    CurrentNodeIndex++;

    if (_path == null || CurrentNodeIndex >= _path.Count)
    {
      // No more nodes → complete
      CompletePath();
      return;
    }

    Transform node = _path[CurrentNodeIndex];
    _target = node.position;

    // Choose axis order
    Vector2 pos = _rb.position;
    Vector2 delta = _target - pos;
    if (preferLongestAxisFirst)
    {
      _movingAlongXFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
    }
    else
    {
      _movingAlongXFirst = true; // always X first
    }

    // Compute first leg sub-target
    _subTarget = _movingAlongXFirst
        ? new Vector2(_target.x, pos.y)
        : new Vector2(pos.x, _target.y);

    // If first leg is already effectively reached, jump to second leg immediately
    if ((_subTarget - pos).sqrMagnitude <= axisSnapEpsilon * axisSnapEpsilon)
    {
      // Snap to exact subTarget to avoid tiny drift
      _rb.position = _subTarget;
      BeginSecondLegOrArrive();
    }
    else
    {
      _state = _movingAlongXFirst ? State.WalkingX : State.WalkingY;
    }
  }

  private void MoveTowardsAxisAligned(bool xLeg)
  {
    Vector2 pos = _rb.position;

    // Target one axis only
    Vector2 nextPos = pos;
    float step = moveSpeed * Time.fixedDeltaTime;

    if (xLeg)
    {
      float newX = Mathf.MoveTowards(pos.x, _subTarget.x, step);
      nextPos = new Vector2(newX, pos.y);
      // Velocity purely X (helps animations if you read velocity)
      _rb.linearVelocity = new Vector2(Mathf.Sign(_subTarget.x - pos.x) * moveSpeed, 0f);
    }
    else
    {
      float newY = Mathf.MoveTowards(pos.y, _subTarget.y, step);
      nextPos = new Vector2(pos.x, newY);
      _rb.linearVelocity = new Vector2(0f, Mathf.Sign(_subTarget.y - pos.y) * moveSpeed);
    }

    _rb.MovePosition(nextPos);

    // Reached sub-target?
    if ((nextPos - _subTarget).sqrMagnitude <= axisSnapEpsilon * axisSnapEpsilon)
    {
      // Snap exactly to avoid error accumulation
      _rb.position = _subTarget;
      _rb.linearVelocity = Vector2.zero;
      BeginSecondLegOrArrive();
    }
  }

  private void BeginSecondLegOrArrive()
  {
    Vector2 pos = _rb.position;

    // If first leg brought us to full target (delta on other axis ~0), we "arrived"
    bool xDone = Mathf.Abs(pos.x - _target.x) <= axisSnapEpsilon;
    bool yDone = Mathf.Abs(pos.y - _target.y) <= axisSnapEpsilon;

    if (xDone && yDone)
    {
      // Snap and dwell
      _rb.position = _target;
      ArriveAtNode();
      return;
    }

    // Otherwise, set second leg to complete to full target
    _subTarget = _target;
    _state = xDone ? State.WalkingY : State.WalkingX;
  }

  private void ArriveAtNode()
  {
    Transform node = _path[CurrentNodeIndex];
    OnNodeArrived?.Invoke(CurrentNodeIndex, node);

    // Toggle selector ON for current node during dwell
    ShowSelector(_path[CurrentNodeIndex], true);

    // Start dwell
    _dwellTimer = Mathf.Max(0f, dwellTime);
    _pressedDuringDwell = false;
    _hasBufferedPress = false;
    _state = State.Dwelling;
    OnDwellStarted?.Invoke(CurrentNodeIndex, _dwellTimer);

    // Special case: zero dwell means instant advance
    if (_dwellTimer <= 0f)
    {
      EndDwellAndAdvance();
    }
  }

  private void EndDwellAndAdvance()
  {
    // Turn OFF selector for the node we just finished
    ShowSelector(_path[CurrentNodeIndex], false);

    bool pressed = _pressedDuringDwell;
    OnDwellCompleted?.Invoke(CurrentNodeIndex, pressed);

    // Last node?
    if (CurrentNodeIndex >= _path.Count - 1)
    {
      CompletePath();
    }
    else
    {
      AdvanceToNextNode();
    }
  }

  private void CompletePath()
  {
    if (IsPathCompleted)
    {
      HideAllSelectors();
      return;
    }

    _state = State.Completed;
    IsPathRunning = false;
    IsPathCompleted = true;
    _rb.linearVelocity = Vector2.zero;

    OnPathCompleted?.Invoke();
  }

  private void ResetLocalState()
  {
    _path = null;
    CurrentNodeIndex = -1;
    _target = default;
    _subTarget = default;
    _movingAlongXFirst = true;
    _dwellTimer = 0f;
    _pressedDuringDwell = false;
    _hasBufferedPress = false;
    IsPathCompleted = false;
    IsPathRunning = false;
    _state = State.Idle;
  }

#if UNITY_EDITOR
  // Optional gizmos to visualize the current path and node indices
  private void OnDrawGizmosSelected()
  {
    if (_path == null || _path.Count == 0) return;

    Gizmos.color = Color.cyan;
    for (int i = 0; i < _path.Count; i++)
    {
      if (_path[i] == null) continue;
      Gizmos.DrawWireSphere(_path[i].position, 0.1f);
      if (i < _path.Count - 1 && _path[i + 1] != null)
      {
        // Draw orthogonal legs to make the axis-only intention clear
        Vector3 a = _path[i].position;
        Vector3 b = _path[i + 1].position;

        // Show X-then-Y polyline (just for clarity in editor)
        Vector3 mid = new Vector3(b.x, a.y, a.z);
        Gizmos.DrawLine(a, mid);
        Gizmos.DrawLine(mid, b);
      }
    }
  }
#endif
}

