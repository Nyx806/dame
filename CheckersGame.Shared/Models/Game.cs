using System;
using System.Collections.Generic;

namespace CheckersGame.Shared.Models
{
    public class Game
    {
        public Board Board { get; private set; }
        public PieceColor CurrentPlayer { get; private set; }
        public GameState State { get; private set; }
        public string? Player1Id { get; set; }
        public string? Player2Id { get; set; }
        public string GameId { get; }

        public Game(string gameId)
        {
            GameId = gameId;
            Board = new Board();
            CurrentPlayer = PieceColor.White;
            State = GameState.WaitingForPlayers;
        }

        public void SetState(GameState newState)
        {
            State = newState;
        }

        public void StartGame(string player1Id, string? player2Id)
        {
            if (State != GameState.WaitingForPlayers)
                throw new InvalidOperationException("Game is already in progress");

            Player1Id = player1Id;
            Player2Id = player2Id;
            State = GameState.InProgress;
        }

        public bool MakeMove(string playerId, int fromRow, int fromCol, int toRow, int toCol)
        {
            Console.WriteLine($"Game: MakeMove called by PlayerId: {playerId}, From: ({fromRow},{fromCol}), To: ({toRow},{toCol})");
            Console.WriteLine($"Game: Current state: {State}, CurrentPlayer: {CurrentPlayer}");
            Console.WriteLine($"Game: Player1Id: {Player1Id}, Player2Id: {Player2Id}");
            
            if (State != GameState.InProgress)
            {
                Console.WriteLine($"Game: Move invalid - GameState is not InProgress ({State})");
                return false;
            }

            if (!IsPlayerTurn(playerId))
            {
                Console.WriteLine($"Game: Move invalid - Not player's turn. CurrentPlayer: {CurrentPlayer}, PlayerId: {playerId}");
                return false;
            }

            Piece? piece = Board.GetPiece(fromRow, fromCol);
            Console.WriteLine($"Game: Piece at source: {piece?.Color} {piece?.Type}");
            
            if (piece == null || piece.Color != CurrentPlayer)
            {
                Console.WriteLine($"Game: Move invalid - No piece at source or wrong color. Piece: {piece?.Color}, CurrentPlayer: {CurrentPlayer}");
                return false;
            }

            // Essayer d'abord une capture
            Console.WriteLine($"Game: Checking if capture is valid from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            if (Board.IsValidCapture(fromRow, fromCol, toRow, toCol))
            {
                Console.WriteLine($"Game: Valid capture detected from ({fromRow},{fromCol}) to ({toRow},{toCol})");
                try
                {
                    Board.CapturePiece(fromRow, fromCol, toRow, toCol);
                    Console.WriteLine("Game: Capture successful");
                    SwitchPlayer();
                    Console.WriteLine("Game: Player switched after capture");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Game: Error during capture: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Game: No valid capture from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            }

            // Sinon, essayer un mouvement simple
            Console.WriteLine($"Game: Checking if simple move is valid from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            if (Board.IsValidMove(fromRow, fromCol, toRow, toCol))
            {
                Console.WriteLine("Game: Attempting simple move...");
                Board.MovePiece(fromRow, fromCol, toRow, toCol);
                SwitchPlayer();
                Console.WriteLine("Game: Simple move successful.");
                return true;
            }
            else
            {
                Console.WriteLine($"Game: No valid simple move from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            }

            Console.WriteLine("Game: Move invalid - No valid move or capture.");
            return false;
        }

        private void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White;
            CheckGameState();
        }

        private void CheckGameState()
        {
            if (!Board.HasValidMoves(CurrentPlayer))
            {
                State = GameState.Finished;
            }
        }

        public bool IsPlayerTurn(string playerId)
        {
            Console.WriteLine($"Game: Checking if it's player's turn. PlayerId: {playerId}, Player1Id: {Player1Id}, Player2Id: {Player2Id}, CurrentPlayer: {CurrentPlayer}, GameState: {State}");
            if (State != GameState.InProgress)
                return false;

            if (CurrentPlayer == PieceColor.White && playerId == Player1Id)
            {
                Console.WriteLine($"Game: It IS White player's turn.");
                return true;
            }

            if (CurrentPlayer == PieceColor.Black && playerId == Player2Id)
            {
                Console.WriteLine($"Game: It IS Black player's turn.");
                return true;
            }

            Console.WriteLine($"Game: It is NOT player's turn.");
            return false;
        }

        public PieceColor GetPlayerColor(string playerId)
        {
            if (playerId == Player1Id)
                return PieceColor.White;
            if (playerId == Player2Id)
                return PieceColor.Black;
            throw new ArgumentException("Player is not in this game");
        }
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        Finished
    }
} 