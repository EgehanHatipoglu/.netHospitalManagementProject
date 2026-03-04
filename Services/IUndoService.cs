namespace HospitalManagementAvolonia.Services
{
    public interface IUndoService
    {
        void RecordOperation(string opStr);
        string? UndoLastOperation();
        string Peek();
    }
}
