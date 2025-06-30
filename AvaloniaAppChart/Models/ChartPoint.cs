using ReactiveUI;

namespace AvaloniaAppChart.Models
{
    public class ChartPoint : ReactiveObject
    {
        private double? _x;
        public double? X
        {
            get => _x;
            set
            {
                this.RaiseAndSetIfChanged(ref _x, value);
                this.RaisePropertyChanged(nameof(IsFilled));
            }
        }

        private double? _y;
        public double? Y
        {
            get => _y;
            set
            {
                this.RaiseAndSetIfChanged(ref _y, value);
                this.RaisePropertyChanged(nameof(IsFilled));
            }
        }

        public bool IsFilled => X.HasValue && Y.HasValue;
    }
}