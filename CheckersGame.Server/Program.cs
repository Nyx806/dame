using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckersGame.Shared.Models;

namespace CheckersGame.Server
{
    class Program
    {
        private static readonly Dictionary<string, Game> activeGames = new Dictionary<string, Game>();
        private static readonly Dictionary<string, WebSocket> playerConnections = new Dictionary<string, WebSocket>();
        private static readonly object lockObject = new object();

        static async Task Main(string[] args)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:5001/");
            httpListener.Start();

            Console.WriteLine("Server started at http://localhost:5001/");

            while (true)
            {
                var context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    _ = HandleWebSocketConnection(webSocketContext.WebSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private static async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var playerId = Guid.NewGuid().ToString();

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var message = JsonSerializer.Deserialize<GameMessage>(messageJson);

                        if (message is JoinGameMessage)
                        {
                            await HandleJoinGame(playerId, webSocket);
                        }
                        else if (message is MakeMoveMessage moveMessage)
                        {
                            await HandleMakeMove(playerId, moveMessage);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandlePlayerDisconnect(playerId);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling WebSocket connection: {ex.Message}");
                await HandlePlayerDisconnect(playerId);
            }
        }

        private static async Task HandleJoinGame(string playerId, WebSocket webSocket)
        {
            Game? gameToSendMessages = null;
            string? player1IdToSend = null;
            string? player2IdToSend = null;
            PieceColor player1Color = default;
            PieceColor player2Color = default;
            bool isNewGame = false;

            lock (lockObject)
            {
                playerConnections[playerId] = webSocket;
                Console.WriteLine($"Player {playerId} connected.");

                Game? game = null;

                // Chercher une partie en attente avec un seul joueur
                foreach (var activeGame in activeGames.Values)
                {
                    // Assurez-vous que Player1Id n'est pas null et que Player2Id est null
                    if (activeGame.State == GameState.WaitingForPlayers && activeGame.Player1Id != null && activeGame.Player2Id == null)
                    {
                        game = activeGame;
                        break;
                    }
                }

                if (game == null)
                {
                    // Aucune partie en attente, créer une nouvelle partie et attendre un second joueur
                    game = new Game(Guid.NewGuid().ToString());
                    activeGames[game.GameId] = game;
                    game.Player1Id = playerId; // Le premier joueur est Player1
                    Console.WriteLine($"New game {game.GameId} created by player {playerId}.");

                    gameToSendMessages = game;
                    player1IdToSend = playerId;
                    player1Color = game.GetPlayerColor(playerId);
                    isNewGame = true;
                }
                else
                {
                    // Une partie en attente trouvée, ce joueur est le second joueur
                    game.Player2Id = playerId; // Le second joueur est Player2
                    Console.WriteLine($"Player {playerId} joined game {game.GameId}. Game starting.");
                    game.StartGame(game.Player1Id, game.Player2Id); // Démarrer la partie avec les deux joueurs

                    gameToSendMessages = game;
                    player1IdToSend = game.Player1Id;
                    player2IdToSend = game.Player2Id;
                    player1Color = game.GetPlayerColor(game.Player1Id);
                    player2Color = game.GetPlayerColor(game.Player2Id);
                    isNewGame = false;
                }
            } // End of lock block

            if (gameToSendMessages != null)
            {
                if (isNewGame)
                {
                    // Envoyer GameStartedMessage au premier joueur
                    await SendGameStartedMessage(gameToSendMessages, player1IdToSend!, player1Color);

                    // Envoyer l'état initial du jeu au premier joueur (l'état est toujours WaitingForPlayers)
                    await SendGameState(gameToSendMessages, player1IdToSend!);
                }
                else
                {
                    // Envoyer GameStartedMessage et l'état mis à jour aux deux joueurs
                    if (player1IdToSend != null)
                    {
                        await SendGameStartedMessage(gameToSendMessages, player1IdToSend, player1Color);
                        Console.WriteLine($"[SERVER] About to send GameState to Player1: {player1IdToSend}");
                        await SendGameState(gameToSendMessages, player1IdToSend);
                    }
                    if (player2IdToSend != null)
                    {
                        await SendGameStartedMessage(gameToSendMessages, player2IdToSend, player2Color);
                        Console.WriteLine($"[SERVER] About to send GameState to Player2: {player2IdToSend}");
                        await SendGameState(gameToSendMessages, player2IdToSend);
                    }
                }
            }
        }

        private static async Task HandleMakeMove(string playerId, MakeMoveMessage moveMessage)
        {
            lock (lockObject)
            {
                if (!activeGames.TryGetValue(moveMessage.GameId ?? "", out Game? game))
                {
                    _ = SendErrorMessage(playerId, "Game not found");
                    return;
                }

                bool success = game.MakeMove(playerId, moveMessage.FromRow, moveMessage.FromCol, moveMessage.ToRow, moveMessage.ToCol);

                if (success)
                {
                    // Envoyer l'état mis à jour à tous les joueurs
                    if (game.Player1Id != null)
                        _ = SendGameState(game, game.Player1Id);
                    if (game.Player2Id != null)
                        _ = SendGameState(game, game.Player2Id);
                }
                else
                {
                    _ = SendErrorMessage(playerId, "Invalid move");
                }
            }
        }

        private static async Task HandlePlayerDisconnect(string playerId)
        {
            lock (lockObject)
            {
                if (playerConnections.ContainsKey(playerId))
                {
                    playerConnections.Remove(playerId);
                }

                // Trouver et mettre à jour les parties affectées
                foreach (var game in activeGames.Values)
                {
                    if (game.Player1Id == playerId || game.Player2Id == playerId)
                    {
                        if (game.State != GameState.Finished)
                        {
                            game.SetState(GameState.Finished);
                        }
                        if (game.Player1Id != null)
                            _ = SendGameState(game, game.Player1Id);
                        if (game.Player2Id != null)
                            _ = SendGameState(game, game.Player2Id);
                    }
                }
            }
        }

        private static async Task SendGameState(Game game, string playerId)
        {
            if (!playerConnections.TryGetValue(playerId, out WebSocket? webSocket) || webSocket == null)
                return;

            var message = new GameStateMessage
            {
                GameId = game.GameId,
                PlayerId = playerId,
                BoardState = CreateBoardState(game.Board),
                CurrentPlayer = game.CurrentPlayer,
                GameState = game.State
            };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var messageJson = JsonSerializer.Serialize(message, options);
            Console.WriteLine($"[SERVER] Sending GameStateMessage to {playerId}: {messageJson}");
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"[SERVER] GameStateMessage sent to {playerId}");
        }

        private static async Task SendErrorMessage(string playerId, string errorMessage)
        {
            if (!playerConnections.TryGetValue(playerId, out WebSocket? webSocket) || webSocket == null)
                return;

            var message = new MoveResultMessage
            {
                Success = false,
                ErrorMessage = errorMessage
            };

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static BoardState CreateBoardState(Board board)
        {
            var boardState = new BoardState();
            Console.WriteLine("Creating BoardState...");

            for (int row = 0; row < Board.BOARD_SIZE; row++)
            {
                for (int col = 0; col < Board.BOARD_SIZE; col++)
                {
                    var piece = board.GetPiece(row, col);
                    if (piece != null)
                    {
                        boardState.Pieces[row][col] = new PieceState
                        {
                            Type = piece.Type,
                            Color = piece.Color,
                            Row = piece.Row,
                            Column = piece.Column
                        };
                        Console.WriteLine($"  Piece found at ({row},{col}): {piece.Color} {piece.Type}");
                    } else {
                        boardState.Pieces[row][col] = null;
                    }
                }
            }
            Console.WriteLine($"BoardState created with {boardState.Pieces.SelectMany(x => x).Count(p => p != null)} pieces.");
            return boardState;
        }

        // Nouvelle méthode pour envoyer GameStartedMessage
        private static async Task SendGameStartedMessage(Game game, string playerId, PieceColor playerColor)
        {
            if (!playerConnections.TryGetValue(playerId, out WebSocket? webSocket) || webSocket == null)
                return;

            var message = new GameStartedMessage
            {
                GameId = game.GameId,
                PlayerId = playerId,
                Player1Id = game.Player1Id,
                Player2Id = game.Player2Id,
                YourColor = playerColor
            };

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
