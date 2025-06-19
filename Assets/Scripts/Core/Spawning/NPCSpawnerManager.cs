using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public class NPCSpawnInfo
{
    public string sectionName;
    public string subSectionName = "Default";
    public GameObject npcPrefab;
    public Transform spawnPoint;
    public Transform[] patrolPoints;
}

public class NPCSpawnerManager : NetworkBehaviour
{
    [Header("Fixed Spawns")]
    public List<NPCSpawnInfo> fixedSpawns;
    public float respawnDelay = 10f;

    [Header("Random Spawns")]
    public bool enableRandomSpawns = false;
    public List<GameObject> randomNpcPrefabs;
    public Vector2 randomX;
    public Vector2 randomY;
    public float randomSpawnInterval = 20f;
    public int maxRandomSpawns = 10;

    private HashSet<GameObject> activeRandomNPCs = new HashSet<GameObject>();
    private Dictionary<NPCSpawnInfo, GameObject> spawnedNPCs = new();

    private void Start()
    {
        if (IsServer)
        {
            // Spawn all fixed NPCs
            foreach (var spawnInfo in fixedSpawns)
            {
                SpawnAtInfo(spawnInfo);
            }

            // Start random spawn coroutine if enabled
            if (enableRandomSpawns)
                StartCoroutine(RandomSpawnRoutine());
        }
    }

    private void SpawnAtInfo(NPCSpawnInfo info)
    {
        GameObject npc = Instantiate(info.npcPrefab, info.spawnPoint.position, info.spawnPoint.rotation);
        var netObj = npc.GetComponent<NetworkObject>();
        netObj.Spawn();
        spawnedNPCs[info] = npc;

        // Assign patrol points if available
        var networkedNPC = npc.GetComponent<NetworkedNPC>();
        if (networkedNPC != null && info.patrolPoints != null)
            networkedNPC.PatrolPoints = info.patrolPoints;

        // Subscribe to death event
        var health = npc.GetComponent<Health>();
        if (health != null)
            health.OnDie += () => StartCoroutine(RespawnAfterDelay(info));
    }

    private IEnumerator RespawnAfterDelay(NPCSpawnInfo info)
    {
        yield return new WaitForSeconds(respawnDelay);

        // Clean up old reference if needed
        if (spawnedNPCs.ContainsKey(info))
            spawnedNPCs.Remove(info);

        SpawnAtInfo(info);
    }

    private IEnumerator RandomSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(randomSpawnInterval);

            if (activeRandomNPCs.Count >= maxRandomSpawns) { continue; }

            Vector2 pos = GetSpawnPosition();
            // Pick a random prefab from the list
            if (randomNpcPrefabs.Count == 0) continue;
            GameObject prefab = randomNpcPrefabs[Random.Range(0, randomNpcPrefabs.Count)];
            GameObject npc = Instantiate(prefab, pos, Quaternion.identity);
            var netObj = npc.GetComponent<NetworkObject>();
            netObj.Spawn();
            activeRandomNPCs.Add(npc);

            var health = npc.GetComponent<Health>();
            if (health != null)
            {
                health.OnDie += () => StartCoroutine(RemoveRandomNPCDelayed(npc));
            }
        }
    }

    private IEnumerator RemoveRandomNPCDelayed(GameObject npc)
    {
        yield return null;
        if (activeRandomNPCs.Contains(npc))
        {
            activeRandomNPCs.Remove(npc);
        }
    }

    private Vector2 GetSpawnPosition()
    {
        float x = 0;
        float y = 0;

        while (true)
        {
            x = Random.Range(randomX.x, randomX.y);
            y = Random.Range(randomY.x, randomY.y);

            Vector2 spawnPoint = new Vector2(x, y);
            bool isOccupied = Physics2D.OverlapCircle(spawnPoint, 0.5f) != null;
            if (!isOccupied)
            {
                return spawnPoint;
            }
        }
    }
}