using Antlr4.Runtime.Tree;
using System.Globalization;
using System.Linq;

// Переконайтеся, що namespace правильний (myexcel або MyExcelMAUIApp)
namespace myexcel.Core.Parsing 
{
    public class LabCalculatorVisitor : LabCalculatorBaseVisitor<double>
    {
        private readonly Func<string, double> _cellValueProvider;

        public LabCalculatorVisitor(Func<string, double> cellValueProvider)
        {
            _cellValueProvider = cellValueProvider ?? throw new ArgumentNullException(nameof(cellValueProvider));
        }

        public override double VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
        {
            return this.Visit(context.expression());
        }

        public override double VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
        {
            return this.Visit(context.expression());
        }

        public override double VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
        {
            return double.Parse(context.NUMBER().GetText(), CultureInfo.InvariantCulture);
        }

        public override double VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
        {
            string cellName = context.IDENTIFIER().GetText().ToUpper();
            try
            {
                return _cellValueProvider(cellName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        public override double VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context)
        {
            double value = this.Visit(context.expression());
            if (context.op.Type == LabCalculatorLexer.SUBTRACT)
            {
                return -value;
            }
            return value;
        }
        
        public override double VisitIncDecExpr(LabCalculatorParser.IncDecExprContext context)
        {
            double value = this.Visit(context.expression());
            if (context.op.Type == LabCalculatorLexer.INC)
            {
                return value + 1;
            }
            return value - 1;
        }

        public override double VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
        {
            double left = this.Visit(context.expression(0));
            double right = this.Visit(context.expression(1));
            return Math.Pow(left, right);
        }

        public override double VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
        {
            double left = this.Visit(context.expression(0));
            double right = this.Visit(context.expression(1));

            if (context.op.Type == LabCalculatorLexer.MULTIPLY)
            {
                return left * right;
            }
            if (right == 0)
            {
                throw new DivideByZeroException("Ділення на нуль.");
            }
            return left / right;
        }

        public override double VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
        {
            double left = this.Visit(context.expression(0));
            double right = this.Visit(context.expression(1));

            if (context.op.Type == LabCalculatorLexer.ADD)
            {
                return left + right;
            }
            return left - right;
        }

        public override double VisitModDivExpr(LabCalculatorParser.ModDivExprContext context)
        {
            double left = this.Visit(context.expression(0));
            double right = this.Visit(context.expression(1));

            if (right == 0)
            {
                throw new DivideByZeroException("Ділення на нуль (MOD або DIV).");
            }
            if (context.op.Type == LabCalculatorLexer.MOD)
            {
                return left % right;
            }
            return (int)left / (int)right;
        }

        public override double VisitMMinExpr(LabCalculatorParser.MMinExprContext context)
        {
            var expressions = context.paramlist()?.expression();
            if (expressions == null || !expressions.Any())
            {
                throw new ArgumentException("mmin() вимагає хоча б один аргумент.");
            }
            
            double minValue = double.PositiveInfinity;
            foreach (var expr in expressions)
            {
                minValue = Math.Min(minValue, this.Visit(expr));
            }
            return minValue;
        }

        public override double VisitMMaxExpr(LabCalculatorParser.MMaxExprContext context)
        {
            var expressions = context.paramlist()?.expression();
            if (expressions == null || !expressions.Any())
            {
                throw new ArgumentException("mmax() вимагає хоча б один аргумент.");
            }
            
            double maxValue = double.NegativeInfinity;
            foreach (var expr in expressions)
            {
                maxValue = Math.Max(maxValue, this.Visit(expr));
            }
            return maxValue;
        }
    }
}