using Unity.Netcode;
using UnityEngine;

public class AreaMusicTrigger : MonoBehaviour
{
    [SerializeField] private string areaName;

    private void Start()
    {
        if (string.IsNullOrEmpty(areaName))
        {
            areaName = gameObject.name;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            if (player.IsOwner)
            {
                AudioManager.Instance.PlayAreaMusic(areaName);
            }
        }
    }
}
