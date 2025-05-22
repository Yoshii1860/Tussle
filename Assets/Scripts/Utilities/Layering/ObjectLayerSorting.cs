using UnityEngine;

public class ObjectLayerSorting : MonoBehaviour
{
    void Start()
    {
        if (!ApplicationData.Mode().Equals("server"))
        {
            // Get all SpriteRenderers in children
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) { return; }
            // Use each sprite's Y-position
            float yPos = spriteRenderer.transform.position.y;
            // Set sorting order: lower Y = higher order (rendered later, on top)
            spriteRenderer.sortingOrder = -Mathf.RoundToInt(yPos * 100);
        }
    }
}