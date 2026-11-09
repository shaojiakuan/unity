using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dianji : InteractableObj
{
    public GameObject UI;
    // Start is called before the first frame update
    public override void Interact()
    {
        base.Interact();
        UI.SetActive(true);
    }

    public override void StopInteract()
    {
        base.StopInteract();
        UI.SetActive(false);
    }
}
