using Antlr4.Runtime;
using System.IO;

// Переконайтеся, що namespace правильний (myexcel або MyExcelMAUIApp)
namespace myexcel.Core.Parsing 
{
    public class ExpressionEvaluator
    {
        private readonly Func<string, double> _cellValueProvider;

        public ExpressionEvaluator(Func<string, double> cellValueProvider)
        {
            _cellValueProvider = cellValueProvider;
        }

        public double Evaluate(string expression)
        {
            var inputStream = new AntlrInputStream(expression);
            var lexer = new LabCalculatorLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new LabCalculatorParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());

            var tree = parser.compileUnit();

            var visitor = new LabCalculatorVisitor(_cellValueProvider);
            return visitor.Visit(tree);
        }
    }

    public class ThrowExceptionErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output, 
            IRecognizer recognizer, 
            IToken offendingSymbol, 
            int line, 
            int charPositionInLine, 
            string msg, 
            RecognitionException e)
        {
            throw new ArgumentException($"Синтаксична помилка: {msg} (позиція {charPositionInLine})");
        }
    }
}