using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Combat/Attack")]
public class Attack : ScriptableObject
{
    public string attackName;
    public int damage;
    public float cooldown;
    public float range;
    public int secondStatCost;
    public int staminaCost;
    public string animationTrigger;
    public bool isTriggerBool;
    public Sprite icon;
    public GameObject serverPrefab;
    public GameObject clientPrefab;
    public ProjectileBehavior projectileBehavior;
}