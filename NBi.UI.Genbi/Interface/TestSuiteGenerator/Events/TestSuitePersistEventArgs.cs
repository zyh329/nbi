﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBi.UI.Genbi.Interface.TestSuiteGenerator.Events
{
    public class TestSuitePersistEventArgs : FilePersistEventArgs
    {
        public TestSuitePersistEventArgs(string fileName) : base (fileName)
        {
        }
    }
}
