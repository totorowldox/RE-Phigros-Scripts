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
    private float speedFactor = 1f;

    public bool alphaExtensionEnabled = false;

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
        Init();
    }

    public void Init()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
        if(!isImage)
            targetScale = new Vector2(200 * 2.5f * Camera.main.orthographicSize * Camera.main.aspect / sr.sprite.texture.width, 
                200 * 0.008f * Camera.main.orthographicSize / sr.sprite.texture.height);
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
        notes.Sort((l, r) => l.Note.time.CompareTo(r.Note.time));
        
        foreach (var i in line.judgeLineRotateEvents)
        {
            if (GlobalSetting.isMirror)
            {
                i.start = 360 - i.start;
                i.end = 360 - i.end;
            }
        }
        foreach (var i in line.judgeLineMoveEvents)
        {
            if (GlobalSetting.formatVersion is 3 or 114514)
            {
                if (GlobalSetting.isMirror)
                {
                    i.start = 1 - i.start;
                    i.end = 1 - i.end;
                }
            }
            else if (GlobalSetting.formatVersion == 1)
            {
                if (GlobalSetting.isMirror)
                {
                    float origin = i.start / 1000;
                    i.start = i.start - origin + 800 - origin;
                    origin = i.end / 1000;
                    i.end = i.end - origin + 800 - origin;
                }
            }
        }
    }

    private void InitNote(int type, note i, int fact)
    {
        GameObject t = new GameObject();
        Destroy(t);
        if (GlobalSetting.isMirror)
            i.positionX = -i.positionX;
        Vector3 temp = transform.position;
        temp.x += i.positionX * 16f / 9f / GlobalSetting.aspect;// * .985f;
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
        if (!i.isFake)
        {
            notes.Add(t.GetComponent<NoteMovement>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobalSetting.playing)
            sr.color = !isImage?GlobalSetting.lineColors[GlobalSetting.lineStat]:Color.white;

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

    internal (NoteMovement note, float flickTime) GetNearestNote(Finger finger, float dx)
    {
        var flickTime = 999999f;
        var tempNoteList = new List<NoteMovement>();
        var tolerance = judgeTime.judgeTime + pgrTime;
        for(var j = 0; j < notes.Count; j++)
        {
            var i = notes[j];
            if (i.Note.isFake)
                continue;
            if (!JudgementManager.NoteInJudgeArea(dx, i.cachedTransform.localPosition.x))
                continue;
            if (i.Note.time > tolerance)
                break;
            if (i.notetype is 2 or 4) //flick和drag另外判定
            {
                var suc = i.Judge(pgrTime, finger);
                if (i.notetype == 4) //如果判定了flick就不判定tap
                    flickTime = Math.Min(i.Note.time, flickTime);
            }
            else
                tempNoteList.Add(i);
        }
        if (tempNoteList.Count == 0)
            return (null, flickTime);
        var note = tempNoteList[0];
        for (var j = 0; j < tempNoteList.Count; j++)
        {
            if (note.Note.time > tempNoteList[j].Note.time)
                note = tempNoteList[j]; //选time最小的note判定
        }
        return (note, flickTime);
    }

    void FixedUpdate()
    {
        if (Math.Abs(pgrTime - GlobalSetting.musicProgress) >= .1f)
            pgrTime = GlobalSetting.musicProgress;
    }

    protected virtual void UpdateMovement()
    {
        //if (!moving)
        //{
        int easeType = 0;

        if (line.judgeLineMoveEvents.Count == 0)
            return;
        int j = 0;
        //foreach (judgeLineEvent b in line.judgeLineMoveEvents)
        for (j = 0; j < line.judgeLineMoveEvents.Count; j++)
        {
            if (pgrTime < line.judgeLineMoveEvents[j].startTime) break;
        }

        j = Mathf.Max(0, j - 1);
        
        judgeLineEvent i = line.judgeLineMoveEvents[j];
        
        if (GlobalSetting.formatVersion is 3 or 114514)
        {
            i.startTime = i.startTime < 0 ? 0 : i.startTime;
            i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
            Vector3 a = new Vector3();
            a.x = GlobalSetting.screenWidth * i.end + GlobalSetting.widthOffset;
            a.y = GlobalSetting.screenHeight * i.end2;
            a.z = 10;
            a = Camera.main.ScreenToWorldPoint(a);
            a.z = 0;
            moveTarget = a;
            a.x = GlobalSetting.screenWidth * i.start + GlobalSetting.widthOffset;
            a.y = GlobalSetting.screenHeight * i.start2;
            a.z = 10;
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
            a.z = 10;
            a = Camera.main.ScreenToWorldPoint(a);
            a.z = 0;
            moveTarget = a;
            a.x = i.start / 1000 / 880 * GlobalSetting.screenWidth + GlobalSetting.widthOffset;
            a.y = i.start % 1000 / 520 * GlobalSetting.screenHeight;
            a.z = 10;
            a = Camera.main.ScreenToWorldPoint(a);
            a.z = 0;
            moveFrom = a;
        }
        moveTargetTime = i.endTime;
        moveFromTime = i.startTime;
        easeType = i.easeType;

        var x = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
            pgrTime - moveFromTime, moveTargetTime - moveFromTime, 
            moveFrom.x, moveTarget.x);
        var y = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
            pgrTime - moveFromTime, moveTargetTime - moveFromTime, 
            moveFrom.y, moveTarget.y);
        transform.position = new Vector2(x, y);
    }

    protected virtual void UpdateRotation()
    {
        //if (!rotating)

        int easeType = 0;
        
        if (line.judgeLineRotateEvents.Count == 0)
            return;
        int j = 0;
        //foreach (judgeLineEvent b in line.judgeLineMoveEvents)
        for (j = 0; j < line.judgeLineRotateEvents.Count; j++)
        {
            if (pgrTime < line.judgeLineRotateEvents[j].startTime) break;
        }
        j = Mathf.Max(0, j - 1);
        judgeLineEvent i = line.judgeLineRotateEvents[j];
        i.startTime = i.startTime < 0 ? 0 : i.startTime;
        i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
        rotateTarget = i.end;
        rotateFrom = i.start;
        rotateTargetTime = i.endTime;
        rotateFromTime = i.startTime;
        easeType = i.easeType;
            
        var angles = transform.localEulerAngles;
        var z = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
            pgrTime - rotateFromTime, rotateTargetTime - rotateFromTime, 
            rotateFrom, rotateTarget);
        transform.localEulerAngles = new Vector3(angles.x, angles.y, z);
    }

    protected virtual void UpdateAlpha()
    {
        int easeType = 0;
        
        if (line.judgeLineDisappearEvents.Count == 0)
            return;
        int j = 0;
        for (j = 0; j < line.judgeLineDisappearEvents.Count; j++)
        {
            if (pgrTime < line.judgeLineDisappearEvents[j].startTime) break;
        }

        j = Mathf.Max(0, j - 1);
        judgeLineEvent i = line.judgeLineDisappearEvents[j];
        i.startTime = i.startTime < 0 ? 0 : i.startTime;
        i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
        appearTarget = i.end;
        appearFrom = i.start;
        appearTargetTime = i.endTime;
        appearFromTime = i.startTime;
        easeType = i.easeType;
            
        var color = sr.color;
        var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
            pgrTime - appearFromTime, appearTargetTime - appearFromTime, 
            appearFrom, appearTarget);
        alphaExtensionEnabled = a < 0;
        sr.color = new Color(color.r, color.g, color.b, a);
    }

    protected virtual void UpdateSpeed()
    {
        foreach (judgeLineSpeedEvent b in line.speedEvents)
        {
            judgeLineSpeedEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
            virtualPosY = (pgrTime - i.startTime) * i.value * speedFactor + i.floorPosition;
        }
    }

    public void ResetScale()
    {
        StartCoroutine(ResetScaleCoroutine());
    }

    private IEnumerator ResetScaleCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        transform.localScale = targetScale;
        if (GlobalSetting.is3D)
        {
            transform.eulerAngles = new Vector3(30, 0, 0);
        }
    }

    public float CalculateNoteHeight(float time)
    {
        return CalculateNoteHeight_internal(time) - CalculateNoteHeight_internal(pgrTime);
    }

    public float CalculateHeightDistance(float first, float second)
    {
        return CalculateNoteHeight_internal(first) - CalculateNoteHeight_internal(second);
    }

    private float CalculateNoteHeight_internal(float time)
    {
        var ret = 0f;
        foreach (var k in line.speedEvents) {
            //if (time > k.endTime) continue;
            if (time < k.startTime) break;
            if (time > k.endTime)
            {
                ret += k.value * (k.endTime - k.startTime);
            }
            else
            {
                ret += k.value * (time - k.startTime);
            }
        }
        return ret;
    }

}
