using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_XLogo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sample = new Sample())
            {
                sample.Show();
                System.Threading.Thread.Sleep(10000);
            }
        }
    }
}
