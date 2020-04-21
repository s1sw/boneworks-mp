using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiplayerMod;

namespace Tests
{
    [TestClass]
    public class MessageTests
    {
        private const float TEST_MARGIN = 0.00001f;

        [TestMethod]
        public void TestFloatMessageRoundTrip()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteFloat(0.1235f);
            msg.WriteFloat(0.2124f);
            msg.WriteFloat(0.135f);

            byte[] msgBytes = msg.GetBytes();
            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadFloat(), 0.1235f, TEST_MARGIN);
            Assert.AreEqual(readMsg.ReadFloat(), 0.2124f, TEST_MARGIN);
            Assert.AreEqual(readMsg.ReadFloat(), 0.135f, TEST_MARGIN);
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

        [TestMethod]
        public void TestStringMessageRoundTrip()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteUnicodeString("hello world!");

            byte[] msgBytes = msg.GetBytes();

            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadUnicodeString(), "hello world!");
        }

        [TestMethod]
        public void TestMultipleRoundTrip()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte(172);
            msg.WriteUnicodeString("hello world! Зарегистрируйтесь ⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌");
            msg.WriteFloat(1412.2f);

            byte[] msgBytes = msg.GetBytes();

            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadByte(), 172);
            Assert.AreEqual(readMsg.ReadUnicodeString(), "hello world! Зарегистрируйтесь ⡌⠁⠧⠑ ⠼⠁⠒  ⡍⠜⠇⠑⠹⠰⠎ ⡣⠕⠌");
            Assert.AreEqual(readMsg.ReadFloat(), 1412.2f, TEST_MARGIN);
        }

        [TestMethod]
        public void TestPlayerNameMessage()
        {
            PlayerNameMessage pnm = new PlayerNameMessage();
            pnm.name = "Someone Somewhere";

            byte[] msgBytes = pnm.MakeMsg().GetBytes();

            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadByte(), (byte)MessageType.PlayerName);

            PlayerNameMessage pnm2 = new PlayerNameMessage(readMsg);
            Assert.AreEqual(pnm2.name, "Someone Somewhere");
        }

        [TestMethod]
        public void TestOtherPlayerNameMessage()
        {
            OtherPlayerNameMessage opnm = new OtherPlayerNameMessage();
            opnm.playerId = 124;
            opnm.name = "Someone Somewhere";

            byte[] msgBytes = opnm.MakeMsg().GetBytes();

            P2PMessage readMsg = new P2PMessage(msgBytes);

            Assert.AreEqual(readMsg.ReadByte(), (byte)MessageType.OtherPlayerName);

            OtherPlayerNameMessage opnm2 = new OtherPlayerNameMessage(readMsg);
            Assert.AreEqual(opnm2.playerId, 124);
            Assert.AreEqual(opnm2.name, "Someone Somewhere");
        }
    }
}
