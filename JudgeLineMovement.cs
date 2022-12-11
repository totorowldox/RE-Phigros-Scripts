using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MainCore;
using MainCore.Data;
using MainCore.UI;
using MainCore.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace MainCore
{
    public class JudgeTime
    {
        public float pTime;
        public float gTime;
        public float bTime;
        public float judgeTime;
    }

    public sealed class JudgeLineMovement : MonoBehaviour
    {
        //Serializable fields
        public NoteMovement Tap;
        public NoteMovement Flick;
        public NoteMovement Drag;
        public HoldMovement Hold;
        public Transform JudgeLineTopTransform;
        public TextMesh customText;

        //Private fields
        private double virtualPosYVersion1 = 0;
        private SpriteRenderer sr;
        private float alphaVal = 0;
        private Camera mainCamera;
        private bool isUILine = false;

        //Public fields
        [NonSerialized] public Vector3 TargetScale = new Vector3(10, 3, 1);
        [NonSerialized] public bool IsImage = false;
        [NonSerialized] public List<NoteMovement> Notes = new List<NoteMovement>();
        [NonSerialized] public List<NoteMovement> NotesCanBeUpdated = new List<NoteMovement>();
        [NonSerialized] public List<note> ProcessingNotes = new();
        [NonSerialized] public List<float> PositionX = new List<float>(20);
        [NonSerialized] public int ID;
        public judgeLine Line;
        public readonly JudgeTime JudgeTime = new JudgeTime();
    
        //Properties
        public AlphaExtendMode AlphaExtension { get; private set; }
        public float VisibleTime { get; private set; }
        public double JudgeLineDistance { get; private set; }
        public float PgrTime { get; private set; }
        public Vector2 ScaleEventScale { get; private set; } = Vector2.one;
        public Vector2 MoveEventValue { get; private set; } = Vector2.one / 2;
        public float RotateEventValue { get; private set; } = 0;
        public float AlphaEventValue { get; private set; } = 0;
        public float SpeedFactor { get; private set; } = 1;
        private Vector2 UIMove { get; set; } = Vector2.zero;
        private bool InclineFlag { get; set; } = false;
    
        //Constant fields
        private const float MinimumDistanceToShow = 75f;
    
        //Static fields


        void Awake()
        {
            SpeedFactor = GlobalSetting.noteSpeedFactor * 6f;
        }

        void Start()
        {
            Init();
        }

        private void Init()
        {
            mainCamera = Camera.main;
            
            sr = gameObject.GetComponent<SpriteRenderer>();

            if (Line.useImage)
            {
                sr.sprite = Line.customImage;
                TargetScale = new Vector3(1f, 1f, 1);
                IsImage = true;
                //sr.sortingOrder = 0;
            }

            if (Line.attachUI != "**tHiSisnOne AtTaCH U_i TEmPlAtE**")
            {
                isUILine = true;
            }
        
            if(!IsImage)
                TargetScale = new Vector2(236 * 2.5f * Camera.main.orthographicSize * GlobalSetting.aspect / sr.sprite.texture.width, 
                    220 * 0.008f * Camera.main.orthographicSize / sr.sprite.texture.height);
            if (GlobalSetting.formatVersion == 1)
            {
                foreach (judgeLineSpeedEvent b in Line.speedEvents)
                {
                    if (b.startTime < 0)
                        b.startTime = 0;
                    b.floorPosition = virtualPosYVersion1;
                    virtualPosYVersion1 += (b.endTime - b.startTime) * b.value;
                }
            }
            JudgeTime.bTime = 0.16f;
            JudgeTime.gTime = 0.08f;
            JudgeTime.judgeTime = 0.2f;
            transform.localScale = TargetScale;
            for (int i = 0; i < 20; i++)
                PositionX.Add(0f);
            ProcessingNotes = Line.notesBelow.Concat(Line.notesAbove).ToList();
            ArrangeFloorPosition();
            if (GlobalSetting.isMirror)
            {
                for (var layer = 0; layer < Line.rpeLayers.Count; layer++)
                {
                    foreach (var i in Line.rpeLayers[layer].rotateEvents)
                    {
                        i.start = 360 - i.start;
                        i.end = 360 - i.end;
                    }
                    foreach (var i in Line.rpeLayers[layer].moveXEvents)
                    {
                        if (GlobalSetting.formatVersion != 1)
                        {
                            i.start = 1 - i.start;
                            i.end = 1 - i.end;
                        }
                        else
                        {
                            float origin = i.start / 1000;
                            i.start = i.start - origin + 800 - origin;
                            origin = i.end / 1000;
                            i.end = i.end - origin + 800 - origin;
                        }
                    }
                }
            }
        }

        private void InitNote(int type, note i, int fact)
        {
            ProcessingNotes.Remove(i);
            NoteMovement t;
            if (GlobalSetting.isMirror)
                i.positionX = -i.positionX;
            switch (type)
            {
                case 1:
                    t = NotePool.GetInstance().GetObj("Tap");//Instantiate(Tap, temp, Quaternion.identity);
                    break;
                case 2:
                    t = NotePool.GetInstance().GetObj("Drag");//Instantiate(Drag, temp, Quaternion.identity);
                    break;
                case 4:
                    t = NotePool.GetInstance().GetObj("Flick");//Instantiate(Flick, temp, Quaternion.identity);
                    break;
                case 3:
                    t = NotePool.GetInstance().GetObj("Hold");//Instantiate(Hold, temp + new Vector3(0, 0, 10), Quaternion.identity);
                    break;
                default:
                    t = NotePool.GetInstance().GetObj("Tap");//Instantiate(Tap, temp, Quaternion.identity);
                    break;
            }
            t.Note = i;
            t.notetype = type;
            t.isAbove = fact;
            t.parentLine = this;
            t.transform.SetParent(JudgeLineTopTransform);
            if (!i.isFake)
            {
                Notes.Add(t);
            }
            t.OnStart();
            NotesCanBeUpdated.Add(t);
        }

        // Update is called once per frame
        void Update()
        {
            if (!GlobalSetting.Playing)
            {
                sr.color = Color.clear;
                sr.sortingOrder = Line.zOrder * GlobalSetting.maximumZOrder + ID;
                return;
            }
        
            PgrTime = Main.NowTime;
        
            JudgeLineDistance = CalculateNoteHeight_internal(PgrTime);
        
            UpdateEventLayers();

            UpdateExtendedState();

            UpdateNotes();
        }

        internal (NoteMovement note, float flickTime, float absDistance) GetNearestNote(Finger finger, float dx)
        {
            var flickTime = 9999f;
            var tempNoteList = new List<NoteMovement>();
            var tolerance = JudgeTime.judgeTime + PgrTime;
            for(var j = 0; j < Notes.Count; j++)
            {
                var i = Notes[j];
                if (i.Note.time >= tolerance)
                    continue;
                if (i.Note.isFake)
                    continue;
                if (i.destroyed)
                    continue;
                if (!JudgementManager.NoteInJudgeArea(dx, i.cachedTransform.localPosition.x))
                    continue;
                if (i.notetype is 2 or 4) //flick和drag另外判定
                {
                    flickTime = Math.Min(i.Note.time, flickTime);
                }   
                if (i.status != NoteStat.None)
                    continue;
                if (i.notetype is 2 or 4) //flick和drag另外判定
                {
                    i.Judge(PgrTime, finger);
                }
                else
                    tempNoteList.Add(i);
            }
            if (tempNoteList.Count == 0)
                return (null, flickTime, 99999);
            var note = tempNoteList[0];
            for (var j = 0; j < tempNoteList.Count; j++)
            {
                if (note.Note.time > tempNoteList[j].Note.time)
                    note = tempNoteList[j]; //选time最小的note判定
            }
            return (note, flickTime, Math.Abs(note.cachedTransform.localPosition.x - dx));
        }

        //We don't need any sync with music!
        //Stopwatch is accurate enough!

        private void UpdateNotes()
        {
            
            /*var cnt = ProcessingNotes.Count;
            
            NativeArray<double> floorPosArray = new NativeArray<double>(cnt, Allocator.TempJob);
            NativeArray<float> yOffsetArray = new NativeArray<float>(cnt, Allocator.TempJob);
            NativeArray<float> noteSpeedArray = new NativeArray<float>(cnt, Allocator.TempJob);
            
            for (var index = 0; index < cnt; index++)
            {
                floorPosArray[index] = ProcessingNotes[index].floorPosition;
                yOffsetArray[index] = ProcessingNotes[index].yOffset;
                noteSpeedArray[index] = (float) ProcessingNotes[index].speed;
            }

            var updateNotesJob = new UpdateNotesJob()
            {
                floorPosArray = floorPosArray,
                yOffsetArray = yOffsetArray,
                noteSpeedArray = noteSpeedArray,
                judgeLineDistance = JudgeLineDistance,
                speedFactor = SpeedFactor
            };
            var jobHandle = updateNotesJob.Schedule(cnt, 5);

            jobHandle.Complete();*/
            
            for (var index = Math.Min(5000, ProcessingNotes.Count - 1); index >= 0; index--)
            {
                var n = ProcessingNotes[index];
                var height = (float) (n.floorPosition - JudgeLineDistance + n.yOffset) * SpeedFactor * n.speed;
                if (height <= MinimumDistanceToShow)
                {
                    InitNote(n.type, n, n.isAbove ? 1 : -1);
                }
            }
            
            /*cnt = NotesCanBeUpdated.Count;
            
            floorPosArray = new NativeArray<double>(cnt, Allocator.TempJob);
            yOffsetArray = new NativeArray<float>(cnt, Allocator.TempJob);
            noteSpeedArray = new NativeArray<float>(cnt, Allocator.TempJob);
            
            for (var index = 0; index < cnt; index++)
            {
                floorPosArray[index] = NotesCanBeUpdated[index].Note.floorPosition;
                yOffsetArray[index] = NotesCanBeUpdated[index].Note.yOffset;
                noteSpeedArray[index] = (float) NotesCanBeUpdated[index].Note.speed;
            }

            updateNotesJob = new UpdateNotesJob()
            {
                floorPosArray = floorPosArray,
                yOffsetArray = yOffsetArray,
                noteSpeedArray = noteSpeedArray,
                judgeLineDistance = JudgeLineDistance,
                speedFactor = SpeedFactor
            };
            jobHandle = updateNotesJob.Schedule(cnt, 5);

            jobHandle.Complete();*/
            
            for (var index = Math.Min(5000, NotesCanBeUpdated.Count - 1); index >= 0; index--)
            {
                //NotesCanBeUpdated[index].OnUpdate((float) floorPosArray[index]);
                var n = NotesCanBeUpdated[index];
                float height = (float) ((n.Note.floorPosition - JudgeLineDistance + n.Note.yOffset) * SpeedFactor * n.Note.speed);
                if (height < MinimumDistanceToShow)
                {
                    if (!n.gameObject.activeSelf)
                        n.gameObject.SetActive(true);
                }
                else 
                {
                    if (n.gameObject.activeSelf)
                        n.gameObject.SetActive(false);
                    continue;
                }
                n.OnUpdate(height);
            }
        }
        
        #region Update Events

        private void UpdateEventLayers()
        {
            var x = 0f;
            var y = 0f;
            var rotation = 0f;
            var inclined = 0f;
            var alpha = 0f;
        
            for (var i = 0; i < Line.rpeLayers.Count; i++)
            {
                x += UpdateMovementX(i);
                y += UpdateMovementY(i);
                rotation += UpdateRotation(i);
                alpha += UpdateAlpha(i);
            }

            x -= Line.rpeLayers.Count * .5f;
            y -= Line.rpeLayers.Count * .5f;
            inclined = UpdateInclineEvent();

            var transform1 = JudgeLineTopTransform;
            MoveEventValue = GameUtils.GetTransformedXY(new Vector2(x * 160f / 9f, y * 10f));
            if (Line.father > -1)
            {
                var t = GlobalSetting.lines[Line.father].MoveEventValue;
                var r = GlobalSetting.lines[Line.father].RotateEventValue;
                var matrix = Matrix4x4.TRS(t, Quaternion.Euler(0, 0, r), Vector3.one);
                t = matrix.MultiplyPoint3x4(MoveEventValue);
                transform1.position = t;
                UIMove = t;
            }
            else
            {
                transform1.position = MoveEventValue;
                UIMove = MoveEventValue;
            }
            
        
            var angles = JudgeLineTopTransform.localEulerAngles;
            transform1.localEulerAngles = new Vector3(0, angles.y, rotation);
            RotateEventValue = rotation;
            alphaVal = alpha;
            if (alphaVal < 0 && AlphaExtension == AlphaExtendMode.VisibleAll)
            {
                AlphaExtension = AlphaExtendMode.InvisibleAll;
            }

            if (isUILine)
            {
                AlphaEventValue = alphaVal;
                alphaVal = 0;
            }
        }

        private float UpdateInclineEvent()
        {
            int easeType = 0;
        
            if (Line.extended.inclineEvents.Count == 0)
                return 0;
        
            var i = GameUtils.GetEventFromCurrentTime(Line.extended.inclineEvents, PgrTime);
            i.startTime = i.startTime < 0 ? 0 : i.startTime;
            i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
            easeType = i.easeType;
            
            var z = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
                PgrTime - i.startTime, i.endTime - i.startTime, 
                i.start, i.end, i.easingLeft, i.easingRight);
            return z;
        }

        private float UpdateMovementX(int layer)
        {
            int easeType = 0;

            if (Line.rpeLayers[layer].moveXEvents.Count == 0)
                return .5f;

            var t1 = 0f;
            var t2 = 0f;
        
            var i = GameUtils.GetEventFromCurrentTime(Line.rpeLayers[layer].moveXEvents, PgrTime);
        
            if (GlobalSetting.formatVersion is not 1)
            {
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                t2 = i.end;
                t1 = i.start;
            }
            else
            {
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                t2 = i.end / 1000 / 880 - .5f;
                t1 = i.start / 1000 / 880 - .5f;
            }
        
            easeType = i.easeType;

            var x = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
                PgrTime - i.startTime, i.endTime - i.startTime, 
                t1, t2, i.easingLeft, i.easingRight);
            return x;
        }

        private float UpdateMovementY(int layer)
        {
            int easeType = 0;

            if (Line.rpeLayers[layer].moveYEvents.Count == 0)
                return .5f;
        
            var t1 = 0f;
            var t2 = 0f;
        
            var i = GameUtils.GetEventFromCurrentTime(Line.rpeLayers[layer].moveYEvents, PgrTime);
        
            if (GlobalSetting.formatVersion is not 1)
            {
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                t2 = i.end;
                t1 = i.start;
            }
            else
            {
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                t2 = i.end % 1000 / 520 - .5f;
                t1 = i.start % 1000 / 520 - .5f;
            }
        
            easeType = i.easeType;

            var y = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
                PgrTime - i.startTime, i.endTime - i.startTime, 
                t1, t2, i.easingLeft, i.easingRight);
            return y;
        }

        private float UpdateRotation(int layer)
        {
            int easeType = 0;
        
            if (Line.rpeLayers[layer].rotateEvents.Count == 0)
                return 0 ;
        
            var i = GameUtils.GetEventFromCurrentTime(Line.rpeLayers[layer].rotateEvents, PgrTime);;
            i.startTime = i.startTime < 0 ? 0 : i.startTime;
            i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
            easeType = i.easeType;
            
            var z = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
                PgrTime - i.startTime, i.endTime - i.startTime, 
                i.start, i.end, i.easingLeft, i.easingRight);
            return z;
        }

        private float UpdateAlpha(int layer)
        {
            int easeType = 0;
        
            if (Line.rpeLayers[layer].alphaEvents.Count == 0)
                return 0;
        
            var i = GameUtils.GetEventFromCurrentTime(Line.rpeLayers[layer].alphaEvents, PgrTime);
            i.startTime = i.startTime < 0 ? 0 : i.startTime;
            i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
            easeType = i.easeType;
        
            var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType, 
                PgrTime - i.startTime, i.endTime - i.startTime, 
                i.start, i.end, i.easingLeft, i.easingRight);

            AlphaExtension = i.alphaMode;
            VisibleTime = PgrTime + i.visibleTime;

            return a;
        }

        private void UpdateExtendedState()
        {
            float UpdateScaleX()
            {
                int easeType = 0;

                if (Line.extended.scaleXEvents.Count == 0)
                    return 1;

                var i = GameUtils.GetEventFromCurrentTime(Line.extended.scaleXEvents, PgrTime);
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                easeType = i.easeType;

                var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType,
                    PgrTime - i.startTime, i.endTime - i.startTime,
                    i.start, i.end);
                return a;
            }

            float UpdateScaleY()
            {
                int easeType = 0;

                if (Line.extended.scaleYEvents.Count == 0)
                    return 1;

                var i = GameUtils.GetEventFromCurrentTime(Line.extended.scaleYEvents, PgrTime);
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                easeType = i.easeType;

                var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType,
                    PgrTime - i.startTime, i.endTime - i.startTime,
                    i.start, i.end);
                return a;
            }

            Color UpdateColor()
            {
                int easeType = 0;

                if (Line.extended.colorEvents.Count == 0)
                    return GlobalSetting.lineColors[GlobalSetting.lineStat].SetAlpha(alphaVal);

                var i = GameUtils.GetEventFromCurrentTime(Line.extended.colorEvents, PgrTime);
                i.startTime = i.startTime < 0 ? 0 : i.startTime;
                i.endTime = i.endTime > 10000000 ? i.startTime + 1 : i.endTime;
                easeType = i.easeType;

                var r = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType,
                    PgrTime - i.startTime, i.endTime - i.startTime,
                    i.start.r, i.end.r);
                var g = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType,
                    PgrTime - i.startTime, i.endTime - i.startTime,
                    i.start.g, i.end.g);
                var b = EaseUtils.GetEaseResult((EaseUtils.EaseType) easeType,
                    PgrTime - i.startTime, i.endTime - i.startTime,
                    i.start.b, i.end.b);
                return new Color(r, g, b, alphaVal);
            }

            string UpdateCustomText()
            {
                customText.color = sr.color;
                string targetStr = "ThisIsNULLText";
                if (Line.extended.textEvents.Count > 0)
                {
                    var i = GameUtils.GetEventFromCurrentTime(Line.extended.textEvents, PgrTime);

                    float startT = i.startTime;
                    float endT = i.endTime;
                    string start = i.start;
                    string end = i.end;
                    var now = EaseUtils.GetEaseResult((EaseUtils.EaseType) i.easingType, PgrTime - startT, endT - startT,
                        0, 1);
                    start ??= "";
                    end ??= "";
                    if (start.Contains("%P%") && end.Contains("%P%"))
                    {
                        float startNum = float.Parse(start.Replace("%P%", "").Trim());
                        float endNum = float.Parse(end.Replace("%P%", "").Trim());
                        float targetNum = startNum + (endNum - startNum) * now; // 1,-1 => 1 + (-1-1) * n
                        targetStr = (start.Contains(".") || end.Contains("."))
                            ? $"{targetNum:F3}"
                            : $"{Mathf.RoundToInt(targetNum)}";
                    }
                    else
                    {
                        if (end.Length >= start.Length)
                        {
                            int deltaLength = (end.Length - start.Length) * Mathf.RoundToInt(now);
                            int deltaPoint = start.Length + deltaLength;
                            if (end.StartsWith(start))
                                targetStr = end.Substring(0, deltaPoint);
                            else if (end.EndsWith(start))
                                targetStr = end.Substring(end.Length - deltaPoint - 1, deltaPoint);
                            else targetStr = now >= 0.5f ? end : start;
                        }
                        else
                        {
                            int deltaLength = (start.Length - end.Length) * Mathf.RoundToInt(now);
                            int deltaPoint = start.Length - deltaLength;
                            if (start.StartsWith(end))
                                targetStr = start.Substring(0, deltaPoint);
                            else if (start.EndsWith(end))
                                targetStr = start.Substring(deltaLength, deltaPoint);
                            else targetStr = now >= 0.5f ? end : start;
                        }
                    }
                }
                return targetStr;
            }

            ScaleEventScale = new Vector2(UpdateScaleX(), UpdateScaleY());

            var targetColor = UpdateColor();
        
            transform.localScale = ScaleEventScale * TargetScale;
        
            sr.color = targetColor;

            var str = UpdateCustomText();
            if (str != "ThisIsNULLText")
            {
                sr.color = Color.clear;
                customText.text = str;
                customText.transform.localScale = ScaleEventScale;
            }

            if (isUILine)
            {
                AttachUIManager.Instance.FillUIStates(Line.attachUI, UIMove, ScaleEventScale, RotateEventValue, targetColor.SetAlpha(AlphaEventValue));
            }
        }
    
        #endregion
        
        public void ResetScale()
        {
            StartCoroutine(ResetScaleCoroutine());
        }

        private IEnumerator ResetScaleCoroutine()
        {
            yield return new WaitForSeconds(0.2f);
            transform.localScale = TargetScale;
            if (GlobalSetting.is3D)
            {
                JudgeLineTopTransform.eulerAngles = new Vector3(30, 0, 0);
            }
        }

        public float CalculateNoteHeight(float time)
        {
            return (float) (CalculateNoteHeight_internal(time) - JudgeLineDistance);
        }

        private double CalculateNoteHeight_internal(float time)
        {
            var ret = 0d;
            foreach (var layer in Line.rpeLayers)
            {
                var e = GameUtils.GetEventFromCurrentTime(layer.speedEvents, time);
                if (e == null) continue;
                ret += e.floorPosition;
                ret += (e.value * 2 + (e.endValue - e.value) * (time - e.startTime) / (e.endTime - e.startTime)) *
                       (time - e.startTime) * .5;
            }
            return ret;
        }

        private void ArrangeFloorPosition()
        {
            var ret = 0d;
            foreach (var layer in Line.rpeLayers)
            {
                ret = 0d;
                foreach (var k in layer.speedEvents)
                {
                    k.floorPosition = ret;
                    ret += (k.value + k.endValue) * (k.endTime - k.startTime) * .5;
                }
            }

            foreach (var note in ProcessingNotes)
            {
                double t = CalculateNoteHeight_internal(note.time);
                note.floorPosition = t;
            }
        }

        public float GetNoteAlphaFactor(float dis)
        {
            if (Line.alphaControl.Count == 0)
                return 1;
            var i = GameUtils.GetEventFromCurrentTime(Line.alphaControl, dis);
            var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) i.easeType,
                dis - i.start, i.end - i.start,
                i.startValue, i.endValue);
            return a;
        }

        public float GetNoteXFactor(float dis)
        {
            if (Line.posControl.Count == 0)
                return 1;
            var i = GameUtils.GetEventFromCurrentTime(Line.posControl, dis);
            var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) i.easeType,
                dis - i.start, i.end - i.start,
                i.startValue, i.endValue);
            return a;
        }
        
        public float GetNoteYFactor(float dis)
        {
            if (Line.yControl.Count == 0)
                return 1;
            var i = GameUtils.GetEventFromCurrentTime(Line.yControl, dis);
            var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) i.easeType,
                dis - i.start, i.end - i.start,
                i.startValue, i.endValue);
            return a;
        }
        
        public float GetNoteSizeFactor(float dis)
        {
            if (Line.sizeControl.Count == 0)
                return 1;
            var i = GameUtils.GetEventFromCurrentTime(Line.sizeControl, dis);
            var a = EaseUtils.GetEaseResult((EaseUtils.EaseType) i.easeType,
                dis - i.start, i.end - i.start,
                i.startValue, i.endValue);
            return a;
        }
    }
}

[BurstCompile]
public struct UpdateNotesJob : IJobParallelFor
{
    public NativeArray<double> floorPosArray;
    public NativeArray<float> yOffsetArray;
    public NativeArray<float> noteSpeedArray;
    public double judgeLineDistance;
    public float speedFactor;

    public void Execute(int index)
    {
        float height = (float) (floorPosArray[index] - judgeLineDistance + yOffsetArray[index]);
        floorPosArray[index] = height * speedFactor * noteSpeedArray[index];
    }
}