using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerRange : MonoBehaviour
{
    [SerializeField] private Tower Tower;
    private List<GameObject> targets = new List<GameObject>();

    void Start()
    {

    }

    void Update()
    {
      UpdateRange();

    }

    public void UpdateRange()
    {
      transform.localScale = new Vector3(Tower.range, Tower.range, Tower.range);
    }
}
