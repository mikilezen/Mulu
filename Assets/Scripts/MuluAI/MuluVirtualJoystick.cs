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
    }
}
