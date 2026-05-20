using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteView : MonoBehaviour, IPointerClickHandler
{
    public NoteData Data { get; private set; }
    public GameObject AnchorRoot { get; private set; }

    private TMP_Text[] titleTexts;
    private TMP_Text[] contentTexts;
    private NoteStyleManager styleManager;
    private Canvas[] noteCanvases;
    private LineRenderer selectionFrame;
    private BoxCollider noteCollider;

    public void Initialize(NoteData data, GameObject anchorRoot, bool selectOnCreate = true)
    {
        Data = data == null ? new NoteData() : data.Clone();
        AnchorRoot = anchorRoot;
        Data.ApplyDefaults();

        CacheComponents();
        NormalizeSelectionCollider();
        WireButtons();
        RefreshFromData();

        NoteManager.Instance?.RegisterView(this);
        if (selectOnCreate)
            NoteManager.Instance?.SelectNote(this);
        else
            SetSelectedVisual(false);
    }

    private void OnEnable()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.NoteSelected += HandleNoteSelected;
    }

    private void OnDisable()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.NoteSelected -= HandleNoteSelected;
    }

    private void OnDestroy()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.NoteSelected -= HandleNoteSelected;

        NoteManager.Instance?.UnregisterView(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    private void OnMouseDown()
    {
        Select();
    }

    public void Select()
    {
        NoteManager.Instance?.SelectNote(this);

        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(this);
    }

    public void SetSelectedVisual(bool isSelected)
    {
        EnsureSelectionFrame();

        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(isSelected);
    }

    public void RefreshFromData()
    {
        if (Data == null) return;

        Data.ApplyDefaults();
        CacheComponents();

        string displayTitle = Data.isCompleted ? Data.title + " (Done)" : Data.title;
        foreach (TMP_Text text in titleTexts)
        {
            if (text != null)
                text.text = displayTitle;
        }

        string displayContent = string.IsNullOrWhiteSpace(Data.content) ? Data.annotation : Data.content;
        foreach (TMP_Text text in contentTexts)
        {
            if (text != null)
                text.text = displayContent;
        }

        if (styleManager != null)
            styleManager.ApplyStyle(Data);

        SetCanvasesVisible(Data.isVisible);
    }

    public void ToggleCompleted()
    {
        if (Data == null) return;
        Data.isCompleted = !Data.isCompleted;
        SaveAndRefresh();
    }

    public void ToggleVisible()
    {
        if (Data == null) return;
        Data.isVisible = !Data.isVisible;
        SaveAndRefresh();
    }

    public void OpenEditor()
    {
        Select();
        NoteEditPanel.EnsureExists().Open(this);
    }

    public void DeleteNote()
    {
        if (Data == null) return;
        NoteManager.Instance?.RemoveNote(Data.id);
    }

    public void StartSpeechInput()
    {
        Select();
        NoteEditPanel panel = NoteEditPanel.EnsureExists();
        panel.Open(this);
        panel.StartSpeechInput();
    }

    public void SaveAndRefresh()
    {
        if (Data == null) return;

        Data.worldPosition = AnchorRoot != null ? AnchorRoot.transform.position : transform.position;
        Data.worldRotation = AnchorRoot != null ? AnchorRoot.transform.eulerAngles : transform.eulerAngles;
        NoteManager.Instance?.UpdateNote(Data);
        RefreshFromData();
    }

    private void CacheComponents()
    {
        if (styleManager == null)
            styleManager = GetComponentInChildren<NoteStyleManager>(true);

        if (titleTexts == null || titleTexts.Length == 0)
            titleTexts = FindTexts("TitleText-NeedtoEdite", "TitleText");

        if (contentTexts == null || contentTexts.Length == 0)
            contentTexts = FindTexts("ContentText-NeedtoEdite", "ContentText");

        if (noteCanvases == null || noteCanvases.Length == 0)
            noteCanvases = GetComponentsInChildren<Canvas>(true);

        if (noteCollider == null)
            noteCollider = GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>(true);

        EnsureSelectionFrame();
    }

    private void HandleNoteSelected(NoteView selected)
    {
        SetSelectedVisual(selected == this);
    }

    private void WireButtons()
    {
        WireButton("CheckButton", ToggleCompleted);
        WireButton("EditButton", OpenEditor);
        WireButton("DeleteButton", DeleteNote);
        WireButton("VoiceButton", StartSpeechInput);
    }

    private void WireButton(string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = FindDeepChild(transform, childName);
        if (child == null) return;

        Button button = child.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private TMP_Text[] FindTexts(params string[] childNames)
    {
        System.Collections.Generic.List<TMP_Text> texts = new System.Collections.Generic.List<TMP_Text>();
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);

        foreach (string childName in childNames)
        {
            foreach (TMP_Text text in allTexts)
            {
                if (text != null && text.name == childName && !texts.Contains(text))
                    texts.Add(text);
            }
        }

        if (texts.Count == 0)
            Debug.LogWarning("NoteView could not find note text fields on " + name + ".");

        return texts.ToArray();
    }

    private void SetCanvasesVisible(bool visible)
    {
        if (noteCanvases == null) return;

        foreach (Canvas canvas in noteCanvases)
        {
            if (canvas != null)
                canvas.gameObject.SetActive(visible);
        }

        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(visible && NoteManager.Instance != null && NoteManager.Instance.SelectedNote == this);
    }

    private void EnsureSelectionFrame()
    {
        if (selectionFrame != null)
            return;

        GameObject frameObject = new GameObject("SelectedNoteFrame");
        frameObject.transform.SetParent(transform, false);
        frameObject.transform.localPosition = new Vector3(0f, 0f, -0.03f);
        frameObject.transform.localRotation = Quaternion.identity;
        frameObject.transform.localScale = Vector3.one;

        selectionFrame = frameObject.AddComponent<LineRenderer>();
        selectionFrame.useWorldSpace = false;
        selectionFrame.loop = true;
        selectionFrame.positionCount = 4;
        selectionFrame.widthMultiplier = 0.01f;
        selectionFrame.numCornerVertices = 2;
        selectionFrame.numCapVertices = 2;
        selectionFrame.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        selectionFrame.receiveShadows = false;
        selectionFrame.material = new Material(Shader.Find("Sprites/Default"));
        selectionFrame.startColor = new Color(0.1f, 0.9f, 1f, 1f);
        selectionFrame.endColor = new Color(0.1f, 0.9f, 1f, 1f);

        const float halfWidth = 0.32f;
        const float halfHeight = 0.46f;
        selectionFrame.SetPosition(0, new Vector3(-halfWidth, 0f, -halfHeight));
        selectionFrame.SetPosition(1, new Vector3(-halfWidth, 0f, halfHeight));
        selectionFrame.SetPosition(2, new Vector3(halfWidth, 0f, halfHeight));
        selectionFrame.SetPosition(3, new Vector3(halfWidth, 0f, -halfHeight));
        selectionFrame.gameObject.SetActive(false);
    }

    private void NormalizeSelectionCollider()
    {
        if (noteCollider == null)
            noteCollider = GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>(true);

        if (noteCollider == null)
            return;

        noteCollider.center = new Vector3(0f, 0f, 0f);
        noteCollider.size = new Vector3(0.7f, 1.0f, 0.08f);
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
