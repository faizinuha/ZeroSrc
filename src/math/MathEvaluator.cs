using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZeroMix
{
    /// <summary>
    /// Lightweight Math Expression Evaluator
    /// Supports: +, -, *, /, ^, %, sqrt, abs, round, floor, ceil, pi, e
    /// NO external dependencies - Pure C#
    /// </summary>
    public static class MathEvaluator
    {
        private static readonly Dictionary<string, double> Constants = new()
        {
            { "pi", Math.PI },
            { "e", Math.E }
        };

        /// <summary>
        /// Deteksi apakah string adalah math expression
        /// </summary>
        public static bool IsMathExpression(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Hapus whitespace
            input = input.Trim().ToLower();

            // Cek apakah mengandung operator atau fungsi math
            var mathPattern = @"^[\d\s\+\-\*\/\^\%\(\)\.piea-z]+$";
            if (!Regex.IsMatch(input, mathPattern)) return false;

            // Harus ada minimal 1 operator atau fungsi
            var hasOperator = Regex.IsMatch(input, @"[\+\-\*\/\^\%]");
            var hasFunction = Regex.IsMatch(input, @"(sqrt|abs|round|floor|ceil|sin|cos|tan|log)");
            var hasConstant = input.Contains("pi") || input.Contains("e");

            return hasOperator || hasFunction || hasConstant;
        }

        /// <summary>
        /// Evaluate math expression dan return hasil
        /// </summary>
        public static (bool success, double result, string error) Evaluate(string expression)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                    return (false, 0, "Empty expression");

                // Normalize expression
                expression = expression.ToLower().Replace(" ", "");

                // Replace constants
                foreach (var constant in Constants)
                {
                    expression = expression.Replace(constant.Key, constant.Value.ToString(CultureInfo.InvariantCulture));
                }

                // Evaluate functions first
                expression = EvaluateFunctions(expression);

                // Evaluate main expression
                double result = EvaluateExpression(expression);

                return (true, result, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }

        private static string EvaluateFunctions(string expression)
        {
            // sqrt(x)
            expression = Regex.Replace(expression, @"sqrt\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Sqrt(value).ToString(CultureInfo.InvariantCulture);
            });

            // abs(x)
            expression = Regex.Replace(expression, @"abs\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Abs(value).ToString(CultureInfo.InvariantCulture);
            });

            // round(x)
            expression = Regex.Replace(expression, @"round\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            });

            // floor(x)
            expression = Regex.Replace(expression, @"floor\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Floor(value).ToString(CultureInfo.InvariantCulture);
            });

            // ceil(x)
            expression = Regex.Replace(expression, @"ceil\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Ceiling(value).ToString(CultureInfo.InvariantCulture);
            });

            // sin(x), cos(x), tan(x)
            expression = Regex.Replace(expression, @"sin\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Sin(value).ToString(CultureInfo.InvariantCulture);
            });

            expression = Regex.Replace(expression, @"cos\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Cos(value).ToString(CultureInfo.InvariantCulture);
            });

            expression = Regex.Replace(expression, @"tan\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Tan(value).ToString(CultureInfo.InvariantCulture);
            });

            // log(x)
            expression = Regex.Replace(expression, @"log\(([^)]+)\)", match =>
            {
                double value = EvaluateExpression(match.Groups[1].Value);
                return Math.Log10(value).ToString(CultureInfo.InvariantCulture);
            });

            return expression;
        }

        private static double EvaluateExpression(string expression)
        {
            // Remove whitespace
            expression = expression.Replace(" ", "");

            // Handle parentheses first
            while (expression.Contains("("))
            {
                var match = Regex.Match(expression, @"\(([^()]+)\)");
                if (!match.Success) break;

                var innerResult = EvaluateExpression(match.Groups[1].Value);
                expression = expression.Replace(match.Value, innerResult.ToString(CultureInfo.InvariantCulture));
            }

            // Evaluate power (^) - highest precedence
            while (expression.Contains("^"))
            {
                var match = Regex.Match(expression, @"([\d\.]+)\^([\d\.]+)");
                if (!match.Success) break;

                double left = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                double right = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                double result = Math.Pow(left, right);

                expression = expression.Replace(match.Value, result.ToString(CultureInfo.InvariantCulture));
            }

            // Evaluate *, /, %
            var tokens = Tokenize(expression);
            tokens = EvaluateMultiplicationDivision(tokens);

            // Evaluate +, -
            tokens = EvaluateAdditionSubtraction(tokens);

            return tokens.Count == 1 ? double.Parse(tokens[0], CultureInfo.InvariantCulture) : 0;
        }

        private static List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var currentNumber = "";

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (char.IsDigit(c) || c == '.')
                {
                    currentNumber += c;
                }
                else if (c == '-' && (i == 0 || "+-*/^%(".Contains(expression[i - 1])))
                {
                    // Negative number
                    currentNumber += c;
                }
                else if ("+-*/%^".Contains(c))
                {
                    if (!string.IsNullOrEmpty(currentNumber))
                    {
                        tokens.Add(currentNumber);
                        currentNumber = "";
                    }
                    tokens.Add(c.ToString());
                }
            }

            if (!string.IsNullOrEmpty(currentNumber))
                tokens.Add(currentNumber);

            return tokens;
        }

        private static List<string> EvaluateMultiplicationDivision(List<string> tokens)
        {
            var result = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                if (i > 0 && i < tokens.Count - 1)
                {
                    if (tokens[i] == "*")
                    {
                        double left = double.Parse(result[result.Count - 1], CultureInfo.InvariantCulture);
                        double right = double.Parse(tokens[i + 1], CultureInfo.InvariantCulture);
                        result[result.Count - 1] = (left * right).ToString(CultureInfo.InvariantCulture);
                        i++; // Skip next token
                        continue;
                    }
                    else if (tokens[i] == "/")
                    {
                        double left = double.Parse(result[result.Count - 1], CultureInfo.InvariantCulture);
                        double right = double.Parse(tokens[i + 1], CultureInfo.InvariantCulture);
                        result[result.Count - 1] = (left / right).ToString(CultureInfo.InvariantCulture);
                        i++; // Skip next token
                        continue;
                    }
                    else if (tokens[i] == "%")
                    {
                        double left = double.Parse(result[result.Count - 1], CultureInfo.InvariantCulture);
                        double right = double.Parse(tokens[i + 1], CultureInfo.InvariantCulture);
                        result[result.Count - 1] = (left % right).ToString(CultureInfo.InvariantCulture);
                        i++; // Skip next token
                        continue;
                    }
                }

                result.Add(tokens[i]);
            }

            return result;
        }

        private static List<string> EvaluateAdditionSubtraction(List<string> tokens)
        {
            double result = double.Parse(tokens[0], CultureInfo.InvariantCulture);

            for (int i = 1; i < tokens.Count; i += 2)
            {
                if (i >= tokens.Count - 1) break;

                string op = tokens[i];
                double value = double.Parse(tokens[i + 1], CultureInfo.InvariantCulture);

                if (op == "+")
                    result += value;
                else if (op == "-")
                    result -= value;
            }

            return new List<string> { result.ToString(CultureInfo.InvariantCulture) };
        }

        /// <summary>
        /// Format hasil untuk display yang lebih readable
        /// </summary>
        public static string FormatResult(double result)
        {
            // Jika integer, tampilkan tanpa desimal
            if (result == Math.Floor(result))
                return result.ToString("0");

            // Jika desimal, tampilkan max 10 digit
            return result.ToString("0.##########");
        }
    }
}
