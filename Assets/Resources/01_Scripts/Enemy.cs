using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Enemy : MonoBehaviour
{
  [SerializeField] private float movespeed = 0f;
  [SerializeField] private int hitpoints = 0;
  [SerializeField] private int value = 0;   // Gold reward when killed
  [SerializeField] private int damage = 0;  // Damage dealt to player if leaked

  private Rigidbody2D rb;
  private Transform checkpoint;
  private int index = 0;

  private Animator animator;
  private SpriteRenderer spriteRenderer;
  private Collider2D col;

  private bool isDying = false;   // true while death anim plays
  public bool IsDying => isDying;

  [Header("Pathing")]
  [SerializeField] private float arriveRadius = 0.2f; // a bit bigger for 2x/4x

  void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
    animator = GetComponent<Animator>();
    spriteRenderer = GetComponent<SpriteRenderer>();
    col = GetComponent<Collider2D>(); // add a collider if not present
  }

  void Start()
  {
    checkpoint = EnemyManager.main.checkpoints[index];
  }

  void Update()
  {
    if (isDying) return; // no logic while dying

    checkpoint = EnemyManager.main.checkpoints[index];

    if (Vector2.Distance(checkpoint.transform.position, transform.position) <= 0.1f)
    {
      index++;
      if (index >= EnemyManager.main.checkpoints.Length)
      {
        LeakAndDamage();
        return;
      }
    }

    if (hitpoints <= 0)
    {
      Die(giveReward: true); // killed by towers/projectiles -> give gold
    }
  }

  void FixedUpdate()
  {
    if (isDying) { rb.linearVelocity = Vector2.zero; return; }

    // Safety: make sure checkpoints exist
    if (EnemyManager.main == null || EnemyManager.main.checkpoints == null || EnemyManager.main.checkpoints.Length == 0)
      return;

    // Consume as many checkpoints as needed this physics step
    int safety = 8; // avoid infinite loops on bad data
    while (safety-- > 0)
    {
      if (index >= EnemyManager.main.checkpoints.Length)
      {
        // Already beyond last node (shouldn’t normally happen)
        LeakAndDamage();
        return;
      }

      checkpoint = EnemyManager.main.checkpoints[index];
      if (checkpoint == null) { index++; continue; }

      Vector2 pos = rb.position;
      Vector2 target = checkpoint.position;
      Vector2 toTarget = target - pos;
      float dist = toTarget.magnitude;

      // Reached (within radius): snap & advance
      if (dist <= arriveRadius)
      {
        rb.MovePosition(target);
        index++;
        if (index >= EnemyManager.main.checkpoints.Length)
        {
          LeakAndDamage();
          return;
        }
        // loop again in case we can reach next node this tick
        continue;
      }

      float step = movespeed * Time.fixedDeltaTime;

      // Would overshoot this frame? snap & advance
      if (step >= dist)
      {
        rb.MovePosition(target);
        index++;
        if (index >= EnemyManager.main.checkpoints.Length)
        {
          LeakAndDamage();
          return;
        }
        continue;
      }

      // Normal movement this frame
      Vector2 dir = toTarget / dist;
      rb.linearVelocity = dir * movespeed;

      // Animation intent (keep your "EnemyHit" guard)
      var st = animator.GetCurrentAnimatorStateInfo(0);
      if (!st.IsName("EnemyHit"))
      {
        if (dir.x > 0.1f)
        {
          animator.Play("EnemyRight");
          spriteRenderer.flipX = false;
        }
        else if (dir.x < -0.1f)
        {
          animator.Play("EnemyRight");
          spriteRenderer.flipX = true;
        }
        else
        {
          animator.Play("EnemyRight");
        }
      }

      // We’ve taken our movement for this tick; no more nodes to consume
      break;
    }
  }

  public void DamageEnemy(int dmg)
  {
    if (isDying) return; // ignore damage while dying
    hitpoints -= dmg;
    animator.Play("EnemyHit");
  }

  private void LeakAndDamage()
  {
    if (PlayerSession.main != null)
      PlayerSession.main.ApplyDamage(damage);

    Die(giveReward: false); // leaked -> no gold reward
  }

  // NEW: enter dying state, play animation, destroy on anim event
  public void Die(bool giveReward)
  {
    if (isDying) return; // guard double-entry
    isDying = true;

    // payout first so UI feels instant
    if (giveReward && PlayerSession.main != null)
      PlayerSession.main.AddGold(value);

    // stop motion/physics/targeting
    rb.linearVelocity = Vector2.zero;
    rb.angularVelocity = 0f;
    rb.simulated = false;         // freezes physics motion

    if (col) col.enabled = false; // drops out of tower range immediately
    gameObject.tag = "Untagged";  // ensure no longer matches "Enemy" filters

    animator.SetTrigger("Die");
    // DO NOT destroy here — wait for Animation Event to call Animation_DeathComplete()
  }

  // ANIMATION EVENT (add this event at the last frame of your EnemyDeath clip)
  public void Animation_DeathComplete()
  {
    EnemyTracker.AliveCount--;
    Destroy(gameObject);
  }

  // OPTIONAL: keep KillEnemy for compatibility (routes to Die)
  public void KillEnemy(bool giveReward)
  {
    Die(giveReward);
  }
}

