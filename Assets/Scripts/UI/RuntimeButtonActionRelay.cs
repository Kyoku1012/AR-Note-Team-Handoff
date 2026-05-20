using UnityEngine;
using UnityEngine.Events;

public class RuntimeButtonActionRelay : MonoBehaviour
{
    private const float DuplicateInvokeWindowSeconds = 0.25f;

    private UnityAction action;
    private float lastInvokeRealtime = -1f;

    public void Configure(UnityAction newAction)
    {
        action = newAction;
    }

    public void Invoke()
    {
        if (Time.unscaledTime - lastInvokeRealtime < DuplicateInvokeWindowSeconds)
            return;

        lastInvokeRealtime = Time.unscaledTime;

        if (action == null)
        {
            Debug.LogWarning("Runtime button action is missing on " + name);
            return;
        }

        action.Invoke();
    }
}
