using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    private Camera mainCamera;

    public void OnClick()
    {
    }

    private void OnMouseDown()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Ray2D ray = new Ray2D(mousePos, Vector2.zero);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, Vector2.zero);

        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            OnClick();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
    }
}
