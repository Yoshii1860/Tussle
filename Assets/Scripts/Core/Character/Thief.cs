using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;


public class Thief : Character
{
    [SerializeField] private Collider2D swordCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
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