using UnityEngine;
using UnityEngine.UI;

namespace MuluAI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Mulu AI/Runtime UI Bootstrap")]
    public class MuluRuntimeUIBootstrap : MonoBehaviour
    {
        [Header("Backend")]
        public string worldId = "default-world";
        public string userId = "local-player";
        public Transform playerTransform;

        [Header("Generated Portrait UI")]
        public bool buildUiOnStart = true;
        public bool rebuildExistingCanvas = true;
        public Font uiFont;
        public Color glassColor = new Color(0.035f, 0.045f, 0.065f, 0.88f);
        public Color panelColor = new Color(0.07f, 0.085f, 0.11f, 0.94f);
        public Color accentColor = new Color(0.08f, 0.52f, 1f, 1f);
        public Color warmAccentColor = new Color(1f, 0.67f, 0.18f, 1f);
        public Color buttonColor = new Color(0.12f, 0.145f, 0.18f, 0.96f);

        private void Start()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EnsureBuilt();
            }
        }

        private void EnsureBuilt()
        {
            if (!buildUiOnStart)
            {
                return;
            }

            if (GameObject.Find("MuluMobileCanvas") != null)
            {
                return;
            }

            Build();
        }

        [ContextMenu("Build Mulu UI")]
        public void Build()
        {
            Screen.orientation = ScreenOrientation.Portrait;

            if (rebuildExistingCanvas)
            {
                DestroyExistingGeneratedCanvas();
            }

            Canvas canvas = CreateCanvas();
            RectTransform safeRoot = CreatePanel("SafeAreaRoot", canvas.transform, AnchorStretch(), Vector2.zero, Vector2.zero, Color.clear);
            AddSafeAreaIfAvailable(safeRoot.gameObject);

            MuluWebClient webClient = GetOrAdd<MuluWebClient>(gameObject);
            AssetPlacementManager placementManager = GetOrAdd<AssetPlacementManager>(gameObject);
            DynamicHUDController hudController = GetOrAdd<DynamicHUDController>(gameObject);
            EnvironmentController environmentController = GetOrAdd<EnvironmentController>(gameObject);
            MuluPromptBuildController promptController = GetOrAdd<MuluPromptBuildController>(gameObject);

            Transform runtimeCharacter = BuildCharacterStage();

            RectTransform chatShell = CreatePanel(
                "AICommandConsole",
                safeRoot,
                new AnchorPreset(new Vector2(0f, 0.34f), new Vector2(1f, 1f), new Vector2(0.5f, 1f)),
                new Vector2(0f, -26f),
                new Vector2(-48f, -28f),
                glassColor);

            CreateHeader(chatShell);

            ScrollRect chatScroll = CreateChatScroll(chatShell, out RectTransform chatContent);
            MuluChatLogView chatLog = chatShell.gameObject.AddComponent<MuluChatLogView>();
            ReflectionSet(chatLog, "content", chatContent);
            ReflectionSet(chatLog, "scrollRect", chatScroll);
            ReflectionSet(chatLog, "font", ResolveFont());
            chatLog.AddAiMessage("Tell me what to build. I can change controls, spawn assets, and reshape the scene from your prompt.");

            Text narrative = CreateText("NarrativeMirror", chatShell, "", 20, FontStyle.Normal, TextAnchor.UpperLeft);
            narrative.color = new Color(1f, 1f, 1f, 0f);
            SetRect(narrative.rectTransform, new AnchorPreset(new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero), Vector2.zero, Vector2.zero);

            RectTransform composer = CreatePanel(
                "PromptComposer",
                safeRoot,
                new AnchorPreset(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f)),
                new Vector2(0f, 26f),
                new Vector2(-48f, 178f),
                panelColor);

            InputField input = CreateInput(composer);
            SetRect(input.GetComponent<RectTransform>(), new AnchorPreset(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f)), new Vector2(-118f, 16f), new Vector2(-268f, 96f));

            Button submit = CreateButton("BuildButton", composer, "Build", accentColor, 30);
            SetRect(submit.GetComponent<RectTransform>(), new AnchorPreset(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)), new Vector2(-24f, 16f), new Vector2(212f, 96f));

            Text status = CreateText("StatusLine", composer, "Ready", 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            status.color = new Color(0.76f, 0.84f, 0.94f, 1f);
            SetRect(status.rectTransform, new AnchorPreset(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)), new Vector2(28f, 18f), new Vector2(-56f, 38f));

            RectTransform buttonGrid = CreatePanel(
                "ActionButtonGrid",
                safeRoot,
                new AnchorPreset(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f)),
                new Vector2(-34f, 220f),
                new Vector2(392f, 392f),
                Color.clear);

            RectTransform joystickAnchor = CreatePanel(
                "JoystickAnchor",
                safeRoot,
                new AnchorPreset(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)),
                new Vector2(172f, 245f),
                new Vector2(300f, 300f),
                Color.clear);

            GameObject actionButtonPrefab = CreateActionButtonTemplate(canvas.transform);
            GameObject joystickPrefab = CreateJoystickTemplate(canvas.transform);

            ReflectionSet(hudController, "targetCanvas", canvas);
            ReflectionSet(hudController, "virtualJoystickPrefab", joystickPrefab);
            ReflectionSet(hudController, "actionButtonPrefab", actionButtonPrefab);
            ReflectionSet(hudController, "buttonGridContainer", buttonGrid);
            ReflectionSet(hudController, "joystickAnchor", joystickAnchor);
            ReflectionSet(hudController, "actionButtonSize", new Vector2(168f, 168f));
            ReflectionSet(hudController, "actionButtonSpacing", 18f);
            ReflectionSet(hudController, "buttonColor", buttonColor);

            ReflectionSet(promptController, "webClient", webClient);
            ReflectionSet(promptController, "assetPlacementManager", placementManager);
            ReflectionSet(promptController, "hudController", hudController);
            ReflectionSet(promptController, "environmentController", environmentController);
            ReflectionSet(promptController, "playerTransform", runtimeCharacter != null ? runtimeCharacter : playerTransform);
            ReflectionSet(promptController, "promptInputField", input);
            ReflectionSet(promptController, "submitButton", submit);
            ReflectionSet(promptController, "narrativeText", narrative);
            ReflectionSet(promptController, "statusText", status);
            ReflectionSet(promptController, "chatLogView", chatLog);
            ReflectionSet(promptController, "worldId", worldId);
            ReflectionSet(promptController, "userId", userId);

            // Build the joystick + action buttons in BOTH edit mode and at runtime.
            // Previously this was guarded by !Application.isPlaying, which meant the
            // joystick showed in the editor but was never spawned when pressing Play.
            hudController.ConfigureGamepad(new ControlsConfig
            {
                joystick_enabled = true,
                buttons = new System.Collections.Generic.List<JoystickButton>()
            });

            submit.onClick.RemoveAllListeners();
            submit.onClick.AddListener(promptController.SubmitPromptFromInput);
            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    promptController.SubmitPrompt(value);
                }
            });

            Debug.Log("Mulu ChatGPT-style portrait game UI generated.");
        }

        private Transform BuildCharacterStage()
        {
            Transform character = playerTransform;

            if (character == null)
            {
                GameObject existingCharacter = GameObject.Find("CoolPirate_EditableCharacter") ?? GameObject.Find("CharacterFallback");
                if (existingCharacter != null)
                {
                    character = existingCharacter.transform;
                }
            }

            GameObject stage = GameObject.Find("MuluRuntimeCharacterStage");
            if (stage == null)
            {
                stage = new GameObject("MuluRuntimeCharacterStage");
            }

            stage.transform.position = Vector3.zero;

            if (character == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "CharacterFallback";
                fallback.transform.SetParent(stage.transform, false);
                fallback.transform.localPosition = Vector3.zero;
                fallback.transform.localRotation = Quaternion.identity;
                fallback.transform.localScale = Vector3.one * 1.5f;
                character = fallback.transform;
            }

            if (character.parent != stage.transform)
            {
                character.SetParent(stage.transform, true);
            }

            GameObject platform = GameObject.Find("CharacterStagePlatform");
            Renderer platformRenderer = null;
            if (platform == null)
            {
                platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = "CharacterStagePlatform";
                platform.transform.SetParent(stage.transform, false);
                platform.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                platform.transform.localScale = new Vector3(6f, 0.05f, 6f);
                platformRenderer = platform.GetComponent<Renderer>();
            }
            else
            {
                platform.transform.SetParent(stage.transform, false);
                platformRenderer = platform.GetComponent<Renderer>();
            }

            if (platformRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    platformRenderer.sharedMaterial = new Material(shader)
                    {
                        name = "MuluRuntimePlatformMaterial"
                    };
                    platformRenderer.sharedMaterial.color = new Color(0.08f, 0.1f, 0.15f, 1f);
                }
            }

            Light keyLight = new GameObject("CharacterKeyLight").AddComponent<Light>();
            keyLight.transform.SetParent(stage.transform, false);
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.5f;
            keyLight.transform.localRotation = Quaternion.Euler(30f, -30f, 0f);

            Light fillLight = new GameObject("CharacterFillLight").AddComponent<Light>();
            fillLight.transform.SetParent(stage.transform, false);
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.6f;
            fillLight.transform.localRotation = Quaternion.Euler(20f, 150f, 0f);

            Light rimLight = new GameObject("CharacterRimLight").AddComponent<Light>();
            rimLight.transform.SetParent(stage.transform, false);
            rimLight.type = LightType.Directional;
            rimLight.intensity = 1.2f;
            rimLight.transform.localRotation = Quaternion.Euler(135f, 45f, 0f);

            GameObject oldMainCamera = GameObject.Find("Main Camera");
            if (oldMainCamera != null)
            {
                Destroy(oldMainCamera);
            }

            Camera cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.transform.SetParent(stage.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.0f, 4.0f);
            cam.transform.localRotation = Quaternion.Euler(3f, 180f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = -1;

            MuluCharacterConfigurator configurator = stage.GetComponent<MuluCharacterConfigurator>();
            if (configurator == null)
            {
                configurator = stage.AddComponent<MuluCharacterConfigurator>();
            }

            configurator.mainCamera = cam;
            configurator.keyLight = keyLight;
            configurator.fillLight = fillLight;
            configurator.rimLight = rimLight;
            configurator.platformRenderer = platformRenderer;
            configurator.characterTransform = character;
            configurator.ApplySettings();

            return character;
        }

        [ContextMenu("Clear Mulu UI")]
        public void ClearGeneratedUi()
        {
            DestroyExistingGeneratedCanvas();
        }

        private void CreateHeader(RectTransform parent)
        {
            RectTransform header = CreatePanel(
                "Header",
                parent,
                new AnchorPreset(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f)),
                Vector2.zero,
                new Vector2(0f, 116f),
                Color.clear);

            Text title = CreateText("Title", header, "Mulu AI Director", 38, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new AnchorPreset(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f)), new Vector2(34f, 8f), new Vector2(-320f, -22f));

            Text chip = CreateText("ModeChip", header, "LIVE BUILD", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            chip.color = Color.black;
            RectTransform chipRect = CreatePanel("ModeChipBg", header, new AnchorPreset(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)), new Vector2(-34f, 8f), new Vector2(184f, 52f), warmAccentColor);
            chip.transform.SetParent(chipRect, false);
            SetRect(chip.rectTransform, AnchorStretch(), Vector2.zero, Vector2.zero);
        }

        private ScrollRect CreateChatScroll(RectTransform parent, out RectTransform content)
        {
            RectTransform scrollRoot = CreatePanel(
                "ChatScroll",
                parent,
                new AnchorPreset(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)),
                new Vector2(0f, -58f),
                new Vector2(-34f, -132f),
                Color.clear);

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            RectTransform viewport = CreatePanel("Viewport", scrollRoot, AnchorStretch(), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.08f));
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            content = CreatePanel(
                "Content",
                viewport,
                new AnchorPreset(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f)),
                Vector2.zero,
                new Vector2(0f, 0f),
                Color.clear);

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
            return scrollRect;
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("MuluCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            return canvas;
        }

        private InputField CreateInput(Transform parent)
        {
            RectTransform inputRoot = CreatePanel("PromptInput", parent, AnchorStretch(), Vector2.zero, Vector2.zero, Color.white);
            Image image = inputRoot.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.97f);

            Text text = CreateText("Text", inputRoot, "", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
            text.color = Color.black;
            SetRect(text.rectTransform, AnchorStretch(), new Vector2(24f, 0f), new Vector2(-48f, -10f));

            Text placeholder = CreateText("Placeholder", inputRoot, "Ask Mulu to build your scene...", 28, FontStyle.Italic, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0f, 0f, 0f, 0.42f);
            SetRect(placeholder.rectTransform, AnchorStretch(), new Vector2(24f, 0f), new Vector2(-48f, -10f));

            InputField input = inputRoot.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private Button CreateButton(string name, Transform parent, string label, Color color, int fontSize)
        {
            RectTransform root = CreatePanel(name, parent, AnchorStretch(), Vector2.zero, Vector2.zero, color);
            Button button = root.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            button.colors = colors;

            Text text = CreateText("Label", root, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, AnchorStretch(), Vector2.zero, Vector2.zero);
            return button;
        }

        private GameObject CreateActionButtonTemplate(Transform parent)
        {
            Button button = CreateButton("ActionButtonTemplate", parent, "Action", buttonColor, 26);
            button.gameObject.SetActive(false);
            return button.gameObject;
        }

        private GameObject CreateJoystickTemplate(Transform parent)
        {
            RectTransform root = CreatePanel("JoystickTemplate", parent, AnchorStretch(), Vector2.zero, new Vector2(270f, 270f), new Color(0.02f, 0.03f, 0.045f, 0.42f));
            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = true;
            }

            RectTransform ring = CreatePanel("Ring", root, new AnchorPreset(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), Vector2.zero, new Vector2(210f, 210f), new Color(1f, 1f, 1f, 0.08f));
            Image ringImage = ring.GetComponent<Image>();
            if (ringImage != null)
            {
                ringImage.raycastTarget = false;
            }

            RectTransform knob = CreatePanel("Knob", root, new AnchorPreset(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), Vector2.zero, new Vector2(105f, 105f), accentColor);
            Image knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.raycastTarget = false;
            }

            // Attach the actual joystick behaviour so the spawned joystick is interactive.
            // Without this the template was just a static panel that did nothing.
            root.gameObject.AddComponent<MuluVirtualJoystick>();

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private RectTransform CreatePanel(string name, Transform parent, AnchorPreset anchors, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetRect(rect, anchors, position, size);
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = ResolveFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Font ResolveFont()
        {
            return uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void DestroyExistingGeneratedCanvas()
        {
            GameObject existing = GameObject.Find("MuluCanvas");
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
            }
        }

        private static void AddSafeAreaIfAvailable(GameObject target)
        {
            System.Type safeAreaType = System.Type.GetType("Mulu.UI.SafeAreaApplier, Assembly-CSharp");
            if (safeAreaType != null && target.GetComponent(safeAreaType) == null)
            {
                target.AddComponent(safeAreaType);
            }
        }

        private static void SetRect(RectTransform rect, AnchorPreset anchors, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchors.Min;
            rect.anchorMax = anchors.Max;
            rect.pivot = anchors.Pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static AnchorPreset AnchorStretch()
        {
            return new AnchorPreset(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void ReflectionSet(Object target, string fieldName, object value)
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
