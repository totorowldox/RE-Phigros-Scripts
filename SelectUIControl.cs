﻿using System.Collections;
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
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        GlobalSetting.chartName = chartNameUI.GetComponent<InputField>().text.Trim();
#if UNITY_EDITOR
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
        SceneManager.LoadSceneAsync("LoadingScene");
    }

    public void OnClickPath()
    {
        tempPath = pathSelector.text;
        chartPathDropdown.options = getFileName(tempPath, "");
        musicPathDropdown.options = getFileName(tempPath, ".wav");
        illustrationPathDropdown.options = getFileName(tempPath, ".png");
    }

    public static List<Dropdown.OptionData> getFileName(string path, string type)
    {
#if UNITY_EDITOR
        List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (FileInfo f in root.GetFiles())
            if (f.FullName.EndsWith(type))
                list.Add(new Dropdown.OptionData(f.Name));
        return list;
#else
        List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
        DirectoryInfo root = new DirectoryInfo(path);
        foreach (FileInfo f in root.GetFiles())
            if (f.FullName.EndsWith(type))
                list.Add(new Dropdown.OptionData(f.FullName));
        return list;
#endif

    }

    public void SpeedChange()
    {
        speed = float.Parse(GameObject.Find("SpeedDropdown").GetComponent<Dropdown>().captionText.text.Trim('x'));
        GlobalSetting.noteSpeedFactor = speed;
    }
}
