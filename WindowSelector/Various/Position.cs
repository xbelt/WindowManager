namespace WindowSelector
{
    public class Position
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Position(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y + " Width: " + Width + " Height: " + Height;
        }

        public string Pretty()
        {
            return "X: " + X.ToString("##.00") + " | Y: " + Y.ToString("##.00") + " | Width: " + Width.ToString("##.00") + " | Height: " + Height.ToString("##.00");
        }
    }
}