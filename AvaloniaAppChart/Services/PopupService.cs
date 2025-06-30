using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System.Threading.Tasks;

namespace AvaloniaAppChart.Services
{
    public static class PopupService
    {
        public static async Task ShowPopupAsync(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                Content = new TextBlock { Text = message, Margin = new Thickness(20) }
            };

            Window? owner = null;

            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                owner = desktop.MainWindow;
            }

            await dialog.ShowDialog(owner);
        }

        public static async Task<bool> ShowConfirmPopupAsync(string title, string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            var yesButton = new Button
            {
                Content = "Да",
                Width = 100,
                Margin = new Thickness(10)
            };

            var noButton = new Button
            {
                Content = "Нет",
                Width = 100,
                Margin = new Thickness(10)
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children = { yesButton, noButton }
            };

            var dialogContent = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    buttons
                }
            };

            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                Content = dialogContent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            yesButton.Click += (_, _) =>
            {
                tcs.SetResult(true);
                dialog.Close();
            };

            noButton.Click += (_, _) =>
            {
                tcs.SetResult(false);
                dialog.Close();
            };

            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            {
                await dialog.ShowDialog(app.MainWindow);
            }

            return await tcs.Task;
        }
    }
}
