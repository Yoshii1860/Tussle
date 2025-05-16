using Unity.Netcode;
using UnityEngine;

public class KillCounter : NetworkBehaviour
{
    private NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int Kills => kills.Value;

    public override void OnNetworkSpawn()
    {
        if (IsClient && !IsOwner) return;
        // Only the owner initializes or updates locally, server syncs
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddKillServerRpc()
    {
        kills.Value++;
        UpdateLeaderboardServerRpc(kills.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLeaderboardServerRpc(int newKills)
    {
        Leaderboard.Instance.UpdateKills(NetworkObject.OwnerClientId, newKills);
    }
}