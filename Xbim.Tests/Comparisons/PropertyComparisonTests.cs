using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Analysis.Comparing;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    public class PropertyComparisonTests
    {
        [TestMethod]
        public void PropertyComparisonIdenticalTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            var createProperties = new Action<XbimModel>(model => {
                using (var txn = model.BeginTransaction())
                {
                    var cols = model.Instances.OfType<IfcColumn>().ToList();
                    var first = cols[0];
                    var second = cols[1];

                    first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                    first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                    first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                    first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                    first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                    first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                    second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                    second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                    second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                    second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                    second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                    second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                    txn.Commit();
                }
            });

            createProperties(baseline);
            createProperties(revision);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(0, result.Added.Count());
            Assert.AreEqual(0, result.Deleted.Count());
            Assert.AreEqual(2, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonDifPsetNameTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.3", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.3", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.3", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }


            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonDifPropNameTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsOnStock", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }


            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonAlternativePropTypeTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcIdentifier("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcLogical(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }


            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(0, result.Added.Count());
            Assert.AreEqual(0, result.Deleted.Count());
            Assert.AreEqual(2, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonDifPropValueStringTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Batman"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }


            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonDifPropValueNumberTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.365));
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(12.366)); //here is the change
                first.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(10.254));
                first.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Bob the builder"));
                first.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("N45.65.c45"));
                first.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(true));
                first.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical());

                second.SetPropertySingleValue("Set No.1", "Fire resistance", new IfcTimeMeasure(18.365));
                second.SetPropertySingleValue("Set No.1", "Acustic rating", new IfcNumericMeasure(80.674));
                second.SetPropertySingleValue("Set No.1", "Manufacturer", new IfcLabel("Joker"));
                second.SetPropertySingleValue("Set No.2", "Identifier", new IfcIdentifier("S45_78_96"));
                second.SetPropertySingleValue("Set No.2", "IsAvailable", new IfcBoolean(false));
                second.SetPropertySingleValue("Set No.2", "IsConfirmed", new IfcLogical(true));

                txn.Commit();
            }


            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonIdenticalTablesTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            var createProperties = new Action<XbimModel>(model =>
            {
                using (var txn = model.BeginTransaction())
                {
                    var cols = model.Instances.OfType<IfcColumn>().ToList();
                    var first = cols[0];
                    var second = cols[1];

                    first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.654));
                    first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(13.654));
                    first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(16.654));

                    second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.954));
                    second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(52.654));
                    second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(15.654));

                    txn.Commit();
                }
            });

            createProperties(baseline);
            createProperties(revision);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(0, result.Added.Count());
            Assert.AreEqual(0, result.Deleted.Count());
            Assert.AreEqual(2, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonTablesDiffValuesTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(13.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(16.654));

                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.954));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(52.654));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(15.654));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(13.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(18.654)); //changed value

                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.954));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(52.654));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(15.654));

                txn.Commit();
            }

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }

        [TestMethod]
        public void PropertyComparisonTablesDiffNamesTest()
        {
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            using (var txn = baseline.BeginTransaction())
            {
                var cols = baseline.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(13.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(16.654));

                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.954));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(52.654));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(15.654));

                txn.Commit();
            }

            using (var txn = revision.BeginTransaction())
            {
                var cols = revision.Instances.OfType<IfcColumn>().ToList();
                var first = cols[0];
                var second = cols[1];

                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Length"), new IfcLengthMeasure(12.654));//changed name
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(13.654));
                first.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(16.654)); 

                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Width"), new IfcLengthMeasure(12.954));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Height"), new IfcLengthMeasure(52.654));
                second.SetPropertyTableItemValue("Set No.1", "Dimensions", new IfcLabel("Depth"), new IfcLengthMeasure(15.654));

                txn.Commit();
            }

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new PropertyComparer(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            Assert.AreEqual(1, result.Added.Count());
            Assert.AreEqual(1, result.Deleted.Count());
            Assert.AreEqual(1, result.MatchOneToOne.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
        }
    }
}
