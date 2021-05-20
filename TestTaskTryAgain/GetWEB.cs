using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TestTaskTryAgain
{
    class GetWEB
    {
        public void CreateHTML(string url)
        {//основные списки и чапаем везде
            Stopwatch timeall = Stopwatch.StartNew();
            List<string> HTMLScan = new List<string>();
            List<string> HTMLSitemap = new List<string>();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
                HTMLScan.Add(url);
                //скан лично сайта
                HTMLScan = ScanWebSite(HTMLScan);
                //если есть то не будет пустой
                HTMLSitemap = getSitemapIfExist(url, HTMLSitemap);
                //теперь ползем в функцию сравнения списков и времени
            }
            catch (WebException webExcp)
            {//проверка на допуск к сайт. возможна ошибка 403,404
                WebExceptionStatus status = webExcp.Status;
                if (status == WebExceptionStatus.ProtocolError)
                {
                    Console.Write("The server returned protocol error ");
                    HttpWebResponse httpResponse = (HttpWebResponse)webExcp.Response;
                    Console.WriteLine((int)httpResponse.StatusCode + " - "
                       + httpResponse.StatusCode);
                }
            }
            finally
            {//если ссылка была не рабочей то ничего не будет
                OutputPage(HTMLScan, HTMLSitemap);
                timeall.Stop();
                Console.WriteLine("Time: " + timeall.Elapsed.Minutes);
                Console.Write("Press <Enter>");
                Console.ReadLine();
            }
        }
        private bool TryRequest(string url)
        {
            try
            {//находим нужный формат страницы
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
                return (response.ContentType.IndexOf("text/html") != -1);
            }
            catch
            {
                return false;
            }
        }

        private List<string> ScanWebSite(List<string> HTMLScan)
        {//сканируем.https://www.slavutich-azov.org/about.html
            string MainWeb = GetMainWebPage(HTMLScan[0]);
            List<string> AllPages = new List<string>();
            AllPages.Add(HTMLScan[0]);
            string HRef = @"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>\S+))";
            Regex r = new Regex(HRef, RegexOptions.IgnoreCase | RegexOptions.Compiled,
                                            TimeSpan.FromSeconds(1));
            for (int i = 0; i < HTMLScan.Count(); i++)
            {
                if (HTMLScan[i].Contains(MainWeb.Substring(5)))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HTMLScan[i]);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader read = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
                    string HTMLtxt = read.ReadToEnd();
                    response.Close();
                    Match match = r.Match(HTMLtxt);
                    List<string> matches = new List<string>();
                    while (match.Success)
                    {//преобразование в адекватную строку
                     //строка  favicon.ico не есть ок, надо http:блабла
                        string url__ = match.Value;
                        url__ = url__.Substring(6);
                        int length = url__.LastIndexOf("\"");
                        if (length != -1)
                            url__ = url__.Substring(0, length);
                        if (!url__.Contains("http"))
                        {
                            url__ = MainWeb + url__;
                            if (url__.IndexOf(MainWeb + "/") == -1)
                                url__ = url__.Insert(MainWeb.Length, "/");
                        }
                        if (url__.Contains(MainWeb.Substring(5)))
                            matches.Add(url__);
                        match=match.NextMatch();
                    }
                    //получаем те ссылки которых точно нет в списке. а значит в нигде
                    List<string> ExceptMatchInHTML = matches.Except(AllPages).ToList();
                    if (ExceptMatchInHTML.Count != 0)
                        for (int k = 0; k < ExceptMatchInHTML.Count; k++)
                        {
                            AllPages.Add(ExceptMatchInHTML[k]);
                            if (ExceptMatchInHTML[k].IndexOf("#") == -1)
                            {//проверка на совпадение и разницу только в http or https
                                List<string> query = HTMLScan.Where(web => web.IndexOf(ExceptMatchInHTML[k].Substring(5)) != -1).ToList();
                                if (query.Count() == 0 || ExceptMatchInHTML[k] == MainWeb + "/")
                                    if (TryRequest(ExceptMatchInHTML[k]) == true)
                                        //добавление в общий список который ретернем
                                        HTMLScan.Add(ExceptMatchInHTML[k]);
                            }
                        }
                }
            }
            return HTMLScan;
        }
        private List<string> getSitemapIfExist(string url, List<string> HTMLSitemap)
        {//получаем если все ок ссылки из сайтмапа
            string MainWeb = GetMainWebPage(url);
            try
            {
                WebRequest request = WebRequest.Create(MainWeb + "/sitemap.xml");
                WebResponse response = request.GetResponse();
                response.Close();
                HTMLSitemap = FindOnSitemapXML(MainWeb + "/sitemap.xml");
            }
            catch
            {
                string urlSitemapXML = "";
                WebRequest request = WebRequest.Create(MainWeb + "/robots.txt");
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1251));
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.IndexOf("Sitemap: ") != -1)
                    {
                        urlSitemapXML = line.Substring(9);
                        HTMLSitemap = FindOnSitemapXML(urlSitemapXML);
                    }
                }
                response.Close();
            }
            return HTMLSitemap;
        }

        private List<string> FindOnSitemapXML(string urlSitemapXML)
        {//волшебный сайтмап если есть или через робота
            List<string> sitemapHTMLDOC = new List<string>();
            string MainPage = GetMainWebPage(urlSitemapXML);
            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.Load(urlSitemapXML);
                XmlElement xRoot = xDoc.DocumentElement;
                foreach (XmlNode xnode in xRoot)
                    foreach (XmlNode childnode in xnode.ChildNodes)
                        if (childnode.Name == "loc")
                            if (TryRequest(childnode.InnerText) == true)
                                if (!sitemapHTMLDOC.Contains(childnode.InnerText))
                                    sitemapHTMLDOC.Add(childnode.InnerText);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return sitemapHTMLDOC;
        }

        private void OutputPage(List<string> HTMLScan, List<string> HTMLSitemap)
        {
            if (HTMLSitemap.Count == 0)
            {
                Console.WriteLine("Sitemap doesn`t exist!!");
                HTMLScan = RemoveHttps(HTMLScan);
                OutputPageTime(HTMLScan);
                Console.WriteLine("Urls(html documents) found after crawling a website: " + HTMLScan.Count);
            }
            else
            {
                HTMLScan = RemoveHttps(HTMLScan);
                HTMLSitemap = RemoveHttps(HTMLSitemap);
                List<string> ExistSitemapNoWeb = HTMLSitemap.Except(HTMLScan).ToList();
                List<string> ExistWebpNoSitemap = HTMLScan.Except(HTMLSitemap).ToList();
                Console.WriteLine("Urls FOUNDED IN SITEMAP.XML but not founded after crawling a web site");
                OutputPageTime(ExistSitemapNoWeb);
                Console.WriteLine("Urls FOUNDED BY CRAWLING THE WEBSITE but not in sitemap.xml");
                OutputPageTime(ExistWebpNoSitemap);
                Console.WriteLine("Urls(html documents) found after crawling a website: " + HTMLScan.Count);
                Console.WriteLine("Urls found in sitemap: " + HTMLSitemap.Count);
            }
        }

        private string GetMainWebPage(string url)
        {
            string MainWeb = url;
            int findMainPage = MainWeb.IndexOf("/", 8);
            if (findMainPage != -1)
                MainWeb = MainWeb.Substring(0, findMainPage);
            return MainWeb;
        }

        private List<string> RemoveHttps(List<string> HTML)
        {//изза разницы в ссылка между http и https
            for (int i = 0; i < HTML.Count(); i++)
            {
                if (HTML[i].Contains("https://"))
                    HTML[i] = HTML[i].Substring(8);
                if (HTML[i].Contains("http://"))
                    HTML[i] = HTML[i].Substring(7);
            }
            return HTML;
        }

        private void OutputPageTime(List<string> HTML)
        {
            Dictionary<string, int> URLWithTime = new Dictionary<string, int>();
            foreach (string url in HTML)
            {//рассчет времени
                try
                {
                    int time = GetTime("http://" + url);
                    URLWithTime.Add(url, time);
                }
                catch
                {
                    int time = GetTime("https://" + url);
                    URLWithTime.Add(url, time);
                }
            }
            URLWithTime = URLWithTime.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var urlTime in URLWithTime)
                Console.WriteLine("URL: " + urlTime.Key + " ;  Time: " + urlTime.Value + "ms");
        }

        private int GetTime(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            Stopwatch sw = Stopwatch.StartNew();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            sw.Stop();
            response.Close();
            return (int)sw.ElapsedMilliseconds;
        }
    }
}
