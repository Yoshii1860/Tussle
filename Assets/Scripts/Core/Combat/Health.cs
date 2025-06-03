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
        Debug.Log($"Health: Taking damage: {damageAmount} from attacker: {attackerClientId}");
        ModifyHealth(-damageAmount, attackerClientId);
    }

    public void TakeDamageOverTime(int damageAmount, float duration, float interval, ulong attackerClientId)
    {
        if (isDead) { return; }

        StartCoroutine(DamageOverTime(damageAmount, duration, interval, attackerClientId));
    }

    private IEnumerator DamageOverTime(int damageAmount, float duration, float interval, ulong attackerClientId)
    {
        if (isDead) { yield break; }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            ModifyHealth(-damageAmount, attackerClientId);
            yield return new WaitForSeconds(interval);
            elapsedTime += interval;
        }
    }

    public void Heal(int healAmount)
    {
        ModifyHealth(healAmount);
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
            ModifyHealth(regenerationAmount);
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;
        }
    }

    private void ModifyHealth(int value, ulong lastAttackerClientId = default)
    {
        if (isDead) { return; }

        if (!IsServer)
        {
            Debug.LogWarning("Health: ModifyHealth called on client, but should only be called on server.");
            return;
        }

        Debug.Log($"Health: Modifying health by {value}. Current health: {CurrentHealth.Value}, Max health: {MaxHealth}");
        int newHealth = CurrentHealth.Value + (int)(value * protectionPercentage);
        CurrentHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);

        if (CurrentHealth.Value == 0)
        {
            Debug.Log($"Health: Player died. Last attacker: {lastAttackerClientId}");
            OnDie?.Invoke(this);
            isDead = true;

            UpdateKillsOnCounter(lastAttackerClientId);
        }
    }

    private void UpdateKillsOnCounter(ulong lastAttackerClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Health: UpdateKillsOnCounter called on client, but should only be called on server.");
            return;
        }

        Debug.Log($"Health: Updating kills on counter for attacker: {lastAttackerClientId}");
        if (lastAttackerClientId != default &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerClientId, out Unity.Netcode.NetworkClient client))
        {
            Debug.Log($"Health: Attacker found: {client.PlayerObject.name}");
            NetworkObject attackerNetworkObject = client.PlayerObject;
            if (attackerNetworkObject != null &&
                attackerNetworkObject.TryGetComponent<KillCounter>(out KillCounter attackerKillCounter))
            {
                Debug.Log($"Health: Incrementing kill counter for attacker: {attackerNetworkObject.name}");
                attackerKillCounter.AddKill();
            }
        }
        else if (lastAttackerClientId == NetworkManager.Singleton.LocalClientId)
        {
            NetworkObject attackerNetworkObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (attackerNetworkObject != null &&
                attackerNetworkObject.TryGetComponent<KillCounter>(out KillCounter attackerKillCounter))
            {
                Debug.Log($"Health: Incrementing kill counter for local player: {attackerNetworkObject.name}");
                attackerKillCounter.AddKill();
            }
        }
    }

    [ClientRpc]
    public void PlayDeathAnimationClientRpc()
    {
        Animator animator = GetComponent<Animator>();
        animator.SetTrigger("Die");
    }
}
