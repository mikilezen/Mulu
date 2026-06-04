using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MuluAI
{
    /// <summary>
    /// GTA-style touch camera controller for the character preview.
    /// - Horizontal swipe/drag = orbit camera left/right around character + rotate character
    /// - Vertical swipe/drag   = pitch camera up/down (tilt angle)
    /// Works on both mobile touch AND desktop mouse. Bypasses EventSystem for reliability on Android.
    /// </summary>
    public class MuluCameraSwipeControl : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Transform targetCharacter;
        public Transform cameraTransform;

        [Header("Orbit Settings")]
        public float distance = 4.0f;
        public float horizontalSensitivity = 0.3f;
        public float verticalSensitivity = 0.2f;
        public float minPitch = -15f;
        public float maxPitch = 70f;

        [Header("Character Rotation")]
        [Tooltip("How much the character rotates when you swipe left/right (0 = camera only, 1 = character follows camera)")]
        public float characterRotateRatio = 0.3f;

        [Header("Smoothing")]
        public float smoothSpeed = 8f;

        private float yaw = 180f;
        private float pitch = 12f;
        private float targetYaw = 180f;
        private float targetPitch = 12f;
        public Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);

        private bool isDragging = false;
        private Vector2 lastPointerPos;

        private void Start()
        {
            // Auto-find camera (Main Camera first, then fallback searches)
            if (cameraTransform == null)
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    var go = GameObject.Find("Main Camera") ?? GameObject.Find("CharacterPreviewCamera");
                    if (go != null) cam = go.GetComponent<Camera>();
                }
                if (cam != null) cameraTransform = cam.transform;
            }

            // Auto-find character
            if (targetCharacter == null)
            {
                GameObject stage = GameObject.Find("MuluCharacterStage");
                if (stage != null)
                {
                    targetCharacter = stage.transform.Find("CoolPirate_EditableCharacter")
                        ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null);
                }
            }

            targetYaw = yaw;
            targetPitch = pitch;
            UpdateCameraPosition();
        }

        // ─── EventSystem drag handlers (UI-based) ───

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            lastPointerPos = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            lastPointerPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (cameraTransform == null) return;

            Vector2 delta = eventData.delta;
            ApplySwipe(delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        // ─── Fallback: raw touch/mouse input (works even if EventSystem misses it) ───

        private void Update()
        {
            if (!Application.isPlaying) return;

#if ENABLE_INPUT_SYSTEM
            if (UpdateInputSystemFallback())
            {
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            UpdateLegacyTouchFallback();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool UpdateInputSystemFallback()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                var touch = touchscreen.touches[i];
                Vector2 position = touch.position.ReadValue();

                if (touch.press.wasPressedThisFrame)
                {
                    if (IsTouchOverThisElement(position) && !IsTouchOverJoystick(position))
                    {
                        isDragging = true;
                        lastPointerPos = position;
                        return true;
                    }
                }
                else if (touch.press.isPressed && isDragging)
                {
                    Vector2 delta = position - lastPointerPos;
                    lastPointerPos = position;
                    ApplySwipe(delta);
                    return true;
                }
                else if (touch.press.wasReleasedThisFrame && isDragging)
                {
                    isDragging = false;
                    return true;
                }
            }

            return false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private void UpdateLegacyTouchFallback()
        {
            // Mobile touch input (bypasses EventSystem for reliability)
            if (Input.touchCount <= 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);

            // Only process if the touch is over the preview area (this RawImage)
            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchOverThisElement(touch.position) && !IsTouchOverJoystick(touch.position))
                {
                    isDragging = true;
                    lastPointerPos = touch.position;
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.position - lastPointerPos;
                lastPointerPos = touch.position;
                ApplySwipe(delta);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
#endif

        private void ApplySwipe(Vector2 delta)
        {
            // Horizontal: orbit camera left/right + slightly rotate character
            targetYaw += delta.x * horizontalSensitivity;

            // Vertical: pitch camera up/down (swipe up = look up, swipe down = look down)
            targetPitch -= delta.y * verticalSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Rotate character slightly with horizontal swipe (GTA style)
            if (targetCharacter != null && Mathf.Abs(delta.x) > 0.5f)
            {
                targetCharacter.Rotate(Vector3.up, delta.x * horizontalSensitivity * characterRotateRatio);
            }
        }

        private void LateUpdate()
        {
            // Smooth interpolation for buttery camera movement
            yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * smoothSpeed);
            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * smoothSpeed);

            UpdateCameraPosition();
        }

        public void UpdateCameraPosition()
        {
            if (cameraTransform == null) return;

            // Calculate orbit pivot (character position + offset, or default stage position)
            Vector3 pivot = targetOffset;
            if (targetCharacter != null)
            {
                pivot = targetCharacter.position + targetOffset;
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = pivot - (rotation * Vector3.forward * distance);

            cameraTransform.position = position;
            cameraTransform.rotation = rotation;
        }

        /// <summary>
        /// Check if a screen position is over this UI element's RectTransform.
        /// </summary>
        private bool IsTouchOverThisElement(Vector2 screenPos)
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) return true; // If no RectTransform, assume it's okay

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
        }

        private static bool IsTouchOverJoystick(Vector2 screenPos)
        {
            MuluVirtualJoystick[] joysticks = FindObjectsByType<MuluVirtualJoystick>(FindObjectsSortMode.None);
            for (int i = 0; i < joysticks.Length; i++)
            {
                MuluVirtualJoystick joystick = joysticks[i];
                if (joystick == null || !joystick.isActiveAndEnabled)
                {
                    continue;
                }

                RectTransform rect = joystick.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                Canvas canvas = joystick.GetComponentInParent<Canvas>();
                Camera camera = null;
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    camera = canvas.worldCamera;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, camera))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
