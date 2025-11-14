using myexcel.Core; // Використовуємо правильний namespace

namespace myexcel
{
    public partial class MainPage : ContentPage
    {
        // Початкові розміри з PDF. Використовуються ЛИШЕ для ініціалізації Core.
        const int InitialCountColumn = 20;
        const int InitialCountRow = 50;

        private readonly SpreadsheetCore _core;

        public MainPage()
        {
            InitializeComponent();
            
            // Ініціалізуємо Ядро
            _core = new SpreadsheetCore(InitialCountRow, InitialCountColumn);
            
            // Будуємо UI на основі даних з Ядра
            RebuildGridUI();
        }

        // --- Керування UI ---

        // Повна перебудова UI
        // Викликається 1 раз на старті та після завантаження файлу (Load)
        private void RebuildGridUI()
        {
            // Повністю очищуємо все
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            // === ГОЛОВНЕ ВИПРАВЛЕННЯ: ===
            // Додаємо визначення для нульового рядка (A, B, C...)
            grid.RowDefinitions.Add(new RowDefinition());
            // =============================
            
            // Додаємо визначення для нульового стовпця (1, 2, 3...)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            // Створення стовпців UI (A, B, C...)
            for (int col = 0; col < _core.ColumnCount; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                
                var label = new Label
                {
                    Text = GetColumnName(col + 1), // col+1, бо індекси з 1
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, 0); // Ставимо у 0-й рядок (тепер він існує)
                Grid.SetColumn(label, col + 1); // col+1, бо 0-й стовпець - заголовки
                grid.Children.Add(label);
            }

            // Створення рядків UI (1, 2, 3...)
            for (int row = 0; row < _core.RowCount; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, row + 1); // row+1, бо 0-й рядок - заголовки
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                // Створення комірок (Entry)
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

        // Легке оновлення значень
        // Викликається, коли змінюються лише дані (після Calculate або Entry_Unfocused)
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
                        // Оновлюємо текст Entry згідно даних в Core
                        entry.Text = _core.GetCellDisplayValue(row, col);
                    }
                }
            }
        }

        // --- ОБРОБНИКИ ПОДІЙ ---

        // Виправлено для CS8622 (nullable warning)
        private void Entry_Unfocused(object? sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            if (entry == null) return;

            int row = Grid.GetRow(entry) - 1;
            int col = Grid.GetColumn(entry) - 1;
            
            if (row < 0 || col < 0) return; // Це не клітинка сітки (можливо textInput)

            // 1. UI передає команду Ядру
            _core.UpdateCell(row, col, entry.Text);
        }

        // --- ОПТИМІЗОВАНІ МЕТОДИ ДОДАВАННЯ/ВИДАЛЕННЯ ---

        private void AddRowButton_Clicked(object? sender, EventArgs e)
        {
            _core.AddRow();
            
            int newUiRowIndex = _core.RowCount; 
            
            grid.RowDefinitions.Add(new RowDefinition());

            var label = new Label
            {
                Text = newUiRowIndex.ToString(),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetRow(label, newUiRowIndex);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 0; col < _core.ColumnCount; col++)
            {
                var entry = new Entry
                {
                    Text = "", 
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, newUiRowIndex);
                Grid.SetColumn(entry, col + 1); 
                grid.Children.Add(entry);
            }
        }

        private void DeleteRowButton_Clicked(object? sender, EventArgs e)
        {
            // 1. ПЕРЕВІРЯЄМО СТАН ЯДРА. Чи є взагалі дані для видалення?
            if (_core.RowCount <= 0)
                return;

            // 2. Отримуємо індекс ОСТАННЬОГО РЯДКА в UI.
            //    (Оскільки UI має рядок заголовка, індекс = _core.RowCount)
            int lastUiRowIndex = _core.RowCount; 
            
            // 3. Видаляємо всі UI-елементи з цього рядка
            var childrenToRemove = grid.Children
                .Where(c => grid.GetRow(c) == lastUiRowIndex)
                .ToList();
                
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }

            // 4. Видаляємо саме визначення рядка з UI
            grid.RowDefinitions.RemoveAt(lastUiRowIndex);

            // 5. ТЕПЕР кажемо ядру видалити свої дані
            _core.DeleteRow(); 
        }

        private void AddColumnButton_Clicked(object? sender, EventArgs e)
        {
            _core.AddColumn();

            int newUiColIndex = _core.ColumnCount; 

            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new Label
            {
                Text = GetColumnName(newUiColIndex),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, newUiColIndex);
            grid.Children.Add(label);

            for (int row = 0; row < _core.RowCount; row++)
            {
                var entry = new Entry
                {
                    Text = "",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, row + 1);
                Grid.SetColumn(entry, newUiColIndex);
                grid.Children.Add(entry);
            }
        }

        private void DeleteColumnButton_Clicked(object? sender, EventArgs e)
        {
            // 1. ПЕРЕВІРЯЄМО СТАН ЯДРА.
            if (_core.ColumnCount <= 0)
                return;

            // 2. Отримуємо індекс ОСТАННЬОГО СТОВПЦЯ в UI
            int lastUiColIndex = _core.ColumnCount;

            // 3. Видаляємо всі UI-елементи з цього стовпця
            var childrenToRemove = grid.Children
                .Where(c => grid.GetColumn(c) == lastUiColIndex)
                .ToList();
                
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }

            // 4. Видаляємо саме визначення стовпця з UI
            grid.ColumnDefinitions.RemoveAt(lastUiColIndex);

            // 5. ТЕПЕР кажемо ядру видалити свої дані
            _core.DeleteColumn();
        }

        // --- РЕАЛІЗОВАНІ ЗБЕРЕЖЕННЯ / ЗАВАНТАЖЕННЯ ---

        private async void SaveButton_Clicked(object? sender, EventArgs e)
        {
            string fileName = "mysheet.json";
            // Зберігаємо у безпечну папку, спільну для додатку
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            try
            {
                await _core.SaveToFile(path);
                
                await DisplayAlert("Збережено", 
                    $"Файл успішно збережено у:\n{path}", 
                    "OK");
            }
            catch (Exception ex)
            {
                // Показуємо помилку, якщо Core її "кинув"
                await DisplayAlert("Помилка збереження", 
                    $"Не вдалося зберегти файл: {ex.Message}", 
                    "OK");
            }
        }

        private async void ReadButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                // Налаштування фільтру для .json файлів
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
                
                // 1. Відкриваємо системний діалог вибору файлу
                var result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    // 2. Ядро завантажує дані
                    await _core.LoadFromFile(result.FullPath);
                    
                    // 3. Потрібна повна перебудова UI,
                    //    оскільки розміри таблиці могли змінитися
                    RebuildGridUI();
                    
                    await DisplayAlert("Завантажено", "Таблицю успішно завантажено.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Показуємо помилку, якщо Core її "кинув"
                await DisplayAlert("Помилка завантаження", 
                    $"Не вдалося завантажити або обробити файл: {ex.Message}", 
                    "OK");
            }
        }

        // --- ІНШІ ОБРОБНИКИ ---

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
                "Лабораторна робота 1.Cаплюкова Юрія", 
                "OK");
        }

        // Допоміжний метод з PDF
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