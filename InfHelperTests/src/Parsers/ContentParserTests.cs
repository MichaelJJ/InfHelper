﻿using System;
using System.Collections.Generic;
using System.Linq;
using InfHelper.Models;
using InfHelper.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfHelperTests.Parsers
{
    [TestClass()]
    public class ContentParserTests
    {
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
        public void MultipleCategoryParsing()
        {
            int CATEGORIES_COUNT = new Random().Next(1,10);
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
                Assert.IsTrue(firstCategory.Keys[i].KeyValues.First().Value == $"Value{i}" );
            }
        }
    }
}