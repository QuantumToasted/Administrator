using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace Administrator.Common
{
    public static class ImageTools
    {
        public static class Fonts
        {
            internal static Font Header(float size)
                => SystemFonts.CreateFont("Mosk Bold 700", size);

            internal static Font Text(float size)
                => SystemFonts.CreateFont("Mosk Light 300", size);

            internal static Font TF2(float size)
                => SystemFonts.CreateFont("TF2 Build", size);

            internal static Font TF2Secondary(float size)
                => SystemFonts.CreateFont("TF2 Secondary", size);
        }

        public static class Colors
        {
            internal static Rgba32 GetGradeColor(Grade grade)
            {
                return grade switch
                {
                    Grade.Civilian => new Rgba32(176, 195, 217),
                    Grade.Freelance => new Rgba32(94, 152, 217),
                    Grade.Mercenary => new Rgba32(75, 105, 255),
                    Grade.Commando => new Rgba32(136, 71, 255),
                    Grade.Assassin => new Rgba32(211, 44, 230),
                    Grade.Elite => new Rgba32(235, 75, 75),
                    _ => Rgba32.White,
                };
            }

            internal static Rgba32 XpBar
                => new Rgba32(94, 151, 45);

            internal static Rgba32 Background
                => new Rgba32(44, 47, 51);

            internal static Rgba32 DarkButTransparent
                => new Rgba32(35, 39, 42, 225);

            internal static Rgba32 Blurple
                => new Rgba32(114, 137, 213);

            internal static Rgba32 LessDark
                => new Rgba32(67, 74, 79);

            internal static Rgba32 WayLessDark
                => new Rgba32(104, 110, 117);
        }

        public static Point Justify(Point point, Image image, Justification justification)
        {
            point = justification switch
            {
                Justification.TopLeft => new Point(point.X, point.Y),
                Justification.TopCenter => new Point(point.X - image.Width / 2, point.Y),
                Justification.TopRight => new Point(point.X - image.Width, point.Y),
                Justification.Left => new Point(point.X, point.Y - image.Height / 2),
                Justification.Center => new Point(point.X - image.Width / 2, point.Y - image.Height / 2),
                Justification.Right => new Point(point.X - image.Width, point.Y - image.Height / 2),
                Justification.BottomLeft => new Point(point.X, point.Y - image.Height),
                Justification.BottomCenter => new Point(point.X - image.Width / 2, point.Y - image.Height),
                Justification.BottomRight => new Point(point.X - image.Width, point.Y - image.Height),
                _ => new Point(point.X, point.Y)
            };

            return point;
        }
    }
}