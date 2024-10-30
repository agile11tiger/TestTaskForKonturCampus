using VirtualMachine.Core;
using VirtualMachine.Core.Debugger.Model;

namespace VirtualMachine.Core.Debugger.Server.BreakPoints
{
	public class BreakPointsConverter
	{
		public IBreakPoint FromDto(BreakPointDto dto)
		{
			return new BreakPoint(new Word(dto.Address), dto.Name, dto.Condition);
		}

		public BreakPointDto ToDto(IBreakPoint bp)
		{
            return new BreakPointDto
            {
                Name = bp.Name,
                Address = bp.Address.ToUInt(),
                Condition = bp.Condition?.InitialCondition
			};
		}

		private class BreakPoint : IBreakPoint
		{
			public BreakPoint(Word address, string name, string condition)
			{
				Address = address;
				Name = name;
                Condition = condition is null ? null : new BreakPointCondition(condition);
			}

			public Word Address { get; }
			public string Name { get; }
            public BreakPointCondition Condition { get; }

            public bool ShouldStop(IReadOnlyMemory memory)
            {
                if (Condition is null)
                    return true;
                else
                    return Condition.CheckVeracity(memory);
            }
		}
	}
}