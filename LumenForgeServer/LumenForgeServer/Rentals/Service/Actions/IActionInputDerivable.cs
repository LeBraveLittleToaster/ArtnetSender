namespace LumenForgeServer.Rentals.Service.Actions
{
    public interface IActionInputDerivable <T> where T : ActionInput
    {
        /// <summary>
        /// Executes the to action input operation.
        /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
        /// </summary>
        /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
        /// <returns>The operation result.</returns>
        public T ToActionInput();
    }
}
