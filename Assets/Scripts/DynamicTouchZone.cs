using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Terresquall;

/// <summary>
/// Vùng chạm tàng hình: dời VirtualJoystick tới ngón tay và forward pointer events.
/// </summary>
[RequireComponent(typeof(Image))]
public class DynamicTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Target Joystick")]
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private VirtualJoystick joystick;

    [Header("Settings")]
    [SerializeField] private bool returnToStartPos = true;
    [SerializeField] [Range(0f, 1f)] private float idleAlpha = 0.4f;

    private Vector2 startAnchoredPos;
    private CanvasGroup joystickCanvasGroup;
    private bool hasStartPos;

    private void Awake()
    {
        Image zoneImage = GetComponent<Image>();
        if (zoneImage != null)
        {
            Color color = zoneImage.color;
            color.a = 0f;
            zoneImage.color = color;
            zoneImage.raycastTarget = true;
        }

        ResolveJoystickReferences();
    }

    private void Start()
    {
        CacheStartPosition();
        ApplyIdleVisual();
    }

    public void Configure(RectTransform root, VirtualJoystick targetJoystick)
    {
        joystickRoot = root;
        joystick = targetJoystick;
        ResolveJoystickReferences();
        CacheStartPosition();
        ApplyIdleVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TryGetJoystick(out RectTransform root, out VirtualJoystick joy))
            return;

        MoveJoystickToScreenPoint(root, eventData);
        SetJoystickAlpha(1f);
        joy.OnPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryGetJoystick(out _, out VirtualJoystick joy))
            return;

        // VirtualJoystick không có IDragHandler — cập nhật axis qua pointer down mỗi frame kéo.
        joy.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!TryGetJoystick(out RectTransform root, out VirtualJoystick joy))
            return;

        if (returnToStartPos && hasStartPos)
            root.anchoredPosition = startAnchoredPos;

        joy.OnPointerUp(eventData);
        ResetControlStickToCenter(joy);

        ApplyIdleVisual();
    }

    private void ResolveJoystickReferences()
    {
        if (joystickRoot != null && joystick == null)
            joystick = joystickRoot.GetComponent<VirtualJoystick>();
    }

    private void CacheStartPosition()
    {
        if (joystickRoot == null)
            return;

        startAnchoredPos = joystickRoot.anchoredPosition;
        hasStartPos = true;
    }

    private void ApplyIdleVisual()
    {
        if (joystickRoot == null)
            return;

        if (joystickCanvasGroup == null)
        {
            joystickCanvasGroup = joystickRoot.GetComponent<CanvasGroup>();
            if (joystickCanvasGroup == null)
                joystickCanvasGroup = joystickRoot.gameObject.AddComponent<CanvasGroup>();
        }

        joystickCanvasGroup.blocksRaycasts = false;
        joystickCanvasGroup.interactable = false;
        joystickCanvasGroup.alpha = idleAlpha;
    }

    private void SetJoystickAlpha(float alpha)
    {
        if (joystickCanvasGroup != null)
            joystickCanvasGroup.alpha = alpha;
    }

    private bool TryGetJoystick(out RectTransform root, out VirtualJoystick joy)
    {
        ResolveJoystickReferences();
        root = joystickRoot;
        joy = joystick;
        return root != null && joy != null;
    }

    private static void MoveJoystickToScreenPoint(RectTransform root, PointerEventData eventData)
    {
        RectTransform parentRect = root.parent as RectTransform;
        if (parentRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            root.anchoredPosition = localPoint;
        }
    }

    private static void ResetControlStickToCenter(VirtualJoystick joy)
    {
        if (joy == null || joy.controlStick == null)
            return;

        RectTransform knob = joy.controlStick.rectTransform;
        knob.anchoredPosition = Vector2.zero;
        knob.localPosition = Vector3.zero;
    }
}
