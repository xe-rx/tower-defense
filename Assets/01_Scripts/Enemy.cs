using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
  [SerializeField] private float movespeed = 0f;
  [SerializeField] private int hitpoints = 0;
  [SerializeField] private int value = 0;
  [SerializeField] private int damage = 0;

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
        Destroy(gameObject);
      }
    }
  }

  void FixedUpdate()
  {
    Vector2 direction = (checkpoint.position - transform.position).normalized;
    rb.linearVelocity = direction * movespeed;

    // Shitty way of handling animation for the time being
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
}
