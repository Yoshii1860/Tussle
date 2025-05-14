using UnityEngine;
using Unity.Netcode;
using System;

public class DealMeleeDamageOnContact : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageCooldown = 0.2f;

    private ulong ownerClientId;
    private float lastDamageTime;
    private bool hasDealtDamageThisFrame;
    private NetworkObject parentNetworkObject;

    private int damageOnStart;

    private void Start()
    {
        damageOnStart = damageAmount;
    }

    public void SetOwner(ulong ownerClientId)
    {
        this.ownerClientId = ownerClientId;
        parentNetworkObject = GetComponentInParent<NetworkObject>();
        if (parentNetworkObject == null)
        {
            Debug.LogWarning("No NetworkObject found in parent hierarchy for DealMeleeDamageOnContact!");
        }
    }

    public void DamageBoost(float damageMultiplier, float duration)
    {
        damageAmount = (int)(damageOnStart * damageMultiplier);
        Invoke(nameof(ResetDamage), duration);
    }

    private object ResetDamage()
    {
        damageAmount = damageOnStart;
        return null;
    }

    private void Update()
    {
        hasDealtDamageThisFrame = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (parentNetworkObject == null || !parentNetworkObject.IsOwner) return;
        if (Time.time - lastDamageTime < damageCooldown || hasDealtDamageThisFrame) return;
        if (other.attachedRigidbody == null) return;
        if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            if (networkObject.OwnerClientId == ownerClientId) return;
        }

        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            health.TakeDamage(damageAmount, ownerClientId);
            lastDamageTime = Time.time;
            hasDealtDamageThisFrame = true;
        }
    }
}