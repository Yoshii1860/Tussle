using UnityEngine;

public class Archer : Character
{
    protected override void PerformPrimaryAction()
    {
        Debug.Log("Archer: No Primary Action");
    }

    protected override void PerformSecondaryAction()
    {
        Debug.Log("Archer: Shoot Arrow");
        // Calculate projectile spawn position (e.g., at the bow)
        Vector3 spawnPosition = transform.position + (isFacingLeft.Value ? Vector3.right : Vector3.left) * 1f;
        Quaternion rotation = Quaternion.identity;
        SpawnProjectileServerRpc(spawnPosition, rotation);
    }
}