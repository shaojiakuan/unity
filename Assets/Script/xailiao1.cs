using UnityEngine;
using System.Collections;
public class xailiao1 : MonoBehaviour
{
    public float upwardForce = 10f; // 向上的力
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(ApplyForceAfterDelay(3f)); // 延迟3秒后应用力
    }

    private IEnumerator ApplyForceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // 等待3秒

        // 应用向上的力
        rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
    }
}