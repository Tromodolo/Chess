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
                    RenderToMove();
                    RenderGameEnd();
                    RenderTakenPieces();
                }

                var textPadding = 8;
                var textStartX = WindowWidth / 2 + textPadding;

                // Misc texts
                DrawText("Press 'r' to restart", textStartX, textPadding, Colors.White);
                //DrawText("lily = big dumb", textStartX, FontSize + textPadding, Colors.White);

                DrawFrame();
            }
        }

        private static void HandleEventLoop() {
            while (SDL_PollEvent(out var e) == 1) {
                switch (e.type) {
                    case SDL_EventType.SDL_KEYDOWN:
                        if (e.key.keysym.sym == SDL_Keycode.SDLK_r) {
                            Chess.LoadFromFENString("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
                            Chess.GenerateMoves();
                        }
                        break;
                    case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                        HandleClick(e.button.x, e.button.y);
                        break;
                    case SDL_EventType.SDL_QUIT:
                        Playing = false;
                        break;
                }
            }
        }

        private static void HandleClick(int x, int y) {
            var file = x / SpriteWidth / WindowSizeMultiplier;
            var rank = y / SpriteWidth / WindowSizeMultiplier;
            Chess.OnSquarePressed(file, rank);
        }

        private static void RenderToMove() {
            var textPadding = FontSize / 2;
            var textStartX = WindowWidth / 2 + textPadding;

            var toMoveString = Chess.ToMove == ChessLogic.PieceColor.White ? "White" : "Black";
            DrawText("To move", textStartX, FontSize * 4, Colors.White, fontSize: FontSize * 2);
            DrawText(toMoveString, textStartX, FontSize * 6, Colors.White, true, FontSize * 2);
        }

        private static void RenderGameEnd() {
            var textPadding = FontSize / 2;
            var textStartX = WindowWidth / 2 + textPadding;

            if (Chess.GameEnded) {
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
                DrawText("Game Ended", textStartX, FontSize * 10, Colors.White, true, FontSize * 2);
                DrawText(resultText, textStartX, FontSize * 12, Colors.White, true, FontSize * 2);
            }
        }

        private static void RenderTakenPieces() {
            var textPadding = FontSize / 2;
            var textStartX = WindowWidth / 2 + textPadding;

            Chess.WhiteTaken.Sort();
            Chess.BlackTaken.Sort();

            var drawTakenSprites = (int y, List<ChessLogic.PieceType> list) => {
                int xOffset = 0;

                foreach (var type in list) {
                    ulong sprite;
                    switch (type) {
                        case ChessLogic.PieceType.Pawn:
                            sprite = ChessLogic.Sprites.Pawn;
                            break;
                        case ChessLogic.PieceType.Knight:
                            sprite = ChessLogic.Sprites.Knight;
                            break;
                        case ChessLogic.PieceType.Bishop:
                            sprite = ChessLogic.Sprites.Bishop;
                            break;
                        case ChessLogic.PieceType.Rook:
                            sprite = ChessLogic.Sprites.Rook;
                            break;
                        case ChessLogic.PieceType.Queen:
                            sprite = ChessLogic.Sprites.Queen;
                            break;
                        case ChessLogic.PieceType.King:
                            sprite = ChessLogic.Sprites.King;
                            break;
                        default:
                            sprite = 0;
                            break;
                    }

                    DrawData(sprite, textStartX + xOffset, y, Colors.White, true, FontSize * 2);

                    // Add x offset on every drawn sprite
                    // And if offset is large enough, split to second row
                    xOffset += FontSize * 2;
                    if (xOffset >= FontSize * 16) {
                        xOffset = 0;
                        y += FontSize * 2;
                    }
                }
            };

            DrawText("White's Taken:", textStartX, FontSize * 16, Colors.White, fontSize: FontSize * 2);
            drawTakenSprites(FontSize * 18, Chess.WhiteTaken);

            DrawText("Black's Taken:", textStartX, FontSize * 24, Colors.White, fontSize: FontSize * 2);
            drawTakenSprites(FontSize * 26, Chess.BlackTaken);
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

        private static void RenderPiece(ChessPiece piece, int file, int rank) {
            Colors.ColorSpec color = Colors.Beige;
            if (piece.Color == ChessLogic.PieceColor.Black) {
                color = Colors.Black;
            }

            ulong sprite;
            switch(piece.Type) {
                case ChessLogic.PieceType.Pawn:
                    sprite = ChessLogic.Sprites.Pawn;
                    break;
                case ChessLogic.PieceType.Knight:
                    sprite = ChessLogic.Sprites.Knight;
                    break;
                case ChessLogic.PieceType.Bishop:
                    sprite = ChessLogic.Sprites.Bishop;
                    break;
                case ChessLogic.PieceType.Rook:
                    sprite = ChessLogic.Sprites.Rook;
                    break;
                case ChessLogic.PieceType.Queen:
                    sprite = ChessLogic.Sprites.Queen;
                    break;
                case ChessLogic.PieceType.King:
                    sprite = ChessLogic.Sprites.King;
                    break;
                default:
                    sprite = 0;
                    break;
            }

            DrawData(sprite, file * SpriteWidth, rank * SpriteWidth, color, size: SpriteWidth);
        }

        private static void DrawText(string text, int textX, int textY, Colors.ColorSpec color, bool clearBehind = false, int fontSize = FontSize) {
            text = text.ToUpper();

            int xOffset = 0;
            // The font is offset by 1 pixel,
            // possibly more if fontsize isnt 8
            var yOffset = fontSize / 8;
            textY += yOffset;

            foreach (char character in text) {
                // Fetch font character from array
                // it is stores as uint64, and every bit represents a pixel
                var fontCharacter = Font.Data[character];

                DrawData(fontCharacter, textX + xOffset, textY, color, clearBehind, fontSize);
             
                xOffset += fontSize;
            }
        }

        /// <summary>
        /// Takes in a ulong and iterates through the binary for the values to get sprite shape
        /// </summary>
        /// <param name="data"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="clearBehind"></param>
        /// <param name="size"></param>
        private static void DrawData(ulong data, int x, int y, Colors.ColorSpec color, bool clearBehind = false, int size = FontSize) {
            for (var yPos = 0; yPos < size; yPos++) {
                for (var xPos = 0; xPos < size; xPos++) {
                    // Don't forget to add positions + offset
                    var renderAtX = x + xPos;
                    var renderAtY = y + yPos;

                    // In cases where data is not 8 pixels, translate the iterated
                    // size and map it to a position in the original 8x8 sprite
                    var translatedX = 8 * xPos / size;
                    var translatedY = 8 * yPos / size;

                    // Get which pixel to render and then if it is 1, render it
                    // This was hellish to get to work without inverted text
                    var bitPos = translatedY * 8 + translatedX;
                    if (((data >>> (63 - bitPos)) & 1) == 1) {
                        SetPixel(renderAtX, renderAtY, color);
                    } else {
                        if (clearBehind) {
                            SetPixel(renderAtX, renderAtY, Colors.Black);
                        }
                    }
                }
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