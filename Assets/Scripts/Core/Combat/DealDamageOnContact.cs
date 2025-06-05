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

    private ulong ownerClientId;
    private Dictionary<Health, Coroutine> activeDamageOverTimeCoroutines = new Dictionary<Health, Coroutine>();

    public void SetOwner(ulong ownerClientId)
    {
        this.ownerClientId = ownerClientId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"DealDamageOnContact: OnTriggerEnter2D with {other.name}");
        if (other.attachedRigidbody == null) { return; }

        if (teamIndexStorage.TeamIndex != -1)
        {
            if (other.attachedRigidbody.TryGetComponent<Player>(out Player player))
            {
                if (player.TeamIndex.Value == teamIndexStorage.TeamIndex)
                {
                    Debug.Log($"DealDamageOnContact: Ignoring contact with teammate {player.name} on team {player.TeamIndex.Value}");
                    return;
                }
            }
        }

        if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            if (netObj.OwnerClientId == ownerClientId)
            {
                // Ignore self
                return;
            }
        }

        if (other.attachedRigidbody.TryGetComponent<Health>(out Health health))
        {
            Debug.Log($"DealDamageOnContact: Dealing {DamageAmount} damage to Health component on {other.name}");
            health.TakeDamage(DamageAmount, ownerClientId);

            if (isDamageOverTime)
            {
                if (!isDamageWhileInContact)
                {
                    Debug.Log($"DealDamageOnContact: Starting damage over time on {other.name} while in contact");
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
                Debug.Log($"DealDamageOnContact: Stopping damage over time on {health.name} due to exit contact");
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
