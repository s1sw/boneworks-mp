using Il2CppSystem.Text;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod
{
    public class P2PMessage
    {
        readonly List<byte[]> byteChunks = new List<byte[]>();
        readonly byte[] rBytes;
        int rPos = 0;

        public P2PMessage()
        {
        }

        public P2PMessage(byte[] bytes)
        {
            rBytes = bytes;
        }

        public byte[] GetBytes()
        {
            int totalLength = 0;
            foreach (byte[] chunk in byteChunks)
            {
                totalLength += chunk.Length;
            }

            byte[] bArr = new byte[totalLength];
            int cPos = 0;

            foreach (byte[] chunk in byteChunks)
            {
                chunk.CopyTo(bArr, cPos);
                cPos += chunk.Length;
            }

            return bArr;
        }

        public void WriteByte(byte b)
        {
            byteChunks.Add(new byte[] { b });
        }

        public void WriteFloat(float f)
        {
            byteChunks.Add(BitConverter.GetBytes(f));
        }

        public void WriteShort(short s)
        {
            byteChunks.Add(BitConverter.GetBytes(s));
        }

        public void WriteVector3(Vector3 v3)
        {
            WriteFloat(v3.x);
            WriteFloat(v3.y);
            WriteFloat(v3.z);
        }

        public void WriteCompressedVector3(Vector3 v3, Vector3 basis, float range = 2.0f)
        {
            Vector3 difference = v3 - basis;
            difference *= short.MaxValue / range;


        }

        public void WriteQuaternion(Quaternion q)
        {
            WriteFloat(q.x);
            WriteFloat(q.y);
            WriteFloat(q.z);
            WriteFloat(q.w);
        }

        // TOOD: Storing the largest index as a whole byte
        // is pretty wasteful.
        public void WriteCompressedQuaternion(Quaternion q)
        {
            byte largestIndex = 255;
            float largest = float.MinValue;

            Vector3 components = new Vector3();

            if (Mathf.Abs(q.w) > largest)
            {
                largest = q.w;

                largestIndex = 0;
                components.x = q.x;
                components.y = q.y;
                components.z = q.z;
            }

            if (Mathf.Abs(q.x) > largest)
            {
                largest = q.x;

                largestIndex = 1;
                components.x = q.w;
                components.y = q.y;
                components.z = q.z;
            }

            if (Mathf.Abs(q.y) > largest)
            {
                largest = q.y;
                largestIndex = 2;

                components.x = q.w;
                components.y = q.x;
                components.z = q.z;
            }

            if (Mathf.Abs(q.z) > largest)
            {
                largest = q.z;
                largestIndex = 3;

                components.x = q.w;
                components.y = q.x;
                components.z = q.y;
            }

            // Negative and positive quaternions represent the same rotation,
            // so to avoid sending a sign over the network
            // we can just make everything else negative.
            if (largest < 0.0f)
                components *= -1.0f;

            // Compress components

            byte cX, cY, cZ;

            cX = (byte)((components.x + 1.0f) * 127);
            cY = (byte)((components.y + 1.0f) * 127);
            cZ = (byte)((components.z + 1.0f) * 127);

            WriteByte(largestIndex);
            WriteByte(cX);
            WriteByte(cY);
            WriteByte(cZ);
        }

        public Quaternion ReadCompressedQuaternion()
        {
            byte largestIndex = ReadByte();
            byte cA = ReadByte();
            byte cB = ReadByte();
            byte cC = ReadByte();

            float a = (cA / 127.0f) - 1.0f;
            float b = (cB / 127.0f) - 1.0f;
            float c = (cC / 127.0f) - 1.0f;

            // Unity's Mathf is really slow due to IL2CPP but we can't use .NET's MathF either :(
            float largest = (float)Math.Sqrt(1 - (a * a) - (b * b) - (c * c));

            switch (largestIndex)
            {
                case 0:
                    return new Quaternion(a, b, c, largest);
                case 1:
                    return new Quaternion(largest, b, c, a);
                case 2:
                    return new Quaternion(b, largest, c, a);
                case 3:
                    return new Quaternion(b, c, largest, a);
            }

            return Quaternion.identity;
        }

        public void WriteUnicodeString(string str)
        {
            byte[] bArr = System.Text.Encoding.UTF8.GetBytes(str);
            WriteByte((byte)bArr.Length);
            byteChunks.Add(bArr);
        }

        public byte ReadByte()
        {
            byte v = rBytes[rPos];
            rPos++;
            return v;
        }

        public float ReadFloat()
        {
            float v = BitConverter.ToSingle(rBytes, rPos);
            rPos += sizeof(float);
            return v;
        }

        public Vector3 ReadVector3()
        {

            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }


        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }

        public ulong ReadUlong()
        {
            ulong id = BitConverter.ToUInt64(rBytes, rPos);
            rPos += sizeof(ulong);
            return id;
        }

        public string ReadString()
        {
            byte length = ReadByte();
            char[] str = new char[length];

            for (int i = 0; i < length; i++)
                str[i] = (char)ReadByte();


            return new string(str);
        }

        public string ReadUnicodeString()
        {
            byte length = ReadByte();
            string ret = System.Text.Encoding.UTF8.GetString(rBytes, rPos, length);
            rPos += length;

            return ret;
        }
    }
}
