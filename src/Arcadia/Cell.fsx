
//delegate CellChange of CellEventType
//
//type ICell =
//    abstract Lock : unit -> unit
//    abstract Unlock : unit -> unit
//    
//    event CellChange Change

//    /// <summary>
//    /// Notification that a cell has changed
//    /// </summary>
//    /// <param name="eventType">type of event passed</param>
//    /// <param name="sender">the cell that triggered this change</param>
//    /// <param name="epoch">the time epoch of the original source change.. used for throttling</param>
//    /// <param name="transaction">optionaly the transaction that is completing</param>
//    public delegate void CellChange (CellEventType eventType, ICell sender, DateTime epoch, ITransaction transaction);
//
//    /// <summary>
//    /// Calllback notification for the completion of all dependant changes in a transaction
//    /// </summary>
//    /// <param name="transaction">the transaction being completed</param>
//    public delegate void TransactionComplete (ITransaction transaction);


//    public interface ICell
//    {
//        void Lock ();
//        void Unlock ();
//        event CellChange Change;
//        void OnChange (CellEventType eventType, ICell root, DateTime epoch, ITransaction transaction);
//        bool HasListeners { get; }
//        // The parent property provides for instruments to be composed of a number of cells
//        IModel Parent { get; set; }
//        String Mnmonic { get; set; }
//        ITransaction Transaction { get; set; }
//        CellMethod Method { get; }
//        DateTime Epoch { get; }
//        /// <summary>
//        /// get the value of the Cell in a generic form
//        /// </summary>
//        object BoxValue { get; }
//        /// <summary>
//        /// copy the current state from the other cell of the same type as this one
//        /// </summary>
//        /// <param name="other">the other cell that has the same type</param>
//        void Copy (ICell other);
//
//        /// <summary>
//        /// Transfer any subscriptions from this object to the new destination object 
//        /// </summary>
//        /// <param name="destination">the recipient of change subscriptions</param>
//        void TransferTo (ICell destination);
//    }