using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Health : NetworkBehaviour
{
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();

    private bool isDead;
    private float protectionPercentage = 1;

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

    public void ApplyProtection(float protectionPercentage, float duration)
    {
        if (isDead) { return; }

        this.protectionPercentage = protectionPercentage;
        Invoke(nameof(RemoveProtection), duration);
    }

    private void RemoveProtection()
    {
        protectionPercentage = 1;
    }

    public void ApplyRegeneration(int regenerationAmount, float duration)
    {
        if (isDead) { return; }

        StartCoroutine(RegenerateHealth(regenerationAmount, duration));
    }

    private IEnumerator RegenerateHealth(int regenerationAmount, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration + 0.5f) // Adding 0.5f to ensure the last regeneration is applied
        {
            elapsedTime += Time.deltaTime;
            ModifyHealthServerRpc(regenerationAmount);
            yield return new WaitForSeconds(1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyHealthServerRpc(int value, ulong lastAttackerClientId = default)
    {
        if (isDead) { return; }

        int newHealth = CurrentHealth.Value + (int)(value * protectionPercentage);
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
