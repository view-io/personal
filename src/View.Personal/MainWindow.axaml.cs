namespace View.Personal
{
    using Avalonia;
    using Avalonia.Controls;
    using System.Linq;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // To access internals initialized in App.axaml.cs:
            //   var app = (App)Application.Current;
            //   var value = app._LiteGraph;
        }

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                string? selectedContent = selectedItem.Content?.ToString();
                WorkspaceText.Text = selectedContent;
                
                // Show/hide the dashboard panel based on selection
                if (selectedContent == "Dashboard")
                {
                    DashboardPanel.IsVisible = true;
                    WorkspaceText.IsVisible = false;
                }
                else
                {
                    DashboardPanel.IsVisible = false;
                    WorkspaceText.IsVisible = true;
                }
            }
        }

        private void NavigateToSettings_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Settings") is ListBoxItem settingsItem)
            {
                NavList.SelectedItem = settingsItem;
            }
        }

        private void NavigateToMyFiles_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "My Files") is ListBoxItem myFilesItem)
            {
                NavList.SelectedItem = myFilesItem;
            }
        }

        private void NavigateToChat_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Chat") is ListBoxItem chatItem)
            {
                NavList.SelectedItem = chatItem;
            }
        }
    }
}