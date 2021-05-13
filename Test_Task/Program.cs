using System;

namespace Test_Task
{
    class Program
    {
        static void Main(string[] args)
        {
            Get_Info get_Info = new Get_Info();
            Console.Write("Enter URL: ");
            string url_ = Console.ReadLine();
            get_Info.FoundAllHTML(url_);
        }
    }
}
