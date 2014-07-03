﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using NBi.Xml.Constraints;
using NBi.Xml.Decoration.Command;

namespace NBi.Xml
{
    public class XmlManager
    {
        public virtual TestSuiteXml TestSuite { get; protected set; }
        public virtual NameValueCollection ConnectionStrings { get; set; }
        protected bool isValid;

        public XmlManager()
        {
            docXml = new XmlDocument();
            ConnectionStrings = new NameValueCollection();
        }

        public virtual void Load(string filename)
        {
            Load(filename, false);
        }

        public virtual void Load(string filename, bool isDtdProcessing)
        {
            if (!this.Validate(filename, isDtdProcessing))
                throw new ArgumentException("The test suite is not valid. Check with the XSD");

            //Create the XmlReaderSettings
            var settings = BuildXmlReaderSettings(isDtdProcessing);
            // Create the XmlReader object.
            using (var xmlReader = XmlReader.Create(filename, settings))
                Read(xmlReader);

            //Apply Settings hacks
            var basePath = System.IO.Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar;
            TestSuite.Settings.BasePath = basePath;
            TestSuite.Settings.GetValuesFromConfig(ConnectionStrings);

            using (var xmlReader = XmlReader.Create(filename, settings))
                docXml.Load(xmlReader);
            ReassignXml();
        }

        private readonly XmlDocument docXml;
        public void Read(StreamReader reader)
        {
            //Add the attributes that should only be used during read phase
            //These attributes are kept for compatibility with previous versions
            //They should never been used during write process
            var attrs = new SpecificReadAttributes();
            attrs.Build();

            // Create an instance of the XmlSerializer specifying type and read-attributes.
            XmlSerializer serializer = new XmlSerializer(typeof(TestSuiteXml), attrs);


            using (reader)
            {
                // Use the Deserialize method to restore the object's state.
                TestSuite = (TestSuiteXml)serializer.Deserialize(reader);
            }

            //Apply defaults
            foreach (var test in TestSuite.GetAllTests())
                ApplyDefaultSettings(test);

        }

        public void Read(XmlReader reader)
        {
            //Add the attributes that should only be used during read phase
            //These attributes are kept for compatibility with previous versions
            //They should never been used during write process
            var attrs = new SpecificReadAttributes();
            attrs.Build();

            // Create an instance of the XmlSerializer specifying type and read-attributes.
            XmlSerializer serializer = new XmlSerializer(typeof(TestSuiteXml), attrs);


            using (reader)
            {
                // Use the Deserialize method to restore the object's state.
                TestSuite = (TestSuiteXml)serializer.Deserialize(reader);
            }

            //Apply defaults
            foreach (var test in TestSuite.GetAllTests())
                ApplyDefaultSettings(test);

        }

        private void ApplyDefaultSettings(TestXml test)
        {
            foreach (var sut in test.Systems)
            {
                sut.Default = TestSuite.Settings.GetDefault(Settings.SettingsXml.DefaultScope.SystemUnderTest);
                sut.Settings = TestSuite.Settings;
            }
            foreach (var ctr in test.Constraints)
            {
                ctr.Default = TestSuite.Settings.GetDefault(Settings.SettingsXml.DefaultScope.Assert);
                ctr.Settings = TestSuite.Settings;
                if (ctr is IReferenceFriendly && TestSuite.Settings != null)
                    ((IReferenceFriendly)ctr).AssignReferences(TestSuite.Settings.References);
            }

            var decorationCommands = new List<DecorationCommandXml>();
            decorationCommands.AddRange(test.Setup.Commands);
            decorationCommands.AddRange(test.Cleanup.Commands);
            foreach (var cmd in decorationCommands)
            {
                cmd.Settings = TestSuite.Settings;
            }
        }

        protected internal void ReassignXml()
        {
            //Get the Xml content of the tests define in the testSuite
            var testNodes = docXml.GetElementsByTagName("test");
            for (int i = 0; i < TestSuite.Tests.Count; i++)
            {
                //Add indentation and line breaks
                var nodeXml = new XmlDocument();
                nodeXml.LoadXml(testNodes[i].OuterXml);
                var content = XmlBeautifier.Beautify(nodeXml);
                //Add the content to the test (Used for StackTrace)
                TestSuite.Tests[i].Content = content;
            }
        }


        public void Persist(string filename, TestSuiteXml testSuite)
        {
            // Create an instance of the XmlSerializer specifying type and namespace.
            var serializer = new XmlSerializer(typeof(TestSuiteXml));

            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                // Use the Serialize method to store the object's state.
                serializer.Serialize(writer, testSuite);
            }
            //Debug.Write(XmlSerializeFrom<TestSuiteXml>(testSuite));
        }

        public TestXml DeserializeTest(string objectData)
        {
            return XmlDeserializeFromString<TestXml>(objectData);
        }

        protected internal T XmlDeserializeFromString<T>(string objectData)
        {
            return (T)XmlDeserializeFromString(objectData, typeof(T));
        }

        protected object XmlDeserializeFromString(string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }

        public string SerializeTest(TestXml objectData)
        {
            return XmlSerializeFrom<TestXml>(objectData);
        }

        protected internal string XmlSerializeFrom<T>(T objectData)
        {
            return SerializeFrom(objectData, typeof(T));
        }

        protected string SerializeFrom(object objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            var result = string.Empty;
            using (var writer = new StringWriter())
            {
                // Use the Serialize method to store the object's state.
                serializer.Serialize(writer, objectData);
                result = writer.ToString();
            }
            return result;
        }

        protected bool Validate(string filename, bool isDtdProcessing)
        {
            var settings = BuildXmlReaderSettings(isDtdProcessing);

            isValid = true;

            //ensure the file is existing
            if (!File.Exists(filename))
                throw new ArgumentException(string.Format("Test suite '{0}' not found!", filename));

            // Create the XmlReader object.
            XmlReader reader = XmlReader.Create(filename, settings);

            try
            {
                // Parse the file. 
                while (reader.Read()) ;
                //The validationeventhandler and the catch are the only thing that will set isValid to false
            }
            catch (Exception ex)
            {
                isValid = false;
                if (ex is XmlException)
                    if (ex.Message.Contains("For security reasons DTD is prohibited"))
                        Console.WriteLine("DTD is prohibited. To activate it, set the flag allow-dtd-processing to true in the config file associated to this test-suite");
                Console.WriteLine(ex.Message);
            }
            return isValid;
        }

        private XmlReaderSettings BuildXmlReaderSettings(bool isDtdProcessing)
        {
            // Set the validation settings.
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            //Removed for Issue#2 on Codeplex
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            //Allow DTD processing
            if (isDtdProcessing)
                settings.DtdProcessing = DtdProcessing.Parse;
            else
                settings.DtdProcessing = DtdProcessing.Prohibit;

            // Supply the credentials necessary to access the DTD file stored on the network.
            XmlUrlResolver resolver = new XmlUrlResolver();
            resolver.Credentials = System.Net.CredentialCache.DefaultCredentials;
            settings.XmlResolver = resolver;

            //Get the Schema
            // A Stream is needed to read the XSD document contained in the assembly.
            using (Stream stream = Assembly.GetExecutingAssembly()
                                           .GetManifestResourceStream("NBi.Xml.NBi-TestSuite.xsd"))
            {
                settings.Schemas.Add("http://NBi/TestSuite", XmlReader.Create(stream));
                settings.Schemas.Compile();
            }
            return settings;
        }

        private void ValidationCallBack(Object sender, ValidationEventArgs args)
        {
            //This is only called on error
            // Display any warnings or errors.

            if (args.Severity == XmlSeverityType.Warning)
                Console.WriteLine("Warning: Matching schema not found.  No validation occurred." + args.Message);
            else
                Console.WriteLine("Validation error: " + args.Message);

            isValid = false; //Validation failed
        }



    }
}
