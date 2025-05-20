using System.Collections.Generic;
using UnityEngine;

public class Graveyard : MonoBehaviour
{
    private static List<Graveyard> graveyards = new List<Graveyard>();

    public static Graveyard GetNearestGraveyard(Vector3 position)
    {
        if (graveyards.Count == 0)
        {
            Debug.LogError("No graveyards available!");
            return null;
        }

        Graveyard nearestGraveyard = null;
        float shortestDistance = float.MaxValue;

        foreach (Graveyard graveyard in graveyards)
        {
            float distance = Vector3.Distance(position, graveyard.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestGraveyard = graveyard;
            }
        }

        return nearestGraveyard;
    }

    private void OnEnable()
    {
        graveyards.Add(this);
    }

    private void OnDisable()
    {
        graveyards.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1f);
    }
}