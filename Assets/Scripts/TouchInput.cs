using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

public static class TouchInput
{
    static readonly List<RaycastResult> uiResults = new List<RaycastResult>();

#if ENABLE_INPUT_SYSTEM
    static void EnsureEnabled()
    {
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
    }

    public static int Count
    {
        get
        {
            EnsureEnabled();
            int count = ETouch.activeTouches.Count;
            if (count == 0 && Mouse.current != null && Mouse.current.leftButton.isPressed) return 1;
            return count;
        }
    }

    public static Vector2 Position(int index)
    {
        EnsureEnabled();
        if (index < ETouch.activeTouches.Count) return ETouch.activeTouches[index].screenPosition;
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    public static bool Began(int index)
    {
        EnsureEnabled();
        if (index < ETouch.activeTouches.Count)
            return ETouch.activeTouches[index].phase == UnityEngine.InputSystem.TouchPhase.Began;
        return index == 0 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
#else
    public static int Count
    {
        get
        {
            if (Input.touchCount > 0) return Input.touchCount;
            return Input.GetMouseButton(0) ? 1 : 0;
        }
    }

    public static Vector2 Position(int index)
    {
        if (index < Input.touchCount) return Input.GetTouch(index).position;
        return Input.mousePosition;
    }

    public static bool Began(int index)
    {
        if (index < Input.touchCount) return Input.GetTouch(index).phase == UnityEngine.TouchPhase.Began;
        return index == 0 && Input.touchCount == 0 && Input.GetMouseButtonDown(0);
    }
#endif

    public static bool IsOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        uiResults.Clear();
        EventSystem.current.RaycastAll(pointer, uiResults);
        return uiResults.Count > 0;
    }
}
