using UnityEngine;

public class carrier : MonoBehaviour
{
    public float speed = 0.15f;
    public float x = 0f;

    private Material runtimeMaterial;

    void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            runtimeMaterial = renderer.material;
        }
    }

    void Update()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        x += Time.deltaTime * speed;
        runtimeMaterial.mainTextureOffset = new Vector2(0f, x);
    }
}
