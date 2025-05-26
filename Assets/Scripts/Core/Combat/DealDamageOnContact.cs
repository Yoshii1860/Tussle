using UnityEngine;
using Unity.Netcode;

public class DealDamageOnContact : MonoBehaviour
{
    [SerializeField] public int DamageAmount = 15;

    private ulong ownerClientId;

    public void SetOwner(ulong ownerClientId)
    {
        this.ownerClientId = ownerClientId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"DealDamageOnContact: OnTriggerEnter2D with {other.name}");
        if (other.attachedRigidbody == null) { return; }
        if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            Debug.Log($"DealDamageOnContact: NetworkObject found with OwnerClientId {networkObject.OwnerClientId} and local ClientId {NetworkManager.Singleton.LocalClientId}");
            if (networkObject.OwnerClientId == ownerClientId)
            {
                return;
            }
        }

        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            Debug.Log($"DealDamageOnContact: Dealing {DamageAmount} damage to Health component on {other.name}");
            health.TakeDamage(DamageAmount, ownerClientId);
        }
    }
}
