using UnityEngine;

public class Archer : Character
{
    [SerializeField] private ProjectileLauncher projectileLauncher;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentAttackIndex.OnValueChanged += OnAttackIndexChanged;
        OnAttackIndexChanged(0, currentAttackIndex.Value);

        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent += OnPrimaryAttack;
            inputReader.SecondaryAttackEvent += OnSecondaryAttack;
            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponentInChildren<ProjectileLauncher>();
                if (projectileLauncher == null)
                {
                    Debug.LogWarning("ProjectileLauncher not found on Archer!");
                }
            }
        }
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
        if (!IsOwner) return;
        if (isPressed)
        {
            isAttacking.Value = true;
        }
    }

    private void OnSecondaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
    }

    protected override void OnIsAttackingChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"OnIsAttackingChanged: previousValue={previousValue}, newValue={newValue}");
        if (newValue && !previousValue)
        {
            animator.SetTrigger(currentAttack.animationTrigger);
        }
    }

    public void Shoot()
    {
        if (projectileLauncher != null)
        {
            projectileLauncher.HandleShot(currentAttack);
            Invoke(nameof(ResetAttack), currentAttack.cooldown);
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