using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class Priest : Character
{
    [SerializeField] private Collider2D staffCollider;
    [SerializeField] private ProjectileLauncher projectileLauncher;

    private Vector2 lastClickPosition;

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
        else {Debug.Log($"Priest: OnPrimaryAttack Passes TryCast");}

        isAttacking.Value = true;
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        lastClickPosition = worldPosition;
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

        isSecondaryAction.Value = true;
        Invoke(nameof(ResetSecondaryAttack), secondaryAttack.cooldown);
    }

    public void CastSpell()
    {
        if (projectileLauncher != null)
        {
            Debug.Log($"Priest: Casting Spell - {currentAttack.name}");
            projectileLauncher.HandleShot(currentAttack);
            Invoke(nameof(ResetAttack), currentAttack.cooldown);
        }
    }

    public void AOEAttack()
    {
        if (!IsOwner) { return; }
        AOEAttackServerRpc(transform.position);
        Invoke(nameof(ResetAttack), currentAttack.cooldown);
    }

    public void AOEDistantAttack()
    {
        if (!IsOwner) { return; }
        AOEAttackServerRpc(lastClickPosition, true);
        Invoke(nameof(ResetAttack), currentAttack.cooldown);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AOEAttackServerRpc(Vector2 position, bool isDistant = false)
    {
        SpawnAOEEffect(position);

        if (!isDistant)
        {
            DealAOEDamage();
        }
    }

    private void SpawnAOEEffect(Vector2 position)
    {
        GameObject aoeObject = Instantiate(currentAttack.serverPrefab, position, Quaternion.identity);
        if (aoeObject.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(GetComponent<Player>().TeamIndex.Value);
        }

        if (aoeObject.TryGetComponent<DealDamageOnContact>(out DealDamageOnContact dealDamageOnContact))
        {
            dealDamageOnContact.SetOwner(OwnerClientId);
        }

        SpawnAOEEffectClientRpc(position);
    }

    [ClientRpc]
    private void SpawnAOEEffectClientRpc(Vector2 position)
    {
        GameObject aoeObject = Instantiate(currentAttack.clientPrefab, position, Quaternion.identity);
        if (aoeObject.TryGetComponent<TeamIndexStorage>(out TeamIndexStorage teamIndexStorage))
        {
            teamIndexStorage.Initialize(GetComponent<Player>().TeamIndex.Value);
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

    private void ResetSecondaryAttack()
    {
        if (IsOwner)
        {
            isSecondaryAction.Value = false;
        }
    }
}