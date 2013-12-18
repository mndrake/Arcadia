using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SampleApp.ViewModel;

namespace SampleApp.ViewCS
{
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public partial class GraphView : UserControl
    {
        public GraphView()
        {
            InitializeComponent();
        }
    }

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
            if (element != null && item != null && item is CustomVertex)
            {
                CustomVertex vertexItem = (CustomVertex)item;
                if (vertexItem.IsInput)
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
