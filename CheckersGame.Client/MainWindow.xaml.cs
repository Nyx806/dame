using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CheckersGame.Shared.Models;

namespace CheckersGame.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ClientWebSocket webSocket;
    private string playerId = string.Empty;
    private string gameId = string.Empty;
    private PieceColor playerColor;
    private bool isConnected;
    private const int SQUARE_SIZE = 60;
    private int? selectedRow;
    private int? selectedColumn;
    private BoardState? _currentBoardState;
    private GameStateMessage? _currentGameState;

    public MainWindow()
    {
        InitializeComponent();
        webSocket = new ClientWebSocket();
        this.Loaded += MainWindow_Loaded;
        GameBoard.SizeChanged += GameBoard_SizeChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Forcer le redimensionnement du Canvas
        GameBoard.Width = Math.Min(this.ActualWidth, this.ActualHeight) - 40;
        GameBoard.Height = GameBoard.Width;
        Console.WriteLine($"Client: Window loaded. Canvas size set to {GameBoard.Width}x{GameBoard.Height}");
    }

    private void GameBoard_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Console.WriteLine($"Client: GameBoard SizeChanged. New size: {e.NewSize.Width}x{e.NewSize.Height}");
        if (_currentBoardState != null)
        {
            DrawBoard(_currentBoardState);
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isConnected)
        {
            try
            {
                await webSocket.ConnectAsync(new Uri("ws://localhost:5001"), CancellationToken.None);
                isConnected = true;
                ConnectButton.Content = "Déconnecter";
                StatusText.Text = "Connecté";
                _ = ReceiveMessages();
                await SendJoinGameMessage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion : {ex.Message}");
            }
        }
        else
        {
            await Disconnect();
        }
    }

    private async Task Disconnect()
    {
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
        }
        isConnected = false;
        ConnectButton.Content = "Se connecter";
        StatusText.Text = "Déconnecté";
        GameBoard.Children.Clear();
        GameStatusText.Text = "";
    }

    private async Task SendJoinGameMessage()
    {
        var message = new JoinGameMessage();
        await SendMessage(message);
    }

    private async Task SendMakeMoveMessage(int fromRow, int fromCol, int toRow, int toCol)
    {
        Console.WriteLine($"Client: Sending move message - From: ({fromRow},{fromCol}) To: ({toRow},{toCol})");
        Console.WriteLine($"Client: GameId: {gameId}, PlayerId: {playerId}");
        var message = new MakeMoveMessage
        {
            GameId = gameId,
            PlayerId = playerId,
            FromRow = fromRow,
            FromCol = fromCol,
            ToRow = toRow,
            ToCol = toCol
        };
        await SendMessage(message);
    }

    private async Task SendMessage(GameMessage message)
    {
        var messageJson = JsonSerializer.Serialize(message);
        Console.WriteLine($"Client: Sending message: {messageJson}");
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("Client: Waiting for next message...");
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                Console.WriteLine($"Client: Received message. MessageType: {result.MessageType}, Count: {result.Count}");

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Client: Received RAW JSON: {messageJson}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // Désérialiser d'abord pour obtenir le MessageType
                    GameMessage? baseMessage = null;
                    try
                    {
                        baseMessage = JsonSerializer.Deserialize<GameMessage>(messageJson, options);
                    }
                    catch (Exception deserializeEx)
                    {
                        Console.WriteLine($"Client: Base Deserialization Error: {deserializeEx.Message}");
                        Console.WriteLine($"Client: JSON that failed: {messageJson}");
                        continue; 
                    }

                    if (baseMessage == null || string.IsNullOrEmpty(baseMessage.MessageType))
                    {
                        Console.WriteLine("Client: Base message is null or MessageType is missing.");
                        continue;
                    }

                    Console.WriteLine($"Client: Identified message type: '{baseMessage.MessageType}'");

                    Dispatcher.Invoke(() =>
                    {
                        switch (baseMessage.MessageType)
                        {
                            case "GameStarted":
                                try
                                {
                                    var gameStarted = JsonSerializer.Deserialize<GameStartedMessage>(messageJson, options);
                                    if (gameStarted != null) HandleGameStarted(gameStarted);
                                } catch (Exception ex) { Console.WriteLine($"Client: Error handling GameStartedMessage: {ex.Message}"); }
                                break;
                            case "GameState":
                                Console.WriteLine("Client: Attempting to deserialize GameStateMessage in ReceiveMessages.");
                                try
                                {
                                    var gameState = JsonSerializer.Deserialize<GameStateMessage>(messageJson, options);
                                    if (gameState != null)
                                    {
                                        Console.WriteLine($"Client: Deserialized GameStateMessage. CurrentPlayer: {gameState.CurrentPlayer}, GameState: {gameState.GameState}, BoardState exists: {gameState.BoardState != null}");
                                        HandleGameState(gameState);
                                    }
                                } catch (Exception ex) {
                                    Console.WriteLine($"Client: Error handling GameStateMessage in ReceiveMessages: {ex.Message}");
                                    MessageBox.Show($"Client: Erreur lors du traitement de GameStateMessage : {ex.Message}\nJSON: {messageJson}");
                                }
                                break;
                            case "MoveResult":
                                try
                                {
                                    var moveResult = JsonSerializer.Deserialize<MoveResultMessage>(messageJson, options);
                                    if (moveResult != null) HandleMoveResult(moveResult);
                                } catch (Exception ex) { Console.WriteLine($"Client: Error handling MoveResultMessage: {ex.Message}"); }
                                break;
                            default:
                                Console.WriteLine($"Client: Unhandled specific message type: {baseMessage.MessageType}. Raw JSON: {messageJson}");
                                break;
                        }
                    });
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client: WebSocket close message received.");
                    await Disconnect();
                    break;
                }
                else
                {
                    Console.WriteLine($"Client: Unhandled WebSocket message type: {result.MessageType}");
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Client: General ReceiveMessages Error: {ex.Message}");
                Console.WriteLine($"Client: Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Erreur de réception : {ex.Message}");
                _ = Disconnect();
            });
        }
    }

    private void HandleGameStarted(GameStartedMessage message)
    {
        playerId = message.PlayerId;
        playerColor = message.YourColor;
        gameId = message.GameId;
        GameStatusText.Text = $"Vous jouez les {playerColor}";
    }

    private void HandleGameState(GameStateMessage message)
    {
        Console.WriteLine("Client: Entering HandleGameState.");
        if (message.BoardState != null)
        {
            Console.WriteLine($"Client: Previous board state pieces count: {_currentBoardState?.Pieces.SelectMany(row => row).Count(p => p != null) ?? 0}");
            _currentBoardState = message.BoardState;
            _currentGameState = message;
            Console.WriteLine($"Client: New board state pieces count: {message.BoardState.Pieces.SelectMany(row => row).Count(p => p != null)}");
            Console.WriteLine($"Client: HandleGameState received BoardState with {message.BoardState.Pieces.SelectMany(row => row).Count(p => p != null)} pieces.");
            Console.WriteLine($"Client: HandleGameState - CurrentPlayer: {message.CurrentPlayer}, GameState: {message.GameState}");
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine("Client: Dispatcher.Invoke calling DrawBoard and UpdateGameStatus.");
                DrawBoard(message.BoardState);
                UpdateGameStatus(message);
            });
        }
        else
        {
            Console.WriteLine("Client: HandleGameState received a null BoardState.");
        }
    }

    private void HandleMoveResult(MoveResultMessage message)
    {
        Console.WriteLine($"Client: Entering HandleMoveResult. Success: {message.Success}");
        if (!message.Success)
        {
            MessageBox.Show(message.ErrorMessage);
        }
        else
        {
            Console.WriteLine($"Client: MoveResult success. CurrentPlayer: {message.CurrentPlayer}, GameState: {message.GameState}");
            if (message.BoardState != null)
            {
                Console.WriteLine($"Client: Previous board state pieces count: {_currentBoardState?.Pieces.SelectMany(row => row).Count(p => p != null) ?? 0}");
                _currentBoardState = message.BoardState;
                Console.WriteLine($"Client: New board state pieces count: {message.BoardState.Pieces.SelectMany(row => row).Count(p => p != null)}");
                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine("Client: Dispatcher.Invoke in HandleMoveResult calling DrawBoard and UpdateGameStatus.");
                    DrawBoard(message.BoardState);
                    // Create a GameStateMessage from MoveResultMessage for UpdateGameStatus
                    var gameStateForUpdate = new GameStateMessage
                    {
                        CurrentPlayer = message.CurrentPlayer,
                        GameState = message.GameState,
                        BoardState = message.BoardState
                    };
                    UpdateGameStatus(gameStateForUpdate);
                });
            }
            else
            {
                Console.WriteLine("Client: MoveResult received a null BoardState.");
            }
        }
    }

    private void DrawBoard(BoardState boardState)
    {
        Console.WriteLine($"Client: Entering DrawBoard method. BoardState received: {boardState != null}");
        Console.WriteLine($"Client: GameBoard ActualWidth: {GameBoard.ActualWidth}, ActualHeight: {GameBoard.ActualHeight}");
        GameBoard.Children.Clear();

        // Ajuster la taille du Canvas pour qu'il soit carré
        double boardSize = Math.Min(GameBoard.ActualWidth, GameBoard.ActualHeight) - 20; // 20 pixels de marge
        double squareSize = boardSize / Board.BOARD_SIZE;
        Console.WriteLine($"Client: Calculated Board size: {boardSize}, Square size: {squareSize}");

        // Dessiner le plateau
        for (int row = 0; row < Board.BOARD_SIZE; row++)
        {
            for (int col = 0; col < Board.BOARD_SIZE; col++)
            {
                // Créer un conteneur pour la case et son contenu
                var squareContainer = new Canvas
                {
                    Width = squareSize,
                    Height = squareSize
                };
                Canvas.SetLeft(squareContainer, col * squareSize);
                Canvas.SetTop(squareContainer, row * squareSize);

                // Ajouter l'événement de clic sur le conteneur
                int currentRow = row;
                int currentCol = col;
                squareContainer.MouseLeftButtonDown += (s, e) => HandleSquareClick(currentRow, currentCol);

                // Ajouter le fond de la case
                var square = new Rectangle
                {
                    Width = squareSize,
                    Height = squareSize,
                    Fill = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray
                };
                squareContainer.Children.Add(square);

                // Dessiner la pièce si présente
                var piece = boardState.Pieces[row][col];
                if (piece != null)
                {
                    Console.WriteLine($"Client: Drawing piece at ({row},{col}) Color: {piece.Color} Type: {piece.Type}");
                    
                    var pieceEllipse = new Ellipse
                    {
                        Width = squareSize * 0.8,
                        Height = squareSize * 0.8,
                        Fill = piece.Color == PieceColor.Black ? Brushes.Black : Brushes.White,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 2
                    };

                    // Centrer le pion dans le conteneur
                    Canvas.SetLeft(pieceEllipse, (squareSize - pieceEllipse.Width) / 2);
                    Canvas.SetTop(pieceEllipse, (squareSize - pieceEllipse.Height) / 2);
                    squareContainer.Children.Add(pieceEllipse);

                    // Si c'est une dame, ajouter une couronne
                    if (piece.Type == PieceType.King)
                    {
                        var crown = new TextBlock
                        {
                            Text = "♛",
                            FontSize = squareSize * 0.4,
                            Foreground = piece.Color == PieceColor.Black ? Brushes.White : Brushes.Black,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        // Centrer la couronne dans le conteneur
                        Canvas.SetLeft(crown, (squareSize - crown.FontSize) / 2);
                        Canvas.SetTop(crown, (squareSize - crown.FontSize) / 2);
                        squareContainer.Children.Add(crown);
                    }
                }

                GameBoard.Children.Add(squareContainer);
            }
        }

        // Mettre en évidence la case sélectionnée si une pièce est sélectionnée
        if (selectedRow.HasValue && selectedColumn.HasValue)
        {
            var highlight = new Rectangle
            {
                Width = squareSize,
                Height = squareSize,
                Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)),
                Stroke = Brushes.Green,
                StrokeThickness = 3
            };

            Canvas.SetLeft(highlight, selectedColumn.Value * squareSize);
            Canvas.SetTop(highlight, selectedRow.Value * squareSize);
            GameBoard.Children.Add(highlight);

            // Ajouter un effet de surbrillance sur la pièce sélectionnée
            var piece = boardState.Pieces[selectedRow.Value][selectedColumn.Value];
            if (piece != null)
            {
                var pieceHighlight = new Ellipse
                {
                    Width = squareSize * 0.85,
                    Height = squareSize * 0.85,
                    Stroke = Brushes.Green,
                    StrokeThickness = 3
                };

                Canvas.SetLeft(pieceHighlight, selectedColumn.Value * squareSize + (squareSize - pieceHighlight.Width) / 2);
                Canvas.SetTop(pieceHighlight, selectedRow.Value * squareSize + (squareSize - pieceHighlight.Height) / 2);
                GameBoard.Children.Add(pieceHighlight);
            }
        }

        Console.WriteLine($"Client: Finished drawing board. Total pieces: {boardState.Pieces.SelectMany(row => row).Count(p => p != null)} pieces.");
    }

    private async void HandleSquareClick(int row, int col)
    {
        Console.WriteLine($"Client: HandleSquareClick received - Row: {row}, Col: {col}");
        if (!isConnected || string.IsNullOrEmpty(gameId))
        {
            Console.WriteLine("Client: Cannot handle click - not connected or no game ID");
            return;
        }

        if (_currentBoardState == null)
        {
            Console.WriteLine("Client: Cannot handle click - no current board state");
            return;
        }

        // Vérifier si c'est votre tour
        if (_currentGameState?.CurrentPlayer != playerColor)
        {
            Console.WriteLine("Client: Not your turn");
            MessageBox.Show("Ce n'est pas votre tour de jouer");
            return;
        }

        var pieceAtLocation = _currentBoardState.Pieces[row][col];
        Console.WriteLine($"Client: Piece at location ({row},{col}): {(pieceAtLocation != null ? $"{pieceAtLocation.Color} {pieceAtLocation.Type}" : "None")}");

        // Si une pièce est déjà sélectionnée
        if (selectedRow.HasValue && selectedColumn.HasValue)
        {
            // Si on clique sur une autre pièce de la même couleur, changer la sélection
            if (pieceAtLocation != null && pieceAtLocation.Color == playerColor)
            {
                selectedRow = row;
                selectedColumn = col;
                Console.WriteLine($"Client: Changed selection to - Row: {selectedRow.Value}, Col: {selectedColumn.Value}");
                DrawBoard(_currentBoardState);
                return;
            }

            // Sinon, essayer de faire un mouvement
            Console.WriteLine($"Client: Existing selection ({selectedRow.Value},{selectedColumn.Value}). Attempting to move to ({row},{col})");
            await SendMakeMoveMessage(selectedRow.Value, selectedColumn.Value, row, col);
            selectedRow = null;
            selectedColumn = null;
            Console.WriteLine("Client: Move message sent and selection cleared.");
        }
        else
        {
            // Vérifier si la case contient une pièce et si c'est la bonne couleur
            if (pieceAtLocation != null && pieceAtLocation.Color == playerColor)
            {
                // Sélectionner une nouvelle case
                selectedRow = row;
                selectedColumn = col;
                Console.WriteLine($"Client: New selection - Row: {selectedRow.Value}, Col: {selectedColumn.Value}. Piece: {pieceAtLocation.Color} {pieceAtLocation.Type}");
                DrawBoard(_currentBoardState);
            }
            else
            {
                Console.WriteLine($"Client: Cannot select - No piece or wrong color at ({row},{col})");
            }
        }
    }

    private void UpdateGameStatus(GameStateMessage message)
    {
        if (message.GameState == GameState.Finished)
        {
            GameStatusText.Text = "Partie terminée";
        }
        else if (message.CurrentPlayer == playerColor)
        {
            GameStatusText.Text = "C'est votre tour";
        }
        else
        {
            GameStatusText.Text = "En attente de l'adversaire";
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        await Disconnect();
        base.OnClosed(e);
    }
}