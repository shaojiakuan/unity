using UnityEngine;

public class WaterSurfaceFloat : MonoBehaviour
{
    [Header("上下波动")]
    public float amplitude = 0.0025f;
    public float frequency = 1.1f;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = startLocalPos + new Vector3(0f, yOffset, 0f);
    }
}