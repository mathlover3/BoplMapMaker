using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoplFixedMath;
using UnityEngine;

namespace MapMaker
{
    public class SignalSystem : MonoUpdatable
    {
        public static List<LogicOutput> LogicOutputs = new List<LogicOutput>();
        public static List<LogicInput> LogicInputs = new List<LogicInput>();
        public static List<LogicOutput> LogicTriggerOutputs = new List<LogicOutput>();
        //registers the trigger and returns the id
        public static void RegisterLogicGate(LogicGate LogicGate)
        {
            UnityEngine.Debug.Log("RegisterLogicGate");
            for (int i = 0; i < LogicGate.OutputSignals.Count; i++)
            {
                UnityEngine.Debug.Log("RegisterOutput");
                if (LogicOutputs.Count == 0)
                {
                    LogicOutputs.Insert(0, LogicGate.OutputSignals[i]);
                    return;
                }
                var InsertSpot = BinarySearchLogicOutputSignalId(LogicGate.OutputSignals[i].Signal);
                LogicOutputs.Insert(InsertSpot, LogicGate.OutputSignals[i]);
            }
            for (int i = 0; i < LogicGate.InputSignals.Count; i++)
            {
                UnityEngine.Debug.Log("RegisterInput");
                if (LogicInputs.Count == 0)
                {
                    LogicInputs.Insert(0, LogicGate.InputSignals[i]);
                    return;
                }
                var InsertSpot = BinarySearchLogicInputSignalId(LogicGate.InputSignals[i].Signal);
                LogicInputs.Insert(InsertSpot, LogicGate.InputSignals[i]);
            }
        }
        public static void RegisterTrigger(LogicOutput logicOutput)
        {
            UnityEngine.Debug.Log("RegisterTrigger");
                if (LogicTriggerOutputs.Count == 0)
                {
                    LogicTriggerOutputs.Insert(0, logicOutput);
                    return;
                }
                var InsertSpot = BinarySearchLogicTriggerOutputSignalId(logicOutput.Signal);
                LogicTriggerOutputs.Insert(InsertSpot, logicOutput);
        }
        //returns the id of the first LogicOutput with that signal id. assumes the List is sorted
        public static int BinarySearchLogicOutputSignalId(ushort Signal)
        {
            //UnityEngine.Debug.Log("BinarySearchLogicOutputSignalId");
            var LowerBound = 0;
            var UpperBound = LogicOutputs.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound < UpperBound)
            {
                Middle = (int)Math.Floor((float)(LowerBound + UpperBound / 2));
                //UnityEngine.Debug.Log("Middle is " + Middle);
                var SignalAtMiddle = LogicOutputs[Middle].Signal;
                if (SignalAtMiddle > Signal)
                {
                    UpperBound = Middle;
                }
                else if (SignalAtMiddle < Signal)
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
            if (LogicOutputs.Count != 0)
            {
                while (Middle >= 0 && LogicOutputs[Middle].Signal == Signal)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New Middle is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        //returns the id of the first LogicInput with that signal id. assumes the List is sorted
        public static int BinarySearchLogicInputSignalId(ushort Signal)
        {
            //UnityEngine.Debug.Log("BinarySearchLogicInputSignalId");
            var LowerBound = 0;
            var UpperBound = LogicInputs.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound < UpperBound)
            {
                Middle = (int)Math.Floor((float)(LowerBound + UpperBound / 2));
                //UnityEngine.Debug.Log("Middle is " + Middle);
                var SignalAtMiddle = LogicInputs[Middle].Signal;
                if (SignalAtMiddle > Signal)
                {
                    UpperBound = Middle;
                }
                else if (SignalAtMiddle < Signal)
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
            if (LogicInputs.Count != 0)
            {
                while (Middle >= 0 && LogicInputs[Middle].Signal == Signal)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New Middle is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        //returns the id of the first LogicOutput with that signal id. assumes the List is sorted
        public static int BinarySearchLogicTriggerOutputSignalId(ushort Signal)
        {
            //UnityEngine.Debug.Log("BinarySearchLogicOutputSignalId");
            var LowerBound = 0;
            var UpperBound = LogicTriggerOutputs.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound < UpperBound)
            {
                Middle = (int)Math.Floor((float)(LowerBound + UpperBound / 2));
                //UnityEngine.Debug.Log("Middle is " + Middle);
                var SignalAtMiddle = LogicTriggerOutputs[Middle].Signal;
                if (SignalAtMiddle > Signal)
                {
                    UpperBound = Middle;
                }
                else if (SignalAtMiddle < Signal)
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
            if (LogicTriggerOutputs.Count != 0)
            {
                while (Middle >= 0 && LogicTriggerOutputs[Middle].Signal == Signal)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New Middle is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        public static List<LogicInput> GetLogicInputs(ushort signal)
        {
            //if there are no LogicInputs return a empty list
            if (LogicInputs.Count == 0)
            {
                UnityEngine.Debug.Log($"no LogicInputs");
                return new List<LogicInput>();
            }
            //outerwise get the first LogicInput with that signal id if there is one
            var FirstLogicInputIndex = BinarySearchLogicInputSignalId(signal);
            //if theres no LogicInputs with that signal return a empty list
            if (LogicInputs[FirstLogicInputIndex].Signal != signal)
            {
                UnityEngine.Debug.Log($"no LogicInputs with signal {signal}");
                return new List<LogicInput>();
            }
            var CurrentLogicInputIndex = FirstLogicInputIndex;
            //create a new list
            List<LogicInput> inputs = new List<LogicInput>();
            //while its not past the last LogicInput and this LogicInputs signal is correct check if its IsOn is true and if so add it to the list
            while (CurrentLogicInputIndex < LogicInputs.Count && LogicInputs[CurrentLogicInputIndex].Signal == signal)
            {
                inputs.Add(LogicInputs[CurrentLogicInputIndex]);
                CurrentLogicInputIndex++;
            }
            return inputs;
        }
        public static List<LogicOutput> GetLogicOutputs(ushort signal)
        {
            var allOutputs = new List<LogicOutput>();
            allOutputs.AddRange(LogicOutputs);
            allOutputs.AddRange(LogicTriggerOutputs);

            //if there are no LogicOutputs return a empty list
            if (allOutputs.Count == 0)
            {
                return new List<LogicOutput>();
            }
            //outerwise get the first LogicOutput with that signal id if there is one
            var FirstLogicOutputIndex = BinarySearchLogicOutputSignalId(signal);
            //if theres no LogicOutputs with that signal return a empty list
            if (allOutputs[FirstLogicOutputIndex].Signal != signal)
            {
                return new List<LogicOutput>();
            }
            var CurrentLogicOutputIndex = FirstLogicOutputIndex;
            //create a new list
            List<LogicOutput> outputs = new List<LogicOutput>();
            //while its not past the last LogicOutput and this LogicOutputs signal is correct check if its IsOn is true and if so add it to the list
            while (CurrentLogicOutputIndex < allOutputs.Count && allOutputs[CurrentLogicOutputIndex].Signal == signal)
            {
                outputs.Add(allOutputs[CurrentLogicOutputIndex]);
                CurrentLogicOutputIndex++;
            }
            return outputs;
        }
        public void SetUpDicts()
        {
            var allOutputs = new List<LogicOutput>();
            allOutputs.AddRange(LogicOutputs);
            allOutputs.AddRange(LogicTriggerOutputs);
            foreach (var output in allOutputs)
            {
                //UnityEngine.Debug.Log($"allOutputs has length of {allOutputs.Count}");
                var inputs = GetLogicInputs(output.Signal);
                //UnityEngine.Debug.Log($"LogicInputs for signal {output.Signal} has length {inputs.Count}");
                foreach (var input in inputs)
                {
                    //UnityEngine.Debug.Log($"input: {input} input.inputs: {input.inputs}");
                    if (input.inputs == null)
                    {
                        //UnityEngine.Debug.Log($"input.inputs IS NULL!");
                    }
                    input.inputs.Add(output);
                    //UnityEngine.Debug.Log($"added output to inputs");
                    //they have the same signal so lets just get them both out of the way in one fail swoop.
                    output.outputs.Add(input);
                    //UnityEngine.Debug.Log($"added input to outputs");
                    //this should attact all inputs to all corsponding outputs in one go so we shouldnt need this. keeping it here just in case though.
                    /*
                    foreach (var output2 in GetLogicOutputs(input.Signal))
                    {
                        SetUpDictsRecusive(output2);
                    }
                    */
                }
            }
            Updater.RegisterUpdatable(this);
        }

        public override void Init()
        {
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {
            for (int i = 0; i < LogicTriggerOutputs.Count; i++)
            {
                LogicOutput output = LogicTriggerOutputs[i];
                CallAllLogic(output, SimDeltaTime);
            }
        }
        private static void CallAllLogic(LogicOutput output, Fix SimDeltaTime)
        {
            //UnityEngine.Debug.Log($"CallAllLogic");
            //triggers dont have gates
            if (output.gate)
            {
                output.gate.Logic(SimDeltaTime);
            }
            for (int i = 0; i < output.outputs.Count; i++)
            {
                //UnityEngine.Debug.Log($"output.outputs is of length {output.outputs.Count}");
                LogicInput input = output.outputs[i];
                //UnityEngine.Debug.Log($"output.IsOn is: {output.IsOn}");
                input.IsOn = output.IsOn;
                input.gate.Logic(SimDeltaTime);
                for (int j = 0; j < input.gate.OutputSignals.Count; j++)
                {
                    //UnityEngine.Debug.Log($"input.gate.OutputSignals is of length {input.gate.OutputSignals.Count}");
                    LogicOutput output2 = input.gate.OutputSignals[j];
                    CallAllLogic(output2, SimDeltaTime);
                }
            }
        }
    }
}
