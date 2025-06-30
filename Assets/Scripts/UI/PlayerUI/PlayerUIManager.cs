using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerUIManager : NetworkBehaviour
{
    [SerializeField] private Transform buffHolder; // The UI panel or container to hold buff entities
    [SerializeField] private GameObject buffPrefab; // The BuffPrefabEntity prefab

    private Dictionary<ObjectType, BuffEntity> activeBuffs = new Dictionary<ObjectType, BuffEntity>();

    [ClientRpc]
    public void SpawnBuffClientRpc(ObjectType objectType, float duration)
    {
        Debug.Log($"PlayerUIManager: Spawning buff {objectType} with duration {duration} for Player {OwnerClientId}");
        if (activeBuffs.TryGetValue(objectType, out BuffEntity existingBuff) && existingBuff != null)
        {
            Debug.Log($"PlayerUIManager: Found existing buff for {objectType}, extending duration.");
            existingBuff.AddTime(duration);
        }
        else
        {
            Debug.Log($"PlayerUIManager: No existing buff found for {objectType}, creating a new one.");
            GameObject buffEntity = Instantiate(buffPrefab, buffHolder);
            BuffEntity buff = buffEntity.GetComponent<BuffEntity>();
            if (buff != null)
            {
                buff.Initialize(objectType, duration);
                activeBuffs[objectType] = buff;
            }
        }
    }

    [ClientRpc]
    public void RemoveBuffClientRpc(ObjectType objectType)
    {
        if (activeBuffs.TryGetValue(objectType, out BuffEntity buff))
        {
            Destroy(buff.gameObject);
            activeBuffs.Remove(objectType);
        }
    }

    private void Update()
    {
        // Clean up buffs that have been destroyed
        var toRemove = new List<ObjectType>();
        foreach (var kvp in activeBuffs)
        {
            if (kvp.Value == null)
                toRemove.Add(kvp.Key);
        }
        foreach (var key in toRemove)
            activeBuffs.Remove(key);
    }
}