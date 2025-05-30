using UnityEngine;
using Unity.Netcode;

public class PotionInvisibility : InteractableObject
{
    [SerializeField] private float duration = 10f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<Player>(out Player player) && playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager playerUIManager))
        {
            player.Invisibility(duration);
            playerUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, duration);
        }
    }
}