using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker
{
    public class InstantiateFixedBetter : MonoBehaviour
    {
        public static GameObject InstantiateFixed(GameObject prefab, Vector3 pos)
        {
            Updater.BeginRegisterPrefab();
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, pos, Quaternion.identity);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
            Updater.EndRegisterPrefab();
            FixTransform component = gameObject.GetComponent<FixTransform>();
            var pos2 = new Vec2((Fix)pos.x, (Fix)pos.y);
            component.position = pos2;
            component.rotationInner = Fix.Zero;
            component.upInner = Vec2.up;
            component.rightInner = Vec2.right;
            return gameObject;
        }
    }
}
