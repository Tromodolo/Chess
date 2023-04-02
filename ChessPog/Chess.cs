using System;
using static ChessPog.Chess;
using Clr = ChessPog.Colors;

namespace ChessPog {
    internal class Chess {
        internal static List<Piece> Board = new();
        internal static Side ToMove;

        internal static List<PieceType> WhiteTaken = new();
        internal static List<PieceType> BlackTaken = new();

        internal static int? MovedFromX = null;
        internal static int? MovedFromY = null;
        internal static int? MovedToX = null;
        internal static int? MovedToY = null;
        internal static Piece? PickedUp;

        internal static bool CanRedraw = true;

        internal static bool GameEnded;
        internal static GameResult Result;

        //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
        /*
         * A FEN record defines a particular game position, all in one text line and using only the ASCII character set. A text file with only FEN data records should use the filename extension .fen.[4]
           A FEN record contains six fields, each separated by a space. The fields are as follows:[5]

           Piece placement data: Each rank is described, starting with rank 8 and ending with rank 1, with a "/" between each one; 
           within each rank, the contents of the squares are described in order from the a-file to the h-file.
           Each piece is identified by a single letter taken from the standard English names in algebraic notation 
           (pawn = "P", knight = "N", bishop = "B", rook = "R", queen = "Q" and king = "K"). 
           White pieces are designated using uppercase letters ("PNBRQK"), while black pieces use lowercase letters ("pnbrqk"). 
           A set of one or more consecutive empty squares within a rank is denoted by a digit from "1" to "8", corresponding to the number of squares.
           Active color: "w" means that White is to move; "b" means that Black is to move.
           Castling availability: If neither side has the ability to castle, this field uses the character "-". Otherwise, this field contains one or more letters: "K" if White can castle kingside, "Q" if White can castle queenside, "k" if Black can castle kingside, and "q" if Black can castle queenside. A situation that temporarily prevents castling does not prevent the use of this notation.
           En passant target square: This is a square over which a pawn has just passed while moving two squares; it is given in algebraic notation. 
           If there is no en passant target square, this field uses the character "-". 
           This is recorded regardless of whether there is a pawn in position to capture en passant.[6] 
           An updated version of the spec has since made it so the target square is only recorded if a legal en passant move 
           is possible but the old version of the standard is the one most commonly used.[7][8]
           Halfmove clock: The number of halfmoves since the last capture or pawn advance, used for the fifty-move rule.[9]
           Fullmove number: The number of the full moves. It starts at 1 and is incremented after Black's move.
         */
        internal static void LoadFromFENString(string fen) {
            int rank = 0;
            int file = 0;
            foreach (var character in fen) {
                if (rank >= 8) {
                    break;
                } else if (character.Equals('\\') || file >= 8) {
                    rank++;
                    file = 0;
                    continue;
                } else if (char.IsNumber(character)) {
                    file += (int)char.GetNumericValue(character);
                } else if (char.IsLetter(character)) {
                    var type = PieceType.Pawn;

                    if (char.ToLower(character).Equals('r')) {
                        type = PieceType.Rook;
                    } else if (char.ToLower(character).Equals('b')) {
                        type = PieceType.Bishop;
                    } else if (char.ToLower(character).Equals('n')) {
                        type = PieceType.Knight;
                    } else if (char.ToLower(character).Equals('q')) {
                        type = PieceType.Queen;
                    } else if (char.ToLower(character).Equals('k')) {
                        type = PieceType.King;
                    }

                    Board.Add(new Piece {
                        Side = char.IsUpper(character) ? Side.White : Side.Black,
                        Type = type,
                        File = file,
                        Rank = rank,
                    });
                } else if (character.Equals(' ')) {
                    break;
                }
                file++;
            }
        }

        internal static void PressSquare(int file, int rank) {
            if (PickedUp != null) {
                if (!PickedUp.IsValidMove(file, rank, out PieceMove move)) {
                    PickedUp = null;
                    return;
                }

                DoMove(move);
                foreach (var boardPiece in Board) {
                    if (boardPiece.Side == ToMove) {
                        boardPiece.JustMoved = false;
                    }
                    boardPiece.GenerateMoves();
                }
                PickedUp = null;
            } else {
                var piece = Board.FirstOrDefault(piece => piece.File == file && piece.Rank == rank);
                if (piece != null) {
                    if (piece.Side != ToMove) {
                        return;
                    }
                    if (piece.Moves.Count == 0) {
                        // No legal moves for this piece
                        return;
                    } 

                    PickedUp = piece;
                    MovedFromX = null;
                    MovedFromY = null;
                    MovedToX = null;
                    MovedToY = null;
                }
            }

            CanRedraw = false;
            FilterLegalMoves();
            CheckWinCondition();
            CanRedraw = true;
        }

        internal static void DoMove(PieceMove move) {
            var file = move.File;
            var rank = move.Rank;

            if (move.CapturedPiece != null) {
                Board.Remove(move.CapturedPiece);

                if (ToMove == Side.White) {
                    WhiteTaken.Add(move.CapturedPiece.Type);
                } else {
                    BlackTaken.Add(move.CapturedPiece.Type);
                }
            } else if (move.CastlingPiece != null) {
                if (move.CastlingPiece.File == 0) { // Long Castle
                    move.CastlingPiece.File = file + 1;
                } else if (move.CastlingPiece.File == 7) { // Short Castle
                    move.CastlingPiece.File = file - 1;
                }
            }

            MovedFromX = move.Piece.File;
            MovedFromY = move.Piece.Rank;
            MovedToX = file;
            MovedToY = rank;

            move.Piece.MovedRanks = move.Piece.Rank - rank;
            move.Piece.File = file;
            move.Piece.Rank = rank;
            move.Piece.TimesMoved++;
            move.Piece.JustMoved = true;

            if (move.Piece.Type == PieceType.Pawn && 
                (move.Rank == 0 || move.Rank == 7)) {
                move.Piece.Type = PieceType.Queen;
            }

            if (ToMove == Side.White) {
                ToMove = Side.Black;
            } else {
                ToMove = Side.White;
            }
        }

        internal static void UndoMove(PieceMove move) {
            if (move.CapturedPiece != null) {
                Board.Add(move.CapturedPiece);
                if (move.Piece.Side == Side.White) {
                    WhiteTaken.Remove(move.CapturedPiece.Type);
                } else {
                    BlackTaken.Remove(move.CapturedPiece.Type);
                }
            } else if (move.CastlingPiece != null) {
                if (move.CastlingPiece.File >= 4) { // Short castle
                    move.CastlingPiece.File = 7;
                } else { // Long castle
                    move.CastlingPiece.File = 0;
                }
            }

            if (move.Piece.Type != move.OriginalType) {
                move.Piece.Type = move.OriginalType;
            }

            move.Piece.File = move.OriginalFile;
            move.Piece.Rank = move.OriginalRank;
            move.Piece.TimesMoved--;

            if (ToMove == Side.White) {
                ToMove = Side.Black;
            } else {
                ToMove = Side.White;
            }

            MovedFromX = null;
            MovedFromY = null;
            MovedToX = null;
            MovedToY = null;
        }

        internal static void FilterLegalMoves() {
            var moves = Board.Where(x => x.Side == ToMove).SelectMany(x => x.Moves).ToList();
            var opponentPieces = Board.Where(x => x.Side != ToMove).ToList();
            var illegalMoves = new List<PieceMove>();

            foreach (var move in moves) {
                DoMove(move);

                if (move.CapturedPiece != null) {
                    opponentPieces.Remove(move.CapturedPiece);
                }

                foreach (var opponentPiece in opponentPieces) {
                    opponentPiece.GenerateMoves();
                }
                var opponentMoves = opponentPieces
                    .SelectMany(x => x.Moves)
                    .Where(x => x.CapturedPiece != null);

                if (opponentMoves.Any(x => x.CapturedPiece.Side != ToMove && x.CapturedPiece.Type == PieceType.King)) {
                    // Illegal move
                    illegalMoves.Add(move);
                }

                if (move.CapturedPiece != null) {
                    opponentPieces.Add(move.CapturedPiece);
                }

                UndoMove(move);
            }


            foreach (var move in illegalMoves) {
                move.Piece.Moves.Remove(move);
            }
        }

        internal static void CheckWinCondition() {
            var moves = Board.Where(x => x.Side == ToMove).SelectMany(x => x.Moves).ToList();
            if (moves.Count() == 0) {
                // Game is over, find out if checkmate or if stalemate

                var opponentPieces = Board.Where(x => x.Side != ToMove).ToList();
                var opponentMoves = opponentPieces
                    .SelectMany(x => x.Moves)
                    .Where(x => x.CapturedPiece != null);
                if (opponentMoves.Any(x => x.CapturedPiece.Side == ToMove && x.CapturedPiece.Type == PieceType.King)) {
                    if (ToMove == Side.White) {
                        Result = GameResult.BlackWin;
                    } else {
                        Result = GameResult.WhiteWin;
                    }
                } else {
                    Result = GameResult.Draw;
                }

                GameEnded = true;

            }
        }

        internal struct PieceMove {
            internal Piece Piece;
            internal PieceType OriginalType;
            internal int OriginalFile;
            internal int OriginalRank;
            internal int File;
            internal int Rank;
            internal Piece CapturedPiece;
            internal Piece CastlingPiece;
        }

        internal struct DistanceToEdge {
            internal int Left;
            internal int Right;
            internal int Top;
            internal int Bottom;
        }

        internal class Piece {
            internal PieceType Type;
            internal Side Side;

            internal int TimesMoved;
            internal int MovedRanks;
            internal bool JustMoved;

            internal int File;
            internal int Rank;

            internal List<PieceMove> Moves = new List<PieceMove>();
            //internal DistanceToEdge DistanceToEdge;

            internal bool Equals(Piece other) {
                return Type == other.Type
                    && Side == other.Side
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

            internal void GenerateMoves() {
                Moves = new List<PieceMove>();

                if (IsSlidingPiece()) {
                    GenerateSlidingMoves();
                } else {
                    GenerateGeneralMoves();
                }
            }

            private bool TryAddMove(int newFile, int newRank, Piece? enPassantPiece = null, Piece? castlingPiece = null) {
                if (newFile < 0 || newFile >= 8 || newRank < 0 || newRank >= 8) {
                    return false;
                }

                var pieceOnNewPosition = Board.FirstOrDefault(piece => {
                    if (piece.File == newFile && piece.Rank == newRank) {
                        return true;
                    }
                    return false;
                });

                if (pieceOnNewPosition != null) {
                    if (pieceOnNewPosition.Side != Side) {
                        Moves.Add(new PieceMove {
                            Piece = this,
                            OriginalType = Type,
                            OriginalFile = File,
                            OriginalRank = Rank,
                            File = newFile,
                            Rank = newRank,
                            CapturedPiece = pieceOnNewPosition,
                        });
                    }
                    return false;
                } else if (enPassantPiece != null) {
                    if (enPassantPiece.Side != Side) {
                        Moves.Add(new PieceMove {
                            Piece = this,
                            OriginalType = Type,
                            OriginalFile = File,
                            OriginalRank = Rank,
                            File = newFile,
                            Rank = newRank,
                            CapturedPiece = enPassantPiece,
                        });
                    }
                    return false;
                } else if (castlingPiece != null) {
                    if (castlingPiece.Side == Side) {
                        Moves.Add(new PieceMove {
                            Piece = this,
                            OriginalType = Type,
                            OriginalFile = File,
                            OriginalRank = Rank,
                            File = newFile,
                            Rank = newRank,
                            CastlingPiece = castlingPiece,
                        });
                    }
                    return false;
                }

                Moves.Add(new PieceMove {
                    Piece = this,
                    OriginalType = Type,
                    OriginalFile = File,
                    OriginalRank = Rank,
                    File = newFile,
                    Rank = newRank,
                });
                return true;
            }

            private void GenerateSlidingMoves() {
                var possibleOffsets = new int[8][] {
                    new int[] { -1,  0  }, // Left
                    new int[] {  0,  1  }, // Up
                    new int[] {  1,  0  }, // Right
                    new int[] {  0, -1  }, // Down
                    new int[] { -1,  1  }, // Left Up
                    new int[] {  1,  1  }, // Right Up
                    new int[] {  1, -1  }, // Right Down
                    new int[] { -1, -1  }, // Left Down
                };

                switch (Type) {
                    case PieceType.Bishop:
                        possibleOffsets = possibleOffsets[4..];
                        break;
                    case PieceType.Rook:
                        possibleOffsets = possibleOffsets[0..4];
                        break;
                    case PieceType.Queen:
                        break;
                }

                foreach (var offset in possibleOffsets) {
                    for (var length = 1; length < 8; length++) {
                        var newFile = File + length * offset[0];
                        var newRank = Rank + length * offset[1];

                        if (!TryAddMove(newFile, newRank)) {
                            break;
                        }
                    }
                }
            }

            private void GenerateGeneralMoves() {
                switch (Type) {
                    case PieceType.Pawn:
                        if (!Board.Any(x => x.File == File && x.Rank == (Side == Side.White ? Rank - 1 : Rank + 1))) {
                            TryAddMove(File, Side == Side.White ? Rank - 1 : Rank + 1);
                        }
                        if (TimesMoved == 0 && 
                            !Board.Any(x => x.File == File && x.Rank == (Side == Side.White ? Rank - 2 : Rank + 2))) {
                            TryAddMove(File, Side == Side.White ? Rank - 2 : Rank + 2);
                        }

                        int compareRank = Rank;
                        if (Side == Side.White) {
                            compareRank -= 1;
                        } else {
                            compareRank += 1;
                        }

                        // Captures
                        if (Board.Any(x => x.File == File + 1 && x.Rank == compareRank)) {
                            TryAddMove(File + 1, compareRank);
                        }
                        if (Board.Any(x => x.File == File - 1 && x.Rank == compareRank)) {
                            TryAddMove(File - 1, compareRank);
                        }

                        // En passant
                        if (Board.Any(x => x.File == File - 1 && x.Rank == Rank && x.Side != Side)) {
                            var piece = Board.FirstOrDefault(x => x.File == File - 1 && x.Rank == Rank && x.Side != Side);
                            if (piece.TimesMoved == 1 && Math.Abs(piece.MovedRanks) == 2 && piece.JustMoved) {
                                TryAddMove(File - 1, compareRank, piece);
                            }
                        }
                        if (Board.Any(x => x.File == File + 1 && x.Rank == Rank && x.Side != Side)) {
                            var piece = Board.FirstOrDefault(x => x.File == File + 1 && x.Rank == Rank && x.Side != Side);
                            if (piece.TimesMoved == 1 && Math.Abs(piece.MovedRanks) == 2 && piece.JustMoved) {
                                TryAddMove(File + 1, compareRank, piece);
                            }
                        }

                        break;
                    case PieceType.Knight:
                        TryAddMove(File + 2, Rank + 1);
                        TryAddMove(File + 2, Rank - 1);

                        TryAddMove(File - 2, Rank + 1);
                        TryAddMove(File - 2, Rank - 1);

                        TryAddMove(File + 1, Rank + 2);
                        TryAddMove(File - 1, Rank + 2);

                        TryAddMove(File + 1, Rank - 2);
                        TryAddMove(File - 1, Rank - 2);
                        break;
                    case PieceType.King:
                        TryAddMove(File, Rank + 1);
                        TryAddMove(File, Rank - 1);

                        TryAddMove(File + 1, Rank);
                        TryAddMove(File - 1, Rank);

                        TryAddMove(File - 1, Rank - 1);
                        TryAddMove(File - 1, Rank + 1);
                        TryAddMove(File + 1, Rank - 1);
                        TryAddMove(File + 1, Rank + 1);

                        // Castling
                        if (TimesMoved == 0) {
                            // Long Castle
                            if (Board.Any(x => x.File == 0 && x.Rank == Rank && x.Side == Side)) {
                                var piece = Board.FirstOrDefault(x => x.File == 0 && x.Rank == Rank && x.Side == Side);
                                if (piece.TimesMoved == 0) {
                                    if (!Board.Any(x => x.File == 1 && x.Rank == Rank && x.Side == Side) &&
                                        !Board.Any(x => x.File == 2 && x.Rank == Rank && x.Side == Side) &&
                                        !Board.Any(x => x.File == 3 && x.Rank == Rank && x.Side == Side)) {
                                        TryAddMove(File - 2, Rank, castlingPiece: piece);
                                    }
                                }
                            }

                            // Short Castle
                            if (Board.Any(x => x.File == 7 && x.Rank == Rank && x.Side == Side)) {
                                var piece = Board.FirstOrDefault(x => x.File == 7 && x.Rank == Rank && x.Side == Side);
                                if (piece.TimesMoved == 0) {
                                    if (!Board.Any(x => x.File == 6 && x.Rank == Rank && x.Side == Side) &&
                                        !Board.Any(x => x.File == 5 && x.Rank == Rank && x.Side == Side)) {
                                        TryAddMove(File + 2, Rank, castlingPiece: piece);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        internal enum GameResult {
            WhiteWin,
            BlackWin,
            Draw,
        }

        internal enum PieceType {
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen,
            King
        }

        internal enum Side {
            White,
            Black
        }

        internal int[] PieceValues = new int[] {
            1,
            3,
            3,
            5,
            9,
            0
        };

        internal class Sprites {
            internal static Clr.ColorSpec[] Rook = new Clr.ColorSpec[] {
                Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty,
                Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty,
                Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty,
                Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty,
                Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty,
                Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty,
                Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty,
                Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty,
            };
        }   
    }
}
