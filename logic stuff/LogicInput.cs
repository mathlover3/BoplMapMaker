﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker
{
    public class LogicInput
    {
        public LogicInput()
        {
            inputs = new List<LogicOutput>();
        }
        public int UUid;
        //must be set before registering your gate
        public LogicGate gate;
        public GameObject Owner;
        public bool IsOn;
        //these are the inputs to this input

        public List<LogicOutput> inputs;
    }
}
