using UnityEngine;

public class SimulateQt1 : MonoBehaviour
{
    private bool isShowTip;
    public Conveyer Conveyer;
    private float sp = 0.05f;
    private int enterCount = 0; // 用于记录鼠标进入的次数

    void Start()
    {
        Conveyer.Speed = sp;
        isShowTip = false;
    }

    void OnMouseEnter()
    {
        // 增加鼠标进入的次数
        enterCount++;

        // 根据进入次数的奇偶性切换 isShowTip 状态
        isShowTip = (enterCount % 2 != 0); // 奇数次为 true，偶数次为 false
    }

    void OnMouseExit()
    {
        // 鼠标移出时不改变 isShowTip 的状态
    }

    void OnGUI()
    {
        if (isShowTip)
        {
            sp = 0f;
            Conveyer.Speed = sp;
        }
        else
        {
            sp = 0.05f;
            Conveyer.Speed = sp;
        }
    }

    void Update()
    {
        // 在此示例中不需要额外的更新逻辑
    }
}
