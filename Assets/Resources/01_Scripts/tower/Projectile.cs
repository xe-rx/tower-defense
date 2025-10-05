using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
  [Header("Tuning")]
  public float speed = 7f;
  public float maxLifetime = 3f;
  public bool homing = true;
  [Tooltip("Small delay so the impact spark reads before damage applies.")]
  public float impactDamageDelay = 0.05f;

  [Header("FX")]
  public GameObject impactPrefab;

  private Transform target;
  private int pendingDamage;
  private Vector3 lastKnownPos;
  private bool hasHit;
  private Rigidbody2D rb;

  void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    rb.gravityScale = 0f;
    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
  }

  /// <summary>
  /// Initialize this projectile toward a target with a given damage amount.
  /// </summary>
  public void Init(Transform t, int towerDamage, float spd, bool isHoming)
  {
    target = t;
    pendingDamage = towerDamage;
    speed = spd;
    homing = isHoming;

    lastKnownPos = t ? t.position : transform.position + transform.right * 2f;
    StartCoroutine(Lifetime());
  }

  void Update()
  {
    if (hasHit) return;

    Vector3 aim = target ? target.position : lastKnownPos;
    Vector3 to = aim - transform.position;
    if (to.sqrMagnitude < 0.0001f) return;

    // rotate to face flight direction
    transform.right = to.normalized;

    float step = speed * Time.deltaTime;
    Vector3 delta = to.normalized * step;
    transform.position += delta;

    if (!homing && target != null)
    {
      // lock trajectory after first frame
      lastKnownPos = target.position;
      target = null;
    }
  }

  private void OnTriggerEnter2D(Collider2D col)
  {
    if (hasHit) return;
    if (!col.CompareTag("Enemy")) return;

    var enemy = col.GetComponent<Enemy>();
    if (enemy == null || enemy.IsDying) return;

    hasHit = true;

    // stop movement immediately
    rb.linearVelocity = Vector2.zero;
    rb.simulated = false;

    if (impactPrefab)
      Instantiate(impactPrefab, transform.position, Quaternion.identity);

    StartCoroutine(DoDamageThenDie(enemy));
  }

  private IEnumerator DoDamageThenDie(Enemy enemy)
  {
    if (impactDamageDelay > 0f)
      yield return new WaitForSeconds(impactDamageDelay);

    if (enemy != null && !enemy.IsDying)
      enemy.DamageEnemy(pendingDamage);

    Destroy(gameObject);
  }

  private IEnumerator Lifetime()
  {
    yield return new WaitForSeconds(maxLifetime);
    if (!hasHit) Destroy(gameObject);
  }
}

