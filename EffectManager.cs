using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public bool isHold;
    private GameObject sr3;
    // Start is called before the first frame update 
    void Start()
    {
        transform.localScale = new Vector3(GlobalSetting.globalNoteScale / 0.15f, GlobalSetting.globalNoteScale / 0.15f, GlobalSetting.globalNoteScale / 0.15f);
        //GetComponent<Animator>().Play("");
        sr3 = transform.GetChild(3).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<SpriteRenderer>().color == GlobalSetting.lineColors[JudgeLineStat.FC])
        {
            if (transform.childCount == 4)
            {
                //Destroy(transform.GetChild(3).gameObject);
                //sr3.color = new Color(0, 0, 0, 0);
                sr3.SetActive(false);
            }
        }
    }

    public void DestroyThis()
    {
        //StartCoroutine(RecycleObj());
        ObjectPool.GetInstance().RecycleObj(gameObject);
    }

    public IEnumerator RecycleObj()
    {
        yield return new WaitForSeconds(0.2f);
        ObjectPool.GetInstance().RecycleObj(gameObject);
    }
}
