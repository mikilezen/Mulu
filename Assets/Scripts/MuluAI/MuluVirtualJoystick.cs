using UnityEngine;
using UnityEngine.EventSystems;

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

        private RectTransform ring;
        private RectTransform knob;
        private Canvas canvas;
        private Camera eventCamera;
        private float maxRange = 100f;
        private int activePointerId = NoPointer;
        private bool usingMousePointer;

        public Vector2 InputDirection { get; private set; }

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

            if (Input.touchCount > 0)
            {
                UpdateTouchFallback();
                return;
            }

            UpdateMouseFallback();
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

        private void UpdateTouchFallback()
        {
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

            if (!activeTouch.HasValue)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if ((touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        && IsScreenPointInside(touch.position))
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
                return;
            }

            Touch current = activeTouch.Value;
            if (current.phase == TouchPhase.Ended || current.phase == TouchPhase.Canceled)
            {
                ResetJoystick();
                return;
            }

            SetFromScreenPosition(current.position, eventCamera);
        }

        private void UpdateMouseFallback()
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

        private void ResetJoystick()
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
