using KizhiPart3;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace KizhiPart3Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            stream = new FileStream(streamPath, FileMode.Create, FileAccess.ReadWrite);
            writer = new StreamWriter(stream, Encoding.Default);
            reader = new StreamReader(stream, Encoding.Default);
            writer.AutoFlush = true;
            debugger = new Debugger(writer);
        }

        private string streamPath = @"C:\Users\Timur\Desktop\test\hta.txt";
        private FileStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        private Debugger debugger;

        [Test]
        public void SimpleFunc()
        {
            var command = "" +
                "set cat 5\n" +
                "def cat\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd().Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5", str);
        }

        [TearDown]
        public void TearDown()
        {
            stream.Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            stream.Close();
        }

        [Test]
        public void ShouldStepOverWork()
        {
            var code = "" +
                "def test\n" +
                "    set a 4\n" +
                "    set b 5\n" +
                "set t 5\n" +
                "call test\n" +
                "print a";

            debugger.ExecuteLine(code);
            debugger.ExecuteLine("add break 1");
            debugger.ExecuteLine("add break 4");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("step over");
            debugger.ExecuteLine("step");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("4", str);
        }

        [Test]
        public void ShouldClearMemoryAfterInterpretation()
        {
            var code = "" +
                "print a\n" +
                "set a 5";

            debugger.ExecuteLine(code);
            debugger.ExecuteLine("run"); //очистится память после run
            debugger.ExecuteLine("run"); //вызываеться в холостую

            stream.Position = 0;
            var str = reader.ReadToEnd();
            Assert.AreEqual("Переменная отсутствует в памяти\r\nПеременная отсутствует в памяти\r\n", str);
        }

        [Test]
        public void SimpleTest()
        {
            var command = "set a 5\nprint a";
            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("5", str);
        }

        [Test]
        public void CallAfterDef_Nested()
        {
            var command = "" +
                "set a 5\n" +
                "call dog\n" +
                "def cat\n" +
                "    def{def dog\n" +
                "    print a}";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("5", str);
        }

        [Test]
        public void CallAfterDef()
        {
            var command = "" +
                "set a 5\n" +
                "call test\n" +
                "def test\n" +
                "    print a";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("5", str);
        }

        public void Eternal()
        {
            var command = "" +
                "def cat\n" +
                "    call cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("5", str);
        }

        [Test]
        public void EmtyFunc()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            Assert.AreEqual("", str);
        }

        [Test]
        public void EmtyNastedFunc1()
        {
            var command = "" +
                   "def cat\n" +
                   "    def{def dog}\n" +
                   "    call dog\n" +
                   "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual(null, str);
        }

        [Test]
        public void EmtyNastedFunc()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    print cat\n" +
                "    def{def dog}\n" +
                "    call dog\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 5", str);
        }

        [Test]
        public void EmtyNastedFuncDoubleBrackets()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    print cat\n" +
                "    def{def dog\n" +
                "    def{def wolf}}\n" +
                "    call dog\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 5", str);
        }

        [Test]
        public void FuncCallFunc()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    call dog\n" +
                "def dog\n" +
                "    print dog\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("10", str);
        }

        [Test]
        public void FuncInFunc()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    print cat\n" +
                "    def{def dog\n" +
                "    print dog}\n" +
                "    call dog\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 10 5", str);
        }

        [Test]
        public void DoubleClosingBrackets()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "set wolf 15\n" +
                "def cat\n" +
                "    print cat\n" +
                "    def{def dog\n" +
                "    print dog\n" +
                "    def{def wolf\n" +
                "    print wolf}}\n" +
                "    call dog\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 10 5", str);
        }

        [Test]
        public void DoubleClosingBracketsInFunc()
        {
            var command = "" +
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
                "    call dog" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 10 15 10 5", str);
        }

        [Test]
        public void BreakPoint()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    call dog\n" +
                "def dog\n" +
                "    print dog\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 6");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("step over");

            stream.Position = 0;
            var str = reader.ReadLine(); 
            Assert.AreEqual("10", str);
        }

        [Test]
        public void S()
        {
            var command = "" +
                "set cat 5\n" +
                "set dog 10\n" +
                "def cat\n" +
                "    call dog\n" +
                "def dog\n" +
                "    print dog\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 6");
            debugger.ExecuteLine("add break 3");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("step over");
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadLine();
            Assert.AreEqual("10", str);
        }

        [Test]
        public void PrintMem()
        {
            var command = "" +
                "def cat\n" +
                "    set cat 5\n" +
                "    call dog\n" +
                "def dog\n" +
                "    set dog 10\n" +
                "call cat\n" +
                "set cat 111";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 6");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("print mem");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("cat 5 1 dog 10 4", str);
        }

        [Test]
        public void SimplePrintTrace()
        {
            var command = "" +
                "def cat\n" +
                "    set cat 5\n" +
                "    call dog\n" +
                "def dog\n" +
                "    set dog 10\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 1");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("print trace");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5 cat", str);
        }

        [Test]
        public void PrintTraceFuncCallFunc()
        {
            var command = "" +
                "def cat\n" +
                "    set cat 5\n" +
                "    call dog\n" +
                "def dog\n" +
                "    set dog 10\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 4");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("print trace");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("2 dog 5 cat", str);
        }

        [Test]
        public void PrintTrace()
        {
            var command = "" +
                "def test\n" +
                "    set a 4\n" +
                "set t 5\n" +
                "call test\n" +
                "sub a 3\n" +
                "call test\n" +
                "print a";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 1");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("print trace");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("run");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("3 test 4", str);
        }

        [Test]
        public void ClearPrintTrace()
        {
            var command = "" +
                "def cat\n" +
                "    set cat 5\n" +
                "    call dog\n" +
                "def dog\n" +
                "    set dog 10\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("print trace");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("", str);
        }


        [Test]
        public void StepOver()
        {
            var command = "" +
                "set cat 5\n" +
                "def cat\n" +
                "    print cat\n" +
                "call cat";

            debugger.ExecuteLine(command);
            debugger.ExecuteLine("add break 2");
            debugger.ExecuteLine("add break 3");
            debugger.ExecuteLine("run");
            debugger.ExecuteLine("step over");

            stream.Position = 0;
            var str = reader.ReadToEnd();
            str = str.Replace("\r\n", " ").TrimEnd();
            Assert.AreEqual("5", str);
        }
    }
}