using System;
using System.Collections.Generic;

namespace VirtualMachine.Core.Debugger.Server.BreakPoints
{
    public class BreakPointCondition
    {
        public readonly string InitialCondition;

        public BreakPointCondition(string str)
        {
            InitialCondition = str;
            var array = str.Split(' ');

            if (array.Length != 3)
                throw new FormatException("The format should be like this: operand comparisonOperator operand");

            ConvertToWord(array[0], ref firstArgConst, ref firstArgAddress);
            comparisonOperator = array[1];
            ConvertToWord(array[2], ref secondArgConst, ref secondArgAddress);
        }

        public bool CheckVeracity(IReadOnlyMemory memory)
        {
            var arg1 = firstArgConst != Word.Zero ? firstArgConst : memory.ReadWord(firstArgAddress);
            var arg2 = secondArgConst != Word.Zero ? secondArgConst : memory.ReadWord(secondArgAddress);
            return comparisonOperators[comparisonOperator](arg1, arg2);
        }

        private static readonly Dictionary<string, Func<Word, Word, bool>> comparisonOperators = new Dictionary<string, Func<Word, Word, bool>>
        {
            { "==", (a, b) => a == b },
            { "!=", (a, b) => a != b },
            { "<=", (a, b) => a <= b },
            { "<",  (a, b) => a < b },
            { ">=", (a, b) => a >= b },
            { ">",  (a, b) => a > b }
        };
        private string comparisonOperator;
        private Word firstArgConst;
        private Word firstArgAddress;
        private Word secondArgConst;
        private Word secondArgAddress;

        private void ConvertToWord(string str, ref Word argConst, ref Word argAddress)
        {
            if (str.Substring(0, 3) == "mem")
                argAddress = new Word(Convert.ToUInt32(str.Substring(4, str.Length - 5), 16));
            else
                argConst = new Word(Convert.ToUInt32(str, 16));
        }
    }
}
