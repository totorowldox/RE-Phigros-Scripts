using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finger
{
    public int index;
    public TouchPhase phase;
    public Vector2[] lastPositions = new Vector2[15];
    public Vector2 newPosition;
    Vector3 screenPosition;
    Vector3 worldPosition;
    public bool isNewFlick;
    public bool isFirstClick
    {
        get
        {
            return phase == TouchPhase.Began;
        }
    }
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
}



public class JudgementManager : MonoBehaviour
{
    public Finger[] fingers = new Finger[10];
    private int numOfFingers;
    public static JudgementManager m_instance;

    NoteMovement[] notesInJudge = new NoteMovement[10];

    // Start is called before the first frame update
    void Start()
    {
        if (m_instance == null)
            m_instance = this;
        else
            Destroy(this);
        fingers.Initialize();
        for (int i = 0; i < 10; i++)
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
            fingers[i].phase = touch.phase;
            fingers[i].newPosition = touch.position;
            fingers[i].index = i;
            fingers[i].CheckInput();
        }
    }

#if UNITY_EDITOR
    public void UpdateMouseInput()
    {
        numOfFingers = 1;

        if (Input.GetMouseButtonDown(0)) fingers[0].phase = TouchPhase.Began;
        else if (Input.GetMouseButton(0)) fingers[0].phase = TouchPhase.Moved;
        else if (Input.GetMouseButtonUp(0)) fingers[0].phase = TouchPhase.Canceled;
        fingers[0].newPosition = Input.mousePosition;
        fingers[0].CheckInput();
    }
#endif

    public void UpdateJudge()
    {
        if (numOfFingers == 0)
            return;
        foreach (GameObject i in GameObject.FindGameObjectsWithTag("Lines"))
        {
            for (int k = 0; k < numOfFingers; k++)
            {
                float dx = GetLocalPosition(fingers[k].newPosition, i.transform).x;
                i.GetComponent<JudgeLineMovement>().positionX[k] = dx;
            }
        }

        if (numOfFingers > 0)
        {
            for (int i = 0; i < numOfFingers; i++)
            {
                int counter = 0;
                foreach(GameObject j in GameObject.FindGameObjectsWithTag("Lines"))
                {
                    NoteMovement n = j.GetComponent<JudgeLineMovement>().GetNearestNote(fingers[i]);
                    if (n != null)
                    {
                        notesInJudge[counter] = n;
                        counter++;
                    }
                }

                if (/*!fingers[i].isFirstClick || */counter == 0)
                    continue;

                NoteMovement note = notesInJudge[0];
                for (int j = 0; j < counter; j++)
                {
                    if (note.Note.time < notesInJudge[j].Note.time)
                        note = notesInJudge[j];
                }
                note.judge(GlobalSetting.musicProgress, fingers[i]);
            }
        }
    }

    public static Vector3 GetLocalPosition(Vector3 worldPosition, Transform parent)
    {
        Vector3 localPosition = parent.InverseTransformPoint(worldPosition);
        return localPosition;
    }

    public static bool NoteInJudgeArea(float fingerX, float noteX, int above)
    {
        return (fingerX > noteX - GlobalSetting.globalNoteScale / 2 && fingerX < noteX + GlobalSetting.globalNoteScale / 2);
    }
}
