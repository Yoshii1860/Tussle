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
        if (activeBuffs.TryGetValue(objectType, out BuffEntity existingBuff) && existingBuff != null)
        {
            existingBuff.AddTime(duration);
        }
        else
        {
            GameObject buffEntity = Instantiate(buffPrefab, buffHolder);
            BuffEntity buff = buffEntity.GetComponent<BuffEntity>();
            if (buff != null)
            {
                buff.Initialize(objectType, duration);
                activeBuffs[objectType] = buff;
            }
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