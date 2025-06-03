using UnityEngine;

[CreateAssetMenu(menuName = "Combat/ProjectileBehaviors/ArcherMultishot")]
public class ArcherMultishot : ProjectileBehavior
{
    public int arrowCount = 3;
    public float spreadAngle = 10f;
    public string projectileKey = "Arrow";

    public override void Launch(ProjectileLauncher launcher, Attack attack)
    {
        Vector2 baseDirection = launcher.ArrowDirection; // or use launcher.CalculateDirection()
        Vector2 spawnPos = launcher.ProjectileSpawnPoint.position; // expose this property if needed

        float startAngle = -spreadAngle * (arrowCount - 1) / 2f;

        for (int i = 0; i < arrowCount; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;

            // Fire on server (which will also call the client RPC for visuals)
            launcher.FireProjectileServer(spawnPos, direction.normalized, projectileKey);
        }
    }
}