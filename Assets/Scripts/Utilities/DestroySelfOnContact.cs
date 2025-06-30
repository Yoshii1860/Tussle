using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{
    [SerializeField] private TeamIndexStorage teamIndexStorage;
    private const int NPCTeamIndex = -2; // Default value for NPCs
    private const int FFAIndex = -1; // Free-for-all index

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Destructable"))
        {
            if (teamIndexStorage.TeamIndex == NPCTeamIndex) { return; }
            Destroy(gameObject, 0.1f);
            return;
        }
        if (teamIndexStorage.TeamIndex != FFAIndex)
        {
            if (other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.TryGetComponent<Player>(out Player player))
            {
                if (player.TeamIndex.Value == teamIndexStorage.TeamIndex)
                {
                    Debug.Log($"DestroySelfOnContact: Ignoring contact with teammate {player.name} on team {player.TeamIndex.Value}");
                    return; // Ignore contact with teammates
                }
            }
        }

        if (other.attachedRigidbody.TryGetComponent<NetworkedNPC>(out NetworkedNPC npc))
        {
            if (npc.TeamIndex == teamIndexStorage.TeamIndex)
            {
                Debug.Log($"DestroySelfOnContact: Ignoring contact with NPC {npc.name} on team {npc.TeamIndex}");
                return; // Ignore contact with NPCs on the same team
            }
        }

        Debug.Log($"DestroySelfOnContact: Destroying {gameObject.name} on contact with {other.gameObject.name}");
        Destroy(gameObject, 0.1f);
    }
}
