using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    float deltaTime = 0.0f;
    void Start()
    {
        QualitySettings.vSyncCount = 0; // �رմ�ֱͬ��
        Application.targetFrameRate = 60;
    }

    
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f; // ƽ���˲�
        float fps = 1.0f / deltaTime;
    }
}
