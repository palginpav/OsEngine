using System;

namespace CalculatorModule
{
    /// <summary>
    /// Simple calculator module for testing dynamic loading
    /// </summary>
    public class CalculatorModule
    {
        public string Name => "Calculator Module";
        public string Version => "1.0.0";
        public string Description => "A simple calculator module for testing dynamic loading";

        public double Add(double a, double b) => a + b;
        public double Subtract(double a, double b) => a - b;
        public double Multiply(double a, double b) => a * b;
        public double Divide(double a, double b) => b != 0 ? a / b : throw new DivideByZeroException();

        public override string ToString()
        {
            return $"{Name} v{Version} - {Description}";
        }
    }
}
