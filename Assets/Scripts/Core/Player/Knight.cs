using UnityEngine;
using Unity.Netcode;


public class Knight : Character
{
    [SerializeField] private Collider2D swordCollider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent -= OnPrimaryAttack;
            inputReader.SecondaryAttackEvent -= OnSecondaryAttack;
        }
    }

    private void OnPrimaryAttack()
    {
        if (!IsOwner) return;
        isAttacking.Value = true;
        animator.SetTrigger("Attack");
        Invoke(nameof(ResetAttack), 0.4f);
        Debug.Log("Knight: Sword Attack");
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

    protected override void OnIsAttackingChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            animator.SetTrigger("Attack");
        }
    }

    protected override void OnIsSecondaryActionChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("Secondary", newValue);
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