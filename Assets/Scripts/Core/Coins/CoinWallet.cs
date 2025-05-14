using UnityEngine;
using Unity.Netcode;

public class CoinWallet : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private BountyCoin coinPrefab;
    [SerializeField] private Health health;

    [Header("Settings")]
    [SerializeField] private float coinSpread = 3f;
    [SerializeField] private float bountyPercentage = 0.33f;
    [SerializeField] private int bountyCoinCount = 10;
    [SerializeField] private int minBountyCoinValue = 2;
    [SerializeField] private LayerMask layerMask;
    private Collider2D[] coinBuffer = new Collider2D[10];
    private float coinRadius;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        coinRadius = coinPrefab.GetComponent<CircleCollider2D>().radius;

        health.OnDie += HandleCoinsAfterDeath;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        health.OnDie -= HandleCoinsAfterDeath;
    }

    private void HandleCoinsAfterDeath(Health health)
    {
        int bountyValue = (int)(CoinCount.Value * bountyPercentage);
        int bountyCoinValue = bountyValue / bountyCoinCount;
        if (bountyCoinValue < minBountyCoinValue) { return; }

        for (int i = 0; i <= bountyCoinCount; i++)
        {
            BountyCoin coinInstance = Instantiate(coinPrefab, GetSpawnPosition(), Quaternion.identity);
            coinInstance.SetCoinValue(bountyCoinValue);
            coinInstance.NetworkObject.Spawn();
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

    public NetworkVariable<int> CoinCount = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
        );

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<Coin>(out Coin coin)) { return; }

        int value = coin.Collect();

        if (!IsServer) { return; }

        CoinCount.Value += value;
    }
}

