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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        checkpoint = EnemyManager.main.checkpoints[index];
    }

    void Update()
    {
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
            KillEnemy(true); // enemy killed by towers/projectiles -> give gold
        }
    }

    void FixedUpdate()
    {
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
        hitpoints -= dmg;
        animator.Play("EnemyHit");
    }

    private void LeakAndDamage()
    {
        if (PlayerSession.main != null)
            PlayerSession.main.ApplyDamage(damage);

        KillEnemy(false); // leaked -> no gold reward
    }

    public void KillEnemy(bool giveReward)
    {
        if (giveReward && PlayerSession.main != null)
            PlayerSession.main.AddGold(value);

        Destroy(gameObject);
        EnemyTracker.AliveCount--;
        // TODO: add death animation or effects here if desired
    }
}

