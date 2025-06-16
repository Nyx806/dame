using System;
using System.Collections.Generic;
using System.Linq;

namespace CheckersGame.Shared.Models
{
    public class Board
    {
        public const int BOARD_SIZE = 8;
        private List<List<Piece?>> pieces;

        public Board()
        {
            pieces = new List<List<Piece?>>();
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                var rowList = new List<Piece?>();
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    rowList.Add(null);
                }
                pieces.Add(rowList);
            }
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // Placer les pièces noires
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        pieces[row][col] = new Piece(PieceColor.Black, row, col);
                    }
                }
            }

            // Placer les pièces blanches
            for (int row = 5; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        pieces[row][col] = new Piece(PieceColor.White, row, col);
                    }
                }
            }
        }

        public Piece? GetPiece(int row, int col)
        {
            if (IsValidPosition(row, col))
            {
                return pieces[row][col];
            }
            return null;
        }

        public bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
        }

        public bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Console.WriteLine($"Board: Checking move from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            if (!IsValidPosition(fromRow, fromCol) || !IsValidPosition(toRow, toCol))
            {
                Console.WriteLine($"Board: Invalid position - from: ({fromRow},{fromCol}), to: ({toRow},{toCol})");
                return false;
            }

            Piece? piece = GetPiece(fromRow, fromCol);
            if (piece == null)
            {
                Console.WriteLine($"Board: No piece at source position ({fromRow},{fromCol})");
                return false;
            }

            Console.WriteLine($"Board: Found piece at source: {piece.Color} {piece.Type}");

            // Vérifier si la case de destination est vide
            if (GetPiece(toRow, toCol) != null)
            {
                Console.WriteLine($"Board: Destination square ({toRow},{toCol}) is not empty");
                return false;
            }

            // Vérifier si le mouvement est valide pour la pièce
            bool isValid = piece.IsValidMove(toRow, toCol);
            Console.WriteLine($"Board: Piece.IsValidMove returned {isValid} for {piece.Color} {piece.Type} from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            return isValid;
        }

        public bool IsValidCapture(int fromRow, int fromCol, int toRow, int toCol)
        {
            Console.WriteLine($"Board: Checking capture from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            if (!IsValidPosition(fromRow, fromCol) || !IsValidPosition(toRow, toCol))
            {
                Console.WriteLine($"Board: Invalid position - from: ({fromRow},{fromCol}), to: ({toRow},{toCol})");
                return false;
            }

            Piece? piece = GetPiece(fromRow, fromCol);
            if (piece == null)
            {
                Console.WriteLine($"Board: No piece at source position ({fromRow},{fromCol})");
                return false;
            }

            Console.WriteLine($"Board: Found piece at source: {piece.Color} {piece.Type}");

            // Vérifier si la case de destination est vide
            if (GetPiece(toRow, toCol) != null)
            {
                Console.WriteLine($"Board: Destination square ({toRow},{toCol}) is not empty");
                return false;
            }

            // Calculer la position de la pièce capturée
            int capturedRow = (fromRow + toRow) / 2;
            int capturedCol = (fromCol + toCol) / 2;
            Piece? capturedPiece = GetPiece(capturedRow, capturedCol);

            Console.WriteLine($"Board: Checking for captured piece at ({capturedRow},{capturedCol})");
            Console.WriteLine($"Board: Captured piece details - Exists: {capturedPiece != null}, Color: {capturedPiece?.Color}, Type: {capturedPiece?.Type}");

            // Vérifier si une pièce adverse est présente
            if (capturedPiece == null)
            {
                Console.WriteLine($"Board: No piece to capture at ({capturedRow},{capturedCol})");
                return false;
            }
            if (capturedPiece.Color == piece.Color)
            {
                Console.WriteLine($"Board: Cannot capture own piece at ({capturedRow},{capturedCol})");
                return false;
            }

            // Vérifier si le mouvement est valide pour la pièce
            bool isValid = piece.IsValidMove(toRow, toCol, true);
            Console.WriteLine($"Board: Piece.IsValidMove for capture returned {isValid} for {piece.Color} {piece.Type} from ({fromRow},{fromCol}) to ({toRow},{toCol})");
            if (!isValid)
            {
                Console.WriteLine($"Board: Invalid move pattern for capture");
            }
            return isValid;
        }

        public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsValidMove(fromRow, fromCol, toRow, toCol))
                throw new InvalidOperationException("Invalid move");

            Piece? piece = GetPiece(fromRow, fromCol);
            if (piece == null)
                throw new InvalidOperationException("No piece at source position");

            pieces[toRow][toCol] = piece;
            pieces[fromRow][fromCol] = null;
            piece.Row = toRow;
            piece.Column = toCol;

            // Promouvoir en roi si nécessaire
            if ((piece.Color == PieceColor.White && toRow == 0) ||
                (piece.Color == PieceColor.Black && toRow == BOARD_SIZE - 1))
            {
                piece.PromoteToKing();
            }
        }

        public void CapturePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsValidCapture(fromRow, fromCol, toRow, toCol))
                throw new InvalidOperationException("Invalid capture");

            int capturedRow = (fromRow + toRow) / 2;
            int capturedCol = (fromCol + toCol) / 2;

            // Déplacer la pièce directement sans vérifier IsValidMove
            Piece? piece = GetPiece(fromRow, fromCol);
            if (piece == null)
                throw new InvalidOperationException("No piece at source position");

            pieces[toRow][toCol] = piece;
            pieces[fromRow][fromCol] = null;
            piece.Row = toRow;
            piece.Column = toCol;

            // Supprimer la pièce capturée
            pieces[capturedRow][capturedCol] = null;

            // Promouvoir en roi si nécessaire
            if ((piece.Color == PieceColor.White && toRow == 0) ||
                (piece.Color == PieceColor.Black && toRow == BOARD_SIZE - 1))
            {
                piece.PromoteToKing();
            }
        }

        public List<(int row, int col)> GetValidMoves(int row, int col)
        {
            var moves = new List<(int row, int col)>();
            Piece? piece = GetPiece(row, col);
            if (piece == null)
                return moves;

            // Vérifier les mouvements simples
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (IsValidMove(row, col, r, c))
                    {
                        moves.Add((r, c));
                    }
                }
            }

            // Vérifier les captures
            for (int r = 0; r < BOARD_SIZE; r++)
            {
                for (int c = 0; c < BOARD_SIZE; c++)
                {
                    if (IsValidCapture(row, col, r, c))
                    {
                        moves.Add((r, c));
                    }
                }
            }

            return moves;
        }

        public bool HasValidMoves(PieceColor color)
        {
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int col = 0; col < BOARD_SIZE; col++)
                {
                    Piece? piece = GetPiece(row, col);
                    if (piece != null && piece.Color == color)
                    {
                        if (GetValidMoves(row, col).Any())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
} 