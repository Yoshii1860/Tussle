using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;


public class Health : NetworkBehaviour
{
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();

    private bool isDead;

    public Action<Health> OnDie;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        CurrentHealth.Value = MaxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        ModifyHealthServerRpc(-damageAmount);
    }

    public void Heal(int healAmount)
    {
        ModifyHealthServerRpc(healAmount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHealthServerRpc(int value)
    {
        if (isDead) { return; }

        int newHealth = CurrentHealth.Value + value;
        CurrentHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentHealth.Value == 0)
        {
            OnDie?.Invoke(this);
            isDead = true;
        }
    }
}
