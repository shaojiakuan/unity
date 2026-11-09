using UnityEngine;

public class ABCDGroupTransparencyController : MonoBehaviour
{
    [Header("�����壨A / B / C / D��")]
    public GameObject objA;
    public GameObject objB;
    public GameObject objC;
    public GameObject objD;

    [Header("��͸������")]
    public Material transparentMaterial;

    // ����ԭʼ����
    private Material[] matA;
    private Material[] matB;
    private Material[] matC;
    private Material[] matD;

    void Start()
    {
        matA = objA != null ? GetMaterials(objA) : new Material[0];
        matB = objB != null ? GetMaterials(objB) : new Material[0];
        matC = objC != null ? GetMaterials(objC) : new Material[0];
        matD = objD != null ? GetMaterials(objD) : new Material[0];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ShowOnly(objA);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ShowOnly(objB);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ShowOnly(objC);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            ShowOnly(objD);

        if (Input.GetKeyDown(KeyCode.Alpha0))
            RestoreAll();
    }

    /// <summary>
    /// ֻ��ʾָ��������Ϊ��͸��������ȫ������
    /// </summary>
    void ShowOnly(GameObject target)
    {
        if (objA != null) objA.SetActive(false);
        if (objB != null) objB.SetActive(false);
        if (objC != null) objC.SetActive(false);
        if (objD != null) objD.SetActive(false);

        if (target != null)
        {
            target.SetActive(true);
            SetTransparent(target);
        }
    }

    /// <summary>
    /// ���ø����������� Renderer Ϊ��͸��
    /// </summary>
    void SetTransparent(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.material = transparentMaterial;
        }
    }

    /// <summary>
    /// �ָ�ȫ��������
    /// </summary>
    void RestoreAll()
    {
        Restore(objA, matA);
        Restore(objB, matB);
        Restore(objC, matC);
        Restore(objD, matD);
    }

    void Restore(GameObject obj, Material[] mats)
    {
        if (obj == null) return;
        obj.SetActive(true);

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (i < mats.Length)
                renderers[i].material = mats[i];
        }
    }

    /// <summary>
    /// ��ȡ������������ Renderer ��ԭʼ����
    /// </summary>
    Material[] GetMaterials(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Material[] mats = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            mats[i] = renderers[i].material;
        }

        return mats;
    }
}
