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
        int length = 0;
        List<byte[]> byteChunks = new List<byte[]>();
        byte[] rBytes;
        int rPos = 0;

        public P2PMessage()
        {
        }

        public P2PMessage(byte[] bytes)
        {
            this.length = bytes.Length;
            rBytes = bytes;
        }

        public byte[] GetBytes()
        {
            int totalLength = 0;
            foreach (Byte[] chunk in byteChunks)
            {
                totalLength += chunk.Length;
            }

            byte[] bArr = new byte[totalLength];
            int cPos = 0;

            foreach (Byte[] chunk in byteChunks)
            {
                chunk.CopyTo(bArr, cPos);
                cPos += chunk.Length;
            }

            return bArr;
        }

        public void WriteByte(byte b)
        {
            byteChunks.Add(new byte[] { b });
            length += 1;
        }

        public void WriteFloat(float f)
        {
            byte[] bytes = new byte[sizeof(float)];
            BitConverter.GetBytes(f).CopyTo(bytes, 0);
            byteChunks.Add(bytes);
            length += sizeof(float);
        }

        public void WriteVector3(Vector3 v3)
        {
            WriteFloat(v3.x);
            WriteFloat(v3.y);
            WriteFloat(v3.z);
        }

        public void WriteQuaternion(Quaternion q)
        {
            WriteFloat(q.x);
            WriteFloat(q.y);
            WriteFloat(q.z);
            WriteFloat(q.w);
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
    }
}
