using UnityEngine;
using Unity.Netcode;

public class PotionSpeed : InteractableObject
{
    [SerializeField] private float speedMultiplier = 1.25f;
    [SerializeField] private float duration = 10f;

    protected override void UseEffect(ulong clientId)
    {
        if (!IsServer) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj.TryGetComponent<Character>(out Character characterScript) &&
            playerObj.TryGetComponent<PlayerUIManager>(out PlayerUIManager buffUIManager))
        {
            characterScript.MovementBoost(speedMultiplier, duration);
            buffUIManager.SpawnBuffClientRpc((ObjectType)ObjectTypeId.Value, duration);
        }
    }
}