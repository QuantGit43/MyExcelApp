namespace myexcel.Core
{
    // Клас, що описує дані ОДНІЄЇ клітинки.
    // Він нічого не знає про UI.
    public class Cell
    {
        // Те, що ввів користувач (напр., "=A1+5" або "Привіт")
        public string Expression { get; set; } = string.Empty;

        // Обчислене значення (напр., 15 або "Привіт")
        public object Value { get; set; } = string.Empty;
    }
}