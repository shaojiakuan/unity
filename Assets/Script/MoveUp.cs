using UnityEngine;

public class MoveUp : MonoBehaviour
{
    public float speed = 0.1f;

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
    }
}