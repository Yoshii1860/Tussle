using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
using Unity.Services.Matchmaker.Models;
using System;



public class Knight : Character
{
    [Header("References")]
    [SerializeField] private Collider2D swordCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
            inputReader.SecondaryAttackEvent += OnSecondaryAttack;
            inputReader.ChangeAttackEvent += OnAttackChange;

            OnAttackChange(0); // Set initial attack
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
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent -= OnPrimaryAttack;
            inputReader.SecondaryAttackEvent -= OnSecondaryAttack;
        }
    }

    private void OnAttackChange(int index)
    {
        currentAttack = attacks[index];
        CurrentAttack = currentAttack;
    }

    private void OnPrimaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (!isPressed) return;
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
        Debug.Log("Knight: AOE Attack Started");
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }
}