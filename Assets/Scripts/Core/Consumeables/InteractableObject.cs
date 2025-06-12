using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public abstract class InteractableObject : NetworkBehaviour
{
    public event Action<InteractableObject> OnPickup; // Event to notify when an object is picked up
    [SerializeField] private ObjectType objectType; // Enum for object type
    [SerializeField] private string editorObjectName; // Editor-friendly string for input
    [SerializeField] private int value = 10;

    [HideInInspector] // Prevent rendering in Inspector to avoid "Type not renderable"
    public NetworkVariable<FixedString32Bytes> ObjectName;
    [HideInInspector] // Prevent rendering in Inspector to avoid "Type not renderable"
    public NetworkVariable<int> Value;
    [HideInInspector] // Prevent rendering in Inspector to avoid "Type not renderable"
    public NetworkVariable<bool> IsPickedUp;
    [HideInInspector] // Prevent rendering in Inspector to avoid "Type not renderable"
    public NetworkVariable<int> ObjectTypeId; // Sync the enum

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ObjectName.Value = new FixedString32Bytes(editorObjectName);
            Value.Value = value;
            ObjectTypeId.Value = (int)objectType;
            IsPickedUp.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PickupServerRpc(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("PickupServerRpc called on a client, but this should only run on the server.");
            return;
        }

        if (IsPickedUp.Value)
        {
            Debug.LogWarning("PickupServerRpc: Object is already picked up.");
            return;
        }

        OnPickup?.Invoke(this);
        IsPickedUp.Value = true;
        UseEffect(clientId); // Apply effect immediately
        DestroyClientRpc();
        Debug.Log($"Object {ObjectName.Value} (Type: {ObjectTypeId.Value}) picked up and used by Client {clientId}");
    }

    protected abstract void UseEffect(ulong clientId);

/*
    public void Drop(Vector3 position)
    {
        if (!IsServer)
        {
            return;
        }
        transform.position = position;
        IsPickedUp.Value = false;
        gameObject.SetActive(true);
        SetVisibilityClientRpc(true);
        Debug.Log($"Object {ObjectName.Value} (Type: {ObjectTypeId.Value}) dropped at {position}");
    }
*/

    [ClientRpc]
    private void DestroyClientRpc()
    {
        Debug.Log($"DestroyClientRpc called for {ObjectName.Value} - NetworkObjectId: {NetworkObjectId}");
        Destroy(gameObject);
    }

/*
    [ClientRpc]
    private void SetVisibilityClientRpc(bool isVisible)
    {
        gameObject.SetActive(isVisible);
        Debug.Log($"SetVisibilityClientRpc called with isVisible: {isVisible}");
    }
*/
}