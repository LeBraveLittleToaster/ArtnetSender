namespace LumenForgeServer.Rentals.Service.Actions
{
    public interface IActionInputDerivable <T> where T : ActionInput
    {
        public T ToActionInput();
    }
}
