using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Giữ nút UI để bắn liên tục (mobile fire button).
/// </summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData) => IsHeld = true;

    public void OnPointerUp(PointerEventData eventData) => IsHeld = false;

    public void OnPointerExit(PointerEventData eventData) => IsHeld = false;
}
