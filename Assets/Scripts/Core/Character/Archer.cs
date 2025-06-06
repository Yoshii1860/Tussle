using UnityEngine;
using UnityEngine.EventSystems;

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
        if (!IsOwner ||
            EventSystem.current.IsPointerOverGameObject() ||
            !isPressed ||
            !CanPerformAttack())
        {
            return;
        }

        if (!secondStat.TryCast(currentAttack.secondStatCost)) { return; }

        isAttacking.Value = true;
        Invoke(nameof(ResetAttack), currentAttack.cooldown);
    }

    private void OnSecondaryAttack(bool isPressed)
    {
        if (!IsOwner ||
            EventSystem.current.IsPointerOverGameObject() ||
            !isPressed ||
            !CanPerformAttack())
        {
            return;
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