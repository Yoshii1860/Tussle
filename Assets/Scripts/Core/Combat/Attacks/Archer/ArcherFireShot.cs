using UnityEngine;

[CreateAssetMenu(menuName = "Combat/ProjectileBehaviors/ArcherFireShot")]
public class ArcherFireShot : ProjectileBehavior
{
    public string projectileKey = "FireArrow";

    public override void Launch(ProjectileLauncher launcher, Attack attack)
    {
        launcher.FireProjectileServer(launcher.ProjectileSpawnPoint.position, launcher.ArrowDirection, projectileKey);
    }
}