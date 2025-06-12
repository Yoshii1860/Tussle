using UnityEngine;
using Unity.Netcode;

public class ExitHouse : MonoBehaviour
{
    [SerializeField] private HouseData houseData;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Player>(out Player player))
        {
            if (player.IsOwner && houseData != null)
            {
                RequestExitServerRpc(player.OwnerClientId);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestExitServerRpc(ulong clientId)
    {
        if (houseData != null)
        {
            houseData.PlayerRequestingExitServerRpc(clientId);
        }
    }
}