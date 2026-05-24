using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class NoteHistoryPanel : MonoBehaviour
{
    private const string FilterAll = "all";
    private static readonly string[] Filters = { FilterAll, "low", "medium", "high" };

    private readonly Dictionary<string, Image> tabBackgrounds = new Dictionary<string, Image>();
    private Transform contentRoot;
    private Text emptyStateText;
    private string activeFilter = FilterAll;

    public static NoteHistoryPanel EnsureExists()
    {
        NoteHistoryPanel existing = FindObjectOfType<NoteHistoryPanel>(true);
        if (existing != null)
            return existing;

        GameObject canvasObject = new GameObject("RuntimeNoteHistoryCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(430, 932);
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        GameObject panelObject = new GameObject("NoteHistoryPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0.96f, 0.97f, 0.94f, 0.98f);

        NoteHistoryPanel panel = panelObject.AddComponent<NoteHistoryPanel>();
        panel.BuildUi(panelObject.transform);
        panel.gameObject.SetActive(false);
        return panel;
    }

    public void Open()
    {
        activeFilter = FilterAll;
        gameObject.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void SetFilter(string filter)
    {
        activeFilter = string.IsNullOrWhiteSpace(filter) ? FilterAll : filter;
        Refresh();
    }

    private void Refresh()
    {
        UpdateTabs();
        ClearContent();

        List<NoteData> notes = NoteManager.Instance == null
            ? new List<NoteData>()
            : NoteManager.Instance.GetAllNotes()
                .Where(note => note != null)
                .Select(note =>
                {
                    note.ApplyDefaults();
                    return note;
                })
                .Where(MatchesFilter)
                .OrderBy(note => GetPriorityRank(note.priorityId))
                .ThenBy(note => note.title, StringComparer.OrdinalIgnoreCase)
                .ToList();

        emptyStateText.gameObject.SetActive(notes.Count == 0);
        if (notes.Count == 0)
            return;

        NoteStyleManager styleManager = FindObjectOfType<NoteStyleManager>(true);
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        foreach (NoteData note in notes)
            CreateNoteRow(contentRoot, font, styleManager, note);
    }

    private bool MatchesFilter(NoteData note)
    {
        if (note == null)
            return false;

        if (activeFilter == FilterAll)
            return true;

        return Normalize(note.priorityId) == activeFilter;
    }

    private void OpenNote(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id))
            return;

        NoteView view = NoteManager.Instance == null ? null : NoteManager.Instance.GetView(note.id);
        Close();

        if (view != null)
        {
            view.OpenEditor();
            return;
        }

        NoteEditPanel.EnsureExists().Open(note);
    }

    private void BuildUi(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateBlock(parent, "HistoryHeader", new Vector2(0, -26), new Vector2(430, 52), new Color(0.1f, 0.12f, 0.12f, 1f));
        CreateButton(parent, "<", font, new Vector2(-185, -26), new Vector2(42, 38), Close, new Color(0f, 0f, 0f, 0f), Color.white, 26);
        Text title = CreateLabel(parent, "Note History", font, 20, new Vector2(0, -26), new Vector2(260, 38), Color.white, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;

        CreateBlock(parent, "TabBarBackground", new Vector2(0, -78), new Vector2(390, 36), new Color(0.87f, 0.89f, 0.84f, 1f));
        for (int i = 0; i < Filters.Length; i++)
        {
            string filter = Filters[i];
            Button tab = CreateButton(parent, ToTitle(filter), font, new Vector2(-146 + i * 97, -78), new Vector2(88, 28), () => SetFilter(filter), new Color(1f, 1f, 1f, 0.45f), new Color(0.12f, 0.14f, 0.13f, 1f), 13);
            tabBackgrounds[filter] = tab.GetComponent<Image>();
        }

        GameObject viewport = new GameObject("HistoryViewport");
        viewport.transform.SetParent(parent, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.5f, 0f);
        viewportRect.anchorMax = new Vector2(0.5f, 1f);
        viewportRect.offsetMin = new Vector2(-197, 16);
        viewportRect.offsetMax = new Vector2(197, -104);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = new GameObject("HistoryContent");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0, 0);
        contentRect.offsetMax = new Vector2(0, 0);
        contentRoot = content.transform;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 4, 10);
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        emptyStateText = CreateLabel(parent, "No notes in this priority", font, 17, new Vector2(0, -270), new Vector2(300, 42), new Color(0.38f, 0.42f, 0.38f, 1f), TextAnchor.MiddleCenter);
    }

    private void CreateNoteRow(Transform parent, Font font, NoteStyleManager styleManager, NoteData note)
    {
        bool isCompleted = note != null && note.isCompleted;
        Color rowBackgroundColor = isCompleted ? new Color(0.88f, 0.89f, 0.86f, 1f) : Color.white;
        Color primaryTextColor = isCompleted ? new Color(0.42f, 0.44f, 0.42f, 1f) : new Color(0.09f, 0.11f, 0.1f, 1f);
        Color secondaryTextColor = isCompleted ? new Color(0.58f, 0.6f, 0.57f, 1f) : new Color(0.42f, 0.46f, 0.43f, 1f);

        GameObject row = new GameObject("HistoryNoteRow");
        row.transform.SetParent(parent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(382, 68);

        Image background = row.AddComponent<Image>();
        background.color = rowBackgroundColor;

        Button button = row.AddComponent<Button>();
        RuntimeButtonActionRelay relay = row.AddComponent<RuntimeButtonActionRelay>();
        relay.Configure(() => OpenNote(note));
        button.onClick.AddListener(relay.Invoke);

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 68;
        layout.minHeight = 68;

        bool hasPriority = !string.IsNullOrWhiteSpace(note.priorityId);
        if (hasPriority)
        {
            Image colorStrip = CreateChildBlock(row.transform, "PriorityStrip", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(5, 52), isCompleted ? new Color(0.62f, 0.64f, 0.61f, 1f) : GetPriorityColor(note.priorityId));
            colorStrip.rectTransform.anchoredPosition = new Vector2(10, 0);
        }

        bool hasIcon = !string.IsNullOrWhiteSpace(note.iconId);
        if (hasIcon)
        {
            Sprite iconSprite = GetIconSprite(styleManager, note.iconId);
            Image icon = CreateChildBlock(row.transform, "NoteIcon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30, 30), isCompleted ? new Color(0.78f, 0.8f, 0.76f, 1f) : new Color(0.93f, 0.95f, 0.92f, 1f));
            icon.rectTransform.anchoredPosition = new Vector2(34, 0);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            if (iconSprite != null && isCompleted)
                icon.color = new Color(0.72f, 0.74f, 0.7f, 1f);
            Text iconFallback = CreateChildLabel(icon.transform, GetIconFallbackLabel(note.iconId), font, 10, isCompleted ? secondaryTextColor : new Color(0.22f, 0.25f, 0.23f, 1f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            iconFallback.gameObject.SetActive(iconSprite == null);
        }

        float textLeft = hasIcon ? 58f : 28f;
        float textRight = hasPriority ? -86f : -38f;
        Text title = CreateChildLabel(row.transform, GetDisplayTitle(note), font, 15, primaryTextColor, TextAnchor.MiddleLeft, new Vector2(textLeft, 31), new Vector2(textRight, -10));
        title.fontStyle = FontStyle.Bold;
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Truncate;

        Text reminder = CreateChildLabel(row.transform, GetReminderText(note), font, 11, secondaryTextColor, TextAnchor.MiddleLeft, new Vector2(textLeft, 12), new Vector2(textRight, -38));
        reminder.gameObject.SetActive(!string.IsNullOrWhiteSpace(reminder.text));

        if (hasPriority)
        {
            Image priorityBadge = CreateChildBlock(row.transform, "PriorityBadge", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(64, 24), isCompleted ? new Color(0.64f, 0.66f, 0.63f, 0.88f) : GetPriorityBadgeColor(note.priorityId));
            priorityBadge.rectTransform.anchoredPosition = new Vector2(-40, 12);
            Sprite prioritySprite = GetPrioritySprite(styleManager, note.priorityId);
            if (prioritySprite != null)
            {
                priorityBadge.sprite = prioritySprite;
                priorityBadge.preserveAspect = true;
                priorityBadge.color = isCompleted ? new Color(0.74f, 0.76f, 0.72f, 1f) : Color.white;
            }

            Text priorityText = CreateChildLabel(priorityBadge.transform, ToTitle(note.priorityId), font, 11, isCompleted ? new Color(0.88f, 0.89f, 0.86f, 1f) : Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            priorityText.gameObject.SetActive(prioritySprite == null);
        }

        CreateChildLabel(row.transform, ">", font, 20, isCompleted ? secondaryTextColor : new Color(0.44f, 0.48f, 0.45f, 1f), TextAnchor.MiddleCenter, new Vector2(350, 8), new Vector2(-8, -38));
    }

    private void UpdateTabs()
    {
        foreach (KeyValuePair<string, Image> tab in tabBackgrounds)
        {
            if (tab.Value == null)
                continue;

            bool selected = tab.Key == activeFilter;
            tab.Value.color = selected ? new Color(0.12f, 0.16f, 0.14f, 1f) : new Color(1f, 1f, 1f, 0.5f);
            Text label = tab.Value.GetComponentInChildren<Text>();
            if (label != null)
                label.color = selected ? Color.white : new Color(0.12f, 0.14f, 0.13f, 1f);
        }
    }

    private void ClearContent()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private static Image CreateBlock(Transform parent, string name, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Image CreateChildBlock(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 dimensions, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = dimensions;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityAction action, Color backgroundColor, Color textColor, int fontSize)
    {
        GameObject obj = new GameObject(label + "Button");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Image image = obj.AddComponent<Image>();
        image.color = backgroundColor;
        Button button = obj.AddComponent<Button>();
        RuntimeButtonActionRelay relay = obj.AddComponent<RuntimeButtonActionRelay>();
        relay.Configure(action);
        button.onClick.AddListener(relay.Invoke);
        CreateChildLabel(obj.transform, label, font, fontSize, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Text CreateLabel(Transform parent, string text, Font font, int size, Vector2 position, Vector2 dimensions, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(text + "Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static Text CreateChildLabel(Transform parent, string text, Font font, int size, Color color, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(text + "Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<BaseInputModule>() == null)
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static string GetDisplayTitle(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.title))
            return "New Note";

        return note.title;
    }

    private static string GetReminderText(NoteData note)
    {
        if (note == null || !note.hasReminder || string.IsNullOrWhiteSpace(note.reminderTime))
            return "";

        if (ReminderManager.TryParseReminderTime(note.reminderTime, out DateTime reminderTime))
            return "Reminder " + reminderTime.ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);

        return "Reminder " + note.reminderTime;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static int GetPriorityRank(string priorityId)
    {
        switch (Normalize(priorityId))
        {
            case "high": return 0;
            case "medium": return 1;
            case "low": return 2;
            default: return 3;
        }
    }

    private static Color GetPriorityColor(string priorityId)
    {
        switch (Normalize(priorityId))
        {
            case "high": return new Color(0.78f, 0.12f, 0.14f, 1f);
            case "medium": return new Color(0.9f, 0.63f, 0.16f, 1f);
            case "low": return new Color(0.24f, 0.61f, 0.34f, 1f);
            default: return new Color(0.55f, 0.59f, 0.56f, 1f);
        }
    }

    private static Color GetPriorityBadgeColor(string priorityId)
    {
        Color color = GetPriorityColor(priorityId);
        color.a = 0.92f;
        return color;
    }

    private static Sprite GetIconSprite(NoteStyleManager styleManager, string iconId)
    {
        if (styleManager == null)
            return null;

        switch (Normalize(iconId))
        {
            case "star": return styleManager.starIcon;
            case "finish": return styleManager.finishIcon;
            case "inprocess": return styleManager.inprocessIcon;
            case "reminder": return styleManager.reminderIcon;
            case "work": return styleManager.workIcon;
            case "study": return styleManager.studyIcon;
            case "shopping": return styleManager.shoppingIcon;
            default: return null;
        }
    }

    private static Sprite GetPrioritySprite(NoteStyleManager styleManager, string priorityId)
    {
        if (styleManager == null)
            return null;

        switch (Normalize(priorityId))
        {
            case "high": return styleManager.priorityHighSprite;
            case "medium": return styleManager.priorityMediumSprite;
            case "low": return styleManager.priorityLowSprite;
            default: return null;
        }
    }

    private static string GetIconFallbackLabel(string iconId)
    {
        switch (Normalize(iconId))
        {
            case "star": return "Star";
            case "finish": return "Done";
            case "inprocess": return "Doing";
            case "reminder": return "Bell";
            case "work": return "Work";
            case "study": return "Study";
            case "shopping": return "Shop";
            default: return "Note";
        }
    }
}
