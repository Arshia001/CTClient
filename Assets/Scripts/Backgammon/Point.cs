using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Backgammon
{
    public class Point : CheckerStore
    {
        public int Index;
        public bool ForwardIsDown;
        public float CheckerDistance;
        public float CheckerHeight;
        public Vector2 ColliderMargin;


        protected override void GetColliderTransform(out Vector3 Position, out Vector2 Scale)
        {
            Position = transform.position + 2 * (Index < 12 ? Vector3.back : Vector3.forward) * CheckerDistance;
            Scale = new Vector2(CheckerDistance + 2 * ColliderMargin.x, 5 * CheckerDistance + 2 * ColliderMargin.y);
        }

        protected override Tuple<Vector3, Quaternion> GetCheckerTransform(int Index)
        {
            if (Index <= 4)
                return new Tuple<Vector3, Quaternion>(transform.position + Index * (ForwardIsDown ? Vector3.back : Vector3.forward) * CheckerDistance, Quaternion.identity);
            else if (Index <= 8)
                return new Tuple<Vector3, Quaternion>(transform.position + (Index - 4.5f) * (ForwardIsDown ? Vector3.back : Vector3.forward) * CheckerDistance + Vector3.up * CheckerHeight, Quaternion.identity);
            else if (Index <= 11)
                return new Tuple<Vector3, Quaternion>(transform.position + (Index - 8.0f) * (ForwardIsDown ? Vector3.back : Vector3.forward) * CheckerDistance + Vector3.up * 2 * CheckerHeight, Quaternion.identity);
            else if (Index <= 13)
                return new Tuple<Vector3, Quaternion>(transform.position + (Index - 10.5f) * (ForwardIsDown ? Vector3.back : Vector3.forward) * CheckerDistance + Vector3.up * 3 * CheckerHeight, Quaternion.identity);
            else
                return new Tuple<Vector3, Quaternion>(transform.position + 2 * (ForwardIsDown ? Vector3.back : Vector3.forward) * CheckerDistance + Vector3.up * 4 * CheckerHeight, Quaternion.identity);
        }

        protected override void OnClick()
        {
            Manager.OnPointClicked(Index);
        }

        public override int? GetFromPointIndex()
        {
            return Index;
        }

        public override int? GetToPointIndex()
        {
            return Index;
        }
    }
}
