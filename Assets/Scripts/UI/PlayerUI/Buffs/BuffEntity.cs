using UnityEngine;
using UnityEngine.UI;

public class BuffEntity : MonoBehaviour
{
    [SerializeField] private Image buffImage; // The UI Image component to display the buff icon
    [SerializeField] private Sprite[] buffSprites; // Array of sprites for each ObjectType, assigned in the Inspector
    private float duration; // Duration of the buff
    private float remainingTime; // Remaining time for the buff

    public void Initialize(ObjectType objectType, float duration)
    {
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
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            // Optional: Update a UI timer or progress bar (e.g., buffImage.fillAmount = remainingTime / duration)
        }
        else
        {
            Destroy(gameObject); // Despawn the UI element when the buff expires
        }
    }
}