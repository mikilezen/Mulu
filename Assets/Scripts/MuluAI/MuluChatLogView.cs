using UnityEngine;
using UnityEngine.UI;

namespace MuluAI
{
    public class MuluChatLogView : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Font font;
        [SerializeField] private Color userBubbleColor = new Color(0.08f, 0.45f, 0.88f, 1f);
        [SerializeField] private Color aiBubbleColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        [SerializeField] private Color systemTextColor = new Color(0.72f, 0.78f, 0.86f, 1f);

        [Header("Sprites (Assigned by Builder)")]
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private Sprite avatarBgSprite;

        public void AddUserMessage(string message)
        {
            AddBubble("You", message, userBubbleColor, Color.white, TextAnchor.MiddleRight);
        }

        public void AddAiMessage(string message)
        {
            AddBubble("Mulu", message, aiBubbleColor, Color.white, TextAnchor.MiddleLeft);
        }

        public void AddSystemMessage(string message)
        {
            AddBubble("System", message, Color.clear, systemTextColor, TextAnchor.MiddleCenter);
        }

        public void Clear()
        {
            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private void AddBubble(string sender, string message, Color bubbleColor, Color textColor, TextAnchor alignment)
        {
            if (content == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Create row container
            GameObject row = new GameObject(sender + "MessageRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(content, false);

            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.padding = new RectOffset(16, 16, 8, 8);
            rowLayout.spacing = 16f;
            rowLayout.childAlignment = alignment;

            LayoutElement rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = 80f;

            // Avatars and bubbles creation order based on sender
            if (sender == "Mulu")
            {
                // AI Message: Avatar (Left) -> Bubble (Right)
                CreateAiAvatar(row.transform);
                CreateMessageBubble(row.transform, sender, message, bubbleColor, textColor);
            }
            else if (sender == "You")
            {
                // User Message: Bubble (Left) -> Avatar (Right)
                CreateMessageBubble(row.transform, sender, message, bubbleColor, textColor);
                CreateUserAvatar(row.transform);
            }
            else
            {
                // System message (no avatar, centered bubble)
                CreateMessageBubble(row.transform, sender, message, bubbleColor, textColor);
            }

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void CreateMessageBubble(Transform parent, string sender, string message, Color bubbleColor, Color textColor)
        {
            GameObject bubble = new GameObject(sender + "Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            bubble.transform.SetParent(parent, false);

            Image bubbleImage = bubble.GetComponent<Image>();
            bubbleImage.color = bubbleColor;
            bubbleImage.enabled = bubbleColor.a > 0.01f;
            if (bubbleSprite != null)
            {
                bubbleImage.sprite = bubbleSprite;
                bubbleImage.type = Image.Type.Sliced;
            }

            VerticalLayoutGroup bubbleLayout = bubble.GetComponent<VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(24, 24, 16, 18);
            bubbleLayout.spacing = 6f;
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = bubble.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement bubbleElement = bubble.GetComponent<LayoutElement>();
            bubbleElement.preferredWidth = 720f;
            bubbleElement.flexibleWidth = 0f;

            if (sender != "System")
            {
                Text senderText = CreateText("Sender", bubble.transform, sender, 20, FontStyle.Bold, textColor);
                senderText.color = Color.Lerp(textColor, systemTextColor, 0.4f);
            }

            Text bodyText = CreateText("Body", bubble.transform, message, 28, FontStyle.Normal, textColor);
            bodyText.alignment = TextAnchor.UpperLeft;
        }

        private void CreateAiAvatar(Transform parent)
        {
            GameObject avatar = new GameObject("AiAvatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(parent, false);
            
            RectTransform r = avatar.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(64, 64);

            Image img = avatar.GetComponent<Image>();
            img.color = new Color(0.1f, 0.52f, 1f, 1f); // Deep blue circular background
            if (avatarBgSprite != null)
            {
                img.sprite = avatarBgSprite;
            }

            // Sub-icon for Gemini/AI look (✦ symbol)
            Text label = CreateText("IconSymbol", avatar.transform, "✦", 28, FontStyle.Bold, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void CreateUserAvatar(Transform parent)
        {
            GameObject avatar = new GameObject("UserAvatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(parent, false);
            
            RectTransform r = avatar.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(64, 64);

            Image img = avatar.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.3f, 1f); // Slate circular background
            if (avatarBgSprite != null)
            {
                img.sprite = avatarBgSprite;
            }

            // Head (circle)
            GameObject head = new GameObject("Head", typeof(RectTransform), typeof(Image));
            head.transform.SetParent(avatar.transform, false);
            RectTransform headR = head.GetComponent<RectTransform>();
            headR.sizeDelta = new Vector2(18, 18);
            headR.anchoredPosition = new Vector2(0, 8);
            Image headImg = head.GetComponent<Image>();
            headImg.color = Color.white;
            headImg.sprite = avatarBgSprite;

            // Shoulders (circle representing body segment)
            GameObject shoulders = new GameObject("Shoulders", typeof(RectTransform), typeof(Image));
            shoulders.transform.SetParent(avatar.transform, false);
            RectTransform shouldersR = shoulders.GetComponent<RectTransform>();
            shouldersR.sizeDelta = new Vector2(36, 14);
            shouldersR.anchoredPosition = new Vector2(0, -10);
            Image shouldersImg = shoulders.GetComponent<Image>();
            shouldersImg.color = Color.white;
            shouldersImg.sprite = avatarBgSprite;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // Disable to ensure responsive click-through

            ContentSizeFitter fitter = textObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return text;
        }
    }
}
