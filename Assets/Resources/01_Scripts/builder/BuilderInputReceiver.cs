using UnityEngine;
using UnityEngine.InputSystem;

public class BuilderInputReceiver : MonoBehaviour
{
    [SerializeField] private Builder builder;

    // This signature MUST be public and match the event's parameter type
    public void OnBuild(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (builder != null) builder.RegisterSpacePress();
    }

    private void Reset()
    {
        if (builder == null) builder = GetComponent<Builder>() ?? GetComponentInParent<Builder>();
    }
}

