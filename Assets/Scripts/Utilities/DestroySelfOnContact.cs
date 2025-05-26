using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject, 0.1f);
    }
}
