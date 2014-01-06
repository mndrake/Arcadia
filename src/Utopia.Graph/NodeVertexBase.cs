namespace Utopia.Graph
{
    using System;
    using System.ComponentModel;
    using Utopia;
    using Utopia.Graph;

    public class NodeVertexBase : INotifyPropertyChanged, INodeVertex
    {
        public event PropertyChangedEventHandler PropertyChanged;

        INode _node;

        public NodeVertexBase(INode node)
        { 
            _node = node;
            _node.Changed += new ChangedEventHandler((sender,args) => RaisePropertyChanged("Dirty"));
        }

        public virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public INode Node { get { return _node; } }

        public string Id { get { return _node.Id; } }

        public bool IsInput { get { return _node.IsInput; } }

        public string Status 
        { 
            get 
            {
                if (_node.Status.IsDirty) { return "Dirty"; }

                return null;
            }
        }

        public bool Dirty { get { return _node.Dirty; } }

        public override string ToString() { return string.Format("{0}-{1}",Id,IsInput); }
    }
}