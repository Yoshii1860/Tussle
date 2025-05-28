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
        flagImage = flag.GetComponent<Image>();
        Debug.Log($"TeamColorDisplay: flagImage is {flagImage != null}");
        if (flagImage == null)
        {
            Debug.LogError("TeamColorDisplay: flag does not have an Image component!");
            return;
        }

        HandleTeamChanged(-1, player.TeamIndex.Value);
        player.TeamIndex.OnValueChanged += HandleTeamChanged;
    }

    private void OnDestroy()
    {
        player.TeamIndex.OnValueChanged -= HandleTeamChanged;
    }

    private void HandleTeamChanged(int previousValue, int newValue)
    {
        Debug.Log($"TeamColorDisplay: HandleTeamChanged called with previousValue={previousValue}, newValue={newValue}");
        Color teamColor = teamColorLookup.GetTeamColor(newValue);
        if (teamColor == Color.white)
        {
            Debug.LogWarning($"TeamColorDisplay: No color found for team index {newValue}, using default color.");
            flag.SetActive(false);
        }
        else
        {
            flagImage.color = teamColor;
        }
    }
}