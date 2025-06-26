using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Tilemaps;

public class ItemDropper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private PropHealth propHealth;
    [Space(10)]

    [Header("Prefabs")]
    [SerializeField] private BountyCoin coinPrefab;
    [SerializeField] private GameObject[] itemPrefabs; // Array of prefabs for other items to drop
    [Space(10)]

    [Header("Settings")]
    [SerializeField] private float coinSpread = 3f;
    [SerializeField] private int minBountyCoinCount = 3;
    [SerializeField] private int maxBountyCoinCount = 10;
    [SerializeField] private int minBountyCoinValue = 2;
    [SerializeField] private int maxBountyCoinValue = 5;
    [SerializeField] private float difficultyMultiplier = 1f;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float itemDropChance = 0.5f;

    [Space(10)]
    [Header("Chest Settings")]
    [SerializeField] private float chestOpenDuration = 3f;
    public float ChestOpenDuration => chestOpenDuration;
    private float coinRadius;
    private bool isLocked = false;
    public bool IsLocked() => isLocked;
    private bool isChest = false;
    public bool IsChest() => isChest;

    private void Start()
    {
        coinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius;

        if (health != null) health.OnDie += DropItems;
        else if (propHealth != null) propHealth.OnDestroyed += DropItems;
        else isChest = true;
    }

    public void DropItems()
    {
        int bountyCoinCount = Mathf.RoundToInt(Random.Range(minBountyCoinCount, maxBountyCoinCount + 1) * difficultyMultiplier);
        int bountyCoinValue = Mathf.RoundToInt(Random.Range(minBountyCoinValue, maxBountyCoinValue + 1) * difficultyMultiplier);

        for (int i = 0; i < bountyCoinCount; i++)
        {
            BountyCoin coinInstance = Instantiate(coinPrefab, GetSpawnPosition(), Quaternion.identity);
            coinInstance.SetCoinValue(bountyCoinValue);
            coinInstance.NetworkObject.Spawn();
        }

        int random = Random.Range(0, 100);
        if (random < itemDropChance * 100)
        {
            GameObject itemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
            GameObject itemInstance = Instantiate(itemPrefab, GetSpawnPosition(), Quaternion.identity);
            if (itemInstance.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                networkObject.Spawn();
            }
        }
    }

    public void OpenChest()
    {
        if (isLocked) return;
        isLocked = true;

        StartCoroutine(ChestRoutine());
    }

    private IEnumerator ChestRoutine()
    {
        SetChestOpenClientRpc(true);
        yield return new WaitForSeconds(1.5f);
        DropItems();
        yield return new WaitForSeconds(3f);
        SetChestOpenClientRpc(false);
        yield return new WaitForSeconds(2f);
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    private void SetChestOpenClientRpc(bool open)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            AudioManager.Instance.PlaySFXAtPosition("Chest", transform.position);
            anim.SetBool("Open", open);
        }
    }

    private Vector2 GetSpawnPosition()
    {
        Tilemap[] tilemaps = GameManager.Instance.GetAllTilemaps();
        int maxAttempts = 50;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 spawnPoint = (Vector2)transform.position + Random.insideUnitCircle * coinSpread;
            bool isOccupied = Physics2D.OverlapCircle(spawnPoint, coinRadius) != null;

            if (tilemaps != null)
            {
                foreach (Tilemap tm in tilemaps)
                {
                    if (tm != null && tm.GetComponent<TilemapCollider2D>() != null)
                    {
                        Vector3Int cellPosition = tm.WorldToCell(spawnPoint);
                        TileBase tile = tm.GetTile(cellPosition);
                        if (tile != null)
                        {
                            Collider2D tileCollider = Physics2D.OverlapPoint(spawnPoint, layerMask);
                            if (tileCollider != null && tileCollider is TilemapCollider2D)
                            {
                                isOccupied = true;
                                Debug.Log($"Collision with tilemap at {spawnPoint}, Cell: {cellPosition}, Tilemap: {tm.name}");
                            }
                        }
                    }
                }
            }
            if (!isOccupied)
            {
                Debug.Log($"Valid spawn at {spawnPoint}");
                return spawnPoint;
            }
        }

        float coinSpreadIncrease = coinSpread * 2f;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 spawnPoint = (Vector2)transform.position + Random.insideUnitCircle * coinSpreadIncrease;
            bool isOccupied = Physics2D.OverlapCircle(spawnPoint, coinRadius / 2) != null;
            if (tilemaps != null)
            {
                foreach (Tilemap tm in tilemaps)
                {
                    if (tm != null && tm.GetComponent<TilemapCollider2D>() != null)
                    {
                        Vector3Int cellPosition = tm.WorldToCell(spawnPoint);
                        TileBase tile = tm.GetTile(cellPosition);
                        if (tile != null)
                        {
                            Collider2D tileCollider = Physics2D.OverlapPoint(spawnPoint, layerMask);
                            if (tileCollider != null && tileCollider is TilemapCollider2D)
                            {
                                isOccupied = true;
                                Debug.Log($"Collision with tilemap at {spawnPoint}, Cell: {cellPosition}, Tilemap: {tm.name}");
                            }
                        }
                    }
                }
            }
            if (!isOccupied)
            {
                Debug.Log($"Valid spawn at {spawnPoint}");
                return spawnPoint;
            }
        }
        Debug.LogWarning($"Falling back to FindValidFallbackPosition");
        return (Vector2)transform.position + Random.insideUnitCircle;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDie -= DropItems;
        else if (propHealth != null) propHealth.OnDestroyed -= DropItems;
    }
}



