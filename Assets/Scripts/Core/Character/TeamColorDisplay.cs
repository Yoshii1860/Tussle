using UnityEngine;
using UnityEngine.UI;

public class TeamColorDisplay : MonoBehaviour
{
    [SerializeField] private TeamColorLookup teamColorLookup;
    [SerializeField] private Player player;
    [SerializeField] private GameObject flag;

    private Image flagImage;

    private void Start()
    {
        if (IsClient())
        {
            flagImage = flag.GetComponent<Image>();
            if (flagImage == null)
            {
                Debug.LogWarning("TeamColorDisplay: flag does not have an Image component! for player " + player.name);
                return;
            }

            HandleTeamChanged(-1, player.TeamIndex.Value);
        }

        player.TeamIndex.OnValueChanged += HandleTeamChanged;
    }

    private void OnDestroy()
    {
        player.TeamIndex.OnValueChanged -= HandleTeamChanged;
    }

    private void HandleTeamChanged(int previousValue, int newValue)
    {
        if (!IsClient()) { return; } 

        Debug.Log($"TeamColorDisplay: HandleTeamChanged called with previousValue={previousValue}, newValue={newValue} for player {player.name}");
        Color teamColor = teamColorLookup.GetTeamColor(newValue);
        if (teamColor == Color.white)
        {
            Debug.LogWarning($"TeamColorDisplay: No color found for team index {newValue}, using default color for player {player.name}");
            flag.SetActive(false);
        }
        else
        {
            Debug.Log($"TeamColorDisplay: Setting flag color to {teamColor} for team index {newValue} of player {player.name}");
            flagImage.color = teamColor;
        }
    }

    private bool IsClient()
    {
#if UNITY_SERVER
        return false;
#endif
        return true;
    }
}