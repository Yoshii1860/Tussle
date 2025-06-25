using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class PropSpawnInfo
{
    public string sectionName;
    public string subSectionName = "Default";
    public GameObject propPrefab;
    public Transform spawnPoint;
}

public class PropSpawnerManager : NetworkBehaviour
{
    [Header("Fixed Prop Spawns")]
    public List<PropSpawnInfo> fixedProps;
    public float propRespawnDelay = 10f;

    [Header("Random Item Spawns")]
    public bool enableRandomItems = false;
    public List<GameObject> randomItemPrefabs;
    public Vector2 randomX;
    public Vector2 randomY;
    public float randomItemSpawnInterval = 20f;
    public int maxRandomItems = 10;

    private Dictionary<PropSpawnInfo, GameObject> spawnedProps = new();
    private HashSet<GameObject> activeRandomItems = new();

    private void Start()
    {
        if (IsServer)
        {
            // Spawn all fixed props
            foreach (var info in fixedProps)
            {
                SpawnFixedProp(info);
            }

            // Start random item spawn coroutine if enabled
            if (enableRandomItems)
                StartCoroutine(RandomItemSpawnRoutine());
        }
    }

    private void SpawnFixedProp(PropSpawnInfo info)
    {
        GameObject prop = Instantiate(info.propPrefab, info.spawnPoint.position, info.spawnPoint.rotation);
        var netObj = prop.GetComponent<NetworkObject>();
        netObj.Spawn();
        spawnedProps[info] = prop;

        // Subscribe to destruction event
        var propHealth = prop.GetComponent<PropHealth>();
        if (propHealth != null)
        {
            propHealth.OnDestroyed += () => StartCoroutine(RespawnPropAfterDelay(info));
        }
    }

    private IEnumerator RespawnPropAfterDelay(PropSpawnInfo info)
    {
        yield return new WaitForSeconds(propRespawnDelay);

        if (spawnedProps.ContainsKey(info))
            spawnedProps.Remove(info);

        SpawnFixedProp(info);
    }

    private IEnumerator RandomItemSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(randomItemSpawnInterval);

            if (activeRandomItems.Count >= maxRandomItems) continue;
            if (randomItemPrefabs.Count == 0) continue;

            Vector2 pos = GetRandomSpawnPosition();
            GameObject prefab = randomItemPrefabs[Random.Range(0, randomItemPrefabs.Count)];
            GameObject item = Instantiate(prefab, pos, Quaternion.identity);
            var netObj = item.GetComponent<NetworkObject>();
            netObj.Spawn();
            activeRandomItems.Add(item);

            // Remove from set when destroyed
            var propHealth = item.GetComponent<PropHealth>();
            if (propHealth != null)
            {
                propHealth.OnDestroyed += () => StartCoroutine(RemoveRandomItemDelayed(item));
            }
            else
            {
                // If item doesn't have PropHealth, remove after a fixed time (optional)
                StartCoroutine(RemoveRandomItemDelayed(item, 60f));
            }
        }
    }

    private IEnumerator RemoveRandomItemDelayed(GameObject item, float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        activeRandomItems.Remove(item);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        while (true)
        {
            float x = Random.Range(randomX.x, randomX.y);
            float y = Random.Range(randomY.x, randomY.y);
            Vector2 spawnPoint = new Vector2(x, y);
            bool isOccupied = Physics2D.OverlapCircle(spawnPoint, 0.5f) != null;
            if (!isOccupied)
                return spawnPoint;
        }
    }
}