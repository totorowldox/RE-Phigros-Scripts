using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PhigrosResultManager : MonoBehaviour
{
    public GameObject[] ranks = new GameObject[10];
    private Vector2 targetResolution = new Vector2(1920, 1080);
    // Start is called before the first frame update
    void Start()
    {
        GameObject.Find("SongsName").GetComponent<Text>().text = GlobalSetting.chartName;
        GameObject.Find("Perfect").GetComponent<Text>().text = GlobalSetting.scoreCounter.perfectCnt.ToString();
        GameObject.Find("Good").GetComponent<Text>().text = GlobalSetting.scoreCounter.goodCnt.ToString();
        GameObject.Find("Bad").GetComponent<Text>().text = GlobalSetting.scoreCounter.badCnt.ToString();
        GameObject.Find("Miss").GetComponent<Text>().text = GlobalSetting.scoreCounter.missCnt.ToString();
        GameObject.Find("Accuracy").GetComponent<Text>().text = (GlobalSetting.scoreCounter.accuracy * 100f).ToString("0.00") + "%";
        GameObject.Find("ScoreText").GetComponent<Text>().text = Mathf.RoundToInt(GlobalSetting.scoreCounter.score).ToString().PadLeft(7, '0');
        GameObject.Find("History").GetComponent<Text>().text = "NEW BEST   0000000  +" + Mathf.RoundToInt(GlobalSetting.scoreCounter.score).ToString().PadLeft(7, '0');
        GameObject.Find("MaxCombo").GetComponent<Text>().text = GlobalSetting.scoreCounter.maxcombo.ToString();
        GameObject.Find("Difficulty").GetComponent<Text>().text = GlobalSetting.difficulty;
        GameObject.Find("CoverImage").GetComponent<Image>().sprite = GlobalSetting.backgroundImage;
        GameObject.Find("Translucent Image").GetComponent<Image>().sprite = GlobalSetting.backgroundImage;
        GameObject.Find("Early").GetComponent<Text>().text = GlobalSetting.scoreCounter.early.ToString();
        GameObject.Find("Late").GetComponent<Text>().text = GlobalSetting.scoreCounter.late.ToString();
        getRank(GlobalSetting.scoreCounter.score);
    }
    private void getRank(float scoreNum)
    {
        int a = Mathf.RoundToInt(scoreNum);
        if (a >= 1e6) ranks[0].SetActive(true);
        else if (a >= 9.6e5) ranks[1].SetActive(true);
        else if (a >= 9.2e5) ranks[2].SetActive(true);
        else if (a >= 8.8e5) ranks[3].SetActive(true);
        else if (a >= 8.2e5) ranks[4].SetActive(true);
        else if (a >= 7e5) ranks[5].SetActive(true);
        else ranks[6].SetActive(true);
    }
    public void NextButtonClicked()
    {
        SceneManager.LoadSceneAsync("ChartSelectorScene");
    }
    public void RetryButtonClicked()
    {
        SceneManager.LoadSceneAsync("LoadingScene");
    }
}
