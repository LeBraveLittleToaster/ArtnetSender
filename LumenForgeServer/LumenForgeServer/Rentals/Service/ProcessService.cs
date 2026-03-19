using LumenForgeServer.Rentals.Domain;
using System.Diagnostics;

namespace LumenForgeServer.Rentals.Service
{
    public class ProcessService
    {
        public Task<List<Process>> GetActiveProcesses() {
            return null;
        }

        public Task<ProcessInstance> StartProcess(ProcessRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<List<ActionType>> GetAvailableActions(Guid processInstanceGuid) {
            throw new NotImplementedException();
        }

        public Task CreateNextStep(Guid processInstanceGuid, ActionType actionType) {
            throw new NotImplementedException();
        }

        public Task<SubmitActionResult> SubmitAction(ActionType actionType, SubmitActionInput submitActionInput)  {
            throw new NotImplementedException();
        }
    }
}
