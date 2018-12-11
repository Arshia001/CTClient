using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Color = BackgammonLogic.Color;
using System;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Backgammon
{
    public class BearOff : CheckerStore
    {
        [HideInInspector]
        public Color Color;
        public bool Self;
        public float CheckerDistance;
        public Vector2 ColliderMargin;


        protected override void GetColliderTransform(out Vector3 Position, out Vector2 Scale)
        {
            Position = transform.position + 7 * (Self ? Vector3.forward : Vector3.back) * CheckerDistance;
            Scale = new Vector2(CheckerDistance + 2 * ColliderMargin.x, 15 * CheckerDistance + 2 * ColliderMargin.y);
        }

        protected override Tuple<Vector3, Quaternion> GetCheckerTransform(int Index)
        {
            return new Tuple<Vector3, Quaternion>(transform.position + (Self ? 14 - Index : Index) * (Self ? Vector3.forward : Vector3.back) * CheckerDistance, Quaternion.Euler(-90, 0, 0));
        }

        protected override void OnClick()
        {
            Manager.OnBearOffClicked(Color);
        }

        public override int? GetFromPointIndex()
        {
            return null;
        }

        public override int? GetToPointIndex()
        {
            return -1;
        }
    }
}
