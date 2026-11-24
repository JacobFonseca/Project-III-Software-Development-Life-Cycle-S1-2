using HydroFlow.Core;
using HydroFlow.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hydroflow.Tests;

[TestClass]
public class AppSettingsTests
{
    private static string TempFile(string text)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, text);
        return path;
    }

    [TestMethod]
    public void MissingFile_ReturnsDefault()
    {
        string path = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".txt");
        Assert.AreEqual(1.0, AppSettings.ReadPollingSeconds(path), 0.0001);
    }

    [TestMethod]
    public void ValidValue_IsUsed()
    {
        Assert.AreEqual(0.5, AppSettings.ReadPollingSeconds(TempFile("0.5")), 0.0001);
    }

    [TestMethod]
    public void Garbage_ReturnsDefault()
    {
        Assert.AreEqual(1.0, AppSettings.ReadPollingSeconds(TempFile("abc")), 0.0001);
    }

    [TestMethod]
    public void TooLarge_ReturnsDefault()
    {
        Assert.AreEqual(1.0, AppSettings.ReadPollingSeconds(TempFile("10")), 0.0001);
    }

    [TestMethod]
    public void EmptyFile_ReturnsDefault()
    {
        Assert.AreEqual(1.0, AppSettings.ReadPollingSeconds(TempFile("")), 0.0001);
    }
}