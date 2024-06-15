using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class ShootRay : LogicGate
    {
        public RayType rayType;
        public Fix VarenceInDegrees = Fix.Zero;
        //blink stuff
        public Fix BlinkMinPlayerDuration = (Fix)0.5;
        public Fix BlinkWallDuration = (Fix)3;
        public Fix BlinkWallDelay = (Fix)1;
        public Fix BlinkWallShake = (Fix)1;
        //private stuff
        private bool WasOnLastTick = false;
        private FixTransform fixTransform;
        public void Register()
        {
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

                }
            }
        }
        public enum RayType
        {
            Blink,
            Grow,
            Shrink
        }
    }
}
