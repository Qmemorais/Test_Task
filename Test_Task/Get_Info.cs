using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace Test_Task
{
    class Get_Info
    {
        public bool Get_Request(string url_,string MainPage)
        {//проверка на text/html
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url_);
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.ContentType.IndexOf("text/html")!=-1)
                {
                    response.Close();
                    return true;
                }
                else
                {
                    response.Close();
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public void FoundAllHTML(string url_)
        {//общая функция. из нее идем везде
            List<string> HTMLDocScan = new List<string>();
            List<string> sitemapHTMLDOC = new List<string>();
            try
            {
                WebRequest request_ = WebRequest.Create(url_);
                request_.Timeout = 10000;
                WebResponse response_ = request_.GetResponse();
                response_.Close();
                string mainURL = "";
                HTMLDocScan.Add(url_);
                int findMainPage = url_.IndexOf("/", 8);
                if (findMainPage != -1)
                    mainURL = url_.Substring(0, findMainPage);
                HTMLDocScan = FinadAllURLHTMLOnPage(HTMLDocScan, mainURL);
                try
                {
                    WebRequest request = WebRequest.Create(mainURL + "/sitemap.xml");
                    WebResponse response = request.GetResponse();
                    response.Close();
                    sitemapHTMLDOC = FindOnSitemapXML(mainURL + "/sitemap.xml", mainURL);
                    OutputPages(HTMLDocScan, sitemapHTMLDOC, mainURL);
                }
                catch
                {
                    try
                    {
                        string urlSitemapXML = "";
                        WebRequest request = WebRequest.Create(mainURL + "/robots.txt");
                        WebResponse response = request.GetResponse();
                        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.IndexOf("Sitemap: ") != -1)
                            {
                                urlSitemapXML = line.Substring(9);
                                sitemapHTMLDOC = FindOnSitemapXML(urlSitemapXML, mainURL);
                            }
                        }
                        response.Close();
                        OutputPages(HTMLDocScan, sitemapHTMLDOC, mainURL);
                    }
                    catch (WebException webExcp)
                    {
                        IfSitemapDoesnotExistFindTime(HTMLDocScan,mainURL);
                        WebExceptionStatus status = webExcp.Status;
                        if (status == WebExceptionStatus.ProtocolError)
                        {
                            Console.Write("The server returned protocol error ");
                            HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                            Console.WriteLine((int)httpResponse.StatusCode + " - "
                               + httpResponse.StatusCode);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Wrong URL");
                    }
                }
            }
            catch (WebException webExcp)
            {
                WebExceptionStatus status = webExcp.Status;
                if (status == WebExceptionStatus.ProtocolError)
                {
                    Console.Write("The server returned protocol error ");
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    Console.WriteLine((int)httpResponse.StatusCode + " - "
                       + httpResponse.StatusCode);
                }
            }
        }

        public List<string> FinadAllURLHTMLOnPage(List<string> HTMLDocScan,string MainPage)
        {//поиск сканированием страниц
            for(int i=0;i< HTMLDocScan.Count();i++)
            {
                try
                {
                    if (HTMLDocScan[i].Contains(MainPage)) {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HTMLDocScan[i]);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            int IndexOfHREF = line.IndexOf("href=\"");
                            if (IndexOfHREF != -1)
                            {
                                line = line.Substring(IndexOfHREF + 6);
                                int IndexOfEndHREF = line.IndexOf('"');
                                line = line.Substring(0, IndexOfEndHREF);
                                if (!line.Contains("http"))
                                {
                                    line = MainPage + line;
                                    if (line.IndexOf(MainPage + "/") == -1)
                                        line = line.Insert(MainPage.Length, "/");
                                }
                                if (Get_Request(line, MainPage) == true)
                                    if (!HTMLDocScan.Contains(line))
                                        HTMLDocScan.Add(line);
                            }
                        }
                        response.Close();
                    }
                }
                catch (WebException webExcp)
                {
                    WebExceptionStatus status = webExcp.Status;
                    if (status == WebExceptionStatus.ProtocolError)
                    {
                        Console.Write("The server returned protocol error ");
                        HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                        Console.WriteLine((int)httpResponse.StatusCode + " - "
                           + httpResponse.StatusCode);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Wrong URL");
                }
            }
            return HTMLDocScan;
        }

        public List<string> FindOnSitemapXML(string urlSitemapXML,string MainPage)
        {//волшебный сайтмап если есть или через робота
            List<string> sitemapHTMLDOC = new List<string>();
            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.Load(urlSitemapXML);
                XmlElement xRoot = xDoc.DocumentElement;
                foreach (XmlNode xnode in xRoot)
                    foreach (XmlNode childnode in xnode.ChildNodes)
                        if (childnode.Name == "loc")
                            if (Get_Request(childnode.InnerText, MainPage) == true)
                                if (!sitemapHTMLDOC.Contains(childnode.InnerText))
                                    sitemapHTMLDOC.Add(childnode.InnerText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return sitemapHTMLDOC;
        }

        public void OutputPages(List<string> HTMLDocScan, List<string> sitemapHTMLDOC, string MainPage)
        {//вывод в окно
            HTMLDocScan = RemoveHttps(HTMLDocScan);
            sitemapHTMLDOC = RemoveHttps(sitemapHTMLDOC);
            List<string> ExistSitemapNoWeb = sitemapHTMLDOC.Except(HTMLDocScan).ToList();
            List<string> ExistWebpNoSitemap = HTMLDocScan.Except(sitemapHTMLDOC).ToList();
            Console.WriteLine("Urls FOUNDED IN SITEMAP.XML but not founded after crawling a web site");
            FindTime(ExistSitemapNoWeb, MainPage);
            Console.WriteLine("Urls FOUNDED BY CRAWLING THE WEBSITE but not in sitemap.xml");
            FindTime(ExistWebpNoSitemap, MainPage);
            Console.WriteLine("Urls(html documents) found after crawling a website: " + HTMLDocScan.Count);
            Console.WriteLine("Urls found in sitemap: " + sitemapHTMLDOC.Count);
        }
        public void FindTime(List<string> Existing, string MainPage)
        {
            Dictionary<string, int> URLWithTime = new Dictionary<string, int>();
            foreach (string url in Existing)
            {//рассчет времени
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://" + url);
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    sw.Stop();
                    response.Close();
                    URLWithTime.Add(url, (int)sw.ElapsedMilliseconds);
                }
                catch
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + url);
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    sw.Stop();
                    response.Close();
                    URLWithTime.Add(url, (int)sw.ElapsedMilliseconds);
                }
            }
            URLWithTime = URLWithTime.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var urlTime in URLWithTime)
                Console.WriteLine("URL: " + urlTime.Key + " ;  Time: " + urlTime.Value + "ms");
        }

        public void IfSitemapDoesnotExistFindTime(List<string> Existing,string MainPage)
        {//как и предыдщуая только если так и не нашли сайтмап
            Dictionary<string,int> URLWithTime = new Dictionary<string, int>();
            foreach (string url in Existing)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://" + url);
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    sw.Stop();
                    response.Close();
                    URLWithTime.Add(url, (int)sw.ElapsedMilliseconds);
                }
                catch
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://" + url);
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    sw.Stop();
                    response.Close();
                    URLWithTime.Add(url, (int)sw.ElapsedMilliseconds);
                }
            }
            URLWithTime = URLWithTime.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key,pair => pair.Value);
            foreach (var urlTime in URLWithTime)
                Console.WriteLine("URL: " + urlTime.Key + " ;  Time: " + urlTime.Value + "ms");
        }

        public List<string> RemoveHttps(List<string> HTMLDOC)
        {//изза разницы в ссылка между http и https
            for(int i=0;i<HTMLDOC.Count();i++)
            {
                if (HTMLDOC[i].Contains("https://"))
                    HTMLDOC[i]=HTMLDOC[i].Substring(8);
                if (HTMLDOC[i].Contains("http://"))
                    HTMLDOC[i] = HTMLDOC[i].Substring(7);
            }
            return HTMLDOC;
        }
    }
}