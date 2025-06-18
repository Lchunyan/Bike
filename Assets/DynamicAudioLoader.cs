using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class DynamicAudioLoader : MonoBehaviour
{
    private string relativeAudioFolder = "Data"; // ��� exe ��Ŀ¼
    private List<string> audioPaths = new List<string>();
    private int currentIndex = 0;
    private AudioSource audioSource;

    void Start()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", relativeAudioFolder);

        // ��ȡ������Ƶ�ļ�·��
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
                // ���ѡ��һ����ʼ����
                currentIndex = Random.Range(0, audioPaths.Count);
                StartCoroutine(LoadAndPlayAudio("file://" + audioPaths[currentIndex]));

                // ������һ�������ŵ���Ƶ
                currentIndex = (currentIndex + 1) % audioPaths.Count;
            }
        }
        else
        {
            Debug.LogWarning("δ�ҵ���ƵĿ¼��" + fullPath);
        }

        // ���� AudioSource
        audioSource = transform.GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (audioPaths.Count == 0)
            {
                Debug.LogWarning("��Ƶ�б�Ϊ�գ�");
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
                Debug.LogError("����ʧ��: " + path + " | " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("������Ƶ: " + path);
            }
        }
    }
}
