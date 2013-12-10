﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NBi.Xml.Items;
using NBi.Xml.Settings;

namespace NBi.Xml.Constraints
{
    public abstract class AbstractConstraintForCollectionXml : AbstractConstraintXml
    {
        public override DefaultXml Default
        {
            get { return base.Default; }
            set
            {
                base.Default = value;
                if (Query != null)
                    Query.Default = value;
            }
        }

        [XmlAttribute("ignore-case")]
        [DefaultValue(false)]
        public bool IgnoreCase { get; set; }

        [XmlElement("item")]
        public List<string> Items { get; set; }

        [XmlElement("predefined")]
        public PredefinedItems PredefinedItems { get; set; }

        [XmlElement("one-column-query")]
        public QueryXml Query { get; set; }

        public IEnumerable<string> GetItems()
        {
            var list = new List<string>();
            if (Items != null)
                list.AddRange(Items);
            if (PredefinedItems != null)
                list.AddRange(PredefinedItems.GetItems());

            return list;
        }

        public AbstractConstraintForCollectionXml()
        {
            Items = new List<string>();
        }
    }
}
