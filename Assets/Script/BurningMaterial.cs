using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class BurningMaterial : MonoBehaviour
{
    public static readonly List<BurningMaterial> ActiveMaterials = new List<BurningMaterial>();

    public float DestroyLine;
    public float Speed11;
    public Material Material2;
    public Material Material3;
    public Material Material4;
    public float x;
    private float startX; // Store the initial x position
    private Renderer cachedRenderer;
    private PooledGameObject pooledObject;
    private Material currentStageMaterial;

    private void OnEnable()
    {
        if (!ActiveMaterials.Contains(this))
        {
            ActiveMaterials.Add(this);
        }

        startX = transform.position.x;
        x = startX;
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (pooledObject == null)
        {
            pooledObject = GetComponent<PooledGameObject>();
        }

        currentStageMaterial = null;
    }

    private void OnDisable()
    {
        ActiveMaterials.Remove(this);
    }

    public void Start()
    {
        startX = transform.position.x; // Store the starting x position
        x = startX;
        cachedRenderer = GetComponent<Renderer>();
        pooledObject = GetComponent<PooledGameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = transform.position;
        x += Speed11 * Time.deltaTime;
        position.x = x;
        transform.position = position;

        // Check if we've traveled 0.2f from the starting position
        if (x >= startX + 0.2f)
        {
            ReleaseOrDestroy();
            return; // Exit early since we're destroying the object
        }

        UpdateStageMaterial();
    }

    private void UpdateStageMaterial()
    {
        if (x > 14f)
        {
            ReleaseOrDestroy();
            return;
        }

        Material nextMaterial = null;
        if (x > 13.2f)
        {
            nextMaterial = Material4;
        }
        else if (x > 12.3f)
        {
            nextMaterial = Material3;
        }
        else if (x > 11.4f)
        {
            nextMaterial = Material2;
        }

        if (nextMaterial != null && nextMaterial != currentStageMaterial && cachedRenderer != null)
        {
            cachedRenderer.sharedMaterial = nextMaterial;
            currentStageMaterial = nextMaterial;
        }
    }

    private void ReleaseOrDestroy()
    {
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
