using System;
using System.Text;
using System.Security.Cryptography;

namespace AssetStudio
{
    public class PGR
    {
        private const string Header = "#$unity3dchina!@";

        private static readonly Aes Aes;
        private static readonly string[] Keys =
        {
            "kurokurokurokuro",
            "y5XPvqLOrCokWRIa"
        };

        private static ICryptoTransform Encryptor;

        public byte[] Index = new byte[0x10];
        public byte[] Sub = new byte[0x10];

        static PGR()
        {
            Aes = Aes.Create("AesManaged");
            Aes.Mode = CipherMode.ECB;
            Encryptor = Aes.CreateEncryptor();
        }

        public PGR(EndianBinaryReader reader)
        {
            reader.ReadUInt32();

            var (data1, key1) = ReadVector(reader);
            var (data2, key2) = ReadVector(reader);

            DecryptKey(key2, data2);

            var str = Encoding.UTF8.GetString(data2);
            if (str != Header)
                throw new Exception("Invalid Signature !!");

            DecryptKey(key1, data1);

            data1 = data1.ToUInt4Array();
            Array.Copy(data1, Index, 0x10);
            for (var (i, j) = (0, 0x10); i < 4; i++, j += 4)
            {
                Sub[i] = data1[j];
                Sub[i + 4] = data1[j + 1];
                Sub[i + 8] = data1[j + 2];
                Sub[i + 12] = data1[j + 3];
            }
        }

        public static void UpdateKey(int version)
        {
            var selectedKey = Keys[version];
            var keyBytes = Encoding.UTF8.GetBytes(selectedKey);

            Aes.Key = keyBytes;

            Encryptor = Aes.CreateEncryptor();
        }

        public void DecryptBlock(byte[] bytes, int size, int index)
        {
            var offset = 0;
            do
            {
                offset += Decrypt(bytes.AsSpan(offset), index++, size - offset);
            } while (offset < size);
        }

        private (byte[], byte[]) ReadVector(EndianBinaryReader reader)
        {
            var data = reader.ReadBytes(0x10);
            var key = reader.ReadBytes(0x10);
            reader.ReadByte();

            return (data, key);
        }

        private void DecryptKey(byte[] key, byte[] data)
        {
            key = Encryptor.TransformFinalBlock(key, 0, key.Length);
            for (int i = 0; i < 0x10; i++)
                data[i] ^= key[i];
        }

        private int DecryptByte(Span<byte> bytes, ref int offset, ref int index)
        {
            var b = Sub[((index >> 2) & 3) + 4] + Sub[index & 3] + Sub[((index >> 4) & 3) + 8] + Sub[((byte)index >> 6) + 12];
            bytes[offset] = (byte)((Index[bytes[offset] & 0xF] - b) & 0xF | 0x10 * (Index[bytes[offset] >> 4] - b));
            b = bytes[offset];
            offset++;
            index++;
            return b;
        }

        private int Decrypt(Span<byte> bytes, int index, int remaining)
        {
            var offset = 0;
            
            var curByte = DecryptByte(bytes, ref offset, ref index);
            var byteHigh = curByte >> 4;
            var byteLow = curByte & 0xF;

            if (byteHigh == 0xF)
            {
                int b;
                do
                {
                    b = DecryptByte(bytes, ref offset, ref index);
                    byteHigh += b;
                } while (b == 0xFF);
            }

            offset += byteHigh;

            if (offset < remaining)
            {
                DecryptByte(bytes, ref offset, ref index);
                DecryptByte(bytes, ref offset, ref index);
                if (byteLow == 0xF)
                {
                    int b;
                    do
                    {
                        b = DecryptByte(bytes, ref offset, ref index);
                    } while(b == 0xFF);
                }
            }

            return offset;
        }
    }
}