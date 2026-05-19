using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class NoteEditPanel : MonoBehaviour
{
    private static Sprite circleSprite;

    private readonly Dictionary<string, Image> colorSwatches = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> iconButtons = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> iconSprites = new Dictionary<string, Image>();
    private readonly Dictionary<string, Text> iconFallbackLabels = new Dictionary<string, Text>();
    private readonly Dictionary<string, Image> priorityButtons = new Dictionary<string, Image>();
    private readonly Dictionary<string, Text> colorLabels = new Dictionary<string, Text>();

    private NoteView currentNote;
    private Image panelBackground;
    private InputField noteInput;
    private InputField titleInput;
    private InputField reminderDateInput;
    private InputField reminderTimeInput;
    private Toggle reminderToggle;
    private Text speechStatusText;
    private GameObject launcherRoot;

    private string selectedColorName = "yellow";
    private string selectedIconId = "";
    private string selectedPriorityId = "";

    private static readonly string[] ColorNames = { "yellow", "pink", "blue", "green" };
    private static readonly string[] IconIds = { "star", "finish", "inprocess", "reminder", "work", "study", "shopping" };
    private static readonly string[] PriorityIds = { "low", "medium", "high" };

    public static NoteEditPanel EnsureExists()
    {
        NoteEditPanel existing = FindObjectOfType<NoteEditPanel>(true);
        if (existing != null)
            return existing;

        GameObject canvasObject = new GameObject("RuntimeNoteEditCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        GameObject panelObject = CreatePanel(canvasObject.transform);
        NoteEditPanel panel = panelObject.AddComponent<NoteEditPanel>();
        panel.panelBackground = panelObject.GetComponent<Image>();
        panel.BuildUi(panelObject.transform);
        panel.gameObject.SetActive(false);
        panel.launcherRoot = CreateLauncher(canvasObject.transform, panel);
        return panel;
    }

    public void Open(NoteView noteView)
    {
        currentNote = noteView;
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteManager.Instance?.SelectNote(currentNote);
        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(currentNote);

        NoteData data = currentNote.Data;
        data.ApplyDefaults();

        titleInput.text = data.title == "New Note" ? "" : data.title;
        noteInput.text = data.content;
        selectedColorName = NormalizeChoice(data.colorName, "yellow", ColorNames);
        selectedIconId = NormalizeChoice(data.iconId, "", IconIds);
        selectedPriorityId = NormalizeChoice(data.priorityId, "", PriorityIds);
        reminderToggle.isOn = data.hasAlarm;
        SetReminderInputs(data.alarmTime);
        UpdateSelectionVisuals();
        UpdateIconSprites();
        UpdateSpeechStatus("");

        gameObject.SetActive(true);
        SetLauncherVisible(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        SetLauncherVisible(true);
    }

    public void OpenSelected()
    {
        NoteView selected = NoteManager.Instance == null ? null : NoteManager.Instance.SelectedNote ?? NoteManager.Instance.GetFirstView();
        if (selected != null)
        {
            Open(selected);
            return;
        }

        Debug.Log("Edit Note clicked with no selected note; creating a center-screen note for editing.");
        PlaceNote placeNote = FindObjectOfType<PlaceNote>();
        if (placeNote != null)
        {
            NoteView note = placeNote.CreateCenterScreenNote();
            if (note != null)
                Open(note);
        }
    }

    private void Save()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteData data = currentNote.Data;
        string noteText = GetInputValue(noteInput);
        data.content = noteText;
        data.title = string.IsNullOrWhiteSpace(GetInputValue(titleInput)) ? GenerateTitleFromContent(noteText) : GetInputValue(titleInput);
        data.annotation = "";
        data.colorName = selectedColorName;
        data.colorLabel = selectedColorName;
        data.iconId = selectedIconId;
        data.priorityId = selectedPriorityId;
        data.hasAlarm = reminderToggle.isOn;
        data.alarmTime = BuildAlarmTime();
        data.alarmRepeatRule = AlarmManager.RepeatNone;
        data.hasReminder = data.hasAlarm;
        data.reminderTime = data.alarmTime;

        if (data.hasAlarm && !AlarmManager.TryParseAlarmTime(data.alarmTime, out DateTime _))
        {
            Debug.LogWarning("Alarm time must be like 2026-05-25 09:30.");
            data.hasAlarm = false;
            data.hasReminder = false;
            reminderToggle.isOn = false;
        }

        currentNote.SaveAndRefresh();
        Close();
    }

    private void DeleteCurrent()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        string noteId = currentNote.Data.id;
        Close();
        NoteManager.Instance?.RemoveNote(noteId);
    }

    private void DictateNote()
    {
        if (noteInput == null)
            return;

        UpdateSpeechStatus("Listening...");
        SpeechToTextManager.Instance?.StartDictation(
            text =>
            {
                string currentText = GetInputValue(noteInput);
                noteInput.text = string.IsNullOrWhiteSpace(currentText) ? text : currentText + " " + text;

                if (currentNote != null && currentNote.Data != null)
                {
                    currentNote.Data.hasTranscript = true;
                    currentNote.Data.transcriptText = text;
                    currentNote.Data.transcriptSource = "content";
                    currentNote.Data.transcriptUpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                UpdateSpeechStatus("Speech added to note.");
            },
            error => UpdateSpeechStatus(error));
    }

    private void SelectColor(string colorName)
    {
        selectedColorName = colorName;
        UpdateSelectionVisuals();
    }

    private void SelectIcon(string iconId)
    {
        selectedIconId = iconId;
        UpdateSelectionVisuals();
    }

    private void SelectPriority(string priorityId)
    {
        selectedPriorityId = priorityId;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        foreach (KeyValuePair<string, Image> swatch in colorSwatches)
        {
            bool selected = swatch.Key == selectedColorName;
            if (swatch.Value != null)
                swatch.Value.color = GetColorValue(swatch.Key, selected ? 1f : 0.72f);

            if (colorLabels.TryGetValue(swatch.Key, out Text label) && label != null)
                label.text = selected ? "✓" : "";
        }

        foreach (KeyValuePair<string, Image> icon in iconButtons)
        {
            if (icon.Value != null)
                icon.Value.color = icon.Key == selectedIconId ? new Color(0.75f, 0.9f, 0.72f, 1f) : new Color(1f, 1f, 1f, 0.35f);
        }

        foreach (KeyValuePair<string, Image> priority in priorityButtons)
        {
            if (priority.Value == null) continue;

            if (priority.Key == selectedPriorityId)
                priority.Value.color = priority.Key == "high" ? new Color(0.8f, 0.12f, 0.14f, 1f) : new Color(0.75f, 0.9f, 0.72f, 1f);
            else
                priority.Value.color = new Color(1f, 1f, 1f, 0.45f);
        }

        if (panelBackground != null)
            panelBackground.color = GetPanelColor(selectedColorName);
    }

    private void UpdateIconSprites()
    {
        NoteStyleManager styleManager = currentNote == null ? null : currentNote.GetComponentInChildren<NoteStyleManager>(true);

        foreach (string iconId in IconIds)
        {
            Sprite sprite = GetIconSprite(styleManager, iconId);
            if (iconSprites.TryGetValue(iconId, out Image image) && image != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.gameObject.SetActive(sprite != null);
            }

            if (iconFallbackLabels.TryGetValue(iconId, out Text label) && label != null)
                label.gameObject.SetActive(sprite == null);
        }
    }

    private void UpdateSpeechStatus(string message)
    {
        if (speechStatusText == null)
            return;

        speechStatusText.text = string.IsNullOrWhiteSpace(message) ? "Speech input ready" : message;
    }

    private void BuildUi(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateBlock(parent, "HeaderBar", new Vector2(0, -28), new Vector2(430, 58), new Color(0.09f, 0.1f, 0.11f, 0.96f));
        CreateButton(parent, "X", font, new Vector2(-185, -28), new Vector2(46, 38), Close, new Color(0f, 0f, 0f, 0f), Color.white, 26);
        CreateTopAnchoredLabel(parent, "Edit AR Note", font, 24, new Vector2(0, -28), new Vector2(260, 38), Color.white);
        CreateButton(parent, "OK", font, new Vector2(185, -28), new Vector2(54, 38), Save, new Color(0f, 0f, 0f, 0f), Color.white, 20);

        CreateRowLabel(parent, "Title", font, -102);
        titleInput = CreateInput(parent, "", font, new Vector2(72, -102), new Vector2(280, 36), false, 80);

        CreateRowLabel(parent, "Note", font, -162);
        noteInput = CreateInput(parent, "", font, new Vector2(72, -168), new Vector2(280, 86), true, 4000);

        CreateRowLabel(parent, "Color", font, -250);
        BuildColorRow(parent, font, -250);

        CreateRowLabel(parent, "Icon", font, -306);
        BuildIconRow(parent, font, -306);

        CreateRowLabel(parent, "Priority", font, -366);
        BuildPriorityRow(parent, font, -366);

        CreateRowLabel(parent, "Reminder", font, -426);
        reminderDateInput = CreateInput(parent, "", font, new Vector2(18, -426), new Vector2(136, 34), false, 10);
        reminderTimeInput = CreateInput(parent, "", font, new Vector2(154, -426), new Vector2(88, 34), false, 5);
        CreateTopAnchoredLabel(parent, "yyyy-MM-dd", font, 11, new Vector2(18, -453), new Vector2(136, 18), new Color(0.25f, 0.25f, 0.22f, 1f));
        CreateTopAnchoredLabel(parent, "HH:mm", font, 11, new Vector2(154, -453), new Vector2(88, 18), new Color(0.25f, 0.25f, 0.22f, 1f));
        reminderToggle = CreateToggle(parent, "Enable Reminder", font, new Vector2(72, -492));

        CreateRowLabel(parent, "Speech", font, -554);
        CreateButton(parent, "Mic", font, new Vector2(-12, -554), new Vector2(86, 36), DictateNote, new Color(0.95f, 0.95f, 0.86f, 1f), Color.black, 15);
        speechStatusText = CreateTopAnchoredLabel(parent, "Speech input ready", font, 13, new Vector2(120, -554), new Vector2(166, 30), Color.black);

        CreateButton(parent, "Delete", font, new Vector2(-120, -616), new Vector2(96, 38), DeleteCurrent, new Color(0.95f, 0.95f, 0.86f, 1f), new Color(0.75f, 0.1f, 0.1f, 1f), 16);
        CreateButton(parent, "Save Note", font, new Vector2(100, -616), new Vector2(140, 40), Save, new Color(0.22f, 0.72f, 0.32f, 1f), Color.white, 17);
    }

    private void BuildColorRow(Transform parent, Font font, float y)
    {
        colorSwatches.Clear();
        colorLabels.Clear();

        for (int i = 0; i < ColorNames.Length; i++)
        {
            string colorName = ColorNames[i];
            Button button = CreateButton(parent, "", font, new Vector2(-15 + i * 64, y), new Vector2(44, 44), () => SelectColor(colorName), GetColorValue(colorName, 0.72f), Color.black, 16);
            Image image = button.GetComponent<Image>();
            Text label = button.GetComponentInChildren<Text>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            label.color = Color.white;
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            colorSwatches[colorName] = image;
            colorLabels[colorName] = label;
        }
    }

    private void BuildIconRow(Transform parent, Font font, float y)
    {
        iconButtons.Clear();

        for (int i = 0; i < IconIds.Length; i++)
        {
            string iconId = IconIds[i];
            Button button = CreateButton(parent, "", font, new Vector2(-50 + i * 40, y), new Vector2(36, 34), () => SelectIcon(iconId), new Color(1f, 1f, 1f, 0.35f), Color.black, 10);
            Image spriteImage = CreateChildImage(button.transform, iconId + "Icon", new Vector2(24, 24), Color.white);
            Text fallbackLabel = CreateCenteredChildLabel(button.transform, GetIconFallbackLabel(iconId), font, 10, Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            iconButtons[iconId] = button.GetComponent<Image>();
            iconSprites[iconId] = spriteImage;
            iconFallbackLabels[iconId] = fallbackLabel;
        }
    }

    private void BuildPriorityRow(Transform parent, Font font, float y)
    {
        priorityButtons.Clear();

        for (int i = 0; i < PriorityIds.Length; i++)
        {
            string priorityId = PriorityIds[i];
            Button button = CreateButton(parent, ToTitle(priorityId), font, new Vector2(-34 + i * 94, y), new Vector2(82, 38), () => SelectPriority(priorityId), new Color(1f, 1f, 1f, 0.45f), Color.black, 15);
            priorityButtons[priorityId] = button.GetComponent<Image>();
        }
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("NoteEditPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(430, 685);

        Image image = panel.AddComponent<Image>();
        image.color = GetPanelColor("yellow");
        return panel;
    }

    private static GameObject CreateLauncher(Transform parent, NoteEditPanel panel)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject launcher = new GameObject("NoteEditLauncher");
        launcher.transform.SetParent(parent, false);
        RectTransform rect = launcher.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        CreateButton(launcher.transform, "Create Note", font, new Vector2(-230, -40), new Vector2(120, 40), panel.CreateCenterScreenNote);
        CreateButton(launcher.transform, "Edit Note", font, new Vector2(-230, -88), new Vector2(120, 40), panel.OpenSelected);
        CreateButton(launcher.transform, "Clear DB", font, new Vector2(-230, -136), new Vector2(120, 40), panel.ClearAllData);
        return launcher;
    }

    private void ClearAllData()
    {
        if (NoteManager.Instance == null)
            return;

        Close();
        currentNote = null;
        NoteManager.Instance.ClearAllNotesAndData();
        Debug.Log("All notes and local voice files have been cleared.");
    }

    private void CreateCenterScreenNote()
    {
        PlaceNote placeNote = FindObjectOfType<PlaceNote>();
        if (placeNote == null)
        {
            Debug.LogWarning("No PlaceNote component found in the scene. Open MainScene before creating a center-screen note.");
            return;
        }

        NoteView note = placeNote.CreateCenterScreenNote();
        if (note != null)
            Open(note);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<StandaloneInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("StandaloneInputModule added to existing EventSystem for note UI compatibility.");
            }
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Debug.Log("Runtime EventSystem created for note UI.");
    }

    private void SetLauncherVisible(bool visible)
    {
        if (launcherRoot != null)
            launcherRoot.SetActive(visible);
    }

    private static Text CreateRowLabel(Transform parent, string text, Font font, float y)
    {
        Text label = CreateTopAnchoredLabel(parent, text, font, 16, new Vector2(-154, y), new Vector2(90, 28), Color.black);
        label.alignment = TextAnchor.MiddleLeft;
        return label;
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

    private static Text CreateTopAnchoredLabel(Transform parent, string text, Font font, int size, Vector2 position, Vector2 dimensions, Color color)
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
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Image CreateChildImage(Transform parent, string name, Vector2 dimensions, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateCenteredChildLabel(Transform parent, string text, Font font, int size, Color color, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
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

    private static InputField CreateInput(Transform parent, string placeholder, Font font, Vector2 position, Vector2 dimensions, bool multiline = false, int characterLimit = 0)
    {
        GameObject obj = new GameObject((string.IsNullOrEmpty(placeholder) ? "Text" : placeholder) + "Input");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(1f, 0.99f, 0.88f, 1f);

        InputField input = obj.AddComponent<InputField>();
        Text text = CreateInputText(obj.transform, "Text", font, Color.black);
        Text placeholderText = CreateInputText(obj.transform, "Placeholder", font, new Color(0.45f, 0.45f, 0.45f));
        text.text = "";
        placeholderText.text = placeholder;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.text = "";
        input.characterLimit = characterLimit;
        input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
        input.contentType = InputField.ContentType.Standard;
        return input;
    }

    private static Text CreateInputText(Transform parent, string name, Font font, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 5);
        rect.offsetMax = new Vector2(-8, -5);

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 16;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        return text;
    }

    private static Toggle CreateToggle(Transform parent, string label, Font font, Vector2 position)
    {
        GameObject obj = new GameObject(label + "Toggle");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(250, 32);

        Image background = obj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.02f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        GameObject track = new GameObject("SwitchTrack");
        track.transform.SetParent(obj.transform, false);
        RectTransform trackRect = track.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(58, 30);
        trackRect.anchoredPosition = new Vector2(-30, 0);
        Image trackImage = track.AddComponent<Image>();
        trackImage.color = new Color(0.82f, 0.82f, 0.76f, 1f);
        trackImage.raycastTarget = false;

        GameObject checkmark = new GameObject("SwitchOn");
        checkmark.transform.SetParent(obj.transform, false);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(1f, 0.5f);
        checkRect.anchorMax = new Vector2(1f, 0.5f);
        checkRect.sizeDelta = new Vector2(58, 30);
        checkRect.anchoredPosition = new Vector2(-30, 0);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.24f, 0.76f, 0.34f, 1f);
        checkImage.raycastTarget = false;
        toggle.graphic = checkImage;

        CreateCenteredChildLabel(obj.transform, label, font, 15, Color.black, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(-54, 0));
        return toggle;
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityAction action)
    {
        return CreateButton(parent, label, font, position, dimensions, action, new Color(0.95f, 0.95f, 0.95f, 1f), Color.black, 15);
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

        CreateCenteredChildLabel(obj.transform, label, font, fontSize, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        return button;
    }

    private void SetReminderInputs(string alarmTime)
    {
        reminderDateInput.text = "";
        reminderTimeInput.text = "";

        if (string.IsNullOrWhiteSpace(alarmTime))
            return;

        if (AlarmManager.TryParseAlarmTime(alarmTime, out DateTime dateTime))
        {
            reminderDateInput.text = dateTime.ToString("yyyy-MM-dd");
            reminderTimeInput.text = dateTime.ToString("HH:mm");
        }
    }

    private string BuildAlarmTime()
    {
        string date = GetInputValue(reminderDateInput);
        string time = GetInputValue(reminderTimeInput);
        return string.IsNullOrWhiteSpace(date) && string.IsNullOrWhiteSpace(time) ? "" : date + " " + time;
    }

    private static string GetInputValue(InputField input)
    {
        if (input == null)
            return "";

        string value = input.text ?? "";
        Text placeholderText = input.placeholder as Text;
        if (placeholderText != null && value == placeholderText.text)
            return "";

        return value;
    }

    private static string GenerateTitleFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "New Note";

        string firstLine = content.Split('\n')[0].Trim();
        return firstLine.Length <= 32 ? firstLine : firstLine.Substring(0, 32);
    }

    private static string NormalizeChoice(string value, string fallback, string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string normalized = value.Trim().ToLowerInvariant();
        foreach (string allowedValue in allowedValues)
        {
            if (normalized == allowedValue)
                return normalized;
        }

        return fallback;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static string GetIconFallbackLabel(string iconId)
    {
        switch (iconId)
        {
            case "star": return "Star";
            case "finish": return "Done";
            case "inprocess": return "Doing";
            case "reminder": return "Bell";
            case "work": return "Work";
            case "study": return "Study";
            case "shopping": return "Shop";
            default: return iconId;
        }
    }

    private static Sprite GetIconSprite(NoteStyleManager styleManager, string iconId)
    {
        if (styleManager == null)
            return null;

        switch (iconId)
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

    private static Color GetColorValue(string colorName, float alpha)
    {
        switch (colorName)
        {
            case "pink": return new Color(1f, 0.56f, 0.66f, alpha);
            case "blue": return new Color(0.55f, 0.82f, 0.92f, alpha);
            case "green": return new Color(0.56f, 0.85f, 0.48f, alpha);
            case "yellow":
            default:
                return new Color(0.96f, 0.78f, 0.18f, alpha);
        }
    }

    private static Color GetPanelColor(string colorName)
    {
        switch (colorName)
        {
            case "pink": return new Color(1f, 0.86f, 0.89f, 0.96f);
            case "blue": return new Color(0.86f, 0.95f, 0.98f, 0.96f);
            case "green": return new Color(0.88f, 0.96f, 0.82f, 0.96f);
            case "yellow":
            default:
                return new Color(1f, 0.96f, 0.68f, 0.96f);
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 1) * 0.5f;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}

public class RuntimeButtonActionRelay : MonoBehaviour
{
    private UnityAction action;

    public void Configure(UnityAction newAction)
    {
        action = newAction;
    }

    public void Invoke()
    {
        if (action == null)
        {
            Debug.LogWarning("Runtime button action is missing on " + name);
            return;
        }

        action.Invoke();
    }
}
