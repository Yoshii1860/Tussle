using UnityEngine;
using Unity.Netcode;
using System;

public class PropHealth : NetworkBehaviour
{
    [field: SerializeField] public int MaxHealth { get; private set; } = 10;
    public NetworkVariable<int> CurrentPropHealth = new NetworkVariable<int>();
    [SerializeField] private GameObject particlePrefab;
    public Action OnDestroyed;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        CurrentPropHealth.Value = MaxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        ModifyPropHealth(-damageAmount);
    }

    private void ModifyPropHealth(int value)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Health: ModifyHealth called on client, but should only be called on server.");
            return;
        }

        int newHealth = CurrentPropHealth.Value + value;
        CurrentPropHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentPropHealth.Value <= 0)
        {
            OnDestroyed?.Invoke();
            DestroyObjectServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyObjectServerRpc()
    {
        DestroyObjectClientRpc();
        gameObject.GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void DestroyObjectClientRpc()
    {
        Instantiate(particlePrefab, transform.position, Quaternion.identity);
    }
}
