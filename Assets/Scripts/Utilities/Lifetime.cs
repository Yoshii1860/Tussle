using Unity.Netcode;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(DespawnAfterDelay(netObj));
            }
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

    private System.Collections.IEnumerator DespawnAfterDelay(NetworkObject netObj)
    {
        yield return new WaitForSeconds(lifetime);
        netObj.Despawn();
    }
}