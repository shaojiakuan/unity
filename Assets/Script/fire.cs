using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fire : MonoBehaviour
{
    ParticleSystem particleSystem;
    ParticleSystem.ForceOverLifetimeModule forceMode;
    ParticleSystem.ColorOverLifetimeModule colorMode;

    // Use this for initialization
    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        forceMode = particleSystem.forceOverLifetime;
        colorMode = particleSystem.colorOverLifetime;
    }

    public void small(){
        particleSystem.startSize =  particleSystem.startSize * 0.7f;
		particleSystem.startLifetime = particleSystem.startLifetime * 0.7f;
    }

    public void big(){
        //起始生命周期
        particleSystem.startSize =  particleSystem.startSize * 1.5f;
		particleSystem.startLifetime = particleSystem.startLifetime * 1.5f;
    }
    public void highya(){
        var col = particleSystem.colorOverLifetime;
        Gradient grad = new Gradient();
        //                                                         蓝色       下面的第一个键                红色下面的第二个键                                                  aphla值， 位置                       
        grad.SetKeys( new GradientColorKey[] {new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 0.5f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0.3f, 1.0f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);
    }  
        public void LeftWind()
    {
        ParticleSystem.MinMaxCurve temp = forceMode.x;
        temp.constantMax += -2f;
        forceMode.x = temp;
    }
    public void RightWind()
    {
        ParticleSystem.MinMaxCurve temp = forceMode.x;
        temp.constantMax += 2f;
        forceMode.x = temp;
    }
}

