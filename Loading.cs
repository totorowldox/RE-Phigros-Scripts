using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    private double time;
    private AsyncOperation operation;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(AsyncLoading());
        if (GlobalSetting.chartpath.Contains(".pec"))
            GameObject.Find("Text").GetComponent<Text>().text = "Converting PEC to JSON\nBy lchzh3473...";
    }

    private IEnumerator AsyncLoading()
    {
        operation = SceneManager.LoadSceneAsync("PlayingScene");
        yield return operation;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time >= 1)
        {
            time = 0;
            GetComponent<UnityEngine.UI.Text>().text += '.';
        }
    }
}
