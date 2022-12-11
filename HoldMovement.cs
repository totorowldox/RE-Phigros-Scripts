﻿using System;
using MainCore.Utilities;
using UnityEngine;
using Utilities;

namespace MainCore
{
    public class HoldMovement : NoteMovement
    {
    
        public Sprite HLspriteHoldBody;
        private SpriteRenderer[] holdRenders = new SpriteRenderer[3];
        private Transform[] holdParts = new Transform[3];
        private float holdLengthFactor = 19.036f;
        private bool holdMissed = false;
        private bool holdCatched = false;
        private bool holdOK = false;
        private float holdRealLength = 0;
        private float holdOriginLength = 0;
        private float holdEffectCnt = 0.2f;

        private float holdEventScale = 1f;

        private float releaseCounter = 0f;

        public override void OnStart()
        {
            destroyed = false;
            status = NoteStat.None;
            holdEffect = null;
            holdMissed = false;
            holdCatched = false;
            holdOK = false;
            holdRealLength = 0;
            holdOriginLength = 0;
            holdEffectCnt = 0.2f;
            releaseCounter = 0f;
            
            var scaleFactor = Note.size;
            originalSize = new Vector3(scaleFactor, isAbove, 1);
            gameObject.transform.localScale = originalSize;
            holdParts[0] = gameObject.transform.GetChild(0);
            holdParts[1] = gameObject.transform.GetChild(1);
            holdParts[2] = gameObject.transform.GetChild(2);
            holdRenders[0] = gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
            holdRenders[1] = gameObject.transform.GetChild(1).gameObject.GetComponent<SpriteRenderer>();
            holdRenders[2] = gameObject.transform.GetChild(2).gameObject.GetComponent<SpriteRenderer>();

            if (Note.isMulti)
            {
                holdRenders[0].sprite = HLsprite;
                holdRenders[1].sprite = HLspriteHoldBody;
            }
            else
            {
                holdRenders[0].sprite = NormalSprites[0];
                holdRenders[1].sprite = NormalSprites[1];
            }

            holdOriginLength = (parentLine.CalculateNoteHeight(Note.time + Note.holdTime) -
                                parentLine.CalculateNoteHeight(Note.time));

            cachedTransform = transform;
            cachedTransform.localEulerAngles = new Vector3(0, 0, 0);
            cachedTransform.localPosition = new Vector3(Note.positionX, 0, 0);
        
            holdParts[0].localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);
            holdParts[1].localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);
            holdParts[2].localScale = new Vector3(GlobalSetting.globalNoteScale, GlobalSetting.globalNoteScale, 1.0f);

            BeforeStart();
        }

        protected override void BeforeStart()
        {
            holdRenders[0].color = new Color(0, 0, 0, 0);
            holdRenders[1].color = new Color(0, 0, 0, 0);
            holdRenders[2].color = new Color(0, 0, 0, 0);
        }

        protected override void CheckOverLine()
        {
            bool isOverLine = parentLine.Line.isCover && cachedTransform.localPosition.y * isAbove < -0.01f;
            if (isOverLine)
            {
                temporaryColors[0] = new Color(1, 1, 1, 0);
                temporaryColors[1] = new Color(1, 1, 1, 0);
                temporaryColors[2] = new Color(1, 1, 1, 0);
            }
            else
            {
                if (parentLine.PgrTime < Note.time)
                    temporaryColors[0] = new Color(1, 1, 1, Note.alpha);
                else
                    temporaryColors[0] = new Color(1, 1, 1, 0f);
                temporaryColors[1] = new Color(1, 1, 1, Note.alpha);
                temporaryColors[2] = new Color(1, 1, 1, Note.alpha);
            }
        }

        protected override void CheckJudgeStatus()
        {
            if (GlobalSetting.autoPlay)
            {
                if (parentLine.PgrTime >= Note.time && !holdCatched)
                {
                    holdCatched = true;
                    GlobalSetting.PlayNoteSound(notetype);
                    status = NoteStat.Perfect;
                }
                if (parentLine.PgrTime >=
                    Note.time + Math.Max(0, Note.holdTime - parentLine.JudgeTime.judgeTime) && !holdOK)
                {
                    GlobalSetting.scoreCounter.Add(NoteStat.Perfect);
                    holdOK = true;
                }
                if (holdCatched)
                {
                    UpdateEffect();
                }
                if (parentLine.PgrTime >= Note.time + Note.holdTime)
                {
                    destroyed = true;
                }
                return;
            }
            if (holdCatched && !holdOK && !holdMissed && !GlobalSetting.Paused)
                JudgeHold();
            if (Note.time <= parentLine.PgrTime)
            {
                if (parentLine.PgrTime - Note.time >= parentLine.JudgeTime.bTime && !holdCatched && !GlobalSetting.autoPlay)
                {
                    holdMissed = true;
                    if (status != NoteStat.Miss)
                    {
                        GlobalSetting.scoreCounter.Add(NoteStat.Miss);
                        status = NoteStat.Miss;
                    }
                    temporaryColors[1] = new Color(1, 1, 1, 0.45f * Note.alpha);
                    temporaryColors[2] = new Color(1, 1, 1, 0.45f * Note.alpha);
                }
            }
            if (status != NoteStat.None)
                UpdateEffect();
            if (parentLine.PgrTime >= Note.time + Note.holdTime)
            {
                if (holdOK) destroyed = true;
                else if (parentLine.PgrTime >= Note.time + .2f) destroyed = true;
            }
            if (Note.time - parentLine.PgrTime < -parentLine.JudgeTime.bTime && !holdCatched && status == NoteStat.None) //没接住miss
            {
                GlobalSetting.scoreCounter.Add(NoteStat.Miss);
                status = NoteStat.Miss;
                parentLine.Notes.Remove(this);
                holdMissed = true;
            }
        }

        protected override void UpdateNoteHeight(float noteHeight)
        {
            if (parentLine.PgrTime < Note.time)
            {
                var localPosition = cachedTransform.localPosition;
                localPosition = new Vector3(GameUtils.GetAspectX(Note.positionX) * parentLine.GetNoteXFactor(noteHeight),
                    (isAbove * noteHeight) * parentLine.GetNoteYFactor(noteHeight), 
                    localPosition.z);
                cachedTransform.localPosition = localPosition;
            }
            else
            {
                var localPosition = cachedTransform.localPosition;
                localPosition = new Vector3(GameUtils.GetAspectX(Note.positionX) * parentLine.GetNoteXFactor(noteHeight),
                    0,
                    localPosition.z);
                cachedTransform.localPosition = localPosition;
            }
            cachedTransform.localScale = originalSize * parentLine.GetNoteSizeFactor(noteHeight);
            HoldLengthReset();
        }

        protected override void UpdateRenderer()
        {
            holdRenders[0].color = temporaryColors[0];
            holdRenders[1].color = temporaryColors[1];
            holdRenders[2].color = temporaryColors[2];
        }
    
        protected override void UpdateEffect()
        {
            if (status is NoteStat.Miss) return;
            if (GlobalSetting.Paused) return;
            if (holdCatched || holdOK)
            {
                holdEffectCnt += Time.deltaTime;
                if (holdEffectCnt >= 0.2f)
                {
                    holdEffect = HitEffectManager.GetInstance().GetObj(status == NoteStat.Perfect ? "clickRaw_0" : "clickRaw_0 1");
                    holdEffect.transform.position = cachedTransform.position;
                    holdEffect.PlayParticle();
                    holdEffectCnt = 0;
                }
            }
        }

        public override bool Judge(float time, Finger f)
        {
            if (status != NoteStat.None)
                return false;
            float deltaTime = Note.time - time;
            if (!holdOK && !holdMissed)
            {      
                if (!holdCatched && f.IsFirstClick)
                {
                    f.ClearTapFlag();
                    if (deltaTime > parentLine.JudgeTime.gTime)
                    {
                        status = NoteStat.Early;
                        //GlobalSetting.scoreCounter.early++;
                        GlobalSetting.PlayNoteSound(notetype);
                    }
                    else if (deltaTime > -parentLine.JudgeTime.gTime)
                    {
                        status = NoteStat.Perfect;
                        GlobalSetting.PlayNoteSound(notetype);
                    }
                    else
                    {
                        status = NoteStat.Late;
                        //GlobalSetting.scoreCounter.late++;
                        GlobalSetting.PlayNoteSound(notetype);
                    }
                    holdCatched = true;
                    parentLine.Notes.Remove(this);
                    return true;
                }
            }

            return false;
        }

        private void HoldLengthReset()
        {
            if (Note.time <= parentLine.PgrTime)
            {
                var clampedLength = Math.Max(0, parentLine.CalculateNoteHeight(Note.time + Note.holdTime));
                //holdRealLength = (float) (.3f * Note.speed * clampedLength * speedFactor / 6.75f);
                holdRealLength = (float) (Note.speed * clampedLength * parentLine.SpeedFactor / 19f);
                holdParts[1].localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
                holdParts[2].localPosition = new Vector3(0, holdRealLength * holdLengthFactor, 0);
            }
            else
            {
                holdRealLength = (float) (Note.speed * holdOriginLength * parentLine.SpeedFactor / 19f);
                holdParts[1].localScale = new Vector3(GlobalSetting.globalNoteScale, holdRealLength, 1.0f);
                holdParts[2].localPosition = new Vector3(0, holdRealLength * holdLengthFactor, 0);
            }
        }
    
        private void JudgeHold()
        {
            if (Note.time + Note.holdTime - parentLine.JudgeTime.judgeTime <= parentLine.PgrTime)
            {
                holdOK = true;
                GlobalSetting.scoreCounter.Add(status);
                return;
            }
            holdCatched = false;
            for (int i = 0; i < JudgementManager.m_instance.numOfFingers; i++)
            {
                float dx = parentLine.PositionX[i];
                if (JudgementManager.m_instance.fingers[i].phase != TouchPhase.Canceled && JudgementManager.NoteInJudgeArea(dx, cachedTransform.localPosition.x))
                {
                    releaseCounter = 0;
                    holdCatched = true;
                    break;
                }
            }
            if (!holdCatched)//按住后断了hold => miss
            {
                releaseCounter += Time.deltaTime;
                holdCatched = true;
                if (releaseCounter > parentLine.JudgeTime.gTime)
                {
                    holdCatched = false;
                    holdMissed = true;
                    GlobalSetting.scoreCounter.Add(NoteStat.Miss);
                    status = NoteStat.Miss;
                }
            }
        }
    }
}