using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Kanvas;
using Kanvas.Encoding;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_talestune.Images
{
    public enum BinaryPixelFormat : ushort
    {
        Rgb888 = 0,
        Abgr8888,
        Rgb565,
        Rgba4444,
        Rgba5551,
        La44,
        La88,
        L8,
        A8,
    }

    public class BinaryImageHeader
    {
        public BinaryPixelFormat PixelFormat;
        public ushort Width;
        public ushort Height;

        public Size Size => new Size(Width, Height);

        public void Load(BinaryReaderX br)
        {
            br.ReadType<BinaryImageHeader>();
        }
    }

    public class BinaryImageSupport
    {
        private static readonly IDictionary<int, IColorEncoding> PixelFormats = new Dictionary<int, IColorEncoding>
        {
            [(ushort)BinaryPixelFormat.Rgb888] = ImageFormats.Rgb888(),
            [(ushort)BinaryPixelFormat.Abgr8888] = Abgr8888(),
            [(ushort)BinaryPixelFormat.Rgb565] = ImageFormats.Rgb565(),
            [(ushort)BinaryPixelFormat.Rgba5551] = ImageFormats.Rgba5551(),
            [(ushort)BinaryPixelFormat.La44] = ImageFormats.La44(),
            [(ushort)BinaryPixelFormat.La88] = ImageFormats.La88(),
            [(ushort)BinaryPixelFormat.L8] = ImageFormats.L8(),
            [(ushort)BinaryPixelFormat.A8] = ImageFormats.A8(),
        };

        private static readonly IDictionary<BinaryPixelFormat, int> PixelByteSizes = new Dictionary<BinaryPixelFormat, int>
        {
            [BinaryPixelFormat.Rgb888] = 3,
            [BinaryPixelFormat.Abgr8888] = 4,
            [BinaryPixelFormat.Rgb565] = 2,
            [BinaryPixelFormat.Rgba5551] = 2,
            [BinaryPixelFormat.La44] = 1,
            [BinaryPixelFormat.La88] = 2,
            [BinaryPixelFormat.L8] = 1,
            [BinaryPixelFormat.A8] = 1,
        };

        public static IColorEncoding Abgr8888(ByteOrder byteOrder = ByteOrder.LittleEndian) => new Rgba(8, 8, 8, 8, "ABGR", byteOrder);

        public static int CalculateImageDataSize(BinaryImageHeader header)
        {
            return header.Width * header.Height * PixelFormats[(int)header.PixelFormat].BitDepth / 8;
        }

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(PixelFormats);
            return definition;
        }
    }
}
