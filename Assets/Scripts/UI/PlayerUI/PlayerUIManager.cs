using UnityEngine;
using Unity.Netcode;

public class PlayerUIManager : NetworkBehaviour
{
    [SerializeField] private Transform buffHolder; // The UI panel or container to hold buff entities
    [SerializeField] private GameObject buffPrefab; // The BuffPrefabEntity prefab

    [ClientRpc]
    public void SpawnBuffClientRpc(ObjectType objectType, float duration)
    {
        // This runs on all clients to spawn the UI element
        GameObject buffEntity = Instantiate(buffPrefab, buffHolder);
        BuffEntity buff = buffEntity.GetComponent<BuffEntity>();
        if (buff != null)
        {
            buff.Initialize(objectType, duration);
        }
    }
}