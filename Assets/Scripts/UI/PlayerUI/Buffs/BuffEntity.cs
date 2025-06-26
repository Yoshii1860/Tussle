using UnityEngine;
using UnityEngine.UI;

public class BuffEntity : MonoBehaviour
{
    [SerializeField] private Image buffImage; // The UI Image component to display the buff icon
    [SerializeField] private Sprite[] buffSprites; // Array of sprites for each ObjectType, assigned in the Inspector
    private float duration; // Duration of the buff
    private float remainingTime; // Remaining time for the buff
    private bool isPermanent = false; // Whether the buff is permanent (duration = 0)

    private ObjectType objectType;

    public void Initialize(ObjectType objectType, float duration)
    {
        this.objectType = objectType;
        if (duration == 0)
        {
            isPermanent = true;
        }
        else
        this.duration = duration;
        this.remainingTime = duration;

        // Set the sprite based on the ObjectType
        int enumIndex = (int)objectType;
        if (enumIndex >= 0 && enumIndex < buffSprites.Length && buffSprites[enumIndex] != null)
        {
            buffImage.sprite = buffSprites[enumIndex];
        }
    }

    private void Update()
    {
        if (!isPermanent && remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            // Optional: Update a UI timer or progress bar (e.g., buffImage.fillAmount = remainingTime / duration)
        }
        else if (!isPermanent && remainingTime <= 0)
        {
            Destroy(gameObject); // Despawn the UI element when the buff expires
        }
    }

    public void AddTime(float extraTime)
    {
        remainingTime += extraTime;
        duration += extraTime;
    }

    public ObjectType GetObjectType() => objectType;
}