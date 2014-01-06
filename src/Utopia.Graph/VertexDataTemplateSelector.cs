namespace Utopia.Graph
{
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Controls;
    using Utopia.Graph;

    public class VertexDataTemplateSelector : DataTemplateSelector
    {
        public VertexDataTemplateSelector() : base()
        {
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element != null && item != null && item is INodeVertex)
            {
                var vertexItem = (INodeVertex)item;
                if (vertexItem.Node.IsInput)
                {
                    return element.FindResource("InputVertexTemplate") as DataTemplate;
                }
                else
                {
                    return element.FindResource("OutputVertexTemplate") as DataTemplate;
                }
            }

            return null;
        }
    }
}