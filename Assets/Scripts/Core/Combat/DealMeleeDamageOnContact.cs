using UnityEngine;
using Unity.Netcode;
using System;

public class DealMeleeDamageOnContact : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Character character;
    [SerializeField] private TeamIndexStorage teamIndexStorage;
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageCooldown = 0.2f;

    private ulong ownerClientId;
    private float lastDamageTime;
    private bool hasDealtDamageThisFrame;
    private NetworkObject parentNetworkObject;

    private int damageOnStart;
    private const int NPCTeamIndex = -2; // Default value for NPCs
    private const int FFAIndex = -1; // Free-for-all index

    private void Start()
    {
        if (teamIndexStorage == null)
        {
            Debug.LogWarning("TeamIndexStorage is not assigned in DealMeleeDamageOnContact!");
            return;
        }
        int teamIndex = player != null ? player.TeamIndex.Value : NPCTeamIndex;
        teamIndexStorage.Initialize(teamIndex);
        damageOnStart = damageAmount;
    }

    public void SetOwner(ulong ownerClientId)
    {
        this.ownerClientId = ownerClientId;
        parentNetworkObject = GetComponentInParent<NetworkObject>();
        if (parentNetworkObject == null)
        {
            Debug.LogWarning("No NetworkObject found in parent hierarchy for DealMeleeDamageOnContact!");
        }
    }

    public void DamageBoost(float damageMultiplier, float duration)
    {
        damageAmount = (int)(damageOnStart * damageMultiplier);
        Invoke(nameof(ResetDamage), duration);
    }

    private object ResetDamage()
    {
        damageAmount = damageOnStart;
        return null;
    }

    private void Update()
    {
        hasDealtDamageThisFrame = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log($"DealMeleeDamageOnContact: OnTriggerEnter2D with {other.name}");
        if (Time.time - lastDamageTime < damageCooldown || hasDealtDamageThisFrame) return;

        if (character != null)
        {
            damageAmount = character.CurrentAttack.damage;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Destructable"))
        {
            if (other.transform.root.TryGetComponent<PropHealth>(out PropHealth propHealth))
            {
                if (teamIndexStorage.TeamIndex == NPCTeamIndex) { return; }

                propHealth.TakeDamage(damageAmount);
                return;
            }
        }

        if (other.attachedRigidbody == null) return;
        if (other.gameObject == transform.root.gameObject) return; // Ignore self

        if (teamIndexStorage != null && teamIndexStorage.TeamIndex != FFAIndex)
        {
            Debug.Log($"DealDamageOnContact: Checking team index for {other.name} with team index {teamIndexStorage.TeamIndex}");

            if (other.attachedRigidbody.TryGetComponent<Player>(out Player player))
            {
                if (player.TeamIndex.Value == teamIndexStorage.TeamIndex)
                {
                    Debug.Log($"DealDamageOnContact: Ignoring contact with teammate {player.name} on team {player.TeamIndex.Value}");
                    return;
                }
            }
            else if (other.attachedRigidbody.TryGetComponent<NetworkedNPC>(out NetworkedNPC npc))
            {
                if (npc.TeamIndex == teamIndexStorage.TeamIndex)
                {
                    Debug.Log($"DealDamageOnContact: Ignoring contact with NPC {npc.name} on team {npc.TeamIndex}");
                    return;
                }
            }
        }

        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            Debug.Log($"DealMeleeDamageOnContact: Dealing {damageAmount} damage to {other.name}");
            health.TakeDamage(damageAmount, ownerClientId);
            lastDamageTime = Time.time;
            hasDealtDamageThisFrame = true;
        }
    }
}