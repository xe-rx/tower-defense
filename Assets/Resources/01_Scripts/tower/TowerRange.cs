using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class TowerRange : MonoBehaviour
{
    [SerializeField] private Tower Tower;
    private readonly List<GameObject> targets = new();

    private CircleCollider2D circle;

    void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true; // make sure it's a trigger
    }

    void Start()
    {
        UpdateRange();
    }

    void Update()
    {
        // Clean out destroyed enemies
        while (targets.Count > 0 && targets[0] == null)
            targets.RemoveAt(0);

        Tower.target = targets.Count > 0 ? targets[0] : null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger entered by " + collision.name);

        if (collision.CompareTag("Enemy"))
        {
            targets.Add(collision.gameObject);
            Debug.Log("Enemy added to list: " + collision.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            targets.Remove(collision.gameObject);
            Debug.Log("Enemy removed from list: " + collision.name);
        }
    }

    public void UpdateRange()
    {
        // Set collider radius instead of scaling transform
        circle.radius = Tower.range > 0 ? Tower.range : 0.1f;
    }
}

