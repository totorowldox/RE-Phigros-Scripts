using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgeTime
{
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
    public List<GameObject> notes = new List<GameObject>();
    public float pgrTime = 0;
    public float timeFactor = 0;
    private const bool DEBUG = false;
    public double virtualPosY = 0;
    private float virtualPosYVersion1 = 0;
    public JudgeTime judgeTime = new JudgeTime();

    private float slope = 0;

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
        //transform.GetChild(0).gameObject.GetComponent<SpriteMask>().frontSortingOrder = id * 2;
        //transform.GetChild(0).gameObject.GetComponent<SpriteMask>().backSortingOrder = id * 2 - 1;
        //transform.GetChild(1).gameObject.GetComponent<SpriteMask>().frontSortingOrder = id * 2 + 1;
        //transform.GetChild(1).gameObject.GetComponent<SpriteMask>().backSortingOrder = id * 2;
        timeFactor = 1.875f / line.bpm; //Proportional
        if (GlobalSetting.formatVersion == 1)
        {
            foreach (judgeLineSpeedEvent b in line.speedEvents)
            {
                if (b.startTime < 0)
                    b.startTime = 0;
                b.floorPosition = virtualPosYVersion1;
                virtualPosYVersion1 += (b.endTime - b.startTime) * b.value * timeFactor;
            }
        }
        judgeTime.bTime = 0.16f / timeFactor;
        judgeTime.gTime = 0.08f / timeFactor;
        judgeTime.judgeTime = 0.2f / timeFactor;
        transform.localScale = new Vector3(10, 2, 1);
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
        //t.tag = $"Note_Line{id}";
        t.transform.SetParent(transform);
        //t.SetActive(false);
        notes.Add(t);
    }

    // Update is called once per frame
    void Update()
    {

        gameObject.GetComponent<SpriteRenderer>().color = GlobalSetting.lineColors[GlobalSetting.lineStat];

        if (!GlobalSetting.playing)
            return;

        pgrTime += (Time.deltaTime / timeFactor);  //Time convert
        //pgrTime = GlobalSetting.musicProgress / timeFactor;
        

        UpdateMovement();
        UpdateRotation();
        UpdateAlpha();

        #region SPEED EVENT
        foreach (judgeLineSpeedEvent b in line.speedEvents)
        {
            judgeLineSpeedEvent i = b;
            if (pgrTime < i.startTime) break;
            if (pgrTime > i.endTime) continue;
            virtualPosY = (pgrTime - i.startTime) * i.value * timeFactor + i.floorPosition;
        }
        #endregion

    }

    void FixedUpdate()
    {
        if (Math.Abs(pgrTime - GlobalSetting.musicProgress / timeFactor) >= 10)
            pgrTime = GlobalSetting.musicProgress / timeFactor;
        if (!GlobalSetting.autoPlay)
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (Input.anyKey)
                {
                    foreach (GameObject i in notes)
                    {
                        if (i.GetComponent<NoteMovement>().Note.time - pgrTime > judgeTime.judgeTime)
                            break;
                        if (!i.GetComponent<NoteMovement>().destroyed)
                        {
                            i.GetComponent<NoteMovement>().judge(pgrTime);
                            break;
                        }

                    }
                }
            }
        }
    }

    void UpdateMovement()
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
                    a.x = Screen.width * i.end;
                    a.y = Screen.height * i.end2;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveTarget = a;
                    a.x = Screen.width * i.start;
                    a.y = Screen.height * i.start2;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveFrom = a;
                }
                else if (GlobalSetting.formatVersion == 1)
                {
                    i.startTime = i.startTime < 0 ? 0 : i.startTime;
                    i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                    Vector3 a = new Vector3();
                    a.x = i.end / 1000 / 880 * Screen.width;
                    a.y = i.end % 1000 / 520 * Screen.height;
                    a = Camera.main.ScreenToWorldPoint(a);
                    a.z = 0;
                    moveTarget = a;
                    a.x = i.start / 1000 / 880 * Screen.width;
                    a.y = i.start % 1000 / 520 * Screen.height;
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

    void UpdateRotation()
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

    void UpdateAlpha()
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
                gameObject.GetComponent<SpriteRenderer>().color = new Color(GlobalSetting.lineColors[GlobalSetting.lineStat].r, GlobalSetting.lineColors[GlobalSetting.lineStat].g, GlobalSetting.lineColors[GlobalSetting.lineStat].b, Mathf.Lerp(appearFrom, appearTarget, (pgrTime - appearFromTime) / (appearTargetTime - appearFromTime)));
        //}
    }

    void OnGUI()
    {
        Event @event = Event.current;
        if (!@event.isKey)
            return;
        //GUI.Label(new Rect(Camera.main.WorldToScreenPoint(transform.position), new Vector2(100, 100)), $"<size=20><color=green>{id}</color></size>");
        
            /*if (Input.GetMouseButton(0))
            {
                Vector2 dir = new Vector2(- Mathf.Sin(transform.localEulerAngles.z * Mathf.Deg2Rad), Mathf.Cos( - transform.localEulerAngles.z * Mathf.Deg2Rad));
                Debug.DrawRay(Camera.main.ScreenPointToRay(Input.mousePosition).origin,
                   dir * 100,
                   Color.red, 0.2f);
                Debug.DrawRay(Camera.main.ScreenPointToRay(Input.mousePosition).origin,
                   dir * -100,
                   Color.red, 0.2f);
                Ray rayup = new Ray(Camera.main.ScreenPointToRay(Input.mousePosition).origin, dir);
                Ray raydown = new Ray(Camera.main.ScreenPointToRay(Input.mousePosition).origin, -dir);
                if (Physics.Raycast(rayup, out RaycastHit hit, Mathf.Infinity))
                {
                    Debug.Log("aaa");
                    foreach (GameObject i in notes)
                    {
                        if (i.GetComponent<NoteMovement>().Note.time - pgrTime > judgeTime.bTime)
                            break;
                        if (i.transform == hit.transform)
                            i.GetComponent<NoteMovement>().judge(pgrTime);
                    }
                }
                if (Physics.Raycast(raydown, out hit, Mathf.Infinity))
                {
                    Debug.Log("aaa");
                    foreach (GameObject i in notes)
                    {
                        if (i.GetComponent<NoteMovement>().Note.time - pgrTime > judgeTime.bTime)
                            break;
                        if (i.transform == hit.transform)
                            i.GetComponent<NoteMovement>().judge(pgrTime);
                    }
                }
            }*/
    }

    void ResetScale()
    {
        StartCoroutine(ResetScaleCoroutine());
    }

    private IEnumerator ResetScaleCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        transform.localScale = new Vector3(10, 2, 1);
        yield return new WaitForSeconds(1f);
        foreach(GameObject i in notes)
        {
            i.SetActive(true);
            i.GetComponent<NoteMovement>().changeAlpha();
        }
    }

}
