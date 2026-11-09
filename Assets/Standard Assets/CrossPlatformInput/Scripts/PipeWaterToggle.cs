using UnityEngine;

public class PipeWaterToggle : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Q;
    public Material pipeMaterial;

    [Header("目标参数")]
    public float targetPipeAlpha = 0.3f;
    public float targetWaterIntensity = 1f;
    public Vector2 flowSpeed = new Vector2(0, 0.5f);

    private bool _isOn;
    private float _defaultAlpha;
    private float _defaultIntensity;

    void Start()
    {
        _defaultAlpha = pipeMaterial.GetFloat("_PipeAlpha");
        _defaultIntensity = pipeMaterial.GetFloat("_WaterIntensity");
        pipeMaterial.SetVector("_FlowSpeed", flowSpeed);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _isOn = !_isOn;
            pipeMaterial.SetFloat("_PipeAlpha", _isOn ? targetPipeAlpha : _defaultAlpha);
            pipeMaterial.SetFloat("_WaterIntensity", _isOn ? targetWaterIntensity : _defaultIntensity);
        }
    }
}
