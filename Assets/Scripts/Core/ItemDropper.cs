using UnityEngine;
using Unity.Netcode;

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
    private float coinRadius;

    private void Start()
    {
        coinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius;

        if (health != null) health.OnDie += DropItems;
        else propHealth.OnDestroyed += DropItems;
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

    private Vector2 GetSpawnPosition()
    {
        while (true)
        {
            Vector2 spawnPoint = (Vector2)transform.position + Random.insideUnitCircle * coinSpread;
            bool isOccupied = Physics2D.OverlapCircle(spawnPoint, coinRadius, layerMask) != null;
            if (!isOccupied)
            {
                return spawnPoint;
            }
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDie -= DropItems;
        else propHealth.OnDestroyed -= DropItems;
    }
}
