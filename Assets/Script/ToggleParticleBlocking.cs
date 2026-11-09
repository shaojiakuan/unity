using UnityEngine;

public class ToggleParticleBlocking : MonoBehaviour
{
    private Collider blockingCollider;
    private bool isBlocked = false;
    private Camera mainCamera;

    void Start()
    {
        blockingCollider = gameObject.AddComponent<BoxCollider>();
        blockingCollider.isTrigger = true;
        blockingCollider.enabled = false;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.transform == transform)
        {
            ToggleBlocking();
        }
    }

    private void ToggleBlocking()
    {
        isBlocked = !isBlocked;
        blockingCollider.enabled = isBlocked;
    }
}
