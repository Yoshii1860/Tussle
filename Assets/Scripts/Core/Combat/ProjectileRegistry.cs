using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Projectile Registry")]
public class ProjectileRegistry : ScriptableObject
{
    [System.Serializable]
    public struct ProjectileEntry
    {
        public string key;
        public GameObject serverPrefab;
        public GameObject clientPrefab;
    }

    public ProjectileEntry[] projectiles;

    public GameObject GetPrefab(string key, bool isServer)
    {
        foreach (var entry in projectiles)
        {
            if (entry.key == key)
            {
                return isServer ? entry.serverPrefab : entry.clientPrefab;
            }
        }
        Debug.LogError($"ProjectileRegistry: No prefab found for key '{key}'");
        return null;
    }
}