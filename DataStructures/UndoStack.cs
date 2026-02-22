namespace HospitalManagementWPF.DataStructures
{
    /// <summary>
    /// Linked-List Stack for Undo operations.
    /// Each node stores an operation string and an optional data object for restoration.
    /// </summary>
    public class UndoStack
    {
        public class StackNode
        {
            public string Operation;
            public object? Data;
            public StackNode? Next;

            public StackNode(string operation, object? data)
            {
                Operation = operation;
                Data = data;
                Next = null;
            }
        }

        private StackNode? _top;

        public UndoStack()
        {
            _top = null;
        }

        /// <summary>
        /// Push a simple operation (no data).
        /// </summary>
        public void Push(string operation)
        {
            Push(operation, null);
        }

        /// <summary>
        /// Push an operation with associated data (e.g., for restoring deleted objects).
        /// </summary>
        public void Push(string operation, object? data)
        {
            StackNode newNode = new StackNode(operation, data);
            newNode.Next = _top;
            _top = newNode;
        }

        /// <summary>
        /// Pop: Returns only the operation string.
        /// </summary>
        public string? Pop()
        {
            if (IsEmpty) return null;
            string operation = _top!.Operation;
            _top = _top.Next;
            return operation;
        }

        /// <summary>
        /// PopWithData: Returns the entire node including stored data.
        /// </summary>
        public StackNode? PopWithData()
        {
            if (IsEmpty) return null;
            StackNode node = _top!;
            _top = _top.Next;
            return node;
        }

        public object? PeekData()
        {
            if (IsEmpty) return null;
            return _top!.Data;
        }

        public string? PeekOperation()
        {
            if (IsEmpty) return null;
            return _top!.Operation;
        }

        public bool IsEmpty => _top == null;
    }
}
