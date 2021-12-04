using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public int notetype = -1;
    public note Note;
    public int isAbove;
    private float speedFactor = 3f;
    public Sprite HLsprite;
    public Sprite HLspriteHoldBody;
    public AudioClip tapSound;
    public bool destroyed = false;
    private bool audioPlayed = false;
    private float holdRealLength = 0;
    public GameObject tapEffect;
    private GameObject holdEffect;
    public GameObject badTap;
    private float holdEffectCnt = 0.2f;
    private float disappearTime;
    private NoteStat status = NoteStat.None;

    private float holdLengthFactor = 1f;

    //优化
    private AudioSource audioSource { get { return gameObject.GetComponent<AudioSource>();} }
    private JudgeLineMovement parentLine { get { return gameObject.GetComponentInParent<JudgeLineMovement>(); } }



    // Start is called before the first frame update
    void Start()
    {
        speedFactor = GlobalSetting.noteSpeedFactor * 3f;
        gameObject.AddComponent<AudioSource>();
        gameObject.GetComponent<AudioSource>().playOnAwake = false;
        gameObject.GetComponent<AudioSource>().clip = tapSound;
        if (notetype == 3)
        {
            gameObject.transform.localScale = new Vector3(0.1f, isAbove / 2.0f, 1);
            holdRealLength = speedFactor * Note.speed * GlobalSetting.globalNoteScale * Note.holdTime * parentLine.timeFactor / 2f - GlobalSetting.globalNoteScale / 38f;
            gameObject.transform.GetChild(0).localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);
            gameObject.transform.GetChild(1).localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
            gameObject.transform.GetChild(2).localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);

            gameObject.transform.GetChild(1).localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f, 0);
            gameObject.transform.GetChild(2).localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f + holdRealLength * 19, 0);

            //gameObject.GetComponent<SpriteRenderer>().sortingOrder = (isAbove == 1 ? parentLine.id * 2 : parentLine.id * 2 + 1);
            //transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sortingOrder = (isAbove == 1 ? parentLine.id * 2 : parentLine.id * 2 + 1);
            //transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sortingOrder = (isAbove == 1 ? parentLine.id * 2 : parentLine.id * 2 + 1);
            //transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().sortingOrder = (isAbove == 1 ? parentLine.id * 2 : parentLine.id * 2 + 1);
        }
        else
            gameObject.transform.localScale = new Vector3(GlobalSetting.globalNoteScale / 10, isAbove * GlobalSetting.globalNoteScale / 2, 1);
        disappearTime = (Note.type != 3 ? (Note.time + parentLine.judgeTime.bTime) : (Note.time + Note.holdTime));
    }

    // Update is called once per frame
    void Update()
    {
        if (destroyed && !audioSource.isPlaying)
        {
            parentLine.notes.Remove(gameObject);
            Destroy(gameObject);
            return;
        }
        else if (destroyed)
            return;


        if (!GlobalSetting.playing)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
            if (Note.type == 3)
            {
                gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
                gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
                gameObject.transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
            }
            if (GlobalSetting.highLightedNotes[Note.time] > 1 && GlobalSetting.highLight)
            {
                if (Note.type != 3)
                    gameObject.GetComponent<SpriteRenderer>().sprite = HLsprite;
                else
                {
                    gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = HLsprite;
                    gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().sprite = HLspriteHoldBody;
                    //holdLengthFactor = 1.026f;
                }
            }
            return;
        }
        if (Note.type != 3)
        {
            transform.localPosition = new Vector3(transform.localPosition.x,
            (float)(isAbove * (Note.floorPosition - parentLine.virtualPosY) * 
            Note.speed * speedFactor), transform.localPosition.z);
        }
        else if (Note.time <= parentLine.pgrTime)
        {
            transform.localPosition = new Vector3(transform.localPosition.x,
            0, transform.localPosition.z);
            gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
            holdLengthReset();
        }
        else
        {
            transform.localPosition = new Vector3(transform.localPosition.x,
            (float)(isAbove * (Note.floorPosition - parentLine.virtualPosY) *
            speedFactor), transform.localPosition.z);
        }
        if (GlobalSetting.autoPlay)
        {
            if (parentLine.pgrTime - Note.time >= 0 && !audioPlayed)
            {
                if (notetype == 1 || notetype == 3)
                {
                    float rannum = Random.Range(0f, 1f);
                    if (rannum < 1.1f)
                    {
                        GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                        status = NoteStat.Perfect;
                        audioPlayed = true;
                    }
                    else
                    {
                        GlobalSetting.scoreCounter.add(NoteStat.Good);
                        status = NoteStat.Good;
                        audioPlayed = true;
                    }
                }
                else
                {
                    GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                    status = NoteStat.Perfect;
                    audioPlayed = true;
                }
                
                audioSource.Play();
            }
        }

        if (Note.time <= parentLine.pgrTime)
        {
            if (Note.type != 3)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, Mathf.Max(1 - (parentLine.pgrTime - Note.time) / parentLine.judgeTime.bTime, 0));
            }
            else if (!audioPlayed &&  parentLine.pgrTime - Note.time >= parentLine.judgeTime.bTime)
            {
                gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.45f);
                gameObject.transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.45f);
            }
        }

        if (audioPlayed)
            judge();
        if (parentLine.pgrTime >= disappearTime)
        {
            parentLine.notes.Remove(gameObject);
            Destroy(gameObject);
        }

        if (Note.time - parentLine.pgrTime < -parentLine.judgeTime.bTime && notetype != 3)
        {
            GlobalSetting.scoreCounter.add(NoteStat.Miss);
            status = NoteStat.Miss;
            Destroy(gameObject);
        }
    }

    public void changeAlpha()
    {
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        if (Note.type == 3)
        {
            gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = Color.white;
            gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>().color = Color.white;
            gameObject.transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void judge()
    {
        if (audioPlayed && notetype != 3)
        {
            transform.localPosition = new Vector3(transform.localPosition.x,
            0, transform.localPosition.z);
            destroyed = true;
            if (Note.type != 3)
                holdEffect = Instantiate(tapEffect, transform.position, Quaternion.identity);
            gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
        }
        if (Note.type == 3)
        {
            holdEffectCnt += Time.deltaTime;
            if (holdEffectCnt > 0.1f)
            {
                holdEffect = Instantiate(tapEffect, new Vector2(
                    transform.parent.position.x + Mathf.Cos(parentLine.transform.eulerAngles.z * Mathf.Deg2Rad) * Note.positionX,
                    transform.parent.position.y + Mathf.Sin(parentLine.transform.eulerAngles.z * Mathf.Deg2Rad) * Note.positionX), Quaternion.identity);
                //holdEffect.GetComponent<EffectManager>().isHold = true;
                holdEffectCnt = 0;
            }
        }
        if (holdEffect != null)
        {
            if (status == NoteStat.Perfect)
                holdEffect.GetComponent<SpriteRenderer>().color = GlobalSetting.lineColors[JudgeLineStat.AP];
            else if (status == NoteStat.Good)
                holdEffect.GetComponent<SpriteRenderer>().color = GlobalSetting.lineColors[JudgeLineStat.FC];
            else
                Destroy(holdEffect);
        }
    }

    private void holdLengthReset()
    {
        holdRealLength = speedFactor * Note.speed * GlobalSetting.globalNoteScale * (Note.time + Note.holdTime - parentLine.pgrTime) * parentLine.timeFactor / 2f;
        gameObject.transform.GetChild(1).localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
        gameObject.transform.GetChild(1).localPosition = new Vector3(0, 0, 0);
        gameObject.transform.GetChild(2).localPosition = new Vector3(0, holdRealLength * 19 * holdLengthFactor, 0);
    }

    public void judge(float time)
    {
        if (status != NoteStat.None)
            return;
        float deltaTime = Note.time - time;
        if (notetype == 1)
        {
            if (deltaTime > parentLine.judgeTime.bTime)
            {
                status = NoteStat.Bad;
                GlobalSetting.scoreCounter.add(NoteStat.Bad);
                Instantiate(badTap, transform.position, Quaternion.identity).transform.localScale = transform.lossyScale;
                Destroy(gameObject);
            }
            else if (deltaTime > parentLine.judgeTime.gTime)
            {
                status = NoteStat.Good;
                GlobalSetting.scoreCounter.add(NoteStat.Good);
                audioPlayed = true;
                audioSource.Play();
            }
            else if (deltaTime > -parentLine.judgeTime.gTime)
            {
                status = NoteStat.Perfect;
                GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                audioPlayed = true;
                audioSource.Play();
            }
            else
            {
                status = NoteStat.Good;
                GlobalSetting.scoreCounter.add(NoteStat.Good);
                audioPlayed = true;
                audioSource.Play();
            }
        }
        else if (notetype != 3)
        {
            status = NoteStat.Perfect;
            GlobalSetting.scoreCounter.add(NoteStat.Perfect);
            audioPlayed = true;
            audioSource.Play();
        }
        return;
    }
}
