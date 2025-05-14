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

    public void TakeDamage(int damageAmount, ulong attackerClientId)
    {
        ModifyHealthServerRpc(-damageAmount, attackerClientId);
    }

    public void Heal(int healAmount)
    {
        ModifyHealthServerRpc(healAmount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHealthServerRpc(int value, ulong lastAttackerClientId = default)
    {
        if (isDead) { return; }

        int newHealth = CurrentHealth.Value + value;
        CurrentHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentHealth.Value == 0)
        {
            OnDie?.Invoke(this);
            isDead = true;

            NetworkServer.Instance.AddKill(lastAttackerClientId);
        }
    }

    [ClientRpc]
    public void PlayDeathAnimationClientRpc()
    {
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("Die");
    }
}
