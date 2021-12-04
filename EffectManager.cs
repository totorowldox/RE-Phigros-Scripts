using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public bool isHold;
    // Start is called before the first frame update 
    void Start()
    {
        transform.localScale = new Vector3(GlobalSetting.globalNoteScale / 0.2f, GlobalSetting.globalNoteScale / 0.2f, GlobalSetting.globalNoteScale / 0.2f);
        //GetComponent<Animator>().Play("");
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<SpriteRenderer>().color == GlobalSetting.lineColors[JudgeLineStat.FC])
        {
            if (transform.childCount == 4)
            {
                Destroy(transform.GetChild(3).gameObject);
            }
        }
    }

    public void DestroyThis()
    {
        Destroy(gameObject);
    }
}
