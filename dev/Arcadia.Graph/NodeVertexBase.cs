namespace Arcadia.Graph
{
    using System;
    using System.ComponentModel;
    using Arcadia;
    using Arcadia.Graph;

    public class NodeVertexBase : INotifyPropertyChanged, INodeVertex
    {
        public event PropertyChangedEventHandler PropertyChanged;

        INode _node;

        public NodeVertexBase(INode node)
        { 
            _node = node;
            _node.Changed += new ChangedEventHandler((sender,args) => RaisePropertyChanged("Dirty"));
            _node.Changed += new ChangedEventHandler((sender, args) => RaisePropertyChanged("Status"));
        }

        public virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public INode Node { get { return _node; } }

        public string Id { get { return _node.Id; } }

        public bool IsInput { get { return _node.IsInput; } }

        public int Status
        {
            get
            {
                if (_node.Status == NodeStatus.Valid) { return 2; }
                if (_node.Status == NodeStatus.Error) { return 0; }
                return 1; // Dirty
            }
        }

        public bool Dirty { get { return _node.IsDirty; } }

        public override string ToString() { return string.Format("{0}-{1}",Id,IsInput); }
    }
}