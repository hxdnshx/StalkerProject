using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace StalkerProject
{
    public static class ConsoleHelper
    {
        public static string CallGit(string arg)
        {
            ProcessStartInfo proc=new ProcessStartInfo("git",arg);

        }
    }
}
