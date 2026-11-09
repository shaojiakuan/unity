using UnityEngine;
using System.Collections;

public class MovingObject : MonoBehaviour
{
    [Header("�ƶ�����")]
    [Tooltip("ÿ���ƶ��ľ���")]
    public float moveDistance = 5f;  // ��Inspector�е����ƶ�����

    [Tooltip("�ƶ��ٶ�")]
    public float moveSpeed = 3f;

    [Tooltip("�����յ��ȴ���ʱ��")]
    public float waitTimeAtEnd = 0.5f;

    [Tooltip("��������ȴ���ʱ��")]
    public float waitTimeAtStart = 0.5f;

    private Vector3 startPosition;    // ���λ��
    private Vector3 targetPosition;   // �յ�λ��

    void Start()
    {
        // ��¼��ʼλ����Ϊ���
        startPosition = transform.position;

        // �����յ�λ�ã�Y�Ḻ����
        targetPosition = startPosition + Vector3.down * moveDistance;

        // ��ʼ�Զ�ѭ���ƶ�
        StartCoroutine(MoveLoop());
    }

    // �Զ�ѭ���ƶ���Э��
    IEnumerator MoveLoop()
    {
        while (true)
        {
            // ������ƶ����յ�
            yield return StartCoroutine(MoveToPosition(targetPosition));
            yield return new WaitForSeconds(waitTimeAtEnd);

            // ˲�ƻ����
            transform.position = startPosition;
            yield return new WaitForSeconds(waitTimeAtStart);

            // ��һ��ѭ��
        }
    }

    // �ƶ���ָ��λ�õ�Э��
    IEnumerator MoveToPosition(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            // ʹ��MoveTowardsƽ���ƶ�
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;  // �ȴ���һ֡
        }

        // ȷ����ȷ����Ŀ��λ��
        transform.position = targetPos;
    }

    // ���ӻ������յ㣨��Scene��ͼ����ʾ��
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // �ڱ༭ģʽ��Ԥ���ƶ���Χ
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.3f);

            Vector3 endPos = transform.position + Vector3.down * moveDistance;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPos, 0.3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, endPos);
        }
        else
        {
            // ������ģʽ����ʾʵ��λ��
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startPosition, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPosition, 0.3f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, targetPosition);
        }
    }

    // ��Inspector���޸Ĳ���ʱʵʱ����
    void OnValidate()
    {
        if (Application.isPlaying && startPosition != Vector3.zero)
        {
            // ���¼����յ�λ��
            targetPosition = startPosition + Vector3.down * moveDistance;
        }
    }
}