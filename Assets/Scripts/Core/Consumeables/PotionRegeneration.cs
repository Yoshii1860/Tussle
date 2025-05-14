using UnityEngine;
using Unity.Netcode;

public class PotionRegeneration : InteractableObject
{
    [SerializeField] private int healPerSecond = 5;
    [SerializeField] private float duration = 10f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<Health>(out Health health) &&
            playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager playerUIManager))
        {
            health.ApplyRegeneration(healPerSecond, duration);
            playerUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, duration);
        }
    }
}