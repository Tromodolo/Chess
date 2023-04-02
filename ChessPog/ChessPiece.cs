using static ChessPog.ChessLogic;

namespace ChessPog {
    internal class ChessPiece {
        internal PieceType Type;
        internal PieceColor Color;

        internal int TimesMoved;
        internal int MovedRanks;
        internal bool JustMoved;

        internal int File;
        internal int Rank;

        internal List<PieceMove> Moves = new List<PieceMove>();

        internal bool Equals(ChessPiece other) {
            return Type == other.Type
                && Color == other.Color
                && TimesMoved == other.TimesMoved
                && File == other.File
                && Rank == other.Rank;
        }

        internal bool IsSlidingPiece() {
            return Type == PieceType.Bishop || Type == PieceType.Rook || Type == PieceType.Queen;
        }

        internal bool IsValidMove(int file, int rank, out PieceMove move) {
            if (Moves.Any(x => x.File == file && x.Rank == rank)) {
                move = Moves.FirstOrDefault(x => x.File == file && x.Rank == rank);
                return true;
            }
            move = new PieceMove { };
            return false;
        }
    }
}
