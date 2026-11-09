using UnityEngine;
using UnityEngine.UI;

public class ObjectVisibilityToggleGroup : MonoBehaviour
{
    public enum ToggleMode
    {
        RendererOnly,
        SetActive
    }

    [Header("Targets")]
    public GameObject[] targetObjects;
    public bool includeChildren = true;
    public ToggleMode toggleMode = ToggleMode.RendererOnly;

    [Header("State")]
    public bool showObjects = true;
    public bool applyOnStart = true;

    [Header("Optional UI")]
    public Toggle uiToggle;
    public KeyCode hotkey = KeyCode.None;

    private bool appliedShowState;

    private void Start()
    {
        appliedShowState = !showObjects;

        if (uiToggle != null)
        {
            uiToggle.isOn = showObjects;
            uiToggle.onValueChanged.AddListener(SetVisible);
        }

        if (applyOnStart)
        {
            ApplyVisibility();
        }
    }

    private void Update()
    {
        if (hotkey != KeyCode.None && Input.GetKeyDown(hotkey))
        {
            ToggleVisible();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyVisibilityInEditor();
        }
    }

    [ContextMenu("Apply Visibility")]
    public void ApplyVisibility()
    {
        if (appliedShowState == showObjects)
        {
            return;
        }

        ApplyVisibilityImmediate(showObjects);
        appliedShowState = showObjects;
    }

    [ContextMenu("Show Objects")]
    public void ShowObjects()
    {
        SetVisible(true);
    }

    [ContextMenu("Hide Objects")]
    public void HideObjects()
    {
        SetVisible(false);
    }

    public void ToggleVisible()
    {
        SetVisible(!showObjects);
    }

    public void SetVisible(bool visible)
    {
        showObjects = visible;
        ApplyVisibilityImmediate(showObjects);
        appliedShowState = showObjects;

        if (uiToggle != null && uiToggle.isOn != showObjects)
        {
            uiToggle.isOn = showObjects;
        }
    }

    private void ApplyVisibilityInEditor()
    {
        ApplyVisibilityImmediate(showObjects);
        appliedShowState = showObjects;
    }

    private void ApplyVisibilityImmediate(bool visible)
    {
        if (targetObjects == null)
        {
            return;
        }

        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject target = targetObjects[i];
            if (target == null)
            {
                continue;
            }

            if (toggleMode == ToggleMode.SetActive)
            {
                target.SetActive(visible);
            }
            else
            {
                SetRenderersVisible(target, visible);
            }
        }
    }

    private void SetRenderersVisible(GameObject target, bool visible)
    {
        Renderer[] renderers = includeChildren
            ? target.GetComponentsInChildren<Renderer>(true)
            : target.GetComponents<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }
}
