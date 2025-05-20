using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class DestructableObject : NetworkBehaviour
{
    [SerializeField] private UnityEvent onDestroyActions;
    [SerializeField] private string triggeringTag = "Weapon";
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag(triggeringTag))
        {
            currentHealth--;
            if (currentHealth <= 0)
            {
                DestroyObjectServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyObjectServerRpc()
    {
        onDestroyActions.Invoke();
        DestroyObjectClientRpc();
        Destroy(gameObject);
    }

    [ClientRpc]
    private void DestroyObjectClientRpc()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}