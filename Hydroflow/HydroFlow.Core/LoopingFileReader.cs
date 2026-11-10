/*
 * File: LoopingFileReader.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * Loads readings from a data file and repeats them in order.
 * Proposed API pending team confirmation.
 */

using System.Globalization;

namespace HydroFlow.Core;

public class LoopingFileReader : IDataSource
{
    // Store the readings and the position of the next value.
    private readonly double[] _values;
    private int _index;

    /*
     * Purpose: Load the file and check that each reading is a finite number.
     */
    public LoopingFileReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        var values = new List<double>();
        int lineNumber = 0;

        foreach (var line in File.ReadLines(filePath))
        {
            lineNumber++;

            // Skip blank lines while keeping the original line numbers.
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Use a period for decimals and reject invalid or nonfinite values.
            if (!double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || !double.IsFinite(value))
            {
                throw new FormatException(
                    $"Line {lineNumber} of '{filePath}' is not a valid finite number: '{line}'.");
            }

            values.Add(value);
        }

        // At least one reading is needed for the file to loop.
        if (values.Count == 0)
            throw new InvalidDataException($"'{filePath}' contains no readings.");

        _values = values.ToArray();
    }

    /*
     * Purpose: Return the next reading and wrap back to the first when needed.
     */
    public double ReadNext()
    {
        double value = _values[_index];
        _index = (_index + 1) % _values.Length;
        return value;
    }
}