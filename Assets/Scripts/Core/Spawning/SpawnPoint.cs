using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPoint : MonoBehaviour
{
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    public static float[] GetRandomSpawnPos()
    {
        Debug.Log($"GetRandomSpawnPos: {spawnPoints.Count} spawn points available.");
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points available!");
            Debug.LogError($"Scene: {SceneManager.GetActiveScene().name}");
            return new float[] { 0, 0, 0 };
        }

        int randomIndex = Random.Range(0, spawnPoints.Count);
        return new float[]
        {
            spawnPoints[randomIndex].transform.position.x,
            spawnPoints[randomIndex].transform.position.y,
            spawnPoints[randomIndex].transform.position.z
        };
    }

    private void OnEnable()
    {
        spawnPoints.Add(this);
    }

    private void OnDisable()
    {
        spawnPoints.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1f);
    }
}
