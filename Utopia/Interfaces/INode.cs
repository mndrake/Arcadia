namespace Utopia
{
    using System;
    using Microsoft.FSharp.Control;

    public interface INode
    {
        event ChangedEventHandler Changed;
        ICalculationHandler Calculation {get;}
        INode[] DependentNodes { get; }
        bool Dirty { get; }
        FSharpAsync<object> Eval { get; }
        string ID { get; }
        bool IsInput { get; }
        bool Processing { get; }
        object UntypedValue { get; set; }
        void Update();
    }

    public interface INode<U> : INode
    {
        U Value { get; set; }
    }
}




/*
namespace Utopia

open System
open System.Collections.Generic
open System.ComponentModel


type ICalculationEngine = 
    abstract Calculation : ICalculationHandler with get
    abstract Nodes : List<INode> with get
*/