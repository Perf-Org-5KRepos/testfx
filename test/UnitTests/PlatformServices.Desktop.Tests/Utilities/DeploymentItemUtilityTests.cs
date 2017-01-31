// Copyright (c) Microsoft. All rights reserved.

namespace MSTestAdapter.PlatformServices.Desktop.UnitTests.Utilities
{
    extern alias FrameworkV1;
    extern alias FrameworkV2;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices;
    using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Deployment;
    using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using Moq;

    using Assert = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
    using CollectionAssert = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
    using StringAssert = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.StringAssert;
    using TestClass = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
    using TestFrameworkV2 = FrameworkV2::Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestInitialize = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute;
    using TestMethod = FrameworkV1::Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;

    [TestClass]
    public class DeploymentItemUtilityTests
    {
        private Mock<ReflectionUtility> mockReflectionUtility;
        private DeploymentItemUtility deploymentItemUtility;
        private ICollection<string> warnings;

        private string defaultDeploymentItemPath = @"c:\temp";
        private string defaultDeploymentItemOutputDirectory = "out";

        internal static TestProperty DeploymentItemsProperty = TestProperty.Register(
            "MSTestDiscoverer2.DeploymentItems",
            "DeploymentItems",
            typeof(KeyValuePair<string, string>[]),
            TestPropertyAttributes.Hidden,
            typeof(TestCase));

        [TestInitialize]
        public void TestInit()
        {
            this.mockReflectionUtility = new Mock<ReflectionUtility>();
            this.deploymentItemUtility = new DeploymentItemUtility(this.mockReflectionUtility.Object);
            this.warnings = new List<string>();
        }

        #region GetClassLevelDeploymentItems tests

        [TestMethod]
        public void GetClassLevelDeploymentItemsShouldReturnEmptyListWhenNoDeploymentItems()
        {
            var deploymentItems = this.deploymentItemUtility.GetClassLevelDeploymentItems(typeof(DeploymentItemUtilityTests), this.warnings);

            Assert.IsNotNull(deploymentItems);
            Assert.AreEqual(0, deploymentItems.Count);
        }

        [TestMethod]
        public void GetClassLevelDeploymentItemsShouldReturnADeploymentItem()
        {
            this.SetupDeploymentItems(
                typeof(DeploymentItemUtilityTests),
                new[]
                    {
                        new KeyValuePair<string, string>(
                            this.defaultDeploymentItemPath,
                            this.defaultDeploymentItemOutputDirectory)
                    });

            var deploymentItems = this.deploymentItemUtility.GetClassLevelDeploymentItems(typeof(DeploymentItemUtilityTests), this.warnings);
            var expectedDeploymentItems = new DeploymentItem[]
                                              {
                                                  new DeploymentItem(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };
            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetClassLevelDeploymentItemsShouldReturnMoreThanOneDeploymentItems()
        {
            var deploymentItemAttributes = new[]
                                               {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory),
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath + "\\temp2",
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            this.SetupDeploymentItems(typeof(DeploymentItemUtilityTests), deploymentItemAttributes);

            var deploymentItems =
                this.deploymentItemUtility.GetClassLevelDeploymentItems(
                    typeof(DeploymentItemUtilityTests),
                    this.warnings);

            var expectedDeploymentItems = new DeploymentItem[]
                                              {
                                                  new DeploymentItem(
                                                      deploymentItemAttributes[0].Key,
                                                      deploymentItemAttributes[0].Value),
                                                  new DeploymentItem(
                                                      deploymentItemAttributes[1].Key,
                                                      deploymentItemAttributes[1].Value)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetClassLevelDeploymentItemsShouldNotReturnDuplicateDeploymentItemEntries()
        {
            var deploymentItemAttributes = new[]
                                               {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory),
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            this.SetupDeploymentItems(typeof(DeploymentItemUtilityTests), deploymentItemAttributes);

            var deploymentItems =
                this.deploymentItemUtility.GetClassLevelDeploymentItems(
                    typeof(DeploymentItemUtilityTests),
                    this.warnings);

            var expectedDeploymentItems = new[]
                                              {
                                                  new DeploymentItem(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetClassLevelDeploymentItemsShouldReportWarningsForInvalidDeploymentItems()
        {
            var deploymentItemAttributes = new[]
                                               {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory),
                                                   new KeyValuePair<string, string>(
                                                       null,
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            this.SetupDeploymentItems(typeof(DeploymentItemUtilityTests), deploymentItemAttributes);
            
            var deploymentItems = this.deploymentItemUtility.GetClassLevelDeploymentItems(typeof(DeploymentItemUtilityTests), this.warnings);

            var expectedDeploymentItems = new DeploymentItem[]
                                              {
                                                  new DeploymentItem(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
            Assert.AreEqual(1, this.warnings.Count);
            TestFrameworkV2.StringAssert.Contains(this.warnings.ToArray()[0], Resource.DeploymentItemPathCannotBeNullOrEmpty);
        }

#endregion

        #region GetDeploymentItems tests

        [TestMethod]
        public void GetDeploymentItemsShouldReturnNullOnNoDeploymentItems()
        {
            Assert.IsNull(this.deploymentItemUtility.GetDeploymentItems(
                typeof(DeploymentItemUtilityTests).GetMethod("GetDeploymentItemsShouldReturnNullOnNoDeploymentItems"),
                null,
                this.warnings));
        }

        [TestMethod]
        public void GetDeploymentItemsShouldReturnMethodLevelDeploymentItemsOnly()
        {
            var deploymentItemAttributes = new[]
                                                  {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory),
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath + "\\temp2",
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            var memberInfo =
                typeof(DeploymentItemUtilityTests).GetMethod(
                    "GetDeploymentItemsShouldReturnNullOnNoDeploymentItems");

            this.SetupDeploymentItems(memberInfo, deploymentItemAttributes);

            var deploymentItems = this.deploymentItemUtility.GetDeploymentItems(
                memberInfo,
                null,
                this.warnings);
            
            CollectionAssert.AreEqual(deploymentItemAttributes, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetDeploymentItemsShouldReturnClassLevelDeploymentItemsOnly()
        {
            // Arrange.
            var classLevelDeploymentItems = new DeploymentItem[]
                                                {
                                                    new DeploymentItem(
                                                        this.defaultDeploymentItemPath,
                                                        this.defaultDeploymentItemOutputDirectory),
                                                    new DeploymentItem(
                                                        this.defaultDeploymentItemPath + "\\temp2",
                                                        this.defaultDeploymentItemOutputDirectory)
                                                };

            // Act.
            var deploymentItems = this.deploymentItemUtility.GetDeploymentItems(
                typeof(DeploymentItemUtilityTests).GetMethod("GetDeploymentItemsShouldReturnNullOnNoDeploymentItems"),
                classLevelDeploymentItems,
                this.warnings);

            // Assert.
            var expectedDeploymentItems = new[]
                                              {
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory),
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath + "\\temp2",
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetDeploymentItemsShouldReturnClassAndMethodLevelDeploymentItems()
        {
            // Arrange.
            var deploymentItemAttributes = new[]
                                                  {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            var memberInfo =
                typeof(DeploymentItemUtilityTests).GetMethod(
                    "GetDeploymentItemsShouldReturnClassAndMethodLevelDeploymentItems");
            this.SetupDeploymentItems(memberInfo, deploymentItemAttributes);

            var classLevelDeploymentItems = new[]
                                                {
                                                    new DeploymentItem(
                                                        this.defaultDeploymentItemPath + "\\temp2",
                                                        this.defaultDeploymentItemOutputDirectory)
                                                };
            
            // Act.
            var deploymentItems = this.deploymentItemUtility.GetDeploymentItems(
                memberInfo,
                classLevelDeploymentItems,
                this.warnings);

            // Assert.
            var expectedDeploymentItems = new KeyValuePair<string, string>[]
                                              {
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory),
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath + "\\temp2",
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        [TestMethod]
        public void GetDeploymentItemsShouldReturnClassAndMethodLevelDeploymentItemsWithoutDuplicates()
        {
            // Arrange.
            var deploymentItemAttributes = new[]
                                                  {
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath,
                                                       this.defaultDeploymentItemOutputDirectory),
                                                   new KeyValuePair<string, string>(
                                                       this.defaultDeploymentItemPath + "\\temp2",
                                                       this.defaultDeploymentItemOutputDirectory)
                                               };
            var memberInfo =
                typeof(DeploymentItemUtilityTests).GetMethod(
                    "GetDeploymentItemsShouldReturnClassAndMethodLevelDeploymentItems");
            this.SetupDeploymentItems(memberInfo, deploymentItemAttributes);

            var classLevelDeploymentItems = new DeploymentItem[]
                                                {
                                                    new DeploymentItem(
                                                        this.defaultDeploymentItemPath,
                                                        this.defaultDeploymentItemOutputDirectory),
                                                    new DeploymentItem(
                                                        this.defaultDeploymentItemPath + "\\temp1",
                                                        this.defaultDeploymentItemOutputDirectory)
                                                };
            
            // Act.
            var deploymentItems = this.deploymentItemUtility.GetDeploymentItems(
                memberInfo,
                classLevelDeploymentItems,
                this.warnings);

            // Assert.
            var expectedDeploymentItems = new KeyValuePair<string, string>[]
                                              {
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath,
                                                      this.defaultDeploymentItemOutputDirectory),
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath + "\\temp2",
                                                      this.defaultDeploymentItemOutputDirectory),
                                                  new KeyValuePair<string, string>(
                                                      this.defaultDeploymentItemPath + "\\temp1",
                                                      this.defaultDeploymentItemOutputDirectory)
                                              };

            CollectionAssert.AreEqual(expectedDeploymentItems, deploymentItems.ToArray());
        }

        #endregion

        #region IsValidDeploymentItem tests

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfSourcePathIsNull()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem(null, this.defaultDeploymentItemOutputDirectory, out warning));

            StringAssert.Contains(Resource.DeploymentItemPathCannotBeNullOrEmpty, warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfSourcePathIsEmpty()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem(string.Empty, this.defaultDeploymentItemOutputDirectory, out warning));

            StringAssert.Contains(Resource.DeploymentItemPathCannotBeNullOrEmpty, warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfDeploymentOutputDirectoryIsNull()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem(this.defaultDeploymentItemPath, null, out warning));

            StringAssert.Contains(Resource.DeploymentItemOutputDirectoryCannotBeNull, warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfSourcePathHasInvalidCharacters()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem("C:<>", this.defaultDeploymentItemOutputDirectory, out warning));

            StringAssert.Contains(
                string.Format(
                    Resource.DeploymentItemContainsInvalidCharacters,
                    "C:<>",
                    this.defaultDeploymentItemOutputDirectory),
                warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfOutputDirectoryHasInvalidCharacters()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem(this.defaultDeploymentItemPath, "<>", out warning));

            StringAssert.Contains(
                string.Format(
                    Resource.DeploymentItemContainsInvalidCharacters,
                    this.defaultDeploymentItemPath,
                    "<>"),
                warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReportWarningIfDeploymentOutputDirectoryIsRooted()
        {
            string warning;
            Assert.IsFalse(this.deploymentItemUtility.IsValidDeploymentItem(this.defaultDeploymentItemPath, "C:\\temp", out warning));

            StringAssert.Contains(
               string.Format(
                   Resource.DeploymentItemOutputDirectoryMustBeRelative,
                   "C:\\temp"),
               warning);
        }

        [TestMethod]
        public void IsValidDeploymentItemShouldReturnTrueForAValidDeploymentItem()
        {
            string warning;
            Assert.IsTrue(this.deploymentItemUtility.IsValidDeploymentItem(this.defaultDeploymentItemPath, this.defaultDeploymentItemOutputDirectory, out warning));

            Assert.IsTrue(string.Empty.Equals(warning));
        }
        #endregion

        #region HasDeployItems tests

        [TestMethod]
        public void HasDeployItemsShouldReturnFalseForNoDeploymentItems()
        {
            TestCase testCase = new TestCase("A.C.M", new System.Uri("executor://testExecutor"), "A");
            testCase.SetPropertyValue(DeploymentItemsProperty, null);

            Assert.IsFalse(this.deploymentItemUtility.HasDeploymentItems(testCase));
        }

        [TestMethod]
        public void HasDeployItemsShouldReturnTrueWhenDeploymentItemsArePresent()
        {
            TestCase testCase = new TestCase("A.C.M", new System.Uri("executor://testExecutor"), "A");
            testCase.SetPropertyValue(
                DeploymentItemsProperty,
                new[]
                    {
                        new KeyValuePair<string, string>(
                            this.defaultDeploymentItemPath,
                            this.defaultDeploymentItemOutputDirectory)
                    });

            Assert.IsTrue(this.deploymentItemUtility.HasDeploymentItems(testCase));
        }

        #endregion

        #region private methods

        private void SetupDeploymentItems(MemberInfo memberInfo, KeyValuePair<string, string>[] deploymentItems)
        {
            var deploymentItemAttributes = new List<TestFrameworkV2.DeploymentItemAttribute>();

            foreach (var deploymentItem in deploymentItems)
            {
                deploymentItemAttributes.Add(new TestFrameworkV2.DeploymentItemAttribute(deploymentItem.Key, deploymentItem.Value));
            }

            this.mockReflectionUtility.Setup(
                ru =>
                ru.GetCustomAttributes(
                    memberInfo,
                    typeof(TestFrameworkV2.DeploymentItemAttribute))).Returns((object[])deploymentItemAttributes.ToArray());
        }

        #endregion
    }
}