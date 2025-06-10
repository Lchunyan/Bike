using SBPScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public int isFirstTimeScanningDevices = 0;
    public Button StartSearch; //开始查找按钮
    public Button StartConnect; //开始连接按钮


    public bool isScanningServices = false;
    public bool isScanningCharacteristics = false;
    public bool isSubscribed = false;
    public Button serviceScanButton;
    public Text serviceScanStatusText;
    public Dropdown serviceDropdown;
    public Button characteristicScanButton;
    public Text characteristicScanStatusText;
    public Dropdown characteristicDropdown;
    public Button subscribeButton;

    public InputField writeInput;

    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
    string lastError;

    // Start is called before the first frame update







    BleApi.ScanStatus status;
    //设备//----------------------
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    string[] targetDeviceNames = { };//,"55235-1"   "14787-1", "55235-1" //"BB CS1 250350E4",
    float ScanningDevicesTime;

    IDictionary<string, timedate> CadenceValueDic = new Dictionary<string, timedate>(); //每个设备的last踏频值
    private float SetCameraTime = 0f;





    //界面//----------------------
    public InputField DeviceNumText;
    public InputField DeviceText_1;
    public InputField DeviceText_2;
    public InputField DeviceText_3;
    public InputField DeviceText_4;
    public InputField DeviceText_5;
    public InputField DeviceText_6;
    public Text ShowTimeTipsText;
    private GameObject StartViewTrans;
    private Transform Obj321;

    //结算界面
    private GameObject OverViewTrans;
    public Text TodayTimeText;

    //圈数还是时间
    private Toggle RoundToggle;
    private InputField InputRoundText;
    private Toggle TimeToggle;
    private InputField InputTimeText;
    private Text TimeCountdown;


    public GameObject BiycycleStandardTrans;
    public List<GameObject> BicyclePosList;
    private int CloneIndex = 0;
    public List<BicycleController> BicycleControllers = new List<BicycleController>();

    public BicycleCamera MainCamera;



    void Start()
    {
        StartViewTrans = transform.Find("StartView").gameObject;//StartView  StartView
        Obj321 = transform.Find("321");
        Obj321.gameObject.SetActive(false);

        RoundToggle = StartViewTrans.transform.Find("RoundToggle").GetComponent<Toggle>();
        RoundToggle.isOn = false;
        RoundToggle.onValueChanged.AddListener((ison)=>
        {
            if (ison)
            {
                TimeToggle.isOn = false;
            }
            else
            {
                TimeToggle.isOn = true;
            }
        });
        InputRoundText = StartViewTrans.transform.Find("InputRound").Find("InputField (Legacy)").GetComponent<InputField>(); 
        TimeToggle = StartViewTrans.transform.Find("TimeToggle").GetComponent<Toggle>();
        TimeToggle.isOn = true;
        TimeToggle.onValueChanged.AddListener((ison) =>
        {
            if (ison)
            {
                RoundToggle.isOn = false;
            }
            else
            {
                RoundToggle.isOn = true;
            }
        });
        InputTimeText = StartViewTrans.transform.Find("InputTime").Find("InputField (Legacy)").GetComponent<InputField>();
        TimeCountdown = transform.Find("Time_Countdown").Find("time").GetComponent<Text>();
        TimeCountdown.transform.parent.gameObject.SetActive(false);


        ///结算界面
        OverViewTrans = transform.Find("OverView").gameObject;
        TodayTimeText = OverViewTrans.transform.Find("ABLAZING").Find("daytime").GetComponent<Text>();
        //OverViewTrans.SetActive(false);




        BiycycleStandardTrans.gameObject.SetActive(false);
        discoveredDevices.Clear();

        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("0"))) DeviceText_1.text = PlayerPrefs.GetString("0");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("1"))) DeviceText_2.text = PlayerPrefs.GetString("1");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("2"))) DeviceText_3.text = PlayerPrefs.GetString("2");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("3"))) DeviceText_4.text = PlayerPrefs.GetString("3");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("4"))) DeviceText_5.text = PlayerPrefs.GetString("4");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("5"))) DeviceText_6.text = PlayerPrefs.GetString("5");


    }
    
    // Update is called once per frame
    void Update()
    {
        if (isFirstTimeScanningDevices == 1) //第一次查找设备
        {
            ScanningDevicesTime += Time.deltaTime;
            if (ScanningDevicesTime >= 010)// 第一次定时5秒确认一下个数
            {
                Debug.Log("ScanningDevicesTime>6------");
                isFirstTimeScanningDevices = 0;
                ScanningDevicesTime = 0;
                BleApi.StopDeviceScan();


                if (discoveredDevices.Count > 0)
                {
                    foreach (string DeviceID in discoveredDevices.Keys)
                    {
                        Debug.Log("查找到DeviceID-----" + DeviceID);
                    }
                    if (targetDeviceNames.Length == discoveredDevices.Count)
                    {
                        ShowTimeTipsText.text = "查找到" + discoveredDevices.Count + "个设备，请开始连接";  //开始创建

                        StartSearch.interactable = false;
                        StartCoroutine(CreateBicyclesWithDelay());
                    }
                    else if (targetDeviceNames.Length > discoveredDevices.Count)
                    {
                        ShowTimeTipsText.text = "查找到" + discoveredDevices.Count + "个设备，请重新查找再连接";
                    }
                }
                else
                {
                    ShowTimeTipsText.text = "无设备可连接，请重新查找再连接";
                }
            }



            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
            do
            {
                status = BleApi.PollDevice(ref res, false);
                if (status == BleApi.ScanStatus.AVAILABLE)
                {
                    //Debug.Log("res.isConnectable-------" + res.isConnectable + "----" + res.name);
                    if (!devices.ContainsKey(res.id))
                        devices[res.id] = new Dictionary<string, string>() {
                            { "name", "" },
                            { "isConnectable", "False" }
                        };
                    if (res.nameUpdated)
                        devices[res.id]["name"] = res.name;
                    if (res.isConnectableUpdated)
                    {
                        devices[res.id]["isConnectable"] = res.isConnectable.ToString();
                    }
                    // consider only devices which have a name and which are connectable
                    if (devices[res.id]["name"] != "" && devices[res.id]["isConnectable"] == "True")
                    {
                        if (targetDeviceNames.Contains<string>(devices[res.id]["name"]))
                        {
                            if (!discoveredDevices.ContainsKey(res.id))
                            {
                                discoveredDevices.Add(res.id, devices[res.id]["name"]);
                            }

                            if (!CadenceValueDic.ContainsKey(res.id))
                            {
                                timedate d = new timedate();
                                CadenceValueDic.Add(res.id, d);
                            }

                            if (targetDeviceNames.Length == discoveredDevices.Count)
                            {
                                isFirstTimeScanningDevices = 0;
                                ScanningDevicesTime = 0;
                                BleApi.StopDeviceScan();

                                ShowTimeTipsText.text = "查找到" + discoveredDevices.Count + "个设备，请开始连接";  //开始创建

                                StartSearch.interactable = false;
                                StartCoroutine(CreateBicyclesWithDelay());
                            }
                        }
                    }

                }
                else if (status == BleApi.ScanStatus.FINISHED)
                {
                    BleApi.StopDeviceScan();
                    Debug.Log("status == BleApi.ScanStatus.FINISHED----------");
                }
            } while (status == BleApi.ScanStatus.AVAILABLE);
        }
        if (isSubscribed)
        {
            BleApi.BLEData res = new BleApi.BLEData();
            while (BleApi.PollData(out res, false))
            {
                //subcribeText.text = BitConverter.ToString(res.buf, 0, res.size);
                // subcribeText.text = Encoding.ASCII.GetString(res.buf, 0, res.size);

                byte[] packageReceived = res.buf;
                string dID = res.deviceId;
                //Debug.Log(dID+"----DID");

                //第一次点击连接之后开始有数据  
                if (isFirstTimeScanningDevices == 2)
                {
                    if (discoveredDevices.Count > 0)
                    {
                        if (discoveredDevices.ContainsKey(dID))
                        {
                            CloneIndex++;
                            Debug.Log("已连接..." + dID);
                            if (CloneIndex == discoveredDevices.Count)
                            {
                                StartViewTrans.SetActive(false);
                                ShowTimeTipsText.text = "已全部连接...";
                                Invoke("ShowTimeTipsTextFalse", 6f);
                           

                                StartCoroutine(IEnumShowOneChildAt321Time()); 
                                isFirstTimeScanningDevices = 3;
                            }
                        }
                    }
                }

                //"已全部连接..."
                if (isFirstTimeScanningDevices == 4)
                {
                    byte flags = packageReceived[0];
                    int index = 1;

                    //bool wheelRevPresent = (flags & 0x01) != 0;
                    bool crankRevPresent = (flags & 0x02) != 0;

                    //if (wheelRevPresent && packageReceived.Length >= index + 6)
                    //{
                    //    uint cumulativeWheelRevs = BitConverter.ToUInt32(packageReceived, index);
                    //    index += 4;
                    //    ushort lastWheelEventTime = BitConverter.ToUInt16(packageReceived, index);
                    //    index += 2;

                    //    Debug.Log("设备名称为" + dID + "轮子转速Wheel Revs: " + cumulativeWheelRevs + " | Last Time: " + lastWheelEventTime);
                    //}




                    if (crankRevPresent && packageReceived.Length >= index + 4)
                    {
                        ushort cumulativeCrankRevs = BitConverter.ToUInt16(packageReceived, index);
                        index += 2;
                        ushort lastCrankEventTime = BitConverter.ToUInt16(packageReceived, index);
                        index += 2;
                        Debug.Log("设备名称为" + dID + "踏频转速Crank Revs: " + cumulativeCrankRevs + " | Last Time: " + lastCrankEventTime);
                        //float currentTime = Time.time;
                        if (CadenceValueDic.ContainsKey(dID))
                        {
                            timedate d = CadenceValueDic[dID];
                            d.currentCadenceValue = cumulativeCrankRevs;
                            d.detatime = Time.time;
                            if (d.detatime - d.lasttime >= 1f) // 每1秒计算一次
                            {

                                d.lasttime = d.detatime;
                                int delta = d.currentCadenceValue - d.lastCadenceValue;
                                d.lastCadenceValue = cumulativeCrankRevs;
                                UpdateSpeed(dID, delta); // 把增量映射到速度档位
                            }
                        }
                    }
                }
            }


            if (BicycleControllers.Count >= 2)
            {
                SetCameraTime += Time.deltaTime;
                if (SetCameraTime >= 3)
                {
                    // 先对列表按 totalDistance 降序排序
                    List<BicycleController> sortedList = BicycleControllers
                        .OrderByDescending(b => b.totalDistance)
                        .ToList();

                    // 再取第二个元素（下标为 1）
                    if (sortedList.Count >= 2)
                    {
                        BicycleController secondFurthest = sortedList[1];
                        Debug.Log("第二远的是: " + secondFurthest.DeviceID + "，距离为: " + secondFurthest.totalDistance);
                        MainCamera.target = secondFurthest.transform;
                    }

                    SetCameraTime = 0;
                }
            }
        }
    }


    IEnumerator CreateBicyclesWithDelay()
    {
        int LDex = 0;
        foreach (string did in discoveredDevices.Keys)
        {
            Debug.Log("创建自行车对象+1");
            GameObject BiycycleObj = Instantiate(BiycycleStandardTrans,
            BicyclePosList[LDex].transform.position, BicyclePosList[LDex].transform.rotation);
            BiycycleObj.SetActive(true);
            BicycleController con = BiycycleObj.transform.GetComponent<BicycleController>();
            con.enabled = false;
            con.DeviceID = did;
            BicycleControllers.Add(con);
            if (LDex == 0)
            {
                MainCamera.target = BiycycleObj.transform;
            }
            LDex++;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("已全部创建完成...");
    }


    public void OnApplicationQuit()
    {
        BleApi.Quit();
    }

    public void StartStopDeviceScan()
    {
        discoveredDevices.Clear();
        ScanningDevicesTime = 0;


        List<string> deviceNameList = new List<string>();

        PlayerPrefs.DeleteAll();
        if (!string.IsNullOrEmpty(DeviceText_1.text)) deviceNameList.Add(DeviceText_1.text);
        if (!string.IsNullOrEmpty(DeviceText_2.text)) deviceNameList.Add(DeviceText_2.text);
        if (!string.IsNullOrEmpty(DeviceText_3.text)) deviceNameList.Add(DeviceText_3.text);
        if (!string.IsNullOrEmpty(DeviceText_4.text)) deviceNameList.Add(DeviceText_4.text);
        if (!string.IsNullOrEmpty(DeviceText_5.text)) deviceNameList.Add(DeviceText_5.text);
        if (!string.IsNullOrEmpty(DeviceText_6.text)) deviceNameList.Add(DeviceText_6.text);
        targetDeviceNames = deviceNameList.ToArray();

        for (int i = 0; i < targetDeviceNames.Length; i++)
        {
            PlayerPrefs.SetString(i.ToString(), targetDeviceNames[i]);
        }

        BleApi.StartDeviceScan();
        isFirstTimeScanningDevices = 1;
    }


    public void Subscribe2() /////////////////////////////////////////////////////////////////////
    {
        if (targetDeviceNames.Length != discoveredDevices.Count)
        {
            ShowTimeTipsText.text = "请查找到对应设备再连接";
            return;
        }

        foreach (string DeviceID in discoveredDevices.Keys)
        {
            BleApi.SubscribeCharacteristic(DeviceID, "{00001816-0000-1000-8000-00805f9b34fb}", "{00002a5b-0000-1000-8000-00805f9b34fb}", false);
        }
        isSubscribed = true;
        isFirstTimeScanningDevices = 2;//第一次点击连接
    }
    void UpdateSpeed(string SID, int delta)
    {
        float speed = 1;

        if (delta <= 1)
            speed = 2f;
        else if (delta <= 4)
            speed = 10;
        else if (delta <= 6)
            speed = 6;
        else if (delta <= 8)
            speed = 8;
        else if (delta <= 10)
            speed = 10;
        else if (delta <= 12)
            speed = 10;
        else if (delta <= 14)
            speed = 10;
        else if (delta <= 16)
            speed = 10;
        else
            speed = 10;

        Debug.Log(SID + "--的speed---------" + speed);

        //if (isFirstTimeScanningDevices == 4)
        {
            for (int i = 0; i < BicycleControllers.Count; i++)
            {
                if (BicycleControllers[i].DeviceID == SID)
                {
                    BicycleControllers[i].topSpeed = speed;
                    break;
                }
            }
        }
    }



    void ShowTimeTipsTextFalse()
    {
        ShowTimeTipsText.text = string.Empty;
        ShowTimeTipsText.gameObject.SetActive(false);
    }

    IEnumerator IEnumShowOneChildAt321Time()
    {
        yield return new WaitForSeconds(0.1f);
        Obj321.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        int count = Obj321.childCount;
        for (int i = 0; i < count; i++)
        {
            // 先全部隐藏
            for (int j = 0; j < count; j++)
            {
                Obj321.GetChild(j).gameObject.SetActive(false);
            }
            // 激活当前要显示的
            Obj321.GetChild(i).gameObject.SetActive(true);
            StartCoroutine(IEnumTime321ChangeBig(Obj321.GetChild(i).transform));
           
            // 等待 delay 秒
            yield return new WaitForSeconds(1.1f);
        }

        yield return new WaitForSeconds(0.2f);
        Obj321.gameObject.SetActive(false);
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            BicycleControllers[i].enabled = true;
        }

        yield return new WaitForSeconds(0.1f);
        isFirstTimeScanningDevices = 4;

        //判断是圈数还是时间
        if(RoundToggle.isOn)
        {
            int input_round = int.Parse(InputRoundText.text);
        }
        else if(TimeToggle.isOn)
        {
            int input_time = int.Parse(InputTimeText.text);
            StartCoroutine(IEnumTimeCountdown(input_time)); // 倒计时  分钟
        }                                    
    }
    /// <summary>
    /// 321逐渐变大
    /// </summary>
    /// <param name="timetrans"></param>
    /// <returns></returns>
    IEnumerator IEnumTime321ChangeBig(Transform timetrans)
    {
        Vector3 startScale = new Vector3(0.2f, 0.2f, 0.2f);
        Vector3 targetScale = Vector3.one;
        float duration = 0.8f;
        float timer = 0f;
        timetrans.localScale = startScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            timetrans.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null; // 等待下一帧
        }

        timetrans.localScale = targetScale; // 确保最终完全到位
    }


    IEnumerator IEnumTimeCountdown(int time)
    {
        TimeCountdown.transform.parent.gameObject.SetActive(true);
        int totalSeconds = time * 60;

        while (totalSeconds > 0)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            TimeCountdown.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            yield return new WaitForSeconds(1f);
            totalSeconds--;
        }

        // 最后显示 00:00
        TimeCountdown.text = "00:00";
        //结束


        DateTime now = DateTime.Now;
        // 自定义日期格式：日 + 月英文简称 + 年
        string formattedDate = now.ToString("dd MMM yyyy");
        TodayTimeText.text = formattedDate;
    }




}


public class timedate
{
    public float detatime;
    public float lasttime;
    public int currentCadenceValue;
    public int lastCadenceValue;

}













//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class Demo : MonoBehaviour
//{
//    public bool isScanningDevices = false;
//    public bool isScanningDevices2 = false; //断线重连


//    public bool isScanningServices = false;
//    public bool isScanningCharacteristics = false;
//    public bool isSubscribed = false;
//    public GameObject deviceScanResultProto;
//    public Button serviceScanButton;
//    public Text serviceScanStatusText;
//    public Dropdown serviceDropdown;
//    public Button characteristicScanButton;
//    public Text characteristicScanStatusText;
//    public Dropdown characteristicDropdown;
//    public Button subscribeButton;
//    public Text subcribeText;
//    public Button writeButton;
//    public InputField writeInput;
//    public Text errorText;

//    Transform scanResultRoot;
//    Dictionary<string, string> characteristicNames = new Dictionary<string, string>();
//    Dictionary<string, Dictionary<string, string>> devices = new Dictionary<string, Dictionary<string, string>>();
//    string lastError;

//    // Start is called before the first frame update



//    //设备//----------------------
//    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
//    string[] targetDeviceNames = { "14787-1", "55235-1" };//,"55235-1"
//    float ScanningDevicesTime;
//    private bool isWrite;
//    private float betwentime;

//    IDictionary<string, int> lastCadenceValueDic = new Dictionary<string, int>();
//    float lastTime = 0f;


//    //界面//----------------------
//    public InputField DeviceNumText;
//    public InputField DeviceText_1;
//    public InputField DeviceText_2;
//    public InputField DeviceText_3;
//    public InputField DeviceText_4;
//    public InputField DeviceText_5;
//    public InputField DeviceText_6;

//    void Start()
//    {
//        discoveredDevices.Clear();
//        scanResultRoot = deviceScanResultProto.transform.parent;
//        deviceScanResultProto.transform.SetParent(null);
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        BleApi.ScanStatus status;

//        if (isScanningDevices2)
//        {
//            BleApi.DeviceUpdate res2 = new BleApi.DeviceUpdate();
//            do
//            {
//                status = BleApi.PollDevice(ref res2, false);
//                if (status == BleApi.ScanStatus.AVAILABLE)
//                {
//                    if (!devices.ContainsKey(res2.id))
//                        devices[res2.id] = new Dictionary<string, string>() {
//                            { "name", "" },
//                            { "isConnectable", "False" }
//                        };
//                    if (res2.nameUpdated)
//                        devices[res2.id]["name"] = res2.name;
//                    if (res2.isConnectableUpdated)
//                    {
//                        devices[res2.id]["isConnectable"] = res2.isConnectable.ToString();
//                    }
//                    if (devices[res2.id]["name"] != "" && devices[res2.id]["isConnectable"] == "True")
//                    {
//                        if (targetDeviceNames.Contains<string>(devices[res2.id]["name"]))
//                        {
//                            if (!discoveredDevices.ContainsKey(res2.id))
//                            {
//                                discoveredDevices.Add(res2.id, devices[res2.id]["name"]);
//                                if (discoveredDevices.Count == targetDeviceNames.Length)
//                                {
//                                    Subscribe2();
//                                    isScanningDevices2 = false;
//                                    BleApi.StopDeviceScan();
//                                }
//                            }
//                        }
//                    }
//                }
//            } while (status == BleApi.ScanStatus.AVAILABLE);
//        }



//        if (isScanningDevices)  //第一次连
//        {
//            ScanningDevicesTime += Time.deltaTime;
//            if (ScanningDevicesTime >= 5)// 定时5秒确认一下个数
//            {
//                isScanningDevices = false;
//                ScanningDevicesTime = 0;
//                BleApi.StopDeviceScan();


//                if (discoveredDevices.Count > 0)
//                {
//                    Debug.Log("查找到" + discoveredDevices.Count + "个设备，请重新查找或者开始连接");
//                    foreach (string DeviceID in discoveredDevices.Keys)
//                    {
//                        Debug.Log("查找到DeviceID-----" + DeviceID);
//                    }
//                }
//                else
//                {
//                    Debug.Log("查找到0个设备，请重新查找");
//                }
//            }



//            BleApi.DeviceUpdate res = new BleApi.DeviceUpdate();
//            do
//            {
//                status = BleApi.PollDevice(ref res, false);
//                if (status == BleApi.ScanStatus.AVAILABLE)
//                {
//                    // Debug.Log("res.isConnectable-------" + res.isConnectable + "----" + res.name);
//                    if (!devices.ContainsKey(res.id))
//                        devices[res.id] = new Dictionary<string, string>() {
//                            { "name", "" },
//                            { "isConnectable", "False" }
//                        };
//                    if (res.nameUpdated)
//                        devices[res.id]["name"] = res.name;
//                    if (res.isConnectableUpdated)
//                    {
//                        devices[res.id]["isConnectable"] = res.isConnectable.ToString();
//                    }
//                    // consider only devices which have a name and which are connectable
//                    if (devices[res.id]["name"] != "" && devices[res.id]["isConnectable"] == "True")
//                    {
//                        // add new device to list

//                        GameObject g = Instantiate(deviceScanResultProto, scanResultRoot);
//                        g.name = res.id;
//                        g.transform.GetChild(0).GetComponent<Text>().text = devices[res.id]["name"];
//                        g.transform.GetChild(1).GetComponent<Text>().text = res.id;




//                        if (targetDeviceNames.Contains<string>(devices[res.id]["name"]))
//                        {
//                            if (!discoveredDevices.ContainsKey(res.id))
//                            {
//                                discoveredDevices.Add(res.id, devices[res.id]["name"]);
//                                //BleApi.SubscribeCharacteristic(res.id, "{00001816-0000-1000-8000-00805f9b34fb}", "{00002a5b-0000-1000-8000-00805f9b34fb}", false);
//                                //isSubscribed = true;
//                                //Debug.Log("连一个------" + res.id);
//                            }
//                            if (!lastCadenceValueDic.ContainsKey(res.id))
//                            {
//                                lastCadenceValueDic.Add(res.id, 0);
//                            }
//                        }
//                    }



//                    //if (targetDeviceNames.Contains<string>(res.name))
//                    //{
//                    //    if (!discoveredDevices.ContainsKey(res.id))
//                    //    {
//                    //        discoveredDevices.Add(res.id, res.name);

//                    //        if (discoveredDevices.Count <= targetDeviceNames.Length)
//                    //        {
//                    //            Subscribe2();
//                    //            Debug.Log("Subscribe20----------");
//                    //        }
//                    //    }
//                    //}

//                }
//                else if (status == BleApi.ScanStatus.FINISHED)
//                {
//                    isScanningDevices = false;
//                }
//            } while (status == BleApi.ScanStatus.AVAILABLE);
//        }

//        if (isSubscribed)
//        {
//            bool hadData = false;
//            BleApi.BLEData res = new BleApi.BLEData();
//            betwentime += Time.deltaTime;
//            while (BleApi.PollData(out res, false))
//            {
//                hadData = true;
//                isWrite = true;
//                betwentime = 0;
//                subcribeText.text = BitConverter.ToString(res.buf, 0, res.size);
//                // subcribeText.text = Encoding.ASCII.GetString(res.buf, 0, res.size);

//                byte[] packageReceived = res.buf;
//                string dID = res.deviceId;

//                if (discoveredDevices.Count > 0)
//                {
//                    if (discoveredDevices.ContainsKey(dID))
//                    {
//                        discoveredDevices.Remove(dID);
//                        Debug.Log("已连接..." + dID);
//                    }
//                }


//                byte flags = packageReceived[0];
//                int index = 1;

//                bool wheelRevPresent = (flags & 0x01) != 0;
//                bool crankRevPresent = (flags & 0x02) != 0;

//                if (wheelRevPresent && packageReceived.Length >= index + 6)
//                {
//                    uint cumulativeWheelRevs = BitConverter.ToUInt32(packageReceived, index);
//                    index += 4;
//                    ushort lastWheelEventTime = BitConverter.ToUInt16(packageReceived, index);
//                    index += 2;

//                    Debug.Log("设备名称为" + dID + "轮子转速Wheel Revs: " + cumulativeWheelRevs + " | Last Time: " + lastWheelEventTime);
//                }




//                if (crankRevPresent && packageReceived.Length >= index + 4)
//                {
//                    ushort cumulativeCrankRevs = BitConverter.ToUInt16(packageReceived, index);
//                    index += 2;
//                    ushort lastCrankEventTime = BitConverter.ToUInt16(packageReceived, index);
//                    index += 2;

//                    Debug.Log("设备名称为" + dID + "踏频转速Crank Revs: " + cumulativeCrankRevs + " | Last Time: " + lastCrankEventTime);
//                    float currentTime = Time.time;
//                    if (currentTime - lastTime >= 3.0f) // 每秒计算一次
//                    {
//                        int currentCadenceValue = cumulativeCrankRevs; // 获取当前踏频值
//                        int delta = currentCadenceValue - lastCadenceValueDic[dID];
//                        Debug.Log(delta + "-----------delta" + "--dID---" + dID);
//                        lastCadenceValueDic[dID] = currentCadenceValue;
//                        lastTime = currentTime;

//                        UpdateSpeed(dID, delta); // 把增量映射到速度档位
//                    }
//                }


//            }
//            if (!hadData && isWrite)
//            {
//                if (betwentime > 1f) // 1秒没数据
//                {
//                    Debug.LogWarning("---所有设备可能断开或没有响应");
//                    OnApplicationQuit();
//                    isWrite = false;
//                    betwentime = 0;
//                    isSubscribed = false;
//                    BleApi.StartDeviceScan();
//                    isScanningDevices2 = true;
//                }
//            }
//            //try
//            //{

//            //}
//            //catch (Exception e)
//            //{
//            //    Debug.Log("catch----------!BleApi.PollData");
//            //}


//            //if (!hadData)
//            {
//                // 没有任何数据时执行
//                //OnApplicationQuit();
//                //Debug.Log("!hadData   OnApplicationQuit------------------------");
//            }












//        }




//        {
//            // log potential errors
//            BleApi.ErrorMessage res = new BleApi.ErrorMessage();
//            BleApi.GetError(out res);
//            if (lastError != res.msg)
//            {
//                Debug.LogError(res.msg);
//                errorText.text = res.msg;
//                lastError = res.msg;
//            }
//        }
//    }

//    public void OnApplicationQuit()
//    {
//        BleApi.Quit();
//    }

//    public void StartStopDeviceScan()
//    {
//        for (int i = scanResultRoot.childCount - 1; i >= 0; i--)
//            Destroy(scanResultRoot.GetChild(i).gameObject);
//        discoveredDevices.Clear();
//        ScanningDevicesTime = 0;
//        BleApi.StartDeviceScan();
//        isScanningDevices = true;


//        List<string> deviceNameList = new List<string>();
//        if (!string.IsNullOrEmpty(DeviceText_1.text)) deviceNameList.Add(DeviceText_1.text);
//        if (!string.IsNullOrEmpty(DeviceText_2.text)) deviceNameList.Add(DeviceText_2.text);
//        if (!string.IsNullOrEmpty(DeviceText_3.text)) deviceNameList.Add(DeviceText_3.text);
//        if (!string.IsNullOrEmpty(DeviceText_4.text)) deviceNameList.Add(DeviceText_4.text);
//        if (!string.IsNullOrEmpty(DeviceText_5.text)) deviceNameList.Add(DeviceText_5.text);
//        if (!string.IsNullOrEmpty(DeviceText_6.text)) deviceNameList.Add(DeviceText_6.text);
//        string[] targetDeviceNames = deviceNameList.ToArray();
//    }




//    public void Subscribe2() /////////////////////////////////////////////////////////////////////
//    {
//        foreach (string DeviceID in discoveredDevices.Keys)
//        {
//            BleApi.SubscribeCharacteristic(DeviceID, "{00001816-0000-1000-8000-00805f9b34fb}", "{00002a5b-0000-1000-8000-00805f9b34fb}", false);
//        }
//        isSubscribed = true;
//    }









//    void UpdateSpeed(string SID, int delta)
//    {
//        int speed = 1;

//        if (delta <= 0)
//            speed = 0;
//        else if (delta <= 2)
//            speed = 2;
//        else if (delta <= 4)
//            speed = 4;
//        else if (delta <= 6)
//            speed = 6;
//        else if (delta <= 8)
//            speed = 8;
//        else if (delta <= 10)
//            speed = 10;
//        else if (delta <= 12)
//            speed = 12;
//        else if (delta <= 14)
//            speed = 14;
//        else if (delta <= 16)
//            speed = 16;
//        else
//            speed = 20;

//        Debug.Log(SID + "--的speed---------" + speed);
//    }

//}

