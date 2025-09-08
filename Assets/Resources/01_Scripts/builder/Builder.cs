using UnityEngine;

public class Builder : MonoBehaviour
{

  private float movespeed = 4f;
  public Transform[] checkpoints;

  private Transform checkpoint;
  private int index = 0;

  private Rigidbody2D rb;

  void Awake()
  {
    rb = GetComponent<Rigidbody2D>();
  }

  void Start()
  {
    checkpoint = checkpoints[index];
  }

  void Update()
  {
    checkpoint = checkpoints[index];

    if (Vector2.Distance(checkpoint.transform.position, transform.position) <= 0.1f)
    {
      index++;
      if (index >= EnemyManager.main.checkpoints.Length)
      {
        index = 0;
      }
    }
  }

  void FixedUpdate()
  {
    Vector2 direction = (checkpoint.position - transform.position).normalized;
    rb.linearVelocity = direction * movespeed;
  }

}
