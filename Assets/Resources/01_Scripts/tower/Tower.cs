using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
  public float range = 5f;
  public int damage = 1;
  public float fireRate = 1f;

  public GameObject target;
  private float cooldown = 0f;

  void Start()
  {

  }

  void Update()
  {
    if (target)
    {
      var enemy = target.GetComponent<Enemy>();
      if (enemy == null || enemy.IsDying) { target = null; return; }

      if (cooldown >= fireRate)
      {
        enemy.DamageEnemy(damage);
        cooldown = 0f;
        Debug.Log("enemy damaged");
      }
      else
      {
        cooldown += Time.deltaTime;
      }
    }
  }
}
