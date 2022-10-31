using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public int notetype = -1;
    public note Note;
    public int isAbove;
    private float speedFactor = 2.25f;
    public Sprite HLsprite;
    public Sprite HLspriteHoldBody;
    public AudioClip tapSound;
    public bool destroyed = false;
    private float holdRealLength = 0;
    public GameObject tapEffect;
    private GameObject holdEffect;
    public GameObject badTap;
    private float holdEffectCnt = 0.2f;
    private float disappearTime;
    public NoteStat status = NoteStat.None;
    private float destroyTime;
    private bool holdMissed = false;
    private bool holdCatched = false;
    private bool holdOK = false;
    private SpriteRenderer thisRenderer;

    private float holdLengthFactor = 1f;
    public int parentLineId = -1;

    private SpriteRenderer[] holdRenders = new SpriteRenderer[3];
    private Transform[] holdParts = new Transform[3];
    public Transform cachedTransform;

    private double holdOriginSpeed;

    private float holdOriginLength;

    //优化
    public JudgeLineMovement parentLine => GlobalSetting.lines[parentLineId];

    // Start is called before the first frame update
    void Start()
    {
        if(GlobalSetting.oldTexture && notetype == 3)
        {
            transform.GetChild(2).gameObject.SetActive(false);
        }
        if (notetype == 3)
        {
            gameObject.transform.localScale = new Vector3(1 / parentLine.targetScale.x, isAbove / parentLine.targetScale.y, 1);
            holdRealLength = 3f * (float)Note.speed * 0.2f * Note.holdTime / 2f;
            gameObject.transform.GetChild(0).localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);
            gameObject.transform.GetChild(1).localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
            gameObject.transform.GetChild(2).localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);

            gameObject.transform.GetChild(1).localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f, 0);
            gameObject.transform.GetChild(2).localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f + holdRealLength * 19, 0);
        }
        else
            gameObject.transform.localScale = new Vector3(GlobalSetting.globalNoteScale / parentLine.targetScale.x, isAbove * GlobalSetting.globalNoteScale / parentLine.targetScale.y, 1);
        disappearTime = (Note.type != 3 ? (Note.time + parentLine.judgeTime.bTime) : (Note.time + Note.holdTime));
        status = NoteStat.None;
        if (!GlobalSetting.tapSounds.ContainsKey(notetype))
        {
            GlobalSetting.tapSounds.Add(notetype, tapSound);
        }
        thisRenderer = GetComponent<SpriteRenderer>();
        if (Note.type == 3)
        {
            holdParts[0] = gameObject.transform.GetChild(0);
            holdParts[1] = gameObject.transform.GetChild(1);
            holdParts[2] = gameObject.transform.GetChild(2);
            holdRenders[0] = gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
            holdRenders[1] = gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();
            holdRenders[2] = gameObject.transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>();
        }

        transform.localPosition = new Vector3(transform.localPosition.x * (GlobalSetting.aspect / 16f * 9f), transform.localPosition.y, transform.localPosition.z);
        holdOriginSpeed = Note.speed;
        cachedTransform = transform;

        holdOriginLength = parentLine.CalculateNoteHeight(Note.time + Note.holdTime) -
                           parentLine.CalculateNoteHeight(Note.time);
    }
    
    // Update is called once per frame
    void Update()
    {
        speedFactor = GlobalSetting.noteSpeedFactor * 3f / Mathf.Sqrt(GlobalSetting.aspect);
        if (Note.type == 3)
        {
            Note.speed = holdOriginSpeed;
        }
        
        if (destroyed)
        {
            Destroy(gameObject);
            return;
        }
        
        if (status == NoteStat.Perfect && (notetype == 2 || notetype == 4))
        {
            var localPosition = cachedTransform.localPosition;
            localPosition = new Vector3(localPosition.x,
            (float)(isAbove * (Note.floorPosition - parentLine.virtualPosY) *
            Note.speed * speedFactor), localPosition.z);
            cachedTransform.localPosition = localPosition;
            return;
        }

        if (!GlobalSetting.playing)
        {
            thisRenderer.color = new Color(0, 0, 0, 0);
            if (Note.type == 3)
            {
                holdRenders[0].color = new Color(0, 0, 0, 0);
                holdRenders[1].color = new Color(0, 0, 0, 0);
                holdRenders[2].color = new Color(0, 0, 0, 0);
            }
            if (GlobalSetting.highLightedNotes[Note.time] > 1 && GlobalSetting.highLight)
            {
                if (Note.type != 3)
                    thisRenderer.sprite = HLsprite;
                else
                {
                    holdRenders[0].sprite = HLsprite;
                    holdRenders[1].sprite = HLspriteHoldBody;
                    holdLengthFactor = 1.026f;
                }
            }
            return;
        }
        if (Note.type != 3)
        {
            var localPosition = cachedTransform.localPosition;
            localPosition = new Vector3(localPosition.x,
                (float)(isAbove * (Note.floorPosition - parentLine.virtualPosY) * 
                        Note.speed * speedFactor), localPosition.z);
            cachedTransform.localPosition = localPosition;
        }
        else if (Note.time <= parentLine.pgrTime)
        {
            var localPosition = cachedTransform.localPosition;
            localPosition = new Vector3(localPosition.x,
            0, localPosition.z);
            cachedTransform.localPosition = localPosition;
            holdRenders[0].color = new Color(0, 0, 0, 0);
        }
        else
        {
            var localPosition = cachedTransform.localPosition;
            localPosition = new Vector3(localPosition.x,
                (float)(isAbove * (Note.floorPosition - parentLine.virtualPosY) *
                        speedFactor), localPosition.z);
            cachedTransform.localPosition = localPosition;
        }
        if (notetype == 3)
            HoldLengthReset();
        if (Note.time <= 1e9) // 隐藏过线Note
        {
            bool isOverLine = GlobalSetting.formatVersion != 114514 && cachedTransform.localPosition.y * isAbove < -0.01;
            thisRenderer.color = isOverLine ? new Color(1, 1, 1, 0) : new Color(1, 1, 1, 1);
            if (isOverLine && notetype == 3)
            {
                holdRenders[0].color = new Color(1, 1, 1, 0f);
                holdRenders[1].color = new Color(1, 1, 1, 0f);
                holdRenders[2].color = new Color(1, 1, 1, 0f);
            }
            else if (notetype == 3)
            {
                if (!holdCatched && !holdMissed)
                    holdRenders[0].color = new Color(1, 1, 1, 1f);
                else
                    holdRenders[0].color = new Color(1, 1, 1, 0f);
                holdRenders[1].color = new Color(1, 1, 1, 1f);
                holdRenders[2].color = new Color(1, 1, 1, 1f);
            }
        }
        //判定↓
        if (!Note.isFake)
        {
            if (GlobalSetting.autoPlay)
            {
                if (parentLine.pgrTime - Note.time >= 0 && status == NoteStat.None)
                {
                    if (notetype != 3)
                    {
                        GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                        status = NoteStat.Perfect;
                        parentLine.notes.Remove(this);
                        destroyed = true;
                    }
                    else
                    {
                        GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                        status = NoteStat.Perfect;
                        holdCatched = holdOK = true;
                    }

                    GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
                }
            }
            if (holdCatched && !holdOK && !holdMissed && !GlobalSetting.autoPlay)
                JudgeHold();
            if (Note.time <= parentLine.pgrTime)
            {
                if (Note.type != 3)
                {
                    thisRenderer.color = new Color(1, 1, 1, Mathf.Max(1 - (parentLine.pgrTime - Note.time) / parentLine.judgeTime.bTime, 0));
                }
                else if (parentLine.pgrTime - Note.time >= parentLine.judgeTime.bTime && !holdCatched && !GlobalSetting.autoPlay)
                {
                    holdMissed = true;
                    if (status != NoteStat.Miss)
                    {
                        GlobalSetting.scoreCounter.add(NoteStat.Miss);
                        status = NoteStat.Miss;
                    }
                    holdRenders[1].color = new Color(1, 1, 1, 0.45f);
                    holdRenders[2].color = new Color(1, 1, 1, 0.45f);
                }
            }

            if (status != NoteStat.None)
                UpdateEffect();
            if (parentLine.pgrTime >= disappearTime)
            {
                if (status == NoteStat.None && notetype != 3)//排除hold
                {
                    GlobalSetting.scoreCounter.add(NoteStat.Miss);
                    status = NoteStat.Miss;
                    parentLine.notes.Remove(this);
                }
                destroyed = true;
            }

            if (Note.time - parentLine.pgrTime < -parentLine.judgeTime.bTime && !holdCatched && status == NoteStat.None) //没接住miss
            {
                GlobalSetting.scoreCounter.add(NoteStat.Miss);
                status = NoteStat.Miss;
                parentLine.notes.Remove(this);
                if(notetype != 3)
                {
                    destroyed = true;
                }
                holdMissed = true;
            }
        }
        else
        {
            if (parentLine.pgrTime >= Note.time)
            {
                destroyed = true;
            }
        }
        
        //alpha扩展
        if (parentLine.alphaExtensionEnabled)
        {
            if (notetype == 3)
            {
                holdRenders[0].color = Color.clear;
                holdRenders[1].color = Color.clear;
                holdRenders[2].color = Color.clear;
            }
            else
            {
                thisRenderer.color = Color.clear;
            }
        }
    }
    private void UpdateEffect()
    {
        if (notetype != 3)
        {
            var localPosition = cachedTransform.localPosition;
            localPosition = new Vector3(localPosition.x,
            0, localPosition.z);
            cachedTransform.localPosition = localPosition;
            destroyed = true;
            holdEffect = ObjectPool.GetInstance().GetObj(GlobalSetting.oldTexture ? "HitFX_01" : "clickRaw_0");
            holdEffect.transform.position = cachedTransform.position;
            if (GlobalSetting.is3D)
            {
                holdEffect.transform.eulerAngles = cachedTransform.eulerAngles;
            }
            thisRenderer.color = new Color(0, 0, 0, 0);
        }
        if (Note.type == 3 && (holdCatched || holdOK))
        {
            holdEffectCnt += Time.deltaTime;
            if (holdEffectCnt > 0.2f)
            {
                /*holdEffect = Instantiate(tapEffect, new Vector2(
                    transform.parent.position.x + Mathf.Cos(parentLine.transform.eulerAngles.z * Mathf.Deg2Rad) * Note.positionX,
                    transform.parent.position.y + Mathf.Sin(parentLine.transform.eulerAngles.z * Mathf.Deg2Rad) * Note.positionX), Quaternion.identity);*/
                holdEffect = ObjectPool.GetInstance().GetObj(GlobalSetting.oldTexture ? "HitFX_01" : "clickRaw_0");
                holdEffect.transform.position = cachedTransform.position;
                if (GlobalSetting.is3D)
                {
                    holdEffect.transform.eulerAngles = cachedTransform.eulerAngles;
                }
                //holdEffect.GetComponent<EffectManager>().isHold = true;
                holdEffectCnt = 0;
            }
        }
        if (holdEffect != null)
        {
            if (status == NoteStat.Perfect)
                holdEffect.GetComponent<SpriteRenderer>().color = GlobalSetting.lineColors[JudgeLineStat.AP];
            else if (status == NoteStat.Good || status == NoteStat.Early || status == NoteStat.Late)
                holdEffect.GetComponent<SpriteRenderer>().color = GlobalSetting.lineColors[JudgeLineStat.FC];
        }
    }

    private void HoldLengthReset()
    {
        if (Note.time <= parentLine.pgrTime)
        {
            holdRealLength = (.3f * parentLine.CalculateNoteHeight(Note.time + Note.holdTime) + GlobalSetting.globalNoteScale / 38f) * speedFactor / 2.25f;
            holdParts[1].localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
            holdParts[1].localPosition = new Vector3(0, 0, 0);
            holdParts[2].localPosition = new Vector3(0, holdRealLength * 19, 0);
        }
        else
        {
            //holdRealLength = (.3f * (float) Note.speed * Note.holdTime) * speedFactor / 2.25f;
            holdRealLength = .3f * holdOriginLength * speedFactor / 2.25f;
            try
            {
                holdParts[0].localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);
                holdParts[1].localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
                holdParts[2].localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);

                holdParts[1].localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f, 0);
                holdParts[2].localPosition = new Vector3(0, GlobalSetting.globalNoteScale / 2f + holdRealLength * 19, 0);
            }
            catch
            {
                // ignored
            }
        }
    }

    public bool Judge(float time, Finger f)
    {
        if (status != NoteStat.None)
            return false;
        float deltaTime = Note.time - time;
        if (notetype == 1 && f.IsFirstClick)
        {
            f.ClearTapFlag();
            if (deltaTime > parentLine.judgeTime.bTime)
            {
                status = NoteStat.Bad;
                GlobalSetting.scoreCounter.add(NoteStat.Bad);
                Instantiate(badTap, cachedTransform.position, cachedTransform.rotation).transform.localScale = cachedTransform.lossyScale;
                parentLine.notes.Remove(this);
                destroyed = true;
            }
            else if (deltaTime > parentLine.judgeTime.gTime)
            {
                status = NoteStat.Good;
                GlobalSetting.scoreCounter.add(NoteStat.Good);
                GlobalSetting.scoreCounter.early++;
                parentLine.notes.Remove(this);
                GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
            }
            else if (deltaTime > -parentLine.judgeTime.gTime)
            {
                status = NoteStat.Perfect;
                GlobalSetting.scoreCounter.add(NoteStat.Perfect);
                parentLine.notes.Remove(this);
                GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
            }
            else
            {
                status = NoteStat.Good;
                GlobalSetting.scoreCounter.add(NoteStat.Good);
                GlobalSetting.scoreCounter.late++;
                parentLine.notes.Remove(this);
                GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
            }
            return true;
        }
        else if (notetype == 2 && Mathf.Abs(deltaTime) < parentLine.judgeTime.judgeTime)
        {
            status = NoteStat.Perfect;
            destroyTime = deltaTime;
            StartCoroutine(DestroyDelayed(destroyTime));
            parentLine.notes.Remove(this);
            return true;
        }
        else if (notetype == 4 && Mathf.Abs(deltaTime) < parentLine.judgeTime.judgeTime && f.phase == TouchPhase.Moved)
        {
            status = NoteStat.Perfect;
            destroyTime = deltaTime;
            StartCoroutine(DestroyDelayed(destroyTime));
            parentLine.notes.Remove(this);
            return true;
        }
        else if (notetype == 3 && !holdOK && !holdMissed)
        {      
            if (!holdCatched && f.IsFirstClick)
            {
                f.ClearTapFlag();
                if (deltaTime > parentLine.judgeTime.gTime)
                {
                    status = NoteStat.Early;
                    //GlobalSetting.scoreCounter.early++;
                    GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
                }
                else if (deltaTime > -parentLine.judgeTime.gTime)
                {
                    status = NoteStat.Perfect;
                    GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
                }
                else
                {
                    status = NoteStat.Late;
                    //GlobalSetting.scoreCounter.late++;
                    GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
                }
                holdCatched = true;
                parentLine.notes.Remove(this);
                return true;
            }
            /*else
            {
                if (Note.time + Note.holdTime - parentLine.judgeTime.gTime > parentLine.pgrTime)
                {
                    holdOK = true;
                    GlobalSetting.scoreCounter.add(status);
                    parentLine.notes.Remove(this);
                }
            }*/
            /*status = NoteStat.Perfect;
            GlobalSetting.scoreCounter.add(NoteStat.Perfect);
            GlobalSetting.playNoteSound(notetype, transform.position);
            parentLine.notes.Remove(this);*/
        }
        return false;
    }

    private IEnumerator DestroyDelayed(float delay)
    {
        destroyed = false;
        yield return new WaitForSeconds(delay);
        GlobalSetting.scoreCounter.add(status);
        GlobalSetting.PlayNoteSound(notetype, cachedTransform.position);
        UpdateEffect();
        destroyed = true;
    }

    private void JudgeHold()
    {
        if (Note.time + Note.holdTime - parentLine.judgeTime.gTime <= parentLine.pgrTime)
        {
            holdOK = true;
            GlobalSetting.scoreCounter.add(status);
            return;
        }
        holdCatched = false;
        for (int i = 0; i < JudgementManager.m_instance.numOfFingers; i++)
        {
            float dx = parentLine.positionX[i];
            if (JudgementManager.m_instance.fingers[i].phase != TouchPhase.Canceled && JudgementManager.NoteInJudgeArea(dx, cachedTransform.localPosition.x))
            {
                holdCatched = true;
                break;
            }
        }
        if (!holdCatched)//按住后断了hold => miss
        {
            holdMissed = true;
            GlobalSetting.scoreCounter.add(NoteStat.Miss);
            status = NoteStat.Miss;
        }
    }

    private void OnDestroy()
    {
        try
        {
            parentLine.notes.Remove(this);
        }
        catch
        {
            Debug.Log("already removed");
        }
    }
}
