using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace png_to_screen_thingy
{
    internal class Program
    {
        public static Color Blend(Color colA, Color colB, double amount)
        {
            byte r = (byte)(colA.R * amount + colB.R * (1 - amount));
            byte g = (byte)(colA.G * amount + colB.G * (1 - amount));
            byte b = (byte)(colA.B * amount + colB.B * (1 - amount));
            return Color.FromArgb(255, r, g, b);
        }

        public static Color[] ConsoleCols = new Color[16];
        public static ConsoleColor[] _ConsoleCols = new ConsoleColor[16];
        public static Color[,,] MixedConsoleCols = new Color[16, 8, 256];

        public static Dictionary<(int r, int g, int b), (char chr, ConsoleColor fg, ConsoleColor bg)> Mappings = new Dictionary<(int r, int g, int b), (char chr, ConsoleColor fg, ConsoleColor bg)>();

        public static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b)
        {
            int index = 0;
            double minDistance = double.MaxValue;
            for (int i = 0; i < ConsoleCols.Length; i++)
            {
                var c = ConsoleCols[i];
                double distance = Math.Sqrt(Math.Pow(c.R - r, 2.0) + Math.Pow(c.G - g, 2.0) + Math.Pow(c.B - b, 2.0));
                if (distance < minDistance)
                {
                    index = i;
                    minDistance = distance;
                }
            }
            return _ConsoleCols[index];
        }

        public static Color GetColorFromConsoleColor(ConsoleColor col)
        {
            var n = Enum.GetName(typeof(ConsoleColor), col);
            return System.Drawing.Color.FromName(n == "DarkYellow" ? "Orange" : n); // bug fix
        }

        public static void FillBasicConsoleColorArr()
        {
            for (int i = 0; i < ConsoleCols.Length; i++)
            {
                ConsoleCols[i] = GetColorFromConsoleColor((ConsoleColor)i);
                _ConsoleCols[i] = (ConsoleColor)i;
            }
        }

        public static void FillMixedColorArr()
        {
            for (int a = 0; a < MixedConsoleCols.GetLength(0); a++)
                for (int b = 0; b < MixedConsoleCols.GetLength(1); b++)
                    for (int c = 0; c < MixedConsoleCols.GetLength(2); c++)
                        MixedConsoleCols[a, b, c] = Blend(ConsoleCols[a], ConsoleCols[b], (c / 255.0));
        }

        public static Color ColorBlend(List<Color> clrArr)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            foreach (Color color in clrArr)
            {
                r += color.R;
                g += color.G;
                b += color.B;
            }
            r = r / clrArr.Count;
            g = g / clrArr.Count;
            b = b / clrArr.Count;
            return Color.FromArgb(r, g, b);
        }

        public static (char chr, ConsoleColor fg, ConsoleColor bg) GetClosestRGBCombo(int r, int g, int b)
        {
            // search through the array and find the closest color

            if (Mappings.ContainsKey((r, g, b)))
                return Mappings[(r, g, b)];

            int bg = 0, fg = 0, chr = 0;
            double delta = double.MaxValue;

            for (int _a = 0; _a < MixedConsoleCols.GetLength(0); _a++)
                for (int _b = 0; _b < MixedConsoleCols.GetLength(1); _b++)
                    for (int _c = 0; _c < MixedConsoleCols.GetLength(2); _c++)
                    {
                        var t = Math.Pow(MixedConsoleCols[_a, _b, _c].R - r, 2.0) + Math.Pow(MixedConsoleCols[_a, _b, _c].G - g, 2.0) + Math.Pow(MixedConsoleCols[_a, _b, _c].B - b, 2.0);
                        if (t == 0.0)
                            return (map[_c], _ConsoleCols[_a], _ConsoleCols[_b]);
                        if (t < delta)
                        {
                            delta = t;

                            fg = _a;
                            bg = _b;
                            chr = _c;
                        }
                    }

            Mappings[(r, g, b)] = (map[chr], _ConsoleCols[fg], _ConsoleCols[bg]);

            return (map[chr], _ConsoleCols[fg], _ConsoleCols[bg]);
        }

        public static string map;
        public static (char chr, ConsoleColor fg, ConsoleColor bg)[] VgaMap = new (char chr, ConsoleColor fg, ConsoleColor bg)[256];
        public static (char chr, ConsoleColor fg, ConsoleColor bg)[] RGBMap;

        static void FillMappingsWithVGA()
        {
            //Bitmap img = new Bitmap("vga a.png");

            //for (int y = 0; y < img.Height; y++)
            //    for (int x = 0; x < img.Width; x++)
            //    {
            //        var c = img.GetPixel(x, y);
            //        VgaMap[x + y * 16] = GetClosestRGBCombo(c.R, c.G, c.B);
            //    }

            const int shiftAmt = 6;
            int _256Shift = 256 >> shiftAmt;
            RGBMap = new (char chr, ConsoleColor fg, ConsoleColor bg)[_256Shift * _256Shift * _256Shift];

            for (int r = 0; r < _256Shift; r++)
            {
                for (int g = 0; g < _256Shift; g++)
                    for (int b = 0; b < _256Shift; b++)
                    {
                        int aR = r << shiftAmt;
                        int aG = g << shiftAmt;
                        int aB = b << shiftAmt;

                        RGBMap[r + (g * _256Shift) + (b * _256Shift * _256Shift)] = GetClosestRGBCombo(aR, aG, aB);
                    }
                Console.WriteLine($"{r}/{_256Shift}");
            }
            Console.Clear();
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
                return;

            map = File.ReadAllText("map.txt");

            FillBasicConsoleColorArr();

            FillMixedColorArr();

            if (true)
            {
                FillMappingsWithVGA();
                using (StreamWriter writer = new StreamWriter("vgamap.txt"))
                {
                    foreach (var group in VgaMap)
                        writer.WriteLine($"{(int)group.fg},{(int)group.bg},{(int)group.chr}");
                }
                using (StreamWriter writer = new StreamWriter("rgbmap.txt"))
                {
                    foreach (var group in RGBMap)
                        writer.WriteLine($"{(int)group.fg},{(int)group.bg},{(int)group.chr}");
                }
            }
            else
            {
                // load
                using (StreamReader reader = new StreamReader("vgamap.txt"))
                {
                    int l = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        var split = reader.ReadLine().Split(',');
                        VgaMap[i] = ((char)int.Parse(split[2]), _ConsoleCols[int.Parse(split[0])], _ConsoleCols[int.Parse(split[1])]);
                    }
                }

                using (StreamReader reader = new StreamReader("rgbmap.txt"))
                {
                    int l = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        var split = reader.ReadLine().Split(',');
                        VgaMap[i] = ((char)int.Parse(split[2]), _ConsoleCols[int.Parse(split[0])], _ConsoleCols[int.Parse(split[1])]);
                    }
                }


                {
                    Bitmap img2 = new Bitmap("vga a.png");
                    int i = 0;
                    for (int y = 0; y < img2.Height; y++)
                        for (int x = 0; x < img2.Width; x++)
                        {
                            var c = img2.GetPixel(x, y);
                            Mappings[(c.R, c.G, c.B)] = VgaMap[i];
                            i++;
                        }
                }
            }



            // 80 x 25
            Bitmap img = new Bitmap(args[0]);

            int sizeX = 80;
            int sizeY = 25;

            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    int x1 = (x * img.Width) / sizeX;
                    int y1 = (y * img.Height) / sizeY;
                    int x2 = ((x + 1) * img.Width - 1) / sizeX;
                    int y2 = ((y + 1) * img.Height - 1) / sizeY;

                    //Color c = img.GetPixel(x1, y1);
                    Color c;
                    if (false)
                    {
                        List<Color> tempList = new List<Color>();

                        // go through the scaled rect and get the average color
                        for (int _y = y1; _y <= y2; _y++)
                            for (int _x = x1; _x <= x2; _x++)
                                tempList.Add(img.GetPixel(_x, _y));

                        c = ColorBlend(tempList);
                    }
                    else
                        c = img.GetPixel(x1, y1);

                    const int shiftAmt = 7;

                    int r = (c.R >> shiftAmt) << shiftAmt;
                    int g = (c.G >> shiftAmt) << shiftAmt;
                    int b = (c.B >> shiftAmt) << shiftAmt;

                    var data = GetClosestRGBCombo(r, g, b); ;// GetClosestRGBCombo(c.R, c.G, c.B);

                    //data.fg = ClosestConsoleColor(c.R, c.G, c.B);
                    //data.bg = ConsoleColor.Black;
                    //data.chr = '#';


                    Console.ForegroundColor = data.fg;
                    Console.BackgroundColor = data.bg;
                    Console.Write(data.chr);
                }
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("\n\n");
            Console.WriteLine($"COLS: {Mappings.Count}");
            Console.WriteLine("\n\nEnd.");
            Console.ReadLine();
            return;
        }
    }
}
