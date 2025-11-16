using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.IO; 
using myexcel.Core; // Використовуємо правильний namespace
using CommunityToolkit.Maui.Storage; // Для FileSaver
using System.Threading; // Для CancellationToken

namespace myexcel
{
    public partial class MainPage : ContentPage
    {
        const int InitialCountColumn = 20;
        const int InitialCountRow = 20;

        private readonly SpreadsheetCore _core;

        public MainPage()
        {
            InitializeComponent();
            _core = new SpreadsheetCore(InitialCountRow, InitialCountRow);
            RebuildGridUI();
        }
        
        private void RebuildGridUI()
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            grid.RowDefinitions.Add(new RowDefinition());

            grid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int col = 0; col < _core.ColumnCount; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                
                var label = new Label
                {
                    Text = GetColumnName(col + 1), // col+1
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, 0); 
                Grid.SetColumn(label, col + 1); 
                grid.Children.Add(label);
            }

            for (int row = 0; row < _core.RowCount; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, row + 1); 
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);
                
                for (int col = 0; col < _core.ColumnCount; col++)
                {
                    var entry = new Entry
                    {
                        Text = _core.GetCellDisplayValue(row, col),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    entry.Unfocused += Entry_Unfocused;
                    Grid.SetRow(entry, row + 1);
                    Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }
            }
        }
        
        private void RefreshGridDisplayValues()
        {
            foreach (var child in grid.Children)
            {
                if (child is Entry entry)
                {
                    int row = Grid.GetRow(entry) - 1;
                    int col = Grid.GetColumn(entry) - 1;

                    if (row >= 0 && col >= 0 && row < _core.RowCount && col < _core.ColumnCount)
                    {
                        entry.Text = _core.GetCellDisplayValue(row, col);
                    }
                }
            }
        }
        
        private void Entry_Unfocused(object? sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            if (entry == null) return;

            int row = Grid.GetRow(entry) - 1;
            int col = Grid.GetColumn(entry) - 1;
            
            if (row < 0 || col < 0) return;

            _core.UpdateCell(row, col, entry.Text);
        }
        
        private void AddRowButton_Clicked(object? sender, EventArgs e)
        {
            _core.AddRow();
            int newUiRowIndex = _core.RowCount; 
            
            grid.RowDefinitions.Add(new RowDefinition());

            var label = new Label { Text = newUiRowIndex.ToString(), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            Grid.SetRow(label, newUiRowIndex);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 0; col < _core.ColumnCount; col++)
            {
                var entry = new Entry { Text = "", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, newUiRowIndex);
                Grid.SetColumn(entry, col + 1); 
                grid.Children.Add(entry);
            }
        }

        private void DeleteRowButton_Clicked(object? sender, EventArgs e)
        {
            if (_core.RowCount <= 0)
                return;

            int lastUiRowIndex = _core.RowCount; 
            
            var childrenToRemove = grid.Children
                .Where(c => grid.GetRow(c) == lastUiRowIndex)
                .ToList();
                
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }

            grid.RowDefinitions.RemoveAt(lastUiRowIndex);
            _core.DeleteRow(); 
        }

        private void AddColumnButton_Clicked(object? sender, EventArgs e)
        {
            _core.AddColumn();
            int newUiColIndex = _core.ColumnCount; 

            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new Label { Text = GetColumnName(newUiColIndex), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, newUiColIndex);
            grid.Children.Add(label);

            for (int row = 0; row < _core.RowCount; row++)
            {
                var entry = new Entry { Text = "", VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, row + 1);
                Grid.SetColumn(entry, newUiColIndex);
                grid.Children.Add(entry);
            }
        }
        private void DeleteColumnButton_Clicked(object? sender, EventArgs e)
        {
            if (_core.ColumnCount <= 0)
                return;

            int lastUiColIndex = _core.ColumnCount;

            var childrenToRemove = grid.Children
                .Where(c => grid.GetColumn(c) == lastUiColIndex)
                .ToList();
                
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }

            grid.ColumnDefinitions.RemoveAt(lastUiColIndex);
            _core.DeleteColumn();
        }
        

        private async void SaveButton_Clicked(object? sender, EventArgs e)
        {
            CancellationToken cancellationToken = CancellationToken.None;

            try
            {
                using var stream = new MemoryStream();
                
                await _core.SaveToStreamAsync(stream);
                
                stream.Seek(0, SeekOrigin.Begin);

                var saveResult = await FileSaver.Default.SaveAsync(
                    "mysheet.json",
                    stream,         
                    cancellationToken
                );

                if (saveResult.IsSuccessful)
                {
                    await DisplayAlert("Збережено", 
                        $"Файл успішно збережено за шляхом:\n{saveResult.FilePath}", 
                        "OK");
                }
                else
                {
                    await DisplayAlert("Скасовано", 
                        $"Не вдалося зберегти файл: {saveResult.Exception?.Message}", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка збереження", 
                    $"Виникла помилка: {ex.Message}", 
                    "OK");
            }
        }

        private async void ReadButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                var fileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".json" } },
                        { DevicePlatform.Android, new[] { "application/json" } },
                        { DevicePlatform.iOS, new[] { "public.json" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.json" } }
                    });

                var options = new PickOptions
                {
                    PickerTitle = "Виберіть файл електронної таблиці (.json)",
                    FileTypes = fileTypes,
                };
                
                var result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    await _core.LoadFromFile(result.FullPath);
                    // Потрібна повна перебудова UI
                    RebuildGridUI();
                    
                    await DisplayAlert("Завантажено", "Таблицю успішно завантажено.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка завантаження", 
                    $"Не вдалося завантажити або обробити файл: {ex.Message}", 
                    "OK");
            }
        }

        private void CalculateButton_Clicked(object? sender, EventArgs e)
        {
            _core.RecalculateAll();
            RefreshGridDisplayValues();
        }

        private async void ExitButton_Clicked(object? sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                System.Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object? sender, EventArgs e)
        {
            await DisplayAlert("Довідка", 
                "Лабораторна робота 1. Варіант 8.\n" + 
                "Підтримувані операції: +, -, *, /, ^, inc, dec, mod, div, mmin, mmax", 
                "OK");
        }
        
        private void ToggleViewButton_Clicked(object? sender, EventArgs e)
        {
            _core.ToggleFormulaMode();

            RefreshGridDisplayValues();
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }
    }
}