﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace NBi.Core.Analysis.Metadata
{
    public class MetadataExcelReader
    {
        public event ProgressStatusHandler ProgressStatusChanged;
        public delegate void ProgressStatusHandler(Object sender, ProgressStatusEventArgs e);
        
        public string Filename { get; private set; }

        protected string _sheetName;
        public string SheetName 
        { 
            get {return _sheetName;}
            set 
            {
                 if(value.EndsWith("$"))
                     value = value.Remove(value.Length-1);
                _sheetName= value;
            }
        }

        public string SheetRange { get; set; }

        protected DataTable _dataTable;
        protected DataTable DataTable
        {
            get
            {
                if (_dataTable == null)
                    _dataTable = Read();
                return _dataTable;
            }
        }

        public IList<string> Tracks { get; private set; }
        public IList<string> Sheets { get; private set; }

        public MetadataExcelReader(string filename)
        {
            Filename = filename;
        }

        public MetadataExcelReader(string filename, string sheetname)
        {
            Filename = filename;
            SheetName = sheetname;
        }
        
        public MetadataExcelReader(string filename, string sheetname, string sheetRange)
        {
            Filename = filename;
            SheetName = sheetname;
            SheetRange = sheetRange;
        }

        protected string GetConnectionString(string filename)
        {
            return
               @"Provider=Microsoft.Jet.OLEDB.4.0;" +
               @"Data Source=" + filename + ";" +
               @"Extended Properties=" + Convert.ToChar(34).ToString() +
               @"Excel 8.0;HDR=YES"//;Excel 12.0 Xml;IMEX=1;TypeGuessRows=0;ImportMixedTypes=Text" 
               + Convert.ToChar(34).ToString()
               ;
        }

        public void Read(string track, ref MeasureGroups measureGroups, ref Dimensions dimensions)
        {
            if (measureGroups == null || dimensions == null)
                throw new ArgumentNullException();

            if (ProgressStatusChanged != null)
                ProgressStatusChanged(this, new ProgressStatusEventArgs(string.Format("Processing Xls file for track {0}", track)));

            int i = 0;
            foreach (DataRow row in DataTable.Rows)
	        {
                i++;
                if (ProgressStatusChanged != null)
                    ProgressStatusChanged(this, new ProgressStatusEventArgs(String.Format("Loading row {0} of {1}", i, DataTable.Rows.Count), i, DataTable.Rows.Count));
                var trackPos = Tracks.IndexOf(track) + 6;
                var r = GetMetadata(row, trackPos);
                
                LoadMetadata(r, true, ref measureGroups, ref dimensions);
            }

            if (ProgressStatusChanged != null)
                ProgressStatusChanged(this, new ProgressStatusEventArgs("Xls file processed"));
        }

        public void Read(ref MeasureGroups measureGroups, ref Dimensions dimensions)
        {
            if (measureGroups == null || dimensions == null)
                throw new ArgumentNullException();

            if (ProgressStatusChanged!=null)
                ProgressStatusChanged(this, new ProgressStatusEventArgs(string.Format("Processing Xls file")));
            int i = 0;
            foreach (DataRow row in DataTable.Rows)
            {
                i++;
                if (ProgressStatusChanged != null)
                    ProgressStatusChanged(this, new ProgressStatusEventArgs(String.Format("Loading row {0} of {1}", i, DataTable.Rows.Count), i, DataTable.Rows.Count));
                var r = GetMetadata(row);

                LoadMetadata(r, false, ref measureGroups, ref dimensions);
            }
            if (ProgressStatusChanged != null)
                ProgressStatusChanged(this, new ProgressStatusEventArgs("Xls file processed"));
        }
  
        private void LoadMetadata(XlsMetadata r, bool filter, ref MeasureGroups measureGroups, ref Dimensions dimensions)
        {
            MeasureGroup mg = null;

            if ((!filter) || r.isChecked)
            {

                if (measureGroups.ContainsKey(r.measureGroupName))
                {
                    mg = measureGroups[r.measureGroupName];
                }
                else
                {
                    mg = new MeasureGroup(r.measureGroupName);
                    measureGroups.Add(mg);
                }

                if (!mg.Measures.ContainsKey(r.measureUniqueName))
                {
                    mg.Measures.Add(r.measureUniqueName, r.measureCaption);
                }

                Dimension dim = null;

                if (dimensions.ContainsKey(r.dimensionUniqueName))
                {
                    dim = dimensions[r.dimensionUniqueName];
                }
                else
                {
                    dim = new Dimension(r.dimensionUniqueName, r.dimensionCaption, new Hierarchies());
                    dimensions.Add(dim);
                }

                if (!dim.Hierarchies.ContainsKey(r.hierarchyUniqueName))
                {
                    var hierarchy = new Hierarchy(r.hierarchyUniqueName, r.hierarchyCaption);
                    dim.Hierarchies.Add(r.hierarchyUniqueName, hierarchy);
                }

                if (!mg.LinkedDimensions.ContainsKey(r.dimensionUniqueName))
                    mg.LinkedDimensions.Add(dim);
            }
        }

        protected DataTable Read()
        {
            if (ProgressStatusChanged != null)
                ProgressStatusChanged(this, new ProgressStatusEventArgs("Reading Xls file"));
            var dt = new DataTable("Metadata");
        
            using (var conn = new OleDbConnection())
            {
                conn.ConnectionString = GetConnectionString(Filename);
                conn.Open();

                string commandText = null;
                if (string.IsNullOrEmpty(SheetRange))
                    commandText = string.Format("SELECT * FROM [{0}$]", SheetName);
                else
                    commandText = string.Format("SELECT * FROM [{0}]${1}", SheetName, SheetRange);

                using (var cmd = new OleDbCommand(commandText, conn))
                {

                    var adapter = new OleDbDataAdapter();
                    adapter.SelectCommand = cmd;
                    adapter.FillSchema(dt, SchemaType.Source);
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        public void GetSheets()
        {
            var dt = new DataTable("Track");

            using (var conn = new OleDbConnection())
            {
                conn.ConnectionString = GetConnectionString(Filename);
                conn.Open();

                dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                String[] excelSheets = new String[dt.Rows.Count];
                int i = 0;

                // Add the sheet name to the string array.
                foreach (DataRow row in dt.Rows)
                {
                    excelSheets[i] = row["TABLE_NAME"].ToString();
                    i++;
                }

                Sheets = excelSheets;
            }

        }

        public void GetTracks()
        {
            var dt = new DataTable("Track");

            using (var conn = new OleDbConnection())
            {
                conn.ConnectionString = GetConnectionString(Filename);
                conn.Open();

                var commandText = string.Format("SELECT * FROM [{0}$A1:AZ1]", SheetName, SheetRange);

                using (var cmd = new OleDbCommand(commandText, conn))
                {

                    var adapter = new OleDbDataAdapter();
                    adapter.SelectCommand = cmd;
                    adapter.FillSchema(dt, SchemaType.Source);
                    adapter.Fill(dt);
                }
            }

            Tracks = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                if (col.Ordinal>5)
                    Tracks.Add(col.ColumnName);
            }
        }

        protected XlsMetadata GetMetadata(DataRow row, int trackPos)
        {
            var res = GetMetadataBasic(row);

            if (trackPos>=0)
                res.isChecked = !row.IsNull(trackPos);
            return res;
        }

        protected XlsMetadata GetMetadata(DataRow row)
        {
            return GetMetadata(row, -1);
        }

        protected XlsMetadata GetMetadataBasic(DataRow row)
        {
            var xlsMetadata = new XlsMetadata();

            xlsMetadata.measureGroupName = row.IsNull("MeasureGroup") ? string.Empty : (string)row["MeasureGroup"];
            
            if (row.Table.Columns.IndexOf("Measure") > 0)
                xlsMetadata.measureCaption = (string)row["Measure"];
            if (row.Table.Columns.IndexOf("MeasureCaption") > 0)
                xlsMetadata.measureCaption = (string)row["MeasureCaption"];
            
            if(row.Table.Columns.IndexOf("MeasureUniqueName")>0)
                xlsMetadata.measureUniqueName = (string)row["MeasureUniqueName"];
            else
                xlsMetadata.measureUniqueName = "[" + (string)row["Measure"] + "]";

            if (row.Table.Columns.IndexOf("DimensionCaption") > 0)
                xlsMetadata.measureUniqueName = (string)row["DimensionCaption"];
            else
                xlsMetadata.dimensionCaption = ((string)row[3]).Replace("[", "").Replace("]", "");

            if (row.Table.Columns.IndexOf("Dimension") > 0)
                xlsMetadata.dimensionUniqueName = (string)row["Dimension"];
            if (row.Table.Columns.IndexOf("DimensionUniqueName") > 0)
                xlsMetadata.dimensionUniqueName = (string)row["DimensionUniqueName"];

            if (row.Table.Columns.IndexOf("DimensionAttribute") > 0)
            {
                xlsMetadata.hierarchyCaption = (string)row["DimensionAttribute"];
                xlsMetadata.hierarchyUniqueName = "[" + (string)row["DimensionAttribute"] + "]";
            }
            if (row.Table.Columns.IndexOf("HierarchyCaption") > 0)
                xlsMetadata.hierarchyCaption = (string)row["HierarchyCaption"];
            if (row.Table.Columns.IndexOf("HierarchyUniqueName") > 0)
                xlsMetadata.hierarchyUniqueName = (string)row["HierarchyUniqueName"];

            return xlsMetadata;
        }

        protected struct XlsMetadata
        {
            public string measureGroupName;
            public string measureCaption;
            public string measureUniqueName;
            public string dimensionCaption;
            public string dimensionUniqueName;
            public string hierarchyCaption;
            public string hierarchyUniqueName;
            public bool isChecked;
        }

       


    }
}
