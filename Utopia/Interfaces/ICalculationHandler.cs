namespace Utopia
{
    using System;

    public delegate void ChangedEventHandler(object sender, EventArgs e);

    public interface ICalculationHandler
    {
        bool Automatic { get; set; }
        void Cancel();
        event ChangedEventHandler Changed;
    }
}