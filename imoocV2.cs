//-----------------------------------------------------------------------
// <copyright file="imoocV2.cs" company="Bruce">
//     Copyright (c) Bruce. All rights reserved.
// </copyright>
// <author>Bruce He</author>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ImoocConsoleApp
{
    /// <summary>
    /// imooc网视频下载程序
    /// 调用方式：imoocV2.Run()
    /// </summary>
    public class imoocV2
    {
        /// <summary>
        /// 本地文件存储路径
        /// </summary>
        public static string localpath = @"C:\imooc\";

        /// <summary>
        /// 慕课网的URL 
        /// </summary>
        public static string learnurl = "http://www.imooc.com/learn/229";

        /// <summary>
        /// 当前的下载线程的集合
        /// </summary>
        public static Dictionary<int, string> curworks = new Dictionary<int, string>();

        /// <summary>
        /// 功能：入口方法
        /// 调用：imoocV2.Run()
        /// </summary>
        public static void Run()
        {
            try
            {
                if (!GetLocalPath())
                {
                    Console.WriteLine("获取本地路径失败！系统将退出。。。");
                    return;
                }
                if (!GetImoocLearnByID())
                {
                    Console.WriteLine("视频地址输入错误！系统将退出。。。");
                    return;
                }
                List<string> list = getClassVideoRequestUrl(LoadHtml(learnurl));
                list = list.Distinct().ToList();
                Console.WriteLine("共抽匹配到" + list.Count + "条视频路径\n\r开始下载视频\r\n**********************************************************\r\n");

                //Thread t = new Thread(new ThreadStart(Monitoring.MonitoringWork));
                //t.Start();

                int i = 0;
                foreach (string item in list)
                {
                    Videos videomodel = GetVideoUrl(item);
                    if (videomodel != null && videomodel.data.result.mpath.Count > 0)
                    {
                        //DownloadFile(videomodel.data.result.mpath[0], CreatePath(videomodel, 0));
                        HttpDownLoadHelper hdl = new HttpDownLoadHelper(videomodel.data.result.mpath[0], CreatePath(videomodel, 0, i), i);

                        //Thread t = new Thread(new ThreadStart(hdl.DownloadFile));
                        //t.Start();
                        ThreadPool.QueueUserWorkItem(hdl.DownloadFile, i);
                    }

                    lock (imoocV2.curworks)
                    {
                        if (!imoocV2.curworks.ContainsKey(i))
                            imoocV2.curworks.Add(i, "");
                    }
                    i++;
                }

                Monitoring m = new Monitoring();
                ThreadPool.QueueUserWorkItem(m.MonitoringWork, 0);

                //Console.WriteLine("下载完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 输入课程地址
        /// </summary>
        /// <returns> 用户输入的课程地址的检查结果 </returns>
        private static bool GetImoocLearnByID()
        {
            bool state = false;
            Console.WriteLine("请输入课程的编号（例如：" + localpath + "的编号为）：229");
            string curl = "";
            while (!state)
            {
                try
                {
                    curl = Console.ReadLine();
                    if (CheckUrlState(string.Format("http://www.imooc.com/learn/{0}", curl)) == HttpStatusCode.OK)
                    {
                        state = true;
                        learnurl = string.Format("http://www.imooc.com/learn/{0}", curl);
                    }
                    else
                    {
                        state = false;
                    }
                }
                catch
                {
                    state = false;
                    Console.WriteLine("输入的URL不符合要求，请重新输入：");
                }
            }
            return state;
        }

        /// <summary>
        /// 获取本地存储路径
        /// </summary>
        /// <returns>用户输入的本地存储路径的检查结果</returns>
        private static bool GetLocalPath()
        {
            bool state = false;
            Console.WriteLine("请输入下载文件的本级存放地址（默认地址是：" + localpath + "）：");
            string fpath = "";
            while (!state)
            {
                try
                {
                    fpath = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(fpath))
                    {
                        if (!Directory.Exists(localpath))
                        {
                            Directory.CreateDirectory(localpath);
                        }
                        state = true;
                        Console.WriteLine("下载文件的本级存放地址已经设置为默认地址：" + localpath);
                        break;
                    }
                    if (!Directory.Exists(fpath))
                    {
                        Directory.CreateDirectory(fpath);
                    }
                    state = true;
                    localpath = fpath[fpath.Length - 1] == '\\' ? fpath : fpath + "\\";
                }
                catch
                {
                    state = false;
                    Console.WriteLine("输入的地址错误，请重新输入：");
                }
            }
            return state;
        }

        /// <summary>
        /// 序列化Json
        /// </summary>
        /// <param name="json">Json Data String</param>
        /// <returns>Json Object</returns>
        private static Videos DealJson(string json)
        {
            Videos model = Newtonsoft.Json.JsonConvert.DeserializeObject<Videos>(json);
            return model;
        }

        /// <summary>
        /// 获取请求视频URL的URL地址
        /// </summary>
        /// <param name="html">当前页面的HTML字符串</param>
        /// <returns>视频地址的泛型列表</returns>
        private static List<string> getClassVideoRequestUrl(string html)
        {
            List<String> list = new List<string>();

            //Regex reg = new Regex("href=\"/video/(\\d+)\"");
            Regex reg = new Regex("/video/(\\d+)[\"|']");
            MatchCollection result = reg.Matches(html);
            foreach (Match item in result)
            {
                string[] strs = item.Value.ToString().Split('/');
                string id = strs[strs.Length - 1];

                list.Add(string.Format("http://www.imooc.com/course/ajaxmediainfo/?mid={0}&mode=flash", id.Remove(id.Length - 1)));
                Console.WriteLine(item.Value);
            }
            return list;
        }

        /// <summary>
        /// 根据URL分析视频URL视频有三种类型（根据清晰度）
        /// </summary>
        /// <param name="url">视频列表页面的URL</param>
        /// <returns>Json Object</returns>
        private static Videos GetVideoUrl(string url)
        {
            string html = LoadHtml(url);
            Videos model = DealJson(html);
            return model;
        }

        /// <summary>
        /// 下载HTML页
        /// </summary>
        /// <param name="url">要下载的HTML页面的URL</param>
        /// <returns>页面的HTML字符串</returns>
        private static string LoadHtml(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Proxy = null;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        /// <summary>
        /// 检查当前地址是否存在
        /// </summary>
        /// <param name="url">要检查的页面的URL</param>
        /// <returns>当前页面的http请求响应结果</returns>
        private static HttpStatusCode CheckUrlState(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Proxy = null;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //Stream myResponseStream = response.GetResponseStream();
            //StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            HttpStatusCode retstate = response.StatusCode;

            //myStreamReader.Close();
            //myResponseStream.Close();
            return retstate;
        }

        /// <summary>
        /// 拼接本地路径
        /// </summary>
        /// <param name="model">当前视频对象实体</param>
        /// <param name="type">要下载的视频的清晰度类型</param>
        /// <param name="num">当前下载的顺序编号</param>
        /// <returns>视频的本地存储路径</returns>
        private static string CreatePath(Videos model, int type, int num)
        {
            //http://v1.mukewang.com/284f9ef8-5812-44ca-888f-57ba3affdc4e/L.mp4
            string name = "";
            if (type == 0)
            {
                name = localpath + num.ToString() + "_" + model.data.result.name + "_H.mp4";
            }
            else if (type == 1)
            {
                name = localpath + num.ToString() + "_" + model.data.result.name + "_M.mp4";
            }
            else if (type == 2)
            {
                name = localpath + num.ToString() + "_" + model.data.result.name + "_L.mp4";
            }
            else
            {
                string[] strs = model.data.result.mpath[1].Split('/');
                name = localpath + model.data.result.name + "_" + strs[strs.Length - 2] + "_" + strs[strs.Length - 1];
            }
            return name;
        }
    }

    #region   数据实体
    /// <summary>
    /// video class
    /// </summary>
    public class Videos
    {
        /// <summary>
        /// 请求结果代码
        /// </summary>
        public int result { get; set; }

        /// <summary>
        /// 返回的数据
        /// </summary>
        public Data data { get; set; }

        /// <summary>
        /// 请求结果信息
        /// </summary>
        public string msg { get; set; }
    }

    public class Option
    {
        public string id { get; set; }
        public string name { get; set; }
        public string tip { get; set; }
        public string is_answer { get; set; }
    }

    public class Content
    {
        public string name { get; set; }
        public IList<Option> options { get; set; }
    }

    public class Practise
    {
        public int id { get; set; }
        public string type { get; set; }
        public int timepoint { get; set; }
        public string status { get; set; }
        public string eid { get; set; }
        public int skip { get; set; }
        public Content content { get; set; }
    }

    public class Result
    {
        public int mid { get; set; }
        public IList<string> mpath { get; set; }
        public string cpid { get; set; }
        public string name { get; set; }
        public int time { get; set; }
        public IList<Practise> practise { get; set; }
    }

    public class Data
    {
        public Result result { get; set; }
    }

    public class SufeiNet_Test
    {
        public int result { get; set; }
        public Data data { get; set; }
        public string msg { get; set; }
    }
    #endregion

    #region 工具类
    /// <summary>
    /// 视频文件Http下载类
    /// </summary>
    public class HttpDownLoadHelper
    {
        public string url { get; set; }
        public string path { get; set; }
        private int workid = 0;
        private static object _syncObj = new object();

        public HttpDownLoadHelper(string url, string path, int workid)
        {
            this.url = url;
            this.path = path;
            this.workid = workid;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="obj">为方便线程使用而添加的参数</param>
        public void DownloadFile(object obj)
        {
            try
            {
                //workid = Convert.ToInt32(obj);
                double filesize = 0;

                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();

                //创建本地文件写入流
                Stream stream = new FileStream(path, FileMode.Create);
                byte[] bArr = new byte[10240000];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    stream.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                    filesize++;

                    lock (_syncObj)
                    {
                        if (imoocV2.curworks.ContainsKey(workid))
                        {
                            imoocV2.curworks[workid] = workid.ToString() + "下载中--->" + Convert.ToInt32(stream.Length / 1024) + "K";
                        }
                    }
                    Thread.Sleep(1000);
                }
                stream.Close();
                responseStream.Close();
                imoocV2.curworks.Remove(workid);
                Console.WriteLine("\n\r----->下载成功\n\r" + path + "\r\n");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("\n\r----->下载出错！！！【^o^】\n\r" + path + "\r\n" + ex.Message + "\r\n");
                int workerThreads = 0;
                int maxWordThreads = 0;
                int compleThreads = 0;
                ThreadPool.GetAvailableThreads(out workerThreads, out compleThreads);
                ThreadPool.GetMaxThreads(out maxWordThreads, out compleThreads);
                if (maxWordThreads - workerThreads > 2)
                {
                    //Console.WriteLine("----->等待 重新下载");
                    Thread.Sleep(2000);
                    DownloadFile(0);
                }
                else
                {
                    imoocV2.curworks.Remove(workid);
                    Console.WriteLine("\n\r----->下载出错！！！【^o^】\n\r" + path + "\r\n" + ex.Message + "\r\n");
                }
            }
        }
    }

    /// <summary>
    /// 下载进度监控类
    /// </summary>
    public class Monitoring
    {
        private static object _syncObj = new object();
        public void MonitoringWork()
        {
            bool isqempty = false;
            while (!isqempty)
            {
                lock (_syncObj)
                {
                    if (imoocV2.curworks.Values.Count > 0)
                    {
                        foreach (var item in imoocV2.curworks)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Value))
                                Console.WriteLine(item.Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine("下载完成！");
                        Program.isStop = true;
                        isqempty = true;
                    }
                }
                Thread.Sleep(3000);
            }
        }
        public void MonitoringWork(object obj)
        {
            bool isqempty = false;
            while (!isqempty)
            {
                lock (_syncObj)
                {
                    if (imoocV2.curworks.Values.Count > 0)
                    {
                        for (int i = 0; i < imoocV2.curworks.Values.Count; i++)
                        {
                            if (!string.IsNullOrWhiteSpace(imoocV2.curworks.Values.ToList()[i]))
                                Console.WriteLine(imoocV2.curworks.Values.ToList()[i]);
                        }
                    }
                    else
                    {
                        Console.WriteLine("下载完成！");
                        Program.isStop = true;
                        isqempty = true;
                    }
                }
                Thread.Sleep(3000);
            }
        }
    }
    #endregion
}
