using BackgammonLogic;
using LightMessage.Common.Util;
using Network;
using Network.Backgammon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Color = BackgammonLogic.Color;

namespace Backgammon
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }


        Color MyColor;
        BackgammonGameLogic Game;
        Dice Dice;
        Board Board;
        BackgammonGameEndPoint EndPoint;

        int? SelectedPoint;
        bool IsCommunicatingWithServer;
        bool WaitingToShowOwnTurnNotification;

        TimeSpan TotalTurnTime;
        DateTime? ThisTurnEndTime;

        bool ResultMyWin;
        bool EndingTurnDueToNoMoves;
        bool SendingRollDiceToServer;

        TKSwipeRecognizer SwipeRecognizer;


        void Awake()
        {
            Instance = this;

            Input.simulateMouseWithTouches = true;

            Board = GetComponent<Board>();
            Dice = GetComponentInChildren<Dice>();

#if UNITY_EDITOR
            if (ConnectionManager.Instance == null)
            {
                SceneManager.LoadScene("Startup");
                return;
            }
#endif

            EndPoint = ConnectionManager.Instance.EndPoint<BackgammonGameEndPoint>();

            EndPoint.OnStartGame += OnStartGame;
            EndPoint.OnInitDiceRolled += OnInitDiceRolled;
            EndPoint.OnMatchResult += OnMatchResult;
            EndPoint.OnOpponentMoved += OnOpponentMoved;
            EndPoint.OnForcedOwnMove += OnForcedOwnMove;
            EndPoint.OnStartTurn += OnStartTurn;
            EndPoint.OnDiceRolled += OnDiceRolled;
            EndPoint.OnEmote += OnEmote;
            EndPoint.OnOpponentUndo += OnOpponentUndo;
        }

        void Start()
        {
            SendReady();

            BackgammonUI.Instance.SetTimers(false, false, 1);

            var Prof = TransientData.Instance.UserProfile;
            var Opp = TransientData.Instance.OpponentInfo;
            if (Opp != null)
                BackgammonUI.Instance.SetPlayerDetails(Prof.Name ?? "", Prof.ActiveItems, Opp.Name, Opp.ActiveItems);

            SwipeRecognizer = new TKSwipeRecognizer(TKSwipeDirection.Right);
            SwipeRecognizer.gestureRecognizedEvent += OnRightSwipe;
            TouchKit.addGestureRecognizer(SwipeRecognizer);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            EndPoint.OnStartGame -= OnStartGame;
            EndPoint.OnInitDiceRolled -= OnInitDiceRolled;
            EndPoint.OnMatchResult -= OnMatchResult;
            EndPoint.OnOpponentMoved -= OnOpponentMoved;
            EndPoint.OnForcedOwnMove -= OnForcedOwnMove;
            EndPoint.OnStartTurn -= OnStartTurn;
            EndPoint.OnDiceRolled -= OnDiceRolled;
            EndPoint.OnEmote -= OnEmote;
            EndPoint.OnOpponentUndo -= OnOpponentUndo;

            TouchKit.removeGestureRecognizer(SwipeRecognizer);
        }

        void Update()
        {
            var ui = BackgammonUI.Instance;

            if (Game != null && ThisTurnEndTime != null)
                ui.SetTimers(Game.Turn == MyColor, Game.Turn != MyColor, (float)((ThisTurnEndTime.Value - DateTime.Now).TotalMilliseconds / TotalTurnTime.TotalMilliseconds));

            if (Game == null || Game.Turn != MyColor)
                ui.SetActionButtons(false, false);
            else
                ui.SetActionButtons(Game.CanUndo(), Game.CanEndTurn() && !EndingTurnDueToNoMoves);

            if (WaitingToShowOwnTurnNotification && CanShowOwnTurnNotification())
            {
                HandleOwnTurnStarted();
                WaitingToShowOwnTurnNotification = false;
            }
        }

        void HandleOwnTurnStarted()
        {
            BackgammonUI.Instance.ShowOwnTurnStarted();
            BackgammonUI.Instance.ShowDiceRollPrompt();
        }

        async void SendReady()
        {
            switch (await ConnectionManager.Instance.EndPoint<MatchmakingEndPoint>().ClientReady())
            {
                case ReadyResponse.OK:
                    if (Game == null) // not started yet
                        BackgammonUI.Instance.ShowWaitingForOpponent();
                    else
                        BackgammonUI.Instance.HideWaitingScreen();
                    break;

                case ReadyResponse.AlreadyInProgress:
                    Debug.LogWarning("Game already in progress, syncing state");
                    HandleOutOfSync();
                    break;

                case ReadyResponse.NotInGame:
                    MainThreadDispatcher.Instance.Enqueue(() => SceneManager.LoadScene("Menu"));
                    break;
            }
        }

        //public void RestoreState()
        //{
        //    if (Game == null)
        //        HandleOutOfSync();
        //}

        void HandleOutOfSync()
        {
            if (Game != null)
            {
                Debug.LogWarning("Resyncing game state with server");
                Game = null;
            }

            IsCommunicatingWithServer = true;
            BackgammonUI.Instance.ShowConnectingToGame();

            EndPoint.GetGameState().ContinueWith(t =>
            {
                try
                {
                    var State = t.Result;

                    TransientData.Instance.CurrentGameID = State.GameID;

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (State.GameState != null)
                        {
                            var NewGame = new BackgammonGameLogic();
                            NewGame.RestoreGameState(State.GameState.Turn, State.GameState.State, State.GameState.Board, State.GameState.Bar, State.GameState.BorneOff, State.GameState.RolledDice, State.GameState.RemainingDice, State.GameState.Stamp, State.GameState.CurrentTurnMoves);

                            Game = NewGame;
                            MyColor = State.MyColor;
                            Dice.OwnColor = MyColor;
                            ThisTurnEndTime = DateTime.Now + State.RemainingTime;
                            TotalTurnTime = State.RemainingTime;

                            Board.InitializeBoard(Game, MyColor,
                                MyColor == Color.White ? TransientData.Instance.UserProfile.ActiveItems : State.OpponentInfo.ActiveItems,
                                MyColor == Color.Black ? TransientData.Instance.UserProfile.ActiveItems : State.OpponentInfo.ActiveItems);

                            //?? handle display of remaining dice
                            if (State.GameState.State == GameState.WaitDice)
                            {
                                if (State.GameState.Turn == MyColor)
                                    HandleOwnTurnStarted();
                            }
                            else
                                Dice.RollTo(State.GameState.Turn, State.GameState.RolledDice[0], State.GameState.Turn, State.GameState.RolledDice[1]);

                            Board.HidePossiblePlayMarkers();
                            Board.HideSelectionMarkers();

                            if (Game.Turn == MyColor && Game.State == GameState.WaitMove)
                                Board.ShowSelectionMarkers(Game.GetPossibleSelections());

                            var Prof = TransientData.Instance.UserProfile;
                            BackgammonUI.Instance.SetPlayerDetails(Prof.Name ?? "", Prof.ActiveItems, State.OpponentInfo.Name ?? "", State.OpponentInfo.ActiveItems);

                            IsCommunicatingWithServer = false;
                            BackgammonUI.Instance.HideWaitingScreen();
                        }
                        else
                        {
                            // Just set opponent info and wait for start of match as usual
                            //?? once we have a loading screen in, we should delay hiding it here
                            TransientData.Instance.OpponentInfo = State.OpponentInfo;
                        }
                    });
                }
                catch (Exception Ex)
                {
                    Debug.LogException(Ex);
                    MainThreadDispatcher.Instance.Enqueue(() => SceneManager.LoadScene("Menu")); //?? loading anim? error message?
                }
            });
        }

        void ProcessMove(sbyte From, sbyte To)
        {
            bool Taken;
            var Moves = Game.GetMoveList(From, To);
            if (Moves == null)
            {
                Debug.Log("Invalid move");
            }
            else
            {
                var Steps = new List<Tuple<sbyte, sbyte, bool>>();
                var InitStamp = Game.Stamp;
                foreach (var M in Moves)
                {
                    if (!Game.MakeMove(From, M, out Taken).IsSuccess())
                    {
                        Debug.LogError("Failed to make move while processing list returned from GameLogic itself, something is very wrong");
                        HandleOutOfSync();
                        return;
                    }
                    Steps.Add(new Tuple<sbyte, sbyte, bool>(From, M, Taken));
                    From = M;
                }

                var FinalStamp = Game.Stamp;

                foreach (var M in Steps)
                    Board.QueueMoveChecker(MyColor, M.Item1, M.Item2, M.Item3);

                //?? handle display of remaining dice

                IsCommunicatingWithServer = true;
                EndPoint.MakeMove(Steps.Select(t => new Tuple<sbyte, sbyte>(t.Item1, t.Item2)), InitStamp).Then(t =>
                {
                    if (t.Result != FinalStamp)
                    {
                        Debug.LogError($"Out of sync w/ server, server stamp {t.Result}, local stamp {FinalStamp}");
                        HandleOutOfSync();
                        return;
                    }

                    IsCommunicatingWithServer = false;
                }, ex => Debug.LogException(ex));
            }

            SelectedPoint = null;

            if (Game.Turn == MyColor && Game.State == GameState.WaitMove)
                Board.ShowSelectionMarkers(Game.GetPossibleSelections());
            else
                ThisTurnEndTime = null; // Stop timers while we receive next turn's data from server
        }

        public bool IsInteractionAllowed()
        {
            return Game != null && Game.Turn == MyColor && Game.State == GameState.WaitMove && !IsCommunicatingWithServer && Dice.IsAnimDone();
        }

        public bool CanShowOwnTurnNotification()
        {
            return Game != null && Game.Turn == MyColor && Game.State == GameState.WaitDice && !IsCommunicatingWithServer && !Board.IsAnyCheckerAnimating() && Dice.IsAnimDone();
        }

        public void EndTurn()
        {
            if (!IsInteractionAllowed())
                return;

            EndTurnInternal();
        }

        async void EndTurnInternal()
        {
            var stamp = Game.Stamp;

            if (Game.CanEndTurn())
            {
                IsCommunicatingWithServer = true;

                var success = await EndPoint.EndTurn(stamp);
                if (!success)
                {
                    Debug.LogError($"Out of sync w/ server on end turn");
                    HandleOutOfSync();
                    return;
                }

                IsCommunicatingWithServer = false;
            }
        }

        void ProcessClick(int Point)
        {
            if (!IsInteractionAllowed())
                return;

            if (SelectedPoint == null)
            {
                if (Game.GetPossibleSelections()?.Contains((sbyte)Point) ?? false)
                {
                    SelectedPoint = Point;
                    Board.ShowSelectionMarkers(new HashSet<sbyte>(new sbyte[] { (sbyte)Point }));
                    Board.ShowPossiblePlayMarkers(Game.GetPossibleMoves((sbyte)SelectedPoint.Value));
                }
            }
            else
            {
                Board.HidePossiblePlayMarkers();
                Board.HideSelectionMarkers();
                ProcessMove((sbyte)SelectedPoint.Value, (sbyte)Point);
            }
        }

        public void OnPointClicked(int Point)
        {
            ProcessClick(Point);
        }

        public void OnBarClicked(Color Color)
        {
            if (Color != MyColor)
                return;

            if (SelectedPoint != null)
                return;

            ProcessClick(-1);
        }

        public void OnBearOffClicked(Color Color)
        {
            if (Color != MyColor)
                return;

            if (SelectedPoint == null)
                return;

            ProcessClick(-1);
        }

        public void OnHoverStarted(int Point)
        {
            if (!IsInteractionAllowed())
                return;

            if (Point == -1 && Game.Bar[MyColor.AsIndex()] <= 0) // Bar
                return;
            else if (Point != -1 && (Game.Board[Point] == 0 || Mathf.Sign(Game.Board[Point]) != MyColor.AsSign()))
                return;

            SelectedPoint = Point;
            Board.ShowSelectionMarkers(new HashSet<sbyte>(new sbyte[] { (sbyte)Point }));
            Board.ShowPossiblePlayMarkers(Game.GetPossibleMoves((sbyte)SelectedPoint.Value));
            Board.StartHover(MyColor, Point);
        }

        public void OnHoverFinished(int? Point)
        {
            if (Point == null)
            {
                SelectedPoint = null;
                Board.ShowSelectionMarkers(Game.GetPossibleSelections());
            }
            else if (SelectedPoint != null) // Just as a safe-guard, shouldn't happen
            {
                ProcessMove((sbyte)SelectedPoint.Value, (sbyte)Point.Value);
                Board.HidePossiblePlayMarkers();
            }

            Board.EndHover();
        }

        public async void Undo()
        {
            if (!IsInteractionAllowed())
                return;

            var stamp = Game.Stamp;

            var result = Game.UndoLastMove();
            if (!result.IsSuccess())
                return;

            Board.HidePossiblePlayMarkers();
            Board.HideSelectionMarkers();
            Board.EndHover();
            SelectedPoint = null;

            Board.QueueMoveCheckerForUndo(MyColor, result.Move.Value.To, result.Move.Value.From, result.Move.Value.PieceTaken);

            IsCommunicatingWithServer = true;
            if (!await EndPoint.UndoLastMove(stamp))
            {
                Debug.LogError($"failed to undo, local result was {result.Result}");
                HandleOutOfSync();
                return;
            }
            IsCommunicatingWithServer = false;

            Board.ShowSelectionMarkers(Game.GetPossibleSelections());
        }

        public void Concede()
        {
            DialogBox.Instance.ShowTwoButton("دست قبول", $"می‌خوای {(Game.BorneOff[MyColor.AsIndex()] == 0 ? "مارس" : "دست")} رو قبول کنی؟", () =>
            {
                EndPoint.Concede().DontCare();
            }, null);
        }

        private void OnStartTurn(bool My, byte Stamp, TimeSpan TurnTime)
        {
            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            EndingTurnDueToNoMoves = false;

            if (Game.Stamp != Stamp)
            {
                Debug.LogError($"stamp mismatch at start of turn, server stamp {Stamp}, own stamp {Game.Stamp}, my turn? {My}");
                HandleOutOfSync();
                return;
            }

            // This also gets called at the start of the game when init dices have been rolled, the condition accounts for that
            if (Game.CanEndTurn())
            {
                var result = Game.EndTurn();
                if (!result.IsSuccess())
                {
                    Debug.LogError($"failed to end turn with result: {result}");
                    HandleOutOfSync();
                    return;
                }
            }

            Debug.Log($"Starting {(My ? "my" : "enemy")} turn");

            ThisTurnEndTime = DateTime.Now.Add(TurnTime);
            TotalTurnTime = TurnTime;

            if (My)
            {
                if (Game.State == GameState.WaitMove) // Start of very first turn, dice were rolled beforehand
                    Board.ShowSelectionMarkers(Game.GetPossibleSelections());
                else
                {
                    if (CanShowOwnTurnNotification())
                        HandleOwnTurnStarted();
                    else
                        WaitingToShowOwnTurnNotification = true;
                }
            }
        }

        private void OnOpponentUndo(byte stamp)
        {
            if (Game.Stamp != stamp)
            {
                Debug.LogError($"stamp mismatch on opponent undo, server stamp {stamp}, own stamp {Game.Stamp}");
                HandleOutOfSync();
                return;
            }

            var result = Game.UndoLastMove();

            if (!result.IsSuccess())
            {
                Debug.LogError($"failed to undo opponent move, server stamp {stamp}, own stamp {Game.Stamp}, undo result {result.Result}");
                HandleOutOfSync();
                return;
            }

            Board.QueueMoveCheckerForUndo(MyColor.Flip(), result.Move.Value.To, result.Move.Value.From, result.Move.Value.PieceTaken);
        }

        void OnEmote(int EmoteID)
        {
            BackgammonUI.Instance.ShowEmote(EmoteID, false);
        }

        bool CanRollDice() => Game.Turn == MyColor && Game.State == GameState.WaitDice;

        public async void RollDice()
        {
            if (SendingRollDiceToServer)
                return;

            SendingRollDiceToServer = true;

            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            Debug.Log("Requesting dice roll");

            if (!CanRollDice())
            {
                Debug.LogError($"Cannot roll in this state, turn {Game.Turn}, state {Game.State}");
                return;
            }

            BackgammonUI.Instance.HideDiceRollPrompt();

            var success = await EndPoint.RollDice(Game.Stamp);

            if (!success)
            {
                SendingRollDiceToServer = false;
                Debug.LogError($"Failed to request to roll dice");
                HandleOutOfSync();
                return;
            }
        }

        void OnRightSwipe(TKSwipeRecognizer obj)
        {
            if (CanRollDice())
                RollDice();
        }

        private void OnDiceRolled(bool My, byte Roll1, byte Roll2, byte Stamp)
        {
            SendingRollDiceToServer = false;

            BackgammonUI.Instance.HideDiceRollPrompt();

            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            RollDiceResult? RollResult = null;
            if (Game.Stamp != Stamp || !(RollResult = Game.ServerDiceRolled(Roll1, Roll2)).Value.IsSuccess())
            {
                Debug.LogError($"Failed to roll dice on server's command, server stamp {Stamp}, own stamp {Game.Stamp}, result is {RollResult}");
                HandleOutOfSync();
                return;
            }

            var Color = My ? MyColor : MyColor.Flip();
            Dice.RollTo(Color, Roll1, Color, Roll2);

            if (Game.Turn == MyColor && Game.State == GameState.WaitMove) // The roll may result in no possible plays
                Board.ShowSelectionMarkers(Game.GetPossibleSelections());
            else
            {
                Board.HideSelectionMarkers();
                Board.HidePossiblePlayMarkers();
            }

            if (Game.Turn == MyColor && Game.CanEndTurn())
            {
                EndingTurnDueToNoMoves = true;
                EndTurnInternal();
            }
        }

        private void OnOpponentMoved(sbyte From, sbyte To, byte Stamp)
        {
            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            bool Taken;
            MakeMoveResult? Res = null;
            if (Stamp != Game.Stamp || !(Res = Game.MakeMove(From, To, out Taken)).Value.IsSuccess())
            {
                Debug.LogError($"Out of sync w/ server, server stamp {Stamp}, own stamp {Game.Stamp}, opponent move resulted in {Res}");
                HandleOutOfSync();
                return;
            }

            Board.QueueMoveChecker(MyColor.Flip(), From, To, Taken);

            //?? handle display of remaining dice
        }

        private void OnForcedOwnMove(sbyte From, sbyte To, byte Stamp)
        {
            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            bool Taken;
            MakeMoveResult? Res = null;
            if (Stamp != Game.Stamp || !(Res = Game.MakeMove(From, To, out Taken)).Value.IsSuccess())
            {
                Debug.LogError($"Out of sync w/ server, server stamp {Stamp}, own stamp {Game.Stamp}, forced own move resulted in {Res}");
                HandleOutOfSync();
                return;
            }

            Board.QueueMoveChecker(MyColor, From, To, Taken);

            //?? handle display of remaining dice
        }

        private void OnMatchResult(bool MyWin, GameOverReason reason, ulong TotalGold, ulong TotalGems, uint TotalXP, uint Level, uint LevelUpDeltaGold, uint LevelUpDeltaGems, uint LevelXP, uint[] UpdatedStatistics)
        {
            var Profile = TransientData.Instance.UserProfile;
            Profile.Gold = TotalGold;
            Profile.Gems = TotalGems;
            Profile.XP = TotalXP;
            Profile.Level = Level;
            Profile.LevelXP = LevelXP;
            Profile.LevelUpDeltaGold = LevelUpDeltaGold;
            Profile.LevelUpDeltaGems = LevelUpDeltaGems;
            Profile.Statistics = UpdatedStatistics;

            ResultMyWin = MyWin;

            if (MyWin && (reason == GameOverReason.Inactivity || reason == GameOverReason.FailedToJoinInTime))
                DialogBox.Instance.ShowOneButton("بردی", "حریف بازی رو ترک کرد", () => ShowEndGameScreen());
            else if (reason == GameOverReason.Concede)
            {
                if (MyWin)
                    DialogBox.Instance.ShowOneButton("بردی", $"حریف {(Game.BorneOff[MyColor.Flip().AsIndex()] == 0 ? "مارس" : "دست")} رو قبول کرد", () => ShowEndGameScreen());
                else
                    ShowEndGameScreen();
            }
            else
                Invoke(nameof(ShowEndGameScreen), 1.0f);
        }

        void ShowEndGameScreen()
        {
            BackgammonUI.Instance.SwitchToEndGameScreen(ResultMyWin);
        }

        public void EndGameScreenFinished()
        {
            SceneManager.LoadScene("Menu");
        }

        private void OnInitDiceRolled(byte My, byte Their, byte Stamp)
        {
            // Still waiting to restore state after reconnection
            if (Game == null)
                return;

            Debug.Log($"init dice: {My}, {Their}");

            if (Game.Stamp != Stamp || !Game.ServerInitDiceRolled(MyColor == Color.White ? My : Their, MyColor == Color.White ? Their : My))
            {
                Debug.LogError($"Failed to roll init dice on server's command, server stamp {Stamp}, own stamp {Game.Stamp}");
                HandleOutOfSync();
                return;
            }

            Dice.RollTo(MyColor, My, MyColor.Flip(), Their);
        }

        private void OnStartGame(byte MyColor, OpponentInfo OpponentInfo, int GameID)
        {
            BackgammonUI.Instance.HideWaitingScreen();

            this.MyColor = MyColor == Color.White.AsIndex() ? Color.White : Color.Black;
            Dice.OwnColor = this.MyColor;
            Debug.Log($"Starting game as {this.MyColor}");
            Game = new BackgammonGameLogic();
            var Tr = TransientData.Instance;
            if (Tr.OpponentInfo == null || Tr.CurrentGameID == 0)
            {
                Tr.OpponentInfo = OpponentInfo;
                Tr.CurrentGameID = GameID;
            }
            Board.InitializeBoard(Game, this.MyColor,
                this.MyColor == Color.White ? Tr.UserProfile.ActiveItems : Tr.OpponentInfo.ActiveItems,
                this.MyColor == Color.Black ? Tr.UserProfile.ActiveItems : Tr.OpponentInfo.ActiveItems);
        }

        public void SendEmote(int ID)
        {
            EndPoint.Emote(ID);
        }
    }
}
