using UnityEngine;
using UnityEngine.Events;

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
