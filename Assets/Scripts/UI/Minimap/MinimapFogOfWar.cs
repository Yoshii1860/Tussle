using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class MinimapFogOfWar : MonoBehaviour
{
    [SerializeField] private RawImage fogOverlay; // The UI element displaying the fog texture
    [SerializeField] private float revealRadius = 10f; // Radius of the area revealed by the player
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f); // Size of the game map in world units
    [SerializeField] public Vector2Int FogTextureSize = new Vector2Int(256, 256); // Resolution of the fog texture
    [HideInInspector]
    [SerializeField] private Texture2D fogTexture; // Texture representing the Fog of War
    [SerializeField] private float resizeFactor = 4.85f; // Factor to resize the minimap
    [SerializeField] private GameObject closeButton;

    private Transform playerTransform; // Reference to the player's transform
    private Color[] fogColors; // Pixel data for the fog texture

    private void Awake()
    {
        if (fogTexture != null) { return; }
        if (NetworkManager.Singleton.IsServer) { return; }

        closeButton.SetActive(false);

        // Initialize the fog texture
        fogTexture = new Texture2D(FogTextureSize.x, FogTextureSize.y, TextureFormat.RGBA32, false);
        fogColors = new Color[FogTextureSize.x * FogTextureSize.y];
        fogTexture.filterMode = FilterMode.Bilinear; // Set filter mode for better quality
        Debug.Log($"[MinimapFogOfWar] Awake: {GetInstanceID()}, fogOverlay: {fogOverlay}, fogTexture: {fogTexture.GetInstanceID()}");

        // Start with the entire map covered (black, fully opaque)
        for (int i = 0; i < fogColors.Length; i++)
        {
            fogColors[i] = new Color(0, 0, 0, 1); // Black with full opacity
        }

        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();
        fogOverlay.texture = fogTexture;
        Debug.Log($"[MinimapFogOfWar] Assigned texture: {fogOverlay.texture != null}");
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer) { return; }
        
        StartCoroutine(UpdateFogOfWar());
    }

    public void OpenMap()
    {
        closeButton.SetActive(true);
        transform.localScale = new Vector3(resizeFactor, resizeFactor, 1f);
    }

    public void CloseMap()
    {
        closeButton.SetActive(false);
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    private IEnumerator WaitForPlayerObject()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        playerTransform = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
    }

    private IEnumerator UpdateFogOfWar()
    {
        while (true)
        {
            if (playerTransform == null)
            {
                yield return StartCoroutine(WaitForPlayerObject());
                continue;
            }

            Vector2 playerPos = new Vector2(playerTransform.position.x, playerTransform.position.y);

            Vector2 normalizedPos = new Vector2(
                (playerPos.x + mapSize.x / 2f) / mapSize.x,
                (playerPos.y + mapSize.y / 2f) / mapSize.y
            );

            Vector2Int texturePos = new Vector2Int(
                Mathf.FloorToInt(normalizedPos.x * FogTextureSize.x),
                Mathf.FloorToInt(normalizedPos.y * FogTextureSize.y)
            );

            RevealArea(texturePos);

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void RevealArea(Vector2Int center)
    {
        int radiusInPixels = Mathf.FloorToInt(revealRadius * FogTextureSize.x / mapSize.x);

        for (int y = -radiusInPixels; y <= radiusInPixels; y++)
        {
            for (int x = -radiusInPixels; x <= radiusInPixels; x++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= radiusInPixels)
                {
                    int texX = Mathf.Clamp(center.x + x, 0, FogTextureSize.x - 1);
                    int texY = Mathf.Clamp(center.y + y, 0, FogTextureSize.y - 1);
                    int index = texY * FogTextureSize.x + texX;

                    // Fully reveal within the radius, no fading
                    fogColors[index] = new Color(0, 0, 0, 0); // Fully transparent
                }
            }
        }

        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();
    }

    // Optional: Reset the fog when the player respawns or the game restarts
    public void ResetFog()
    {
        for (int i = 0; i < fogColors.Length; i++)
        {
            fogColors[i] = new Color(0, 0, 0, 1);
        }
        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();
    }
}