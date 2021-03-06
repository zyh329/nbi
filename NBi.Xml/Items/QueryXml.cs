﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NBi.Core;

namespace NBi.Xml.Items
{
    public class QueryXml : QueryableXml
    {
        
        [XmlAttribute("file")]
        public string File { get; set; }
        
        [XmlAttribute("connectionString-ref")]
        public string ConnectionStringReference { get; set; }

        [XmlText]
        public string InlineQuery { get; set; }


        public override string GetQuery()
        {
            //if Sql is specified then return it
            if (!string.IsNullOrEmpty(InlineQuery))
                return InlineQuery;

            //Else check that file exists and read the file's content
            var file = string.Empty;
            if (Path.IsPathRooted(File))
                file = File;
            else
                file = Settings.BasePath + File;
            if (!System.IO.File.Exists(file))
                throw new ExternalDependencyNotFoundException(file);
            var query = System.IO.File.ReadAllText(file, Encoding.UTF8);
            return query;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public virtual IDbCommand GetCommand()
        {
            var conn = new ConnectionFactory().Get(GetConnectionString());
            var cmd = conn.CreateCommand();
            cmd.CommandText = GetQuery();

            return cmd;
        }

        

    }
}
