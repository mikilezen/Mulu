using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MuluAI
{
    /// <summary>
    /// Reliable 360-degree virtual joystick for mobile touch controls.
    /// Converts the finger position to local joystick-space every frame so diagonal
    /// movement works even when the touch starts away from the exact center.
    /// </summary>
    public class MuluVirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private float rangeScale = 0.42f;
        [SerializeField] private bool acceptTouchesStartedInsideOnly = true;

        private RectTransform ring;
        private RectTransform knob;
        private Canvas canvas;
        private Camera eventCamera;
        private float maxRange = 100f;
        private int activePointerId = NoPointer;
        private bool usingMousePointer;

        public Vector2 InputDirection { get; private set; }
        public bool IsActive => activePointerId != NoPointer && InputDirection.sqrMagnitude > 0.0001f;

        private void Awake()
        {
            CacheReferences();
            ApplyVisuals();
            RecalculateRange();
        }

        private void OnEnable()
        {
            CacheReferences();
            RecalculateRange();
            ResetJoystick();
        }

        private void Update()
        {
            // Raw input fallback keeps the joystick working on Android/iOS even if the
            // EventSystem misses drag events or another UI element tries to scroll.
            if (!Application.isPlaying)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (UpdateInputSystemTouchFallback())
            {
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UpdateLegacyTouchFallback())
            {
                return;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            if (UpdateInputSystemMouseFallback())
            {
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            UpdateLegacyMouseFallback();
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ring == null || knob == null)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            usingMousePointer = eventData.pointerId < 0;
            SetFromScreenPosition(eventData.position, eventData.pressEventCamera ?? eventData.enterEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ring == null || knob == null)
            {
                return;
            }

            if (activePointerId != NoPointer && eventData.pointerId != activePointerId)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            usingMousePointer = eventData.pointerId < 0;
            SetFromScreenPosition(eventData.position, eventData.pressEventCamera ?? eventData.enterEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointerId == NoPointer || eventData.pointerId == activePointerId)
            {
                ResetJoystick();
            }
        }

#if ENABLE_INPUT_SYSTEM
        private bool UpdateInputSystemTouchFallback()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            bool anyPressed = false;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                anyPressed = true;
                int touchId = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (activePointerId != NoPointer && !usingMousePointer && touchId == activePointerId)
                {
                    SetFromScreenPosition(position, eventCamera);
                    return true;
                }
            }

            if (activePointerId != NoPointer && !usingMousePointer && !anyPressed)
            {
                ResetJoystick();
                return true;
            }

            if (activePointerId != NoPointer)
            {
                return anyPressed;
            }

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                if (!touch.press.isPressed)
                {
                    continue;
                }

                Vector2 position = touch.position.ReadValue();
                if (!acceptTouchesStartedInsideOnly || IsScreenPointInside(position))
                {
                    activePointerId = touch.touchId.ReadValue();
                    usingMousePointer = false;
                    SetFromScreenPosition(position, eventCamera);
                    return true;
                }
            }

            return false;
        }

        private bool UpdateInputSystemMouseFallback()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && IsScreenPointInside(position))
            {
                activePointerId = -1;
                usingMousePointer = true;
            }

            if (usingMousePointer && activePointerId != NoPointer && mouse.leftButton.isPressed)
            {
                SetFromScreenPosition(position, eventCamera);
                return true;
            }

            if (usingMousePointer && activePointerId != NoPointer && mouse.leftButton.wasReleasedThisFrame)
            {
                ResetJoystick();
                return true;
            }

            return false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private bool UpdateLegacyTouchFallback()
        {
            if (Input.touchCount <= 0)
            {
                if (activePointerId != NoPointer && !usingMousePointer)
                {
                    ResetJoystick();
                    return true;
                }

                return false;
            }

            Touch? activeTouch = null;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (activePointerId != NoPointer && !usingMousePointer && touch.fingerId == activePointerId)
                {
                    activeTouch = touch;
                    break;
                }
            }

            if (!activeTouch.HasValue && activePointerId == NoPointer)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if ((touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        && (!acceptTouchesStartedInsideOnly || IsScreenPointInside(touch.position)))
                    {
                        activePointerId = touch.fingerId;
                        usingMousePointer = false;
                        activeTouch = touch;
                        break;
                    }
                }
            }

            if (!activeTouch.HasValue)
            {
                return true;
            }

            Touch current = activeTouch.Value;
            if (current.phase == TouchPhase.Ended || current.phase == TouchPhase.Canceled)
            {
                ResetJoystick();
                return true;
            }

            SetFromScreenPosition(current.position, eventCamera);
            return true;
        }

        private void UpdateLegacyMouseFallback()
        {
            if (Input.GetMouseButtonDown(0) && IsScreenPointInside(Input.mousePosition))
            {
                activePointerId = -1;
                usingMousePointer = true;
            }

            if (usingMousePointer && activePointerId != NoPointer && Input.GetMouseButton(0))
            {
                SetFromScreenPosition(Input.mousePosition, eventCamera);
            }
            else if (usingMousePointer && activePointerId != NoPointer && Input.GetMouseButtonUp(0))
            {
                ResetJoystick();
            }
        }
#endif

        private void SetFromScreenPosition(Vector2 screenPosition, Camera camera)
        {
            if (ring == null || knob == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ring, screenPosition, camera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 delta = localPoint - ring.rect.center;
            if (maxRange <= 0.001f)
            {
                RecalculateRange();
            }

            Vector2 clamped = Vector2.ClampMagnitude(delta, maxRange);
            knob.anchoredPosition = clamped;
            InputDirection = maxRange > 0.001f ? Vector2.ClampMagnitude(clamped / maxRange, 1f) : Vector2.zero;
        }

        private bool IsScreenPointInside(Vector2 screenPosition)
        {
            if (ring == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(ring, screenPosition, eventCamera);
        }

        public void ResetJoystick()
        {
            activePointerId = NoPointer;
            usingMousePointer = false;
            InputDirection = Vector2.zero;
            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
        }

        private void CacheReferences()
        {
            ring = GetComponent<RectTransform>();
            knob = transform.Find("Knob")?.GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }

        private void RecalculateRange()
        {
            if (ring == null)
            {
                return;
            }

            Rect rect = ring.rect;
            float smallestSize = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
            if (smallestSize <= 0.001f)
            {
                smallestSize = Mathf.Min(Mathf.Abs(ring.sizeDelta.x), Mathf.Abs(ring.sizeDelta.y));
            }

            maxRange = Mathf.Max(24f, smallestSize * Mathf.Clamp(rangeScale, 0.2f, 0.5f));
        }

        private void ApplyVisuals()
        {
            EnsureCircleVisual(ring, new Color(0.02f, 0.025f, 0.04f, 0.65f), true);
            EnsureCircleVisual(transform.Find("Ring") as RectTransform, new Color(1f, 1f, 1f, 0.08f), false);
            EnsureCircleVisual(knob, new Color(0.08f, 0.52f, 1f, 1f), false);
        }

        private static void EnsureCircleVisual(RectTransform rect, Color color, bool raycastTarget)
        {
            if (rect == null)
            {
                return;
            }

            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.enabled = false;
            }

            var circle = rect.GetComponent<MuluCircleGraphic>();
            if (circle == null)
            {
                circle = rect.gameObject.AddComponent<MuluCircleGraphic>();
            }

            circle.color = color;
            circle.raycastTarget = raycastTarget;
        }
    }
}
