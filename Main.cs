using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
//using UnityEditorInternal;
using UnityEngine.Networking;

public enum NoteStat{
    Perfect,
    Good,
    Bad,
    Miss,
    None
}

public enum JudgeLineStat
{
    AP,
    FC,
    None
}

public class ScoreCounter
{
    public int perfectCnt;
    public int goodCnt;
    public int badCnt;
    public int missCnt;
    public int combo;
    public int maxcombo;
    public int numOfNotes;
    public void add(NoteStat status)
    {
        switch (status)
        {
            case NoteStat.Perfect:
                perfectCnt++;
                combo++;
                break;
            case NoteStat.Good:
                goodCnt++;
                combo++;
                break;
            case NoteStat.Bad:
                badCnt++;
                combo = 0;
                break;
            case NoteStat.Miss:
                missCnt++;
                combo = 0;
                break;
        }
        if (combo > maxcombo)
            maxcombo = combo;
        if (GlobalSetting.lineStat == JudgeLineStat.AP && goodCnt != 0)
            GlobalSetting.lineStat = JudgeLineStat.FC;
        if (GlobalSetting.lineStat != JudgeLineStat.None && (badCnt != 0 || missCnt != 0))
            GlobalSetting.lineStat = JudgeLineStat.None;
    }
    public float score { get { return 1e6f * (perfectCnt * 0.9f + goodCnt * 0.585f + maxcombo * 0.1f) / numOfNotes; } }
}

public static class GlobalSetting
{
    public static bool playing { get; set; }
    public static string chartpath = "E:\\DESKTOP\\001四月の雨 IN Lv.12\\四月の雨.json";
    public static string chartName = "四月の雨";
    public static string musicPath = "E:\\DESKTOP\\001四月の雨 IN Lv.12\\四月の雨.wav";
    public static string illustrationPath = "E:\\DESKTOP\\001四月の雨 IN Lv.12\\四月の雨.png";
    public static int formatVersion = 3;
    public static Dictionary<float, int> highLightedNotes = new Dictionary<float, int>();
    public static float globalNoteScale = 0.24f;
    public static float musicProgress = 0f;
    public static bool highLight;
    public static bool autoPlay = false;
    public static ScoreCounter scoreCounter = new ScoreCounter();
    public static float noteSpeedFactor = 1f;
    public static float offset;
    public static float userOffset;
    public static string difficulty;
    public static JudgeLineStat lineStat = JudgeLineStat.AP;
    public static Dictionary<JudgeLineStat, Color> lineColors = new Dictionary<JudgeLineStat, Color>();
    public static Dictionary<int, AudioClip> tapSounds = new Dictionary<int, AudioClip>();
    public static float screenHeight;
    public static float screenWidth;
    public static float aspect { get { return screenWidth / screenHeight; } }
    public static float widthOffset { get { return (Screen.width - screenWidth) / 2f; } }

    public static List<JudgeLineMovement> lines = new List<JudgeLineMovement>();

    public static void playNoteSound(int notetype, Vector3 pos)
    {
        AudioSource.PlayClipAtPoint(tapSounds[notetype], Camera.main.transform.position);
    }

    public static void reset()
    {
        playing = false;
        highLightedNotes.Clear();
        musicProgress = 0f;
        scoreCounter = new ScoreCounter();
        noteSpeedFactor = 1f;
    }
}

public class Main : MonoBehaviour
{

    private string chart;
    private int m_frames;
    private float m_fps;
    private float m_lastupdateshowtime;
    private Chart json = new Chart();
    public GameObject Line;
    public Image illustration;
    public Text comboText;
    public Text scoreText;
    private float aspect = 16f / 9f;
    
    public AudioClip music;

    // Start is called before the first frame update
    void Awake()
    {
#if UNITY_EDITOR
        string platformPrefix = "file://";
#else 
        string platformPrefix = "file://";
#endif
        if (!GlobalSetting.autoPlay)
            gameObject.AddComponent<JudgementManager>();
        GlobalSetting.lineColors.Add(JudgeLineStat.AP, new Color(0xff / 256f, 0xec / 256f, 0xa0 / 256f, 1));
        GlobalSetting.lineColors.Add(JudgeLineStat.FC, new Color(0xb4 / 256f, 0xe1 / 256f, 0xff / 256f, 1));
        GlobalSetting.lineColors.Add(JudgeLineStat.None, new Color(1, 1, 1, 1));
        //Music
        WWW a = new WWW(platformPrefix + GlobalSetting.musicPath);
        //www.SendWebRequest();
        //var clip = DownloadHandlerAudioClip.GetContent(www);
        while (!a.isDone) { };
        music = a.GetAudioClip();

        a = new WWW(platformPrefix + GlobalSetting.illustrationPath);
        //Illustration
        while (!a.isDone) { };
        Sprite sprite = Sprite.Create(a.texture, new Rect(0, 0, a.texture.width, a.texture.height), new Vector2(0.5f, 0.5f));
        illustration.sprite = sprite;
        Application.targetFrameRate = 120;
        GlobalSetting.playing = false;
        /*Material temp = new Material(Shader.Find("Unlit/IllustrationShader"));
        temp.mainTexture = a.texture;
        temp.name = "tempMaterial";
        //temp.SetFloat("_BlurSize", 100f);
        illustration.material = temp;*/


        init(GlobalSetting.chartpath);
        gameObject.AddComponent<AudioSource>();
        gameObject.GetComponent<AudioSource>().playOnAwake = false;
        gameObject.GetComponent<AudioSource>().clip = music;
        GlobalSetting.formatVersion = json.formatVersion;
        GlobalSetting.scoreCounter.numOfNotes = json.numOfNotes;
        GlobalSetting.offset = json.offset;

        foreach (GameObject i in GameObject.FindGameObjectsWithTag("Lines"))
        {
            GlobalSetting.lines.Add(i.GetComponent<JudgeLineMovement>());
            i.GetComponent<Animation>().Play("StartGradient");
        }
        foreach (GameObject i in GameObject.FindGameObjectsWithTag("UI"))
        {
            i.GetComponent<Animation>().Play("StartGradientChartName");
            i.GetComponent<Text>().text = $"{GlobalSetting.chartName}\n\n";
        }
        Debug.Log(GlobalSetting.highLight);
        StartCoroutine(StartPlay());
        GameObject.Find("SongNameLeftBottom").GetComponent<Text>().text = "   " + GlobalSetting.chartName;
        GameObject.Find("DiffText").GetComponent<Text>().text = GlobalSetting.difficulty + "  ";
        //gameObject.GetComponent<AudioSource>().priority = 10;
    }

    private IEnumerator StartPlay()
    {
        yield return new WaitForSeconds(4);
        gameObject.GetComponent<AudioSource>().Play();
        GlobalSetting.playing = true;

    }

    void Update()
    {
        if (Camera.main.aspect >= aspect)
        {
            GlobalSetting.screenHeight = Screen.height;
            GlobalSetting.screenWidth = Screen.height * aspect;
            Debug.Log($"{GlobalSetting.screenHeight} * {GlobalSetting.screenWidth}");
        }
        else
        {
            GlobalSetting.screenHeight = Screen.height;
            GlobalSetting.screenWidth = Screen.width;
        }
        Camera.main.orthographicSize = 5 / (GlobalSetting.aspect) * (aspect);
        GlobalSetting.noteSpeedFactor = 1 / (GlobalSetting.aspect) * (aspect);
        m_frames++;
        if (Time.realtimeSinceStartup - m_lastupdateshowtime >= 1f)
        {
            m_fps = m_frames / (Time.realtimeSinceStartup - m_lastupdateshowtime);
            m_lastupdateshowtime = Time.realtimeSinceStartup;
            m_frames = 0;
        }

        if (music.length - gameObject.GetComponent<AudioSource>().time <= 0.1f)
        {
            GlobalSetting.playing = false;
        }
        GlobalSetting.musicProgress = Mathf.Max(gameObject.GetComponent<AudioSource>().time - json.offset - GlobalSetting.userOffset, 0);
        if (GlobalSetting.scoreCounter.combo >= 3)
            comboText.text = $"<size={(int)(Screen.height / 24)}>{GlobalSetting.scoreCounter.combo}</size>\n<size={(int)(Screen.height / 25 / 1.875f)}>{(GlobalSetting.autoPlay ? "AUTOPLAY" : "COMBO")}</size>";
        else
            comboText.text = "";
        scoreText.text = $"\n<size={(int)(Screen.height / 25)}>{Mathf.RoundToInt(GlobalSetting.scoreCounter.score).ToString().PadLeft(7, '0')}</size>  ";
        scoreText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10 - GlobalSetting.widthOffset, -10);
        GameObject.Find("SongNameLeftBottom").GetComponent<RectTransform>().anchoredPosition = new Vector2(10 + GlobalSetting.widthOffset / 2, 10);
        GameObject.Find("DiffText").GetComponent<RectTransform>().anchoredPosition = new Vector2(-10 - GlobalSetting.widthOffset / 2, 10);
    }


    void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 1000, 1000), 
            $"<size=30>{((int)gameObject.GetComponent<AudioSource>().time / 60).ToString().PadLeft(2, '0')}" +
            $":{((int)gameObject.GetComponent<AudioSource>().time % 60).ToString().PadLeft(2, '0')}/" +
            $"{((int)music.length / 60).ToString().PadLeft(2, '0')}" +
            $":{((int)music.length % 60).ToString().PadLeft(2, '0')}</size>");
        GUI.Label(new Rect(0, 40, 1000, 1000),
            $"FPS: {m_fps}");
        if (GlobalSetting.noteSpeedFactor != 1f)
        {
            GUI.Label(new Rect(0, 52, 1000, 1000),
            $"<color=lime>Note Speed: {GlobalSetting.noteSpeedFactor}x</color>");
        }
        GUI.Label(new Rect(0, 64, 1000, 1000),
            $"<color=lime>P:{GlobalSetting.scoreCounter.perfectCnt}\n" +
            $"G:{GlobalSetting.scoreCounter.goodCnt}\n" +
            $"B:{GlobalSetting.scoreCounter.badCnt}\n" +
            $"M:{GlobalSetting.scoreCounter.missCnt}\n</color>");
    }

    private void init(string path)
    {
        StreamReader sr = new StreamReader(path, Encoding.Default);
        chart = sr.ReadToEnd();
        sr.Close();
        json = JsonUtility.FromJson<Chart>(chart);
        int i = 0;
        foreach (judgeLine l in json.judgeLineList)
        {
            GameObject t = Instantiate(Line);
            t.GetComponent<JudgeLineMovement>().id = i;
            t.GetComponent<JudgeLineMovement>().line = l;
            //InternalEditorUtility.AddTag($"Note_Line{i - 1}");
            Debug.Log($"Line instatiated. ID: {i}");
            i++;
        }
    }
}
