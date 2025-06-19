using SBPScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
    private bool HasGetMessage; //点击连接，全部有数据反馈
    private int isFirstTimeScanningDevices = 0;
    public Button StartSearchBtn; //开始查找按钮
    public Button StartConnectBtn; //开始连接按钮


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
    private float IsAllConnectTime = 0f;
    List<string> Lingshi_DevicesIDList = new List<string>();

    //一圈之后记录第一个的dis  以便后面同步
    public Dictionary<int, float> RoundDistanceDic = new Dictionary<int, float>(); 



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

    public Text ShowCurrentDeviceNameText;
    //结算界面
    private GameObject OverViewTrans;
    private Transform RankTrans;
    public Text TodayTimeText;
    private GameObject DisconnectViewTrans;
    private Button DisconnectBackBtn;

    //圈数还是时间
    private Toggle RoundToggle;
    private InputField InputRoundText;
    //public int RoundOverNum;
    private int roundovernum;
    public int RoundOverNum
    {
        get
        {
            return roundovernum;
        }
        set
        {
            roundovernum = value;
            if (value == BicycleControllers.Count)
            {
                //结束
                RoundOverNum = 0;
                StartCoroutine(IEnumCommonOver());
            }
        }
    }
    private Toggle TimeToggle;
    private InputField InputTimeText;
    private Text TimeCountdown;
    private Button RestartBtn;
    private Button BackBtn;
    private Transform SearchDelayTrans;
    private bool IsSameWithLastTime; //重新开始时候判断是否与上一次一致

    //Roundtype的界面
    private Transform RoundTypeRankShowViewTrans;
    private Text RoundTypeTimeText;
    private Text RoundTypeTargetRounText;
    private Text RoundTypeCurrentRounText;
    private Scrollbar RoundTypeScorebar;

    public GameObject BiycycleStandardTrans;
    public List<GameObject> BicyclePosList;
    private int CloneIndex = 0;
    public List<BicycleController> BicycleControllers = new List<BicycleController>();

    public BicycleCamera MainCamera;

    private string relativePath = "Assets/Data/CrankRevsNum.json";
    private CrankDataGroup crankDataGroup;

    void Start()
    {
        StartViewTrans = transform.Find("StartView").gameObject;//StartView  StartView
        ShowCurrentDeviceNameText = transform.Find("ShowCurrentDeviceName").Find("ShowCurrentDeviceName").GetComponent<Text>();//StartView  StartView 
        ShowCurrentDeviceNameText.transform.parent.gameObject.SetActive(false);
        Obj321 = transform.Find("321");
        Obj321.gameObject.SetActive(false);
        SearchDelayTrans = StartViewTrans.transform.Find("SearchDelay").Find("SearchDelayRound");
        SearchDelayTrans.gameObject.SetActive(false);

        RoundToggle = StartViewTrans.transform.Find("RoundToggle").GetComponent<Toggle>();
        RoundToggle.isOn = false;
        RoundToggle.onValueChanged.AddListener((ison) =>
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

        RoundTypeRankShowViewTrans = transform.Find("RoundTypeRankShow");
        Text rounddaytimeText = RoundTypeRankShowViewTrans.Find("ABLAZING").Find("daytime").GetComponent<Text>();
        DateTime now = DateTime.Now;
        // 自定义日期格式：日 + 月英文简称 + 年
        string formattedDate = now.ToString("dd MMM yyyy");
        rounddaytimeText.text = formattedDate;
        RoundTypeRankShowViewTrans.gameObject.SetActive(false);
        RoundTypeTimeText = RoundTypeRankShowViewTrans.Find("Time").Find("time").GetComponent<Text>();
        RoundTypeTargetRounText = RoundTypeRankShowViewTrans.Find("RoundNum").Find("TargetRoundNum").GetComponent<Text>();
        RoundTypeCurrentRounText = RoundTypeRankShowViewTrans.Find("RoundNum").Find("CurrentRoundNum").GetComponent<Text>();
        RoundTypeScorebar = RoundTypeRankShowViewTrans.Find("PerRoundScrollbar").GetComponent<Scrollbar>();

        ///结算界面
        OverViewTrans = transform.Find("OverView").gameObject;
        TodayTimeText = OverViewTrans.transform.Find("ABLAZING").Find("daytime").GetComponent<Text>();
        RankTrans = OverViewTrans.transform.Find("Rank");
        OverViewTrans.SetActive(false);

        RestartBtn = OverViewTrans.transform.Find("Restart").GetComponent<Button>();
        RestartBtn.onClick.AddListener(OnRestartClicked);
        BackBtn = OverViewTrans.transform.Find("Back").GetComponent<Button>();
        BackBtn.onClick.AddListener(OnBackClicked);
        RestartBtn.gameObject.SetActive(false);
        BackBtn.gameObject.SetActive(false);

        DisconnectViewTrans = transform.Find("DisconnectView").gameObject;
        DisconnectViewTrans.gameObject.SetActive(false);
        DisconnectBackBtn = DisconnectViewTrans.transform.Find("DisconnectBack").GetComponent<Button>();
        DisconnectBackBtn.onClick.AddListener(OnDisconnectClicked);

        BiycycleStandardTrans.gameObject.SetActive(false);
        discoveredDevices.Clear();

        GetPlayerPrefs();
        LoadJson();
    }

    void GetPlayerPrefs()
    {
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("0"))) DeviceText_1.text = PlayerPrefs.GetString("0");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("1"))) DeviceText_2.text = PlayerPrefs.GetString("1");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("2"))) DeviceText_3.text = PlayerPrefs.GetString("2");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("3"))) DeviceText_4.text = PlayerPrefs.GetString("3");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("4"))) DeviceText_5.text = PlayerPrefs.GetString("4");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("5"))) DeviceText_6.text = PlayerPrefs.GetString("5");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("InputRoundText"))) InputRoundText.text = PlayerPrefs.GetString("InputRoundText");
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("InputTimeText"))) InputTimeText.text = PlayerPrefs.GetString("InputTimeText");
        int i_TimeToggle = PlayerPrefs.GetInt("TimeToggle");
        if (i_TimeToggle == 1)
        {
            TimeToggle.isOn = true;
            RoundToggle.isOn = false;
        }
        else
        {
            TimeToggle.isOn = false;
            RoundToggle.isOn = true;
        }

        Debug.Log(i_TimeToggle + "TimeToggle.isOn----");

        int i_RoundToggle = PlayerPrefs.GetInt("RoundToggle");
        if (i_RoundToggle == 1)
        {
            RoundToggle.isOn = true;
            TimeToggle.isOn = false;
        }
        else
        {
            RoundToggle.isOn = false;
            TimeToggle.isOn = true;
        }

    }
    void SetPlayerPrefs(List<string> deviceNameList)
    {
        PlayerPrefs.DeleteAll();
        //List<string> deviceNameList = new List<string>();
        if (!string.IsNullOrEmpty(DeviceText_1.text)) deviceNameList.Add(DeviceText_1.text);
        if (!string.IsNullOrEmpty(DeviceText_2.text)) deviceNameList.Add(DeviceText_2.text);
        if (!string.IsNullOrEmpty(DeviceText_3.text)) deviceNameList.Add(DeviceText_3.text);
        if (!string.IsNullOrEmpty(DeviceText_4.text)) deviceNameList.Add(DeviceText_4.text);
        if (!string.IsNullOrEmpty(DeviceText_5.text)) deviceNameList.Add(DeviceText_5.text);
        if (!string.IsNullOrEmpty(DeviceText_6.text)) deviceNameList.Add(DeviceText_6.text);
        if (!string.IsNullOrEmpty(InputTimeText.text))
            PlayerPrefs.SetString("InputTimeText", InputTimeText.text);
        if (!string.IsNullOrEmpty(InputRoundText.text))
            PlayerPrefs.SetString("InputRoundText", InputRoundText.text);
        if (RoundToggle.isOn)
        {
            PlayerPrefs.SetInt("RoundToggle", 1);
            PlayerPrefs.SetInt("TimeToggle", 0);
        }
        else if (TimeToggle.isOn)
        {
            PlayerPrefs.SetInt("RoundToggle", 0);
            PlayerPrefs.SetInt("TimeToggle", 1);

            int i_TimeToggle = PlayerPrefs.GetInt("TimeToggle");
            Debug.Log(i_TimeToggle + "TimeToggle.isOn----");



        }
        targetDeviceNames = deviceNameList.ToArray();
        for (int i = 0; i < targetDeviceNames.Length; i++)
        {
            PlayerPrefs.SetString(i.ToString(), targetDeviceNames[i]);
        }
    }

    /// <summary>
    /// 有断连的，就显示出初始界面，让玩家重新查找
    /// </summary>
    private void InitDevices()
    {
        ShowCurrentDeviceNameText.transform.parent.gameObject.SetActive(false);
        TimeCountdown.transform.parent.gameObject.SetActive(false);
        OverViewTrans.gameObject.SetActive(false);
        StartViewTrans.gameObject.SetActive(true);

        Obj321.gameObject.SetActive(false);
        RestartBtn.gameObject.SetActive(false);
        BackBtn.gameObject.SetActive(false);
        DisconnectViewTrans.gameObject.SetActive(false);
        RoundTypeRankShowViewTrans.gameObject.SetActive(false);

        StartSearchBtn.interactable = true;

        //销毁自行车实例
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            Destroy(BicycleControllers[i].gameObject);
        }
        StopAllCoroutines();
        discoveredDevices.Clear();
        BicycleControllers.Clear();
        CloneIndex = 0;
        isSubscribed = false;
    }

    // Update is called once per frame
    void Update()
    {
        //第一次查找设备
        if (isFirstTimeScanningDevices == 1) //第一次查找设备
        {
            ScanningDevicesTime += Time.deltaTime;
            if (ScanningDevicesTime >= 15)// 第一次定时5秒确认一下个数
            {
                Debug.Log("ScanningDevicesTime>15------");
                isFirstTimeScanningDevices = -1;  //停止查找
                ScanningDevicesTime = 0;
                BleApi.StopDeviceScan();
                SearchDelayTrans.gameObject.SetActive(false);


                if (discoveredDevices.Count > 0)
                {
                    foreach (string DeviceID in discoveredDevices.Keys)
                    {
                        Debug.Log("查找到DeviceID-----" + DeviceID);
                    }
                    if (targetDeviceNames.Length == discoveredDevices.Count)
                    {
                        ShowTimeTipsText.gameObject.SetActive(true);
                        ShowTimeTipsText.text = "查找到" + discoveredDevices.Count + "个设备，请开始连接";  //开始创建------------------------------------

                        StartSearchBtn.interactable = false;
                        StartCoroutine(CreateBicyclesWithDelay());
                    }
                    else if (targetDeviceNames.Length > discoveredDevices.Count)
                    {
                        ShowTimeTipsText.gameObject.SetActive(true);
                        ShowTimeTipsText.text = "只找到" + discoveredDevices.Count + "个设备，请重新查找";
                    }
                }
                else
                {
                    ShowTimeTipsText.gameObject.SetActive(true);
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
                                isFirstTimeScanningDevices = 2; //找到设备，还没链接，准备连接
                                ScanningDevicesTime = 0;
                                BleApi.StopDeviceScan();
                                Debug.Log("查找到" + discoveredDevices.Count + "个设备，请开始连接");
                                ShowTimeTipsText.gameObject.SetActive(true);
                                SearchDelayTrans.gameObject.SetActive(false);
                                ShowTimeTipsText.text = "查找到" + discoveredDevices.Count + "个设备，请开始连接";  //开始创建

                                StartSearchBtn.interactable = false;
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


                //判断是否有掉线的   一直加往Lingshi_DevicesIDList加dID
                if (!Lingshi_DevicesIDList.Contains(dID))
                    Lingshi_DevicesIDList.Add(dID);


                //第一次点击连接之后开始有数据反馈  
                if (isFirstTimeScanningDevices == 3)
                {
                    if (discoveredDevices.Count > 0)
                    {
                        if (discoveredDevices.ContainsKey(dID))
                        {
                            CloneIndex++;
                            Debug.Log("已连接..." + dID);
                            if (CloneIndex == discoveredDevices.Count)
                            {
                                HasGetMessage = true;
                                ShowTimeTipsText.text = "已全部连接...";
                                Invoke("ShowTimeTipsTextFalse", 6f);

                                StartCoroutine(IEnumShowOneChildAt321Time());
                                isFirstTimeScanningDevices = 4;   //全部连接
                            }
                        }
                    }
                }

                //"321倒计时结束"
                if (isFirstTimeScanningDevices == 5)
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
                        //Debug.Log("设备名称为" + dID + "踏频转速Crank Revs: " + cumulativeCrankRevs + " | Last Time: " + lastCrankEventTime);

                        //if (CadenceValueDic.ContainsKey(dID))
                        //{
                        //    timedate d = CadenceValueDic[dID];
                        //    d.currentCadenceValue = cumulativeCrankRevs;
                        //    d.detatime = Time.time;
                        //    float time = crankDataGroup.BetweenTime;
                        //    if (d.detatime - d.lasttime >= time) // 每2秒计算一次
                        //    {
                        //        d.lasttime = d.detatime;
                        //        int CrankRevsNum = d.currentCadenceValue - d.lastCadenceValue;
                        //        d.lastCadenceValue = cumulativeCrankRevs;
                        //        UpdateSpeed(dID, CrankRevsNum); // 把增量映射到速度档位
                        //    }
                        //}

                        if (CadenceValueDic.ContainsKey(dID))
                        {
                            CadenceValueDic[dID].currentCadenceValue = cumulativeCrankRevs;
                            CadenceValueDic[dID].currentCrankEventTime = Time.time;
                            int jsontime = crankDataGroup.BetweenTime;
                            if (CadenceValueDic[dID].currentCrankEventTime - CadenceValueDic[dID].lastCrankEventTime >= jsontime) // 每2s 计算一次
                            {
                                if (CadenceValueDic[dID].lastCadenceValue == 0)
                                    CadenceValueDic[dID].lastCadenceValue = cumulativeCrankRevs - 5;
                                int CrankRevsNum = cumulativeCrankRevs - CadenceValueDic[dID].lastCadenceValue;

                                CadenceValueDic[dID].lastCadenceValue = CadenceValueDic[dID].currentCadenceValue;
                                CadenceValueDic[dID].lastCrankEventTime = CadenceValueDic[dID].currentCrankEventTime;
                                UpdateSpeed(dID, CrankRevsNum); // 把增量映射到速度档位
                            }
                        }
                    }
                }
            }


            if (isFirstTimeScanningDevices == 5) ///321倒计时结束
            {
                SetCameraTime += Time.deltaTime;
                if (SetCameraTime >= 3)
                {
                    // 先对列表按 totalDistance 降序排序
                    List<BicycleController> sortedList = BicycleControllers
                        .OrderByDescending(b => b.totalDistance)
                        .ToList();

                    BicycleController secondFurthest = null;
                    // 再取第二个元素（下标为 1）
                    if (sortedList.Count >= 2)
                    {
                        secondFurthest = sortedList[1];
                        Debug.Log("第二远的是: " + secondFurthest.DeviceID + "，距离为: " + secondFurthest.totalDistance);
                        //MainCamera.target = secondFurthest.transform;
                        MainCamera.SetTarget(secondFurthest.transform);

                        ShowCurrentDeviceNameText.transform.parent.gameObject.SetActive(true);
                        ShowCurrentDeviceNameText.text = secondFurthest.DeviceName;
                    }
                    else
                    {
                        secondFurthest = sortedList[0];
                    }

                    if (RoundToggle.isOn)
                    {
                        RoundTypeTargetRounText.text = secondFurthest.TargetRoundIndex.ToString();
                        RoundTypeCurrentRounText.text = secondFurthest.CurrentRoundIndex.ToString();
                        float size = (float)(secondFurthest.pathPointsCount - secondFurthest.NextGotoPointIndex) / (float)secondFurthest.pathPointsCount;
                        RoundTypeScorebar.size = size;
                    }
                    SetCameraTime = 0;
                }
            }

            if (HasGetMessage)
            {
                //判断断连
                IsAllConnectTime += Time.deltaTime;
                if (IsAllConnectTime >= 5)
                {
                    if (discoveredDevices.Count != 0)
                    {
                        ShowTimeTipsText.text = "";
                        // 找出 discoveredDevices 中有，但 Lingshi_DevicesIDList 中没有的 key
                        var extraKeys = discoveredDevices.Keys.Except(Lingshi_DevicesIDList);
                        if (extraKeys.Any())
                        {
                            // 输出对应的 value 值
                            foreach (var key in extraKeys)
                            {
                                string value = discoveredDevices[key];
                                Debug.Log($"掉线设备: ID = {key}, 名称 = {value}");

                                ShowTimeTipsText.text += $"\n{value}设备断开连接，请重新查找并连接";
                                DisconnectViewTrans.gameObject.SetActive(true);
                                StopAllCoroutines();
                            }
                            HasGetMessage = false;
                        }
                        else
                        {
                            Debug.Log("没有掉线的设备");
                        }

                        Lingshi_DevicesIDList.Clear();
                        IsAllConnectTime = 0;
                    }
                }
            }
        }


        if (Input.GetKeyDown(KeyCode.P))
        {
            InitDevices();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 在编辑器中停止播放
#else
    Application.Quit(); // 在打包后的游戏中退出
#endif
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            LoadJson();
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
            con.DeviceName = discoveredDevices[did];
            BicycleControllers.Add(con);
            if (LDex == 0)
            {
                MainCamera.SetTarget(BiycycleObj.transform);
                //MainCamera.target = BiycycleObj.transform;
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
        if (isFirstTimeScanningDevices == 1)
            return;
        List<string> deviceNameList = new List<string>();
        SetPlayerPrefs(deviceNameList);

        ScanningDevicesTime = 0;
        //判断是否一样 BicycleControllers
        var controllerNames = BicycleControllers.Select(b => b.DeviceName);
        bool isContentMatch = new HashSet<string>(controllerNames).SetEquals(deviceNameList);
        if (!isContentMatch) //如果不一样
        {
            IsSameWithLastTime = false;
            discoveredDevices.Clear();
            devices.Clear();
            //销毁自行车实例
            for (int i = 0; i < BicycleControllers.Count; i++)
            {
                Destroy(BicycleControllers[i].gameObject);
            }
            BicycleControllers.Clear();
            CloneIndex = 0;
            OnApplicationQuit();
            StartCoroutine(IEnumSearchDelay());
        }
        else //如果一样，就不需要再查找  直接321开始
        {
            IsSameWithLastTime = true;
            ShowTimeTipsText.gameObject.SetActive(true);
            ShowTimeTipsText.text = "已有连接设备，请直接开始连接";
        }
    }


    public void Subscribe2() /////////////////////////////////////////////////////////////////////
    {
        if (isFirstTimeScanningDevices == 2) ////找到设备，还没链接，准备连接
        {
            foreach (string DeviceID in discoveredDevices.Keys)
            {
                BleApi.SubscribeCharacteristic(DeviceID, "{00001816-0000-1000-8000-00805f9b34fb}", "{00002a5b-0000-1000-8000-00805f9b34fb}", false);
            }
            isSubscribed = true;
            isFirstTimeScanningDevices = 3;//连接ing...
        }
        else
        {
            List<string> deviceNameList = new List<string>();
            SetPlayerPrefs(deviceNameList);
            //判断是否一样 BicycleControllers
            var controllerNames = BicycleControllers.Select(b => b.DeviceName);
            bool isContentMatch = new HashSet<string>(controllerNames).SetEquals(deviceNameList);
            if (!isContentMatch) //如果不一样
            {
                ShowTimeTipsText.gameObject.SetActive(true);
                ShowTimeTipsText.text = "请先查找到对应设备再连接";
                return;
            }
            else
            {
                StartCoroutine(IEnumShowOneChildAt321Time());
            }
        }
    }
    void UpdateSpeed(string SID, int CrankRevsNum)
    {
        float speed = 0;

        if (CrankRevsNum <= crankDataGroup.values[0].CrankRevsNum)
            speed = crankDataGroup.values[0].speed;
        else if (CrankRevsNum <= crankDataGroup.values[1].CrankRevsNum)
            speed = crankDataGroup.values[1].speed;
        else if (CrankRevsNum <= crankDataGroup.values[2].CrankRevsNum)
            speed = crankDataGroup.values[2].speed;
        else if (CrankRevsNum <= crankDataGroup.values[3].CrankRevsNum)
            speed = crankDataGroup.values[3].speed;
        else if (CrankRevsNum <= crankDataGroup.values[4].CrankRevsNum)
            speed = crankDataGroup.values[4].speed;
        else if (CrankRevsNum <= crankDataGroup.values[5].CrankRevsNum)
            speed = crankDataGroup.values[5].speed;
        else if (CrankRevsNum <= crankDataGroup.values[6].CrankRevsNum)
            speed = crankDataGroup.values[6].speed;
        else if (CrankRevsNum <= crankDataGroup.values[7].CrankRevsNum)
            speed = crankDataGroup.values[7].speed;
        else if (CrankRevsNum <= crankDataGroup.values[8].CrankRevsNum)
            speed = crankDataGroup.values[8].speed;
        else if (CrankRevsNum <= crankDataGroup.values[9].CrankRevsNum)
            speed = crankDataGroup.values[9].speed;
        else
            speed = 30;

        Debug.Log(SID + "--的speed---------" + speed);
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            if (BicycleControllers[i].DeviceID == SID)
            {
                if (RoundToggle.isOn)
                {
                    if (!BicycleControllers[i].isRoundRunning)
                    {
                        break;
                    }
                }
                BicycleControllers[i].topSpeed = speed;
                if (speed >= 1)
                {
                    BicycleControllers[i].enabled = true;
                    BicycleControllers[i].rb.isKinematic = false;
                }
                break;
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
        StartViewTrans.SetActive(false);

        RestartBtn.gameObject.SetActive(false);
        BackBtn.gameObject.SetActive(false);

        ShowTimeTipsText.gameObject.SetActive(false);
        Obj321.gameObject.SetActive(true);

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

        yield return new WaitForSeconds(0.3f);
        Obj321.gameObject.SetActive(false);
        isSubscribed = true;
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            BicycleControllers[i].enabled = true;
            BicycleControllers[i].totalDistance = 0;
            BicycleControllers[i].lastPosition = BicycleControllers[i].transform.position;
            Rigidbody rig = BicycleControllers[i].transform.GetComponent<Rigidbody>();
            rig.isKinematic = false;
        }

        yield return new WaitForSeconds(0.1f);
        isFirstTimeScanningDevices = 5;//倒计时结束

        //判断是圈数还是时间  -----------------------------------------------                  判断是圈数还是时间 ----------------------
        if (RoundToggle.isOn)
        {
            int input_round = int.Parse(InputRoundText.text);
            for (int i = 0; i < BicycleControllers.Count; i++)
            {
                BicycleControllers[i].TargetRoundIndex = input_round;
                BicycleControllers[i].StartTimer();
            }


            RoundTypeRankShowViewTrans.gameObject.SetActive(true);
            RoundTypeTargetRounText.text = input_round.ToString();
            RoundTypeCurrentRounText.text = "0";
            RoundTypeScorebar.size = 1;
            StartCoroutine(IEnumRoundTypeTime());
        }
        else if (TimeToggle.isOn)
        {
            float input_time = float.Parse(InputTimeText.text);
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

    /// <summary>
    /// 321之后执行倒计时的开始逻辑
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator IEnumTimeCountdown(float time)
    {
        TimeCountdown.transform.parent.gameObject.SetActive(true);
        float totalSeconds = time * 60;
        Debug.Log(totalSeconds + "totalSeconds--------------------------");

        while (totalSeconds > 0)
        {
            int minutes = (int)totalSeconds / 60;
            int seconds = (int)totalSeconds % 60;

            TimeCountdown.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

            yield return new WaitForSeconds(1f);
            totalSeconds--;
        }

        // 最后显示 00:00----------------------------------------------------------------------最后显示 00:00
        TimeCountdown.text = "00:00";

        StartCoroutine(IEnumCommonOver());
    }

    /// <summary>
    /// 321之后执行圈数Type的倒计时
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    IEnumerator IEnumRoundTypeTime()
    {
        float elapsedTime = 0f;
        while (true)
        {
            elapsedTime += Time.deltaTime;

            int hours = Mathf.FloorToInt(elapsedTime / 3600);
            int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);

            RoundTypeTimeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

            yield return null;
        }
    }


    IEnumerator IEnumCommonOver()
    {
        //结束
        // isSubscribed = false;            //结束了不能置为false-----------------------------------------------------------
        isFirstTimeScanningDevices = -1;
        // 将 Keys 复制成列表，避免枚举器被修改
        List<string> keys = new List<string>(CadenceValueDic.Keys);
        foreach (string key in keys)
        {
            timedate tt = new timedate();
            CadenceValueDic[key] = tt;
        }
        ShowCurrentDeviceNameText.transform.parent.gameObject.SetActive(false);
        RoundTypeRankShowViewTrans.gameObject.SetActive(false);
        OverViewTrans.SetActive(true);
        RoundDistanceDic.Clear();
        DateTime now = DateTime.Now;
        // 自定义日期格式：日 + 月英文简称 + 年
        string formattedDate = now.ToString("dd MMM yyyy");
        TodayTimeText.text = formattedDate;



        int childc = RankTrans.childCount;
        for (int i = 0; i < childc; i++)
        {
            RankTrans.GetChild(i).gameObject.SetActive(false);
        }


        if (TimeToggle.isOn)
        {
            // 先对列表按 totalDistance 降序排序 
            List<BicycleController> sortedList = BicycleControllers
            .OrderByDescending(b => b.totalDistance)
            .ToList();
            for (int i = 0; i < sortedList.Count; i++)  //再根据sortedList显示
            {
                Transform perchild = RankTrans.GetChild(i);
                perchild.gameObject.SetActive(true);

                Text TimeT = perchild.Find("M_time").GetComponent<Text>(); //总时间
                float input_time = float.Parse(InputTimeText.text);
                TimeT.text = input_time.ToString("F1");

                Text RankName = perchild.Find("RankName").GetComponent<Text>(); //名字
                RankName.text = sortedList[i].DeviceName;

                Text DisText = perchild.Find("+dis").Find("dis").GetComponent<Text>(); //总公里
                float itotaldis = sortedList[i].totalDistance / 1000;  //多少公里
                DisText.text = itotaldis.ToString("F2");

                Text SpeedText = perchild.Find("+speed").Find("speed").GetComponent<Text>(); //总公里/时间
                float fhour = input_time / 60;
                float pjspeed = itotaldis / fhour;
                SpeedText.text = pjspeed.ToString("F1");

                BicycleControllers[i].topSpeed = 1f;
                //BicycleControllers[i].totalDistance = 0f;
            }

            yield return new WaitForSeconds(2f);
            for (int i = 0; i < BicycleControllers.Count; i++)
            {
                Rigidbody rig = BicycleControllers[i].transform.GetComponent<Rigidbody>();
                rig.isKinematic = true;
                BicycleControllers[i].NextGotoPointIndex = 0;
                BicycleControllers[i].enabled = false;
            }
            RestartBtn.gameObject.SetActive(true);
            BackBtn.gameObject.SetActive(true);
        }
        else if (RoundToggle.isOn)
        {
            // 先对列表按 time 升序排序 
            List<BicycleController> sortedList = BicycleControllers
            .OrderBy(b => b.AllTime)
            .ToList();
            for (int i = 0; i < sortedList.Count; i++)  //再根据sortedList显示
            {
                Transform perchild = RankTrans.GetChild(i);
                perchild.gameObject.SetActive(true);

                Text TimeT = perchild.Find("M_time").GetComponent<Text>(); //总时间
                float input_time = sortedList[i].AllTime / 60;  //分钟
                TimeT.text = input_time.ToString("F1");

                Text RankName = perchild.Find("RankName").GetComponent<Text>(); //名字
                RankName.text = sortedList[i].DeviceName;

                Text DisText = perchild.Find("+dis").Find("dis").GetComponent<Text>(); //总公里
                float itotaldis = sortedList[i].totalDistance / 1000;  //多少公里
                DisText.text = itotaldis.ToString("F2");

                Text SpeedText = perchild.Find("+speed").Find("speed").GetComponent<Text>(); //总公里/时间 =速度
                float fhour = input_time / 60;
                float pjspeed = itotaldis / fhour;
                SpeedText.text = pjspeed.ToString("F1");

                BicycleControllers[i].topSpeed = 1f;
            }

            yield return new WaitForSeconds(2f);
            for (int i = 0; i < BicycleControllers.Count; i++)
            {
                Rigidbody rig = BicycleControllers[i].transform.GetComponent<Rigidbody>();
                rig.isKinematic = true;
                BicycleControllers[i].NextGotoPointIndex = 0;
                BicycleControllers[i].AllTime = 0;
                BicycleControllers[i].enabled = false;
            }
            RestartBtn.gameObject.SetActive(true);
            BackBtn.gameObject.SetActive(true);
        }

        StopAllCoroutines();
    }



    /// <summary>
    /// 循环“亮度流动”动画
    /// </summary>
    /// <returns></returns>
    IEnumerator IEnumSearchDelay()
    {
        yield return new WaitForSeconds(0.2f);
        BleApi.StartDeviceScan();
        HasGetMessage = false;
        isFirstTimeScanningDevices = 1;

        SearchDelayTrans.gameObject.SetActive(true);
        List<RawImage> ListImg = new List<RawImage>();
        for (int n = 0; n < SearchDelayTrans.childCount; n++)
        {
            RawImage img = SearchDelayTrans.GetChild(n).GetComponent<RawImage>();
            ListImg.Add(img);
        }

        ShowTimeTipsText.gameObject.SetActive(true);
        ShowTimeTipsText.text = "查找中...";
        int count = ListImg.Count;
        int i = 0;
        while (true)
        {
            // 所有先设为 140
            for (int j = 0; j < count; j++)
            {
                SetImageAlpha(ListImg[j], 120);
            }

            // 当前滑动的3个
            int index20 = i % count;
            int index60 = (i - 1 + count) % count;
            int index100 = (i - 2 + count) % count;

            SetImageAlpha(ListImg[index100], 80);
            SetImageAlpha(ListImg[index60], 50);
            SetImageAlpha(ListImg[index20], 20);

            i++; // 向前推进
            yield return new WaitForSeconds(0.1f);

            if (isFirstTimeScanningDevices != 1)  //查找状态
            {
                SearchDelayTrans.gameObject.SetActive(false);
                break;
            }
        }
    }

    void SetImageAlpha(RawImage img, float alpha)
    {
        Color c = img.color;
        c.a = alpha / 255f;
        img.color = c;
    }

    /// <summary>
    /// 重新开始按钮
    /// </summary>
    void OnRestartClicked()
    {
        Debug.Log("Restart button clicked!");
        // 在这里添加再来一局逻辑



        //重置位置
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            BicycleControllers[i].transform.position = BicyclePosList[i].transform.position;
            BicycleControllers[i].transform.rotation = Quaternion.Euler(0, -90, 0);
        }
        OverViewTrans.gameObject.SetActive(false);
        StartCoroutine(IEnumShowOneChildAt321Time());
    }
    void OnBackClicked()
    {
        Debug.Log("OnBack button clicked!");
        // 在这里添加返回逻辑

        //重置位置
        for (int i = 0; i < BicycleControllers.Count; i++)
        {
            BicycleControllers[i].transform.position = BicyclePosList[i].transform.position;
            BicycleControllers[i].transform.rotation = Quaternion.Euler(0, -90, 0);
        }

        OverViewTrans.gameObject.SetActive(false);
        StartViewTrans.gameObject.SetActive(true);
        StartSearchBtn.interactable = true;
    }

    /// <summary>
    /// 油设备断开连接
    /// </summary>
    void OnDisconnectClicked()
    {
        InitDevices();
    }
    void LoadJson()
    {
        string jsonPath;

#if UNITY_EDITOR
        // 编辑器中：直接从 Assets 开始
        jsonPath = Path.Combine(Application.dataPath, "Data/CrankRevsNum.json");
#else
    // 打包后：使用 exe 同目录的 Data 文件夹
    jsonPath = Path.Combine(Application.dataPath, "..", "Data", "CrankRevsNum.json");
#endif

        if (!File.Exists(jsonPath))
        {
            Debug.LogError("找不到 JSON 文件: " + Path.GetFullPath(jsonPath));
            return;
        }

        string json = File.ReadAllText(jsonPath);
        crankDataGroup = JsonUtility.FromJson<CrankDataGroup>(json);

        Debug.Log($"读取成功 BetweenTime: {crankDataGroup.BetweenTime}");
        foreach (var item in crankDataGroup.values)
        {
            Debug.Log($"CrankRevsNum: {item.CrankRevsNum}, speed: {item.speed}");
        }
    }



}


public class timedate
{
    public float detatime;
    public float currentCrankEventTime;
    public float lastCrankEventTime;
    public float lasttime;
    public int currentCadenceValue;
    public int lastCadenceValue;
}








[System.Serializable]
public class CrankData
{
    public int CrankRevsNum;
    public float speed;
}

[System.Serializable]
public class CrankDataGroup
{
    public int BetweenTime;
    public List<CrankData> values;
}
