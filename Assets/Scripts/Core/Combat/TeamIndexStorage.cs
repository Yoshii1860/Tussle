using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class TeamIndexStorage : MonoBehaviour
{
    public int TeamIndex { get; private set; }
    
    public void Initialize(int teamIndex)
    {
        TeamIndex = teamIndex;
    }
}
