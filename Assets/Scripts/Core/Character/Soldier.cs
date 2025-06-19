using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;


public class Soldier : Character
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
        
        if (!secondStat.TryCast(currentAttack.secondStatCost)) { return; }
        
        isAttacking.Value = true;
        Invoke(nameof(ResetAttack), currentAttack.cooldown);
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
            Invoke(nameof(ResetAttack), currentAttack.cooldown);
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
        effectInstance.transform.localRotation = Quaternion.Euler(0, IsFacingLeft ? 0 : -180, effectInstance.transform.localRotation.z);
        var netObj = effectInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
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