using myexcel.Core.Parsing; // Використовуємо правильний namespace
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace myexcel.Core
{
    public class SpreadsheetCore
    {
        private List<List<Cell>> _cells;
        private readonly ExpressionEvaluator _evaluator;
        private HashSet<string> _evaluationChain; 
        private int _initialCols;

        public int RowCount => _cells.Count;
        public int ColumnCount => RowCount > 0 ? _cells[0].Count : 0;

        public SpreadsheetCore(int initialRows, int initialCols)
        {
            _initialCols = initialCols;
            _evaluator = new ExpressionEvaluator(GetCellValueByName);
            _evaluationChain = new HashSet<string>();

            _cells = new List<List<Cell>>();
            for (int r = 0; r < initialRows; r++)
            {
                var newRow = new List<Cell>();
                for (int c = 0; c < initialCols; c++)
                {
                    newRow.Add(new Cell());
                }
                _cells.Add(newRow);
            }
        }

        public void UpdateCell(int row, int col, string expression)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return;

            if (_cells[row][col].Expression == expression)
                return; 

            _cells[row][col].Expression = expression;
            RecalculateAll(); // Запускаємо повний перерахунок
        }

        public string GetCellDisplayValue(int row, int col)
        {
            if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                return string.Empty;
            return _cells[row][col].Value?.ToString() ?? string.Empty;
        }

        // Обробка помилок обчислення
        public void RecalculateAll()
        {
            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    var cell = _cells[r][c];
                    if (string.IsNullOrWhiteSpace(cell.Expression))
                    {
                        cell.Value = string.Empty;
                        continue;
                    }

                    if (!cell.Expression.Trim().StartsWith("="))
                    {
                        cell.Value = cell.Expression;
                    }
                    else
                    {
                        // Перехоплюємо будь-які помилки (синтаксис, ділення на 0, цикли)
                        try
                        {
                            _evaluationChain.Clear(); 
                            cell.Value = EvaluateCell(r, c);
                        }
                        catch (Exception ex)
                        {
                            cell.Value = $"#ERROR: {ex.Message}";
                        }
                    }
                }
            }
        }

        private double EvaluateCell(int row, int col)
        {
            var cell = _cells[row][col];
            string cellName = GetCellName(row, col);

            // 1. Перевірка на циркулярне посилання
            if (_evaluationChain.Contains(cellName))
            {
                throw new InvalidOperationException($"Циркулярне посилання на {cellName}");
            }
            _evaluationChain.Add(cellName);

            // 2. Якщо це не формула, повертаємо значення
            if (!cell.Expression.Trim().StartsWith("="))
            {
                _evaluationChain.Remove(cellName);
                if (double.TryParse(cell.Value?.ToString(), out double val))
                {
                    return val;
                }
                throw new InvalidOperationException($"Клітинка {cellName} не є числом.");
            }

            // 3. Це формула, обчислюємо
            string expression = cell.Expression.Trim().Substring(1); 
            double result = _evaluator.Evaluate(expression);
            
            _evaluationChain.Remove(cellName); 
            return result;
        }

        private double GetCellValueByName(string cellName)
        {
            try
            {
                (int row, int col) = ParseCellName(cellName);
                if (row < 0 || row >= RowCount || col < 0 || col >= ColumnCount)
                {
                    throw new IndexOutOfRangeException($"Посилання {cellName} виходить за межі таблиці.");
                }
                // Рекурсивний виклик для обчислення залежностей
                return EvaluateCell(row, col);
            }
            catch (Exception ex)
            {
                // "Прокидаємо" помилку нагору
                throw new InvalidOperationException($"Помилка у посиланні {cellName}. {ex.Message}", ex);
            }
        }

        // --- Керування сіткою (Логіка) ---
        public void AddRow()
        {
            var newRow = new List<Cell>();
            int cols = ColumnCount > 0 ? ColumnCount : _initialCols;
            for (int c = 0; c < cols; c++)
            {
                newRow.Add(new Cell());
            }
            _cells.Add(newRow);
        }

        public void DeleteRow()
        {
            if (RowCount > 1) _cells.RemoveAt(RowCount - 1);
        }

        public void AddColumn()
        {
            foreach (var row in _cells) row.Add(new Cell());
        }

        public void DeleteColumn()
        {
            if (ColumnCount > 1)
            {
                foreach (var row in _cells) row.RemoveAt(ColumnCount - 1);
            }
        }
        
        // --- Збереження / Завантаження (Логіка) ---
        public async Task SaveToFile(string filePath)
        {
            try
            {
                // 1. Створюємо "чисту" структуру лише з виразами
                var dataToSave = _cells.Select(row => 
                    row.Select(cell => cell.Expression).ToList()
                ).ToList();

                // 2. Серілізуємо в JSON-рядок
                string json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });

                // 3. Зберігаємо у файл
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception)
            {
                // Кидаємо помилку, щоб UI міг її зловити
                throw new IOException("Не вдалося зберегти файл. Перевірте доступи.");
            }
        }

        public async Task LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не знайдено", filePath);

            try
            {
                // 1. Читаємо JSON з файлу
                string json = await File.ReadAllTextAsync(filePath);

                // 2. Десеріалізуємо у нашу структуру
                var loadedData = JsonSerializer.Deserialize<List<List<string>>>(json);

                _cells.Clear(); 
                if (loadedData != null)
                {
                    // 3. Відновлюємо клітинки з завантажених даних
                    foreach (var rowData in loadedData)
                    {
                        var newRow = new List<Cell>();
                        foreach (var exp in rowData)
                        {
                            newRow.Add(new Cell { Expression = exp });
                        }
                        _cells.Add(newRow);
                    }
                }
                // 4. Перераховуємо всю таблицю, щоб заповнити `.Value`
                RecalculateAll();
            }
            catch(Exception ex) 
            { 
                // Кидаємо помилку, щоб UI міг її зловити
                throw new JsonException($"Файл пошкоджений або має невірний формат. {ex.Message}");
            }
        }

        // --- Допоміжні методи ---
        private string GetCellName(int row, int col)
        {
            return $"{GetColumnName(col + 1)}{row + 1}";
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

        private (int row, int col) ParseCellName(string cellName)
        {
            // Використовуємо Regex для надійного парсингу "A1", "BC23" і т.д.
            var match = Regex.Match(cellName.ToUpper(), @"^([A-Z]+)(\d+)$");
            if (!match.Success)
            {
                throw new FormatException($"Невірний формат клітинки: {cellName}");
            }

            string colName = match.Groups[1].Value;
            int row = int.Parse(match.Groups[2].Value) - 1; // "1" -> 0

            int col = 0;
            int pow = 1;
            for (int i = colName.Length - 1; i >= 0; i--)
            {
                col += (colName[i] - 'A' + 1) * pow;
                pow *= 26;
            }
            col--; // 'A' = 0, 'B' = 1...

            return (row, col);
        }
    }
}