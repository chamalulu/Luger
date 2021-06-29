using System;
using System.Text.RegularExpressions;

namespace RenderSandBox
{
    public class RenderOptions
    {
        public int Width { get; set; } = 320;

        public int Height { get; set; } = 240;

        private static readonly Regex SizeRex = new(@"^(?<width>\d+)x(?<height>\d+)$");

        public string Size
        {
            get => $"{Width}x{Height}";
            set
            {
                var match = SizeRex.Match(value);

                if (match.Success)
                {
                    (Width, Height) = (int.Parse(match.Groups["width"].Value), int.Parse(match.Groups["height"].Value));
                }
                else
                {
                    throw new FormatException();
                }
            }
        }

        public string OutFile { get; set; } = "render_output.png";
    }
}
