namespace Utopia
{
    using System;
    using Microsoft.FSharp.Core;

    public interface ICalculationEngine
    {
        event ChangedEventHandler Changed;
        bool Automatic { get; set; }
        void Cancel();
    }
}