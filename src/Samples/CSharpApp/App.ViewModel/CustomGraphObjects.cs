namespace CSharpApp.ViewModel
{
    using System;
    using System.Collections.Generic;
    using GraphSharp.Controls;
    using QuickGraph;
    using Utopia;
    using Utopia.ViewModel;

    public class CustomVertex : ViewModelBase
    {
        public CustomVertex(INode node)
            : base()
        {
            Node = node;

            Node.Changed += new ChangedEventHandler(OnNodeChanged);
        }

        void OnNodeChanged(object sender, EventArgs args)
        {
            this.OnPropertyChanged("Value");
            this.OnPropertyChanged("Dirty");
        }

        public INode Node { get; private set; }

        public string ID { get { return Node.ID; } }

        public bool IsInput { get { return Node.DependentNodes.Length == 0; } }

        public string Value
        {
            get
            {
                if (Node.UntypedValue != null) { return Node.UntypedValue.ToString(); }
                return null;
            }
            set
            {
                int result = 0;
                try
                {
                    result = Convert.ToInt32(value);
                    Node.UntypedValue = (object)result;
                }
                catch (Exception) { }
            }
        }

        public bool Dirty { get { return Node.Dirty; } }

        public override string ToString() { return string.Format("{0}-{1}", ID, IsInput); }
    }

    public class CustomEdge : Edge<CustomVertex>
    {
        public string ID { get; private set; }

        public CustomEdge(CustomVertex source, CustomVertex target)
            : base(source, target)
        {
            ID = string.Format("{0} -> {1}", source.ID, target.ID);

        }

        public override string ToString()
        {
            return ID;
        }
    }

    public class CustomGraph : BidirectionalGraph<CustomVertex, CustomEdge>
    {
        IDictionary<string, CustomVertex> _vertices;

        public CustomGraph(ICalculationEngine calculationEngine)
            : base()
        {
            _vertices = new Dictionary<string, CustomVertex>();

            foreach (var node in calculationEngine.Nodes)
            {
                var vertex = new CustomVertex(node);
                _vertices.Add(node.ID, vertex);
                AddVertex(vertex);
            }

            foreach (var node in calculationEngine.Nodes)
            {
                foreach (var dependentNode in node.DependentNodes)
                {
                    var edge = new CustomEdge(_vertices[dependentNode.ID], _vertices[node.ID]);
                    AddEdge(edge);
                }
            }
        }

        public void Update(string id)
        {
            var vertex = _vertices[id];
            vertex.Node.AsyncCalculate();
        }

    }

    public class CustomGraphLayout : GraphLayout<CustomVertex, CustomEdge, CustomGraph>
    {
        public CustomGraphLayout()
            : base()
        {
        }
    }
}