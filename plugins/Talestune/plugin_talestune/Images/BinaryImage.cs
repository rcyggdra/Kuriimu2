using Komponent.IO;
using Kompression;
using Kontract.Models.Image;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace plugin_talestune.Images
{

    public class BinaryImage
    {
        public bool IsCompressed { get; private set; }

        public BinaryImage()
        {
            IsCompressed = true;
        }

        public ImageInfo Load(Stream fileStream)
        {
            if (fileStream.ReadByte() == 0x10)
            {
                IsCompressed = true;
                var sizeBuffer = new byte[4];
                fileStream.Read(sizeBuffer, 0, 3);
                var decompressedSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer);
                using (MemoryStream ms = new MemoryStream())
                {
                    fileStream.Seek(-4, SeekOrigin.Current);
                    Kompression.Implementations.Compressions.Nintendo.Lz10.Build().Decompress(fileStream, ms);
                    byte[] data = ms.ToArray();
                    using (MemoryStream bimg = new MemoryStream(data))
                    {
                        return LoadFile(bimg);
                    }
                }
            }
            else
            {
                fileStream.Seek(-1, SeekOrigin.Current);
                return LoadFile(fileStream);
            }
        }

        private ImageInfo LoadFile(Stream bimg)
        {
            BinaryReaderX br = new BinaryReaderX(bimg);
            var header = br.ReadType<BinaryImageHeader>();
            var imageDataSize = BinaryImageSupport.CalculateImageDataSize(header);
            if ((bimg.Length - 6) < imageDataSize)
            {
                throw new InvalidDataException();
            }

            ImageInfo imageInfo = new ImageInfo(br.ReadBytes(imageDataSize), (int)header.PixelFormat, header.Size);

            return imageInfo;
        }

        public void Save(Stream fileStream, ImageInfo imageInfo)
        {
            throw new NotImplementedException();
        }
    }
}
