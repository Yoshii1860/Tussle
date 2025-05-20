using UnityEngine;

public class ObjectLayerSorting : MonoBehaviour
{
    void Start()
    {
        // Get all SpriteRenderers in children
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        // Use each sprite's Y-position
        float yPos = spriteRenderer.transform.position.y;
        // Set sorting order: lower Y = higher order (rendered later, on top)
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(yPos * 100);
    }
}