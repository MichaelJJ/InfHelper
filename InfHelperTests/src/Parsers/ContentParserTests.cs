﻿using InfHelper.Exceptions;
using InfHelper.Models;
using InfHelper.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace InfHelperTests.Parsers
{
    [TestClass()]
    public class ContentParserTests
    {
        private const string testFolder = "infs";

        [TestMethod()]
        public void CategoryParsing()
        {
            string test = "[CATEGORY]";
            var categories = new List<Category>();
            var parser = new ContentParser();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);

            parser.Parse(test);

            Assert.IsTrue(categories.Count == 1 && categories.First().Name == "CATEGORY");
        }

        [TestMethod()]
        public void CategoryWithSlashParsing()
        {
            string test = "[CATEGOR\\Y]";
            var categories = new List<Category>();
            var parser = new ContentParser();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);

            parser.Parse(test);

            Assert.IsTrue(categories.Count == 1 && categories.First().Name == "CATEGOR\\Y");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidTokenException), @"'\' are not allowed as the last token in a Category")]
        public void CategoryWithSlashAsLastCharacterParsing()
        {
            string test = "[CATEGORY\\]";
            var categories = new List<Category>();
            var parser = new ContentParser();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);

            parser.Parse(test);
        }

        [TestMethod()]
        public void MultipleCategoryParsing()
        {
            int CATEGORIES_COUNT = new Random().Next(1, 10);
            var testCategories = new List<string>();

            for (int i = 1; i <= CATEGORIES_COUNT; i++)
            {
                testCategories.Add($"[CATEGORY{i}]");
            }

            string test = string.Join(" \n ", testCategories);

            var categories = new List<Category>();
            var parser = new ContentParser();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);

            parser.Parse(test);

            Assert.IsTrue(categories.Count == CATEGORIES_COUNT);

            for (var i = 1; i <= CATEGORIES_COUNT; i++)
            {
                Assert.IsTrue(categories.Any(x => x.Name == $"CATEGORY{i}"));
            }
        }

        [TestMethod()]
        public void SimpleCategoryWithSimpleKey()
        {
            string formula = "[Category] \n Key = Value";
            var parser = new ContentParser();

            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(formula);

            Assert.IsTrue(categories.First().Name == "Category");
            var key = categories.First().Keys.First();

            Assert.IsTrue(string.Equals(key.Id, "Key", StringComparison.Ordinal));
            Assert.IsTrue(key.KeyValues.Count == 1);
            Assert.IsTrue(string.Equals(key.KeyValues[0].Value, "Value", StringComparison.Ordinal));
        }

        [TestMethod()]
        public void SimpleCategoryWithMultipleSimpleKeys()
        {
            string formula = "[Category]";// \n Key = Value \n Key1 = Value1 \n Key2 = Value2 \n Key3 = Value3";
            int Keys_Count = 4;
            for (int i = 0; i < Keys_Count; i++)
            {
                formula = string.Concat(formula, $" \n Key{i} = Value{i}");
            }
            var parser = new ContentParser();

            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(formula);

            var firstCategory = categories.First();

            Assert.IsTrue(firstCategory.Name == "Category");
            Assert.IsTrue(firstCategory.Keys.Count == 4);

            for (var i = 0; i < Keys_Count; i++)
            {
                Assert.IsTrue(firstCategory.Keys[i].Id == $"Key{i}");
                Assert.IsTrue(firstCategory.Keys[i].KeyValues.First().Value == $"Value{i}");
            }
        }

        [TestMethod()]
        public void SimpleCategoryWithMultipleSimpleKeysReal()
        {
            string formula =
                "[Install_MPCIEX_GENM2_D_REV_59_7265_BGN_2x2_HMC_WINT_64_BGN15.Services]\r\nInclude         = netvwifibus.inf\r\nNeeds           = VWiFiBus.Services";

            var parser = new ContentParser();

            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(formula);

            var firstCategory = categories.First();

            Assert.IsTrue(firstCategory.Name == "Install_MPCIEX_GENM2_D_REV_59_7265_BGN_2x2_HMC_WINT_64_BGN15.Services");

            Assert.AreEqual(firstCategory.Keys[0].Id, "Include");
            Assert.AreEqual(firstCategory.Keys[1].Id, "Needs");

            Assert.AreEqual(firstCategory.Keys[0].KeyValues.First().Value, "netvwifibus.inf");
            Assert.AreEqual(firstCategory.Keys[1].KeyValues.First().Value, "VWiFiBus.Services");
        }

        [TestMethod()]
        public void SimpleCategoryWithOneMultiValueKey()
        {
            string formula =
                "[Install_MPCIEX_GENM2_D_REV_61_7265_BGN_2x2_HMC_WINT_64_BGN15.Services] \r\n AddService     = Netwtw04, 2, NIC_Service_WINT_64, Common_EventLog_WINT_64";

            var parser = new ContentParser();

            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(formula);

            var firstCategory = categories.First();

            Assert.AreEqual(firstCategory.Name, "Install_MPCIEX_GENM2_D_REV_61_7265_BGN_2x2_HMC_WINT_64_BGN15.Services");
            Assert.AreEqual(firstCategory.Keys[0].Id, "AddService");
            var values = firstCategory.Keys.First().KeyValues;

            Assert.AreEqual(values[0].Value, "Netwtw04");
            Assert.AreEqual(values[1].Value, "2");
            Assert.AreEqual(values[2].Value, "NIC_Service_WINT_64");
            Assert.AreEqual(values[3].Value, "Common_EventLog_WINT_64");
        }

        [TestMethod()]
        public void SimpleCategoryWithMultipleMultiValueKeys()
        {
            string formula =
                "[Intel.NTAMD64.6.2]\r\n%IntcAudDeviceDesc% = IntcAudModel, HDAUDIO\\FUNC_01&VEN_8086&DEV_2809&SUBSYS_80860101, HDAUDIO\\FUNC_01&VEN_8086&DEV_2809\r\n%IntcAudDeviceDesc% = IntcAudModel, INTELAUDIO\\FUNC_01&VEN_8086&DEV_2809&SUBSYS_80860101, INTELAUDIO\\FUNC_01&VEN_8086&DEV_2809\r\n%IntcAudDeviceDesc% = IntcAudModel, HDAUDIO\\FUNC_01&VEN_8086&DEV_280A&SUBSYS_80860101, HDAUDIO\\FUNC_01&VEN_8086&DEV_280A";

            var parser = new ContentParser();

            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(formula);

            var firstCategory = categories.First();

            Assert.AreEqual(firstCategory.Name, "Intel.NTAMD64.6.2");
            Assert.IsTrue(firstCategory.Keys.Count == 3);

            //first key
            var key = firstCategory.Keys[0];
            Assert.AreEqual(key.Id, "%IntcAudDeviceDesc%");
            Assert.AreEqual(key.KeyValues[0].Value, "IntcAudModel");
            Assert.AreEqual(key.KeyValues[1].Value, "HDAUDIO\\FUNC_01&VEN_8086&DEV_2809&SUBSYS_80860101");
            Assert.AreEqual(key.KeyValues[2].Value, "HDAUDIO\\FUNC_01&VEN_8086&DEV_2809");

            //second key
            key = firstCategory.Keys[1];
            Assert.AreEqual(key.Id, "%IntcAudDeviceDesc%");
            Assert.AreEqual(key.KeyValues[0].Value, "IntcAudModel");
            Assert.AreEqual(key.KeyValues[1].Value, "INTELAUDIO\\FUNC_01&VEN_8086&DEV_2809&SUBSYS_80860101");
            Assert.AreEqual(key.KeyValues[2].Value, "INTELAUDIO\\FUNC_01&VEN_8086&DEV_2809");

            //third key
            key = firstCategory.Keys[2];
            Assert.AreEqual(key.Id, "%IntcAudDeviceDesc%");
            Assert.AreEqual(key.KeyValues[0].Value, "IntcAudModel");
            Assert.AreEqual(key.KeyValues[1].Value, "HDAUDIO\\FUNC_01&VEN_8086&DEV_280A&SUBSYS_80860101");
            Assert.AreEqual(key.KeyValues[2].Value, "HDAUDIO\\FUNC_01&VEN_8086&DEV_280A");
        }

        [TestMethod]
        public void RealLifeFileTest()
        {
            var sw = new Stopwatch();
            var files = Directory.GetFiles(testFolder);
            var parser = new ContentParser();
            foreach (var file in files)
            {
                sw.Reset();
                Trace.WriteLine("Loading file content: " + file);
                var content = File.ReadAllText(file);
                Trace.WriteLine("Parsing file: " + file);
                sw.Start();
                parser.Parse(content);
                sw.Stop();
                Trace.WriteLine($"Completed. Elapsed time: {sw.Elapsed}");
            }
        }
        [TestMethod]
        public void RealLifeFileTestDeep()
        {
            var content = File.ReadAllText(Path.Combine(testFolder, "oem100.inf"));
            var parser = new ContentParser();
            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(content);

            //Check categories names
            Assert.AreEqual(categories[0].Name, "Version");
            Assert.AreEqual(categories[1].Name, "DestinationDirs");
            Assert.AreEqual(categories[2].Name, "Manufacturer");
            Assert.AreEqual(categories[3].Name, "Standard");
            Assert.AreEqual(categories[4].Name, "Standard.NTAMD64");
            Assert.AreEqual(categories[5].Name, "Razer.NTx86");
            Assert.AreEqual(categories[6].Name, "Razer.NTAMD64");
            Assert.AreEqual(categories[7].Name, "Razer.NTAMD64.HW");
            Assert.AreEqual(categories[8].Name, "Razer.NTAMD64.Services");
            Assert.AreEqual(categories[9].Name, "SourceDisksNames");
            Assert.AreEqual(categories[10].Name, "SourceDisksFiles");
            Assert.AreEqual(categories[11].Name, "Razer.NTAMD64.CoInstallers");
            Assert.AreEqual(categories[12].Name, "Razer_CoInstaller_AddReg");
            Assert.AreEqual(categories[13].Name, "Razer_CoInstaller_CopyFiles");
            Assert.AreEqual(categories[14].Name, "Razer_Installer_CopyFiles");
            Assert.AreEqual(categories[15].Name, "Razer_Installer_CopyFilesWOW64");
            Assert.AreEqual(categories[16].Name, "Strings");
        }

        [TestMethod]
        public void SourceDiskFileWithSpaces()
        {
            var content = File.ReadAllText(Path.Combine(testFolder, "sourcediskspaces.inf"));
            var parser = new ContentParser();
            var categories = new List<Category>();
            parser.CategoryDiscovered += (sender, category) => categories.Add(category);
            parser.Parse(content);

            var sourceDiskFiles = categories.Where(c => c.Name == "SourceDisksFiles").ToList();
            Assert.AreEqual(sourceDiskFiles[0].Keys[0].Id, "Dep With Space.txt");
            Assert.AreEqual(sourceDiskFiles[0].Keys[1].Id, "file2.txt");
        }
    }
}