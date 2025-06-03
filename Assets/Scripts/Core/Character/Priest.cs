using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;


public class Priest : Character
{
    [SerializeField] private Collider2D staffCollider;
    [SerializeField] private ProjectileLauncher projectileLauncher;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
            inputReader.SecondaryAttackEvent += OnSecondaryAttack;
        }

        DealMeleeDamageOnContact dealMeleeDamageOnContact = staffCollider.GetComponent<DealMeleeDamageOnContact>();
        if (dealMeleeDamageOnContact != null)
        {
            dealMeleeDamageOnContact.SetOwner(OwnerClientId);
        }
        else
        {
            Debug.LogWarning("DealMeleeDamageOnContact component not found on staffCollider.");
        }

        staffCollider.enabled = false;
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

    private void OnPrimaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        isAttacking.Value = true;
        animator.SetTrigger("Attack");
        Invoke(nameof(ResetAttack), 0.4f);
        Debug.Log("Priest: Staff Attack");
    }

    private void OnSecondaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        if (isPressed)
        {
            isAttacking.Value = true;
            Debug.Log("Archer: Shoot Arrow");
        }
    }

    public void CastSpell()
    {
        if (projectileLauncher != null)
        {
            projectileLauncher.HandleShot(true, currentAttack);
            Invoke(nameof(ResetAttack), 0.2f);
        }
    }

    public void EnableStaffCollider()
    {
        staffCollider.enabled = true;
    }

    public void DisableStaffCollider()
    {
        staffCollider.enabled = false;
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }
}