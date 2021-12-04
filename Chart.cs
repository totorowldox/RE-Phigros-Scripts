using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public List<note> notesAbove = new List<note> ();
    public List<note> notesBelow = new List<note> ();
    public List<judgeLineEvent> judgeLineDisappearEvents = new List<judgeLineEvent>();
    public List<judgeLineEvent> judgeLineMoveEvents = new List<judgeLineEvent>();
    public List<judgeLineEvent> judgeLineRotateEvents = new List<judgeLineEvent>();
}
[System.Serializable]
public class judgeLineSpeedEvent
{
    public int startTime = 0;
    public int endTime = 0;
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
    public float speed = 0;
    public double floorPosition = 0;
}
[System.Serializable]
public class judgeLineEvent
{
    public int startTime = 0;
    public int endTime = 0;
    public float start = 0;
    public float end = 0;
    public float start2 = 0;
    public float end2 = 0;
}

