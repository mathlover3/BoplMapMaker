using BoplFixedMath;
using System;
using System.CodeDom;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        //if true it shows all of the logic gates and there connectsons
        public static bool LogicDebugMode = true;
        //this is for rendering the connectsons.
        public static Dictionary<LogicInput, LineRenderer> LineRenderers = new();
        //these are for the connectsons connected to platforms.
        public static List<LogicInput> LogicInputsThatAlwaysUpdateThereLineConnectsons = new();
        //is it the first update of the round?
        public static bool FirstUpdateOfTheRound = true;
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
        public static void RegisterInputThatUpdatesConnectson(LogicInput input)
        {
            LogicInputsThatAlwaysUpdateThereLineConnectsons.Add(input);
        }
        //returns the id of the first LogicOutput with that UUid id. assumes the List is sorted
        public static int BinarySearchLogicOutputSignalId(int UUid)
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
        public static int BinarySearchLogicInputSignalId(int UUid)
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
        public static int BinarySearchLogicTriggerOutputSignalId(int UUid)
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
        public static List<LogicInput> GetLogicInputs(int UUid)
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
        public static List<LogicOutput> GetLogicOutputs(int UUid)
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
            //this should attach all inputs to all corsponding outputs in one go.
            var allOutputs = new List<LogicOutput>();
            allOutputs.AddRange(LogicOutputs);
            allOutputs.AddRange(LogicStartingOutputs);
            List<int> UUids = new List<int>();
            foreach (var output in allOutputs)
            {
                //if it already contanes the UUid then there are outputs with the same UUid witch isnt allowed.
                if (UUids.Contains(output.UUid))
                {
                    throw new InvalidOperationException("Logic Output UUids must be unique! not generating the logic connectsons!");
                }
                UUids.Add(output.UUid);
            }
            Graph graph = new Graph(UUids.Count);

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
                    GameObject LineRendererGameObject = new GameObject("LineRenderer");
                    LineRenderer lineRenderer = LineRendererGameObject.AddComponent<LineRenderer>();
                    lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
                    lineRenderer.endColor = (lineRenderer.startColor = Color.red);
                    lineRenderer.endWidth = (lineRenderer.startWidth = 0.2f);
                    lineRenderer.positionCount = 2;
                    SetLinePosForLine(lineRenderer, input, output);
                    LineRenderers.Add(input, lineRenderer);
                    //if its not a dealy then add it to the graph to check for cycles
                    if ((input.gate != null && input.gate.GetComponent<SignalDelay>() == null && input.gate.OutputSignals.Count != 0) && (output.gate != null && output.gate.GetComponent<SignalDelay>() == null))
                    {
                        graph.addEdge(output.UUid, input.gate.OutputSignals[0].UUid);
                    }

                    UnityEngine.Debug.Log($"added input to outputs");

                }
            }
            if (graph.isCyclic())
            {
                throw new InvalidOperationException("Logic Gate Loops Must Have a Delay In Them!");
            }
            Updater.RegisterUpdatable(this);
        }
        public static void SetLinePosForLine(LineRenderer lineRenderer, LogicInput input, LogicOutput output)
        {
            var InputOwner = input.Owner;
            var OutputOwner = output.Owner;
            //input gate checking.
            //if they should just be in the middle
            if (InputOwner.GetComponent<Trigger>() != null || InputOwner.GetComponent<Spawner>() != null || InputOwner.GetComponent<DisappearPlatformsOnSignal>() != null || InputOwner.GetComponent<MovingPlatformSignalStuff>() != null || InputOwner.GetComponent<ShootRay>() != null)
            {
                lineRenderer.SetPosition(0, (UnityEngine.Vector3)InputOwner.GetComponent<FixTransform>().position);
                //if its a DisappearPlatformsOnSignal then use the platforms posison instead
                if (InputOwner.GetComponent<DisappearPlatformsOnSignal>() != null)
                {
                    var dis = InputOwner.GetComponent<DisappearPlatformsOnSignal>();
                    lineRenderer.SetPosition(0, (UnityEngine.Vector3)dis.platform.GetComponent<FixTransform>().position);
                }

            }
            if (InputOwner.GetComponent<NotGate>() != null || InputOwner.GetComponent<SignalDelay>() != null)
            {
                var center = (UnityEngine.Vector3)InputOwner.GetComponent<FixTransform>().position;
                var rot1 = InputOwner.GetComponent<FixTransform>().rotationInner;
                var rot = rot1 * (Fix)PhysTools.RadiansToDegrees;
                var scale = InputOwner.transform.localScale.x;
                var pos1 = center + new Vector3(-2, 0);
                var pos = RotatePointAroundPivot(pos1, center, rot);
                lineRenderer.SetPosition(0, pos);
            }
            //multiple inputs
            if (InputOwner.GetComponent<AndGate>() != null || InputOwner.GetComponent<OrGate>() != null)
            {
                var center1 = (UnityEngine.Vector3)InputOwner.GetComponent<FixTransform>().position;
                var InputLength = input.gate.InputSignals.Count;
                var InputIndex = input.gate.InputSignals.IndexOf(input);
                //offset it as these gates can have multiple inputs
                var MaxOffset = 2;
                var spaceing = MaxOffset / (InputLength);
                //the *2 - 1 is so it is in the center.
                var center = center1 + new Vector3(-1, (float)spaceing * (InputIndex) - MaxOffset/2);
                var rot1 = InputOwner.GetComponent<FixTransform>().rotationInner;
                var rot = rot1 * (Fix)PhysTools.RadiansToDegrees;
                var scale = InputOwner.transform.localScale.x;
                var pos = RotatePointAroundPivot(center, center1, rot);
                lineRenderer.SetPosition(0, pos);
            }
            //output checking
            if (OutputOwner.GetComponent<Trigger>() != null || OutputOwner.GetComponent<Spawner>() != null || OutputOwner.GetComponent<DisappearPlatformsOnSignal>() != null || OutputOwner.GetComponent<MovingPlatformSignalStuff>() != null)
            {
                lineRenderer.SetPosition(1, (UnityEngine.Vector3)OutputOwner.GetComponent<FixTransform>().position);
            }
            if (OutputOwner.GetComponent<NotGate>() != null || OutputOwner.GetComponent<SignalDelay>() != null || OutputOwner.GetComponent<AndGate>() != null || OutputOwner.GetComponent<OrGate>() != null)
            {
                var center = (UnityEngine.Vector3)OutputOwner.GetComponent<FixTransform>().position;
                var rot1 = OutputOwner.GetComponent<FixTransform>().rotationInner;
                var rot = rot1 * (Fix)PhysTools.RadiansToDegrees;
                var scale = InputOwner.transform.localScale.x;
                var pos1 = center + new Vector3(2f, 0);
                var pos = RotatePointAroundPivot(pos1, center, rot);
                lineRenderer.SetPosition(1, pos);
            }
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
            if (FirstUpdateOfTheRound)
            {
                FirstUpdateOfTheRound = false;
            }
            foreach (var gate in LogicGatesToAlwaysUpdate)
            {
                gate.Logic(SimDeltaTime);
            }
            foreach(var input in LogicInputsThatAlwaysUpdateThereLineConnectsons)
            {
                var Line = LineRenderers[input];
                var output = input.inputs[0];
                SetLinePosForLine(Line, input, output);
            }
            
        }
        private static void CallAllLogic(LogicOutput output, Fix SimDeltaTime, bool FirstCall)
        {
            //triggers dont have gates
            if (output.gate)
            {
                //if this isnt a first call then we will have already done this 
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
            //if the state hasnt changed dont keep going wasting time. unless its the first update in witch case we want everything to update.
            if (output.IsOn == output.WasOnLastTick && !FirstUpdateOfTheRound)
            {
                return;
            }
            //for all of the inputs the output outputs to...
            for (int i = 0; i < output.outputs.Count; i++)
            {
                //if set the inputs IsOn to the output that is inputing into it and then run the Logic
                LogicInput input = output.outputs[i];
                input.IsOn = output.IsOn;
                //update the line color
                var Line = LineRenderers[input];
                if (input.IsOn)
                {
                    Line.endColor = (Line.startColor = Color.green);
                }
                else Line.endColor = (Line.startColor = Color.red);
                //run the gates logic
                input.gate.Logic(SimDeltaTime);
                //for all of this gates outputs keep looping recusivly
                for (int j = 0; j < input.gate.OutputSignals.Count; j++)
                {
                    LogicOutput output2 = input.gate.OutputSignals[j];
                    CallAllLogic(output2, SimDeltaTime, false);
                }
            }
        }
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Fix angle)
        {
            var PointVec2 = (Vec2)point;
            var PivotVec2 = (Vec2)pivot;
            //offset the point by the pivot so the pivot is at 0,0
            PointVec2 -= PivotVec2;
            //multiply the Point by the complex number representing the rotatson of the angle
            PointVec2 = Vec2.ComplexMul(PointVec2, new Vec2((Fix)angle * (Fix)PhysTools.DegreesToRadians));
            //offset it back
            PointVec2 += PivotVec2;
            return (Vector3)PointVec2;
        }
    }
}
