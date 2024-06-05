using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoplFixedMath;

namespace MapMaker
{
    internal class SignalSystem
    {
        //up to 65536 signals! more then enoth for amost anything! even building a computer???
        public static byte[] Signals = new byte[8192];
        public static List<Trigger> Triggers = new List<Trigger>();
        //returns the value of the signal.
        public static bool GetSignal(uint signal)
        {
            switch (signal % 8)
            {
                //exstract the bit we want
                case 0:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 1) > 0;
                case 1:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 2) > 0;
                case 2:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 4) > 0;
                case 3:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 8) > 0;
                case 4:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 16) > 0;
                case 5:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 32) > 0;
                case 6:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 64) > 0;
                case 7:
                    return (Signals[(uint)Fix.Floor((Fix)signal / (Fix)8)] & 128) > 0;
                default: return false;
            }
        }
        //registers the trigger and returns the id
        public static int RegisterTrigger(Trigger trigger)
        {
            var InsertSpot = BinarySearchTriggerSignalId(trigger.Signal);
            Triggers.Insert(InsertSpot, trigger);
            return InsertSpot;
        }
        //returns the id of the first trigger with that signal id. assumes the List is sorted
        public static int BinarySearchTriggerSignalId(uint signal)
        {
            UnityEngine.Debug.Log("BinarySearchTriggerSignalId");
            var LowerBound = 0;
            var UpperBound = Triggers.Count - 1;
            var Middle = 0;
            UnityEngine.Debug.Log("right before while");
            while (LowerBound < UpperBound)
            {
                Middle = (int)Math.Floor((float)(LowerBound + UpperBound / 2));
                UnityEngine.Debug.Log("Middle is " + Middle);
                var SignalAtMiddle = Triggers[Middle].Signal;
                if (SignalAtMiddle > signal)
                {
                    UpperBound = Middle;
                }
                else if (SignalAtMiddle < signal)
                {
                    LowerBound = Middle;
                }
                else 
                {
                    break;
                }
            }
            //get the first
            UnityEngine.Debug.Log("Middle is " + Middle);
            if (Triggers.Count != 0)
            {
                while (Middle >= 0 && Triggers[Middle].Signal == signal)
                {
                    Middle--;
                    UnityEngine.Debug.Log("New Middle is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
    }
}
