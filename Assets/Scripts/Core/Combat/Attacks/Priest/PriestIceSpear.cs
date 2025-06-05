using UnityEngine;

[CreateAssetMenu(menuName = "Combat/ProjectileBehaviors/PriestIceSpear")]
public class PriestIceSpear : ProjectileBehavior
{
    public string projectileKey = "IceSpear";

    public override void Launch(ProjectileLauncher launcher, Attack attack)
    {
        launcher.FireProjectileServer(launcher.ProjectileSpawnPoint.position, launcher.ArrowDirection, projectileKey);
    }
}