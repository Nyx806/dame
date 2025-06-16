using System;

namespace CheckersGame.Shared.Models
{
    public enum PieceType
    {
        Normal,
        King
    }

    public enum PieceColor
    {
        Black,
        White
    }

    public class Piece
    {
        public PieceType Type { get; private set; }
        public PieceColor Color { get; }
        public int Row { get; set; }
        public int Column { get; set; }

        public Piece(PieceColor color, int row, int column)
        {
            Color = color;
            Type = PieceType.Normal;
            Row = row;
            Column = column;
        }

        public void PromoteToKing()
        {
            if (Type == PieceType.Normal)
            {
                Type = PieceType.King;
            }
        }

        public bool IsValidMove(int toRow, int toCol, bool isCapture = false)
        {
            Console.WriteLine($"Piece: Checking move for {Color} {Type} from ({Row},{Column}) to ({toRow},{toCol}), isCapture: {isCapture}");

            // Vérifier si le mouvement est diagonal
            int rowDiff = Math.Abs(toRow - Row);
            int colDiff = Math.Abs(toCol - Column);
            Console.WriteLine($"Piece: Movement differences - rowDiff: {rowDiff}, colDiff: {colDiff}");
            
            if (rowDiff != colDiff)
            {
                Console.WriteLine($"Piece: Move is not diagonal - rowDiff: {rowDiff}, colDiff: {colDiff}");
                return false;
            }

            // Vérifier la distance
            if (isCapture)
            {
                if (rowDiff != 2)
                {
                    Console.WriteLine($"Piece: Invalid capture distance - must be 2 squares, got {rowDiff}");
                    return false;
                }
                Console.WriteLine($"Piece: Valid capture distance of 2 squares");
            }
            else
            {
                if (rowDiff != 1)
                {
                    Console.WriteLine($"Piece: Invalid move distance - must be 1 square, got {rowDiff}");
                    return false;
                }
                Console.WriteLine($"Piece: Valid move distance of 1 square");
            }

            // Vérifier la direction pour les pièces normales
            if (Type == PieceType.Normal)
            {
                if (Color == PieceColor.White)
                {
                    // Les pièces blanches doivent monter (diminuer le numéro de ligne)
                    if (toRow >= Row)
                    {
                        Console.WriteLine($"Piece: White piece must move up, but trying to move from {Row} to {toRow}");
                        return false;
                    }
                    Console.WriteLine($"Piece: Valid upward movement for white piece");
                }
                else
                {
                    // Les pièces noires doivent descendre (augmenter le numéro de ligne)
                    if (toRow <= Row)
                    {
                        Console.WriteLine($"Piece: Black piece must move down, but trying to move from {Row} to {toRow}");
                        return false;
                    }
                    Console.WriteLine($"Piece: Valid downward movement for black piece");
                }
            }
            else
            {
                Console.WriteLine($"Piece: King piece - no direction restriction");
            }

            Console.WriteLine($"Piece: Move is valid for {Color} {Type} from ({Row},{Column}) to ({toRow},{toCol})");
            return true;
        }
    }
} 