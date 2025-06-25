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
        Debug.Log($"AreaMusicTrigger: {gameObject.name} entered by {other.gameObject.name}");
        if (other.TryGetComponent<Player>(out Player player))
        {
            Debug.Log($"AreaMusicTrigger: {areaName} triggered by player {player.name}");
            AudioManager.Instance.PlayAreaMusic(areaName);
        }
    }
}
