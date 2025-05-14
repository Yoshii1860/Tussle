using UnityEngine;
using Unity.Netcode;

public class PlayerPickup : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner || !other.TryGetComponent<InteractableObject>(out InteractableObject obj)) return;

        obj.PickupServerRpc(OwnerClientId);
    }
}