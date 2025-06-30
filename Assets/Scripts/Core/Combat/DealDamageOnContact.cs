using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private TeamIndexStorage teamIndexStorage;
    [SerializeField] public int DamageAmount = 15;
    [Space(10)]

    [Header("Damage Over Time Settings")]
    [SerializeField] private bool isDamageOverTime = false;
    [SerializeField] private bool isDamageWhileInContact = false;
    [SerializeField] private float damageDuration = 0;
    [SerializeField] private float damageInterval = 0;
    [SerializeField] private int damageOverTime = 0;

    private Vector2 initialVelocity;
    private ulong ownerClientId;
    private Dictionary<Health, Coroutine> activeDamageOverTimeCoroutines = new Dictionary<Health, Coroutine>();

    private const int NPCTeamIndex = -2;
    private const int FFAIndex = -1; // Free-for-all index

    public void SetOwner(ulong ownerClientId)
    {
        this.ownerClientId = ownerClientId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Destructable"))
        {
            if (other.transform.root.TryGetComponent<PropHealth>(out PropHealth propHealth))
            {
                if (teamIndexStorage.TeamIndex == NPCTeamIndex) { return; }

                propHealth.TakeDamage(DamageAmount);
                return;
            }
        }

        if (other.attachedRigidbody == null) { return; }

        if (teamIndexStorage != null && teamIndexStorage.TeamIndex != FFAIndex)
        {

            if (other.attachedRigidbody.TryGetComponent<Player>(out Player player))
            {
                if (player.TeamIndex.Value == teamIndexStorage.TeamIndex)
                {
                    return;
                }
            }
            else if (other.attachedRigidbody.TryGetComponent<NetworkedNPC>(out NetworkedNPC npc))
            {
                if (npc.TeamIndex == teamIndexStorage.TeamIndex)
                {
                    return;
                }
            }
        }

        if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            if (!other.attachedRigidbody.GetComponent<NetworkedNPC>() && !NetworkManager.Singleton.IsHost)
            {
                if (netObj.OwnerClientId == ownerClientId)
                {
                    return;
                }
            }
        }

        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            health.TakeDamage(DamageAmount, ownerClientId);

            if (isDamageOverTime)
            {
                if (!isDamageWhileInContact)
                {
                    health.TakeDamageOverTime(damageOverTime, damageDuration, damageInterval, ownerClientId);
                }
                else
                {
                    // If not in contact, we can start the damage over time effect immediately
                    Coroutine dotCoroutine = StartCoroutine(StartDamageOverTime(health));
                    activeDamageOverTimeCoroutines.Add(health, dotCoroutine);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isDamageOverTime && isDamageWhileInContact)
        {
            if (collision.attachedRigidbody == null) { return; }

            if (collision.attachedRigidbody.TryGetComponent<Health>(out Health health))
            {
                if (activeDamageOverTimeCoroutines.TryGetValue(health, out Coroutine dotCoroutine))
                {
                    StopCoroutine(dotCoroutine);
                    activeDamageOverTimeCoroutines.Remove(health);
                }
            }
        }
    }

    private IEnumerator StartDamageOverTime(Health health)
    {
        while (true)
        {
            health.TakeDamage(DamageAmount, ownerClientId);
            yield return new WaitForSeconds(damageInterval);
        }
    }
}
