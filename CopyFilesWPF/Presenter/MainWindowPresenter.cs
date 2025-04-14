using CopyFilesWPF.Model;
using CopyFilesWPF.View;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CopyFilesWPF.Presenter
{
    public class MainWindowPresenter : IMainWindowPresenter
    {
        private readonly IMainWindowView _mainWindowView;
        private readonly MainWindowModel _mainWindowModel;
        public MainWindowPresenter(IMainWindowView mainWindowView)
        {
            _mainWindowView = mainWindowView;
            _mainWindowModel = new MainWindowModel();
        }
        public void ChooseFileFromButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathFrom = path;
        }
        public void ChooseFileToButtonClick(string path)
        {
            _mainWindowModel.FilePath.PathTo = path;
        }
        public void CopyButtonClick()
        {
            var pathFrom = _mainWindowView.MainWindowView.FromTextBox.Text;
            var pathTo = _mainWindowView.MainWindowView.ToTextBox.Text;

            _mainWindowModel.FilePath.PathFrom = pathFrom;
            _mainWindowModel.FilePath.PathTo = pathTo;

            ClearInputFields();

            _mainWindowView.MainWindowView.Height += 60;

            var fileName = Path.GetFileName(pathFrom);
            var panel = CreateFileCopyPanel(fileName, out var progressBar);

            var pauseButton = CreateControlButton("Pause", panel, PauseCancelClick, 1);
            var cancelButton = CreateControlButton("Cancel", panel, PauseCancelClick, 2);

            panel.Children.Add(pauseButton);
            panel.Children.Add(cancelButton);

            DockPanel.SetDock(panel, Dock.Top);
            _mainWindowView.MainWindowView.MainPanel.Children.Add(panel);

            _mainWindowModel.CopyFile(ProgressChanged, ModelOnComplete, panel);
        }
        private void ClearInputFields()
        {
            _mainWindowView.MainWindowView.FromTextBox.Text = "";
            _mainWindowView.MainWindowView.ToTextBox.Text = "";
        }
        private Grid CreateFileCopyPanel(string fileName, out ProgressBar progressBar)
        {
            var panel = new Grid { Height = 60 };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            panel.RowDefinitions.Add(new RowDefinition());
            var nameBlock = new TextBlock
            {
                Text = fileName,
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetRow(nameBlock, 0);
            Grid.SetColumn(nameBlock, 0);
            panel.Children.Add(nameBlock);

            progressBar = new ProgressBar
            {
                Margin = new Thickness(10)
            };
            Grid.SetRow(progressBar, 1);
            panel.Children.Add(progressBar);
            return panel;
        }
        private Button CreateControlButton(string content, Grid panel, RoutedEventHandler handler, int column)
        {
            var button = new Button
            {
                Content = content,
                Margin = new Thickness(5),
                Tag = panel
            };
            button.Click += handler;
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, column);
            return button;
        }
        private void PauseCancelClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Grid panel && panel.Tag is FileCopier copier)
            {
                button.IsEnabled = false;
                string action = button.Content.ToString();
                switch (action)
                {
                    case "Cancel":
                        copier.CancelFlag = true;
                        break;
                    case "Pause":
                        copier.PauseFlag.Reset();
                        button.Content = "Resume";
                        break;
                    case "Resume":
                        copier.PauseFlag.Set();
                        button.Content = "Pause";
                        break;
                }
                button.IsEnabled = true;
            }
        }
        private void ModelOnComplete(Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    _mainWindowView.MainWindowView.Height -= 60;
                    _mainWindowView.MainWindowView.MainPanel.Children.Remove(panel);
                    _mainWindowView.MainWindowView.CopyButton.IsEnabled = true;
                });
        }
        private void ProgressChanged(double percentage, ref bool cancelFlag, Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate ()
                {
                    foreach (var el in panel.Children)
                    {
                        switch (el)
                        {
                            case ProgressBar bar:
                                bar.Value = percentage;
                                break;

                            case Button button when button.Content.ToString() == "Resume" && !button.IsEnabled:
                                button.Content = "Pause";
                                button.IsEnabled = true;
                                break;

                            case Button button when button.Content.ToString() == "Pause" && !button.IsEnabled:
                                button.Content = "Resume";
                                button.IsEnabled = true;
                                break;
                        }
                    }
                });
        }
    }
}
