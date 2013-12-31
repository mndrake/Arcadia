namespace Utopia
{
    using Microsoft.FSharp.Control;
    using Microsoft.FSharp.Core;
    using Microsoft.FSharp.Core.CompilerServices;
    using System;

    public abstract class NodeBase<U> : INode<U>
    {
        public U Value
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event ChangedEventHandler Changed;

        public ICalculationHandler Calculation
        {
            get { throw new NotImplementedException(); }
        }

        public INode[] DependentNodes
        {
            get { throw new NotImplementedException(); }
        }

        public bool Dirty
        {
            get { throw new NotImplementedException(); }
        }

        public FSharpAsync<object> Eval
        {
            get { throw new NotImplementedException(); }
        }

        public string ID
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsInput
        {
            get { throw new NotImplementedException(); }
        }

        public bool Processing
        {
            get { throw new NotImplementedException(); }
        }

        public object UntypedValue
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}