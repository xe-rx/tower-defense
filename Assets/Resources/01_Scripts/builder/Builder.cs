using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave-agnostic Builder agent that walks an injected path of nodes,
/// dwells at each node, reports completion, and drives animations.
/// Axis-aligned movement only (no diagonals).
/// Walk uses left/right clips (no mirroring).
/// Idle always faces left (flipX = true).
/// Build animation always faces right (flipX = false).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Builder : MonoBehaviour
{
  // ------------ Config ------------
  [Header("Motion")]
  [SerializeField] private float moveSpeed = 4f;           // units/sec
  [SerializeField] private float axisSnapEpsilon = 0.005f; // sub-leg snap to avoid jitter
  [Tooltip("If true, choose the axis with the larger absolute delta first (feels most direct). If false, always X then Y.")]
  [SerializeField] private bool preferLongestAxisFirst = true;

  [Header("Dwell")]
  [SerializeField] private float dwellTime = 1.0f;         // seconds to wait at each node
  [Tooltip("Accept a press this many seconds before dwell end as 'on time'.")]
  [SerializeField] private float inputBufferSeconds = 0.15f;

  [Header("Animation")]
  [Tooltip("Animator with parameters: MoveX (float), MoveY (float), IsMoving (bool), LastX (float), LastY (float), Trigger(Build).")]
  [SerializeField] private Animator animator;
  [SerializeField] private SpriteRenderer sprite;          // flipped only for idle (left) and forced right during Build
  [Tooltip("Animator trigger name for the one-shot build/hammer animation.")]
  [SerializeField] private string buildTriggerName = "Build";
  [Tooltip("State name of the one-shot Build animation (used to force facing right while it plays).")]
  [SerializeField] private string buildStateName = "Build";

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
  public event System.Action<PlotNode> OnBuildRequested;

  // ------------ Internals ------------
  private enum State { Idle, WalkingX, WalkingY, Dwelling, Completed, Cancelled }
  private State _state = State.Idle;

  private Rigidbody2D _rb;
  private List<Transform> _path;      // injected at runtime
  private Vector2 _target;            // full node position
  private Vector2 _subTarget;         // current axis leg target
  private bool _movingAlongXFirst;

  // dwell/input
  private float _dwellTimer;
  private bool _pressedDuringDwell;
  private bool _hasBufferedPress;
  private bool _buildPlayedThisDwell;

  // selector cache
  private readonly Dictionary<Transform, GameObject> _selectorMap = new();

  // ---------- Selector helpers ----------
  private GameObject GetSelectorGO(Transform node)
  {
    if (!node) return null;
    if (_selectorMap.TryGetValue(node, out var go)) return go;
    var child = node.Find("Selector");
    go = child ? child.gameObject : null;
    _selectorMap[node] = go;
    return go;
  }
  private void HideAllSelectors()
  {
    foreach (var kv in _selectorMap)
      if (kv.Value) kv.Value.SetActive(false);
  }
  private void ShowSelector(Transform node, bool show)
  {
    var go = GetSelectorGO(node);
    if (go) go.SetActive(show);
  }

  // ------------- Unity -------------
  private void Awake()
  {
    _rb = GetComponent<Rigidbody2D>();
    _rb.gravityScale = 0f;
    _rb.linearDamping = 0f;
    _rb.angularDamping = 0f;

    if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    if (sprite == null) sprite = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
  }

  private void Update()
  {
    // Dwell timing
    if (_state == State.Dwelling)
    {
      _dwellTimer -= Time.deltaTime;

      if (_hasBufferedPress && _dwellTimer <= inputBufferSeconds)
      {
        _pressedDuringDwell = true;
        _hasBufferedPress = false;
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
      MoveTowardsAxisAligned(true);
    }
    else if (_state == State.WalkingY)
    {
      MoveTowardsAxisAligned(false);
    }
    else
    {
      _rb.linearVelocity = Vector2.zero;
    }

    UpdateAnimatorIntent();
    ApplyFacingRules();
  }

  // ------------- Public API -------------
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

    if (speedOverride.HasValue) moveSpeed = Mathf.Max(0.01f, speedOverride.Value);
    if (dwellOverride.HasValue) dwellTime = Mathf.Max(0f, dwellOverride.Value);

    _path = new List<Transform>(nodes);

    // Hide all selectors by default
    _selectorMap.Clear();
    foreach (var t in _path)
    {
      if (!t) continue;
      var go = GetSelectorGO(t);
      if (go) go.SetActive(false);
    }

    IsPathCompleted = false;
    IsPathRunning = true;
    CurrentNodeIndex = -1;
    _pressedDuringDwell = false;
    _hasBufferedPress = false;
    _buildPlayedThisDwell = false;
    _rb.linearVelocity = Vector2.zero;

    OnPathStarted?.Invoke(_path);
    AdvanceToNextNode();
  }

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

  /// <summary> Call this from your input system when Space is pressed. </summary>
  public void RegisterSpacePress()
  {
    if (_state == State.Dwelling)
    {
      _pressedDuringDwell = true;

      // Trigger the one-shot Build animation (you already have this)
      if (!_buildPlayedThisDwell && animator != null && !string.IsNullOrEmpty(buildTriggerName))
      {
        animator.ResetTrigger(buildTriggerName);
        animator.SetTrigger(buildTriggerName);
        _buildPlayedThisDwell = true;
      }

      // NEW: raise a build request for the current plot
      // We assume each node in _path is on (or under) a PlotNode.
      PlotNode plot = null;
      if (_path != null && CurrentNodeIndex >= 0 && CurrentNodeIndex < _path.Count)
      {
        var nodeT = _path[CurrentNodeIndex];
        if (nodeT) plot = nodeT.GetComponentInParent<PlotNode>();
      }
      if (plot != null) OnBuildRequested?.Invoke(plot);
    }
    else
    {
      _hasBufferedPress = true;
    }
  }


  // ------------- Internals -------------
  private void AdvanceToNextNode()
  {
    CurrentNodeIndex++;

    if (_path == null || CurrentNodeIndex >= _path.Count)
    {
      CompletePath();
      return;
    }

    Transform node = _path[CurrentNodeIndex];
    _target = node.position;

    // Choose axis order
    Vector2 pos = _rb.position;
    Vector2 delta = _target - pos;
    _movingAlongXFirst = preferLongestAxisFirst
        ? Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
        : true; // always X first

    // First leg sub-target
    _subTarget = _movingAlongXFirst
        ? new Vector2(_target.x, pos.y)
        : new Vector2(pos.x, _target.y);

    // If first leg already aligned, skip to second leg/arrive
    if ((_subTarget - pos).sqrMagnitude <= axisSnapEpsilon * axisSnapEpsilon)
    {
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
    float step = moveSpeed * Time.fixedDeltaTime;

    if (xLeg)
    {
      float newX = Mathf.MoveTowards(pos.x, _subTarget.x, step);
      var nextPos = new Vector2(newX, pos.y);
      _rb.linearVelocity = new Vector2(Mathf.Sign(_subTarget.x - pos.x) * moveSpeed, 0f);
      _rb.MovePosition(nextPos);

      if ((nextPos - _subTarget).sqrMagnitude <= axisSnapEpsilon * axisSnapEpsilon)
      {
        _rb.position = _subTarget;
        _rb.linearVelocity = Vector2.zero;
        BeginSecondLegOrArrive();
      }
    }
    else
    {
      float newY = Mathf.MoveTowards(pos.y, _subTarget.y, step);
      var nextPos = new Vector2(pos.x, newY);
      _rb.linearVelocity = new Vector2(0f, Mathf.Sign(_subTarget.y - pos.y) * moveSpeed);
      _rb.MovePosition(nextPos);

      if ((nextPos - _subTarget).sqrMagnitude <= axisSnapEpsilon * axisSnapEpsilon)
      {
        _rb.position = _subTarget;
        _rb.linearVelocity = Vector2.zero;
        BeginSecondLegOrArrive();
      }
    }
  }

  private void BeginSecondLegOrArrive()
  {
    Vector2 pos = _rb.position;
    bool xDone = Mathf.Abs(pos.x - _target.x) <= axisSnapEpsilon;
    bool yDone = Mathf.Abs(pos.y - _target.y) <= axisSnapEpsilon;

    if (xDone && yDone)
    {
      _rb.position = _target;
      ArriveAtNode();
      return;
    }

    _subTarget = _target;
    _state = xDone ? State.WalkingY : State.WalkingX;
  }

  private void ArriveAtNode()
  {
    Transform node = _path[CurrentNodeIndex];
    OnNodeArrived?.Invoke(CurrentNodeIndex, node);

    // Selector ON during dwell
    ShowSelector(_path[CurrentNodeIndex], true);

    // Start dwell
    _dwellTimer = Mathf.Max(0f, dwellTime);
    _pressedDuringDwell = false;
    _hasBufferedPress = false;
    _buildPlayedThisDwell = false;
    _state = State.Dwelling;
    OnDwellStarted?.Invoke(CurrentNodeIndex, _dwellTimer);

    if (_dwellTimer <= 0f)
    {
      EndDwellAndAdvance();
    }
  }

  private void EndDwellAndAdvance()
  {
    // Selector OFF for the node we just finished
    ShowSelector(_path[CurrentNodeIndex], false);

    bool pressed = _pressedDuringDwell;
    OnDwellCompleted?.Invoke(CurrentNodeIndex, pressed);

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
    HideAllSelectors();
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
    _buildPlayedThisDwell = false;
    IsPathCompleted = false;
    IsPathRunning = false;
    _state = State.Idle;
  }

  // -------- Animator driving (intent-based, synced with physics) --------
  private void UpdateAnimatorIntent()
  {
    if (animator == null) return;

    bool moving = (_state == State.WalkingX || _state == State.WalkingY);

    if (moving)
    {
      Vector2 pos = _rb.position;
      float mx, my;

      if (_state == State.WalkingX)
      {
        float dx = _subTarget.x - pos.x;
        mx = Mathf.Approximately(dx, 0f) ? 0f : Mathf.Sign(dx);
        my = 0f;
      }
      else
      {
        float dy = _subTarget.y - pos.y;
        mx = 0f;
        my = Mathf.Approximately(dy, 0f) ? 0f : Mathf.Sign(dy);
      }

      animator.SetBool("IsMoving", true);
      animator.SetFloat("MoveX", mx);
      animator.SetFloat("MoveY", my);
      animator.SetFloat("LastX", mx);
      animator.SetFloat("LastY", my);
    }
    else
    {
      animator.SetBool("IsMoving", false);
      // keep LastX/LastY as-is for idle facing logic in Animator if needed
    }
  }

  /// <summary>
  /// Apply facing rules for SpriteRenderer:
  /// - While Build state is playing: force right (flipX = false)
  /// - While walking: no mirroring (flipX = false) because you have distinct left/right walk clips
  /// - While idle/dwelling/completed/cancelled: always face left (flipX = true)
  /// </summary>
  private void ApplyFacingRules()
  {
    if (sprite == null || animator == null) return;

    var st = animator.GetCurrentAnimatorStateInfo(0);
    bool inBuild = st.IsName(buildStateName);

    if (inBuild)
    {
      sprite.flipX = false; // Build always faces right
      return;
    }

    switch (_state)
    {
      case State.WalkingX:
      case State.WalkingY:
        sprite.flipX = false; // walking clips handle L/R; no code mirroring
        break;

      default:
        // Idle/Dwelling/Completed/Cancelled/Idle state: always face left
        sprite.flipX = true;
        break;
    }
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
        Vector3 a = _path[i].position;
        Vector3 b = _path[i + 1].position;
        Vector3 mid = new Vector3(b.x, a.y, a.z);
        Gizmos.DrawLine(a, mid);
        Gizmos.DrawLine(mid, b);
      }
    }
  }
#endif
}

