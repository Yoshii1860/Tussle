using UnityEngine;

public class Thief : Character
{
    protected override void PerformPrimaryAction()
    {
        Debug.Log("Thief: Attack with Daggers");
    }

    protected override void PerformSecondaryAction()
    {
        Debug.Log("Thief: No Secondary Action");
    }
}