using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;


public class Thief : Character
{
    [SerializeField] private Collider2D swordCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentAttackIndex.OnValueChanged += OnAttackIndexChanged;
        OnAttackIndexChanged(0, currentAttackIndex.Value);

        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
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
        Debug.Log("Thief: Sword Attack");
    }

    public void EnableSwordCollider()
    {
        swordCollider.enabled = true;
    }

    public void DisableSwordCollider()
    {
        swordCollider.enabled = false;
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }
}