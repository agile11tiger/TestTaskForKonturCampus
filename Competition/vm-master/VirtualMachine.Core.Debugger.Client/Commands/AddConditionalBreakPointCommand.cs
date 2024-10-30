using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualMachine.Core.Debugger.Model;

namespace VirtualMachine.Core.Debugger.Client.Commands
{
    public class AddConditionalBreakPointCommand : ICommand
    {
        public string Name { get; } = "bp-add-c";
        public string Info { get; } = "Add conditional break point";
        public IReadOnlyList<string> ParameterNames { get; } = new[] { "name", "address", "condition" };
        
        public Task ExecuteAsync(DebuggerModel model, string[] parameters)
        {
            var bp = new BreakPointDto
            {
                Name = parameters[0],
                Address = uint.Parse(parameters[1]),
                Condition = parameters[2]
            };
            return model.Client.AddBreakPointAsync(bp);
        }
    }
}
