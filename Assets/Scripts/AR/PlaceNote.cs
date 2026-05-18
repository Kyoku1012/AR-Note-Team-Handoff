using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

public class PlaceNote : MonoBehaviour
{
    public GameObject notePrefab;
    public float offset = 0.01f;
    public float noteTapRayDistance = 20f;

    private PlaneFinderBehaviour planeFinder;

    private void Start()
    {
        planeFinder = GetComponent<PlaneFinderBehaviour>();
        StartCoroutine(RestoreSavedNotes());
    }

    private void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("Touch is over UI, skip plane hit test.");
                return;
            }

            if (TryOpenExistingNote(touchPosition))
                return;

            planeFinder?.PerformHitTest(touchPosition);
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.N))
        {
            CreateEditorTestNote();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Mouse is over UI, skip plane hit test.");
                return;
            }

            if (TryOpenExistingNote(mousePosition))
                return;

            planeFinder?.PerformHitTest(mousePosition);
        }
#endif
    }

    public void AnchorCreated(HitTestResult result)
    {
        if (result == null || notePrefab == null) return;

        Quaternion finalRotation = CalculateReadableRotation(result);
        CreateNoteAt("NoteAnchor", result.Position, finalRotation, true, "New Note", "Tap Edit to add details");
    }

#if UNITY_EDITOR
    public NoteView CreateEditorTestNote()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Editor test notes can only be created in Play Mode.");
            return null;
        }

        if (notePrefab == null)
        {
            Debug.LogWarning("PlaceNote cannot create an editor test note because notePrefab is not assigned.");
            return null;
        }

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = cameraTransform.forward.sqrMagnitude > 0.01f ? cameraTransform.forward : Vector3.forward;
        Vector3 position = cameraTransform.position + forward.normalized * 1.2f;
        Quaternion rotation = Quaternion.LookRotation(-forward.normalized, Vector3.up);

        Debug.Log("Created editor test note without Vuforia plane detection. Press N again to create another.");
        return CreateNoteAt("EditorTestNoteAnchor", position, rotation, true, "Editor Test Note", "Created without ground-plane detection");
    }
#endif

    private NoteView CreateNoteAt(string anchorName, Vector3 position, Quaternion rotation, bool selectOnCreate, string title, string content)
    {
        GameObject anchorObj = CreateAnchor(anchorName, position, rotation);
        GameObject instantiatedNote = Instantiate(notePrefab, anchorObj.transform);
        instantiatedNote.transform.localPosition = new Vector3(0, offset, 0);
        instantiatedNote.transform.localRotation = Quaternion.identity;

        NoteData data = new NoteData
        {
            title = title,
            content = content,
            annotation = "",
            isVisible = true,
            colorName = "yellow",
            iconId = "",
            priorityId = "",
            worldPosition = anchorObj.transform.position,
            worldRotation = anchorObj.transform.eulerAngles
        };
        data.ApplyDefaults();

        NoteView noteView = instantiatedNote.GetComponent<NoteView>() ?? instantiatedNote.AddComponent<NoteView>();
        noteView.Initialize(data, anchorObj, selectOnCreate);

        NoteManager.Instance?.AddNote(data);

        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(noteView);

        return noteView;
    }

    private IEnumerator RestoreSavedNotes()
    {
        yield return null;

        if (notePrefab == null || NoteManager.Instance == null)
            yield break;

        foreach (NoteData note in NoteManager.Instance.GetAllNotes())
        {
            if (note == null || string.IsNullOrWhiteSpace(note.id))
                continue;

            if (NoteManager.Instance.GetView(note.id) != null)
                continue;

            GameObject anchorObj = CreateAnchor("NoteAnchor_" + note.id, note.worldPosition, Quaternion.Euler(note.worldRotation));
            GameObject instantiatedNote = Instantiate(notePrefab, anchorObj.transform);
            instantiatedNote.transform.localPosition = new Vector3(0, offset, 0);
            instantiatedNote.transform.localRotation = Quaternion.identity;

            NoteView noteView = instantiatedNote.GetComponent<NoteView>() ?? instantiatedNote.AddComponent<NoteView>();
            noteView.Initialize(note, anchorObj, false);
        }

        ReminderManager.Instance?.RescheduleAll(NoteManager.Instance.GetAllNotes());
    }

    private Quaternion CalculateReadableRotation(HitTestResult result)
    {
        Vector3 surfaceNormal = result.Rotation * Vector3.up;

        if (Mathf.Abs(surfaceNormal.y) < 0.5f)
            return Quaternion.LookRotation(surfaceNormal, Vector3.up);

        Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.01f && Camera.main != null)
            camForward = Camera.main.transform.up;

        return Quaternion.LookRotation(camForward, Vector3.up);
    }

    private GameObject CreateAnchor(string anchorName, Vector3 position, Quaternion rotation)
    {
        GameObject anchorObj = new GameObject(anchorName);
        anchorObj.transform.position = position;
        anchorObj.transform.rotation = rotation;
        anchorObj.AddComponent<AnchorBehaviour>();
        return anchorObj;
    }

    private bool TryOpenExistingNote(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, noteTapRayDistance))
            return false;

        NoteView noteView = hit.collider.GetComponentInParent<NoteView>();
        if (noteView == null)
            return false;

        noteView.OpenEditor();
        Debug.Log("Existing note tapped; opening editor instead of creating a new note.");
        return true;
    }
}
