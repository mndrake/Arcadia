namespace CSharpApp.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using CSharpApp.ViewModels;

    public class SubmitTextBox : TextBox
    {
        public SubmitTextBox()
            : base()
        {
            PreviewKeyDown += new KeyEventHandler(SubmitTextBox_PreviewKeyDown);
        }

        void SubmitTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindingExpression be = GetBindingExpression(TextBox.TextProperty);
                if (be != null)
                {
                    be.UpdateSource();
                }
            }
        }
    }

    public class VertexDataTemplateSelector : DataTemplateSelector
    {
        public VertexDataTemplateSelector() { }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            if (element != null && item != null && item is INodeVertex)
            {
                INodeVertex vertexItem = (INodeVertex)item;
                if (vertexItem.Node.IsInput)
                {
                    return element.FindResource("InputVertexTemplate") as DataTemplate;
                }
                else
                {
                    return element.FindResource("OutputVertexTemplate") as DataTemplate;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
