namespace myexcel.Core
{
    public record Cell
    {
        public string Expression { get; set; } = string.Empty;

        public object Value { get; set; } = string.Empty;
    }
}