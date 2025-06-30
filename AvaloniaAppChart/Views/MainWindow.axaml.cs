using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaAppChart.Services;
using AvaloniaAppChart.ViewModels;
using System;
using System.Threading.Tasks;

namespace AvaloniaAppChart.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var clipboard = Clipboard ?? throw new InvalidOperationException("Clipboard недоступен");
            var vm = new MainWindowViewModel(clipboard);
            DataContext = vm;
        }

        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is MainWindowViewModel vm && vm.HasUnsavedChanges)
            {
                e.Cancel = true;

                var confirmed = await PopupService.ShowConfirmPopupAsync("Подтверждение выхода",
                    "Вы действительно хотите выйти? Несохранённые данные будут потеряны.");

                if (confirmed)
                {
                    vm.HasUnsavedChanges = false;
                    Close();
                }
            }
        }
    }
}