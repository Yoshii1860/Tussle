using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectLayerSorting : MonoBehaviour
{
    void Start()
    {
        if (!ApplicationData.Mode().Equals("server"))
        {
            // Get all SpriteRenderers in children
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Use each sprite's Y-position
                float yPos = spriteRenderer.transform.position.y;
                // Set sorting order: lower Y = higher order (rendered later, on top)
                spriteRenderer.sortingOrder = -Mathf.RoundToInt(yPos * 100);
            }
            else if (TryGetComponent(out TilemapRenderer tilemapRenderer) && TryGetComponent(out Tilemap tilemap))
            {
                int? minY = null;
                BoundsInt bounds = tilemap.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (tilemap.HasTile(pos))
                    {
                        if (minY == null || pos.y < minY) minY = pos.y;
                    }
                }

                if (minY != null)
                {
                    // Use the lowest row's world Y position
                    Vector3 lowestWorldPos = tilemap.CellToWorld(new Vector3Int(0, minY.Value, 0));
                    float yPos = lowestWorldPos.y;
                    tilemapRenderer.sortingOrder = -Mathf.RoundToInt(yPos * 100);
                }
                else
                {
                    float yPos = tilemapRenderer.transform.position.y;
                    tilemapRenderer.sortingOrder = -Mathf.RoundToInt(yPos * 100);
                }
            }
            else
            {
                Debug.LogWarning("ObjectLayerSorting: No SpriteRenderer or TilemapRenderer found on the GameObject.");
            }
        }
    }
}