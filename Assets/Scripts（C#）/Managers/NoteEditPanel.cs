using System;
using UnityEngine;
using UnityEngine.UI;

public class NoteEditPanel : MonoBehaviour
{
    private NoteView currentNote;
    private InputField titleInput;
    private InputField contentInput;
    private InputField annotationInput;
    private InputField reminderInput;
    private Toggle completedToggle;
    private Toggle visibleToggle;
    private Toggle reminderToggle;
    private Text voiceStatusText;

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

        GameObject panelObject = CreatePanel(canvasObject.transform);
        NoteEditPanel panel = panelObject.AddComponent<NoteEditPanel>();
        panel.BuildUi(panelObject.transform);
        panel.gameObject.SetActive(false);
        CreateLauncher(canvasObject.transform, panel);
        return panel;
    }

    public void Open(NoteView noteView)
    {
        currentNote = noteView;
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteData data = currentNote.Data;
        titleInput.text = data.title;
        contentInput.text = data.content;
        annotationInput.text = data.annotation;
        completedToggle.isOn = data.isCompleted;
        visibleToggle.isOn = data.isVisible;
        reminderToggle.isOn = data.hasReminder;
        reminderInput.text = data.reminderTime;
        UpdateVoiceStatus();

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OpenSelected()
    {
        NoteView selected = NoteManager.Instance == null ? null : NoteManager.Instance.SelectedNote ?? NoteManager.Instance.GetFirstView();
        if (selected != null)
            Open(selected);
    }

    private void Save()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteData data = currentNote.Data;
        data.title = titleInput.text;
        data.content = contentInput.text;
        data.annotation = annotationInput.text;
        data.isCompleted = completedToggle.isOn;
        data.isVisible = visibleToggle.isOn;
        data.hasReminder = reminderToggle.isOn;
        data.reminderTime = reminderInput.text;

        if (data.hasReminder && !ReminderManager.TryParseReminderTime(data.reminderTime, out DateTime _))
        {
            Debug.LogWarning("Reminder time must be like 2026-05-25 09:30.");
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

    private void ToggleRecord()
    {
        if (currentNote == null) return;

        VoiceNoteManager.Instance?.ToggleRecording(currentNote);
        UpdateVoiceStatus();
    }

    private void PlayVoice()
    {
        currentNote?.PlayVoice();
    }

    private void DeleteVoice()
    {
        if (currentNote == null) return;

        VoiceNoteManager.Instance?.DeleteVoice(currentNote);
        UpdateVoiceStatus();
    }

    private void UpdateVoiceStatus()
    {
        if (voiceStatusText == null || currentNote == null || currentNote.Data == null)
            return;

        voiceStatusText.text = currentNote.Data.hasVoiceNote ? "Voice memo attached" : "No voice memo";
    }

    private void BuildUi(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateLabel(parent, "Edit AR Note", font, 24, new Vector2(0, -24), new Vector2(340, 34));
        titleInput = CreateInput(parent, "Title", font, new Vector2(0, -72), new Vector2(340, 34));
        contentInput = CreateInput(parent, "Content", font, new Vector2(0, -116), new Vector2(340, 34));
        annotationInput = CreateInput(parent, "Annotation", font, new Vector2(0, -160), new Vector2(340, 34));

        completedToggle = CreateToggle(parent, "Completed", font, new Vector2(-105, -202));
        visibleToggle = CreateToggle(parent, "Visible", font, new Vector2(95, -202));
        reminderToggle = CreateToggle(parent, "Reminder", font, new Vector2(-105, -244));
        reminderInput = CreateInput(parent, "yyyy-MM-dd HH:mm", font, new Vector2(70, -244), new Vector2(205, 32));

        voiceStatusText = CreateLabel(parent, "No voice memo", font, 15, new Vector2(0, -286), new Vector2(340, 24));

        CreateButton(parent, "Record", font, new Vector2(-126, -326), new Vector2(94, 34), ToggleRecord);
        CreateButton(parent, "Play", font, new Vector2(0, -326), new Vector2(94, 34), PlayVoice);
        CreateButton(parent, "Remove", font, new Vector2(126, -326), new Vector2(94, 34), DeleteVoice);

        CreateButton(parent, "Save", font, new Vector2(-126, -374), new Vector2(94, 38), Save);
        CreateButton(parent, "Delete", font, new Vector2(0, -374), new Vector2(94, 38), DeleteCurrent);
        CreateButton(parent, "Close", font, new Vector2(126, -374), new Vector2(94, 38), Close);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("NoteEditPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -24);
        rect.sizeDelta = new Vector2(390, 430);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        return panel;
    }

    private static void CreateLauncher(Transform parent, NoteEditPanel panel)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateButton(parent, "Edit Note", font, new Vector2(0, -32), new Vector2(120, 40), panel.OpenSelected);
    }

    private static Text CreateLabel(Transform parent, string text, Font font, int size, Vector2 position, Vector2 dimensions)
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
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        return label;
    }

    private static InputField CreateInput(Transform parent, string placeholder, Font font, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject(placeholder + "Input");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = Color.white;

        InputField input = obj.AddComponent<InputField>();
        Text text = CreateInputText(obj.transform, "Text", font, Color.black);
        Text placeholderText = CreateInputText(obj.transform, "Placeholder", font, new Color(0.45f, 0.45f, 0.45f));
        placeholderText.text = placeholder;
        input.textComponent = text;
        input.placeholder = placeholderText;
        return input;
    }

    private static Text CreateInputText(Transform parent, string name, Font font, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 4);
        rect.offsetMax = new Vector2(-8, -4);

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 16;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
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
        rect.sizeDelta = new Vector2(150, 30);

        Image background = obj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.02f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(obj.transform, false);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.sizeDelta = new Vector2(22, 22);
        checkRect.anchoredPosition = new Vector2(14, 0);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.35f);
        toggle.graphic = checkImage;

        Text text = CreateLabel(obj.transform, label, font, 15, new Vector2(46, 0), new Vector2(100, 24));
        text.alignment = TextAnchor.MiddleLeft;
        return toggle;
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityEngine.Events.UnityAction action)
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
        image.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button button = obj.AddComponent<Button>();
        button.onClick.AddListener(action);

        Text text = CreateLabel(obj.transform, label, font, 15, Vector2.zero, dimensions);
        text.color = Color.black;
        return button;
    }
}
