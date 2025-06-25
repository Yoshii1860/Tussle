using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"Object {gameObject.name} will despawn after {lifetime} seconds.");
                StartCoroutine(DespawnAfterDelay(netObj));
            }
        }
        else
        {
            Debug.LogWarning($"NetworkObject not found or not spawned on {gameObject.name}. Destroying the GameObject instead.");
            Destroy(gameObject, lifetime);
            Debug.Log($"Object {gameObject.name} will be destroyed after {lifetime} seconds.");
        }
    }

    private IEnumerator DespawnAfterDelay(NetworkObject netObj)
    {
        Debug.Log($"DespawnAfterDelay: Waiting for {lifetime} seconds before despawning {netObj.name}");
        yield return new WaitForSeconds(lifetime);
        Debug.Log($"DespawnAfterDelay: Despawning {netObj.name}");
        netObj.Despawn();
        Debug.Log($"DespawnAfterDelay: {netObj.name} has been despawned.");
    }
}