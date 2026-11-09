using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 传送物体
/// </summary>

public class carrierObj : MonoBehaviour
{
    //物体移动速度
    public float speed = 1.2f;
    //只要产生碰撞就执行
    public void  OnCollisionStay(Collision collision)
    {
        //对传送带上的物体赋予移动效果
        if (collision.gameObject!=null)
        {
            MoveObj(collision.gameObject);
        }
    }
    /// <summary>
    /// 物体移动
    /// </summary>
    /// <param name="gameObject"></param>
    public void MoveObj(GameObject gameObject)
    {
        gameObject.transform.Translate( Vector3.right * speed * Time.deltaTime,Space.World);
    }
}
