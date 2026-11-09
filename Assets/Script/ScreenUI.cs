using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenUI : MonoBehaviour
{
    public GameObject Device;
    public Vector3 Offset;
    public float refreshInterval = 0.05f;

    private Camera mainCamera;
    private RectTransform rectTransform;
    private float refreshTimer;

    private void Awake()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval)
        {
            return;
        }

        refreshTimer = 0f;
        RefreshPosition();
    }

    private void OnEnable()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (Device == null || mainCamera == null || rectTransform == null)
        {
            return;
        }

        if (Vector3.Dot(Device.transform.position - mainCamera.transform.position, mainCamera.transform.forward) > 0)
        {
            rectTransform.anchoredPosition = mainCamera.WorldToScreenPoint(Device.transform.position) - new Vector3(Screen.width / 2, Screen.height / 2, 0) + Offset;
        }
        else
        {
            rectTransform.anchoredPosition = new Vector2(-1000, -1000);
        }
    }
}
