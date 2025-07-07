using Avalonia.Input.Platform;
using AvaloniaAppChart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvaloniaAppChart.Services
{
    public class ClipboardChartService
    {
        private readonly IClipboard _clipboard;

        public ClipboardChartService(IClipboard clipboard)
        {
            _clipboard = clipboard;
        }

        private static readonly string[] _separator = ["\r\n", "\n"];

        public async Task CopyAsync(IEnumerable<СoordinateChartPoint> chartPoints)
        {
            var lines = chartPoints
                .Where(p => p.IsFilled)
                .Select(p => $"{p.X}\t{p.Y}");

            var text = string.Join(Environment.NewLine, lines);
            await _clipboard.SetTextAsync(text);
        }

        public async Task<(List<СoordinateChartPoint> NewPoints, List<string> SkippedLines)> PasteAsync(IEnumerable<СoordinateChartPoint> existingPoints)
        {
            var text = await _clipboard.GetTextAsync();
            var result = new List<СoordinateChartPoint>();
            var skipped = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return (result, skipped);

            var lines = text.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

            var existingSet = existingPoints
                .Where(p => p.IsFilled)
                .Select(p => (p.X!.Value, p.Y!.Value))
                .ToHashSet();

            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], out var x) &&
                    double.TryParse(parts[1], out var y))
                {
                    if (!existingSet.Contains((x, y)))
                    {
                        result.Add(new СoordinateChartPoint { X = x, Y = y });
                        existingSet.Add((x, y));
                    }
                    else
                    {
                        skipped.Add(line);
                    }
                }
            }

            return (result, skipped);
        }
    }
}
