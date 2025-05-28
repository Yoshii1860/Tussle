using UnityEngine;

public class SpawnOnDestroy : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    private void OnDestroy()
    {
        if (prefab != null && gameObject.scene.isLoaded)
        {
            Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }
}
