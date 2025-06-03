using UnityEngine;

[CreateAssetMenu(menuName = "Combat/ProjectileBehaviors/ArcherSingleShot")]
public class ArcherSingleShot : ProjectileBehavior
{
    public string projectileKey = "Arrow";

    public override void Launch(ProjectileLauncher launcher, Attack attack)
    {
        launcher.FireProjectileServer(launcher.ProjectileSpawnPoint.position, launcher.ArrowDirection, projectileKey);
    }
}