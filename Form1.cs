using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mp4baDown
{
    public delegate string HtmlDelegate(string url);  

    public partial class Form1 : Form
    {
        int oldnum = 100;  //重复读取上限值
        bool start = true;
        int moviepage = 1;
        int maxpage =0; //最大页数  会自动更新
        string connectString = "Data Source=.;Initial Catalog=SpiderData;Integrated Security=True";


        public Form1()
        {

            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            start = true;
            maxpage = GetMaxPages();
            listBox1.Items.Add("数据共" + maxpage.ToString() + "页...");
            if (maxpage != 0)
            {
                GetVideoList();
            }
            else
            {
                MessageBox.Show("页码获取错误");
            }
            //HtmlDelegate HtmlData = new HtmlDelegate(GetHtml);
            //IAsyncResult async = HtmlData.BeginInvoke("http://www.mp4ba.com/show.php?hash=a79b962976cfd0d4383d2c8a1ee6ff7ee131e4de", TestCallback, null);
        }

        public void GetVideoList()
        {
            if (!start)
            {
                return;
            }
            try
            {

                HtmlDelegate PageData = new HtmlDelegate(GetHtml);
                IAsyncResult PageAsync = PageData.BeginInvoke("http://www.mp4ba.com/index.php?page=" + (moviepage++).ToString(), AsyncPages, null);
                    
                //return;
                //string InfoBody = "";
                //string ret = GetHtml("http://www.mp4ba.com/index.php?page=" + (moviepage++).ToString());
                //Regex pattern = new Regex(@"data_list""\>([\w\W]+?)\<\/tbody");
                //Match matchMode = pattern.Match(ret);//匹配整个信息段
                //if (matchMode.Success)
                //{
                //    InfoBody = matchMode.Groups[1].Value;
                //}
                //pattern = new Regex(@"tr class=""alt\d""\>([\w\W]+?)\<\/tr");
                //MatchCollection matchsMade = pattern.Matches(InfoBody);//匹配每条数据
                //foreach (Match item in matchsMade)
                //{
                //    pattern = new Regex(@"href=""(show[\w\W]+?)""");
                //    matchMode = pattern.Match(item.Groups[1].Value);  //内容页链接
                //    if (matchMode.Success)
                //    {
                //        HtmlDelegate HtmlData = new HtmlDelegate(GetHtml);
                //        IAsyncResult async = HtmlData.BeginInvoke("http://www.mp4ba.com/" + matchMode.Groups[1].Value, TestCallback, null);
                //    }
                //    else
                //    {
                //        Logs("匹配内容链接", -1, "匹配内容链接失败", item.Groups[1].Value);
                //    }

                //}
            }
            catch (Exception e)
            {
                Logs("获取电影列表", -1, "处理错误 " + e.Message,moviepage.ToString());
            }
        }//从mp4ba上获取电影列表

        private string GetHtml(string url)
        {
            try
            {
                string ret = string.Empty;

                HttpWebRequest request = null;
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    //对服务端证书进行有效性校验（非第三方权威机构颁发的证书，如自己生成的，不进行验证，这里返回true）
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
                    request.ProtocolVersion = HttpVersion.Version10;    //http版本，默认是1.1,这里设置为1.0
                }
                else
                {
                    request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                }

                request.Method = "GET";
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.UTF8);
                ret = sr.ReadToEnd();
                //MessageBox.Show(ret.Length.ToString());
                //response.Close();
                return ret;
            }
            catch(Exception e)
            {
                Logs("获取网页", -1, "读取错误 "+e.Message, url);
                return "";
            }
        }//获取网页内容

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            return false;
        }//https验证函数

        public void Logs(string act, int code, string des = "", string det = "")
        {

            SqlConnection sqlCnt = new SqlConnection(connectString);
            sqlCnt.Open();
            SqlCommand cmd = sqlCnt.CreateCommand();              //创建SqlCommand对象
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO Logs (AppName,Action,AddTime,TypeCode,Description,Details) VALUES ('Mp4ba',@Para1,@Para2,@Para3,@Para4,@Para5)";   //sql语句
            cmd.Parameters.Add("@Para1", SqlDbType.VarChar);
            cmd.Parameters.Add("@Para2", SqlDbType.DateTime);
            cmd.Parameters.Add("@Para3", SqlDbType.Int);
            cmd.Parameters.Add("@Para4", SqlDbType.VarChar);
            cmd.Parameters.Add("@Para5", SqlDbType.VarChar);
            //给参数sql语句的参数赋值
            cmd.Parameters["@Para1"].Value = act;
            cmd.Parameters["@Para2"].Value = DateTime.Now;
            cmd.Parameters["@Para3"].Value = code;
            cmd.Parameters["@Para4"].Value = des;
            cmd.Parameters["@Para5"].Value = det;
            cmd.ExecuteReader();
            sqlCnt.Close();
            sqlCnt.Dispose();           // 释放数据库连接对象
        }

        public void WriteData(string pt, string mt, string mn, string ms , int sn, int dn, int fn, string du, string mi, string su,string mu,int uh,string bu="",string bp="",string re="")
        {
            SqlConnection sqlCnt = new SqlConnection(connectString);
            sqlCnt.Open();
            SqlCommand cmd = sqlCnt.CreateCommand();              //创建SqlCommand对象
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "insert into Mp4baData(PostTime,MovieType,MovieName,MovieSize,SNum,DNum,FNum,AddTime,DetailUrl,MovieIntroduction,SeedUrl,MagnetUrl,UrlHealth,BaiduYunUrl,BaiduYunPwd,Remarks) values (@PostTime,@MovieType,@MovieName,@MovieSize,@SNum,@DNum,@FNum,@AddTime,@DetailUrl,@MovieIntroduction,@SeedUrl,@MagnetUrl,@UrlHealth,@BaiduYunUrl,@BaiduYunPwd,@Remarks)";   //sql语句
            cmd.Parameters.Add("@PostTime", SqlDbType.VarChar);
			cmd.Parameters.Add("@MovieType", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@MovieName", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@MovieSize", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@SNum", SqlDbType.Int,4);
			cmd.Parameters.Add("@DNum", SqlDbType.Int,4);
			cmd.Parameters.Add("@FNum", SqlDbType.Int,4);
			cmd.Parameters.Add("@AddTime", SqlDbType.DateTime);
			cmd.Parameters.Add("@DetailUrl", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@MovieIntroduction", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@SeedUrl", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@MagnetUrl", SqlDbType.VarChar,-1);
			cmd.Parameters.Add("@UrlHealth", SqlDbType.Int,4);
			cmd.Parameters.Add("@BaiduYunUrl", SqlDbType.VarChar,-1);
            cmd.Parameters.Add("@BaiduYunPwd", SqlDbType.VarChar, -1);
			cmd.Parameters.Add("@Remarks", SqlDbType.VarChar,-1);
            cmd.Parameters["@PostTime"].Value = pt;
            cmd.Parameters["@MovieType"].Value = mt;
            cmd.Parameters["@MovieName"].Value = mn;
            cmd.Parameters["@MovieSize"].Value = ms;
            cmd.Parameters["@SNum"].Value = sn;
            cmd.Parameters["@DNum"].Value = dn;
            cmd.Parameters["@FNum"].Value = fn;
            cmd.Parameters["@AddTime"].Value = DateTime.Now;
            cmd.Parameters["@DetailUrl"].Value = du;
            cmd.Parameters["@MovieIntroduction"].Value = mi;
            cmd.Parameters["@SeedUrl"].Value = su;
            cmd.Parameters["@MagnetUrl"].Value = mu;
            cmd.Parameters["@UrlHealth"].Value = uh;
            cmd.Parameters["@BaiduYunUrl"].Value = bu;
            cmd.Parameters["@BaiduYunPwd"].Value = bp;
            cmd.Parameters["@Remarks"].Value = re;
            cmd.ExecuteReader();
            sqlCnt.Close();
            sqlCnt.Dispose();           // 释放数据库连接对象

        }

        public void TestCallback(IAsyncResult data)
        {
            try
            {
                AsyncResult ar = (AsyncResult)data;
                HtmlDelegate del = (HtmlDelegate)ar.AsyncDelegate;
                string res = del.EndInvoke(ar);

                if ( res==null || res=="" )
                {
                    Logs("异步处理函数", -1, "详细信息页面返回为空");
                }
                string posttime = "", name = "", type = "", size = "", detailurl = "", introduction = "", seedurl = "", magneturl = "";
                int snum = -1, dnum = -1, fnum = -1, urlhealth = -1;

                Regex pattern = new Regex(@"<p>发布时间: ([\w\W]+?)</p>");
                Match matchMode = pattern.Match(res);//匹配大小
                posttime = matchMode.Groups[1].Value;
                // <a href="index.php\?sort_id=5">([\w\W]+?)</a>([\w\W]+?)</div>
                pattern = new Regex(@"<div class=""location"">[\w\W]+?<a href=""index.php\?sort_id=[\w\W]*?"">([\w\W]+?)</a>([\w\W]+?)</div>");
                matchMode = pattern.Match(res);  //内容页链接
                name = matchMode.Groups[2].Value.Replace("&raquo;", "").Replace(" ", "");
                type = matchMode.Groups[1].Value;
                pattern = new Regex(@"<p>分享情况: 做种 ([\w\W]+?), 下载 ([\w\W]+?), 完成 ([\w\W]+?)</p>");
                matchMode = pattern.Match(res);  //内容页链接
                try
                {
                    snum = Convert.ToInt32(matchMode.Groups[1].Value);
                    dnum = Convert.ToInt32(matchMode.Groups[2].Value);
                    fnum = Convert.ToInt32(matchMode.Groups[3].Value);
                }
                catch { }
                pattern = new Regex(@"文件总大小：([\w\W]+?)</span>");
                matchMode = pattern.Match(res);
                size = matchMode.Groups[1].Value;

                pattern = new Regex(@"magnet:\?xt=urn:btih:([\w\W]+?)&tr=");
                matchMode = pattern.Match(res);
                detailurl = "http://www.mp4ba.com/show.php?hash=" + matchMode.Groups[1].Value;

                Regex DPattern = new Regex(@"magnet"" href=""([\w\W]+?)""");
                Match DMatch = DPattern.Match(res);
                if (DMatch.Success)
                {
                    magneturl = DMatch.Groups[1].Value;
                }
                DPattern = new Regex(@"download"" href=""([\w\W]+?)""");
                DMatch = DPattern.Match(res);
                if (DMatch.Success)
                {
                    seedurl = DMatch.Groups[1].Value;
                }
                DPattern = new Regex(@"<img src=""images/health/([\w\W]+?).png");
                DMatch = DPattern.Match(res);
                if (DMatch.Success)
                {
                    try
                    {
                        urlhealth = Convert.ToInt32(DMatch.Groups[1].Value);
                    }
                    catch { }
                }
                DPattern = new Regex(@"(<div class=""intro"">[\w\W]+?)<div class=""clear""");
                DMatch = DPattern.Match(res);
                if (DMatch.Success)
                {
                    introduction = DMatch.Groups[1].Value;
                }
                if (name != "" && type != "")
                {
                    WriteData(posttime, type, name, size, snum, dnum, fnum, detailurl, introduction, seedurl, magneturl, urlhealth);
                    listBox1.Items.Add(name + " 已完成 " + posttime);
                }
                else
                {
                    Logs("解析异常", -1, "关键字解析异常...in " + detailurl, res);
                    listBox1.Items.Add(res + " 解析异常 " + posttime);
                }

            }
            catch(Exception e)
            {
                Logs("异步处理函数", -1, "关键字匹配错误"+e.Message,moviepage.ToString());
            }
        }  //异步响应函数

        public void AsyncPages(IAsyncResult data)
        {
            try
            {
                AsyncResult ar = (AsyncResult)data;
                HtmlDelegate del = (HtmlDelegate)ar.AsyncDelegate;
                string res = del.EndInvoke(ar);
                string InfoBody = "";
                Regex pattern = new Regex(@"data_list""\>([\w\W]+?)\<\/tbody");
                Match matchMode = pattern.Match(res);//匹配整个信息段
                if (matchMode.Success)
                {
                    InfoBody = matchMode.Groups[1].Value;
                }
                pattern = new Regex(@"tr class=""alt\d""\>([\w\W]+?)\<\/tr");
                MatchCollection matchsMade = pattern.Matches(InfoBody);//匹配每条数据
                foreach (Match item in matchsMade)
                {
                    pattern = new Regex(@"href=""(show[\w\W]+?)""");
                    matchMode = pattern.Match(item.Groups[1].Value);  //内容页链接
                    if (matchMode.Success)
                    {
                        SqlConnection sqlCnt = new SqlConnection(connectString);
                        sqlCnt.Open();
                        DataSet myDataSet2 = new DataSet();
                        DataTable myTable2 = myDataSet2.Tables["Mp4baData"];
                        SqlDataAdapter myDataAdapter2 = new SqlDataAdapter("SELECT * FROM  Mp4baData where DetailUrl = '" + "http://www.mp4ba.com/" + matchMode.Groups[1].Value + "'", sqlCnt);
                        myDataAdapter2.Fill(myDataSet2, "Mp4baData");
                        myTable2 = myDataSet2.Tables["Mp4baData"];
                        if (myTable2.Rows.Count == 0)
                        {
                            HtmlDelegate HtmlData = new HtmlDelegate(GetHtml);
                            IAsyncResult async = HtmlData.BeginInvoke("http://www.mp4ba.com/" + matchMode.Groups[1].Value, TestCallback, null);
                        }
                        else
                        {
                            listBox1.Items.Add(matchMode.Groups[1].Value + "已保存过...");
                            if (oldnum--<0)
                            {
                                start = false;
                                listBox1.Items.Add("********************已完成更新*********************");
                            }

                            sqlCnt.Close();
                            sqlCnt.Dispose();           // 释放数据库连接对象
                        }

                    }
                    else
                    {
                        Logs("匹配内容链接", -1, "匹配内容链接失败", item.Groups[1].Value);
                    }

                }
                if (moviepage <= maxpage)
                {
                    GetVideoList();
                }
                listBox1.Items.Add ("******"+(moviepage-1) + "页 已完成******");
            }
            catch (Exception e)
            {
                Logs("异步处理函数", -1, "列表解析错误" + e.Message, moviepage.ToString());
            }
        }  //异步响应函数

        public int GetMaxPages()
        {
            string ret = GetHtml("http://www.mp4ba.com/index.php");
            Regex pattern = new Regex(@"<span>&#8230;.</span><a href=""[\w\W]*?page=([\w\W]*?)""");
            Match matchMode = pattern.Match(ret);//匹配整个信息段
            if (matchMode.Success)
            {
                try
                {
                    return Convert.ToInt32(matchMode.Groups[1].Value);
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }

        public void RegexBaiduYun()
        {
            SqlConnection sqlCnt = new SqlConnection(connectString);
            sqlCnt.Open();

            //SqlDataAdapter myDataAdapter = new SqlDataAdapter("select * from Fanslike where FromUID = '" + fromuid +"'", sqlCnt);
            SqlDataAdapter myDataAdapter = new SqlDataAdapter("SELECT TOP 1 * FROM  Mp4baData where Remarks = '' order by id desc", sqlCnt);
            DataSet myDataSet = new DataSet();
            myDataAdapter.Fill(myDataSet, "Mp4baData");
            DataTable myTable = myDataSet.Tables["Mp4baData"];
            if (myTable.Rows.Count > 0)
            {
                myTable.Rows[0]["Remarks"] = DateTime.Now.ToString();
                SqlCommandBuilder mySqlCommandBuilder = new SqlCommandBuilder(myDataAdapter);
                myDataAdapter.Update(myDataSet, "Mp4baData");
                string pwd = "", url = "";
                Regex pattern2 = new Regex(@"(http://pan.baidu.com/[\w\W]+?)""");
                Match matchMode = pattern2.Match(myTable.Rows[0]["MovieIntroduction"].ToString());
                url = matchMode.Groups[1].Value;
                pattern2 = new Regex(@"密码[\w\W]+?([A-Za-z0-9]{4})");
                matchMode = pattern2.Match(myTable.Rows[0]["MovieIntroduction"].ToString());
                pwd = matchMode.Groups[1].Value;

                if (pwd != "" && url != "")
                {
                    myTable.Rows[0]["BaiduYunUrl"] = url;
                    myTable.Rows[0]["BaiduYunPwd"] = pwd;
                    mySqlCommandBuilder = new SqlCommandBuilder(myDataAdapter);
                    myDataAdapter.Update(myDataSet, "Mp4baData");
                    listBox1.Items.Add(myTable.Rows[0]["MovieName"].ToString() + "已匹配到网盘地址...");
                }
                else
                {
                    if (pwd == "" && url != "")
                    {
                        Logs("匹配百度云地址", -1, myTable.Rows[0]["ID"].ToString() + " " + myTable.Rows[0]["MovieName"].ToString() + "匹配密码失败...");
                        listBox1.Items.Add(myTable.Rows[0]["ID"].ToString() + " " + myTable.Rows[0]["MovieName"].ToString() + "匹配密码失败...");
                    }
                    else
                    {
                        listBox1.Items.Add(myTable.Rows[0]["MovieName"].ToString() + "无网盘地址...");
                    }
                }
            }


            myDataSet.Dispose();        // 释放DataSet对象
            sqlCnt.Close();
            sqlCnt.Dispose();           // 释放数据库连接对象
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 300; i++)
            {
                RegexBaiduYun();
            }
        }


    }
}
