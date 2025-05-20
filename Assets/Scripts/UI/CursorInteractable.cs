using UnityEngine;
using UnityEngine.EventSystems;

public class CursorInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Player player; // Reference to your Player script

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cursor.SetCursor(player.cursorHoverTexture, player.cursorHotspot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(player.cursorTexture, player.cursorHotspot, CursorMode.Auto);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Cursor.SetCursor(player.cursorClickTexture, player.cursorHotspot, CursorMode.Auto);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Cursor.SetCursor(player.cursorHoverTexture, player.cursorHotspot, CursorMode.Auto);
    }
}