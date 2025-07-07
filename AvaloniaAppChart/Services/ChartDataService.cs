using Avalonia.Controls;
using AvaloniaAppChart.Models;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace AvaloniaAppChart.Services
{
    public class ChartDataService
    {
        private Window GetMainWindow()
        {
            var app = App.Current ?? throw new InvalidOperationException("App.Current is null");
            if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                return desktop.MainWindow;

            throw new InvalidOperationException("MainWindow is null or application lifetime is not desktop");
        }

        public async Task SaveToFileAsync(IList<СoordinateChartPoint> chartPoints)
        {
            var dialog = new SaveFileDialog
            {
                Filters = { new FileDialogFilter { Name = "CSV Files", Extensions = { "csv" } } },
                DefaultExtension = "csv"
            };

            var filePath = await dialog.ShowAsync(GetMainWindow());
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var lines = chartPoints
                .Where(p => p.IsFilled)
                .Select(p => $"{p.X},{p.Y}");

            await File.WriteAllLinesAsync(filePath, lines);
        }

        public async Task LoadFromFileAsync(ObservableCollection<СoordinateChartPoint> chartPoints)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = { new FileDialogFilter { Name = "CSV Files", Extensions = { "csv" } } }
            };

            var filePaths = await dialog.ShowAsync(GetMainWindow());
            if (filePaths is null || filePaths.Length == 0) return;

            var filePath = filePaths[0];
            var lines = await File.ReadAllLinesAsync(filePath);

            chartPoints.Clear();

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], out var x) &&
                    double.TryParse(parts[1], out var y))
                {
                    var point = new СoordinateChartPoint { X = x, Y = y };
                    chartPoints.Add(point);
                    ((INotifyPropertyChanged)point).PropertyChanged += (_, _) => { };
                }
            }

            var empty = new СoordinateChartPoint();
            chartPoints.Add(empty);
            ((INotifyPropertyChanged)empty).PropertyChanged += (_, _) => { };
        }
    }
}
