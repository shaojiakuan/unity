using UnityEngine;

public class WaterUVFlow : MonoBehaviour
{
    public Renderer waterRenderer;

    [Header("UV滚动速度")]
    public float speedX = 0.02f;
    public float speedY = 0.01f;

    private Material waterMat;
    private Vector2 offset;

    void Start()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }

        waterMat = waterRenderer.material;
        offset = waterMat.mainTextureOffset;
    }

    void Update()
    {
        offset.x += speedX * Time.deltaTime;
        offset.y += speedY * Time.deltaTime;

        waterMat.mainTextureOffset = offset;
    }
}