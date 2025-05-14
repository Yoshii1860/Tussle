using UnityEngine;
using Unity.Netcode;

public class PotionHealth : InteractableObject
{
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float buffDuration = 2f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<Health>(out Health health) && playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager playerUIManager))
        {
            health.Heal((int)healAmount);
            playerUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, buffDuration);
        }
    }
}