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
      if (cooldown >= fireRate)
      {
        // Tower tracks enemy
        // transform.right = target.transform.position - transform.position;

        target.GetComponent<Enemy>().DamageEnemy(damage);
        cooldown = 0f;
        Debug.Log("enemy damaged");

      }
      else
      {
        cooldown += 1 * Time.deltaTime;
      }

    }
  }
}
