﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace NBi.Xml
{
    public class TestXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        
        [XmlAttribute("uid")]
        public string UniqueIdentifier { get; set; }

        [XmlAttribute("description")]
        public string Description { get; set; }

        [XmlElement("Category")]
        public List<string> Categories;

        [XmlElement(Type = typeof(SyntacticallyCorrectXml), ElementName = "SyntacticallyCorrect"),
        XmlElement(Type = typeof(FasterThanXml), ElementName = "FasterThan"),
        XmlElement(Type = typeof(EqualToXml), ElementName = "EqualTo")
        ]
        public List<AbstractConstraintXml> Constraints;

        [XmlElement(Type = typeof(QueryXml), ElementName = "Query")]
        public List<QueryXml> TestCases;

        public TestXml()
        {
            Constraints = new List<AbstractConstraintXml>();
            TestCases = new List<QueryXml>();
            Categories = new List<string>();
        }
    }
}