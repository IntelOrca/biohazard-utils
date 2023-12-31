﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace IntelOrca.Biohazard
{
    public class RE3Archive : IDisposable
    {
        private const ulong IdentCompressed = 0x00706D6F435F6948;

        private readonly static ushort[] g_baseArray = new ushort[] {
            0x00E6, 0x01A4, 0x00E6, 0x01C5,
            0x0130, 0x00E8, 0x03DB, 0x008B,
            0x0141, 0x018E, 0x03AE, 0x0139,
            0x00F0, 0x027A, 0x02C9, 0x01B0,
            0x01F7, 0x0081, 0x0138, 0x0285,
            0x025A, 0x015B, 0x030F, 0x0335,
            0x02E4, 0x01F6, 0x0143, 0x00D1,
            0x0337, 0x0385, 0x007B, 0x00C6,
            0x0335, 0x0141, 0x0186, 0x02A1,
            0x024D, 0x0342, 0x01FB, 0x03E5,
            0x01B0, 0x006D, 0x0140, 0x00C0,
            0x0386, 0x016B, 0x020B, 0x009A,
            0x0241, 0x00DE, 0x015E, 0x035A,
            0x025B, 0x0154, 0x0068, 0x02E8,
            0x0321, 0x0071, 0x01B0, 0x0232,
            0x02D9, 0x0263, 0x0164, 0x0290
        };

        private readonly string _path;
        private readonly FileStream _fs;
        private readonly List<File> _files;
        private readonly object _sync = new object();

        public int NumFiles => _files.Count;

        public RE3Archive(string path)
        {
            _path = path;
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(_fs);
            var a = br.ReadUInt32();
            var b = br.ReadUInt32();
            var c = br.ReadUInt32();
            var d = br.ReadUInt16();

            var directories = new List<string>();
            for (int i = 0; i < 256; i++)
            {
                var level = br.ReadUInt24();
                var size = br.ReadUInt24();
                var attributes = br.ReadByte();
                var name = ReadNullTerminatedString(br, true);
                if (name.Length == 0)
                    break;

                directories.Add(name);
            }
            var basePath = string.Join("/", directories);

            _fs.Position = 0x1000;
            var files = new List<File>();
            var numFiles = br.ReadUInt32();
            for (int i = 0; i < numFiles; i++)
            {
                var offset = br.ReadInt32() * 8;
                var length = br.ReadInt32();
                var name = ReadNullTerminatedString(br);
                var filePath = basePath + '/' + name;
                files.Add(new File(filePath, offset, length));
            }
            _files = files;
        }

        public void Dispose()
        {
            _fs.Dispose();
        }

        public string this[int index] => _files[index].Path;

        public string[] Files => _files.Select(x => x.Path).ToArray();

        private static string ReadNullTerminatedString(BinaryReader br, bool align = false)
        {
            var sb = new StringBuilder();
            byte b;
            while ((b = br.ReadByte()) != 0)
            {
                sb.Append((char)b);
            }
            if (align)
            {
                if ((sb.Length & 1) == 0)
                {
                    br.ReadByte();
                }
            }
            return sb.ToString();
        }

        public byte[] GetFileContents(string path)
        {
            var index = _files.FindIndex(x => x.Path == path);
            if (index == -1)
                throw new ArgumentException("Path not found", nameof(path));

            return GetFileContents(index);
        }

        public byte[] GetFileContents(int index)
        {
            var file = _files[index];
            lock (_sync)
            {
                _fs.Position = file.Offset;
                return DecryptFile(new BinaryReader(_fs));
            }
        }

        private byte[] DecryptFile(BinaryReader br)
        {
            var offset = br.ReadUInt16();
            var numKeys = br.ReadUInt16();
            var length = br.ReadUInt32();
            var ident = br.ReadBytes(8);

            for (int i = 0; i < 8; i++)
                ident[i] ^= ident[7];
            var compressed = BitConverter.ToUInt64(ident, 0) == IdentCompressed;

            var arrayKeys = new uint[numKeys];
            for (int i = 0; i < numKeys; i++)
                arrayKeys[i] = br.ReadUInt32();

            var arrayLength = new uint[numKeys];
            for (int i = 0; i < numKeys; i++)
                arrayLength[i] = br.ReadUInt32();

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            var blockMs = new MemoryStream();
            for (int i = 0; i < numKeys; i++)
            {
                if (compressed)
                {
                    blockMs.Position = 0;
                    DecryptBlock(new BinaryWriter(blockMs), br, arrayKeys[i], arrayLength[i]);
                    blockMs.Position = 0;
                    DecompressFile(new BinaryReader(blockMs), bw, arrayLength[i]);
                }
                else
                {
                    DecryptBlock(bw, br, arrayKeys[i], arrayLength[i]);
                }
            }
            ms.SetLength(length);
            return ms.ToArray();
        }

        private void DecryptBlock(BinaryWriter bw, BinaryReader br, uint key, uint length)
        {
            var xorKey = NextKey(ref key);
            var modulo = NextKey(ref key);
            var baseIndex = modulo % 0x3F;
            var blockIndex = 0;
            for (uint i = 0; i < length; i++)
            {
                if (blockIndex > g_baseArray[baseIndex])
                {
                    modulo = NextKey(ref key);
                    baseIndex = modulo % 0x3F;
                    xorKey = NextKey(ref key);
                    blockIndex = 0;
                }
                var src = br.ReadByte();
                var dst = (byte)(src ^ xorKey);
                bw.Write(dst);
                blockIndex++;
            }
        }

        private static byte NextKey(ref uint key)
        {
            key *= 0x5d588b65;
            key += 0x8000000b;
            return (byte)(key >> 24);
        }

        private static void DecompressFile(BinaryReader br, BinaryWriter bw, uint length)
        {
            int dstLength = 32768;

            var src = br.ReadBytes((int)length);
            var dst = ArrayPool<byte>.Shared.Rent(0x10000);
            try
            {
                var tmp4k = new byte[4096 + 256];
                int dstIndex;

                int srcNumBit, srcIndex, tmpIndex;
                int i, value, value2, tmpStart, tmpLength;

                for (i = 0; i < 256; i++)
                {
                    var tmp = i * 16;
                    for (int ii = 0; ii < 16; ii++)
                        tmp4k[tmp++] = (byte)i;
                }
                Array.Clear(tmp4k, 4096, 256);

                srcNumBit = 0;
                srcIndex = 0;
                tmpIndex = 0;
                dstIndex = 0;
                while (srcIndex < length && dstIndex < dstLength)
                {
                    srcNumBit++;

                    value = src[srcIndex++] << srcNumBit;
                    if (srcIndex < length)
                        value |= src[srcIndex] >> (8 - srcNumBit);

                    if (srcNumBit == 8)
                    {
                        srcIndex++;
                        srcNumBit = 0;
                    }

                    if ((value & (1 << 8)) == 0)
                    {
                        dst[dstIndex++] = tmp4k[tmpIndex++] = (byte)value;
                    }
                    else
                    {
                        value2 = (src[srcIndex++] << srcNumBit) & 0xff;
                        if (srcIndex < length)
                            value2 |= src[srcIndex] >> (8 - srcNumBit);

                        tmpLength = (value2 & 0x0f) + 2;

                        tmpStart = (value2 >> 4) & 0xfff;
                        tmpStart |= (value & 0xff) << 4;

                        if (dstIndex + tmpLength > dstLength)
                        {
                            tmpLength = dstLength - dstIndex;
                        }

                        Array.Copy(tmp4k, tmpStart, dst, dstIndex, tmpLength);
                        Array.Copy(dst, dstIndex, tmp4k, tmpIndex, tmpLength);

                        dstIndex += tmpLength;
                        tmpIndex += tmpLength;
                    }

                    if (tmpIndex >= 4096)
                    {
                        tmpIndex = 0;
                    }
                }
                bw.Write(dst, 0, dstIndex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dst);
            }
        }

        public void Extract(string destination)
        {
            for (int i = 0; i < _files.Count; i++)
            {
                var path = Path.Combine(destination, _files[i].Path);
                var dir = Path.GetDirectoryName(path);
                Directory.CreateDirectory(dir);

                var contents = GetFileContents(i);
                System.IO.File.WriteAllBytes(path, contents);
            }
        }

        [DebuggerDisplay("{Path}")]
        private struct File
        {
            public string Path { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }

            public File(string path, int offset, int length)
            {
                Path = path;
                Offset = offset;
                Length = length;
            }
        }
    }
}
