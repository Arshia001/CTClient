using BackgammonLogic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = BackgammonLogic.Color;
using System;
using System.Linq;

namespace Backgammon
{
    //?? I really should implement an animation manager and move all dice/checker animations there so they can be played back sequentially (though animations can also happen at the same time, such as taking a checker)
    public class Board : MonoBehaviour
    {
        Point[] Points;
        Bar[] Bars;
        BearOff[] BearOffs;

        public GameObject Checker;
        public GameObject SelectionMarker;
        public GameObject PossiblePlayMarker;

        Checker HoveredChecker;

        bool Initialized;
        BackgammonGameLogic InitCachedGame;
        Color InitCachedMyColor;
        IEnumerable<int> CachedWhiteDeco, CachedBlackDeco;


        void Start()
        {
            if (InitCachedGame != null)
            {
                InitializeBoard_Impl(InitCachedGame, InitCachedMyColor, CachedWhiteDeco, CachedBlackDeco);
                InitCachedGame = null;
            }

            Initialized = true;
        }

        void FindCheckerStores(Color MyColor)
        {
            if (Points != null && Bars != null && BearOffs != null)
                return;

            Points = new Point[24];
            int Total = 0;
            foreach (var P in GetComponentsInChildren<Point>())
            {
                if (P.Index < 0 || P.Index > 23)
                    Debug.LogError($"Invalid point index {P.Index} on {P.name}");
                if (MyColor == Color.Black)
                    P.Index = 23 - P.Index;
                if (Points[P.Index] != null)
                    Debug.LogError($"Duplicate point index {P.Index} between {P.name} and {Points[P.Index].name}");

                Points[P.Index] = P;
                ++Total;
            }
            if (Total < 24)
                Debug.LogError($"Too few points {Total}, expected 24");

            BearOffs = new BearOff[2];
            Total = 0;
            foreach (var B in GetComponentsInChildren<BearOff>())
            {
                B.Color = B.Self ? MyColor : MyColor.Flip();
                var Idx = B.Color.AsIndex();
                if (BearOffs[Idx] != null)
                    Debug.LogError($"Duplicate BearOff color {B.Color} between {B.name} and {BearOffs[Idx].name}");

                BearOffs[Idx] = B;
                ++Total;
            }
            if (Total < 2)
                Debug.LogError($"Too few bearoffs {Total}, expected 2");

            Bars = new Bar[2];
            Total = 0;
            foreach (var B in GetComponentsInChildren<Bar>())
            {
                B.Color = B.Self ? MyColor : MyColor.Flip();
                var Idx = B.Color.AsIndex();
                if (Bars[Idx] != null)
                    Debug.LogError($"Duplicate Bar color {B.Color} between {B.name} and {Bars[Idx].name}");

                Bars[Idx] = B;
                ++Total;
            }
            if (Total < 2)
                Debug.LogError($"Too few bars {Total}, expected 2");
        }

        void AddChecker(CheckerStore CS, Color Color, IEnumerable<int> Customizations)
        {
            var GO = Instantiate(Checker);
            GO.transform.parent = transform;
            GO.GetComponent<CheckerDeco>().UpdateDeco(Color, Customizations);
            GO.GetComponent<Checker>().Initialize(CS);
        }

        void InitializeBoard_Impl(BackgammonGameLogic Game, Color MyColor, IEnumerable<int> WhiteDeco, IEnumerable<int> BlackDeco)
        {
            var MeshSpawn = transform.Find("MeshSpawn");
            for (int Idx = MeshSpawn.childCount - 1; Idx >= 0; --Idx)
                Destroy(MeshSpawn.GetChild(Idx).gameObject);

            var T = TransientData.Instance;
            var MeshGO = Instantiate(Resources.Load<GameObject>($"Backgammon/Board/{T.Games[T.CurrentGameID].BoardID}"));
            MeshGO.transform.parent = MeshSpawn;
            MeshGO.transform.localPosition = Vector3.zero;
            MeshGO.transform.localRotation = Quaternion.identity;

            FindCheckerStores(MyColor);

            foreach (var P in Points)
                P.Clear();
            foreach (var B in Bars)
                B.Clear();
            foreach (var B in BearOffs)
                B.Clear();

            for (int i = 0; i < 24; ++i)
                if (Game.Board[i] != 0)
                    for (int j = 0; j < Mathf.Abs(Game.Board[i]); ++j)
                        AddChecker(Points[i], Game.Board[i] < 0 ? Color.Black : Color.White, Game.Board[i] < 0 ? BlackDeco : WhiteDeco);

            for (int i = 0; i < Game.Bar[Color.White.AsIndex()]; ++i)
                AddChecker(Bars[Color.White.AsIndex()], Color.White, WhiteDeco);

            for (int i = 0; i < Game.BorneOff[Color.White.AsIndex()]; ++i)
                AddChecker(BearOffs[Color.White.AsIndex()], Color.White, WhiteDeco);

            for (int i = 0; i < Game.Bar[Color.Black.AsIndex()]; ++i)
                AddChecker(Bars[Color.Black.AsIndex()], Color.Black, BlackDeco);

            for (int i = 0; i < Game.BorneOff[Color.Black.AsIndex()]; ++i)
                AddChecker(BearOffs[Color.Black.AsIndex()], Color.Black, BlackDeco);
        }

        public void InitializeBoard(BackgammonGameLogic Game, Color MyColor, IEnumerable<int> WhiteDeco, IEnumerable<int> BlackDeco)
        {
            if (Initialized)
                InitializeBoard_Impl(Game, MyColor, WhiteDeco, BlackDeco);
            else
            {
                InitCachedGame = Game;
                InitCachedMyColor = MyColor;
                CachedWhiteDeco = WhiteDeco;
                CachedBlackDeco = BlackDeco;
            }
        }

        Checker GetCheckerFrom(Color Color, int From)
        {
            CheckerStore FromCS;

            if (From == -1)
                FromCS = Bars[Color.AsIndex()];
            else
                FromCS = Points[From];

            return FromCS.GetAndRemoveTopChecker();
        }

        void MoveChecker_Impl(Color Color, Checker Checker, int To, Action OnAnimStarted)
        {
            CheckerStore ToCS;

            if (To == -1)
                ToCS = BearOffs[Color.AsIndex()];
            else if (To == -2)
                ToCS = Bars[Color.AsIndex()];
            else
                ToCS = Points[To];

            Checker.QueueMoveTo(ToCS, OnAnimStarted);
        }

        public void QueueMoveChecker(Color Color, int From, int To, bool PieceTakenAtTarget)
        {
            var TakenChecker = PieceTakenAtTarget ? GetCheckerFrom(Color.Flip(), To) : null;

            MoveChecker_Impl(Color, GetCheckerFrom(Color, From), To, PieceTakenAtTarget ? () => MoveChecker_Impl(Color.Flip(), TakenChecker, -2, null) : default(Action));
        }

        Checker GetCheckerFrom_Undo(Color Color, int From)
        {
            CheckerStore FromCS;

            if (From == -1)
                FromCS = BearOffs[Color.AsIndex()];
            else
                FromCS = Points[From];

            return FromCS.GetAndRemoveTopChecker();
        }

        void MoveCheckerForUndo_Impl(Color Color, Checker Checker, int To, Action OnAnimStarted)
        {
            CheckerStore ToCS;

            if (To == -1)
                ToCS = Bars[Color.AsIndex()];
            else
                ToCS = Points[To];

            Checker.QueueMoveTo(ToCS, OnAnimStarted);
        }

        public void QueueMoveCheckerForUndo(Color Color, int From, int To, bool PieceTakenAtTarget)
        {
            var TakenChecker = PieceTakenAtTarget ? Bars[Color.Flip().AsIndex()].GetAndRemoveTopChecker() : null;

            MoveCheckerForUndo_Impl(Color, GetCheckerFrom_Undo(Color, From), To, PieceTakenAtTarget ? () => MoveCheckerForUndo_Impl(Color.Flip(), TakenChecker, From, null) : default(Action));
        }

        public bool IsAnyCheckerAnimating()
        {
            return Points.AsEnumerable<CheckerStore>().Concat(BearOffs).Concat(Bars).Any(cs => cs.IsAnyCheckerAnimating());
        }

        public void StartHover(Color Color, int Point)
        {
            HoveredChecker = (Point == -1 ? (CheckerStore)Bars[Color.AsIndex()] : Points[Point]).PeekTopChecker();
            HoveredChecker.StartHover();
        }

        public void EndHover()
        {
            if (HoveredChecker != null)
                HoveredChecker.EndHover();
        }

        public void ShowSelectionMarkers(HashSet<sbyte> Points)
        {
            for (int i = 0; i < 24; ++i)
                if (Points.Contains((sbyte)i))
                    this.Points[i].ShowSelectionMarker();
                else
                    this.Points[i].HideSelectionMarker();

            foreach (var B in Bars)
                if (B.Self)
                {
                    if (Points.Contains(-1))
                        B.ShowSelectionMarker();
                    else
                        B.HideSelectionMarker();
                }
        }

        public void HideSelectionMarkers()
        {
            for (int i = 0; i < 24; ++i)
                Points[i].HideSelectionMarker();

            foreach (var B in Bars)
                B.HideSelectionMarker();
        }

        public void ShowPossiblePlayMarkers(HashSet<Tuple<sbyte, bool>> Points)
        {
            foreach (var P in this.Points)
                P.HidePossiblePlayMarker();

            foreach (var B in BearOffs)
                B.HidePossiblePlayMarker();

            foreach (var P in Points)
            {
                if (P.Item1 == -1)
                {
                    foreach (var B in BearOffs)
                        if (B.Self)
                            B.ShowPossiblePlayMarker(P.Item2);
                }
                else
                    this.Points[P.Item1].ShowPossiblePlayMarker(P.Item2);
            }
        }

        public void HidePossiblePlayMarkers()
        {
            for (int i = 0; i < 24; ++i)
                Points[i].HidePossiblePlayMarker();

            foreach (var B in BearOffs)
                B.HidePossiblePlayMarker();
        }
    }
}