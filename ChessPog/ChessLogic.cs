using static ChessPog.ChessLogic;
using Clr = ChessPog.Colors;

namespace ChessPog {
    internal class ChessLogic {
        internal List<ChessPiece> Board = new();
        internal PieceColor ToMove;

        // These two are not currently used
        // Possibly used in the future to render points
        internal List<PieceType> WhiteTaken = new();
        internal List<PieceType> BlackTaken = new();

        internal int? MovedFromX = null;
        internal int? MovedFromY = null;
        internal int? MovedToX = null;
        internal int? MovedToY = null;
        internal ChessPiece? PickedUp;

        /// <summary>
        /// Since some funky stuff happens while calculation of legal moves is happening, make sure that its not redrawing while that is happening.
        /// </summary>
        internal bool CanRedraw = true;

        internal bool GameEnded;
        internal GameResult Result;

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
           Source: https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
         */
        internal void LoadFromFENString(string fen) {
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

                    Board.Add(new ChessPiece {
                        Color = char.IsUpper(character) ? PieceColor.White : PieceColor.Black,
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

        internal void GenerateMoves() {
            foreach (var piece in Board) {
                if (piece.Color != ToMove) {
                    continue;
                }

                piece.Moves = new List<PieceMove>();

                if (piece.IsSlidingPiece()) {
                    GenerateSlidingMoves(piece);
                } else {
                    GenerateGeneralMoves(piece);
                }
            }
        }

        internal void OnSquarePressed(int file, int rank) {
            if (PickedUp != null) {
                if (!PickedUp.IsValidMove(file, rank, out PieceMove move)) {
                    PickedUp = null;
                    return;
                }

                DoMove(move);
                foreach (var boardPiece in Board) {
                    // To make sure en passant only works when it has just happened
                    // This makes sure to reset the flag
                    if (boardPiece.Color == ToMove) {
                        boardPiece.JustMoved = false;
                    }
                }
                GenerateMoves();
                PickedUp = null;
            } else {
                var piece = Board.FirstOrDefault(piece => piece.File == file && piece.Rank == rank);
                if (piece != null) {
                    // Don't allow moving pieces that aren't the current turn
                    if (piece.Color != ToMove) {
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

        internal void DoMove(PieceMove move) {
            var file = move.File;
            var rank = move.Rank;

            if (move.CapturedPiece != null) {
                Board.Remove(move.CapturedPiece);

                if (ToMove == PieceColor.White) {
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

            // TODO: Make sure to allow pawns to upgrade into stuff that isn't just a queen
            if (move.Piece.Type == PieceType.Pawn && 
                (move.Rank == 0 || move.Rank == 7)) {
                move.Piece.Type = PieceType.Queen;
            }

            if (ToMove == PieceColor.White) {
                ToMove = PieceColor.Black;
            } else {
                ToMove = PieceColor.White;
            }
        }

        internal void UndoMove(PieceMove move) {
            if (move.CapturedPiece != null) {
                Board.Add(move.CapturedPiece);
                if (move.Piece.Color == PieceColor.White) {
                    WhiteTaken.Remove(move.CapturedPiece.Type);
                } else {
                    BlackTaken.Remove(move.CapturedPiece.Type);
                }
            } else if (move.CastlingPiece != null) {
                if (move.CastlingPiece.File >= 4) { // Short castle, if file is 4 or above, it's presumed to be short
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

            if (ToMove == PieceColor.White) {
                ToMove = PieceColor.Black;
            } else {
                ToMove = PieceColor.White;
            }

            MovedFromX = null;
            MovedFromY = null;
            MovedToX = null;
            MovedToY = null;
        }

        internal void FilterLegalMoves() {
            var moves = Board.Where(x => x.Color == ToMove).SelectMany(x => x.Moves).ToList();
            var opponentPieces = Board.Where(x => x.Color != ToMove).ToList();
            var illegalMoves = new List<PieceMove>();

            foreach (var move in moves) {
                DoMove(move);

                // DoMove can possibly have removed a piece
                // If so, make sure to remove it from opponentPieces
                // so the legality calculation works properly
                if (move.CapturedPiece != null) {
                    opponentPieces.Remove(move.CapturedPiece);
                }

                // Now that the state has changed, regenerate moves
                GenerateMoves();

                // Only check pieces that ACTUALLY capture something
                var opponentMoves = opponentPieces
                    .SelectMany(x => x.Moves)
                    .Where(x => x.CapturedPiece != null);

                // If a move leads to the opponent being able to capture the king, it means it must be an illegal move
                // Therefore, remove the move from play
                if (opponentMoves.Any(x => x.CapturedPiece.Color != ToMove && x.CapturedPiece.Type == PieceType.King)) {
                    illegalMoves.Add(move);
                }

                // Restore opponentPieces if it was removed earlier
                if (move.CapturedPiece != null) {
                    opponentPieces.Add(move.CapturedPiece);
                }

                // Restore the state to how it was before DoMove did its thing
                UndoMove(move);
            }

            // A bit janky but remove the move from the Piece State
            foreach (var move in illegalMoves) {
                move.Piece.Moves.Remove(move);
            }
        }

        internal void CheckWinCondition() {
            var moves = Board.Where(x => x.Color == ToMove).SelectMany(x => x.Moves).ToList();

            // If a user has no valid moves left, there must either be a win condition in play or a stalemate
            if (moves.Count() == 0) {
                // Game is over, find out if checkmate or if stalemate
                var opponentPieces = Board.Where(x => x.Color != ToMove).ToList();
                var opponentMoves = opponentPieces
                    .SelectMany(x => x.Moves)
                    .Where(x => x.CapturedPiece != null);

                if (opponentMoves.Any(x => x.CapturedPiece.Color == ToMove && x.CapturedPiece.Type == PieceType.King)) {
                    if (ToMove == PieceColor.White) {
                        Result = GameResult.BlackWin;
                    } else {
                        Result = GameResult.WhiteWin;
                    }
                } else {
                    // If there's no valid moves, and none of the opponents moves actually capture the king, that means that we must be in a stalemate
                    Result = GameResult.Draw;
                }

                GameEnded = true;
            }
        }

        /// <summary>
        /// Tries to add a move with the specified file and rank to a piece. Alternatively also allow for specifying en passant piece or rook to use for castling.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="newFile"></param>
        /// <param name="newRank"></param>
        /// <param name="enPassantPiece"></param>
        /// <param name="castlingPiece"></param>
        /// <returns>False if a sliding block would be stopped from friendly or enemy piece</returns>
        private bool TryAddMove(ChessPiece piece, int newFile, int newRank, ChessPiece? enPassantPiece = null, ChessPiece? castlingPiece = null) {
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
                if (pieceOnNewPosition.Color != piece.Color) {
                    piece.Moves.Add(new PieceMove {
                        Piece = piece,
                        OriginalType = piece.Type,
                        OriginalFile = piece.File,
                        OriginalRank = piece.Rank,
                        File = newFile,
                        Rank = newRank,
                        CapturedPiece = pieceOnNewPosition,
                    });
                }
                return false;
            } else if (enPassantPiece != null) {
                if (enPassantPiece.Color != piece.Color) {
                    piece.Moves.Add(new PieceMove {
                        Piece = piece,
                        OriginalType = piece.Type,
                        OriginalFile = piece.File,
                        OriginalRank = piece.Rank,
                        File = newFile,
                        Rank = newRank,
                        CapturedPiece = enPassantPiece,
                    });
                }
                return false;
            } else if (castlingPiece != null) {
                if (castlingPiece.Color == piece.Color) {
                    piece.Moves.Add(new PieceMove {
                        Piece = piece,
                        OriginalType = piece.Type,
                        OriginalFile = piece.File,
                        OriginalRank = piece.Rank,
                        File = newFile,
                        Rank = newRank,
                        CastlingPiece = castlingPiece,
                    });
                }
                return false;
            }

            piece.Moves.Add(new PieceMove {
                Piece = piece,
                OriginalType = piece.Type,
                OriginalFile = piece.File,
                OriginalRank = piece.Rank,
                File = newFile,
                Rank = newRank,
            });
            return true;
        }

        private void GenerateSlidingMoves(ChessPiece piece) {
            // Possible directions that sliding pieces can move in
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

            // Depending on the piece type, use a subset of the direction list
            switch (piece.Type) {
                case PieceType.Bishop:
                    possibleOffsets = possibleOffsets[4..];
                    break;
                case PieceType.Rook:
                    possibleOffsets = possibleOffsets[0..4];
                    break;
                case PieceType.Queen:
                    break;
            }

            // Go through every possible position in every direction and check if it allows for valid 
            // If TryAddMove returns false, break would then go to the next available direction and count up again
            foreach (var offset in possibleOffsets) {
                for (var length = 1; length < 8; length++) {
                    var newFile = piece.File + length * offset[0];
                    var newRank = piece.Rank + length * offset[1];

                    if (!TryAddMove(piece, newFile, newRank)) {
                        break;
                    }
                }
            }
        }

        private void GenerateGeneralMoves(ChessPiece piece) {
            switch (piece.Type) {
                case PieceType.Pawn:
                    // If there is no piece in front of a pawn, allow moving forward
                    if (!Board.Any(x => x.File == piece.File && x.Rank == (piece.Color == PieceColor.White ? piece.Rank - 1 : piece.Rank + 1))) {
                        TryAddMove(piece, piece.File, piece.Color == PieceColor.White ? piece.Rank - 1 : piece.Rank + 1);
                    }

                    // If there is no piece AND its the first movement of the pawn, allow moving two ranks
                    if (piece.TimesMoved == 0 &&
                        !Board.Any(x => x.File == piece.File && x.Rank == (piece.Color == PieceColor.White ? piece.Rank - 2 : piece.Rank + 2))) {
                        TryAddMove(piece, piece.File, piece.Color == PieceColor.White ? piece.Rank - 2 : piece.Rank + 2);
                    }


                    int compareRank = piece.Rank;
                    if (piece.Color == PieceColor.White) {
                        compareRank -= 1;
                    } else {
                        compareRank += 1;
                    }

                    // Captures
                    // Check two positions diagonally in front of the pawn
                    if (Board.Any(x => x.File == piece.File + 1 && x.Rank == compareRank)) {
                        TryAddMove(piece, piece.File + 1, compareRank);
                    }
                    if (Board.Any(x => x.File == piece.File - 1 && x.Rank == compareRank)) {
                        TryAddMove(piece, piece.File - 1, compareRank);
                    }

                    // En passant
                    // This is a mess, just google en passant if you want to know how this works
                    if (Board.Any(x => x.File == piece.File - 1 && x.Rank == piece.Rank && x.Color != piece.Color)) {
                        var comparisonPiece = Board.FirstOrDefault(x => x.File == piece.File - 1 && x.Rank == piece.Rank && x.Color != piece.Color);
                        if (comparisonPiece.TimesMoved == 1 && Math.Abs(comparisonPiece.MovedRanks) == 2 && comparisonPiece.JustMoved) {
                            TryAddMove(piece, piece.File - 1, compareRank, piece);
                        }
                    }
                    if (Board.Any(x => x.File == piece.File + 1 && x.Rank == piece.Rank && x.Color != piece.Color)) {
                        var comparisonPiece = Board.FirstOrDefault(x => x.File == piece.File + 1 && x.Rank == piece.Rank && x.Color != piece.Color);
                        if (comparisonPiece.TimesMoved == 1 && Math.Abs(comparisonPiece.MovedRanks) == 2 && comparisonPiece.JustMoved) {
                            TryAddMove(piece, piece.File + 1, compareRank, piece);
                        }
                    }

                    break;
                case PieceType.Knight:
                    // Add every move for knight
                    TryAddMove(piece, piece.File + 2, piece.Rank + 1);
                    TryAddMove(piece, piece.File + 2, piece.Rank - 1);

                    TryAddMove(piece, piece.File - 2, piece.Rank + 1);
                    TryAddMove(piece, piece.File - 2, piece.Rank - 1);

                    TryAddMove(piece, piece.File + 1, piece.Rank + 2);
                    TryAddMove(piece, piece.File - 1, piece.Rank + 2);

                    TryAddMove(piece, piece.File + 1, piece.Rank - 2);
                    TryAddMove(piece, piece.File - 1, piece.Rank - 2);
                    break;
                case PieceType.King:
                    // Add every move for king
                    TryAddMove(piece, piece.File, piece.Rank + 1);
                    TryAddMove(piece, piece.File, piece.Rank - 1);

                    TryAddMove(piece, piece.File + 1, piece.Rank);
                    TryAddMove(piece, piece.File - 1, piece.Rank);

                    TryAddMove(piece, piece.File - 1, piece.Rank - 1);
                    TryAddMove(piece, piece.File - 1, piece.Rank + 1);
                    TryAddMove(piece, piece.File + 1, piece.Rank - 1);
                    TryAddMove(piece, piece.File + 1, piece.Rank + 1);

                    // Castling
                    // If there is no piece between the king and the rook and they both haven't moved yet
                    // Castling would allow the king to move two files, and then the rook moving to the other side of it
                    // The left side of the board has 3 pieces between king and rook and is therefore called Long Castle
                    // The right side has two pieces and is called Short Castle
                    if (piece.TimesMoved == 0) {
                        // Long Castle
                        if (Board.Any(x => x.File == 0 && x.Rank == piece.Rank && x.Color == piece.Color)) {
                            var rookPiece = Board.FirstOrDefault(x => x.File == 0 && x.Rank == piece.Rank && x.Color == piece.Color);
                            if (rookPiece.TimesMoved == 0) {
                                if (!Board.Any(x => x.File == 1 && x.Rank == piece.Rank && x.Color == piece.Color) &&
                                    !Board.Any(x => x.File == 2 && x.Rank == piece.Rank && x.Color == piece.Color) &&
                                    !Board.Any(x => x.File == 3 && x.Rank == piece.Rank && x.Color == piece.Color)) {
                                    TryAddMove(piece, piece.File - 2, piece.Rank, castlingPiece: rookPiece);
                                }
                            }
                        }

                        // Short Castle
                        if (Board.Any(x => x.File == 7 && x.Rank == piece.Rank && x.Color == piece.Color)) {
                            var rookPiece = Board.FirstOrDefault(x => x.File == 7 && x.Rank == piece.Rank && x.Color == piece.Color);
                            if (rookPiece.TimesMoved == 0) {
                                if (!Board.Any(x => x.File == 6 && x.Rank == piece.Rank && x.Color == piece.Color) &&
                                    !Board.Any(x => x.File == 5 && x.Rank == piece.Rank && x.Color == piece.Color)) {
                                    TryAddMove(piece, piece.File + 2, piece.Rank, castlingPiece: rookPiece);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        internal enum GameResult {
            WhiteWin,
            BlackWin,
            Draw,
        }

        internal struct PieceMove {
            internal ChessPiece Piece;
            internal PieceType OriginalType;
            internal int OriginalFile;
            internal int OriginalRank;
            internal int File;
            internal int Rank;
            internal ChessPiece CapturedPiece;
            internal ChessPiece CastlingPiece;
        }

        internal enum PieceType {
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen,
            King
        }

        internal enum PieceColor {
            White,
            Black
        }

        //internal class Sprites {
        //    internal static Clr.ColorSpec[] Rook = new Clr.ColorSpec[] {
        //        Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty,
        //        Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty,
        //        Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty,
        //        Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty,
        //        Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty,
        //        Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty,
        //        Clr.Empty, Clr.Black, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Black, Clr.Empty,
        //        Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty, Clr.Empty,
        //    };
        //}   
    }
}
