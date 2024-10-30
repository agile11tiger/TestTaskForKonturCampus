using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ImageParser
{
    public class ImageInfo
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string Format { get; set; }
        public long Size { get; set; }

        public ImageInfo(int height, int width, string format, long size)
        {
            Height = height;
            Width = width;
            Format = format;
            Size = size;
        }
    }

    public class ImageParser : IImageParser
    {
        private Dictionary<byte[], Func<BinaryReader, ImageInfo>> imageFormatDecoders;

        public ImageParser()
        {
            imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, ImageInfo>>()
            {
                { new byte[]{ 0x42, 0x4D }, DecodeBitmap},
                { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
                { new byte[]{ 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
                { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng }
            };
        }

        public string GetImageInfo(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                var maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;
                var magicBytes = new byte[maxMagicBytesLength];

                for (var i = 0; i < maxMagicBytesLength; i++)
                {
                    magicBytes[i] = reader.ReadByte();

                    foreach (var pair in imageFormatDecoders)
                    {
                        if (StartsWith(magicBytes, pair.Key))
                        {
                            return JsonConvert.SerializeObject(pair.Value(reader), Formatting.Indented);
                        }
                    }
                }
            }

            return null;
        }

        private bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            for (var i = 0; i < thatBytes.Length; i++)
                if (thisBytes[i] != thatBytes[i])
                    return false;

            return true;
        }

        private ImageInfo DecodeBitmap(BinaryReader reader)
        {
            reader.ReadBytes(16);
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            return new ImageInfo(height, width, "Bmp", reader.BaseStream.Length);
        }

        private ImageInfo DecodeGif(BinaryReader reader)
        {
            var width = reader.ReadInt16();
            var height = reader.ReadInt16();
            return new ImageInfo(height, width, "Gif", reader.BaseStream.Length);
        }

        private ImageInfo DecodePng(BinaryReader reader)
        {
            reader.ReadBytes(8);
            var width = ReadLittleEndianInt32(reader);
            var height = ReadLittleEndianInt32(reader);
            return new ImageInfo(height, width, "Png", reader.BaseStream.Length);
        }

        private int ReadLittleEndianInt32(BinaryReader reader)
        {
            var bytes = new byte[sizeof(int)];

            for (var i = bytes.Length - 1; i >= 0; i--)
                bytes[i] = reader.ReadByte();

            return BitConverter.ToInt32(bytes, 0);
        }
    }
}