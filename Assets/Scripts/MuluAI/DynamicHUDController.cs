using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MuluAI
{
    public class DynamicHUDController : MonoBehaviour
    {
        [Header("Portrait UI References")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GameObject virtualJoystickPrefab;
        [SerializeField] private GameObject actionButtonPrefab;
        [SerializeField] private Transform buttonGridContainer;
        [SerializeField] private RectTransform joystickAnchor;

        [Header("Portrait Layout")]
        [SerializeField] private bool forcePortraitOrientation = true;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField] private Vector2 joystickSize = new Vector2(260f, 260f);
        [SerializeField] private Vector2 joystickAnchoredPosition = new Vector2(170f, 170f);
        [SerializeField] private Vector2 actionButtonSize = new Vector2(180f, 180f);
        [SerializeField] private int actionButtonColumns = 2;
        [SerializeField] private float actionButtonSpacing = 24f;

        [Header("Editable Button Style")]
        [SerializeField] private Color buttonColor = new Color(0.12f, 0.14f, 0.18f, 0.9f);
        [SerializeField] private Color buttonTextColor = Color.white;
        [SerializeField] private int buttonFontSize = 34;

        [Header("Actions")]
        public UnityEvent<string> ActionTriggered = new UnityEvent<string>();

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private GameObject activeJoystick;

        public GameObject VirtualJoystickPrefab
        {
            get { return virtualJoystickPrefab; }
            set { virtualJoystickPrefab = value; }
        }

        public GameObject ActionButtonPrefab
        {
            get { return actionButtonPrefab; }
            set { actionButtonPrefab = value; }
        }

        public Transform ButtonGridContainer
        {
            get { return buttonGridContainer; }
            set { buttonGridContainer = value; }
        }

        private void Awake()
        {
            ApplyPortraitCanvasSettings();
            ConfigureGridForPortrait();
        }

        public void ConfigureGamepad(ControlsConfig config)
        {
            ApplyPortraitCanvasSettings();
            ConfigureGridForPortrait();
            ClearButtons();

            bool joystickEnabled = config != null && config.joystick_enabled;
            SetJoystickVisible(joystickEnabled);

            if (config == null || config.buttons == null || actionButtonPrefab == null || buttonGridContainer == null)
            {
                return;
            }

            for (int i = 0; i < config.buttons.Count; i++)
            {
                JoystickButton buttonConfig = config.buttons[i];
                GameObject buttonObject = Instantiate(actionButtonPrefab, buttonGridContainer);
                buttonObject.SetActive(true);
                string buttonId = buttonConfig != null ? buttonConfig.id : string.Empty;
                buttonObject.name = "ActionButton_" + (string.IsNullOrWhiteSpace(buttonId) ? i.ToString() : buttonId);

                RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = actionButtonSize;
                }

                ApplyButtonVisuals(buttonObject, buttonConfig);
                WireButtonClick(buttonObject, buttonConfig);
                spawnedButtons.Add(buttonObject);
            }
        }

        public void OnActionTriggered(string action)
        {
            Debug.Log("Mulu action triggered: " + action);

            if (string.Equals(action, "nitro", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Nitro action routed. Connect this event to the vehicle boost system in the Inspector.");
            }
            else if (string.Equals(action, "attack", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Attack action routed. Connect this event to the combat system in the Inspector.");
            }
            else if (string.Equals(action, "jump", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Jump action routed. Connect this event to the character movement system in the Inspector.");
            }

            ActionTriggered.Invoke(action);
        }

        private void SetJoystickVisible(bool isVisible)
        {
            if (!isVisible)
            {
                if (activeJoystick != null)
                {
                    activeJoystick.SetActive(false);
                }

                return;
            }

            if (activeJoystick == null && virtualJoystickPrefab != null)
            {
                Transform parent = joystickAnchor != null ? joystickAnchor : transform;
                activeJoystick = Instantiate(virtualJoystickPrefab, parent);
                activeJoystick.name = "VirtualJoystick";
                RectTransform rectTransform = activeJoystick.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0f, 0f);
                    rectTransform.anchorMax = new Vector2(0f, 0f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = joystickSize;
                    rectTransform.anchoredPosition = joystickAnchoredPosition;
                }
            }

            if (activeJoystick != null)
            {
                activeJoystick.SetActive(true);
            }
        }

        private void ClearButtons()
        {
            for (int i = spawnedButtons.Count - 1; i >= 0; i--)
            {
                GameObject buttonObject = spawnedButtons[i];
                if (buttonObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(buttonObject);
                }
                else
                {
                    DestroyImmediate(buttonObject);
                }
            }

            spawnedButtons.Clear();
        }

        private void WireButtonClick(GameObject buttonObject, JoystickButton buttonConfig)
        {
            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.GetComponentInChildren<Button>(true);
            }

            if (button == null)
            {
                Debug.LogWarning("ActionButtonPrefab must include a UnityEngine.UI.Button.");
                return;
            }

            string action = buttonConfig != null ? buttonConfig.action : string.Empty;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(delegate { OnActionTriggered(action); });
        }

        private void ApplyButtonVisuals(GameObject buttonObject, JoystickButton buttonConfig)
        {
            Text label = buttonObject.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = buttonConfig != null ? buttonConfig.label : string.Empty;
                label.color = buttonTextColor;
                label.fontSize = buttonFontSize;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(12, buttonFontSize / 2);
                label.resizeTextMaxSize = buttonFontSize;
            }

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.GetComponentInChildren<Image>(true);
            }

            if (image != null)
            {
                image.color = buttonColor;
            }
        }

        private void ApplyPortraitCanvasSettings()
        {
            if (forcePortraitOrientation)
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }

            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if (targetCanvas == null)
            {
                return;
            }

            CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        private void ConfigureGridForPortrait()
        {
            if (buttonGridContainer == null)
            {
                return;
            }

            GridLayoutGroup grid = buttonGridContainer.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = buttonGridContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = actionButtonSize;
            grid.spacing = new Vector2(actionButtonSpacing, actionButtonSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, actionButtonColumns);
            grid.childAlignment = TextAnchor.LowerRight;

            RectTransform rectTransform = buttonGridContainer as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(1f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(1f, 0f);
                rectTransform.anchoredPosition = new Vector2(-64f, 120f);
            }
        }
    }
}
