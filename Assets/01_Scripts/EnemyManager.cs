using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager main;

    public Transform spawnpoint;
    public Transform[] checkpoints;

    void Awake()
    {
      main = this;
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
