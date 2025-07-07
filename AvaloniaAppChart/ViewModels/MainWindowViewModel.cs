using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Input.Platform;
using Avalonia.ReactiveUI;
using AvaloniaAppChart.Services;
using AvaloniaAppChart.Models;
using SkiaSharp;
using Avalonia.Input;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using System.Collections.Generic;


namespace AvaloniaAppChart.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private СoordinateChartPoint? _draggedPoint;
        private List<СoordinateChartPoint> _renderedPoints = [];
        private bool _isDragging = false;
        private bool _suspendSeriesRefresh = false;
        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
        }

        private bool _showInverse;
        public bool ShowInverse
        {
            get => _showInverse;
            set 
            { 
                this.RaiseAndSetIfChanged(ref _showInverse, value); 
                this.RaisePropertyChanged(nameof(IsInverseVisible)); 
            }
        }

        private readonly IClipboard _clipboard;
        private readonly ChartDataService _dataService;
        private readonly ClipboardChartService _clipboardService;

        public ObservableCollection<СoordinateChartPoint> ChartPoints { get; } = [];
        public ISeries[]? Series { get; private set; } = [];
        public ISeries[]? InverseSeries { get; private set; } = [];
        public Axis[] XAxes { get; } =
        [
            new Axis
            {
                MinStep = 1,
                Name = "Ось X",
                Labeler = value => value.ToString("F2"),
                TicksPaint = new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                ShowSeparatorLines = true,
                DrawTicksPath = true
            }
        ];

        public Axis[] YAxes { get; } =
        [
            new Axis
            {
                MinStep = 1,
                Name = "Ось Y",
                Labeler = value => value.ToString("F2"),
                TicksPaint = new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                ShowSeparatorLines = true,
                DrawTicksPath = true
            }
        ];
        public ReactiveCommand<Unit, Unit> CopyCommand { get; }
        public ReactiveCommand<Unit, Unit> PasteCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveToFileCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadFromFileCommand { get; }
        public ReactiveCommand<СoordinateChartPoint, Unit> DeletePointCommand { get; }
        public bool IsInverseVisible => ShowInverse && InverseSeries is not null;

        public ReactiveCommand<Unit, Unit> ToggleInverseCommand { get; }

        public MainWindowViewModel(IClipboard clipboard)
        {
            _clipboard = clipboard;
            _dataService = new ChartDataService();
            _clipboardService = new ClipboardChartService(_clipboard);

            ChartPoints.Add(new СoordinateChartPoint());

            ((INotifyPropertyChanged)ChartPoints[0]).PropertyChanged += OnRowChanged;

            ChartPoints.CollectionChanged += (_, __) => RefreshSeries();

            CopyCommand = ReactiveCommand.CreateFromTask(CopyToClipboardAsync, outputScheduler: AvaloniaScheduler.Instance);
            PasteCommand = ReactiveCommand.CreateFromTask(PasteFromClipboardAsync, outputScheduler: AvaloniaScheduler.Instance);
            ClearCommand = ReactiveCommand.Create(ClearData, outputScheduler: AvaloniaScheduler.Instance);
            DeletePointCommand = ReactiveCommand.Create<СoordinateChartPoint>(DeletePoint, outputScheduler: AvaloniaScheduler.Instance);
            SaveToFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _dataService.SaveToFileAsync(ChartPoints);
                HasUnsavedChanges = false;
                RefreshSeries();
                RefreshInverseSeries();
            }, outputScheduler: AvaloniaScheduler.Instance);

            LoadFromFileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _dataService.LoadFromFileAsync(ChartPoints);
                foreach (var point in ChartPoints)
                    ((INotifyPropertyChanged)point).PropertyChanged += OnRowChanged;
                RefreshSeries();
                RefreshInverseSeries();
                HasUnsavedChanges = false;
            }, outputScheduler: AvaloniaScheduler.Instance);
            LoadFromFileCommand.ThrownExceptions.Subscribe(async ex =>
            {
                await PopupService.ShowConfirmPopupAsync("Ошибка при загрузке", ex.Message);
            });
            ChartPoints.CollectionChanged += (_, __) =>
            {
                RefreshSeries();
                RefreshInverseSeries();
                HasUnsavedChanges = true;
            };

            ToggleInverseCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (!CanShowInverse())
                {
                    await PopupService.ShowPopupAsync(
                        "Обратная функция недоступна",
                        "Функция не может быть обращена, так как значения Y содержат повторы."
                    );
                    return;
                }

                ShowInverse = !ShowInverse;
                RefreshSeries();
                RefreshInverseSeries();
            }, outputScheduler: AvaloniaScheduler.Instance);
        }

        private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suspendSeriesRefresh)
                return;

            if (sender is СoordinateChartPoint point && point.IsFilled && ChartPoints.Last() == point)
            {
                var newPoint = new СoordinateChartPoint();
                ChartPoints.Add(newPoint);
                ((INotifyPropertyChanged)newPoint).PropertyChanged += OnRowChanged;
            }

            RefreshSeries();
            RefreshInverseSeries();
            HasUnsavedChanges = true;
        }

        private void RefreshSeries()
        {
            _renderedPoints = ChartPoints
                .Where(p => p.IsFilled)
                .ToList();

            var series = new LineSeries<СoordinateChartPoint>
            {
                Values = _renderedPoints,
                LineSmoothness = 0,
                GeometrySize = 10,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue),
                Mapping = (model, index) =>
                {
                    var x = model.X ?? 0;
                    var y = model.Y ?? 0;
                    return new Coordinate(x, y);
                }
            };

            series.ChartPointPointerDown += OnPointerDown;

            Series = [series];
            this.RaisePropertyChanged(nameof(Series));
        }

        public async Task CopyToClipboardAsync()
        {
            await _clipboardService.CopyAsync(ChartPoints);
        }

        public async Task PasteFromClipboardAsync()
        {
            var unfilled = ChartPoints.Where(p => !p.IsFilled).ToList();
            foreach (var item in unfilled)
                ChartPoints.Remove(item);

            var (newPoints, skipped) = await _clipboardService.PasteAsync(ChartPoints);

            foreach (var point in newPoints)
            {
                ChartPoints.Add(point);
                ((INotifyPropertyChanged)point).PropertyChanged += OnRowChanged;
            }

            var emptyPoint = new СoordinateChartPoint();
            ChartPoints.Add(emptyPoint);
            ((INotifyPropertyChanged)emptyPoint).PropertyChanged += OnRowChanged;

            RefreshSeries();
            RefreshInverseSeries();

            if (skipped.Count > 0)
            {
                var message = $"Пропущено {skipped.Count} строк (дубликаты):\n" +
                              string.Join('\n', skipped.Take(5)) +
                              (skipped.Count > 5 ? "\n…" : "");

                await PopupService.ShowPopupAsync("Дубликаты", message);
            }
        }

        private void ClearData()
        {
            ChartPoints.Clear();
            var newPoint = new СoordinateChartPoint();
            ChartPoints.Add(newPoint);
            ((INotifyPropertyChanged)newPoint).PropertyChanged += OnRowChanged;
            CheckIfOnlyEmptyRowLeft();
        }

        private void DeletePoint(СoordinateChartPoint point)
        {
            ChartPoints.Remove(point);
            CheckIfOnlyEmptyRowLeft();
        }

        private void CheckIfOnlyEmptyRowLeft()
        {
            if (ChartPoints.Count == 1 && !ChartPoints[0].IsFilled)
            {
                HasUnsavedChanges = false;
            }
        }

        private void RefreshInverseSeries()
        {
            this.RaisePropertyChanged(nameof(InverseSeries));
            this.RaisePropertyChanged(nameof(IsInverseVisible));

            var filledPoints = ChartPoints
                .Where(p => p.IsFilled)
                .ToList();

            var allY = filledPoints.Select(p => p.Y!.Value);
            if (allY.Distinct().Count() != allY.Count())
            {
                InverseSeries = null;
                this.RaisePropertyChanged(nameof(InverseSeries));
                this.RaisePropertyChanged(nameof(IsInverseVisible));
                return;
            }

            var inversePoints = filledPoints
                .OrderBy(p => p.Y!.Value)
                .Select(p => new ObservablePoint(p.Y!.Value, p.X!.Value))
                .ToArray();

            InverseSeries =
            [
                new LineSeries<ObservablePoint>
                {
                    Values = inversePoints,
                    LineSmoothness = 0,
                    GeometrySize = 6,
                    Stroke = new SolidColorPaint(SKColors.Red),
                    Fill = null
                }
            ];

            this.RaisePropertyChanged(nameof(InverseSeries));
            this.RaisePropertyChanged(nameof(IsInverseVisible));
        }

        private bool CanShowInverse()
        {
            var filledPoints = ChartPoints.Where(p => p.IsFilled).ToList();
            var allY = filledPoints.Select(p => p.Y!.Value);
            return allY.Distinct().Count() == allY.Count();
        }

        private void OnChartPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedPoint == null || sender is not CartesianChart chart) return;
            if (!_isDragging) return;

            var pixel = e.GetPosition(chart);
            var dataPoint = chart.ScalePixelsToData(new LvcPointD(pixel.X, pixel.Y));

            _suspendSeriesRefresh = true;

            _draggedPoint.X = dataPoint.X;
            _draggedPoint.Y = dataPoint.Y;

            _suspendSeriesRefresh = false;
        }

        private void OnChartPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is not CartesianChart chart)
                return;

            chart.PointerMoved -= OnChartPointerMoved;
            chart.PointerReleased -= OnChartPointerReleased;

            _draggedPoint = null;
            _isDragging = false;

            RefreshSeries();
            RefreshInverseSeries();
        }

        private void OnPointerDown(IChartView chart, ChartPoint<СoordinateChartPoint, CircleGeometry, LabelGeometry> point)
        {
            _isDragging = true;

            if (point.Model == null || chart is not CartesianChart c) return;

            _draggedPoint = point.Model;

            c.PointerMoved += OnChartPointerMoved;
            c.PointerReleased += OnChartPointerReleased;
        }
    }
}
