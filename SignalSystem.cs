using BoplFixedMath;
using System.Collections.Generic;
using UnityEngine;

namespace MapMaker
{
    public class SignalSystem : MonoUpdatable
    {
        public static List<LogicOutput> LogicOutputs = new List<LogicOutput>();
        public static List<LogicInput> LogicInputs = new List<LogicInput>();
        //these outputs will always have there gates run and if its output changes it runs the later ones. used for stuff that may update there output even if there input doesnt change
        public static List<LogicOutput> LogicStartingOutputs = new List<LogicOutput>();
        //if true this logic gate will run every update. used for stuff that needs updated every frame. NOT SORTED!
        public static List<LogicGate> LogicGatesToAlwaysUpdate = new List<LogicGate>();
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
                    break;
                }
                var InsertSpot = BinarySearchLogicOutputSignalId(LogicGate.OutputSignals[i].UUid);
                LogicOutputs.Insert(InsertSpot, LogicGate.OutputSignals[i]);
            }
            for (int i = 0; i < LogicGate.InputSignals.Count; i++)
            {
                RegisterInput(LogicGate.InputSignals[i]);
            }
        }
        public static void RegisterTrigger(LogicOutput logicOutput)
        {
            UnityEngine.Debug.Log("RegisterTrigger");
            if (LogicStartingOutputs.Count == 0)
            {
                LogicStartingOutputs.Insert(0, logicOutput);
                return;
            }
            var InsertSpot = BinarySearchLogicTriggerOutputSignalId(logicOutput.UUid);
            LogicStartingOutputs.Insert(InsertSpot, logicOutput);
        }
        public static void RegisterInput(LogicInput input)
        {
            UnityEngine.Debug.Log("RegisterInput");
            if (LogicInputs.Count == 0)
            {
                LogicInputs.Insert(0, input);
                return;
            }
            var InsertSpot = BinarySearchLogicInputSignalId(input.UUid);
            LogicInputs.Insert(InsertSpot, input);
        }
        public static void RegisterGateThatAlwaysRuns(LogicGate gate)
        {
            LogicGatesToAlwaysUpdate.Add(gate);
        }
        //returns the id of the first LogicOutput with that UUid id. assumes the List is sorted
        public static int BinarySearchLogicOutputSignalId(ulong UUid)
        {
            UnityEngine.Debug.Log("BinarySearchLogicOutputSignalId");
            var LowerBound = 0;
            var UpperBound = LogicOutputs.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound <= UpperBound)
            {
                Middle = ((LowerBound + UpperBound) / 2);
                //UnityEngine.Debug.Log("mid is " + Middle);
                var SignalAtMiddle = LogicOutputs[Middle].UUid;
                if (UUid < SignalAtMiddle)
                {
                    UpperBound = Middle - 1;
                }
                else if (UUid > SignalAtMiddle)
                {
                    LowerBound = Middle + 1;
                }
                else
                {
                    break;
                }
            }
            //get the first
            //UnityEngine.Debug.Log("mid is " + Middle);
            if (LogicOutputs.Count != 0)
            {
                //if its index 0 and the UUid is greater we want it to run one. hence the >= instead of a ==
                while (Middle >= 0 && LogicOutputs[Middle].UUid >= UUid)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New mid is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        //returns the id of the first LogicInput with that UUid id. assumes the List is sorted
        public static int BinarySearchLogicInputSignalId(ulong UUid)
        {
            UnityEngine.Debug.Log("BinarySearchLogicInputSignalId");
            var min = 0;
            var max = LogicInputs.Count - 1;
            var mid = 0;
            //UnityEngine.Debug.Log("right before while");
            while (min <= max)
            {
                //rounding down results in 0 when there is 2 or less items.
                mid = Mathf.CeilToInt((float)((min + max) / 2));
                UnityEngine.Debug.Log("mid is " + mid);
                var SignalAtMiddle = LogicInputs[mid].UUid;
                if (UUid < SignalAtMiddle)
                {
                    max = mid - 1;
                }
                else if (UUid > SignalAtMiddle)
                {
                    min = mid + 1;
                }
                else
                {
                    break;
                }
            }
            //get the first
            UnityEngine.Debug.Log("mid is " + mid);
            if (LogicInputs.Count != 0)
            {
                //if its index 0 and the UUid is greater we want it to run one. hence the >= instead of a ==
                while (mid >= 0 && LogicInputs[mid].UUid >= UUid)
                {
                    mid--;
                    UnityEngine.Debug.Log("New mid is " + mid);
                }
                return mid + 1;
            }
            return 0;

        }
        //returns the id of the first LogicOutput with that UUid id. assumes the List is sorted
        public static int BinarySearchLogicTriggerOutputSignalId(ulong UUid)
        {
            UnityEngine.Debug.Log("BinarySearchLogicOutputSignalId");
            var LowerBound = 0;
            var UpperBound = LogicStartingOutputs.Count - 1;
            var Middle = 0;
            //UnityEngine.Debug.Log("right before while");
            while (LowerBound <= UpperBound)
            {
                Middle = ((LowerBound + UpperBound) / 2);
                //UnityEngine.Debug.Log("mid is " + Middle);
                var SignalAtMiddle = LogicStartingOutputs[Middle].UUid;
                if (UUid < SignalAtMiddle)
                {
                    UpperBound = Middle - 1;
                }
                else if (UUid > SignalAtMiddle)
                {
                    LowerBound = Middle + 1;
                }
                else
                {
                    break;
                }
            }
            //get the first
            //UnityEngine.Debug.Log("mid is " + Middle);
            if (LogicStartingOutputs.Count != 0)
            {
                //if its index 0 and the UUid is greater we want it to run one. hence the >= instead of a ==
                while (Middle >= 0 && LogicStartingOutputs[Middle].UUid >= UUid)
                {
                    Middle--;
                    //UnityEngine.Debug.Log("New mid is " + Middle);
                }
                return Middle + 1;
            }
            return 0;

        }
        public static List<LogicInput> GetLogicInputs(ulong UUid)
        {
            //if there are no LogicInputs return a empty list
            if (LogicInputs.Count == 0)
            {
                UnityEngine.Debug.Log($"no LogicInputs");
                return new List<LogicInput>();
            }
            //outerwise get the first LogicInput with that UUid id if there is one
            var FirstLogicInputIndex = BinarySearchLogicInputSignalId(UUid);
            //if theres no LogicInputs with that UUid return a empty list
            if (LogicInputs[FirstLogicInputIndex].UUid != UUid)
            {
                UnityEngine.Debug.Log($"no LogicInputs with UUid {UUid}");
                return new List<LogicInput>();
            }
            var CurrentLogicInputIndex = FirstLogicInputIndex;
            //create a new list
            List<LogicInput> inputs = new List<LogicInput>();
            //while its not past the last old LogicInput and this LogicInputs UUid is correct check if its IsOn is true and if so add it to the list
            while (CurrentLogicInputIndex < LogicInputs.Count && LogicInputs[CurrentLogicInputIndex].UUid == UUid)
            {
                UnityEngine.Debug.Log($"adding LogicInput to inputs");
                inputs.Add(LogicInputs[CurrentLogicInputIndex]);
                CurrentLogicInputIndex++;
            }
            return inputs;
        }
        public static List<LogicOutput> GetLogicOutputs(ulong UUid)
        {
            var allOutputs = new List<LogicOutput>();
            allOutputs.AddRange(LogicOutputs);
            allOutputs.AddRange(LogicStartingOutputs);

            //if there are no LogicOutputs return a empty list
            if (allOutputs.Count == 0)
            {
                return new List<LogicOutput>();
            }
            //outerwise get the first LogicOutput with that UUid id if there is one
            var FirstLogicOutputIndex = BinarySearchLogicOutputSignalId(UUid);
            //if theres no LogicOutputs with that UUid return a empty list
            if (allOutputs[FirstLogicOutputIndex].UUid != UUid)
            {
                return new List<LogicOutput>();
            }
            var CurrentLogicOutputIndex = FirstLogicOutputIndex;
            //create a new list
            List<LogicOutput> outputs = new List<LogicOutput>();
            //while its not past the last LogicOutput and this LogicOutputs UUid is correct check if its IsOn is true and if so add it to the list
            while (CurrentLogicOutputIndex < allOutputs.Count && allOutputs[CurrentLogicOutputIndex].UUid == UUid)
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
            allOutputs.AddRange(LogicStartingOutputs);
            foreach (var output in allOutputs)
            {
                UnityEngine.Debug.Log($"allOutputs has length of {allOutputs.Count}");
                var inputs = GetLogicInputs(output.UUid);
                UnityEngine.Debug.Log($"LogicInputs for UUid {output.UUid} has length {inputs.Count}");
                foreach (var input in inputs)
                {
                    if (input.inputs == null)
                    {
                        UnityEngine.Debug.Log($"input.inputs IS NULL!");
                    }
                    input.inputs.Add(output);
                    UnityEngine.Debug.Log($"added output to inputs");
                    //they have the same UUid so lets just get them both out of the way in one fail swoop.
                    output.outputs.Add(input);
                    UnityEngine.Debug.Log($"added input to outputs");
                    //this should attact all inputs to all corsponding outputs in one go so we shouldnt need this. keeping it here just in case though.
                    /*
                    foreach (var output2 in GetLogicOutputs(input.UUid))
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
            for (int i = 0; i < LogicStartingOutputs.Count; i++)
            {
                LogicOutput output = LogicStartingOutputs[i];
                CallAllLogic(output, SimDeltaTime, true);
            }
            foreach (var gate in LogicGatesToAlwaysUpdate)
            {
                gate.Logic(SimDeltaTime);
            }
        }
        private static void CallAllLogic(LogicOutput output, Fix SimDeltaTime, bool FirstCall)
        {
            //triggers dont have gates
            if (output.gate)
            {
                //if this isnt a first call then on line 315 we will have already done this
                if (FirstCall)
                {
                    output.gate.Logic(SimDeltaTime);
                }

                //if this is a delay and its not the first call then we are stoping the prossesing as this may be a loop
                if (output.gate.GetComponent<SignalDelay>() != null && !FirstCall)
                {
                    return;
                }
            }
            //if the state hasnt changed dont keep going wasting time.
            if (output.IsOn == output.WasOnLastTick)
            {
                return;
            }
            UnityEngine.Debug.Log($"CallAllLogic");
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
                    CallAllLogic(output2, SimDeltaTime, false);
                }
            }
        }
    }
}
