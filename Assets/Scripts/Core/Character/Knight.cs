using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
using Unity.Services.Matchmaker.Models;
using System;
using UnityEngine.LowLevel;



public class Knight : Character
{
    [Header("References")]
    [SerializeField] private Collider2D swordCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentAttackIndex.OnValueChanged += OnAttackIndexChanged;
        OnAttackIndexChanged(0, currentAttackIndex.Value);

        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
            inputReader.SecondaryAttackEvent += OnSecondaryAttack;
        }

        DealMeleeDamageOnContact dealMeleeDamageOnContact = swordCollider.GetComponent<DealMeleeDamageOnContact>();
        if (dealMeleeDamageOnContact != null)
        {
            dealMeleeDamageOnContact.SetOwner(OwnerClientId);
        }
        else
        {
            Debug.LogWarning("DealMeleeDamageOnContact component not found on swordCollider.");
        }

        swordCollider.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        currentAttackIndex.OnValueChanged -= OnAttackIndexChanged;

        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent -= OnPrimaryAttack;
            inputReader.SecondaryAttackEvent -= OnSecondaryAttack;
        }
    }

    private void OnAttackIndexChanged(int previous, int current)
    {
        currentAttack = attacks[current];
        CurrentAttack = currentAttack;
    }

    private void OnPrimaryAttack(bool isPressed)
    {
        if (!IsOwner ||
            EventSystem.current.IsPointerOverGameObject() ||
            !isPressed ||
            !CanPerformAttack())
        {
            return;
        }
        
        isAttacking.Value = true;
        Invoke(nameof(ResetAttack), currentAttack.cooldown);
        Debug.Log($"Knight: Attack Started - {currentAttack.name}");
    }

    private void OnSecondaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        isSecondaryAction.Value = isPressed;
        if (!isPressed)
        {
            Debug.Log("Knight: Block Released");
        }
        else
        {
            Debug.Log("Knight: Block Started");
        }
    }

    protected override void OnIsSecondaryActionChanged(bool previousValue, bool newValue)
    {
        animator.SetBool(secondaryAttack.animationTrigger, newValue);
    }

    public void EnableSwordCollider()
    {
        swordCollider.enabled = true;
    }

    public void DisableSwordCollider()
    {
        swordCollider.enabled = false;
    }

    public void AOEAttack()
    {
        if (IsOwner)
        {
            AOEAttackServerRpc();
        }
    }

    [ServerRpc]
    private void AOEAttackServerRpc()
    {
        SpawnAOEEffect();
        DealAOEDamage();
    }

    private void SpawnAOEEffect()
    {
        GameObject effectInstance = Instantiate(currentAttack.clientPrefab, transform.position, Quaternion.identity);
        var netObj = effectInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }

    private void DealAOEDamage()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, currentAttack.range, LayerMask.GetMask(PlayerLayerMask));
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.attachedRigidbody == null) continue;

            if (hitCollider.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
            {
                if (networkObject.OwnerClientId == OwnerClientId) continue; // Ignore self
            }

            int myTeam = GetComponent<Player>().TeamIndex.Value;
            if (myTeam != -1)
            {
                if (hitCollider.attachedRigidbody.TryGetComponent<Player>(out Player player))
                {
                    if (player.TeamIndex.Value == myTeam) continue; // Ignore teammates
                }
            }

            if (hitCollider.attachedRigidbody.TryGetComponent<Health>(out Health health))
            {
                Debug.Log($"Knight: AOE Attack - Dealing {currentAttack.damage} damage to {hitCollider.name}");
                health.TakeDamage(currentAttack.damage, OwnerClientId);
                // Optionally, you can add knockback or other effects here
            }
        }
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }
}