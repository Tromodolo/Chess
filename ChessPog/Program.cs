using static SDL2.SDL;

namespace ChessPog {
    internal class Program {
        static nint GameWindow;
        static nint Texture;
        static nint Renderer;

        const int WindowWidth = 1024;
        const int WindowHeight = 512;
        const int WindowSizeMultiplier = 1;

        const int SpriteWidth = 64;
        const int FontSize = 16;

        static uint[] FrameBuffer = new uint[WindowWidth * WindowHeight];
        static bool Playing = true;

        static ChessLogic Chess;

        static void Main(string[] args) {
            if (SDL_Init(SDL_INIT_VIDEO) < 0) {
                Console.WriteLine($"There was an issue initializing  {SDL_GetError()}");
                return;
            }

            // Create a new window given a title, size, and passes it a flag indicating it should be shown.
            GameWindow = SDL_CreateWindow(
                "Chess", 
                SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 
                WindowWidth * WindowSizeMultiplier, 
                WindowHeight * WindowSizeMultiplier, 
                SDL_WindowFlags.SDL_WINDOW_SHOWN
            );

            if (GameWindow == nint.Zero) {
                Console.WriteLine($"There was an issue creating the window. {SDL_GetError()}");
                return;
            }

            // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
            nint renderer = SDL_CreateRenderer(GameWindow, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            if (renderer == nint.Zero) {
                Console.WriteLine($"There was an issue creating the renderer. {SDL_GetError()}");
                return;
            }

            Texture = SDL_CreateTexture(
                renderer, 
                SDL_PIXELFORMAT_RGB888,
                (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                WindowWidth,
                WindowHeight
            );
            Renderer = renderer;

            Chess = new ChessLogic();
            Chess.LoadFromFENString("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            Chess.GenerateMoves();

            while (Playing) {
                HandleEventLoop();   

                if (Chess.CanRedraw) {
                    RenderBoard();
                    RenderSelectedPiece();
                    RenderSelectedPieceMoves();
                    RenderMovement();
                    RenderGameBoard();
                }

                DrawText("lily = big dumb", WindowWidth / 2, FontSize * 0, Colors.White);
                DrawText("To move", WindowWidth / 2, FontSize * 2, Colors.White);
                DrawText(Chess.ToMove == ChessLogic.PieceColor.White ? "White" : "Black", WindowWidth / 2, FontSize * 3, Colors.White, true);

                if (Chess.GameEnded) {
                    DrawText("Game End", WindowWidth / 2, FontSize * 5, Colors.White, true);
                    string resultText = "";
                    switch (Chess.Result) {
                        case ChessLogic.GameResult.WhiteWin:
                            resultText = "* White won";
                            break;
                        case ChessLogic.GameResult.BlackWin:
                            resultText = "* Black won";
                            break;
                        case ChessLogic.GameResult.Draw:
                            resultText = "Draw";
                            break;
                    }
                    DrawText(resultText, WindowWidth / 2, FontSize * 6, Colors.White, true);
                }

                DrawFrame();
            }
        }

        private static void HandleEventLoop() {
            while (SDL_PollEvent(out var e) == 1) {
                switch (e.type) {
                    case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        HandleClick(e.button.x, e.button.y);
                        break;
                    case SDL_EventType.SDL_QUIT:
                        Playing = false;
                        break;
                }
            }
        }

        private static void RenderSelectedPiece() {
            if (Chess.PickedUp != null) {
                DrawSquare(Chess.PickedUp.File * SpriteWidth, Chess.PickedUp.Rank * SpriteWidth, SpriteWidth, SpriteWidth, Colors.Orange);
            }
        }

        private static void RenderSelectedPieceMoves() {
            if (Chess.PickedUp != null) {
                foreach (var move in Chess.PickedUp.Moves) {
                    DrawSquare(move.File * SpriteWidth, move.Rank * SpriteWidth, SpriteWidth, SpriteWidth, Colors.Orange);
                }
            }
        }

        private static void RenderMovement() {
            if (Chess.MovedFromX != null && Chess.MovedFromY != null) {
                DrawSquare(Chess.MovedFromX.Value * SpriteWidth, Chess.MovedFromY.Value * SpriteWidth, SpriteWidth, SpriteWidth, Colors.Yellow);
            }
            if (Chess.MovedToX != null && Chess.MovedToY != null) {
                DrawSquare(Chess.MovedToX.Value * SpriteWidth, Chess.MovedToY.Value * SpriteWidth, SpriteWidth, SpriteWidth, Colors.Yellow);
            }
        }

        private static void RenderBoard() {
            for (var x = 0; x < 8; x++) {
                for (var y = 0; y < 8; y++) {
                    var tile = (x + y) % 2 == 0 ? Colors.Tan : Colors.Green;

                    DrawSquare(x * SpriteWidth, y * SpriteWidth, SpriteWidth, SpriteWidth, tile);
                }
            }
        }

        private static void RenderGameBoard() {
            foreach (var piece in Chess.Board) {
                RenderPiece(piece, piece.File, piece.Rank);
            }
        }

        private static void HandleClick(int x, int y) {
            var file = x / SpriteWidth / WindowSizeMultiplier;
            var rank = y / SpriteWidth / WindowSizeMultiplier;
            Chess.OnSquarePressed(file, rank);
        }

        private static void RenderPiece(ChessPiece piece, int file, int rank) {
            Colors.ColorSpec color = Colors.White;
            if (piece.Color == ChessLogic.PieceColor.Black) {
                color = Colors.Black;
            }

            var pieceChar = "";
            switch(piece.Type) {
                case ChessLogic.PieceType.Pawn:
                    pieceChar = "P";
                    break;
                case ChessLogic.PieceType.Knight:
                    pieceChar = "N";
                    break;
                case ChessLogic.PieceType.Bishop:
                    pieceChar = "B";
                    break;
                case ChessLogic.PieceType.Rook:
                    pieceChar = "R";
                    break;
                case ChessLogic.PieceType.Queen:
                    pieceChar = "Q";
                    break;
                case ChessLogic.PieceType.King:
                    pieceChar = "K";
                    break;
            }

            DrawText(pieceChar, file * SpriteWidth, rank * SpriteWidth, color, false, SpriteWidth);

            //Colors.ColorSpec[] sprite;
            //switch (piece) {
            //    default:
            //    case Chess.PieceType.Rook: {
            //        sprite = Chess.Sprites.Rook;
            //        break;
            //    }
            //}

            //DrawSprite(ref buffer, sprite, boardX * SpriteWidth, boardY * SpriteWidth, SpriteWidth, SpriteWidth);
        }

        //private static void DrawSprite(ref Span<uint> buffer, Colors.ColorSpec[] sprite, int posX, int posY, int width, int height) {
        //    for (var x = 0; x < 8; x++) {
        //        for (var y = 0; y < 8; y++) {
        //            var pixel = y * 8 + x;

        //            SetPixel(ref buffer, posX + x, posY + y, sprite[pixel]);
        //        }
        //    }
        //}

        private static void DrawText(string text, int textX, int textY, Colors.ColorSpec color, bool clearBehind = false, int fontSize = FontSize) {
            text = text.ToUpper();

            int xOffset = 0;
            foreach (char character in text) {
                // Fetch font character from array
                // it is stores as uint64, and every bit represents a pixel
                var fontCharacter = Font.Data[character];

                for (var yPos = 0; yPos < fontSize; yPos++) {
                    for (var xPos = 0; xPos < fontSize; xPos++) {
                        // Don't forget to add positions + offset
                        var renderAtX = textX + xPos + xOffset;
                        var renderAtY = textY + yPos;

                        // The font is offset by 1 pixel,
                        // possibly more if fontsize isnt 8
                        var offset = fontSize / 8;
                        renderAtY += offset;

                        // In cases where font size is not 8 pixels, translate the iterated
                        // fontsize and map it to a position in the original 8x8 sprite
                        var translatedX = 8 * xPos / fontSize;
                        var translatedY = 8 * yPos / fontSize;

                        // Get which pixel to render and then if it is 1, render it
                        // This was hellish to get to work without inverted text
                        var bitPos = translatedY * 8 + translatedX;
                        if (((fontCharacter >>> (63 - bitPos)) & 1) == 1) {
                            SetPixel(renderAtX, renderAtY, color);
                        } else {
                            if (clearBehind) {
                                SetPixel(renderAtX, renderAtY, Colors.Black);
                            }
                        }
                    }
                }
                xOffset += fontSize;
            }
        }

        private static void DrawSquare(int squareX, int squareY, int width, int height, Colors.ColorSpec color) {
            for (var widthX = 0; widthX < width; widthX++) {
                for (var heightY = 0; heightY < height; heightY++) {
                    SetPixel(widthX + squareX, heightY + squareY, color);
                }
            }
        }

        private static void SetPixel(int x, int y, Colors.ColorSpec color) {
            if (x < 0 || x > WindowWidth || y < 0 || y >= WindowHeight) {
                return;
            }
            if (color.Equals(Colors.Empty)) {
                return;
            }

            FrameBuffer[
                x +
                (y * WindowWidth)
            ] = (uint)((color.r << 16) | (color.g << 8 | (color.b << 0)));
        }

        /// <summary>
        /// Copy the frame buffer to the SDL2 texture, and then copy that texture onto the screen
        /// </summary>
        /// <param name="buffer"></param>
        private static void DrawFrame() {
            unsafe {
                SDL_Rect rect;
                rect.w = WindowWidth * WindowSizeMultiplier;
                rect.h = WindowHeight * WindowSizeMultiplier;
                rect.x = 0;
                rect.y = 0;

                fixed (uint* pArray = FrameBuffer) {
                    var intPtr = new nint(pArray);

                    _ = SDL_UpdateTexture(Texture, ref rect, intPtr, WindowWidth * 4);
                }

                _ = SDL_RenderCopy(Renderer, Texture, nint.Zero, ref rect);
                SDL_RenderPresent(Renderer);
            }
        }
    }
}