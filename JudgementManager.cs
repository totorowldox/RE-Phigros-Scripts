using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Finger
{
    public TouchPhase phase;
    public Vector2[] lastPositions = new Vector2[50];
    public Vector2 newPosition;
    Vector3 screenPosition;
    Vector3 worldPosition;
    public bool isNewFlick;
    private TouchPhase lastPhase = TouchPhase.Canceled;
    public bool IsFirstClick => lastPhase == TouchPhase.Began || phase == TouchPhase.Began;
    int oldPosCounter;
    public void ClearOldPoss()
    {
        oldPosCounter = 0;
    }
    public void CheckInput()
    {
        screenPosition.x = newPosition.x;
        screenPosition.y = newPosition.y;
        screenPosition.z = 8f;
        worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        newPosition.x = worldPosition.x;
        newPosition.y = worldPosition.y;

        //CheckFlick
        isNewFlick = false;
        for (int i = 0; i < oldPosCounter; i++)
        {
            if (Vector2.Distance(lastPositions[i], newPosition) > 0.0075f)
            {
                isNewFlick = true;
                ClearOldPoss();
                break;
            }
        }

        //RecordTouchPosition
        if (phase == TouchPhase.Moved)
        {
            lastPositions[oldPosCounter] = newPosition;
            oldPosCounter++;
            if (oldPosCounter > lastPositions.Length - 1) ClearOldPoss();
        }

    }

    public void UpdatePhase(TouchPhase touchPhase)
    {
        lastPhase = phase;
        phase = touchPhase;
    }

    public void ClearTapFlag()
    {
        lastPhase = TouchPhase.Canceled;
        phase = TouchPhase.Moved;
    }
}



public class JudgementManager : MonoBehaviour
{
    public Finger[] fingers = new Finger[20];
    public int numOfFingers;
    public static JudgementManager m_instance;

    private List<NoteMovement> notesInJudge = new List<NoteMovement>();

    // Start is called before the first frame update
    void Start()
    {
        if (m_instance == null)
            m_instance = this;
        else
            Destroy(this);
        fingers.Initialize();
        for (int i = 0; i < 20; i++)
            fingers[i] = new Finger();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        UpdateMouseInput();//PC
#else
        UpdateTouchInput();//Mobile
#endif
        
        UpdateJudge();
    }

    public void UpdateTouchInput()
    {
        numOfFingers = Input.touchCount;
        for (int i = 0; i < numOfFingers; i++)
        {
            Touch touch = Input.GetTouch(i);
            fingers[i].UpdatePhase(touch.phase);
            fingers[i].newPosition = touch.position;
            fingers[i].CheckInput();
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
    public void UpdateMouseInput()
    {
        

        if (Input.GetMouseButtonDown(0)) fingers[0].phase = TouchPhase.Began;
        else if (Input.GetMouseButton(0)) fingers[0].phase = TouchPhase.Moved;
        else if (Input.GetMouseButtonUp(0)) fingers[0].phase = TouchPhase.Canceled;
        fingers[0].newPosition = Input.mousePosition;
        fingers[0].CheckInput();
        if (Input.GetMouseButton(0))
            numOfFingers = 1;
        else
            numOfFingers = 0;
    }
#endif

    public void UpdateJudge()
    {
        if (numOfFingers == 0)
            return;
        foreach (var i in GlobalSetting.lines)
        {
            for (int k = 0; k < numOfFingers; k++)
            {
                i.positionX[k] = GetLocalPosition(fingers[k].newPosition, i.transform).x;
            }
        }

        float pTime = GlobalSetting.lines[0].pgrTime;
        bool judgedFlick;
        float judgedFlickTime;
        
        for (int i = 0; i < numOfFingers; i++)
        {
            judgedFlick = false;
            judgedFlickTime = 999999f;
            notesInJudge.Clear();
            for (int k = 0; k < GlobalSetting.lines.Count; k++)
            {
                var ret = GlobalSetting.lines[k].GetNearestNote(fingers[i], GlobalSetting.lines[k].positionX[i]);
                var n = ret.note;
                if (n != null)
                {
                    notesInJudge.Add(n);
                }

                if (ret.flickTime <= 99999f)
                {
                    judgedFlick = true;
                    judgedFlickTime = Math.Min(ret.flickTime, judgedFlickTime);
                }
            }

            if (notesInJudge.Count == 0)
                continue;

            NoteMovement note = notesInJudge[0];
            foreach (var t in notesInJudge)
            {
                if (note.Note.time > t.Note.time)
                    note = t;
            }

            if (judgedFlick && note.Note.time > judgedFlickTime) //如果判定了flick且flick在tap前面
            {
                continue;
            }
            note.Judge(pTime, fingers[i]);
        }
    }

    private static Vector3 GetLocalPosition(Vector3 worldPosition, Transform parent) =>
        parent.InverseTransformPoint(worldPosition);

    public static bool NoteInJudgeArea(float fingerX, float noteX)
    {
        return (fingerX > noteX - 0.76f && fingerX < noteX + 0.76f);
    }
}
