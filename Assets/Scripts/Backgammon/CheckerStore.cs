using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Linq;

namespace Backgammon
{
    public abstract class CheckerStore : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float HoldTime = 0.2f;

        protected GameManager Manager { get; private set; }

        Stack<Checker> Checkers = new Stack<Checker>();
        float? PointerDownTime;

        SelectionMarker SelectionMarker;
        PossiblePlayMarker PossiblePlayMarker;


        protected abstract void GetColliderTransform(out Vector3 Position, out Vector2 Scale);
        protected abstract Tuple<Vector3, Quaternion> GetCheckerTransform(int Index);
        protected abstract void OnClick();
        public abstract int? GetFromPointIndex();
        public abstract int? GetToPointIndex();


        void Start()
        {
            Manager = GetComponentInParent<GameManager>();

            var ColliderGO = new GameObject("Collider");
            Vector3 Position;
            Vector2 Scale;
            GetColliderTransform(out Position, out Scale);
            ColliderGO.transform.position = Position;
            ColliderGO.transform.localScale = new Vector3(Scale.x, 0, Scale.y);
            ColliderGO.AddComponent<BoxCollider>().size = Vector3.one;
            ColliderGO.transform.SetParent(transform, true);

            var Board = GetComponentInParent<Board>();
            SelectionMarker = Instantiate(Board.SelectionMarker).GetComponent<SelectionMarker>();
            SelectionMarker.Init(this);
            PossiblePlayMarker = Instantiate(Board.PossiblePlayMarker).GetComponent<PossiblePlayMarker>();
        }

        void Update()
        {
            if (PointerDownTime != null && Time.time - PointerDownTime > HoldTime)
                EnterHoverMode();
        }

        public Tuple<Vector3, Quaternion> AddChecker(Checker Checker)
        {
            Checkers.Push(Checker);
            return GetFinalCheckerPosition();
        }

        public bool IsAnyCheckerAnimating()
        {
            return Checkers.Any(c => c.IsAnimating());
        }

        public Tuple<Vector3, Quaternion> GetFinalCheckerPosition()
        {
            return GetCheckerTransform(Checkers.Count - 1);
        }

        public Checker GetAndRemoveTopChecker() => Checkers.Count <= 0 ? null : Checkers.Pop();

        public Checker PeekTopChecker() => Checkers.Count <= 0 ? null : Checkers.Peek();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 || eventData.pointerId == 0)
                PointerDownTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (PointerDownTime != null && (eventData.pointerId == -1 || eventData.pointerId == 0))
            {
                PointerDownTime = null;
                OnClick();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (PointerDownTime != null && (eventData.pointerId == -1 || eventData.pointerId == 0))
                EnterHoverMode();
        }

        void EnterHoverMode()
        {
            PointerDownTime = null;
            if (GetFromPointIndex() == null)
                return;

            GameManager.Instance.OnHoverStarted(GetFromPointIndex().Value);
        }

        public void Clear()
        {
            foreach (var C in Checkers)
                C.Die();

            Checkers.Clear();
        }

        public void HideSelectionMarker()
        {
            SelectionMarker.Hide();
        }

        public void ShowSelectionMarker()
        {
            SelectionMarker.Show();
        }

        public void HidePossiblePlayMarker()
        {
            PossiblePlayMarker.Hide();
        }

        public void ShowPossiblePlayMarker(bool IsTakeMove)
        {
            PossiblePlayMarker.ShowAt(GetCheckerTransform(Checkers.Count - (IsTakeMove ? 1 : 0)), IsTakeMove);
        }

#if UNITY_EDITOR
        static Mesh CheckerPreviewMesh;

        void OnDrawGizmosSelected()
        {
            if (CheckerPreviewMesh == null)
            {
                var All = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Backgammon/checker e.FBX");
                CheckerPreviewMesh = All.OfType<Mesh>().Where(m => m.name == "checker060").First();
            }

            Gizmos.color = Color.white;
            for (int i = 0; i < 15; ++i)
            {
                var Pos = GetCheckerTransform(i);
                Gizmos.DrawMesh(CheckerPreviewMesh, Pos.Item1, Pos.Item2 * transform.rotation, transform.lossyScale);
            }

            Vector3 Position;
            Vector2 Scale;
            GetColliderTransform(out Position, out Scale);
            Gizmos.DrawWireCube(Position, new Vector3(Scale.x, 0, Scale.y));
        }
#endif
    }
}
