using UnityEngine;
using UnityEngine.UI;

public class HideWallOnClick : MonoBehaviour
{
    public Button yourButton; // 按钮
    public GameObject wall;   // 要隐藏的墙对象

    void Start()
    {
        // 添加点击事件监听器
        yourButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        // 切换墙的可见性
        wall.SetActive(!wall.activeSelf);
    }
}
