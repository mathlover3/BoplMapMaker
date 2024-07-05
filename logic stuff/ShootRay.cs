using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapMaker
{
    public class ShootRay : LogicGate
    {
        //prefabs
        public static GameObject GrowGameObjectPrefab;
        public static GameObject StrinkGameObjectPrefab;
        //general stuff
        public RayType rayType;
        public Fix VarenceInDegrees = Fix.Zero;
        //blink stuff
        public Fix BlinkMinPlayerDuration = (Fix)0.5;
        public Fix BlinkWallDuration = (Fix)3;
        public Fix BlinkWallDelay = (Fix)1;
        public Fix BlinkWallShake = (Fix)1;
        //grow/strink stuff
        public Fix blackHoleGrowth = (Fix)50;
        public Fix ScaleMultiplyer = (Fix)0.8;
        public Fix PlayerMultiplyer = (Fix)0.8;
        public Fix smallNonPlayersMultiplier = (Fix)0.8;
        //private stuff
        private bool WasOnLastTick = false;
        private FixTransform fixTransform;
        private ShootScaleChange shootScaleChange = null;
        public void Awake()
        {
            Debug.Log($"gameObject is {gameObject}");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Debug.Log($"{gameObject.name} Awake in {SceneManager.GetActiveScene().name}");
        }
        void OnDestroy()
        {
            Debug.Log($"{gameObject.name} Destroyed in {SceneManager.GetActiveScene().name}");
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            fixTransform = gameObject.GetComponent<FixTransform>();
            SignalSystem.RegisterLogicGate(this);
        }
        public bool IsOn()
        {
            //only activate on rising edge. (dont want to spam spawn blinks/grows/strinks ext or it might lag)
            var OnLastTick = WasOnLastTick;
            WasOnLastTick = InputSignals[0].IsOn;
            return InputSignals[0].IsOn && !OnLastTick;
        }
        public override void Logic(Fix SimDeltaTime)
        {
            if (IsOn())
            {
                var pos = fixTransform.position;
                var VarenceInRadens = VarenceInDegrees * (Fix)PhysTools.DegreesToRadians;
                //random fix acts odd when the first input is negitive.
                var Varence = Updater.RandomFix(Fix.Zero, VarenceInRadens * (Fix)2) - VarenceInRadens;
                //rot is in radiens
                var rot = fixTransform.rotation + Varence;
                var rotVec = new Vec2(rot);
                switch (rayType) 
                {
                    case RayType.Blink:
                        var hasFired = false;
                        GetComponent<ShootBlink>().minPlayerDuration = BlinkMinPlayerDuration;
                        GetComponent<ShootBlink>().WallDuration = BlinkWallDuration;
                        GetComponent<ShootBlink>().WallDelay = BlinkWallDelay;
                        GetComponent<ShootBlink>().WallShake = BlinkWallShake;
                        GetComponent<ShootBlink>().Shoot(pos, rotVec, ref hasFired);
                        break;
                    case RayType.Grow:
                        ShootGrow();
                        break;
                    case RayType.Shrink:
                        ShootStrink();
                        break;
                }
            }
        }
        public enum RayType
        {
            Blink,
            Grow,
            Shrink
        }
        public void ShootGrow()
        {
            if (shootScaleChange == null)
            {
                var ShootScaleChangeComp = GrowGameObjectPrefab.GetComponent<ShootScaleChange>();
                shootScaleChange = CopyComponent<ShootScaleChange>(ShootScaleChangeComp, gameObject);
                shootScaleChange.Awake();
                var scaleChanger = ShootScaleChangeComp.ScaleChangerPrefab;
                scaleChanger.multiplier = ScaleMultiplyer;
                scaleChanger.PlayerMultiplier = PlayerMultiplyer;
                scaleChanger.smallNonPlayersMultiplier = smallNonPlayersMultiplier;
                shootScaleChange.ScaleChangerPrefab = scaleChanger;
            }
            var VarenceInRadens = VarenceInDegrees * (Fix)PhysTools.DegreesToRadians;
            //random fix acts odd when the first input is negitive.
            var Varence = Updater.RandomFix(Fix.Zero, VarenceInRadens * (Fix)2) - VarenceInRadens;
            //rot is in radiens
            var rot = fixTransform.rotation + Varence;
            var rotVec = new Vec2(rot);
            shootScaleChange.blackHoleGrowthInverse01 = Fix.One / blackHoleGrowth;
            bool ignore = false;
            shootScaleChange.Shoot(fixTransform.position, rotVec, ref ignore, 255);
        }
        public void ShootStrink()
        {
            if (shootScaleChange == null)
            {
                var ShootScaleChangeComp = StrinkGameObjectPrefab.GetComponent<ShootScaleChange>();
                shootScaleChange = CopyComponent<ShootScaleChange>(ShootScaleChangeComp, gameObject);
                shootScaleChange.Awake();
                var scaleChanger = ShootScaleChangeComp.ScaleChangerPrefab;
                scaleChanger.multiplier = ScaleMultiplyer;
                scaleChanger.PlayerMultiplier = PlayerMultiplyer;
                scaleChanger.smallNonPlayersMultiplier = smallNonPlayersMultiplier;
                shootScaleChange.ScaleChangerPrefab = scaleChanger;
            }
            var VarenceInRadens = VarenceInDegrees * (Fix)PhysTools.DegreesToRadians;
            //random fix acts odd when the first input is negitive.
            var Varence = Updater.RandomFix(Fix.Zero, VarenceInRadens * (Fix)2) - VarenceInRadens;
            //rot is in radiens
            var rot = fixTransform.rotation + Varence;
            var rotVec = new Vec2(rot);
            shootScaleChange.blackHoleGrowthInverse01 = Fix.One / blackHoleGrowth;
            bool ignore = false;
            shootScaleChange.Shoot(fixTransform.position, rotVec, ref ignore, 255);
        }
        //https://discussions.unity.com/t/copy-a-component-at-runtime/71172/3
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }
    }
}
