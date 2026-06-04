using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mulu.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Mulu UI/Simple UI Screen")]
    public class SimpleUIScreen : MonoBehaviour
    {
        [Header("General Settings")]
        public string appName = "Mulu";
        [TextArea(2, 4)]
        public string appSubtitle = "Production mobile app shell for a portrait-first experience.";

        [Header("Theme Colors")]
        public Color backgroundColor = new Color(0.05f, 0.07f, 0.1f, 1f);
        public Color surfaceColor = new Color(0.1f, 0.12f, 0.17f, 0.96f);
        public Color surfaceStrongColor = new Color(0.15f, 0.18f, 0.24f, 0.98f);
        public Color accentColor = new Color(0.22f, 0.64f, 0.98f, 1f);
        public Color accentSoftColor = new Color(0.22f, 0.64f, 0.98f, 0.16f);
        public Color textPrimaryColor = Color.white;
        public Color textSecondaryColor = new Color(0.78f, 0.82f, 0.88f, 1f);

        [Header("Events")]
        public UnityEvent onSettingsModalOpened = new UnityEvent();
        public UnityEvent onSettingsModalClosed = new UnityEvent();

        [Header("Serialized References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _root;
        [SerializeField] private List<RectTransform> _pages = new List<RectTransform>();
        [SerializeField] private List<Button> _tabButtons = new List<Button>();
        [SerializeField] private RectTransform _settingsModal;
        [SerializeField] private int _activeTab;

        public Canvas CanvasRef => _canvas;
        public RectTransform RootRef => _root;
        public List<RectTransform> Pages => _pages;
        public List<Button> TabButtons => _tabButtons;
        public RectTransform SettingsModal => _settingsModal;
        public int ActiveTab => _activeTab;

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
            if (_root == null)
            {
                _root = GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            InitializeTabs();
            SelectTab(_activeTab);
            if (_settingsModal != null)
            {
                _settingsModal.gameObject.SetActive(false);
            }
            ApplyTheme();
        }

        private void OnValidate()
        {
            ApplyTheme();
        }

        public void InitializeTabs()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                int index = i;
                Button button = _tabButtons[i];
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => SelectTab(index));
                }
            }
        }

        public void SelectTab(int index)
        {
            if (_pages == null || _pages.Count == 0)
            {
                return;
            }

            // Clamping active tab inside bounds
            _activeTab = Mathf.Clamp(index, 0, _pages.Count - 1);

            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                {
                    _pages[i].gameObject.SetActive(i == _activeTab);
                }
            }

            // Style active and inactive tab buttons dynamically
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                Button button = _tabButtons[i];
                if (button == null) continue;

                Image image = button.GetComponent<Image>();
                Text label = button.GetComponentInChildren<Text>();

                if (i == _activeTab)
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
        }

        public void OpenSettings()
        {
            if (_settingsModal != null)
            {
                _settingsModal.gameObject.SetActive(true);
                onSettingsModalOpened.Invoke();
            }
        }

        public void CloseSettings()
        {
            if (_settingsModal != null)
            {
                _settingsModal.gameObject.SetActive(false);
                onSettingsModalClosed.Invoke();
            }
        }

        public void ApplyTheme()
        {
            if (_canvas == null) return;

            // Apply background color to canvas background image if exists
            Image background = _canvas.GetComponent<Image>();
            if (background != null)
            {
                background.color = backgroundColor;
            }
        }

        [ContextMenu("Auto-Wire Scene References")]
        public void AutoWireReferences()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
            if (_root == null)
            {
                _root = GetComponent<RectTransform>();
            }

            // Find all pages
            Transform pagesContainer = transform.Find("Pages") ?? transform.Find("PageContainer") ?? transform;
            _pages.Clear();
            foreach (Transform child in pagesContainer)
            {
                if (child == transform || child == _canvas.transform) continue;
                if (child.name.EndsWith("Page", StringComparison.OrdinalIgnoreCase) || child.name.Contains("Page"))
                {
                    _pages.Add(child.GetComponent<RectTransform>());
                }
            }

            // Find tab buttons
            Transform tabsContainer = transform.Find("Tabs") ?? transform.Find("TabBar") ?? transform.Find("AppBar") ?? transform;
            _tabButtons.Clear();
            Button[] allButtons = tabsContainer.GetComponentsInChildren<Button>(true);
            foreach (Button btn in allButtons)
            {
                if (btn.name.EndsWith("Tab", StringComparison.OrdinalIgnoreCase) || btn.name.Contains("Tab") || btn.name.Contains("Button"))
                {
                    // Exclude mic/send button which are action components
                    if (btn.name.Contains("Send") || btn.name.Contains("Audio") || btn.name.Contains("Mic") || btn.name.Contains("Build"))
                    {
                        continue;
                    }
                    _tabButtons.Add(btn);
                }
            }

            // Find settings modal
            Transform settingsTrans = transform.Find("SettingsModal") ?? transform.Find("Settings") ?? transform.Find("Modal");
            if (settingsTrans != null)
            {
                _settingsModal = settingsTrans.GetComponent<RectTransform>();
            }

            Debug.Log($"Auto-wired references: Canvas: {_canvas != null}, Pages: {_pages.Count}, TabButtons: {_tabButtons.Count}, SettingsModal: {_settingsModal != null}");
        }

        [ContextMenu("Build UI in Scene (Editable)")]
        public void BuildUITemplate()
        {
#if UNITY_EDITOR
            Debug.Log("Building Editable SimpleUIScreen UI template...");

            // Ensure we have a canvas
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null)
                {
                    GameObject canvasObj = new GameObject("MuluCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    _canvas = canvasObj.GetComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.sortingOrder = 500;

                    CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080f, 1920f);
                    scaler.matchWidthOrHeight = 1f;

                    transform.SetParent(_canvas.transform, false);
                }
            }

            _root = GetComponent<RectTransform>();
            if (_root == null)
            {
                _root = gameObject.AddComponent<RectTransform>();
            }

            // Set root UI constraints to stretch
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = Vector2.zero;

            // Make sure target background exists on Canvas
            Image bgImage = _canvas.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = _canvas.gameObject.AddComponent<Image>();
            }
            bgImage.color = backgroundColor;

            // Create AppBar if missing
            Transform appBarTrans = transform.Find("AppBar");
            if (appBarTrans == null)
            {
                GameObject appBarObj = new GameObject("AppBar", typeof(RectTransform), typeof(Image));
                appBarObj.transform.SetParent(transform, false);
                RectTransform appBarRect = appBarObj.GetComponent<RectTransform>();
                appBarRect.anchorMin = new Vector2(0f, 1f);
                appBarRect.anchorMax = new Vector2(1f, 1f);
                appBarRect.pivot = new Vector2(0.5f, 1f);
                appBarRect.anchoredPosition = Vector2.zero;
                appBarRect.sizeDelta = new Vector2(0f, 136f);
                appBarObj.GetComponent<Image>().color = surfaceColor;
                appBarTrans = appBarObj.transform;

                // Title
                GameObject titleObj = new GameObject("MuluTitle", typeof(RectTransform), typeof(Text));
                titleObj.transform.SetParent(appBarTrans, false);
                Text titleTxt = titleObj.GetComponent<Text>();
                titleTxt.text = appName;
                titleTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleTxt.fontSize = 46;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.alignment = TextAnchor.MiddleLeft;
                titleTxt.color = Color.white;
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 0f);
                titleRect.anchorMax = new Vector2(0.45f, 1f);
                titleRect.pivot = new Vector2(0f, 0.5f);
                titleRect.anchoredPosition = new Vector2(32f, -8f);
                titleRect.sizeDelta = new Vector2(-48f, -22f);
            }

            // Create Pages container if missing
            Transform pagesContainer = transform.Find("Pages");
            if (pagesContainer == null)
            {
                GameObject pagesObj = new GameObject("Pages", typeof(RectTransform));
                pagesObj.transform.SetParent(transform, false);
                RectTransform pagesRect = pagesObj.GetComponent<RectTransform>();
                pagesRect.anchorMin = Vector2.zero;
                pagesRect.anchorMax = Vector2.one;
                pagesRect.pivot = new Vector2(0.5f, 0.5f);
                pagesRect.anchoredPosition = new Vector2(0f, -68f);
                pagesRect.sizeDelta = new Vector2(0f, -272f); // Accommodate header and bottom tabs
                pagesContainer = pagesObj.transform;
            }

            // Generate Default Pages (Home, Account, Menu)
            string[] defaultPages = { "HomePage", "AccountPage", "MenuPage" };
            _pages.Clear();
            for (int i = 0; i < defaultPages.Length; i++)
            {
                Transform pageTrans = pagesContainer.Find(defaultPages[i]);
                if (pageTrans == null)
                {
                    GameObject pageObj = new GameObject(defaultPages[i], typeof(RectTransform), typeof(Image));
                    pageObj.transform.SetParent(pagesContainer, false);
                    RectTransform pageRect = pageObj.GetComponent<RectTransform>();
                    pageRect.anchorMin = Vector2.zero;
                    pageRect.anchorMax = Vector2.one;
                    pageRect.pivot = new Vector2(0.5f, 0.5f);
                    pageRect.anchoredPosition = Vector2.zero;
                    pageRect.sizeDelta = Vector2.zero;
                    pageObj.GetComponent<Image>().color = surfaceColor;

                    // Add content description
                    GameObject descObj = new GameObject("Description", typeof(RectTransform), typeof(Text));
                    descObj.transform.SetParent(pageObj.transform, false);
                    Text descTxt = descObj.GetComponent<Text>();
                    descTxt.text = defaultPages[i].Replace("Page", " Screen Content");
                    descTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    descTxt.fontSize = 32;
                    descTxt.alignment = TextAnchor.MiddleCenter;
                    descTxt.color = textSecondaryColor;
                    RectTransform descRect = descObj.GetComponent<RectTransform>();
                    descRect.anchorMin = Vector2.zero;
                    descRect.anchorMax = Vector2.one;
                    descRect.sizeDelta = Vector2.zero;

                    pageTrans = pageObj.transform;
                }
                _pages.Add(pageTrans.GetComponent<RectTransform>());
            }

            // Create TabButtons bar if missing
            Transform tabsContainer = transform.Find("TabBar");
            if (tabsContainer == null)
            {
                GameObject tabsObj = new GameObject("TabBar", typeof(RectTransform), typeof(Image));
                tabsObj.transform.SetParent(transform, false);
                RectTransform tabsRect = tabsObj.GetComponent<RectTransform>();
                tabsRect.anchorMin = new Vector2(0f, 0f);
                tabsRect.anchorMax = new Vector2(1f, 0f);
                tabsRect.pivot = new Vector2(0.5f, 0f);
                tabsRect.anchoredPosition = Vector2.zero;
                tabsRect.sizeDelta = new Vector2(0f, 136f);
                tabsObj.GetComponent<Image>().color = surfaceColor;
                tabsContainer = tabsObj.transform;
            }

            // Generate Tab Buttons (Home, Account, Menu)
            string[] defaultTabs = { "HomeTab", "AccountTab", "MenuTab" };
            _tabButtons.Clear();
            float btnWidth = 240f;
            float spacing = 20f;
            float totalWidth = (btnWidth * defaultTabs.Length) + (spacing * (defaultTabs.Length - 1));
            float startX = -totalWidth / 2f + btnWidth / 2f;

            for (int i = 0; i < defaultTabs.Length; i++)
            {
                Transform tabTrans = tabsContainer.Find(defaultTabs[i]);
                if (tabTrans == null)
                {
                    GameObject tabObj = new GameObject(defaultTabs[i], typeof(RectTransform), typeof(Image), typeof(Button));
                    tabObj.transform.SetParent(tabsContainer, false);
                    RectTransform tabRect = tabObj.GetComponent<RectTransform>();
                    tabRect.anchorMin = new Vector2(0.5f, 0.5f);
                    tabRect.anchorMax = new Vector2(0.5f, 0.5f);
                    tabRect.pivot = new Vector2(0.5f, 0.5f);
                    tabRect.anchoredPosition = new Vector2(startX + i * (btnWidth + spacing), 0f);
                    tabRect.sizeDelta = new Vector2(btnWidth, 80f);
                    tabObj.GetComponent<Image>().color = surfaceStrongColor;

                    // Add Text label
                    GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    labelObj.transform.SetParent(tabObj.transform, false);
                    Text labelTxt = labelObj.GetComponent<Text>();
                    labelTxt.text = defaultTabs[i].Replace("Tab", "");
                    labelTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    labelTxt.fontSize = 28;
                    labelTxt.fontStyle = FontStyle.Bold;
                    labelTxt.alignment = TextAnchor.MiddleCenter;
                    labelTxt.color = textSecondaryColor;

                    RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.sizeDelta = Vector2.zero;

                    tabTrans = tabObj.transform;
                }
                _tabButtons.Add(tabTrans.GetComponent<Button>());
            }

            // Create SettingsModal if missing
            Transform settingsTrans = transform.Find("SettingsModal");
            if (settingsTrans == null)
            {
                GameObject modalObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image));
                modalObj.transform.SetParent(transform, false);
                _settingsModal = modalObj.GetComponent<RectTransform>();
                _settingsModal.anchorMin = Vector2.zero;
                _settingsModal.anchorMax = Vector2.one;
                _settingsModal.sizeDelta = Vector2.zero;
                modalObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

                // Dialog Panel
                GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panelObj.transform.SetParent(_settingsModal, false);
                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.1f, 0.25f);
                panelRect.anchorMax = new Vector2(0.9f, 0.75f);
                panelRect.sizeDelta = Vector2.zero;
                panelObj.GetComponent<Image>().color = surfaceColor;

                // Close Button
                GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                closeObj.transform.SetParent(panelObj.transform, false);
                RectTransform closeRect = closeObj.GetComponent<RectTransform>();
                closeRect.anchorMin = new Vector2(0.5f, 0.15f);
                closeRect.anchorMax = new Vector2(0.5f, 0.15f);
                closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.sizeDelta = new Vector2(180f, 64f);
                closeObj.GetComponent<Image>().color = accentColor;
                Button closeBtn = closeObj.GetComponent<Button>();
                closeBtn.onClick.AddListener(CloseSettings);

                GameObject closeLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
                closeLabel.transform.SetParent(closeObj.transform, false);
                Text closeText = closeLabel.GetComponent<Text>();
                closeText.text = "Close";
                closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                closeText.fontSize = 24;
                closeText.alignment = TextAnchor.MiddleCenter;
                closeText.color = textPrimaryColor;
                closeLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
                closeLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
                closeLabel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            }
            else
            {
                _settingsModal = settingsTrans.GetComponent<RectTransform>();
            }

            InitializeTabs();
            SelectTab(0);
            _settingsModal.gameObject.SetActive(false);

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("UI build complete! Hierarchy has been fully created in the scene.");
#endif
        }
    }
}
