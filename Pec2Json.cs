using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

public static class Pec2Json
{

    public static Chart Convert(string pec)
    {
        string content = Post("https://pec2json.lchzh3473.workers.dev/demos/requests", $"pec={pec}");
        Debug.Log("return content:" + content);
        
        pec2jsonRet ret = (pec2jsonRet)JsonUtility.FromJson(content, typeof(pec2jsonRet));
        if (ret.messages.Count != 0)
            Debug.LogWarning(ret.messages);
        return ret.data;
    }
    /// <summary>  
    /// 指定Post地址使用Get 方式获取全部字符串  
    /// </summary>  
    /// <param name="url">请求后台地址</param>  
    /// <param name="content">Post提交数据内容(utf-8编码的)</param>  
    /// <returns>结果</returns>  
    public static string Post(string url, string content)
    {
        //申明一个容器result接收数据
        string result = "";
        //首先创建一个HttpWebRequest,申明传输方式POST
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Method = "post";
        req.ContentType = "application/x-www-form-urlencoded";

        //添加POST参数
        byte[] data = Encoding.UTF8.GetBytes(content.Replace("&", "%26"));
        req.ContentLength = data.Length;
        using (Stream reqStream = req.GetRequestStream())
        {
            reqStream.Write(data, 0, data.Length);
            reqStream.Close();
        }

        //申明一个容器resp接收返回数据
        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
        Stream stream = resp.GetResponseStream();
        //获取响应内容  
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            result = reader.ReadToEnd();
        }
        return result;
    }
}
