using UnityEngine;

public class CharacterInteract : MonoBehaviour
{
    public InteractableObj currentInteractingObj;
    public GameObject UI;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && UI != null)
        {
            UI.SetActive(!UI.activeInHierarchy);
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            InteractableObj interactableObj = hit.collider.GetComponent<InteractableObj>();
            if (interactableObj != null)
            {
                if (interactableObj != currentInteractingObj)
                {
                    if (currentInteractingObj != null)
                    {
                        currentInteractingObj.StopInteract();
                    }

                    currentInteractingObj = interactableObj;
                    currentInteractingObj.Interact();
                }
            }
            else if (currentInteractingObj != null)
            {
                currentInteractingObj.StopInteract();
                currentInteractingObj = null;
            }
        }
    }
}
