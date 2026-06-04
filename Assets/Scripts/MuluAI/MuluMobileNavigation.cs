using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MuluAI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MuluMobileNavigation : MonoBehaviour
    {
        [Header("Pages")]
        public GameObject homePage;
        public GameObject accountPage;
        public GameObject menuPage;

        [Header("Chat")]
        public InputField promptInput;
        public Button sendButton;
        public Button audioButton;
        public MuluPromptBuildController promptController;

        [Header("Navigation Tabs")]
        public Button accountButton;
        public Button menuButton;

        [Header("Movement Control")]
        public MuluVirtualJoystick joystick;
        public Button pageBackButton;

        [Header("Movement D-Pad (Legacy Fallback)")]
        public Button forwardButton;
        public Button backButton;
        public Button leftButton;
        public Button rightButton;

        [Header("Character Preview")]
        public Transform characterTransform;

        [Header("Theme Colors")]
        public Color surfaceColor = new Color(0.1f, 0.12f, 0.17f, 0.96f);
        public Color surfaceStrongColor = new Color(0.15f, 0.18f, 0.24f, 0.98f);
        public Color accentColor = new Color(0.08f, 0.52f, 1f, 1f);
        public Color textPrimaryColor = Color.white;
        public Color textSecondaryColor = new Color(0.78f, 0.82f, 0.88f, 1f);

        private bool isRotatingLeft;
        private bool isRotatingRight;
        private bool isMovingForward;
        private bool isMovingBackward;

        private void Awake()
        {
            AutoWire();
            EnsureHomeRuntimeContent();
            AutoWire();
            Wire();
            ShowHome();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ShowHome();
            }
        }

        private Transform cameraTransform;

        private void Update()
        {
            if (characterTransform == null || !Application.isPlaying) return;

            float movementSpeed = 4f;  // units per second

            Vector2 moveDir = Vector2.zero;

            if (joystick != null)
            {
                moveDir = joystick.InputDirection;
            }
            else
            {
                // Fallback to legacy D-pad states
                if (isRotatingLeft) moveDir.x = -1f;
                if (isRotatingRight) moveDir.x = 1f;
                if (isMovingForward) moveDir.y = 1f;
                if (isMovingBackward) moveDir.y = -1f;
            }

            if (moveDir != Vector2.zero)
            {
                // Find camera if not cached
                if (cameraTransform == null)
                {
                    var cam = Camera.main;
                    if (cam == null)
                    {
                        var camGO = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera") ?? GameObject.Find("CharacterPreviewCamera");
                        if (camGO != null) cam = camGO.GetComponent<Camera>();
                    }
                    if (cam != null) cameraTransform = cam.transform;
                }

                // Get camera forward and right directions projected on ground (y = 0 plane)
                Vector3 camForward = Vector3.forward;
                Vector3 camRight = Vector3.right;
                if (cameraTransform != null)
                {
                    camForward = cameraTransform.forward;
                    camRight = cameraTransform.right;
                }
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // Compute desired move direction in world space
                Vector3 moveVector = (camForward * moveDir.y) + (camRight * moveDir.x);
                if (moveVector.sqrMagnitude > 0.001f)
                {
                    // Move the character in world space
                    characterTransform.Translate(moveVector * (movementSpeed * Time.deltaTime), Space.World);

                    // Rotate the character to face the movement direction
                    Quaternion targetRotation = Quaternion.LookRotation(moveVector, Vector3.up);
                    characterTransform.rotation = Quaternion.Slerp(characterTransform.rotation, targetRotation, 12f * Time.deltaTime);
                }
            }
        }

        public void Wire()
        {
            WireButton(accountButton, ShowAccount);
            WireButton(menuButton, ShowMenu);
            WireButton(sendButton, SendPrompt);
            WireButton(audioButton, TriggerAudio);
            WireButton(pageBackButton, ShowHome);

            // Wire continuous pointer down/up events for movement D-pad buttons (if active)
            BindPointerEvents(leftButton, () => isRotatingLeft = true, () => isRotatingLeft = false);
            BindPointerEvents(rightButton, () => isRotatingRight = true, () => isRotatingRight = false);
            BindPointerEvents(forwardButton, () => isMovingForward = true, () => isMovingForward = false);
            BindPointerEvents(backButton, () => isMovingBackward = true, () => isMovingBackward = false);

            // Wire keyboard enter key inside the InputField
            if (promptInput != null)
            {
                promptInput.onEndEdit.RemoveAllListeners();
                promptInput.onEndEdit.AddListener(value =>
                {
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        SendPrompt();
                    }
                });
            }
        }

        public void ShowHome()
        {
            SelectTab(0);
        }

        public void ShowAccount()
        {
            SelectTab(1);
        }

        public void ShowMenu()
        {
            SelectTab(2);
        }

        private void SelectTab(int index)
        {
            SetPage(homePage, index == 0);
            SetPage(accountPage, index == 1);
            SetPage(menuPage, index == 2);

            StyleTabButton(accountButton, index == 1);
            StyleTabButton(menuButton, index == 2);

            if (pageBackButton != null)
            {
                pageBackButton.gameObject.SetActive(index != 0);
            }
        }

        private void StyleTabButton(Button button, bool isActive)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            Text label = button.GetComponentInChildren<Text>();

            if (isActive)
            {
                if (image != null) image.color = accentColor;
                if (label != null) label.color = textPrimaryColor;
            }
            else
            {
                if (image != null) image.color = surfaceStrongColor;
                if (label != null) label.color = textSecondaryColor;
            }
        }

        public void SendPrompt()
        {
            if (promptInput == null || promptController == null)
            {
                Debug.LogWarning("Mulu mobile UI is missing prompt input or prompt controller.");
                return;
            }

            string prompt = promptInput.text;
            if (string.IsNullOrWhiteSpace(prompt)) return;

            promptInput.text = string.Empty;
            promptController.SubmitPrompt(prompt);
        }

        public void TriggerAudio()
        {
            Debug.Log("Mulu audio input tapped. Connect this button to microphone capture.");
        }

        private static void SetPage(GameObject page, bool active)
        {
            if (page != null)
            {
                page.SetActive(active);
            }
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void BindPointerEvents(Button button, System.Action onDown, System.Action onUp)
        {
            if (button == null) return;
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => { onDown(); });
            trigger.triggers.Add(downEntry);

            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => { onUp(); });
            trigger.triggers.Add(upEntry);
        }

        [ContextMenu("Auto-Wire Scene References")]
        public void AutoWire()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
            if (canvas == null) return;

            Transform root = canvas.transform.Find("MuluMobileRoot") ?? canvas.transform;

            homePage = root.Find("HomePage")?.gameObject;
            accountPage = root.Find("AccountPage")?.gameObject;
            menuPage = root.Find("MenuPage")?.gameObject;

            Transform header = root.Find("TopHeader");
            if (header != null)
            {
                pageBackButton = header.Find("PageBackButton")?.GetComponent<Button>();
            }

            Transform appBar = root.Find("BottomNav");
            if (appBar != null)
            {
                accountButton = appBar.Find("AccountTab")?.GetComponent<Button>();
                menuButton = appBar.Find("MenuTab")?.GetComponent<Button>();
            }

            Transform page = homePage != null ? homePage.transform : root;

            Transform composer = page.Find("ChatComposer");
            if (composer != null)
            {
                promptInput = composer.Find("PromptInput")?.GetComponent<InputField>();
                sendButton = composer.Find("SendButton")?.GetComponent<Button>();
                audioButton = composer.Find("AudioButton")?.GetComponent<Button>();
            }

            Transform previewFrame = page.Find("CharacterPreviewFrame");
            if (previewFrame != null)
            {
                joystick = previewFrame.Find("MoveJoystick3D")?.GetComponent<MuluVirtualJoystick>();
                
                Transform pad = previewFrame.Find("MoveJoystick3D");
                if (pad != null)
                {
                    forwardButton = pad.Find("ForwardButton")?.GetComponent<Button>();
                    backButton = pad.Find("BackButton")?.GetComponent<Button>();
                    leftButton = pad.Find("LeftButton")?.GetComponent<Button>();
                    rightButton = pad.Find("RightButton")?.GetComponent<Button>();
                }
            }

            promptController = FindAnyObjectByType<MuluPromptBuildController>();

            GameObject stage = GameObject.Find("MuluCharacterStage");
            if (stage != null)
            {
                characterTransform = stage.transform.Find("CoolPirate_EditableCharacter") 
                    ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null);
            }

            // Wire ChatLogView to the controller
            MuluChatLogView chatLog = page.GetComponentInChildren<MuluChatLogView>();
            if (chatLog != null && promptController != null)
            {
                System.Reflection.FieldInfo field = promptController.GetType().GetField("chatLogView", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(promptController, chatLog);
                }
            }

            Debug.Log("MuluMobileNavigation references auto-wired successfully.");
        }

        private void EnsureHomeRuntimeContent()
        {
            if (homePage == null)
            {
                return;
            }

            RectTransform homeRect = homePage.GetComponent<RectTransform>();
            if (homeRect == null)
            {
                return;
            }

            RectTransform previewFrame = homePage.transform.Find("CharacterPreviewFrame") as RectTransform;
            if (previewFrame == null)
            {
                previewFrame = CreatePanel("CharacterPreviewFrame", homeRect, StretchAnchor(), Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.045f, 0.12f));
                Image previewImage = previewFrame.GetComponent<Image>();
                if (previewImage != null)
                {
                    previewImage.raycastTarget = false;
                }
            }

            MuluChatLogView chatLog = previewFrame.GetComponentInChildren<MuluChatLogView>(true);
            if (chatLog == null)
            {
                chatLog = CreateRuntimeChatScroll(previewFrame);
                chatLog.AddSystemMessage("Chat is ready. Type a prompt below to start.");
            }

            if (previewFrame.Find("MoveJoystick3D") == null)
            {
                CreateRuntimeJoystick(previewFrame);
            }

            if (chatLog != null && promptController != null)
            {
                System.Reflection.FieldInfo field = promptController.GetType().GetField("chatLogView",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(promptController, chatLog);
                }
            }
        }

        private MuluChatLogView CreateRuntimeChatScroll(RectTransform parent)
        {
            RectTransform scrollRoot = CreatePanel("ChatScroll", parent,
                new AnchorPreset(new Vector2(0f, 0.12f), new Vector2(1f, 0.58f), new Vector2(0.5f, 0.5f)),
                Vector2.zero, Vector2.zero, Color.clear);

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            RectTransform viewport = CreatePanel("Viewport", scrollRoot, StretchAnchor(), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.15f));
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            RectTransform content = CreatePanel("Content", viewport,
                new AnchorPreset(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f)),
                Vector2.zero, Vector2.zero, Color.clear);

            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 16, 18);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            MuluChatLogView chatLog = scrollRoot.gameObject.AddComponent<MuluChatLogView>();
            SetPrivateField(chatLog, "content", content);
            SetPrivateField(chatLog, "scrollRect", scrollRect);
            SetPrivateField(chatLog, "font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            return chatLog;
        }

        private MuluVirtualJoystick CreateRuntimeJoystick(RectTransform parent)
        {
            RectTransform pad = CreatePanel("MoveJoystick3D", parent,
                new AnchorPreset(new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero),
                new Vector2(24f, 264f), new Vector2(180f, 180f), new Color(0.02f, 0.025f, 0.04f, 0.65f));
            ApplyCircleVisual(pad, new Color(0.02f, 0.025f, 0.04f, 0.65f), true);

            Image padImage = pad.GetComponent<Image>();
            if (padImage != null)
            {
                padImage.raycastTarget = true;
            }

            RectTransform ring = CreatePanel("Ring", pad, StretchAnchor(), Vector2.zero, new Vector2(-20f, -20f), new Color(1f, 1f, 1f, 0.08f));
            ApplyCircleVisual(ring, new Color(1f, 1f, 1f, 0.08f), false);
            Image ringImage = ring.GetComponent<Image>();
            if (ringImage != null)
            {
                ringImage.raycastTarget = false;
            }

            RectTransform knob = CreatePanel("Knob", pad,
                new AnchorPreset(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
                Vector2.zero, new Vector2(76f, 76f), accentColor);
            ApplyCircleVisual(knob, accentColor, false);
            Image knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.raycastTarget = false;
            }

            return pad.gameObject.AddComponent<MuluVirtualJoystick>();
        }

        private static RectTransform CreatePanel(string name, Transform parent, AnchorPreset anchors, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchors.Min;
            rect.anchorMax = anchors.Max;
            rect.pivot = anchors.Pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private static void ApplyCircleVisual(RectTransform rect, Color color, bool raycastTarget)
        {
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;
            }

            MuluCircleGraphic circle = rect.GetComponent<MuluCircleGraphic>();
            if (circle == null)
            {
                circle = rect.gameObject.AddComponent<MuluCircleGraphic>();
            }

            circle.color = color;
            circle.raycastTarget = raycastTarget;
        }

        private static AnchorPreset StretchAnchor()
        {
            return new AnchorPreset(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private struct AnchorPreset
        {
            public readonly Vector2 Min;
            public readonly Vector2 Max;
            public readonly Vector2 Pivot;

            public AnchorPreset(Vector2 min, Vector2 max, Vector2 pivot)
            {
                Min = min;
                Max = max;
                Pivot = pivot;
            }
        }
    }
}
