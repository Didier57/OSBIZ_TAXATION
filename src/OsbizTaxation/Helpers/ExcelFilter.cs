using System.Collections;
using System.ComponentModel;
using System.Globalization;
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
    private enum FilterMode
    {
        Equals,
        StartsWith,
        Contains,
        EndsWith,
        IsEmpty,
        Different,
        NotStartsWith,
        NotContains,
        NotEndsWith,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Between
    }

    // Label == null insère un séparateur.
    private static readonly (string? Label, FilterMode Mode)[] Operators =
    {
        ("Est égal à", FilterMode.Equals),
        ("Commence par", FilterMode.StartsWith),
        ("Contient", FilterMode.Contains),
        ("Se termine par", FilterMode.EndsWith),
        ("Est vide", FilterMode.IsEmpty),
        (null, FilterMode.Equals),
        ("Est différent de", FilterMode.Different),
        ("Ne commence pas par", FilterMode.NotStartsWith),
        ("Ne contient pas", FilterMode.NotContains),
        ("Ne se termine pas par", FilterMode.NotEndsWith),
        (null, FilterMode.Equals),
        ("Supérieur à", FilterMode.Greater),
        ("Supérieur ou égal à", FilterMode.GreaterOrEqual),
        ("Inférieur à", FilterMode.Less),
        ("Inférieur ou égal à", FilterMode.LessOrEqual),
        ("Compris entre 2 bornes...", FilterMode.Between)
    };

    private sealed class ColumnFilter
    {
        public string Path = string.Empty;
        public HashSet<string>? Allowed;
        public Func<object, bool>? Predicate;
        public Button? Button;
    }

    private sealed class RememberedFilter
    {
        public FilterMode? Mode;
        public string[] Inputs = Array.Empty<string>();
        public HashSet<string>? Allowed;
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
    private static readonly Dictionary<string, RememberedFilter> Remembered = new();
    private static readonly Brush FilteredBrush = new SolidColorBrush(Color.FromRgb(0x0B, 0x66, 0xC3));
    private static readonly Brush GreyBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
    private static readonly Brush HintBrush = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
    private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0xF0, 0xFB));

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

            if (Remembered.TryGetValue(RememberKey(grid, filter), out var remembered))
            {
                if (remembered.Mode is { } mode)
                    filter.Predicate = BuildPredicate(filter.Path, mode, Input(remembered, 0), Input(remembered, 1));
                else
                    filter.Allowed = remembered.Allowed;
                if (filter.Predicate != null || filter.Allowed != null)
                    ApplyIndicator(filter.Button, true);
            }
        }

        grid.Loaded += (_, _) => InstallView(grid);
        grid.PreviewMouseRightButtonUp += (_, e) => OnGridRightClick(grid, e);
        InstallView(grid);
    }

    private static string Input(RememberedFilter filter, int index) =>
        index < filter.Inputs.Length ? filter.Inputs[index] : string.Empty;

    private static string RememberKey(DataGrid grid, ColumnFilter filter) =>
        (grid.Name ?? string.Empty) + "|" + filter.Path;

    private static void StoreRemember(DataGrid grid, ColumnFilter filter, FilterMode? mode, string[] inputs)
    {
        Remembered[RememberKey(grid, filter)] = new RememberedFilter
        {
            Mode = mode,
            Inputs = inputs,
            Allowed = filter.Allowed
        };
    }

    private static void RemoveRemember(DataGrid grid, ColumnFilter filter) =>
        Remembered.Remove(RememberKey(grid, filter));

    private static bool IsRemembered(DataGrid grid, ColumnFilter filter) =>
        Remembered.ContainsKey(RememberKey(grid, filter));

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
            if (filter.Predicate != null && !filter.Predicate(item))
                return false;
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
        panel.Tag = title;
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

        // Champ de recherche avec loupe
        var searchBox = new TextBox
        {
            Padding = new Thickness(0, 3, 3, 3),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var searchGrid = new Grid();
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var magnifier = new Path
        {
            Data = Geometry.Parse("M1,5 A4,4 0 1 0 9,5 A4,4 0 1 0 1,5 M8,8 L11,11"),
            Stroke = GreyBrush,
            StrokeThickness = 1.3,
            Fill = Brushes.Transparent,
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(magnifier, 0);
        Grid.SetColumn(searchBox, 1);
        searchGrid.Children.Add(magnifier);
        searchGrid.Children.Add(searchBox);
        var searchBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xBB)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(6, 6, 6, 6),
            Child = searchGrid
        };

        var list = new ListBox
        {
            MaxHeight = 240,
            MinWidth = 220,
            Margin = new Thickness(6, 0, 6, 4),
            BorderThickness = new Thickness(0)
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
        searchBox.TextChanged += (_, _) =>
        {
            var text = searchBox.Text.Trim();
            view.Filter = o => text.Length == 0 ||
                ((CheckItem)o).Display.Contains(text, StringComparison.CurrentCultureIgnoreCase);
        };

        var checkAll = new CheckBox
        {
            Content = "Tout sélectionner",
            Margin = new Thickness(6, 0, 6, 4),
            IsChecked = items.All(i => i.IsChecked)
        };
        checkAll.Click += (_, _) =>
        {
            var value = checkAll.IsChecked == true;
            foreach (var item in items)
                item.IsChecked = value;
        };

        // Ligne "Filtrer" + sous-menu des opérateurs
        var filterRowText = new TextBlock { Text = "Filtrer", VerticalAlignment = VerticalAlignment.Center };
        var filterRowIcon = new Path
        {
            Data = Geometry.Parse("M0,0 L12,0 L7,6 L7,12 L5,12 L5,6 Z"),
            Fill = GreyBrush,
            Width = 11,
            Height = 11,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var filterArrow = new Path
        {
            Data = Geometry.Parse("M0,0 L6,0 L3,4 Z"),
            Fill = GreyBrush,
            Width = 6,
            Height = 4,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var filterRowPanel = new Grid();
        filterRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        filterRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterRowPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var filterLeft = new StackPanel { Orientation = Orientation.Horizontal };
        filterLeft.Children.Add(filterRowIcon);
        filterLeft.Children.Add(filterRowText);
        Grid.SetColumn(filterLeft, 0);
        Grid.SetColumn(filterArrow, 2);
        filterRowPanel.Children.Add(filterLeft);
        filterRowPanel.Children.Add(filterArrow);
        var filterRow = new Border
        {
            Padding = new Thickness(6, 4, 6, 4),
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = filterRowPanel
        };

        // Editeur de valeur pour l'opérateur choisi
        var editorLabel = new TextBlock { Margin = new Thickness(0, 0, 0, 3) };
        var input1 = new TextBox { Padding = new Thickness(3), Margin = new Thickness(0, 0, 0, 3) };
        var input2 = new TextBox { Padding = new Thickness(3), Margin = new Thickness(0, 0, 0, 3) };
        var betweenPanel = new StackPanel { Visibility = Visibility.Collapsed };
        betweenPanel.Children.Add(new TextBlock { Text = "et", Margin = new Thickness(0, 0, 0, 3) });
        betweenPanel.Children.Add(input2);
        var okButton = new Button
        {
            Content = "OK",
            Padding = new Thickness(12, 3, 12, 3),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var editor = new StackPanel { Margin = new Thickness(6, 2, 6, 6), Visibility = Visibility.Collapsed };
        editor.Children.Add(editorLabel);
        editor.Children.Add(input1);
        editor.Children.Add(betweenPanel);
        editor.Children.Add(okButton);
        var pendingMode = FilterMode.Equals;

        var operatorsPanel = new StackPanel { Visibility = Visibility.Collapsed };
        foreach (var (label, mode) in Operators)
        {
            if (label == null)
            {
                operatorsPanel.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    Margin = new Thickness(6, 3, 6, 3)
                });
                continue;
            }

            var item = new TextBlock { Text = label, Margin = new Thickness(24, 2, 6, 2) };
            var itemBorder = new Border
            {
                Background = Brushes.Transparent,
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = item
            };
            var captured = mode;
            itemBorder.MouseEnter += (_, _) => itemBorder.Background = HoverBrush;
            itemBorder.MouseLeave += (_, _) => itemBorder.Background = Brushes.Transparent;
            itemBorder.MouseLeftButtonUp += (_, _) =>
            {
                pendingMode = captured;
                editorLabel.Text = captured == FilterMode.IsEmpty ? label : label + " :";
                betweenPanel.Visibility = captured == FilterMode.Between ? Visibility.Visible : Visibility.Collapsed;
                input1.Visibility = captured == FilterMode.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
                input1.Text = string.Empty;
                input2.Text = string.Empty;
                operatorsPanel.Visibility = Visibility.Collapsed;
                filterRow.Background = Brushes.Transparent;
                editor.Visibility = Visibility.Visible;
                input1.Focus();
            };
            operatorsPanel.Children.Add(itemBorder);
        }

        filterRow.MouseLeftButtonUp += (_, _) =>
        {
            var open = operatorsPanel.Visibility != Visibility.Visible;
            operatorsPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            filterRow.Background = open ? HoverBrush : Brushes.Transparent;
        };

        // "Supprimer le filtre" + ESC
        var removeText = new TextBlock { Text = "Supprimer le filtre", VerticalAlignment = VerticalAlignment.Center };
        var escText = new TextBlock { Text = "ESC", Foreground = HintBrush, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var removePanel = new Grid();
        removePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        removePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(removeText, 0);
        Grid.SetColumn(escText, 1);
        removePanel.Children.Add(removeText);
        removePanel.Children.Add(escText);
        var removeRow = new Border
        {
            Padding = new Thickness(6, 4, 6, 4),
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = removePanel
        };
        removeRow.MouseEnter += (_, _) => removeRow.Background = HoverBrush;
        removeRow.MouseLeave += (_, _) => removeRow.Background = Brushes.Transparent;

        var remember = new CheckBox
        {
            Content = "Mémoriser les filtres",
            Margin = new Thickness(6, 4, 6, 6),
            IsChecked = IsRemembered(grid, filter)
        };

        var apply = new Button
        {
            Content = "Appliquer",
            Padding = new Thickness(12, 3, 12, 3),
            IsDefault = true
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(6)
        };
        buttons.Children.Add(apply);

        var panel = new StackPanel { Width = 260 };
        panel.Children.Add(searchBorder);
        panel.Children.Add(filterRow);
        panel.Children.Add(new ScrollViewer
        {
            MaxHeight = 340,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = operatorsPanel
        });
        panel.Children.Add(editor);
        panel.Children.Add(removeRow);
        panel.Children.Add(remember);
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

        void ClearFilter()
        {
            filter.Allowed = null;
            filter.Predicate = null;
            foreach (var item in items)
                item.IsChecked = true;
            checkAll.IsChecked = true;
            RemoveRemember(grid, filter);
            Refresh(grid);
            ApplyIndicator(filter.Button, false);
        }

        void SaveRememberState(FilterMode? mode, string[] inputs)
        {
            if (remember.IsChecked == true)
                StoreRemember(grid, filter, mode, inputs);
            else
                RemoveRemember(grid, filter);
        }

        void OnEscape(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;
            ClearFilter();
            popup.IsOpen = false;
            e.Handled = true;
        }

        searchBox.KeyDown += OnEscape;
        input1.KeyDown += OnEscape;
        input2.KeyDown += OnEscape;
        list.KeyDown += OnEscape;

        removeRow.MouseLeftButtonUp += (_, _) =>
        {
            ClearFilter();
            popup.IsOpen = false;
        };

        apply.Click += (_, _) =>
        {
            var selected = items.Where(i => i.IsChecked).Select(i => i.Value).ToHashSet(StringComparer.Ordinal);
            filter.Predicate = null;
            filter.Allowed = selected.Count == items.Count ? null : selected;
            SaveRememberState(null, Array.Empty<string>());
            popup.IsOpen = false;
            Refresh(grid);
            ApplyIndicator(filter.Button, filter.Allowed != null);
        };

        okButton.Click += (_, _) =>
        {
            var v1 = input1.Text.Trim();
            var v2 = input2.Text.Trim();
            if (pendingMode != FilterMode.IsEmpty && v1.Length == 0)
                return;
            if (pendingMode == FilterMode.Between && v2.Length == 0)
                return;

            filter.Allowed = null;
            filter.Predicate = BuildPredicate(filter.Path, pendingMode, v1, v2);
            SaveRememberState(pendingMode, new[] { v1, v2 });
            popup.IsOpen = false;
            Refresh(grid);
            ApplyIndicator(filter.Button, true);
        };

        popup.IsOpen = true;
    }

    private static Func<object, bool> BuildPredicate(string path, FilterMode mode, string value1, string value2) =>
        item =>
        {
            var value = GetValue(item, path) ?? string.Empty;
            return mode switch
            {
                FilterMode.Equals => CompareValues(value, value1) == 0,
                FilterMode.StartsWith => value.StartsWith(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.Contains => value.Contains(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.EndsWith => value.EndsWith(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.IsEmpty => value.Length == 0,
                FilterMode.NotStartsWith => !value.StartsWith(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.NotContains => !value.Contains(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.NotEndsWith => !value.EndsWith(value1, StringComparison.CurrentCultureIgnoreCase),
                FilterMode.Greater => CompareValues(value, value1) > 0,
                FilterMode.GreaterOrEqual => CompareValues(value, value1) >= 0,
                FilterMode.Less => CompareValues(value, value1) < 0,
                FilterMode.LessOrEqual => CompareValues(value, value1) <= 0,
                FilterMode.Different => CompareValues(value, value1) != 0,
                FilterMode.Between => CompareValues(value, value1) >= 0 && CompareValues(value, value2) <= 0,
                _ => true
            };
        };

    private static int CompareValues(string a, string b)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        if (DateTime.TryParse(a, fr, DateTimeStyles.None, out var da) &&
            DateTime.TryParse(b, fr, DateTimeStyles.None, out var db))
            return da.CompareTo(db);

        if (decimal.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var na) &&
            decimal.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out var nb))
            return na.CompareTo(nb);

        return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
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
            filter.Predicate = null;
            ApplyIndicator(filter.Button, false);
            RemoveRemember(grid, filter);
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

    /// <summary>Titre d'origine d'une colonne (l'entete a ete remplace par un panneau avec le bouton de filtre).</summary>
    public static string GetColumnTitle(DataGridColumn column) =>
        (column.Header as FrameworkElement)?.Tag as string ?? column.Header?.ToString() ?? string.Empty;

    /// <summary>Chemin de liaison d'une colonne (utile pour exporter les valeurs affichees).</summary>
    public static string GetColumnPath(DataGridColumn column) => GetPath(column);

    /// <summary>Valeur texte d'une cellule pour un element et un chemin donnes.</summary>
    public static string GetCellValue(object? item, string path) => GetValue(item, path) ?? string.Empty;
}
