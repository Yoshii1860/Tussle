using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KillCounter : NetworkBehaviour
{
    private static Dictionary<ulong, int> killsCache = new Dictionary<ulong, int>();
    private NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int Kills => kills.Value;

    public override void OnNetworkSpawn()
    {

#if UNITY_SERVER
        if (killsCache.TryGetValue(NetworkObject.OwnerClientId, out int cachedKills))
        {
            kills.Value = cachedKills;
            Debug.Log($"KillCounter: Loaded cached kills for client {NetworkObject.OwnerClientId}: {cachedKills}");
            Leaderboard.Instance.UpdateKills(NetworkObject.OwnerClientId, kills.Value);
        }
#endif

        if (IsClient && !IsOwner) return;
        // Only the owner initializes or updates locally, server syncs
    }

    public override void OnNetworkDespawn()
    {

#if UNITY_SERVER
            killsCache[NetworkObject.OwnerClientId] = kills.Value;
            Debug.Log($"KillCounter: Saved kills for client {NetworkObject.OwnerClientId}: {kills.Value}");
#endif

    }

    public void AddKill()
    {
        Debug.Log($"KillCounter: Adding a kill for client {NetworkObject.OwnerClientId}");
        kills.Value++;
        UpdateLeaderboard(kills.Value);
    }

    private void UpdateLeaderboard(int newKills)
    {
        Debug.Log($"KillCounter: Updating leaderboard for client {NetworkObject.OwnerClientId} with kills: {newKills}");
        Leaderboard.Instance.UpdateKills(NetworkObject.OwnerClientId, newKills);
    }
}