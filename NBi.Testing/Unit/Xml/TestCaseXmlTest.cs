﻿using System.IO;
using System.Reflection;
using NBi.NUnit;
using NBi.Xml.Systems;
using NUnit.Framework;

namespace NBi.Testing.Unit.Xml
{
    [TestFixture]
    public class TestCaseXmlTest
    {

        protected string ConnectionString
        {
            get
            {
                //If available use the user file
                if (System.IO.File.Exists("ConnectionString.user.config"))
                {
                    return System.IO.File.ReadAllText("ConnectionString.user.config");
                }
                else if (System.IO.File.Exists("ConnectionString.config"))
                {
                    return System.IO.File.ReadAllText("ConnectionString.config");
                }

                return null;
            }
        }

        #region Setup & Teardown

        [SetUp]
        public void SetUp()
        {
           
        }

        [TearDown]
        public void TearDown()
        {
        }

        #endregion

        [Test]
        public void Query_FilenameSpecified_RetrieveContentOfFile()
        {
            //create a text file on disk
            var filename = DiskOnFile.CreatePhysicalFile("QueryFile.sql", "NBi.Testing.Unit.Xml.Resources.QueryFile.sql");
           
            //Instantiate a Test Case and specify to find the sql in the fie created above
            var testCase = new QueryXml() { Filename = filename };

            // A Stream is needed to read the text file from the assembly.
            string expectedContent;
            using (Stream stream = Assembly.GetExecutingAssembly()
                                           .GetManifestResourceStream("NBi.Testing.Unit.Xml.Resources.QueryFile.sql"))
                using (StreamReader reader = new StreamReader(stream))
                   expectedContent = reader.ReadToEnd();
            
            Assert.AreEqual(expectedContent, testCase.Query);
        }

        [Test]
        public void Query_FilenameNotSpecified_RetrieveContentOfInlineQuery()
        {
            //Instantiate a System Under Test
            var systemUnderTest = new QueryXml() { InlineQuery = "SELECT * FROM Product" };

            Assert.AreEqual("SELECT * FROM Product", systemUnderTest.Query);
        }
       
    }
}