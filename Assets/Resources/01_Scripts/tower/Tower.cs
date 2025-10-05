using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
  public float range = 5f;
  public int damage = 1;
  public float fireRate = 1f;

  [Header("Projectile")]
  [SerializeField] private Projectile projectilePrefab; // drag your prefab
  [SerializeField] private Transform muzzle;           // optional spawn point
  [SerializeField] private float projectileSpeed = 7f;
  [SerializeField] private bool homingProjectiles = true;

  public GameObject target;
  private float cooldown = 0f;

  void Update()
  {
    if (target)
    {
      var enemy = target.GetComponent<Enemy>();
      if (enemy == null || enemy.IsDying) { target = null; return; }

      if (cooldown >= fireRate)
      {
        Fire(enemy.transform);
        cooldown = 0f;
      }
      else
      {
        cooldown += Time.deltaTime;
      }
    }
  }

  private void Fire(Transform enemy)
  {
    if (projectilePrefab == null)
    {
      Debug.LogWarning("No projectilePrefab set on Tower.");
      return;
    }

    Vector3 pos = muzzle ? muzzle.position : transform.position;
    var p = Instantiate(projectilePrefab, pos, Quaternion.identity);
    p.Init(enemy, damage, projectileSpeed, homingProjectiles);
  }

}

