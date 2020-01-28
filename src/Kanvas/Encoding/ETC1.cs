﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Kanvas.Encoding.BlockCompressions.ETC1;
using Kanvas.Encoding.BlockCompressions.ETC1.Models;
using Kanvas.Support;
using Komponent.IO;
using Kontract.Kanvas;
using Kontract.Models.IO;

namespace Kanvas.Encoding
{
    /// <summary>
    /// Defines the ETC1 encoding.
    /// </summary>
    public class ETC1 : IColorEncoding
    {
        private bool _useAlpha;
        private ByteOrder _byteOrder;

        private Decoder _decoder;
        private Encoder _encoder;

        /// <inheritdoc cref="IColorEncoding.BitDepth"/>
        public int BitDepth { get; }

        /// <summary>
        /// The number of bits one block contains of.
        /// </summary>
        public int BlockBitDepth { get; }

        /// <inheritdoc cref="IColorEncoding.FormatName"/>
        public string FormatName { get; }

        /// <inheritdoc cref="IColorEncoding.IsBlockCompression"/>
        public bool IsBlockCompression => true;

        public ETC1(bool useAlpha, bool useZOrder, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            BitDepth = useAlpha ? 8 : 4;
            BlockBitDepth = useAlpha ? 128 : 64;

            _useAlpha = useAlpha;
            _byteOrder = byteOrder;

            _decoder = new Decoder(useZOrder);
            _encoder = new Encoder(useZOrder);

            FormatName = "ETC1" + (useAlpha ? "A4" : "");
        }

        public IEnumerable<Color> Load(byte[] tex)
        {
            using var br = new BinaryReaderX(new MemoryStream(tex), _byteOrder);

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var alpha = _useAlpha ? br.ReadUInt64() : ulong.MaxValue;
                var colors = br.ReadUInt64();

                foreach (var color in _decoder.Get(colors, alpha))
                    yield return color;
            }
        }

        //private Etc1PixelData GetPixelData(BinaryReader br)
        //{
        //    var etc1Alpha = UseAlpha ? Conversion.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder) : ulong.MaxValue;
        //    var colorBlock = Conversion.FromByteArray<ulong>(br.ReadBytes(8), ByteOrder);
        //    var etc1Block = new Block
        //    {
        //        LSB = (ushort)(colorBlock & 0xFFFF),
        //        MSB = (ushort)((colorBlock >> 16) & 0xFFFF),
        //        Flags = (byte)((colorBlock >> 32) & 0xFF),
        //        B = (byte)((colorBlock >> 40) & 0xFF),
        //        G = (byte)((colorBlock >> 48) & 0xFF),
        //        R = (byte)((colorBlock >> 56) & 0xFF)
        //    };

        //    return new Etc1PixelData
        //    {
        //        Alpha = etc1Alpha,
        //        Block = etc1Block
        //    };
        //}

        public byte[] Save(IEnumerable<Color> colors)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.ASCII, true))
            {
                foreach (var color in colors)
                    _encoder.Set(color, data => SetPixelData(bw, data));
            }

            return ms.ToArray();
        }

        //private void SetPixelData(BinaryWriter bw, Etc1PixelData data)
        //{
        //    if (UseAlpha)
        //        bw.Write(Conversion.ToByteArray(data.Alpha, 8, ByteOrder));

        //    ulong colorBlock = 0;
        //    colorBlock |= data.Block.LSB;
        //    colorBlock |= ((ulong)data.Block.MSB << 16);
        //    colorBlock |= ((ulong)data.Block.Flags << 32);
        //    colorBlock |= ((ulong)data.Block.B << 40);
        //    colorBlock |= ((ulong)data.Block.G << 48);
        //    colorBlock |= ((ulong)data.Block.R << 56);

        //    bw.Write(Conversion.ToByteArray(colorBlock, 8, ByteOrder));
        //}
    }
}
