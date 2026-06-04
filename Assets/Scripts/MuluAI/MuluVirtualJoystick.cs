using UnityEngine;
using UnityEngine.EventSystems;

namespace MuluAI
{
    /// <summary>
    /// Smooth virtual joystick for mobile touch controls.
    /// Tracks pointer down origin to prevent knob jumping, scales drag delta by canvas factor,
    /// and provides normalized 360-degree movement input.
    /// </summary>
    public class MuluVirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform ring;
        private RectTransform knob;
        private float maxRange = 100f;
        private Vector2 pointerDownPos;

        public Vector2 InputDirection { get; private set; }

        private void Awake()
        {
            ring = GetComponent<RectTransform>();
            knob = transform.Find("Knob")?.GetComponent<RectTransform>();
            EnsureCircleVisual(GetComponent<RectTransform>(), new Color(0.02f, 0.025f, 0.04f, 0.65f), true);
            EnsureCircleVisual(transform.Find("Ring") as RectTransform, new Color(1f, 1f, 1f, 0.08f), false);
            EnsureCircleVisual(knob, new Color(0.08f, 0.52f, 1f, 1f), false);
            
            if (ring != null)
            {
                maxRange = ring.sizeDelta.x * 0.4f;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ring == null || knob == null) return;
            
            pointerDownPos = eventData.position;
            knob.anchoredPosition = Vector2.zero;
            InputDirection = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ring == null || knob == null) return;

            // Calculate screen space delta
            Vector2 delta = eventData.position - pointerDownPos;

            // Scale delta by canvas scale factor to match anchoredPosition units
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.scaleFactor > 0.001f)
            {
                delta /= canvas.scaleFactor;
            }

            // Clamp movement inside the ring's max range
            if (delta.magnitude > maxRange)
            {
                delta = delta.normalized * maxRange;
            }

            knob.anchoredPosition = delta;
            InputDirection = delta / maxRange; // Normalized -1 to 1 vector
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
            InputDirection = Vector2.zero;
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
