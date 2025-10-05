using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [Tooltip("Optional offset if you want this sprite to draw above/below others at same Y.")]
    public int orderOffset = 0;

    private SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        // Lower Y = higher sorting order (in front)
        sr.sortingOrder = -(int)(transform.position.y * 100) + orderOffset;
    }
}

