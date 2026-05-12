using System.Windows;

namespace PointDynamic
{
    public enum ToolAction { None, IsolateRegion, ClearFilter }

    public partial class PointDynamicWindow : Window
    {
        public ToolAction SelectedAction { get; private set; } = ToolAction.None;

        public PointDynamicWindow()
        {
            InitializeComponent();
        }

        private void BtnIsolate_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = ToolAction.IsolateRegion;
            DialogResult = true;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = ToolAction.ClearFilter;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
