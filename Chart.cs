using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[System.Serializable]
public class Chart
{
    public int formatVersion = 0;
    public float offset = 0;
    public int numOfNotes = 0;
    public List<judgeLine> judgeLineList = new List<judgeLine>();

}
[System.Serializable]
public class judgeLine
{
    public int numOfNotes = 0;
    public int numOfNotesAbove = 0;
    public int numOfNotesBelow = 0;
    public float bpm = -1;
    public List<judgeLineSpeedEvent> speedEvents = new List<judgeLineSpeedEvent>();
    public List<note> notesAbove = new List<note>();
    public List<note> notesBelow = new List<note>();
    public List<judgeLineEvent> judgeLineDisappearEvents = new List<judgeLineEvent>();
    public List<judgeLineEvent> judgeLineMoveEvents = new List<judgeLineEvent>();
    public List<judgeLineEvent> judgeLineRotateEvents = new List<judgeLineEvent>();

    public void PushNote(int type, bool isAbove, float time, float posX, double speed, double floorPos, bool isFake = false, float holdTime = 0)
    {
        if (isAbove)
        {
            notesAbove.Add(new note
            {
                type = type,
                time = time,
                positionX = posX,
                holdTime = holdTime,
                speed = Double.IsNaN(speed) ? 1 : speed,
                floorPosition = floorPos,
                isFake = isFake
            });
            if (!isFake)
                numOfNotesAbove++;
        }
        else
        {
            notesBelow.Add(new note
            {
                type = type,
                time = time,
                positionX = posX,
                holdTime = holdTime,
                speed = Double.IsNaN(speed) ? 1 : speed,
                floorPosition = floorPos,
                isFake = isFake
            });
            if (!isFake)
                numOfNotesBelow++;
        }
        if (!isFake)
            numOfNotes++;
    }

    public void PushEvent(int type, int easeType, float st, float et, float v11, float v12, float v21, float v22)
    {
        switch(type)
        {
            case 1: //speed
                speedEvents.Add(new judgeLineSpeedEvent
                {
                    startTime = st,
                    endTime = et,//以后处理
                    value = v11,
                });
                break;
            case 2: //alpha
                judgeLineDisappearEvents.Add(new judgeLineEvent
                {
                    startTime = st,
                    endTime = et,
                    start = v11,
                    end = v12,
                    easeType = easeType
                });
                break;
            case 3: //move
                judgeLineMoveEvents.Add(new judgeLineEvent
                {
                    startTime = st,
                    endTime = et,
                    start = v11,
                    end = v12,
                    start2 = v21,
                    end2 = v22,
                    easeType = easeType
                });
                break;
            case 4: //rotate
                judgeLineRotateEvents.Add(new judgeLineEvent
                {
                    startTime = st,
                    endTime = et,
                    start = v11,
                    end = v12,
                    start2 = v21,
                    end2 = v22,
                    easeType = easeType
                });
                break;
        }
    }
}
[System.Serializable]
public class judgeLineSpeedEvent
{
    public float startTime = 0;
    public float endTime = 0;
    public double floorPosition = 0;
    public float value = 0;
}
[System.Serializable]
public class note
{
    public int type = 0;
    public float time = 0;
    public float positionX = 0;
    public float holdTime = 0;
    public double speed = 0;
    public double floorPosition = 0;

    public bool isFake = false;
}
[System.Serializable]
public class judgeLineEvent
{
    public float startTime = 0;
    public float endTime = 0;
    public float start = 0;
    public float end = 0;
    public float start2 = 0;
    public float end2 = 0;

    public int easeType = 1;
}

[System.Serializable]
public class pec2jsonRet
{
    public Chart data = new Chart();
    public List<string> messages = new List<string>();
}
