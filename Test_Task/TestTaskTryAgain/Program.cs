using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTaskTryAgain
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter URL: ");
            string url = Console.ReadLine();
            GetWEB getWEB = new GetWEB();
            getWEB.CreateHTML(url);
        }
    }
}
