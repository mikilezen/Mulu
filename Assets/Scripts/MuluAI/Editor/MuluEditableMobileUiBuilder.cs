using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MuluAI.Editor
{
    /// <summary>
    /// Builds a portrait-first, mobile-friendly editable UI directly into the scene.
    /// Bottom navigation bar, Claude-style chat input, scrollable chat feed, dark theme.
    /// Menu: Mulu AI ▸ Delete Old UI And Build Editable Mobile UI
    /// </summary>
    public static class MuluEditableMobileUiBuilder
    {
        private const string ScenePath = "Assets/mulu.unity";
        private const string CharacterAssetPath = "Assets/Characters/CoolPirate.fbx";

        // ═══ Color palette (ai-prompt-box dark theme) ═══
        static readonly Color COL_BG       = new Color(0.035f, 0.045f, 0.065f, 1f);
        static readonly Color COL_SURFACE  = new Color(0.122f, 0.125f, 0.137f, 1f);   // #1F2023
        static readonly Color COL_SURFACE2 = new Color(0.18f, 0.19f, 0.20f, 1f);      // #2E3033
        static readonly Color COL_NAV      = new Color(0.047f, 0.055f, 0.078f, 1f);
        static readonly Color COL_INPUT    = new Color(0.122f, 0.125f, 0.137f, 1f);   // #1F2023 (same as prompt box bg)
        static readonly Color COL_BORDER   = new Color(0.267f, 0.267f, 0.267f, 1f);   // #444444
        static readonly Color COL_ACCENT   = new Color(1f, 1f, 1f, 1f);               // White send button
        static readonly Color COL_ACCENT_BLUE = new Color(0.118f, 0.682f, 0.859f, 1f);// #1EAEDB Search
        static readonly Color COL_ACCENT_PURPLE = new Color(0.545f, 0.361f, 0.965f, 1f); // #8B5CF6 Think
        static readonly Color COL_ACCENT_ORANGE = new Color(0.976f, 0.451f, 0.086f, 1f); // #F97316 Canvas
        static readonly Color COL_TEXT     = new Color(0.937f, 0.949f, 0.969f, 1f);
        static readonly Color COL_DIM      = new Color(0.612f, 0.639f, 0.690f, 1f);   // #9CA3AF
        static readonly Color COL_PREVIEW  = new Color(0.02f, 0.03f, 0.045f, 0.92f);

        // ═══ Layout constants (1080×1920 reference) ═══
        const float HEADER_H  = 120f;
        const float NAV_H     = 150f;
        const float COMPOSE_H = 240f;  // Taller to fit the new prompt box design

        // ═══════════════════════════════════════════════════════════
        //  MAIN BUILD
        // ═══════════════════════════════════════════════════════════

        [MenuItem("Mulu AI/Delete Old UI And Build Editable Mobile UI")]
        public static void BuildMuluScene()
        {
            // Open existing scene only if not already open to avoid assembly reload restrictions
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (File.Exists(Path.Combine(Application.dataPath, "../", ScenePath)))
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // Cleanup previous builds
            DestroyNamed("MuluCanvas");
            DestroyNamed("MuluMobileCanvas");
            DestroyNamed("MuluMobileSystems");
            DestroyNamed("MuluCharacterStage");
            EnsureEventSystem();

            // ── Systems GameObject ──
            GameObject systems = new GameObject("MuluMobileSystems");
            var web    = systems.AddComponent<MuluWebClient>();
            var place  = systems.AddComponent<AssetPlacementManager>();
            var hud    = systems.AddComponent<DynamicHUDController>();
            var env    = systems.AddComponent<EnvironmentController>();
            var prompt = systems.AddComponent<MuluPromptBuildController>();
            var nav    = systems.AddComponent<MuluMobileNavigation>();

            // ── Canvas ──
            Canvas canvas = CreateCanvas();
            RectTransform root = MakeRect("MuluMobileRoot", canvas.transform,
                StretchAnchor(), Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<CanvasGroup>();

            // Add SafeArea if available
            System.Type safeAreaType = System.Type.GetType("Mulu.UI.SafeAreaApplier, Assembly-CSharp");
            if (safeAreaType != null)
            {
                root.gameObject.AddComponent(safeAreaType);
            }

            // ── Top Header ──
            RectTransform header = Box("TopHeader", root,
                Anc(0, 1, 1, 1, 0.5f, 1f), Vector2.zero, new Vector2(0, HEADER_H), COL_NAV);
            
            // Header Title
            Text titleLabel = Lbl("MuluTitle", header, "Mulu AI Director", 44, FontStyle.Bold,
                TextAnchor.MiddleLeft, COL_TEXT);
            SetR(titleLabel.rectTransform,
                Anc(0, 0, 0.5f, 1, 0, 0.5f), new Vector2(136, 0), new Vector2(0, -16));

            // Page Back Button (placed in header on left, hidden initially)
            Button pageBackBtn = Btn("PageBackButton", header, "←", COL_SURFACE2);
            SetR(pageBackBtn.GetComponent<RectTransform>(),
                Anc(0, 0.5f, 0, 0.5f, 0, 0.5f), new Vector2(24, 0), new Vector2(80, 80));
            pageBackBtn.gameObject.SetActive(false);

            // ── Bottom Navigation Bar (Sleek floating layout) ──
            RectTransform navBar = Box("BottomNav", root,
                Anc(0, 0, 1, 0, 0.5f, 0), Vector2.zero, new Vector2(0, NAV_H), COL_NAV);
            
            // Generate Account & Menu buttons (icon-only, centered)
            Button acctBtn = MakeIconTab("AccountTab", navBar, -80f, "account");
            Button menuBtn = MakeIconTab("MenuTab", navBar, 80f, "menu");

            // ── Pages (fill between header and nav bar) ──
            RectTransform homePg = MakePage("HomePage", root);
            homePg.GetComponent<Image>().color = Color.clear;
            RectTransform acctPg = MakePage("AccountPage", root);
            RectTransform menuPg = MakePage("MenuPage", root);

            BuildHome(homePg,
                out InputField pInput, out Button sendBtn, out Button micBtn,
                out MuluVirtualJoystick joystick,
                out Button fwd, out Button bck,
                out Text narrative, out Text statusText, out MuluChatLogView chatLog);
            
            BuildAccount(acctPg);
            BuildMenu(menuPg);

            if (chatLog != null)
            {
                // Wire Chat Log view sprites
                Prop(chatLog, "bubbleSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
                Prop(chatLog, "avatarBgSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
            }

            // ── Character preview stage (3D world-space) ──
            GameObject stage = BuildCharacterStage(homePg);

            // ── Wire Prompt Controller ──
            Prop(prompt, "webClient", web);
            Prop(prompt, "assetPlacementManager", place);
            Prop(prompt, "hudController", hud);
            Prop(prompt, "environmentController", env);
            Prop(prompt, "playerTransform", stage.transform.Find("CoolPirate_EditableCharacter")
                ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null));
            Prop(prompt, "promptInputField", pInput);
            Prop(prompt, "submitButton", sendBtn);
            Prop(prompt, "narrativeText", narrative);
            Prop(prompt, "statusText", statusText);
            Prop(prompt, "chatLogView", chatLog);
            PropStr(prompt, "worldId", "default-world");
            PropStr(prompt, "userId", "local-player");

            // ── Wire Navigation ──
            nav.homePage       = homePg.gameObject;
            nav.accountPage    = acctPg.gameObject;
            nav.menuPage       = menuPg.gameObject;
            nav.promptInput    = pInput;
            nav.sendButton     = sendBtn;
            nav.audioButton    = micBtn;
            nav.promptController = prompt;
            nav.accountButton  = acctBtn;
            nav.menuButton     = menuBtn;
            nav.pageBackButton = pageBackBtn;
            nav.joystick       = joystick;
            nav.forwardButton  = fwd;
            nav.backButton     = bck;
            nav.characterTransform = stage.transform.Find("CoolPirate_EditableCharacter")
                ?? (stage.transform.childCount > 0 ? stage.transform.GetChild(0) : null);

            // ── HUD Templates (hidden) ──
            var grid = Box("ActionButtonGrid", root,
                Anc(1, 0, 1, 0, 1, 0), new Vector2(-34, 220), new Vector2(392, 392), Color.clear);
            var joyA = Box("JoystickAnchor", root,
                Anc(0, 0, 0, 0, 0, 0), new Vector2(172, 245), new Vector2(300, 300), Color.clear);
            var abp = ActionBtnTemplate(canvas.transform);
            var jtp = JoystickTemplate(canvas.transform);
            Prop(hud, "targetCanvas", canvas);
            Prop(hud, "virtualJoystickPrefab", jtp);
            Prop(hud, "actionButtonPrefab", abp);
            Prop(hud, "buttonGridContainer", grid);
            Prop(hud, "joystickAnchor", joyA);

            nav.Wire();
            acctPg.gameObject.SetActive(false);
            menuPg.gameObject.SetActive(false);

            // ── Save scene ──
            Selection.activeGameObject = canvas.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Built editable portrait mobile UI with Joystick & Swipe Camera. Stage: " + stage.name);
        }

        // ═══════════════════════════════════════════════════════════
        //  PAGE BUILDERS
        // ═══════════════════════════════════════════════════════════

        private static void BuildHome(RectTransform pg,
            out InputField input, out Button send, out Button mic,
            out MuluVirtualJoystick joystick,
            out Button fwd, out Button bck,
            out Text narrative, out Text status, out MuluChatLogView chatLog)
        {
            // ── Character Preview Frame (full screen, transparent overlay for touch/joystick) ──
            var previewFrame = Box("CharacterPreviewFrame", pg,
                Anc(0, 0, 1, 1, 0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.clear);
            previewFrame.GetComponent<Image>().raycastTarget = false;
            // NOTE: No Mask here — transparent Image + Mask clips children to nothing

            // Add a subtle glass tint so the center panel does not feel empty before messages arrive.
            previewFrame.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.045f, 0.12f);

            // Narrative text (hidden by default, only shows when AI actually responds)
            narrative = Lbl("NarrativeOutput", previewFrame, "", 24, FontStyle.Normal,
                TextAnchor.MiddleCenter, COL_TEXT);
            narrative.gameObject.SetActive(false);
            SetR(narrative.rectTransform, StretchAnchor(), Vector2.zero, Vector2.zero);

            // Scrollable chat feed restored for the editable center area.
            chatLog = BuildChatScroll(previewFrame, out RectTransform chatContent);
            if (chatLog != null)
            {
                chatLog.AddSystemMessage("Chat is ready. Type a prompt below to start.");
                if (chatContent != null)
                {
                    chatContent.SetAsLastSibling();
                }
            }

            // Circular Virtual Joystick nested inside Preview Frame (bottom-left overlay)
            joystick = BuildJoystick(previewFrame);

            // Forward/Back buttons removed (movement controlled by 360-degree joystick)
            fwd = null;
            bck = null;

            // ── ai-prompt-box Style Chat Composer (flush at bottom, no gaps) ──
            Sprite roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var composer = Box("ChatComposer", pg,
                Anc(0, 0, 1, 0, 0.5f, 0),
                new Vector2(0, 0), new Vector2(-16, COMPOSE_H), COL_SURFACE);
            Image composerImg = composer.GetComponent<Image>();
            composerImg.sprite = roundSprite;
            composerImg.type = Image.Type.Sliced;
            composerImg.pixelsPerUnitMultiplier = 2f;

            // Border outline
            var border = Box("ComposerBorder", pg,
                Anc(0, 0, 1, 0, 0.5f, 0),
                new Vector2(0, -1), new Vector2(-12, COMPOSE_H + 4), COL_BORDER);
            Image borderImg = border.GetComponent<Image>();
            borderImg.sprite = roundSprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.pixelsPerUnitMultiplier = 2f;
            border.SetSiblingIndex(composer.GetSiblingIndex());
            composer.SetAsLastSibling();

            // ── Text Input Area (top portion) ──
            var inputArea = MakeRect("InputArea", composer,
                Anc(0, 0.36f, 1, 1, 0.5f, 0.5f), Vector2.zero, Vector2.zero);
            inputArea.offsetMin = new Vector2(16, 4);
            inputArea.offsetMax = new Vector2(-16, -4);

            var inputBox = Box("PromptInputBox", inputArea,
                StretchAnchor(), Vector2.zero, Vector2.zero, Color.clear);
            input = MakeInputField(inputBox, "Type your message here...");

            // ── Status line (hidden until needed) ──
            status = Lbl("StatusLine", composer, "", 18, FontStyle.Italic,
                TextAnchor.UpperLeft, COL_DIM);
            status.raycastTarget = false;
            SetR(status.rectTransform,
                Anc(0, 0.88f, 1, 1, 0, 1), new Vector2(24, -4), new Vector2(-48, 0));

            // ── Bottom Action Bar (icon buttons row) ──
            var actionBar = MakeRect("ActionBar", composer,
                Anc(0, 0, 1, 0.36f, 0.5f, 0.5f), Vector2.zero, Vector2.zero);
            actionBar.offsetMin = new Vector2(8, 2);
            actionBar.offsetMax = new Vector2(-8, -2);

            // -- LEFT SIDE ICONS --
            var attachBtn = RoundIconBtn("AttachButton", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0, 0.5f),
                new Vector2(16, 0), new Vector2(64, 64),
                "📎", COL_DIM, Color.clear, knobSprite);

            var searchBtn = RoundIconBtn("SearchToggle", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0, 0.5f),
                new Vector2(88, 0), new Vector2(64, 64),
                "🌐", COL_ACCENT_BLUE, Color.clear, knobSprite);

            var div1 = Box("Divider1", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0.5f, 0.5f),
                new Vector2(132, 0), new Vector2(3, 40),
                new Color(0.608f, 0.529f, 0.961f, 0.5f));

            var thinkBtn = RoundIconBtn("ThinkToggle", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0, 0.5f),
                new Vector2(148, 0), new Vector2(64, 64),
                "🧠", COL_ACCENT_PURPLE, Color.clear, knobSprite);

            var div2 = Box("Divider2", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0.5f, 0.5f),
                new Vector2(192, 0), new Vector2(3, 40),
                new Color(0.608f, 0.529f, 0.961f, 0.5f));

            var canvasBtn = RoundIconBtn("CanvasToggle", actionBar,
                Anc(0, 0.5f, 0, 0.5f, 0, 0.5f),
                new Vector2(208, 0), new Vector2(64, 64),
                "📂", COL_ACCENT_ORANGE, Color.clear, knobSprite);

            // -- RIGHT SIDE ICONS --
            mic = RoundIconBtn("AudioButton", actionBar,
                Anc(1, 0.5f, 1, 0.5f, 1, 0.5f),
                new Vector2(-80, 0), new Vector2(64, 64),
                "🎤", COL_DIM, Color.clear, knobSprite).GetComponent<Button>();

            send = RoundIconBtn("SendButton", actionBar,
                Anc(1, 0.5f, 1, 0.5f, 1, 0.5f),
                new Vector2(-12, 0), new Vector2(64, 64),
                "▲", new Color(0.122f, 0.125f, 0.137f, 1f), COL_ACCENT, knobSprite).GetComponent<Button>();
        }

        private static MuluVirtualJoystick BuildJoystick(RectTransform parent)
        {
            // Joystick background panel (positioned above bottom composer)
            var pad = Box("MoveJoystick3D", parent,
                Anc(0, 0, 0, 0, 0, 0),
                new Vector2(24, 264), new Vector2(180, 180),
                new Color(0.02f, 0.025f, 0.04f, 0.65f));

            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            
            // Adjust ring background image to circular Knob sprite
            Image padImage = pad.GetComponent<Image>();
            if (padImage != null)
            {
                padImage.sprite = knobSprite;
                padImage.raycastTarget = true;
            }

            // Ring overlay (thin inner border representation)
            var ring = Box("Ring", pad, StretchAnchor(), Vector2.zero, new Vector2(-20, -20), new Color(1, 1, 1, 0.08f));
            Image ringImage = ring.GetComponent<Image>();
            if (ringImage != null)
            {
                ringImage.sprite = knobSprite;
                ringImage.raycastTarget = false;
            }

            // Central Knob
            var knob = Box("Knob", pad, Anc(0.5f, 0.5f, 0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), COL_ACCENT);
            Image knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.sprite = knobSprite;
                knobImage.raycastTarget = false;
            }

            return pad.gameObject.AddComponent<MuluVirtualJoystick>();
        }

        private static void BuildForwardBackButtons(RectTransform parent, out Button fwd, out Button bck)
        {
            // Container on the bottom-right of the preview frame (positioned above bottom composer)
            var container = Box("MoveButtonsContainer", parent,
                Anc(1, 0, 1, 0, 1, 0),
                new Vector2(-16, 264), new Vector2(90, 192),
                Color.clear);

            // Forward button (▲)
            fwd = Btn("ForwardButton", container, "▲", COL_SURFACE2);
            SetR(fwd.GetComponent<RectTransform>(),
                Anc(0.5f, 1, 0.5f, 1, 0.5f, 1), new Vector2(0, 0), new Vector2(80, 80));

            // Backward button (▼)
            bck = Btn("BackButton", container, "▼", COL_SURFACE2);
            SetR(bck.GetComponent<RectTransform>(),
                Anc(0.5f, 0, 0.5f, 0, 0.5f, 0), new Vector2(0, 0), new Vector2(80, 80));
        }

        private static MuluChatLogView BuildChatScroll(RectTransform parent, out RectTransform content)
        {
            // ChatScroll container (fills between preview and composer, edge-to-edge)
            RectTransform scrollRoot = Box("ChatScroll", parent,
                Anc(0, 0.12f, 1, 0.58f, 0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            RectTransform viewport = Box("Viewport", scrollRoot,
                StretchAnchor(), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.15f));
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            content = Box("Content", viewport,
                Anc(0, 1, 1, 1, 0.5f, 1f), Vector2.zero, Vector2.zero, Color.clear);

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

            // Attach Chat Log component
            MuluChatLogView chatLog = scrollRoot.gameObject.AddComponent<MuluChatLogView>();
            Prop(chatLog, "content", content);
            Prop(chatLog, "scrollRect", scrollRect);
            Prop(chatLog, "font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

            return chatLog;
        }

        private static void BuildAccount(RectTransform pg)
        {
            var t = Lbl("AccountTitle", pg, "Account Settings", 42, FontStyle.Bold,
                TextAnchor.MiddleLeft, COL_TEXT);
            SetR(t.rectTransform,
                Anc(0.08f, 0.85f, 0.92f, 0.95f, 0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            FormInput("NameInput",     pg, "Name",
                Anc(0.08f, 0.68f, 0.92f, 0.76f, 0.5f, 0.5f));
            FormInput("EmailInput",    pg, "Email",
                Anc(0.08f, 0.56f, 0.92f, 0.64f, 0.5f, 0.5f));
            FormInput("PasswordInput", pg, "Password",
                Anc(0.08f, 0.44f, 0.92f, 0.52f, 0.5f, 0.5f));

            Button save = Btn("CreateAccountButton", pg, "Save Profile Changes", COL_ACCENT);
            SetR(save.GetComponent<RectTransform>(),
                Anc(0.08f, 0.30f, 0.92f, 0.38f, 0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
        }

        private static void BuildMenu(RectTransform pg)
        {
            var t = Lbl("MenuTitle", pg, "Menu Options", 42, FontStyle.Bold,
                TextAnchor.MiddleLeft, COL_TEXT);
            SetR(t.rectTransform,
                Anc(0.08f, 0.85f, 0.92f, 0.95f, 0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            string[] items = { "Share World", "Invite Friend", "Export Replay", "Director Settings", "Help & Support" };
            for (int i = 0; i < items.Length; i++)
            {
                Button b = Btn("Menu_" + items[i].Replace(" ", "").Replace("&", ""), pg, items[i], COL_SURFACE2);
                SetR(b.GetComponent<RectTransform>(),
                    Anc(0.08f, 0.73f - i * 0.1f, 0.92f, 0.81f - i * 0.1f, 0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
            }
        }

        // ─── CHARACTER STAGE (3D preview) ───

        private static GameObject BuildCharacterStage(RectTransform homePage)
        {
            // Transparent touch panel child inside CharacterPreviewFrame for swipe controls
            Transform frame = homePage.Find("CharacterPreviewFrame");
            GameObject touchPanel = new GameObject("CharacterCameraTouchPanel",
                typeof(RectTransform), typeof(Image));
            touchPanel.transform.SetParent(frame, false);
            touchPanel.transform.SetAsFirstSibling();
            RectTransform rr = touchPanel.GetComponent<RectTransform>();
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
            
            Image img = touchPanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.005f); // Almost clear, but still raycastable

            // Add interactive touch swipe orbit script!
            touchPanel.AddComponent<MuluCameraSwipeControl>();

            // World-space stage (placed at y=0, main scene ground level)
            GameObject stage = new GameObject("MuluCharacterStage");
            stage.transform.position = new Vector3(0, 0, 0);

            // Character model
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterAssetPath);
            if (asset != null)
            {
                GameObject model = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                model.name = "CoolPirate_EditableCharacter";
                model.transform.SetParent(stage.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 1.5f;
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "CharacterFallback";
                fallback.transform.SetParent(stage.transform, false);
                fallback.transform.localScale = Vector3.one * 1.5f;
            }

            // Circular platform disc under character
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "CharacterStagePlatform";
            platform.transform.SetParent(stage.transform, false);
            platform.transform.localPosition = new Vector3(0, -0.05f, 0);
            platform.transform.localScale = new Vector3(6f, 0.05f, 6f);
            
            // Give it a sleek dark color
            Renderer pRend = platform.GetComponent<Renderer>();
            if (pRend != null)
            {
                pRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                pRend.material.color = new Color(0.08f, 0.1f, 0.15f, 1f);
                pRend.material.SetFloat("_Metallic", 0.8f);
                pRend.material.SetFloat("_Smoothness", 0.8f);
            }

            // Key light (warm directional)
            Light keyLight = new GameObject("CharacterKeyLight").AddComponent<Light>();
            keyLight.transform.SetParent(stage.transform, false);

            // Fill light (cool directional opposite)
            Light fillLight = new GameObject("CharacterFillLight").AddComponent<Light>();
            fillLight.transform.SetParent(stage.transform, false);

            // Rim light (silhouetting backlight)
            Light rimLight = new GameObject("CharacterRimLight").AddComponent<Light>();
            rimLight.transform.SetParent(stage.transform, false);

            // ── Main Camera (renders 3D world directly to screen) ──
            // Safely remove only old scene cameras (NOT Unity Editor cameras)
            // We only target GameObjects named "Main Camera" or tagged "MainCamera"
            try
            {
                // Remove any old "Main Camera" GameObjects first
                GameObject oldMainCam;
                while ((oldMainCam = GameObject.Find("Main Camera")) != null)
                {
                    Object.DestroyImmediate(oldMainCam);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("BuildCharacterStage: Failed to clean old cameras: " + e.Message);
            }

            Camera cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.transform.SetParent(stage.transform, false);
            cam.transform.localPosition = new Vector3(0, 1.0f, 4.0f);
            cam.transform.localRotation = Quaternion.Euler(3, 180, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            cam.fieldOfView = 40;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = -1; // Render behind UI

            // Add the new Character Configurator script so the scene layout is fully editable in the inspector
            var configurator = stage.AddComponent<MuluCharacterConfigurator>();
            configurator.mainCamera = cam;
            configurator.keyLight = keyLight;
            configurator.fillLight = fillLight;
            configurator.rimLight = rimLight;
            configurator.platformRenderer = pRend;

            // Character model reference
            Transform modelTrans = stage.transform.Find("CoolPirate_EditableCharacter");
            if (modelTrans == null && stage.transform.childCount > 0)
            {
                modelTrans = stage.transform.GetChild(0);
            }
            configurator.characterTransform = modelTrans;

            // Camera swipe control
            var swipeControl = touchPanel.GetComponent<MuluCameraSwipeControl>();
            configurator.cameraSwipeControl = swipeControl;
            if (swipeControl != null)
            {
                swipeControl.cameraTransform = cam.transform;
                swipeControl.targetCharacter = modelTrans;
            }

            // Apply all initial parameters (which configures camera centering target offset/distance, platform metallic, and 3-point lights)
            configurator.ApplySettings();

            return stage;
        }

        // ─── UI HELPERS ───

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("MuluMobileCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 900;

            CanvasScaler s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080, 1920);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0f; // Match Width: locks UI relative to 1080px portrait bounds

            return c;
        }

        /// <summary>Page fills space between top header and bottom nav bar.</summary>
        private static RectTransform MakePage(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0, NAV_H);      // bottom inset for nav bar
            rect.offsetMax = new Vector2(0, -HEADER_H);   // top inset for header
            go.GetComponent<Image>().color = COL_BG;
            return rect;
        }

        /// <summary>Generates a beautiful, centered icon-only tab button in the bottom bar.</summary>
        private static Button MakeIconTab(string name, Transform parent, float xOffset, string type)
        {
            Sprite roundBoxSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // Rounded tab button container
            RectTransform bg = Box(name, parent,
                Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(xOffset, 0), new Vector2(120, 120), COL_NAV);
            
            Image bgImg = bg.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = roundBoxSprite;
            }

            Button btn = bg.gameObject.AddComponent<Button>();

            // Icon graphics container (centered inside button)
            RectTransform iconContainer = MakeRect("IconGraphic", bg, StretchAnchor(), Vector2.zero, Vector2.zero);

            if (type == "account")
            {
                // Draw a beautiful vector silhouette of a person
                // Head (circle)
                var head = Box("Head", iconContainer, Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(0, 16), new Vector2(30, 30), COL_DIM);
                if (head.GetComponent<Image>() != null) head.GetComponent<Image>().sprite = knobSprite;
                
                // Shoulders (pill shape)
                var shoulders = Box("Shoulders", iconContainer, Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(0, -18), new Vector2(56, 22), COL_DIM);
                if (shoulders.GetComponent<Image>() != null) shoulders.GetComponent<Image>().sprite = knobSprite;
            }
            else if (type == "menu")
            {
                // Draw hamburger lines
                Box("Bar1", iconContainer, Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(0, 14), new Vector2(44, 6), COL_DIM);
                Box("Bar2", iconContainer, Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(0, 0), new Vector2(44, 6), COL_DIM);
                Box("Bar3", iconContainer, Anc(0.5f, 0.5f, 0.5f, 0.5f), new Vector2(0, -14), new Vector2(44, 6), COL_DIM);
            }

            return btn;
        }

        /// <summary>Creates an InputField inside the given parent (which should have an Image bg).</summary>
        private static InputField MakeInputField(RectTransform parent, string placeholder)
        {
            Text text = Lbl("Text", parent, "", 28, FontStyle.Normal,
                TextAnchor.MiddleLeft, COL_TEXT);
            text.raycastTarget = false; // Prevents blocking input taps
            SetR(text.rectTransform, StretchAnchor(), new Vector2(24, 0), new Vector2(-48, -10));

            Text hint = Lbl("Placeholder", parent, placeholder, 28, FontStyle.Italic,
                TextAnchor.MiddleLeft, COL_DIM);
            hint.raycastTarget = false; // Prevents blocking input taps
            SetR(hint.rectTransform, StretchAnchor(), new Vector2(24, 0), new Vector2(-48, -10));

            InputField inp = parent.gameObject.AddComponent<InputField>();
            inp.textComponent = text;
            inp.placeholder = hint;
            inp.lineType = InputField.LineType.SingleLine;
            return inp;
        }

        /// <summary>Form-style input field positioned at the given anchor.</summary>
        private static void FormInput(string name, RectTransform page, string placeholder, Anchor anchor)
        {
            RectTransform box = Box(name, page, anchor, Vector2.zero, Vector2.zero, COL_INPUT);
            MakeInputField(box, placeholder);
        }

        // ── Primitive builders ──

        private static RectTransform Box(string name, Transform parent,
            Anchor anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rect = MakeRect(name, parent, anchor, pos, size);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static Button Btn(string name, Transform parent, string text, Color color)
        {
            RectTransform bg = Box(name, parent, StretchAnchor(), Vector2.zero, Vector2.zero, color);
            Button btn = bg.gameObject.AddComponent<Button>();
            Text lbl = Lbl("Label", bg, text, 26, FontStyle.Bold, TextAnchor.MiddleCenter, COL_TEXT);
            lbl.raycastTarget = false; // Disable to pass clicks to button
            SetR(lbl.rectTransform, StretchAnchor(), Vector2.zero, Vector2.zero);
            return btn;
        }

        /// <summary>Creates a circular icon button with emoji/text label (like ai-prompt-box action buttons).</summary>
        private static RectTransform RoundIconBtn(string name, Transform parent,
            Anchor anchor, Vector2 pos, Vector2 size,
            string icon, Color iconColor, Color bgColor, Sprite circleSprite)
        {
            RectTransform bg = Box(name, parent, anchor, pos, size, bgColor);
            Image bgImg = bg.GetComponent<Image>();
            if (circleSprite != null)
            {
                bgImg.sprite = circleSprite;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = true;
            }

            bg.gameObject.AddComponent<Button>();

            Text lbl = Lbl("Icon", bg, icon, 28, FontStyle.Normal, TextAnchor.MiddleCenter, iconColor);
            lbl.raycastTarget = false;
            SetR(lbl.rectTransform, StretchAnchor(), Vector2.zero, Vector2.zero);

            return bg;
        }

        private static Text Lbl(string name, Transform parent, string text,
            int size, FontStyle style, TextAnchor align, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false; // Prevent text from blocking clicks
            SetR(t.rectTransform, StretchAnchor(), Vector2.zero, Vector2.zero);
            return t;
        }

        private static RectTransform MakeRect(string name, Transform parent,
            Anchor anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetR(rect, anchor, pos, size);
            return rect;
        }

        private static void SetR(RectTransform rt, Anchor a, Vector2 pos, Vector2 sd)
        {
            rt.anchorMin = a.min;
            rt.anchorMax = a.max;
            rt.pivot = a.pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sd;
        }

        // ── Anchor helpers ──

        private static Anchor StretchAnchor()
        {
            return new Anchor(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        }

        private static Anchor Anc(float x0, float y0, float x1, float y1,
            float px = 0.5f, float py = 0.5f)
        {
            return new Anchor(new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(px, py));
        }

        // ── Serialization helpers ──

        private static void Prop(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
            {
                p.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void PropStr(Object target, string field, string value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
            {
                p.stringValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Utility ──

        private static void EnsureEventSystem()
        {
            GameObject es = GameObject.Find("EventSystem");
            if (es == null)
            {
                es = new GameObject("EventSystem");
            }

            if (es.GetComponent<EventSystem>() == null)
            {
                es.AddComponent<EventSystem>();
            }

            // Check if we should use InputSystemUIInputModule for touch responsiveness on mobile
            System.Type inputSystemUiModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiModuleType != null)
            {
                var oldModule = es.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    Object.DestroyImmediate(oldModule);
                }

                if (es.GetComponent(inputSystemUiModuleType) == null)
                {
                    es.AddComponent(inputSystemUiModuleType);
                }
            }
            else
            {
                if (es.GetComponent<StandaloneInputModule>() == null)
                {
                    es.AddComponent<StandaloneInputModule>();
                }
            }
        }

        private static GameObject ActionBtnTemplate(Transform parent)
        {
            Button b = Btn("ActionButtonTemplate", parent, "Action", COL_SURFACE2);
            b.gameObject.SetActive(false);
            return b.gameObject;
        }

        private static GameObject JoystickTemplate(Transform parent)
        {
            RectTransform r = Box("JoystickTemplate", parent, StretchAnchor(),
                Vector2.zero, new Vector2(270, 270), new Color(0.02f, 0.03f, 0.045f, 0.42f));
            Box("Ring", r, Anc(0.5f, 0.5f, 0.5f, 0.5f),
                Vector2.zero, new Vector2(210, 210), new Color(1, 1, 1, 0.08f));
            Box("Knob", r, Anc(0.5f, 0.5f, 0.5f, 0.5f),
                Vector2.zero, new Vector2(105, 105), COL_ACCENT);
            r.gameObject.SetActive(false);
            return r.gameObject;
        }

        private static void DestroyNamed(string n)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }

        // ── Anchor struct ──

        private struct Anchor
        {
            public readonly Vector2 min, max, pivot;
            public Anchor(Vector2 min, Vector2 max, Vector2 pivot)
            {
                this.min = min;
                this.max = max;
                this.pivot = pivot;
            }
        }
    }
}
