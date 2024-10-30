using NUnit.Framework;
using KizhiPart2;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;

namespace KizhiPart2Tests
{
    [TestFixture]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            stream = new FileStream(streamPath, FileMode.Create, FileAccess.ReadWrite);
            writer = new StreamWriter(stream, Encoding.Default);
            reader = new StreamReader(stream, Encoding.Default);
            writer.AutoFlush = true;
            interpreter = new Interpreter(writer);
        }

        private string streamPath = @"C:\Users\Timur\Desktop\test\hta.txt";
        private FileStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        private Interpreter interpreter;

        [Test]
        public void EmptyNestedFunc()
        {
            var code = "" +
                "def cat\n" +
                "    def{def dog}\n" +
                "    call dog\n" +
                "call cat";

            interpreter.ExecuteLine(code);
            interpreter.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.Pass();
        }

        [Test]
        public void ShouldClearMemoryAfterInterpretation()
        {
            var code = "" +
                "print a\n" +
                "set a 5";

            interpreter.ExecuteLine(code);
            interpreter.ExecuteLine("run"); //очистится память после run
            interpreter.ExecuteLine("run"); //вызываеться в холостую

            stream.Position = 0;
            var str = reader.ReadToEnd();
            Assert.AreEqual("Переменная отсутствует в памяти\r\nПеременная отсутствует в памяти\r\n", str);
        }
        
        [Test]
        public void DoubleClosingBracketsInFunc()
        {
            var code = "" +
              "set cat 5\n" +
              "set dog 10\n" +
              "set wolf 15\n" +
              "set tiger 21\n" +
              "def cat\n" +
              "    print cat\n" +
              "    def{def dog\n" +
              "    print dog\n" +
              "    def{def wolf\n" +
              "    print wolf\n" +
              "    def{def tiger\n" +
              "    print tiger}}\n" +
              "    call wolf\n" +
              "    print dog}\n" +
              "    call dog\n" +
              "    print cat\n" +
              "call cat";

            interpreter.ExecuteLine(code);
            interpreter.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 10 15 10 5", str);
        }

        [TearDown]
        public void TearDown()
        {
            stream.Close();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            stream.Dispose();
        }
    }
}