using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public struct LogicInput
    {
        public LogicInput()
        {
            inputs = new List<LogicOutput>();
        }
        //up to 65536 signals! more then enoth for amost anything! even building a computer???
        public ushort Signal;
        //must be set before registering your gate
        public LogicGate gate;
        public bool IsOn;
        //these are the inputs to this input

        public List<LogicOutput> inputs;
    }
}
