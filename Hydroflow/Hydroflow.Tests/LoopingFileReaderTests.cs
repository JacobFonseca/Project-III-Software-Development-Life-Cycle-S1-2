/*
 * File: LoopingFileReaderTests.cs
 * Author: Jacob Fonseca
 * Project: HydroFlow SCADA
 * Task: HF-53
 *
 * Unit tests for LoopingFileReader. Each test writes its own data file
 * inside a unique temporary folder that is deleted after the test.
 */

using System.Globalization;
using HydroFlow.Core;

namespace HydroFlow.Tests;

[TestClass]
public class LoopingFileReaderTests
{
    // MSTest creates a new class instance for every test, so each test gets its own folder.
    private string _tempDirectory = "";

    /*
     * Purpose: Create a unique temporary folder before each test.
     */
    [TestInitialize]
    public void CreateTempDirectory()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "HydroFlowTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /*
     * Purpose: Delete the temporary folder and its files after each test, even when the test fails.
     */
    [TestCleanup]
    public void DeleteTempDirectory()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    /*
     * Purpose: Confirm values come back in the same order as the file.
     */
    [TestMethod]
    public void ReadNext_MultipleValues_ReturnsValuesInFileOrder()
    {
        var reader = new LoopingFileReader(WriteDataFile("10", "20", "30"));

        Assert.AreEqual(10.0, reader.ReadNext());
        Assert.AreEqual(20.0, reader.ReadNext());
        Assert.AreEqual(30.0, reader.ReadNext());
    }

    /*
     * Purpose: Confirm reading continues from the first value after the last one, for several cycles.
     */
    [TestMethod]
    public void ReadNext_AfterFinalValue_WrapsToFirstValue()
    {
        var reader = new LoopingFileReader(WriteDataFile("10", "20", "30"));

        var expected = new[] { 10.0, 20.0, 30.0, 10.0, 20.0, 30.0, 10.0, 20.0, 30.0 };
        CollectionAssert.AreEqual(expected, ReadValues(reader, expected.Length));
    }

    /*
     * Purpose: Confirm a file with one reading returns that reading every time.
     */
    [TestMethod]
    public void ReadNext_SingleValue_RepeatsValue()
    {
        var reader = new LoopingFileReader(WriteDataFile("42.5"));

        CollectionAssert.AreEqual(new[] { 42.5, 42.5, 42.5, 42.5 }, ReadValues(reader, 4));
    }

    /*
     * Purpose: Confirm empty and whitespace lines are ignored and never returned as readings.
     */
    [TestMethod]
    public void ReadNext_BlankLines_SkipsBlankLines()
    {
        var reader = new LoopingFileReader(WriteDataFile("10", "", "   ", "20", "\t", "30", ""));

        CollectionAssert.AreEqual(new[] { 10.0, 20.0, 30.0, 10.0 }, ReadValues(reader, 4));
    }

    /*
     * Purpose: Confirm decimals and exponents parse with a period even when the
     * current culture uses a comma for decimals.
     */
    [TestMethod]
    public void Constructor_DecimalAndExponentValues_ParsesWithInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var reader = new LoopingFileReader(WriteDataFile("12.5", "-3.25", "1.5e3", "2E-2", "  400  "));

            CollectionAssert.AreEqual(new[] { 12.5, -3.25, 1500.0, 0.02, 400.0 }, ReadValues(reader, 5));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /*
     * Purpose: Confirm invalid text, comma decimals, NaN, infinity and overflow are rejected
     * with a FormatException that names line 2.
     */
    [TestMethod]
    [DataRow("abc")]
    [DataRow("12 lux")]
    [DataRow("1,5")]
    [DataRow("NaN")]
    [DataRow("Infinity")]
    [DataRow("-Infinity")]
    [DataRow("1e400")]
    [DataRow("-1e400")]
    public void Constructor_InvalidValue_ThrowsFormatException(string badValue)
    {
        string path = WriteDataFile("10", badValue);

        try
        {
            new LoopingFileReader(path);
            Assert.Fail("Expected exception");
        }
        catch (FormatException ex)
        {
            StringAssert.Contains(ex.Message, "Line 2 of");
        }
    }

    /*
     * Purpose: Confirm the error reports the physical line number, counting skipped blank lines.
     */
    [TestMethod]
    public void Constructor_InvalidValueAfterBlankLines_ReportsPhysicalLineNumber()
    {
        string path = WriteDataFile("10", "", "20", "   ", "bad");

        try
        {
            new LoopingFileReader(path);
            Assert.Fail("Expected exception");
        }
        catch (FormatException ex)
        {
            StringAssert.Contains(ex.Message, "Line 5 of");
        }
    }

    /*
     * Purpose: Confirm a completely empty file is rejected.
     */
    [TestMethod]
    public void Constructor_EmptyFile_ThrowsInvalidDataException()
    {
        string path = WriteDataFile();

        Assert.IsTrue(Throws<InvalidDataException>(() => new LoopingFileReader(path)));
    }

    /*
     * Purpose: Confirm a file with only blank or whitespace lines is rejected.
     */
    [TestMethod]
    public void Constructor_WhitespaceOnlyFile_ThrowsInvalidDataException()
    {
        string path = WriteDataFile("", "   ", "\t");

        Assert.IsTrue(Throws<InvalidDataException>(() => new LoopingFileReader(path)));
    }

    /*
     * Purpose: Confirm a missing file raises the normal .NET file exception.
     */
    [TestMethod]
    public void Constructor_MissingFile_ThrowsFileNotFoundException()
    {
        string path = Path.Combine(_tempDirectory, "missing.txt");

        Assert.IsTrue(Throws<FileNotFoundException>(() => new LoopingFileReader(path)));
    }

    /*
     * Purpose: Confirm a null, empty or whitespace path is rejected.
     */
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_BlankPath_ThrowsArgumentException(string? path)
    {
        Assert.IsTrue(Throws<ArgumentException>(() => new LoopingFileReader(path!)));
    }

    /*
     * Purpose: Confirm the file is closed after construction and the values stay loaded.
     */
    [TestMethod]
    public void Constructor_AfterLoading_ReleasesFileAndKeepsValues()
    {
        string path = WriteDataFile("10", "20");
        var reader = new LoopingFileReader(path);

        // Opening with FileShare.None fails on Windows if any handle is still open.
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        File.Delete(path);

        Assert.IsFalse(File.Exists(path));
        CollectionAssert.AreEqual(new[] { 10.0, 20.0, 10.0 }, ReadValues(reader, 3));
    }

    /*
     * Purpose: Write the given lines to a data file in this test's folder and return its path.
     */
    private string WriteDataFile(params string[] lines)
    {
        string path = Path.Combine(_tempDirectory, "data.txt");
        File.WriteAllLines(path, lines);
        return path;
    }

    /*
     * Purpose: Read the given number of values from the reader.
     */
    private static double[] ReadValues(LoopingFileReader reader, int count)
    {
        var values = new double[count];
        for (int i = 0; i < count; i++)
            values[i] = reader.ReadNext();
        return values;
    }

    // Same helper as in CoreTests.cs so tests work on any MSTest version.
    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }
}