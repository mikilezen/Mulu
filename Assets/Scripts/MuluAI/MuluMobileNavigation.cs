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
        public Button jumpButton;
        public Button pageBackButton;

        [Header("Movement D-Pad (Legacy Fallback)")]
        public Button forwardButton;
        public Button backButton;
        public Button leftButton;
        public Button rightButton;

        [Header("Character Preview")]
        public Transform characterTransform;
        public float movementSpeed = 3.2f;
        public float rotationSmoothing = 14f;
        public float movementDeadZone = 0.04f;
        public float stageRadius = 2.65f;

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
        private bool jumpQueued;
        private MuluCharacterMotor characterMotor;

        private void Awake()
        {
            AutoWire();
            EnsureStagePresentation();
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

            EnsureCharacterMotor();

            if (joystick == null || !joystick.isActiveAndEnabled)
            {
                joystick = FindAnyObjectByType<MuluVirtualJoystick>();
            }

            Vector2 moveDir = Vector2.zero;

            if (joystick != null && joystick.isActiveAndEnabled)
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

            if (moveDir.sqrMagnitude > movementDeadZone * movementDeadZone)
            {
                moveDir = Vector2.ClampMagnitude(moveDir, 1f);
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

                if (camForward.sqrMagnitude < 0.001f)
                {
                    camForward = Vector3.forward;
                }
                else
                {
                    camForward.Normalize();
                }

                if (camRight.sqrMagnitude < 0.001f)
                {
                    camRight = Vector3.right;
                }
                else
                {
                    camRight.Normalize();
                }

                // Compute desired 360-degree movement in world space, preserving analog joystick strength.
                Vector3 moveVector = (camForward * moveDir.y) + (camRight * moveDir.x);
                float analogStrength = Mathf.Clamp01(moveDir.magnitude);
                if (moveVector.sqrMagnitude > 0.001f)
                {
                    Vector3 moveDirection = moveVector.normalized;
                    if (characterMotor != null)
                    {
                        characterMotor.moveSpeed = movementSpeed;
                        characterMotor.rotationSmoothing = rotationSmoothing;
                        characterMotor.stageRadius = stageRadius;
                        characterMotor.Move(moveDirection, analogStrength, Time.deltaTime);
                    }
                    else
                    {
                        Vector3 nextPosition = characterTransform.position + moveDirection * (movementSpeed * analogStrength * Time.deltaTime);
                        nextPosition = ClampToStage(nextPosition);
                        characterTransform.position = nextPosition;

                        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                        characterTransform.rotation = Quaternion.Slerp(characterTransform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
                    }
                }
            }
            else if (characterMotor != null)
            {
                characterMotor.Stop(Time.deltaTime);
            }

            if (jumpQueued)
            {
                jumpQueued = false;
                if (characterMotor != null)
                {
                    characterMotor.Jump();
                }
            }
        }

        private void EnsureCharacterMotor()
        {
            if (characterTransform == null)
            {
                return;
            }

            if (characterMotor == null || characterMotor.transform != characterTransform)
            {
                characterMotor = characterTransform.GetComponent<MuluCharacterMotor>();
                if (characterMotor == null)
                {
                    characterMotor = characterTransform.gameObject.AddComponent<MuluCharacterMotor>();
                }
            }

            characterMotor.moveSpeed = movementSpeed;
            characterMotor.rotationSmoothing = rotationSmoothing;
            characterMotor.stageRadius = stageRadius;
        }

        private Vector3 ClampToStage(Vector3 position)
        {
            if (stageRadius <= 0f)
            {
                return position;
            }

            Vector3 stageCenter = characterTransform != null && characterTransform.parent != null
                ? characterTransform.parent.position
                : Vector3.zero;
            Vector2 offset = new Vector2(position.x - stageCenter.x, position.z - stageCenter.z);
            if (offset.sqrMagnitude > stageRadius * stageRadius)
            {
                offset = offset.normalized * stageRadius;
                position.x = stageCenter.x + offset.x;
                position.z = stageCenter.z + offset.y;
            }

            return position;
        }

        public void Wire()
        {
            WireButton(accountButton, ShowAccount);
            WireButton(menuButton, ShowMenu);
            WireButton(sendButton, SendPrompt);
            WireButton(audioButton, TriggerAudio);
            WireButton(jumpButton, QueueJump);
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
                    if (IsSubmitKeyPressed())
                    {
                        SendPrompt();
                    }
                });
            }
        }

        private static bool IsSubmitKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                return true;
            }
#endif
            return false;
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

        public void QueueJump()
        {
            jumpQueued = true;
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
                jumpButton = previewFrame.Find("JumpButton")?.GetComponent<Button>();
                joystick = previewFrame.Find("MoveJoystick3D")?.GetComponent<MuluVirtualJoystick>();
                if (joystick == null)
                {
                    joystick = FindAnyObjectByType<MuluVirtualJoystick>();
                }
                
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

            GameObject stage = GameObject.Find("MuluCharacterStage") ?? GameObject.Find("MuluRuntimeCharacterStage");
            if (stage != null)
            {
                characterTransform = stage.transform.Find("CoolPirate_EditableCharacter") 
                    ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null);
                EnsureCharacterMotor();
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

        private void EnsureStagePresentation()
        {
            GameObject stage = GameObject.Find("MuluCharacterStage") ?? GameObject.Find("MuluRuntimeCharacterStage");
            if (stage == null)
            {
                return;
            }

            Transform character = stage.transform.Find("CoolPirate_EditableCharacter")
                ?? stage.transform.Find("CharacterFallback")
                ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null);
            if (character != null)
            {
                characterTransform = character;
                if (character.localPosition.sqrMagnitude > 25f)
                {
                    character.localPosition = Vector3.zero;
                }
                character.localRotation = Quaternion.identity;
            }

            MuluCharacterConfigurator configurator = stage.GetComponent<MuluCharacterConfigurator>();
            if (configurator == null)
            {
                configurator = stage.AddComponent<MuluCharacterConfigurator>();
            }

            MuluCharacterRuntimeCustomizer customizer = stage.GetComponent<MuluCharacterRuntimeCustomizer>();
            if (customizer == null)
            {
                customizer = stage.AddComponent<MuluCharacterRuntimeCustomizer>();
            }
            customizer.currentCharacter = character;

            configurator.characterTransform = character;
            configurator.targetCharacterHeight = 1.45f;
            configurator.platformDiameter = 6f;
            configurator.cameraDistance = 5.25f;
            configurator.cameraTargetOffset = new Vector3(0f, 0.75f, 0f);
            configurator.platformRenderer = stage.transform.Find("CharacterStagePlatform")?.GetComponent<Renderer>();

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera") ?? GameObject.Find("CharacterPreviewCamera");
                if (cameraObject != null)
                {
                    mainCamera = cameraObject.GetComponent<Camera>();
                }
            }

            if (mainCamera != null)
            {
                configurator.mainCamera = mainCamera;
            }

            MuluCameraSwipeControl swipeControl = configurator.cameraSwipeControl != null
                ? configurator.cameraSwipeControl
                : FindAnyObjectByType<MuluCameraSwipeControl>();
            if (swipeControl != null)
            {
                configurator.cameraSwipeControl = swipeControl;
                swipeControl.targetCharacter = character;
                if (mainCamera != null)
                {
                    swipeControl.cameraTransform = mainCamera.transform;
                }
                swipeControl.distance = configurator.cameraDistance;
                swipeControl.targetOffset = configurator.cameraTargetOffset;
            }

            configurator.ApplySettings();
            EnsureCharacterMotor();
            if (characterMotor != null)
            {
                characterMotor.SnapToFloor();
            }

            CreateStageBackdrop(stage.transform);
        }

        private void CreateStageBackdrop(Transform stage)
        {
            if (stage == null || stage.Find("MuluNeonBackdrop") != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material backdropMaterial = shader != null ? new Material(shader) { name = "MuluNeonBackdropMaterial" } : null;
            if (backdropMaterial != null)
            {
                backdropMaterial.color = new Color(0.025f, 0.035f, 0.075f, 1f);
            }

            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "MuluNeonBackdrop";
            backdrop.transform.SetParent(stage, false);
            backdrop.transform.localPosition = new Vector3(0f, 1.15f, -2.85f);
            backdrop.transform.localScale = new Vector3(6.4f, 2.4f, 0.08f);
            Renderer backdropRenderer = backdrop.GetComponent<Renderer>();
            if (backdropRenderer != null && backdropMaterial != null)
            {
                backdropRenderer.sharedMaterial = backdropMaterial;
            }

            AddStageNeonBar(stage, "MuluNeonBarLeft", new Vector3(-2.95f, 1.1f, -2.78f), new Vector3(0.06f, 2.2f, 0.06f), accentColor);
            AddStageNeonBar(stage, "MuluNeonBarRight", new Vector3(2.95f, 1.1f, -2.78f), new Vector3(0.06f, 2.2f, 0.06f), new Color(1f, 0.67f, 0.18f, 1f));
            AddStageNeonBar(stage, "MuluNeonBarTop", new Vector3(0f, 2.18f, -2.78f), new Vector3(5.9f, 0.06f, 0.06f), new Color(0.45f, 0.15f, 1f, 1f));
        }

        private static void AddStageNeonBar(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Renderer renderer = bar.GetComponent<Renderer>();
            if (renderer != null && shader != null)
            {
                Material material = new Material(shader) { name = name + "Material" };
                material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 1.8f);
                }
                renderer.sharedMaterial = material;
            }
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
            }

            if (previewFrame.Find("MoveJoystick3D") == null)
            {
                CreateRuntimeJoystick(previewFrame);
            }

            if (previewFrame.Find("JumpButton") == null)
            {
                jumpButton = CreateRuntimeJumpButton(previewFrame);
            }
            else
            {
                jumpButton = previewFrame.Find("JumpButton")?.GetComponent<Button>();
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

        private Button CreateRuntimeJumpButton(RectTransform parent)
        {
            RectTransform buttonRect = CreatePanel("JumpButton", parent,
                new AnchorPreset(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f)),
                new Vector2(-28f, 278f), new Vector2(132f, 132f), new Color(0.08f, 0.52f, 1f, 0.82f));
            ApplyCircleVisual(buttonRect, new Color(0.08f, 0.52f, 1f, 0.82f), true);

            Button button = buttonRect.gameObject.AddComponent<Button>();
            Text label = CreateRuntimeText("Label", buttonRect, "JUMP", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            label.raycastTarget = false;
            return button;
        }

        private static Text CreateRuntimeText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return text;
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
