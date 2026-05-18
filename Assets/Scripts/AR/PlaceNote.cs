using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

public class PlaceNote : MonoBehaviour
{
    public GameObject notePrefab;
    public float offset = 0.01f;

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                Debug.Log("Touch is over UI, skip plane hit test.");
                return;
            }

            planeFinder?.PerformHitTest(Input.GetTouch(0).position);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Mouse is over UI, skip plane hit test.");
                return;
            }

            planeFinder?.PerformHitTest(Input.mousePosition);
        }
#endif
    }

    public void AnchorCreated(HitTestResult result)
    {
        if (result == null || notePrefab == null) return;

        Quaternion finalRotation = CalculateReadableRotation(result);
        GameObject anchorObj = CreateAnchor("NoteAnchor", result.Position, finalRotation);
        GameObject instantiatedNote = Instantiate(notePrefab, anchorObj.transform);
        instantiatedNote.transform.localPosition = new Vector3(0, offset, 0);
        instantiatedNote.transform.localRotation = Quaternion.identity;

        NoteData data = new NoteData
        {
            title = "New Note",
            content = "Tap Edit to add details",
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
        noteView.Initialize(data, anchorObj);

        NoteManager.Instance?.AddNote(data);

        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(noteView);
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
}
