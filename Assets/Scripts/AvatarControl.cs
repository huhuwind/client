using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AvatarControl : MonoBehaviour
{

    public TextAsset bsname;
    private List<string> ue4BsList;
    public AudioSource audio;
    //头部mesh
    public SkinnedMeshRenderer headSkinMesh;
    public Text text;
    public Button button;


    // TODO需要修改对应映射
    private Dictionary<string, string> UE2ARkit = new Dictionary<string, string> {
        {"A26_Jaw_Forward"   ,"jaw_thrust_c"},
        {"A27_Jaw_Left"     ,"jaw_sideways_l"},
        {"A28_Jaw_Right"     ,"jaw_sideways_r"    },
        {"A25_Jaw_Open"     ,"mouth_stretch_c"   },
        {"A37_Mouth_Close"   ,"mouth_chew_c"  },
        {"A29_Mouth_Funnel"  ,"mouth_funnel_dl,mouth_funnel_dr,mouth_funnel_ul,mouth_funnel_ur"   },
        {"Mouth_Pucker"    ,"mouth_pucker_l,mouth_pucker_r"     },
        {"Mouth_L"    ,"mouth_sideways_l"          },
        {"Mouth_R"    ,"mouth_sideways_r"      },
        {"Mouth_Smile_L"      ,"mouth_lipCornerPull_l"     },
        {"Mouth_Smile_R"     ,"mouth_lipCornerPull_r" },
        {"Mouth_Frown_L"      ,"mouth_lipCornerDepress_l,mouth_lipCornerDepressFix_l"},
        {"Mouth_Frown_R"     ,"mouth_lipCornerDepress_r,mouth_lipCornerDepressFix_r"},
        {"Mouth_Dimple_L"     ,"mouth_dimple_l"},
        {"Mouth_Dimple_R"    ,"mouth_dimple_r"},
        {"A50_Mouth_Stretch_Left"    ,"mouth_lipStretch_l"},
        {"A51_Mouth_Stretch_Right"   ,"mouth_lipStretch_r"},
        {"A34_Mouth_Roll_Lower"      ,"mouth_suck_dl,mouth_suck_dr"},
        {"A33_Mouth_Roll_Upper"      ,"mouth_suck_ul,mouth_suck_ur"},
        {"A36_Mouth_Shrug_Lower"      ,"mouth_chinRaise_d"     },
        {"A35_Mouth_Shrug_Upper"       ,"mouth_chinRaise_u"     },
        {"A48_Mouth_Press_Left"      ,"mouth_press_l"     },
        {"A49_Mouth_Press_Right"     ,"mouth_press_r" },
        {"A46_Mouth_Lower_Down_Left"   ,"mouth_lowerLipDepress_l"   },
        {"A47_Mouth_Lower_Down_Right" ,"mouth_lowerLipDepress_r"},
        {"A44_Mouth_Upper_Up_Left"     ,"mouth_upperLipRaise_l" },
        {"A45_Mouth_Upper_Up_Right"   ,"mouth_upperLipRaise_r" },
    };



    private Dictionary<string, string> modelBsName2ARKit = new Dictionary<string, string> { };

    void Start()
    {
        //设置游戏帧率30fps
        Application.targetFrameRate = 30;

        //读取配置文件
        ue4BsList = bsname.text.Split("\n").ToList();
        btn_onclick(text);
    }

    // Update is called once per frame

    float time = 0;
    void Update()
    {
        if (headSkinMesh != null) {
            time += Time.deltaTime;
            //每隔3秒播放一次眨眼动画
            if (time >= 3) {
                time = 0.0f;
                StartCoroutine(BlinkEye());
            }
        }
    }

    //眨眼动画
    private IEnumerator BlinkEye() {
        List<float> weight = new List<float> { 0, 20, 40, 60, 80, 100, 80, 60, 40, 20, 0 };
        for (var k = 0; k < weight.Count; k++) {
            headSkinMesh.SetBlendShapeWeight(23, weight[k]);
            yield return new WaitForEndOfFrame();
        }
    }


    public IEnumerator SetBsWeight(List<List<float>> valueList, List<string> ue4BsList) {
        SkinnedMeshRenderer smr = headSkinMesh;
        //CharacterAnimation.RunAnimation(1000);
        int bsCount = smr.sharedMesh.blendShapeCount;
        Debug.Log(valueList.Count);
        for (int i = 0; i < valueList.Count; i++) {

            List<float> list = valueList[i];
            for (int j = 0; j < bsCount; j++) {
                string bsName = smr.sharedMesh.GetBlendShapeName(j);
                modelBsName2ARKit.TryGetValue(bsName, out string artKitName);
                Debug.Log(artKitName);
                if (!string.IsNullOrEmpty(artKitName)) {
                    UE2ARkit.TryGetValue(artKitName, out string ue4Bs116Name);

                    if (!string.IsNullOrEmpty(ue4Bs116Name)) {
                        
                        string[] ue4Bs116Names = ue4Bs116Name.Split(",");
                        float weight = 0;
                        if (ue4Bs116Names.Length == 1) {
                            int ue4bsIndex = ue4BsList.FindIndex(e => e == ue4Bs116Name);
                            if (ue4bsIndex >= 0 && ue4bsIndex < list.Count) {
                                weight = Remap(list[ue4bsIndex]);
                            }

                        } else {
                            weight = 1;
                            List<float> weightList = new List<float> { };
                            for (var k = 0; k < 4; k++) {
                                int ue4bsIndex = ue4BsList.FindIndex(e => e == ue4Bs116Name);
                                if (ue4bsIndex == -1) {
                                    weightList.Add(0);
                                } else {
                                    weight = Remap(list[ue4bsIndex]);
                                    weightList.Add(weight);
                                }
                            }
                            weight = Mathf.Max(Remap(weightList[0]), Remap(weightList[1]), Remap(weightList[2]), Remap(weightList[3]));
                        }

                        smr.SetBlendShapeWeight(j, weight);
                    }
                }
            }
            if (i == valueList.Count - 1) {
                //CharacterAnimation.RunAnimation(0);
            }
            yield return new WaitForEndOfFrame();
        }
    }
    private float Remap(float v) {
        return v * 150;
    }

   
    //请求
    public void SendMsg(string msg) {
        //设置服务器地址
        string bsUrl = "http://10.163.225.5:8000?text=" + msg;
        Debug.Log(bsUrl);
        Debug.Log(msg); 
        StartCoroutine(RequestAudioAndWeight(bsUrl, msg));
    }
    public void btn_onclick(Text text)
    {
        string msg = text.text;
        SendMsg(msg);
    }

    IEnumerator RequestAudioAndWeight(string url, string msg) {
        using (UnityWebRequest www = UnityWebRequest.Get(url)) {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                //获取响应头中的内容类型
                string contentType = www.GetResponseHeader("Content-Type");
                if (contentType == "application/json") {
                    //将响应内容转换成json格式的字符串
                    string jsonStr = www.downloadHandler.text;
                    Debug.Log(jsonStr);

                    //解析json字符串，获取二进制数据和权重数据
                    JObject jsonObject = JObject.Parse(jsonStr);
                    byte[] audioData = Encoding.GetEncoding("iso-8859-1").GetBytes(jsonObject.GetValue("audio").ToString());
                    Debug.Log("dwx   len" + audioData.Length);
                    string weightData = jsonObject.GetValue("weight").ToString();
                    AudioClip audioClip = ConvertBytesToClip(audioData);
                    if (audioClip != null) {
                        audio.clip = audioClip;
                    }
                    UpdateModelBsWeight(weightData);
                    Debug.Log("dwx 播放音频");
                    audio.Play();


                } else {
                    Debug.LogError("Invalid Content-Type: " + contentType);
                }
            }
        }
    }

    private void UpdateModelBsWeight(string weights) {

        List<List<float>> valueList = new List<List<float>>();
        List<string> weightPre = weights.Split("\n").ToList();
        Debug.Log(weightPre[1]);
        Debug.Log(weightPre[2]);
        for (int i = 0; i < weightPre.Count; i++) {
            List<string> weight = weightPre[i].Split(',').ToList();
            List<float> list = new List<float>();
            for (int j = 0; j < weight.Count; j++) {
                float.TryParse(weight[j], out float result);
                list.Add(result);
            }
            valueList.Add(list);
        }
        Debug.Log(valueList[5]);
        StartCoroutine(SetBsWeight(valueList, ue4BsList));

    }


    public AudioClip ConvertBytesToClip(byte[] rawData) {
        float[] samples = new float[rawData.Length / 2];
        float rescaleFactor = 32767;
        short st = 0;
        float ft = 0;

        for (int i = 0; i < rawData.Length; i += 2) {
            st = BitConverter.ToInt16(rawData, i);
            ft = st / rescaleFactor;
            samples[i / 2] = ft;
        }

        AudioClip audioClip = AudioClip.Create("mySound", samples.Length, 1, 16000, false);
        audioClip.SetData(samples, 0);
        Debug.Log(audioClip.length);
        return audioClip;
    }



}
