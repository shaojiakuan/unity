using UnityEngine;

public class wangye : MonoBehaviour
{
    [Header("链接设置")]
    public string url = "https://www.baidu.com";

    [Header("悬停设置")]
    public bool requireMouseClick = false; // 是否需要点击而不是悬停

    void OnMouseEnter()
    {
        if (!requireMouseClick)
        {
            OpenURL();
        }
    }

    void OnMouseDown()
    {
        if (requireMouseClick)
        {
            OpenURL();
        }
    }

    void OpenURL()
    {
        Application.OpenURL(url);
        Debug.Log("打开链接: " + url);
    }
}