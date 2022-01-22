using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
//using UnityEditorInternal;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
//using System.ComponentModel;

public enum NoteStat{
    Perfect,
    Good,
    Bad,
    Miss,
    None,
    Early,
    Late
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
    public int early;
    public int late;
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
            case NoteStat.Early:
                goodCnt++;
                early++;
                combo++;
                break;
            case NoteStat.Late:
                goodCnt++;
                late++;
                combo++;
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
    public float accuracy { get { return (perfectCnt + goodCnt * 0.65f) / numOfNotes; } }
}

public static class GlobalSetting
{
    public static bool playing { get; set; }
    public static string chartpath = "E:\\DESKTOP\\pumian\\Apollo\\cachedJson.json";
    public static string chartName = "Apollo";
    public static string musicPath = "E:\\DESKTOP\\pumian\\Apollo\\Apollos.wav";
    public static string illustrationPath = "E:\\DESKTOP\\pumian\\Apollo\\Apollo.png";
    public static int formatVersion = 3;
    public static Dictionary<float, int> highLightedNotes = new Dictionary<float, int>();
    public static float globalNoteScale = 0.25f;
    public static float musicProgress = 0f;
    public static bool highLight;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    public static bool autoPlay = true;
#else
    public static bool autoPlay = false;
#endif
    public static ScoreCounter scoreCounter = new ScoreCounter();
    public static float noteSpeedFactor = 1f;
    public static float offset;
    public static float userOffset;
    public static string difficulty = "Diff";
    public static JudgeLineStat lineStat = JudgeLineStat.AP;
    public static Dictionary<JudgeLineStat, Color> lineColors = new Dictionary<JudgeLineStat, Color>();
    public static Dictionary<int, AudioClip> tapSounds = new Dictionary<int, AudioClip>();
    public static float screenHeight;
    public static float screenWidth;
    public static float aspect { get { return screenWidth / screenHeight; } }
    public static float widthOffset { get { return (Screen.width - screenWidth) / 2f; } }
    public static string chart = "";
    public static CSVReader lineImage = null;
    public static bool usingApi = false;

    public static bool oldTexture = false;
    public static Sprite backgroundImage = null;

    public static List<JudgeLineMovement> lines = new List<JudgeLineMovement>();

    public static void playNoteSound(int notetype, Vector3 pos)
    {
        AudioSource.PlayClipAtPoint(tapSounds[notetype], Camera.main.transform.position, 0.6f);
    }

    public static void reset()
    {
        playing = false;
        highLightedNotes.Clear();
        musicProgress = 0f;
        scoreCounter = new ScoreCounter();
        noteSpeedFactor = 1f;
        lines.Clear();
        lineColors.Clear();
        tapSounds.Clear();
        lineStat = JudgeLineStat.AP;
        ObjectPool.GetInstance().reset();
        lineImage = null;
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
    public Image illustrationBlur;
    public Text comboText;
    public Text scoreText;
    private float aspect = 16f / 9f;
    
    public AudioClip music;
    private AsyncOperation operation;

    // Start is called before the first frame update
    private void Awake()
    {
        GlobalSetting.lineColors.Add(JudgeLineStat.AP, new Color(0xff / 256f, 0xec / 256f, 0xa0 / 256f, 1));
        GlobalSetting.lineColors.Add(JudgeLineStat.FC, new Color(0xb4 / 256f, 0xe1 / 256f, 0xff / 256f, 1));
        GlobalSetting.lineColors.Add(JudgeLineStat.None, new Color(1, 1, 1, 1));
    }

    void Start()
    {
        gameObject.AddComponent<AudioSource>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        string platformPrefix = "file://";
#else
        string platformPrefix = "file://";
#endif
        if (GlobalSetting.usingApi)
            platformPrefix = "";
        if (!GlobalSetting.autoPlay)
            gameObject.AddComponent<JudgementManager>();
        try
        {
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
            GlobalSetting.backgroundImage = sprite;
        }
        catch { }
        
        Application.targetFrameRate = 120;
        GlobalSetting.playing = false;
        /*Material temp = new Material(Shader.Find("Custom/BackBlur"));
        //temp.mainTexture = a.texture;
        temp.name = "tempMaterial";
        //temp.SetFloat("_BlurSize", 100f);
        illustrationBlur.material = temp;*/
        //GameObject.Find("BackGroundCanvas").transform.SetSiblingIndex(100);
        if (!GlobalSetting.usingApi)
        {
            if (!GlobalSetting.chartpath.Contains(".pec"))
                init(GlobalSetting.chartpath);
            else
                init(GlobalSetting.chartpath, true);
        }
        else
            initWeb(GlobalSetting.chartpath);
        
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
        StartCoroutine(StartPlay());
        GameObject.Find("SongNameLeftBottom").GetComponent<Text>().text = "   " + GlobalSetting.chartName;
        GameObject.Find("DiffText").GetComponent<Text>().text = GlobalSetting.difficulty + "  ";
        GameObject.Find("VersionText").GetComponent<Text>().text = $"RE:Phigros v{Application.version} by totorowldox\n";
        //gameObject.GetComponent<AudioSource>().priority = 10;
        //LunarConsolePluginInternal.LunarConsoleConfig.consoleEnabled = true;
        operation = null;
        if (GlobalSetting.lineImage != null)
        {
            loadLineImage();
        }
    }

    private IEnumerator StartPlay()
    {
        yield return new WaitForSeconds(4);
        gameObject.GetComponent<AudioSource>().Play();
        GlobalSetting.playing = true;

    }

    private IEnumerator AsyncLoading()
    {
        operation = SceneManager.LoadSceneAsync("LevelOver 1");
        operation.allowSceneActivation = false;
        yield return new WaitForSeconds(2);
        operation.allowSceneActivation = true;
        yield return operation;
    }

    void Update()
    {
        if (music.length - GlobalSetting.musicProgress <= 0.1f)
        {
            GlobalSetting.chart = chart;
            GlobalSetting.playing = false;
            if (operation == null)
            {
                GameObject.Find("CutInOut").GetComponent<Animation>().Play("CutOut");
                StartCoroutine(AsyncLoading());
            }
            return;
        }
        if (Camera.main.aspect >= aspect)
        {
            GlobalSetting.screenHeight = Screen.height;
            GlobalSetting.screenWidth = Screen.height * aspect;
        }
        else
        {
            GlobalSetting.screenHeight = Screen.height;
            GlobalSetting.screenWidth = Screen.width;
        }
        Camera.main.orthographicSize = 5 / (GlobalSetting.aspect) * (aspect);
        //GlobalSetting.noteSpeedFactor = 1 / (GlobalSetting.aspect) * (aspect);
        m_frames++;
        if (Time.realtimeSinceStartup - m_lastupdateshowtime >= 1f)
        {
            m_fps = m_frames / (Time.realtimeSinceStartup - m_lastupdateshowtime);
            m_lastupdateshowtime = Time.realtimeSinceStartup;
            m_frames = 0;
        }
        GlobalSetting.musicProgress = Mathf.Max(gameObject.GetComponent<AudioSource>().time - json.offset - GlobalSetting.userOffset, 0);
        if (GlobalSetting.scoreCounter.combo >= 3)
            comboText.text = $"{GlobalSetting.scoreCounter.combo}\n<size=24>{(GlobalSetting.autoPlay ? "Autoplay" : "COMBO")}</size>";
        else
            comboText.text = "";
        scoreText.text = $"{Mathf.RoundToInt(GlobalSetting.scoreCounter.score).ToString().PadLeft(7, '0')} ";
    }

    void OnGUI()
    {
        if (GlobalSetting.playing)
            GUI.Label(new Rect(0, 0, 1000, 1000), 
                $"<size=30>{((int)gameObject.GetComponent<AudioSource>().time / 60).ToString().PadLeft(2, '0')}" +
                $":{((int)gameObject.GetComponent<AudioSource>().time % 60).ToString().PadLeft(2, '0')}/" +
                $"{((int)music.length / 60).ToString().PadLeft(2, '0')}" +
                $":{((int)music.length % 60).ToString().PadLeft(2, '0')}</size>");
        /*GUI.Label(new Rect(0, 40, 1000, 1000),
            $"FPS: {m_fps}");*/
        if (GlobalSetting.noteSpeedFactor != 1f)
        {
            GUI.Label(new Rect(0, 52, 1000, 1000),
            $"<color=lime>Note Speed: {GlobalSetting.noteSpeedFactor}x</color>");
        }
        /*GUI.Label(new Rect(0, 64, 1000, 1000),
            $"<color=lime>P:{GlobalSetting.scoreCounter.perfectCnt}\n" +
            $"G:{GlobalSetting.scoreCounter.goodCnt}\n" +
            $"B:{GlobalSetting.scoreCounter.badCnt}\n" +
            $"M:{GlobalSetting.scoreCounter.missCnt}\n</color>");*/
    }

    private void init(string path)
    {
        chart = File.ReadAllText(path);
        json = JsonUtility.FromJson<Chart>(chart);
        preparationChart();
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

    private void init(string path, bool a)
    {
        chart = File.ReadAllText(path);
        json = Pec2Json.Convert(chart);//JsonUtility.FromJson<Chart>(chart);
        try
        {
            StreamWriter sw = new StreamWriter(Path.Combine(PlayerPrefs.GetString("chartFolderPath", ""), "cachedJson.json"));
            sw.Write(JsonUtility.ToJson(json));
            sw.Close();
        }
        catch
        {
            Utils.MakeToast("Error occurs while saving cached json file.");
        }
        preparationChart();
        int i = 0;
        foreach (judgeLine l in json.judgeLineList)
        {
            GameObject t = Instantiate(Line);
            t.GetComponent<JudgeLineMovement>().id = i;
            t.GetComponent<JudgeLineMovement>().line = l;
            //InternalEditorUtility.AddTag($"Note_Line{i - 1}");
            //Debug.Log($"Line instatiated. ID: {i}");
            i++;
        }
    }

    private void initWeb(string path)
    {
        WWW a = new WWW(path);
        //www.SendWebRequest();
        //var clip = DownloadHandlerAudioClip.GetContent(www);
        while (!a.isDone) { };
        chart = a.text;
        Debug.Log(a.text);
        json = JsonUtility.FromJson<Chart>(chart);
        preparationChart();
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

    private void preparationChart()
    {
        foreach(judgeLine line in json.judgeLineList)
        {
            float tempBpm = line.bpm;
            float factor = 1.875f / tempBpm;
            foreach (note n in line.notesAbove)
            {
                n.time = n.time * factor;
                n.holdTime = n.holdTime * factor;
            }
                
            foreach (note n in line.notesBelow)
            {
                n.time = n.time * factor;
                n.holdTime = n.holdTime * factor;
            }
            foreach (judgeLineSpeedEvent e in line.speedEvents)
            {
                e.startTime = e.startTime * factor;
                e.endTime = e.endTime * factor;
            }
            foreach (judgeLineEvent e in line.judgeLineDisappearEvents)
            {
                e.startTime = e.startTime * factor;
                e.endTime = e.endTime * factor;
            }
            foreach (judgeLineEvent e in line.judgeLineRotateEvents)
            {
                e.startTime = e.startTime * factor;
                e.endTime = e.endTime * factor;
            }
            foreach (judgeLineEvent e in line.judgeLineMoveEvents)
            {
                e.startTime = e.startTime * factor;
                e.endTime = e.endTime * factor;
            }
        }
    }

    private void loadLineImage()
    {
        for (int i = 0; i < GlobalSetting.lines.Count; i++)
        {
            try
            {
                int lineId = int.Parse(GlobalSetting.lineImage.GetDataByRowAndCol(i + 1, 1));
                float t1, t2;
                t1 = float.Parse(GlobalSetting.lineImage.GetDataByRowAndCol(i + 1, 3));
                t2 = t1 / float.Parse(GlobalSetting.lineImage.GetDataByRowAndCol(i + 1, 4));
                WWW a = new WWW("file://" + Path.Combine(PlayerPrefs.GetString("chartFolderPath", ""),
                    GlobalSetting.lineImage.GetDataByRowAndCol(i + 1, 2)));
                while (!a.isDone) { };
                Sprite sprite = Sprite.Create(a.texture, new Rect(0, 0, a.texture.width, a.texture.height), new Vector2(0.5f, 0.5f));
                GlobalSetting.lines[lineId].GetComponent<SpriteRenderer>().sprite = sprite;
                GlobalSetting.lines[lineId].targetScale = new Vector3(t1 / 2f, t2 / 2f, 1);
                GlobalSetting.lines[lineId].isImage = true;
            }
            catch
            {
                continue;
            }
        }
    }
}
