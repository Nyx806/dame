using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace CheckersGame.Shared.Models
{
    [JsonDerivedType(typeof(JoinGameMessage), typeDiscriminator: "JoinGame")]
    [JsonDerivedType(typeof(GameStartedMessage), typeDiscriminator: "GameStarted")]
    [JsonDerivedType(typeof(MakeMoveMessage), typeDiscriminator: "MakeMove")]
    [JsonDerivedType(typeof(MoveResultMessage), typeDiscriminator: "MoveResult")]
    public class GameMessage
    {
        public string MessageType { get; set; }
        public string? GameId { get; set; }
        public string? PlayerId { get; set; }

        public GameMessage(string messageType)
        {
            MessageType = messageType;
        }
    }

    public class JoinGameMessage : GameMessage
    {
        public JoinGameMessage() : base("JoinGame") { }
    }

    public class GameStartedMessage : GameMessage
    {
        public string? Player1Id { get; set; }
        public string? Player2Id { get; set; }
        public PieceColor YourColor { get; set; }

        public GameStartedMessage() : base("GameStarted") { }
    }

    public class MakeMoveMessage : GameMessage
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }

        public MakeMoveMessage() : base("MakeMove") { }
    }

    public class MoveResultMessage : GameMessage
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public BoardState? BoardState { get; set; }
        public PieceColor CurrentPlayer { get; set; }
        public GameState GameState { get; set; }

        public MoveResultMessage() : base("MoveResult") { }
    }

    public class GameStateMessage : GameMessage
    {
        public BoardState BoardState { get; set; }
        public PieceColor CurrentPlayer { get; set; }
        public GameState GameState { get; set; }

        public GameStateMessage() : base("GameState") { }
    }

    public class BoardState
    {
        public List<List<PieceState?>> Pieces { get; set; }

        public BoardState()
        {
            Pieces = new List<List<PieceState?>>();
            for (int r = 0; r < Board.BOARD_SIZE; r++)
            {
                var rowList = new List<PieceState?>();
                for (int c = 0; c < Board.BOARD_SIZE; c++)
                {
                    rowList.Add(null);
                }
                Pieces.Add(rowList);
            }
        }
    }

    public class PieceState
    {
        public PieceType? Type { get; set; }
        public PieceColor? Color { get; set; }
        public int? Row { get; set; }
        public int? Column { get; set; }
    }
} 