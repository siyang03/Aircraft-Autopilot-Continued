// GPL-3.0-only. Independent replacement for MechJeb EditableDouble.
using System.Globalization;
namespace AircraftAutopilotContinued
{
    public class EditableDouble
    {
        double storedValue;
        string text;
        public bool Valid { get; private set; }
        public double Val { get => storedValue; set { if (!Maths.Finite(value)) return; storedValue = value; text = value.ToString("G", CultureInfo.InvariantCulture); Valid = true; } }
        public string Text
        {
            get => text;
            set { text = value; double parsed; Valid = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && Maths.Finite(parsed); if (Valid) storedValue = parsed; }
        }
        public EditableDouble(double value) { Val = value; }
        public static implicit operator double(EditableDouble x) => x.Val;
        public static implicit operator EditableDouble(double x) => new EditableDouble(x);
    }
}
