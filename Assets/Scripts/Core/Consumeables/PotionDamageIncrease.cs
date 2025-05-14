using UnityEngine;
using Unity.Netcode;

public class PotionDamageIncrease : InteractableObject
{
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float duration = 10f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager buffUIManager))
        {
            DealMeleeDamageOnContact meleeScript = playerObj.GetComponentInChildren<DealMeleeDamageOnContact>();
            if (meleeScript != null)
            {
                meleeScript.DamageBoost(damageMultiplier, duration);
            }
            buffUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, duration);
        }
    }
}