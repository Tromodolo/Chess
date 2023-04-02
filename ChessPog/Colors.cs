namespace ChessPog {
    internal class Colors {
        internal struct ColorSpec {
            internal int r;
            internal int g;
            internal int b;

            internal ColorSpec(int r, int g, int b) {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            internal bool Equals(ColorSpec other) {
                return r == other.r 
                    && b == other.b 
                    && g == other.g;
            }

            public static implicit operator ColorSpec(uint u) {
                return new ColorSpec(
                    (int)((u >> 16) & 0xFF),
                    (int)((u >> 8) & 0xFF),
                    (int)(u & 0xFF)
                );
            }
        }
        internal static ColorSpec Empty = new ColorSpec(-1, -1, -1);

        internal static ColorSpec White = new ColorSpec(255, 255, 255);
        internal static ColorSpec Black = new ColorSpec(0, 0, 0);
        internal static ColorSpec Gray = new ColorSpec(128, 128, 128);
        internal static ColorSpec Green = new ColorSpec(118, 150, 86);
        internal static ColorSpec Tan = new ColorSpec(238, 238, 210);
        internal static ColorSpec Beige = new ColorSpec(250, 235, 215);
        internal static ColorSpec Orange = new ColorSpec(218, 129, 85);
        internal static ColorSpec Yellow = new ColorSpec(218, 194, 85);
    }
}
