using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Combat/Attack")]
public class Attack : ScriptableObject
{
    public string attackName;
    public int damage;
    public float cooldown;
    public float range;
    public string animationTrigger;
    public bool isTriggerBool;
    public Sprite icon;
    public ProjectileBehavior projectileBehavior;
}