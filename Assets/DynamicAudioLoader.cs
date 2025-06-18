using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class DynamicAudioLoader : MonoBehaviour
{
    private string relativeAudioFolder = "Data"; // 相对 exe 的目录
    private List<string> audioPaths = new List<string>();
    private int currentIndex = 0;
    private AudioSource audioSource;

    void Start()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", relativeAudioFolder);

        // 读取所有音频文件路径
        if (Directory.Exists(fullPath))
        {
            string[] files = Directory.GetFiles(fullPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var path in files)
            {
                if (path.EndsWith(".mp3") || path.EndsWith(".wav") || path.EndsWith(".ogg"))
                {
                    audioPaths.Add(path);
                }
            }

            if (audioPaths.Count > 0)
            {
                // 随机选择一个开始播放
                currentIndex = Random.Range(0, audioPaths.Count);
                StartCoroutine(LoadAndPlayAudio("file://" + audioPaths[currentIndex]));

                // 设置下一个将播放的音频
                currentIndex = (currentIndex + 1) % audioPaths.Count;
            }
        }
        else
        {
            Debug.LogWarning("未找到音频目录：" + fullPath);
        }

        // 创建 AudioSource
        audioSource = transform.GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (audioPaths.Count == 0)
            {
                Debug.LogWarning("音频列表为空！");
                return;
            }

            string path = "file://" + audioPaths[currentIndex];
            StartCoroutine(LoadAndPlayAudio(path));

            currentIndex = (currentIndex + 1) % audioPaths.Count;
        }
    }

    IEnumerator LoadAndPlayAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("加载失败: " + path + " | " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("播放音频: " + path);
            }
        }
    }
}
