using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class JudgeTime
{
    public float pTime;
    public float gTime;
    public float bTime;
    public float judgeTime;
}

public class JudgeLineMovement : MonoBehaviour
{
    public int id;
    public judgeLine line = new judgeLine();
    public GameObject Tap;
    public GameObject Flick;
    public GameObject Drag;
    public GameObject Hold;
    public List<NoteMovement> notes = new List<NoteMovement>();
    public float pgrTime = 0;
    public float timeFactor = 0;
    private const bool DEBUG = false;
    public double virtualPosY = 0;
    private double virtualPosYVersion1 = 0;
    public Vector3 targetScale = new Vector3(10, 3, 1);
    public JudgeTime judgeTime = new JudgeTime();
    public List<float> positionX = new List<float>(20);
    private SpriteRenderer sr = new SpriteRenderer();
    public bool isImage = false;

    #region temp vars
    private Vector3 moveTarget = new Vector3();
    private Vector3 moveFrom = new Vector3();
    private float moveFromTime = 0;
    private float moveTargetTime = 0;

    private float rotateTarget = new int();
    private float rotateFrom = new int();
    private float rotateFromTime = 0;
    private float rotateTargetTime = 0;

    private float appearTarget = new int();
    private float appearFrom = new int();
    private float appearFromTime = 0;
    private float appearTargetTime = 0;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        timeFactor = 1.875f / line.bpm; //Proportional
        if (GlobalSetting.formatVersion == 1)
        {
            foreach (judgeLineSpeedEvent b in line.speedEvents)
            {
                if (b.startTime < 0)
                    b.startTime = 0;
                b.floorPosition = virtualPosYVersion1;
                virtualPosYVersion1 += (b.endTime - b.startTime) * b.value;
            }
        }
        judgeTime.bTime = 0.16f;
        judgeTime.gTime = 0.08f;
        judgeTime.judgeTime = 0.2f;
        transform.localScale = targetScale;
        foreach (note i in line.notesAbove)
        {
            int t;
            if (GlobalSetting.highLightedNotes.TryGetValue(i.time, out t))
                GlobalSetting.highLightedNotes[i.time]++;
            else
                GlobalSetting.highLightedNotes.Add(i.time, 1);
            InitNote(i.type, i, 1);
        }
        foreach (note i in line.notesBelow)
        {
            int t;
            if (GlobalSetting.highLightedNotes.TryGetValue(i.time, out t))
                GlobalSetting.highLightedNotes[i.time]++;
            else
                GlobalSetting.highLightedNotes.Add(i.time, 1);
            InitNote(i.type, i, -1);
        }
        for (int i = 0; i < 20; i++)
            positionX.Add(0f);
        notes.Sort((l, r) =>
        {
            if (l.Note.time < r.Note.time)
                return -1;
            return 1;
        });
        sr = gameObject.GetComponent<SpriteRenderer>();
    }

    private void InitNote(int type, note i, int fact)
    {
        GameObject t = new GameObject();
        Destroy(t);
        Vector3 temp = transform.position;
        temp.x += i.positionX;
        temp.z = 0;
        switch (type)
        {
            case 1:
                t = Instantiate(Tap, temp, Quaternion.identity);
                break;
            case 2:
                t = Instantiate(Drag, temp, Quaternion.identity);
                break;
            case 4:
                t = Instantiate(Flick, temp, Quaternion.identity);
                break;
            case 3:
                t = Instantiate(Hold, temp + new Vector3(0, 0, 10), Quaternion.identity);
                break;
        }
        t.GetComponent<NoteMovement>().Note = i;
        t.GetComponent<NoteMovement>().notetype = type;
        t.GetComponent<NoteMovement>().isAbove = fact;
        t.GetComponent<NoteMovement>().parentLineId = id;
        //t.tag = $"Note_Line{id}";
        t.transform.SetParent(transform);
        //t.SetActive(false);
        notes.Add(t.GetComponent<NoteMovement>());
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobalSetting.playing)
            sr.color = GlobalSetting.lineColors[GlobalSetting.lineStat];

        if (!GlobalSetting.playing)
        {
            if (isImage)
                sr.color = Color.clear;
            return;
        }
            

        //pgrTime = GlobalSetting.musicProgress;  //Time convert
        pgrTime += Time.deltaTime;
        

        UpdateMovement();
        UpdateRotation();
        UpdateAlpha();
        UpdateSpeed();
    }

    internal NoteMovement GetNearestNote(Finger finger)
    {
        float dx = positionX[finger.index];
        List<NoteMovement> tempNoteList = new List<NoteMovement>();
        for(int j = 0; j < notes.Count; j++)
        {
            NoteMovement i = notes[j];
            if (JudgementManager.NoteInJudgeArea(dx, i.transform.localPosition.x, i.isAbove))
            {
                if (i.status != NoteStat.None)
                    continue;
                if (i.Note.time - judgeTime.judgeTime > pgrTime)
                    break;
                if (i.notetype == 2 || i.notetype == 4) //flick和drag另外判定
                {
                    bool t = i.judge(pgrTime, finger);
                    /*if (i.notetype == 4 && t) //如果判定了flick就不判定tap
                        judgedFlick = true;*/
                }
                else
                    tempNoteList.Add(i);
            }
        }
        if (tempNoteList.Count == 0)
            return null;
        NoteMovement note = tempNoteList[0];
        for (int j = 0; j < tempNoteList.Count; j++)
        {
            if (note.Note.time > tempNoteList[j].Note.time)
                note = tempNoteList[j]; //选time最小的note判定
        }
        return note;
    }

    void FixedUpdate()
    {
        if (Math.Abs(pgrTime - GlobalSetting.musicProgress) >= .1f)
            pgrTime = GlobalSetting.musicProgress;
    }

    public virtual void UpdateMovement()
    {
        //if (!moving)
        //{
        foreach (judgeLineEvent b in line.judgeLineMoveEvents)
        {
            judgeLineEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
                if (GlobalSetting.formatVersion == 3)
                {
                    i.startTime = i.startTime < 0 ? 0 : i.startTime;
                    i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                    Vector3 a = new Vector3();
                    a.x = GlobalSetting.screenWidth * i.end + GlobalSetting.widthOffset;
                    a.y = GlobalSetting.screenHeight * i.end2;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveTarget = a;
                    a.x = GlobalSetting.screenWidth * i.start + GlobalSetting.widthOffset;
                    a.y = GlobalSetting.screenHeight * i.start2;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveFrom = a;
                }
                else if (GlobalSetting.formatVersion == 1)
                {
                    i.startTime = i.startTime < 0 ? 0 : i.startTime;
                    i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                    Vector3 a = new Vector3();
                    a.x = i.end / 1000 / 880 * GlobalSetting.screenWidth + GlobalSetting.widthOffset;
                    a.y = i.end % 1000 / 520 * GlobalSetting.screenHeight;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveTarget = a;
                    a.x = i.start / 1000 / 880 * GlobalSetting.screenWidth + GlobalSetting.widthOffset;
                    a.y = i.start % 1000 / 520 * GlobalSetting.screenHeight;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveFrom = a;
                }
                moveTargetTime = i.endTime;
                moveFromTime = i.startTime;
                //line.judgeLineMoveEvents.Remove(b);
                //break;
        }
        transform.position = Vector2.Lerp(moveFrom, moveTarget, (pgrTime - moveFromTime) / (moveTargetTime - moveFromTime));
    }

    public virtual void UpdateRotation()
    {
        //if (!rotating)
        //{
        foreach (judgeLineEvent b in line.judgeLineRotateEvents)
        {
            judgeLineEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
            //MoveEvent(i.endTime - i.startTime, i);
            i.startTime = i.startTime < 0 ? 0 : i.startTime;
            i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
            rotateTarget = i.end;
            rotateFrom = i.start;
            //moveTargetTime = (i.endTime - i.startTime) * timeFactor * Time.deltaTime;
            rotateTargetTime = i.endTime;
            rotateFromTime = i.startTime;
            //line.judgeLineRotateEvents.Remove(b);
            //break;
        }
            
        //}
        //else
        //{
            //if (pgrTime >= rotateTargetTime)
            //{
            //    rotating = false;
            //    transform.rotation = Quaternion.Euler(0, 0, rotateTarget);
            //}
            //else
                transform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rotateFrom, rotateTarget, (pgrTime - rotateFromTime) / (rotateTargetTime - rotateFromTime)));
        //}
    }

    public virtual void UpdateAlpha()
    {
        //if (!appearing)
        //{
        foreach (judgeLineEvent b in line.judgeLineDisappearEvents)
        {
            judgeLineEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
            {
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                appearTarget = i.end;
                appearFrom = i.start;
                appearTargetTime = i.endTime;
                appearFromTime = i.startTime;
                //line.judgeLineDisappearEvents.Remove(b);
                //break;
                //gameObject.GetComponent<SpriteRenderer>().color = new Color(0.96f, 0.96f, 0.66f, appearFrom);
            }
        }
            
        //}
        //else
        //{
            //if (pgrTime >= appearTargetTime)
            //{
            //    appearing = false;
            //    gameObject.GetComponent<SpriteRenderer>().color = new Color(0.96f, 0.96f, 0.66f, appearTarget);
            //}
            //else
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(appearFrom, appearTarget, (pgrTime - appearFromTime) / (appearTargetTime - appearFromTime)));
        //}
    }

    public virtual void UpdateSpeed()
    {
        foreach (judgeLineSpeedEvent b in line.speedEvents)
        {
            judgeLineSpeedEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
            virtualPosY = (pgrTime - i.startTime) * i.value + i.floorPosition;
        }
    }

    public void ResetScale()
    {
        Debug.Log("aaa");
        StartCoroutine(ResetScaleCoroutine());
    }

    private IEnumerator ResetScaleCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        transform.localScale = targetScale;
    }

}
