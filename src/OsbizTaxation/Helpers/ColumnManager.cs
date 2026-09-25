using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace OsbizTaxation.Helpers;

/// <summary>Popup de gestion des colonnes d'une DataGrid : reordonnancement par glisser-deposer et masquage.</summary>
public static class ColumnManager
{
    private static readonly Dictionary<DataGrid, Popup> Open = new();

    private sealed class ColumnItem : INotifyPropertyChanged
    {
        private bool _isVisible = true;

        public DataGridColumn Column { get; init; } = null!;
        public string Title { get; init; } = string.Empty;
        public Action? Changed { get; init; }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                Column.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                Changed?.Invoke();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public static void Show(DataGrid grid, UIElement target, Action? onChanged = null)
    {
        if (Open.TryGetValue(grid, out var existing))
        {
            existing.IsOpen = false;
            Open.Remove(grid);
            return;
        }

        var items = new ObservableCollection<ColumnItem>();
        foreach (var column in grid.Columns.OrderBy(c => c.DisplayIndex))
        {
            items.Add(new ColumnItem
            {
                Column = column,
                Title = ExcelFilter.GetColumnTitle(column),
                IsVisible = column.Visibility == Visibility.Visible,
                Changed = onChanged
            });
        }

        var list = new ListBox
        {
            MaxHeight = 320,
            MinWidth = 250,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(4)
        };
        var template = new DataTemplate();
        var row = new FrameworkElementFactory(typeof(Grid));
        var colGrip = new FrameworkElementFactory(typeof(ColumnDefinition));
        colGrip.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
        var colCheck = new FrameworkElementFactory(typeof(ColumnDefinition));
        colCheck.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
        var colText = new FrameworkElementFactory(typeof(ColumnDefinition));
        colText.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        row.AppendChild(colGrip);
        row.AppendChild(colCheck);
        row.AppendChild(colText);

        var check = new FrameworkElementFactory(typeof(CheckBox));
        check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(ColumnItem.IsVisible))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        check.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 2, 8, 2));
        check.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        check.SetValue(Grid.ColumnProperty, 1);

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(nameof(ColumnItem.Title)));
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        text.SetValue(Grid.ColumnProperty, 2);

        var grip = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
        grip.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M0,1 H12 M0,6 H12"));
        grip.SetValue(System.Windows.Shapes.Path.StrokeProperty, new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)));
        grip.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 2.0);
        grip.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        grip.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 0, 8, 0));
        grip.SetValue(FrameworkElement.CursorProperty, Cursors.SizeAll);
        grip.SetValue(FrameworkElement.ToolTipProperty, "Glisser pour déplacer");
        grip.SetValue(Grid.ColumnProperty, 0);

        row.AppendChild(grip);
        row.AppendChild(check);
        row.AppendChild(text);
        template.VisualTree = row;
        list.ItemTemplate = template;
        list.ItemsSource = items;

        var hint = new TextBlock
        {
            Text = "Glissez pour réordonner, décochez pour masquer.",
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 8, 8, 2)
        };

        var close = new Button
        {
            Content = "Fermer",
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var panel = new StackPanel();
        panel.Children.Add(hint);
        panel.Children.Add(list);
        panel.Children.Add(close);

        var border = new Border
        {
            BorderThickness = new Thickness(1),
            Child = panel
        };
        border.SetResourceReference(Border.BackgroundProperty, "Brush.Menu.Background");
        border.SetResourceReference(Border.BorderBrushProperty, "Brush.Control.Border");
        border.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Brush.Menu.Foreground");

        var popup = new Popup
        {
            Child = border,
            Placement = PlacementMode.Bottom,
            PlacementTarget = target,
            StaysOpen = true,
            AllowsTransparency = true
        };

        var start = new Point();
        ColumnItem? dragged = null;

        list.PreviewMouseLeftButtonDown += (_, e) =>
        {
            start = e.GetPosition(list);
            dragged = ItemAt(list, e.OriginalSource as DependencyObject);
        };

        list.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || dragged == null)
                return;
            var pos = e.GetPosition(list);
            if (Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance &&
                Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance)
                return;
            DragDrop.DoDragDrop(list, dragged, DragDropEffects.Move);
        };

        list.AllowDrop = true;
        list.DragOver += (_, e) => e.Effects = DragDropEffects.Move;
        list.Drop += (_, e) =>
        {
            var dropTarget = ItemAt(list, e.OriginalSource as DependencyObject);
            if (dragged == null || dropTarget == null || ReferenceEquals(dropTarget, dragged))
                return;
            var from = items.IndexOf(dragged);
            var to = items.IndexOf(dropTarget);
            if (from < 0 || to < 0)
                return;
            items.Move(from, to);
            ApplyOrder(items);
            onChanged?.Invoke();
            e.Handled = true;
        };

        close.Click += (_, _) =>
        {
            popup.IsOpen = false;
            Open.Remove(grid);
        };

        Open[grid] = popup;
        popup.IsOpen = true;
    }

    /// <summary>Restaure l'ordre et les colonnes masquees memorises pour une table.</summary>
    public static void ApplyLayout(DataGrid grid, List<string>? order, List<string>? hidden)
    {
        if (hidden is { Count: > 0 })
        {
            foreach (var column in grid.Columns)
                column.Visibility = hidden.Contains(GridColumnWidths.Key(column))
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        if (order is { Count: > 0 })
        {
            var desired = order
                .Select(key => grid.Columns.FirstOrDefault(c => GridColumnWidths.Key(c) == key))
                .Where(c => c != null)
                .Cast<DataGridColumn>()
                .ToList();

            foreach (var column in grid.Columns.OrderBy(c => c.DisplayIndex))
            {
                if (!desired.Contains(column))
                    desired.Add(column);
            }

            for (var i = 0; i < desired.Count; i++)
                desired[i].DisplayIndex = i;
        }
    }

    /// <summary>Memorise l'ordre et les colonnes masquees d'une table.</summary>
    public static void Capture(DataGrid grid, List<string> order, List<string> hidden)
    {
        order.Clear();
        foreach (var column in grid.Columns.OrderBy(c => c.DisplayIndex))
            order.Add(GridColumnWidths.Key(column));

        hidden.Clear();
        foreach (var column in grid.Columns)
        {
            if (column.Visibility != Visibility.Visible)
                hidden.Add(GridColumnWidths.Key(column));
        }
    }

    private static void ApplyOrder(IList<ColumnItem> items)
    {
        for (var i = 0; i < items.Count; i++)
            items[i].Column.DisplayIndex = i;
    }

    private static ColumnItem? ItemAt(ListBox list, DependencyObject? source)
    {
        while (source != null)
        {
            if (source is ListBoxItem item)
                return item.DataContext as ColumnItem;
            source = source is Visual
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }
}
