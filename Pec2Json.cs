using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lean.Transition.Method;
using UnityEngine;

public static class Pec2Json
{

    public static Chart Convert123(string pec)
    {
        string content = Post("https://pec2json.lchzh.xyz/demos/requests", pec);
        Debug.Log("return content:" + content);
        
        pec2jsonRet ret = JsonUtility.FromJson<pec2jsonRet>(content);
        if (ret.messages.Count != 0)
            Debug.LogWarning(ret.messages);
        return ret.data;
    }
    
        private class BpmEvent
        {
            public float start;
            public float end;
            public float bpm;
            public BpmEvent(float b, float s)
            {
                bpm = b;
                start = s;
                end = 1e9f;
            }
        }

        public static Chart Chart123(string chart)
        {
            string[] rawChart = chart.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            Chart retChart = new Chart();
            retChart.formatVersion = 114514;
            BpmEvent[] bpms = new BpmEvent[0];
            if (!double.IsNaN(Convert.ToDouble(rawChart[0].Trim())))
                retChart.offset = ((float)Convert.ToDouble(rawChart[0]) / 1000f - 0.175f);

            int GetLine(int lineId)
            {
                while (retChart.judgeLineList.Count <= lineId)
                {
                    retChart.judgeLineList.Add(new judgeLine());
                }
                return lineId;
            }

            for (int i = 0; i < rawChart.Length; i++)
            {
                string t = rawChart[i].Trim();
                if (t.Length == 0)
                {
                    continue;
                }
                if(t[0] == 'b')//读bpm
                {
                    //bpms.Append());
                    var a = bpms.ToList();
                    a.Add(new BpmEvent(ToFloat(t.Split(' ')[2]), ToFloat(t.Split(' ')[1])));
                    bpms = a.ToArray();
                    if (bpms.Length >= 2)
                    {
                        bpms[^2].end = bpms[^1].start;
                    }
                }
                else if (t[0] == 'n') //读notes
                {
                    string[] splitted = t.Split(' ');
                    if (t[1] != '2')
                    {
                        int lineId = GetLine(ToInt(splitted[1]));
                        float time = RecalcTime(bpms, ToFloat(splitted[2]));
                        float posX = ToFloat(splitted[3]) / 115.2f;
                        bool isAbove = ToInt(splitted[4]) == 1;
                        bool isFake = ToInt(splitted[5]) == 1;
                        float speed = ToFloat(rawChart[i + 1].Trim().Split(' ')[1]);
                        switch (t[1])
                        {
                            case '1':
                                retChart.judgeLineList[lineId].PushNote(1, isAbove, time, posX, speed, 0, isFake);
                                break;
                            case '3':
                                retChart.judgeLineList[lineId].PushNote(4, isAbove, time, posX, speed, 0, isFake);
                                break;
                            case '4':
                                retChart.judgeLineList[lineId].PushNote(2, isAbove, time, posX, speed, 0, isFake);
                                break;
                        }
                    }
                    else
                    {
                        int lineId = GetLine(ToInt(splitted[1]));
                        float time = RecalcTime(bpms, ToFloat(splitted[2]));
                        float timeEnd = RecalcTime(bpms, ToFloat(splitted[3]));
                        float posX = ToFloat(splitted[4]) / 115.2f;
                        float speed = ToFloat(rawChart[i + 1].Trim().Split(' ')[1]);
                        bool isAbove = ToInt(splitted[5]) == 1;
                        bool isFake = ToInt(splitted[6]) == 1;
                        retChart.judgeLineList[lineId].PushNote(3, isAbove, time, posX, speed, 0, isFake, timeEnd - time);
                    }
                    i += 2;
                }
                else if (t[0] == 'c')//读line事件
                {
                    string[] splitted = t.Split(' ');
                    int lineId = GetLine(ToInt(splitted[1]));
                    if ("vpda".Contains(t[1]))
                    {
                        float time = RecalcTime(bpms, ToFloat(splitted[2]));
                        float v11 = ToFloat(splitted[3]);
                        switch (t[1])
                        {
                            case 'v':
                                retChart.judgeLineList[lineId].PushEvent(1, 1, time, time, v11 / 7.0f, v11 / 7.0f, 0, 0);
                                break;
                            case 'p':
                                float v12 = ToFloat(splitted[4]);
                                retChart.judgeLineList[lineId].PushEvent(3, 1, time, time, v11 / 2048f, v11 / 2048f, v12 / 1400f, v12 /1400f);
                                break;
                            case 'd':
                                retChart.judgeLineList[lineId].PushEvent(4, 1, time, time, -v11, -v11, 0, 0);
                                break;
                            case 'a':
                                int temp = ToInt(splitted[3]);
                                retChart.judgeLineList[lineId].PushEvent(2, 1, time, time, temp / 255f, temp / 255f, 0, 0);
                                break;
                        }
                    }
                    else
                    {
                        float startTime = RecalcTime(bpms, ToFloat(splitted[2]));
                        float endTime = RecalcTime(bpms, ToFloat(splitted[3]));
                        float v11 = ToFloat(splitted[4]);
                        if (t[1] == 'm')
                        {
                            float v12 = ToFloat(splitted[5]);
                            int easeType = ToInt(splitted[6]);
                            float orgv1 = 0f, orgv2 = 0f;
                            int temp = retChart.judgeLineList[lineId].judgeLineMoveEvents.Count;
                            if (temp > 0)
                            {
                                orgv1 = retChart.judgeLineList[lineId].judgeLineMoveEvents[temp - 1].end;
                                orgv2 = retChart.judgeLineList[lineId].judgeLineMoveEvents[temp - 1].end2;
                            }
                            retChart.judgeLineList[lineId].PushEvent(3, easeType, startTime, endTime, orgv1, v11 / 2048f, orgv2, v12 / 1400f);
                        }
                        else
                        {
                            float orgv = 0;
                            int temp;
                            int easeType = 1;
                            switch (t[1])
                            {
                                case 'r':
                                    easeType = ToInt(splitted[5]);
                                    temp = retChart.judgeLineList[lineId].judgeLineRotateEvents.Count;
                                    if (temp > 0)
                                    {
                                        orgv = retChart.judgeLineList[lineId].judgeLineRotateEvents[temp - 1].end;
                                    }
                                    retChart.judgeLineList[lineId].PushEvent(4, easeType, startTime, endTime, orgv, -v11, 0, 0);
                                    break;
                                case 'f':
                                    temp = retChart.judgeLineList[lineId].judgeLineDisappearEvents.Count;
                                    if (temp > 0)
                                    {
                                        orgv = retChart.judgeLineList[lineId].judgeLineDisappearEvents[temp - 1].end;
                                    }
                                    retChart.judgeLineList[lineId].PushEvent(2, easeType, startTime, endTime, orgv, ToInt(splitted[4]) / 255f, 0, 0);
                                    break;
                            }
                        }
                    }
                }
            }

            //排序
            foreach(var line in retChart.judgeLineList)
            {
                retChart.numOfNotes += line.numOfNotes;
                line.speedEvents.Sort((a, b) =>
                {
                    return Math.Abs(a.startTime - b.startTime) > .000001f ? a.startTime.CompareTo(b.startTime) : a.endTime.CompareTo(b.endTime);
                });
                line.judgeLineDisappearEvents.Sort((a, b) =>
                {
                    return Math.Abs(a.startTime - b.startTime) > .000001f ? a.startTime.CompareTo(b.startTime) : a.endTime.CompareTo(b.endTime);
                });
                line.judgeLineMoveEvents.Sort((a, b) =>
                {
                    return Math.Abs(a.startTime - b.startTime) > .000001f ? a.startTime.CompareTo(b.startTime) : a.endTime.CompareTo(b.endTime);
                });
                line.judgeLineRotateEvents.Sort((a, b) =>
                {
                    return Math.Abs(a.startTime - b.startTime) > .000001f ? a.startTime.CompareTo(b.startTime) : a.endTime.CompareTo(b.endTime);
                });

                float a = 0, b = 0;
                
                foreach (var e in line.judgeLineDisappearEvents)
                {
                    e.start = a;
                    a = e.end;
                }
                a = 0;
                foreach (var e in line.judgeLineMoveEvents)
                {
                    e.start = a;
                    a = e.end;
                    e.start2 = b;
                    b = e.end2;
                }
                a = 0;
                foreach (var e in line.judgeLineRotateEvents)
                {
                    e.start = a;
                    a = e.end;
                }
                
                line.notesAbove.Sort((a, b) =>
                {
                    return a.time.CompareTo(b.time);
                });
                line.notesBelow.Sort((a, b) =>
                {
                    return a.time.CompareTo(b.time);
                });
            }

            //规范floorPosition
            foreach(var line in retChart.judgeLineList)
            {
                var s = line.speedEvents;
                var y = 0f;
                for (var j = 0; j < s.Count; j++)
                {
                    s[j].endTime = (float)(j < s.Count - 1 ? s[j + 1].startTime : 1e9);
                    if (s[j].startTime < 0) s[j].startTime = 0;
                    s[j].floorPosition = y;
                    y = y + (s[j].endTime - s[j].startTime) * s[j].value;
                }

                line.speedEvents = s;

                foreach (var j in line.notesAbove)
                {
                    var qwqwq = 0d;
                    var qwqwq2 = 0f;
                    var qwqwq3 = 0d;
                    foreach (var k in line.speedEvents) {
                        if (j.time > k.endTime) continue;
                        if (j.time < k.startTime) break;
                        qwqwq = k.floorPosition;
                        qwqwq2 = k.value;
                        qwqwq3 = j.time - k.startTime;
                    }
                    j.floorPosition = qwqwq + qwqwq2 * qwqwq3;
                    if (j.type == 3 && qwqwq2 != 0) j.speed *= qwqwq2;
                }

                foreach (var j in line.notesBelow)
                {
                    var qwqwq = 0d;
                    var qwqwq2 = 0f;
                    var qwqwq3 = 0d;
                    foreach (var k in line.speedEvents)
                    {
                        if (j.time > k.endTime) continue;
                        if (j.time < k.startTime) break;
                        qwqwq = k.floorPosition;
                        qwqwq2 = k.value;
                        qwqwq3 = j.time - k.startTime;
                    }
                    j.floorPosition = qwqwq + qwqwq2 * qwqwq3;
                    if (j.type == 3 && qwqwq2 != 0) j.speed *= qwqwq2;
                }
            }

            return retChart;
        }


        private static float RecalcTime(BpmEvent[] bpms, float time)
        {
            var timePhi = 0f;
            foreach (var i in bpms)
            {
                if (time > i.end)
                {
                    timePhi += (i.end - i.start) * (60f / i.bpm);
                }
                else if (time >= i.start)
                {
                    timePhi += (time - i.start) * (60f / i.bpm);
                }
            }
            return timePhi;
        }

        private static float ToFloat(string s) => (float)Convert.ToDouble(s);

        private static int ToInt(string s) => Convert.ToInt32(s);
        
    public static string Post(string url, string content)
    {
        /*string result = "";
        Uri myUri = new Uri(url);
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(myUri);
        req.Method = "post";
        req.ContentType = "application/x-www-form-urlencoded";

        byte[] data = Encoding.UTF8.GetBytes(content.Replace("&", "%26"));
        req.ContentLength = data.Length;
        using (Stream reqStream = req.GetRequestStream())
        {
            reqStream.Write(data, 0, data.Length);
            reqStream.Close();
        }

        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
        Stream stream = resp.GetResponseStream();

        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            result = reader.ReadToEnd();
        }
        return result;*/
        WWWForm form = new WWWForm();
        form.AddField("pec", content);
        WWW www = new WWW(url, form);
        StreamWriter sw = new StreamWriter(Path.Combine(PlayerPrefs.GetString("chartFolderPath", ""), "request.tmp"));
        sw.Write(content);
        sw.Close();
        
        while(!www.isDone){}

        return www.text;
    }
}
