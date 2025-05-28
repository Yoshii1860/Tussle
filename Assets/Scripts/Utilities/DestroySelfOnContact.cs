using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{
    [SerializeField] private TeamIndexStorage teamIndexStorage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (teamIndexStorage.TeamIndex != -1)
        {
            if (other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.TryGetComponent<Player>(out Player player))
            {
                if (player.TeamIndex.Value == teamIndexStorage.TeamIndex)
                {
                    return; // Ignore contact with teammates
                }
            }
        }

        Destroy(gameObject, 0.1f);
    }
}
