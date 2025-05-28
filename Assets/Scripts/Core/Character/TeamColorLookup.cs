using UnityEngine;

[CreateAssetMenu(fileName = "NewTeamColorLookup", menuName = "Team Color Lookup")]
public class TeamColorLookup : ScriptableObject
{
    [SerializeField] private Color[] teamColors;

    public Color GetTeamColor(int teamIndex)
    {
        if (teamIndex < 0 || teamIndex >= teamColors.Length)
        {
            Debug.LogWarning($"Invalid team index: {teamIndex}. Returning default color.");
            return Color.white;
        }
        else
        {
            return teamColors[teamIndex];
        }
    }
}
