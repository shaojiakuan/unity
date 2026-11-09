using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire1 : MonoBehaviour
{
    public ParticleSystem flame;
    public ParticleSystem smoke;
    // Start is called before the first frame update

    public Color ColorYellow;
    public Color ColorBlue;
    public void setTemperature(float temperature)
    {
        ParticleSystem.MainModule mainModule = flame.main;
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(temperature/* / 1.0f * 0.6f*/);
        ParticleSystem.MainModule smokeModule = smoke.main;
        smokeModule.startSpeed = new ParticleSystem.MinMaxCurve(temperature/* / 1.0f * 0.6f*/);

    }

    public void setPressure(float pressure)
    {
        ParticleSystem.MainModule mainModule = flame.main; 
        mainModule.startColor = Color.Lerp(ColorYellow, ColorBlue, pressure);
        ParticleSystem.MainModule smokeModule = smoke.main;
        smokeModule.startColor = Color.Lerp(ColorYellow, ColorBlue, pressure);
    }
}
