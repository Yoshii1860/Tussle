using UnityEngine;

public class Archer : Character
{
    [SerializeField] private ProjectileLauncher projectileLauncher;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
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
        if (IsOwner && inputReader != null)
        {
            inputReader.PrimaryAttackEvent -= OnPrimaryAttack;
            inputReader.SecondaryAttackEvent -= OnSecondaryAttack;
        }
    }

    private void OnPrimaryAttack()
    {
        if (!IsOwner) return;
    }

    private void OnSecondaryAttack(bool isPressed)
    {
        if (!IsOwner) return;
        if (isPressed)
        {
            isSecondaryTrigger.Value = true;
        }
    }

    protected override void OnIsSecondaryTriggerChanged(bool previousValue, bool newValue)
    {
        if (newValue && !previousValue)
        {
            animator.SetTrigger("SecondaryRelease");
        }
    }

    public void ShootArrow()
    {
        if (projectileLauncher != null)
        {
            projectileLauncher.HandleSecondaryAttack(true);
            Invoke(nameof(ResetSecondaryAttack), 0.2f);
        }
    }

    private void ResetAttack()
    {
        if (IsOwner)
        {
            isAttacking.Value = false;
        }
    }

    private void ResetSecondaryAttack()
    {
        if (IsOwner)
        {
            isSecondaryTrigger.Value = false;
        }
    }
}