using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.IO;

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

    // Start is called before the first frame update
    void Start()
    {
        GlobalSetting.reset();
        tempPath = PlayerPrefs.GetString("chartFolderPath", "");
        pathSelector.text = tempPath;
        //PlayerPrefs.SetString("chartFolderPath", tempPath);
        GameObject.Find("DiffInput").GetComponent<InputField>().text = PlayerPrefs.GetString("difficultyName", "SP Lv.?");
        chartNameUI.GetComponent<InputField>().text = PlayerPrefs.GetString("chartName", "Untitled");
    }

    public void OnClick()
    {
        GlobalSetting.chartName = chartNameUI.GetComponent<InputField>().text.Trim();
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        GlobalSetting.chartpath = tempPath + "\\" + chartPathDropdown.captionText.text;
        GlobalSetting.musicPath = tempPath + "\\" + musicPathDropdown.captionText.text;
        GlobalSetting.illustrationPath = tempPath + "\\" + illustrationPathDropdown.captionText.text;
#else
        GlobalSetting.chartpath = chartPathDropdown.captionText.text;
        GlobalSetting.musicPath = musicPathDropdown.captionText.text;
        GlobalSetting.illustrationPath = illustrationPathDropdown.captionText.text;
#endif
        GlobalSetting.highLight = highlightToggle.isOn;
        GlobalSetting.difficulty = GameObject.Find("DiffInput").GetComponent<InputField>().text;
        GlobalSetting.userOffset = int.Parse(GameObject.Find("DelayInput").GetComponent<InputField>().text) / 1000f;
        PlayerPrefs.SetString("chartFolderPath", tempPath);
        PlayerPrefs.SetString("difficultyName", GlobalSetting.difficulty);
        PlayerPrefs.SetString("chartName", GlobalSetting.chartName);
        //PlayerPrefs.SetInt("userOffset", GlobalSetting.userOffset);
        SceneManager.LoadSceneAsync("LoadingScene");
    }

    public void OnClickPath()
    {
        tempPath = pathSelector.text;
        chartPathDropdown.options = getFileName(tempPath, "");
        musicPathDropdown.options = getFileName(tempPath, ".wav", ".ogg", ".mp3");
        illustrationPathDropdown.options = getFileName(tempPath, ".png", ".bmp", ".jpg");
    }

    public static List<Dropdown.OptionData> getFileName(string path, params string[] typeE)
    {
        List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (string type in typeE)
        {
            foreach (FileInfo f in root.GetFiles())
                if (f.FullName.Trim().EndsWith(type))
                {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    list.Add(new Dropdown.OptionData(f.Name.Trim()));
#else
                    list.Add(new Dropdown.OptionData(f.FullName.Trim()));
#endif
                }
        }
        return list;
    }

    public void SpeedChange()
    {
        speed = float.Parse(GameObject.Find("SpeedDropdown").GetComponent<Dropdown>().captionText.text.Trim('x'));
        GlobalSetting.noteSpeedFactor = speed;
    }
}
