using HospitalManagementAvolonia.DataStructures;

namespace HospitalManagementAvolonia.Services
{
    public class UndoService : IUndoService
    {
        private readonly UndoStack _undoStack = new();

        public void RecordOperation(string opStr)
        {
            _undoStack.Push(opStr);
        }

        public string? UndoLastOperation()
        {
            return _undoStack.Pop();
        }

        public string Peek()
        {
            return _undoStack.PeekOperation() ?? string.Empty;
        }
    }
}
