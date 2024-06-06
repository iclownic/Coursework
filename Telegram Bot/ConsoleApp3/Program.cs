using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ListbuddyBot listbuddyBot = new ListbuddyBot();
            listbuddyBot.Start();
            Console.ReadKey();
        }
    }
}
