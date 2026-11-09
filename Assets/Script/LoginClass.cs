using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class LoginClass : MonoBehaviour
{
    //����ǰ����
    public InputField username, password, confirmPassword;
    public Text reminderText;
    public int errorsNum;
    public Button loginButton;
    public GameObject hallSetUI, loginUI;
    //��������
    public static string myUsername;

    // WebGL SSO: skip Unity login UI when web layer already logged in (?user=xxx)
    private void Start()
    {
        string ssoUser = WebGLBridge.GetUrlQueryParam("user");
        if (!string.IsNullOrEmpty(ssoUser))
        {
            myUsername = ssoUser;
            if (hallSetUI != null) hallSetUI.SetActive(true);
            if (loginUI != null) loginUI.SetActive(false);
            SceneManager.LoadScene(2);
        }
    }

    public void Register()
    {
        if (PlayerPrefs.GetString(username.text) == "")
        {
            if (password.text == confirmPassword.text)
            {
                PlayerPrefs.SetString(username.text, username.text);
                PlayerPrefs.SetString(username.text + "password", password.text);
                reminderText.text = "ע��ɹ���";
            }
            else
            {
                reminderText.text = "�����������벻һ��";
            }
        }
        else
        {
            reminderText.text = "�û��Ѵ���";
        }

    }
    private void Recovery()
    {
        loginButton.interactable = true;
    }
    public void Login()
    {
        if (PlayerPrefs.GetString(username.text) != "")
        {
            if (PlayerPrefs.GetString(username.text + "password") == password.text)
            {
                reminderText.text = "��¼�ɹ�";

                myUsername = username.text;
                hallSetUI.SetActive(true);
                loginUI.SetActive(false);
                SceneManager.LoadScene(2);
            }
            else
            {
                reminderText.text = "�������";
                errorsNum++;
                if (errorsNum >= 3)
                {
                    reminderText.text = "��������3�Σ���30������ԣ�";
                    loginButton.interactable = false;
                    Invoke("Recovery", 5);
                    errorsNum = 0;
                }
            }
        }
        else
        {
            reminderText.text = "�˺Ų�����";
        }
    }
}
