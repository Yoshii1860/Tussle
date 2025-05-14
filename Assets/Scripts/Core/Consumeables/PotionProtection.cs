using UnityEngine;
using Unity.Netcode;

public class PotionProtection : InteractableObject
{
    [SerializeField] private float damageReductionMultiplier = 0.7f;
    [SerializeField] private float duration = 5f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<Health>(out Health health) &&
            playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager buffUIManager))
        {
            health.ApplyProtection(damageReductionMultiplier, duration);
            buffUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, duration);
            Debug.Log($"Client {clientId} received protection (damage reduced by {damageReductionMultiplier * 100}%) for {duration}s with {ObjectName.Value}");
        }
    }
}