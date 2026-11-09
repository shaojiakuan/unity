using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fbs : MonoBehaviour
{
    public enum LimitType
    {
        nolimit = -1,
        limit120 = 120,
        limit60 = 60,
        limit90 = 90,
        limit30 = 30
    }
    public LimitType limitType;
    private void Awoke()
    {
        Application.targetFrameRate = (int)limitType;
    }
}
