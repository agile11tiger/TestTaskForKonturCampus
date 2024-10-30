using System.Threading;
using VirtualMachine.Core;

namespace VirtualMachine.CPU.InstructionSet.Instructions
{
    public class Sleep : InstructionBase
    {
        public Sleep(OperandType first) : base(9, first, OperandType.Ignored, OperandType.Ignored)
        { }

        protected override void ExecuteInternal(ICpu _, IMemory __, Operand sleepDuration, Operand ___, Operand ____)
        {
            Thread.Sleep(sleepDuration.Value.ToInt());
        }
    }
}
