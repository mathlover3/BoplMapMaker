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
        public static List<Trigger> Triggers = new List<Trigger>();
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
            //UnityEngine.Debug.Log("BinarySearchTriggerSignalId");
            var LowerBound = 0;
            var UpperBound = Triggers.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound < UpperBound)
            {
                Middle = (int)Math.Floor((float)(LowerBound + UpperBound / 2));
                //UnityEngine.Debug.Log("Middle is " + Middle);
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
            //UnityEngine.Debug.Log("Middle is " + Middle);
            if (Triggers.Count != 0)
            {
                while (Middle >= 0 && Triggers[Middle].Signal == signal)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New Middle is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        public static bool IsSignalOn(ushort signal)
        {
            //if there are no triggers return false
            if (Triggers.Count == 0)
            {
                //UnityEngine.Debug.Log("No Triggers Registerd");
                return false;
            }
            //outerwise get the first trigger with that signal id if there is one
            var FirstTriggerIndex = BinarySearchTriggerSignalId(signal);
            //UnityEngine.Debug.Log("FirstTriggerIndex: " + FirstTriggerIndex);
            //if theres no trigger with that signal return false
            if (Triggers[FirstTriggerIndex].Signal != signal)
            {
                //UnityEngine.Debug.Log("the First Trigger didnt have the same signal");
                return false;
            }
            var CurrentTriggerIndex = FirstTriggerIndex;
            //while its not past the last trigger and this triggers signal is correct check if its IsOn is true and if so return true
            while (CurrentTriggerIndex < Triggers.Count && Triggers[CurrentTriggerIndex].Signal == signal)
            {
                //UnityEngine.Debug.Log("CurrentTriggerIndex is: " + CurrentTriggerIndex);
                if (Triggers[CurrentTriggerIndex].IsOn)
                {
                    //UnityEngine.Debug.Log("Signal Is On! " + signal);
                    return true;
                }
                CurrentTriggerIndex++;
            }
            //if none of the triggers with that id were on then return false.
            return false;
        }
    }
}
