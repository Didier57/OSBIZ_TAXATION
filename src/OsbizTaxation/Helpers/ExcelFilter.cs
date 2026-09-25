using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OsbizTaxation.Helpers;

/// <summary>Ajoute un tri (clic sur l'entete) et un filtre facon Excel sur chaque colonne d'une DataGrid.</summary>
public static class ExcelFilter
{
    private sealed class ColumnFilter
    {
        public string Path = string.Empty;
        public HashSet<string>? Allowed;
        public Button? Button;
    }

    private sealed class CheckItem : INotifyPropertyChanged
    {
        private bool _isChecked = true;

        public string Value { get; init; } = string.Empty;
        public string Display { get; init; } = string.Empty;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                    return;
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static readonly Dictionary<DataGrid, List<ColumnFilter>> States = new();
    private static readonly Brush FilteredBrush = new SolidColorBrush(Color.FromRgb(0x0B, 0x66, 0xC3));

    public static void Attach(DataGrid grid)
    {
        var filters = new List<ColumnFilter>();
        States[grid] = filters;

        foreach (var column in grid.Columns)
        {
            var path = GetPath(column);
            if (string.IsNullOrEmpty(path))
                continue;

            var filter = new ColumnFilter { Path = path };
            filters.Add(filter);
            column.Header = BuildHeader(grid, column, filter);
        }

        grid.Loaded += (_, _) => InstallView(grid);
        grid.PreviewMouseRightButtonUp += (_, e) => OnGridRightClick(grid, e);
        InstallView(grid);
    }

    private static void OnGridRightClick(DataGrid grid, MouseButtonEventArgs e)
    {
        var header = FindHeader(e.OriginalSource as DependencyObject);
        if (header?.Column == null)
            return;
        if (!States.TryGetValue(grid, out var filters))
            return;

        var filter = filters.FirstOrDefault(f => f.Path == GetPath(header.Column));
        if (filter == null)
            return;

        e.Handled = true;
        grid.Dispatcher.BeginInvoke(new Action(() => ShowPopup(grid, filter, header)), DispatcherPriority.Input);
    }

    private static DataGridColumnHeader? FindHeader(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is DataGridColumnHeader header)
                return header;
            source = source is Visual
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }

    private static void InstallView(DataGrid grid)
    {
        if (grid.ItemsSource == null)
            return;
        CollectionViewSource.GetDefaultView(grid.ItemsSource).Filter = item => Passes(grid, item);
    }

    private static bool Passes(DataGrid grid, object item)
    {
        if (!States.TryGetValue(grid, out var filters))
            return true;

        foreach (var filter in filters)
        {
            if (filter.Allowed == null)
                continue;
            var value = GetValue(item, filter.Path) ?? string.Empty;
            if (!filter.Allowed.Contains(value))
                return false;
        }
        return true;
    }

    private static UIElement BuildHeader(DataGrid grid, DataGridColumn column, ColumnFilter filter)
    {
        var title = column.Header?.ToString() ?? string.Empty;

        var panel = new Grid();
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(label, 0);

        var icon = new Path
        {
            Data = Geometry.Parse("M0,0 L12,0 L7,6 L7,12 L5,12 L5,6 Z"),
            Fill = Brushes.Black,
            Width = 11,
            Height = 11,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var button = new Button
        {
            Content = icon,
            Padding = new Thickness(4, 0, 4, 0),
            Margin = new Thickness(4, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Trier / filtrer",
            Focusable = false
        };
        Grid.SetColumn(button, 1);
        button.Click += (_, e) =>
        {
            e.Handled = true;
            ShowPopup(grid, filter, button);
        };
        filter.Button = button;

        panel.Children.Add(label);
        panel.Children.Add(button);
        return panel;
    }

    private static void ShowPopup(DataGrid grid, ColumnFilter filter, UIElement target)
    {
        var items = GetDistinctValues(grid, filter.Path)
            .Select(v => new CheckItem
            {
                Value = v,
                Display = string.IsNullOrEmpty(v) ? "(vide)" : v,
                IsChecked = filter.Allowed == null || filter.Allowed.Contains(v)
            })
            .ToList();

        var search = new TextBox { Margin = new Thickness(6, 6, 6, 4), Padding = new Thickness(3) };
        var checkAll = new CheckBox
        {
            Content = "Tout sélectionner",
            Margin = new Thickness(6, 0, 6, 4),
            IsChecked = items.All(i => i.IsChecked)
        };
        var list = new ListBox
        {
            MaxHeight = 240,
            MinWidth = 220,
            Margin = new Thickness(6, 0, 6, 4)
        };
        list.ItemsSource = items;

        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(CheckBox));
        factory.SetBinding(ContentControl.ContentProperty, new Binding(nameof(CheckItem.Display)));
        factory.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(CheckItem.IsChecked))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        factory.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 1, 4, 1));
        template.VisualTree = factory;
        list.ItemTemplate = template;

        var view = CollectionViewSource.GetDefaultView(items);
        search.TextChanged += (_, _) =>
        {
            var text = search.Text.Trim();
            view.Filter = o => text.Length == 0 ||
                ((CheckItem)o).Display.Contains(text, StringComparison.CurrentCultureIgnoreCase);
        };

        checkAll.Click += (_, _) =>
        {
            var value = checkAll.IsChecked == true;
            foreach (var item in items)
                item.IsChecked = value;
        };

        var apply = new Button { Content = "Appliquer", Padding = new Thickness(12, 3, 12, 3), Margin = new Thickness(6, 0, 0, 0), IsDefault = true };
        var clear = new Button { Content = "Effacer", Padding = new Thickness(12, 3, 12, 3) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(6)
        };
        buttons.Children.Add(clear);
        buttons.Children.Add(apply);

        var panel = new StackPanel { Width = 250 };
        panel.Children.Add(search);
        panel.Children.Add(checkAll);
        panel.Children.Add(list);
        panel.Children.Add(buttons);

        var border = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = panel
        };

        var popup = new Popup
        {
            Child = border,
            Placement = PlacementMode.Bottom,
            PlacementTarget = target,
            StaysOpen = false,
            AllowsTransparency = true
        };

        clear.Click += (_, _) =>
        {
            filter.Allowed = null;
            popup.IsOpen = false;
            Refresh(grid);
            ApplyIndicator(filter.Button, false);
        };

        apply.Click += (_, _) =>
        {
            var selected = items.Where(i => i.IsChecked).Select(i => i.Value).ToHashSet(StringComparer.Ordinal);
            filter.Allowed = selected.Count == items.Count ? null : selected;
            popup.IsOpen = false;
            Refresh(grid);
            ApplyIndicator(filter.Button, filter.Allowed != null);
        };

        popup.IsOpen = true;
    }

    private static void ApplyIndicator(Button? button, bool active)
    {
        if (button == null)
            return;
        if (button.Content is Path icon)
            icon.Fill = active ? FilteredBrush : Brushes.Black;
        button.ToolTip = active ? "Filtre actif" : "Trier / filtrer";
    }

    /// <summary>Retire tous les filtres de colonnes actifs.</summary>
    public static void Clear(DataGrid grid)
    {
        if (!States.TryGetValue(grid, out var filters))
            return;
        foreach (var filter in filters)
        {
            filter.Allowed = null;
            ApplyIndicator(filter.Button, false);
        }
        Refresh(grid);
    }

    private static void Refresh(DataGrid grid)
    {
        if (grid.ItemsSource == null)
            return;
        CollectionViewSource.GetDefaultView(grid.ItemsSource).Refresh();
    }

    private static List<string> GetDistinctValues(DataGrid grid, string path)
    {
        var set = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        if (grid.ItemsSource is IEnumerable source)
        {
            foreach (var item in source)
                set.Add(GetValue(item, path) ?? string.Empty);
        }
        return set.ToList();
    }

    private static string? GetValue(object? item, string path)
    {
        if (item == null)
            return null;
        var descriptor = TypeDescriptor.GetProperties(item)[path];
        return descriptor?.GetValue(item)?.ToString();
    }

    private static string GetPath(DataGridColumn column)
    {
        if (column is DataGridBoundColumn bound && bound.Binding is Binding binding && binding.Path?.Path is { Length: > 0 } path)
            return path;
        return column.SortMemberPath ?? string.Empty;
    }
}
