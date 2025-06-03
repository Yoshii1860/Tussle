using UnityEngine;

public abstract class ProjectileBehavior : ScriptableObject
{
    public abstract void Launch(ProjectileLauncher launcher, Attack attack);
}