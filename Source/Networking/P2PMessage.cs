using Il2CppSystem.Text;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using StressLevelZero.VRMK;

namespace MultiplayerMod.Networking
{
    public static class QuatExt
    {
        // Unity normally provides this, but it's been stripped in Boneworks :(
        public static float Idx(this Quaternion q, int idx)
        {
            switch (idx)
            {
                case 0:
                    return q.x;
                case 1:
                    return q.y;
                case 2:
                    return q.z;
                case 3:
                    return q.w;
                default:
                    return 0f;
            }
        }
    }

    public class P2PMessage
    {
        readonly List<byte[]> byteChunks = new List<byte[]>();
        readonly byte[] rBytes;
        int rPos = 0;

        // Blank constructor?
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

        public void WriteSignedByte(sbyte s)
        {
            byteChunks.Add(new byte[] { (byte)s });
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

            short x = (short)difference.x;
            short y = (short)difference.y;
            short z = (short)difference.z;

            WriteShort(x);
            WriteShort(y);
            WriteShort(z);
        }

        public void WriteQuaternion(Quaternion q)
        {
            WriteFloat(q.x);
            WriteFloat(q.y);
            WriteFloat(q.z);
            WriteFloat(q.w);
        }

        private const float FLOAT_PRECISION_MULT = 32767.0f;

        public void WriteCompressedQuaternion(Quaternion rotation)
        {
            var maxIndex = (byte)0;
            var maxValue = float.MinValue;
            var sign = 1f;

            // Determine the index of the largest (absolute value) element in the Quaternion.
            // We will transmit only the three smallest elements, and reconstruct the largest
            // element during decoding. 
            for (int i = 0; i < 4; i++)
            {
                var element = rotation.Idx(i);
                var abs = Mathf.Abs(rotation.Idx(i));
                if (abs > maxValue)
                {
                    // We don't need to explicitly transmit the sign bit of the omitted element because you 
                    // can make the omitted element always positive by negating the entire quaternion if 
                    // the omitted element is negative (in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                    // represent the same rotation.), but we need to keep track of the sign for use below.
                    sign = (element < 0) ? -1 : 1;

                    // Keep track of the index of the largest element
                    maxIndex = (byte)i;
                    maxValue = abs;
                }
            }

            // If the maximum value is approximately 1f (such as Quaternion.identity [0,0,0,1]), then we can 
            // reduce storage even further due to the fact that all other fields must be 0f by definition, so 
            // we only need to send the index of the largest field.
            if (Mathf.Approximately(maxValue, 1f))
            {
                // Again, don't need to transmit the sign since in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                // represent the same rotation. We only need to send the index of the single element whose value
                // is 1f in order to recreate an equivalent rotation on the receiver.
                WriteByte((byte)(maxIndex + 4));
                WriteShort(0);
                WriteShort(0);
                WriteShort(0);
                return;
            }

            var a = (short)0;
            var b = (short)0;
            var c = (short)0;

            // We multiply the value of each element by QUAT_PRECISION_MULT before converting to 16-bit integer 
            // in order to maintain precision. This is necessary since by definition each of the three smallest 
            // elements are less than 1.0, and the conversion to 16-bit integer would otherwise truncate everything 
            // to the right of the decimal place. This allows us to keep five decimal places.

            if (maxIndex == 0)
            {
                a = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
                b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
                c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
            }
            else if (maxIndex == 1)
            {
                a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
                b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
                c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
            }
            else if (maxIndex == 2)
            {
                a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
                b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
                c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
            }
            else
            {
                a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
                b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
                c = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
            }

            WriteByte(maxIndex);
            WriteShort(a);
            WriteShort(b);
            WriteShort(c);
        }

        // Smaller but lower quality
        public void WriteSmallerCompressedQuaternion(Quaternion q)
        {
            WriteSignedByte((sbyte)(q.x * 127.0f));
            WriteSignedByte((sbyte)(q.y * 127.0f));
            WriteSignedByte((sbyte)(q.z * 127.0f));
            WriteSignedByte((sbyte)(q.w * 127.0f));
        }

        public void WriteUnicodeString(string str)
        {
            byte[] bArr = System.Text.Encoding.UTF8.GetBytes(str);
            WriteByte((byte)bArr.Length);
            byteChunks.Add(bArr);
        }

        public void WriteUlong(ulong l)
        {
            byte[] bArr = BitConverter.GetBytes(l);
            byteChunks.Add(bArr);
        }

        public Quaternion ReadCompressedQuaternion()
        {
            // Read the index of the omitted field from the stream.
            var maxIndex = ReadByte();

            // Values between 4 and 7 indicate that only the index of the single field whose value is 1f was
            // sent, and (maxIndex - 4) is the correct index for that field.
            if (maxIndex >= 4 && maxIndex <= 7)
            {
                var x = (maxIndex == 4) ? 1f : 0f;
                var y = (maxIndex == 5) ? 1f : 0f;
                var z = (maxIndex == 6) ? 1f : 0f;
                var w = (maxIndex == 7) ? 1f : 0f;

                ReadByte();
                ReadByte();
                ReadByte();
                return new Quaternion(x, y, z, w);
            }

            // Read the other three fields and derive the value of the omitted field
            var a = (float)ReadShort() / FLOAT_PRECISION_MULT;
            var b = (float)ReadShort() / FLOAT_PRECISION_MULT;
            var c = (float)ReadShort() / FLOAT_PRECISION_MULT;
            var d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            if (maxIndex == 0)
                return new Quaternion(d, a, b, c);
            else if (maxIndex == 1)
                return new Quaternion(a, d, b, c);
            else if (maxIndex == 2)
                return new Quaternion(a, b, d, c);

            return new Quaternion(a, b, c, d);
        }

        public Quaternion ReadSmallerCompressedQuaternion()
        {
            return new Quaternion(ReadSignedByte() / 127.0f, ReadSignedByte() / 127.0f, ReadSignedByte() / 127.0f, ReadSignedByte() / 127.0f);
        }

        public Vector3 ReadCompressedVector3(Vector3 basis, float range = 2.0f)
        {
            short x = ReadShort();
            short y = ReadShort();
            short z = ReadShort();

            float fX = x / (float)short.MaxValue * range;
            float fY = y / (float)short.MaxValue * range;
            float fZ = z / (float)short.MaxValue * range;

            return new Vector3(fX, fY, fZ) + basis;
        }

        public byte ReadByte()
        {
            byte v = rBytes[rPos];
            rPos++;
            return v;
        }

        public sbyte ReadSignedByte()
        {
            sbyte v = (sbyte)rBytes[rPos];
            rPos++;
            return v;
        }

        public float ReadFloat()
        {
            float v = BitConverter.ToSingle(rBytes, rPos);
            rPos += sizeof(float);
            return v;
        }

        public short ReadShort()
        {
            short x = BitConverter.ToInt16(rBytes, rPos);
            rPos += sizeof(short);
            return x;
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
