using ServerManagerTool.Common.Enums;
using System.Collections.Generic;

namespace ServerManagerTool.Common.Lib
{
    public class ConsoleCommand
    {
        public ConsoleStatus status;
        public string rawCommand;

        public string command;
        public string args;

        public bool suppressCommand;
        public bool suppressOutput;
        public IEnumerable<string> lines = new string[0];
    }
}
