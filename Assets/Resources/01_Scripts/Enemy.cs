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

    Vector2 direction = (checkpoint.position - transform.position).normalized;
    rb.linearVelocity = direction * movespeed;

    var st = animator.GetCurrentAnimatorStateInfo(0);
    if (st.IsName("EnemyHit")) return;

    // Basic animation switching
    if (direction.x > 0.1f)
    {
      animator.Play("EnemyRight");
      spriteRenderer.flipX = false;
    }
    else if (direction.x < -0.1f)
    {
      animator.Play("EnemyRight");
      spriteRenderer.flipX = true;
    }
    else
    {
      animator.Play("EnemyRight");
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

