using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiplayerMod;

namespace Tests
{
    [TestClass]
    public class MessageTests
    {
        [TestMethod]
        public void TestFloatMessageRoundTrip()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteFloat(0.1235f);
            msg.WriteFloat(0.2124f);
            msg.WriteFloat(0.135f);

            byte[] msgBytes = msg.GetBytes();
            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadFloat(), 0.1235f, 0.00001f);
            Assert.AreEqual(readMsg.ReadFloat(), 0.2124f, 0.00001f);
            Assert.AreEqual(readMsg.ReadFloat(), 0.135f, 0.00001f);
        }

        [TestMethod]
        public void TestByteMessageRoundTrip()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte(12);
            msg.WriteByte(2);
            msg.WriteByte(102);

            byte[] msgBytes = msg.GetBytes();
            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadByte(), 12);
            Assert.AreEqual(readMsg.ReadByte(), 2);
            Assert.AreEqual(readMsg.ReadByte(), 102);
        }
    }
}
