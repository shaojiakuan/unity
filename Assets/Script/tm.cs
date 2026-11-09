using UnityEngine;

public class tm: MonoBehaviour
{
    public Texture2D texture; // �������ͼƬ

    void Start()
    {
        if (texture == null) return;
        if (!texture.isReadable)
        {
            Debug.LogWarning("贴图 " + texture.name + " 未开启 Read/Write Enabled，跳过透明处理");
            return;
        }
        Color transparent = new Color(1f, 1f, 1f, 0f); // ��ɫ͸��
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r == 255 && pixels[i].g == 255 && pixels[i].b == 255) // ����Ƿ�Ϊ��ɫ
            {
                pixels[i] = transparent; // �滻Ϊ͸��
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(); // Ӧ�ø���
    }
}
