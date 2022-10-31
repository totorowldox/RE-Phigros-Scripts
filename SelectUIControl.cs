using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using Keiwando.NFSO;
using System.Linq;

public class SelectUIControl : MonoBehaviour
{
    public GameObject chartNameUI;
    public Dropdown chartPathDropdown;
    public Dropdown musicPathDropdown;
    public Dropdown illustrationPathDropdown;
    public InputField pathSelector;
    public Toggle highlightToggle;
    private string tempPath;
    private float speed;
    //private string internalPath = Application.persistentDataPath.Substring(0, Application.persistentDataPath.IndexOf("/Android"));

    // Start is called before the first frame update
    void Start()
    {
        GlobalSetting.Reset();
        tempPath = PlayerPrefs.GetString("chartFolderPath", "");
        pathSelector.text = tempPath;
        //PlayerPrefs.SetString("chartFolderPath", tempPath);
        GameObject.Find("DiffInput").GetComponent<InputField>().text = PlayerPrefs.GetString("difficultyName", "SP Lv.?");
        chartNameUI.GetComponent<InputField>().text = PlayerPrefs.GetString("chartName", "Untitled");
        GameObject.Find("SelectChart").GetComponent<Button>().interactable = false;
        GameObject.Find("InfoDropdown").GetComponent<Dropdown>().options = getFolders(Application.persistentDataPath);
        OnChangeDropdown();
    }

    public void OnClick()
    {
        GlobalSetting.chartName = chartNameUI.GetComponent<InputField>().text.Trim();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        GlobalSetting.chartpath = tempPath + "\\" + chartPathDropdown.captionText.text;
        GlobalSetting.musicPath = tempPath + "\\" + musicPathDropdown.captionText.text;
        GlobalSetting.illustrationPath = tempPath + "\\" + illustrationPathDropdown.captionText.text;
#else
        //tempPath = Path.Combine(internalPath, tempPath.Substring(tempPath.IndexOf("/0") + 2, tempPath.Length));
        GlobalSetting.chartpath = Path.Combine(tempPath, chartPathDropdown.captionText.text);
        GlobalSetting.musicPath = Path.Combine(tempPath, musicPathDropdown.captionText.text);
        GlobalSetting.illustrationPath = Path.Combine(tempPath, illustrationPathDropdown.captionText.text);
#endif
        GlobalSetting.highLight = highlightToggle.isOn;
        GlobalSetting.difficulty = GameObject.Find("DiffInput").GetComponent<InputField>().text;
        GlobalSetting.userOffset = int.Parse(GameObject.Find("DelayInput").GetComponent<InputField>().text) / 1000f;
        PlayerPrefs.SetString("chartFolderPath", tempPath);
        PlayerPrefs.SetString("difficultyName", GlobalSetting.difficulty);
        PlayerPrefs.SetString("chartName", GlobalSetting.chartName);
        //PlayerPrefs.SetInt("userOffset", GlobalSetting.userOffset);
        PlayerPrefs.Save();
        GlobalSetting.autoPlay = GameObject.Find("AutoToggle").GetComponent<Toggle>().isOn;
        GlobalSetting.isMirror = GameObject.Find("MirrorToggle").GetComponent<Toggle>().isOn;
        GlobalSetting.is3D = GameObject.Find("3DToggle").GetComponent<Toggle>().isOn;
        GlobalSetting.postProcessing = GameObject.Find("PostProcessingToggle").GetComponent<Toggle>().isOn;
        GlobalSetting.usingApi = false;
        SceneManager.LoadSceneAsync("LoadingScene");
    }

    public void OnClickPath()
    {
        tempPath = pathSelector.text;
        chartPathDropdown.options = getFileName(tempPath, ".json", ".pec");
        musicPathDropdown.options = getFileName(tempPath, ".wav", ".ogg", ".mp3");
        illustrationPathDropdown.options = getFileName(tempPath, ".png", ".bmp", ".jpg");
        try
        {
            string t = getFileName(tempPath, "line.csv").FirstOrDefault()?.text;
            if (t != null)
                GlobalSetting.lineImage = new CSVReader(Path.Combine(tempPath, t));
        }
        catch
        {
            GlobalSetting.lineImage = null;
        }
    }

    public static List<Dropdown.OptionData> getFileName(string path, params string[] typeE)
    {
        List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (string type in typeE)
        {
            foreach (FileInfo f in root.GetFiles())
                if (f.FullName.ToLower().Trim().EndsWith(type))
                {
                    list.Add(new Dropdown.OptionData(f.Name.Trim()));
                }
        }
        return list;
    }

    public static List<Dropdown.OptionData> getFolders(string path)
    {
        List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (DirectoryInfo f in root.GetDirectories())
                list.Add(new Dropdown.OptionData(f.Name.Trim()));
        return list;
    }

    public void SpeedChange()
    {
        speed = float.Parse(GameObject.Find("SpeedDropdown").GetComponent<Dropdown>().captionText.text.Trim('x'));
        GlobalSetting.noteSpeedFactor = speed;
    }

    public void SelectedChart()
    {
        NativeFileSO.shared.OpenFile(SupportedFilePreferences.supportedFileTypes, Callback);
    }

    private void Callback(bool wasFileOpened, OpenedFile file)
    {
        if (wasFileOpened)
        {
        }
    }

    public void OnChangeDropdown()
    {
        string t = GameObject.Find("InfoDropdown").GetComponent<Dropdown>().captionText.text;

        chartNameUI.GetComponent<InputField>().text = t.Split('.')[0];
        try
        {
            GameObject.Find("DiffInput").GetComponent<InputField>().text = $"{t.Split('.')[1]} Lv." + t.Split('.')[2];
        }
        catch
        {
            GameObject.Find("DiffInput").GetComponent<InputField>().text = "SP Lv.?";
        }
        pathSelector.text = Path.Combine(Application.persistentDataPath, GameObject.Find("InfoDropdown").GetComponent<Dropdown>().captionText.text);
        OnClickPath();
    }
}
