var folderName = @"C:\Documents\"; //Update this to the folder that contains the ModelAutoBuild.xlsx file

/********************Data Sources********************/
var fileName = "ModelAutoBuild_DataSources.txt";

var Metadata = ReadFile(folderName+fileName);

// Delete all data sources 
foreach(var o in Model.DataSources.ToList())
{
    o.Delete();
}

// Split the file into rows by CR and LF characters:
var tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var srvr = tsvColumns[0];
    var ic = tsvColumns[1];
    var name = srvr + " " + ic;             
    string provider = tsvColumns[2];
    var uID = tsvColumns[3];
    var pwd = tsvColumns[4];
    var conn = "Data Source="+srvr+";Initial Catalog="+ic;
    if (uID != "")
    {
        conn = conn + ";User ID=" + uID + ";Password=" + pwd;
    }
    
    // Add Data Sources
    var obj = Model.AddDataSource(name);
    
    if(provider == "SQL")
    {
        obj.Provider = "System.Data.SqlClient";
        obj.ConnectionString = conn +";Integrated Security=True;Persist Security Info=True";
    }
    else if(provider == "Databricks")
    {
        obj.Provider = "";
        obj.ConnectionString = "Provider=MSDASQL;DSN="+srvr;
    }
}

// Remove duplicated data sources
foreach(var o in Model.DataSources.ToList())
{
    var n = o.Name;
    if(n.Substring(n.Length - 2) == " 1")
    {
        o.Delete();
    }
}
/*****************************************************/

/************************Tables***********************/
fileName = "ModelAutoBuild_Tables.txt";

Metadata = ReadFile(folderName+fileName);

// Delete all partitions 
foreach(var o in Model.Tables.ToList())
{
    o.Delete();

}

// Split the file into rows by CR and LF characters:
tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var name = tsvColumns[0];             
    var dataSource = tsvColumns[1];
    var tableType = tsvColumns[2].ToUpper();
    var schema = tsvColumns[3];
    var dc = tsvColumns[4];
    var m = tsvColumns[5];
    var desc = tsvColumns[6];
    
    var obj = Model.AddTable(name);
    Model.Tables[name].Partitions[name].DataSource = Model.DataSources[dataSource];
    Model.Tables[name].Partitions[name].Query = "SELECT * FROM [" + schema + "].[" + tableType + "_" + name + "]";
    obj.Description = desc;
    if(dc == "Date")
    {
        obj.DataCategory = "Time";
    }
    
    if(m == "DirectQuery" || m == "Direct Query" || m == "DQ")
    {
        Model.Tables[name].Partitions[name].Mode = ModeType.DirectQuery;
    }
}
/*****************************************************/

/*****************Measures and Columns****************/
fileName = "ModelAutoBuild_MeasuresColumns.txt";

Metadata = ReadFile(folderName+fileName);

// Delete all columns 
foreach(var o in Model.AllColumns.ToList())
{
    o.Delete();
}

// Delete all measures 
foreach(var o in Model.AllMeasures.ToList())
{
    o.Delete();
}

// Split the file into rows by CR and LF characters:
tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var tableName = tsvColumns[1];
    var objectName = tsvColumns[0];             
    var objectType = tsvColumns[2];
    var sourceColumn = tsvColumns[3];
    var dt = tsvColumns[4];
    var expr = tsvColumns[5];
    var hide = tsvColumns[6];
    var fmt = tsvColumns[7];
    var key = tsvColumns[8];
    var summ = tsvColumns[9];
    var displayFolder = tsvColumns[10];
    var dataCategory = tsvColumns[11];
    var sortByCol = tsvColumns[12];
    var desc = tsvColumns[13];
    
    // Add column properties
    if(objectType == "Column")
    {
    var obj = Model.Tables[tableName].AddDataColumn(objectName);
    obj.SourceColumn = sourceColumn;
     
    if(dt == "Integer")
    {
       obj.DataType = DataType.Int64;
    } 
    else if(dt == "String")
    {
        obj.DataType = DataType.String;
    }
    else if(dt == "Datetime")
    {
        obj.DataType = DataType.DateTime;
    }
    else if(dt == "Double")
    {
        obj.DataType = DataType.Double;
    }
    if(hide == "Yes")
    {
        obj.IsHidden = true;
    }
    if(key == "Yes")
    {
        obj.IsKey = true;
    }
    if (summ =="None") 
    {
       obj.SummarizeBy = AggregateFunction.None;
    }
    obj.DisplayFolder = displayFolder;
    obj.DataCategory = dataCategory;
    obj.Description = desc;
       if(sortByCol != "")
       {
          obj.SortByColumn = Model.Tables[tableName].Columns[sortByCol]; 
       }
       if(fmt == "Whole Number")
           obj.FormatString = "#,0";
       else if(fmt == "Percentage")
           obj.FormatString = "#,0.0%;-#,0.0%;#,0.0%";
       else if(fmt == "Month Year")
           obj.FormatString = "mmmm YYYY";
       else if(fmt == "Currency")
           obj.FormatString = "$#,0;$#,0;$#,0";
       else if(fmt == "Decimal")
           obj.FormatString = "#,0.0";
    }

    // Add measure properties
    if(objectType == "Measure")
    {
    var obj = Model.Tables[tableName].AddMeasure(objectName); 
    obj.Expression = expr;
    obj.DisplayFolder = displayFolder;
    obj.Description = desc;
    if(hide == "Yes")
    {
        obj.IsHidden = true;
    }
       if(fmt == "Whole Number")
           obj.FormatString = "#,0";
       else if(fmt == "Percentage")
           obj.FormatString = "#,0.0%;-#,0.0%;#,0.0%";
       else if(fmt == "Currency")
           obj.FormatString = "$#,0;$#,0;$#,0";
       else if(fmt == "Month Year")
           obj.FormatString = "mmmm YYYY";
    
    }
}

 // Remove quotes from Expression and FormatString; Format all DAX expressions
foreach(var o in Model.AllMeasures.ToList())
{
    var expr = o.Expression;
    var exprLength = expr.Length;
    var fs = o.FormatString;
    
    // Remove quotes from Expressions
    if(expr[0] == '"')
      {
        o.Expression = expr.Substring(1,exprLength - 2);
      }
    
     o.Expression = o.Expression.Replace("\"\"","\"");
     
    // Remove quotes from Format Strings
     o.FormatString = fs.Trim('"');

    // Uncomment the line below if you want the DAX to be formatted.
    // o.Expression = FormatDax(o.Expression);
}

foreach(var o in Model.AllMeasures.ToList())
{
    var expr = o.Expression;
    
    // Replaces \n with new line
     o.Expression = expr.Replace("\\n", "\r\n");

}

/*****************************************************/

/*********************Relationships*******************/
/*
fileName = "ModelAutoBuild_Relationships.txt";

Metadata = ReadFile(folderName+fileName);

// Delete all relationships 
foreach(var o in Model.Relationships.ToList())
{
    o.Delete();
}

// Split the file into rows by CR and LF characters:
tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var fromTable = tsvColumns[0];  
    var toTable = tsvColumns[2];       
    var fromColumn = tsvColumns[1];
    var toColumn = tsvColumns[3];
    var act = tsvColumns[4];
    var cfb = tsvColumns[5];
    
    // This assumes that the model does not already contain a measure with the same name (if it does, the new measure will get a numeric suffix):
    var obj = Model.AddRelationship();
           obj.FromColumn = Model.Tables[fromTable].Columns[fromColumn];
           obj.ToColumn = Model.Tables[toTable].Columns[toColumn];
           
           if(act == "No")
           {
               obj.IsActive = false;
           }

           if(cfb == "Single")
           {
               obj.CrossFilteringBehavior = CrossFilteringBehavior.OneDirection;
           }

           if(cfb == "Bi")
           {
               obj.CrossFilteringBehavior = CrossFilteringBehavior.BothDirections;
           }
               
           
}
*/
/*****************************************************/

/*************************Model***********************/

fileName = "ModelAutoBuild_Model.txt";

Metadata = ReadFile(folderName+fileName);

// Split the file into rows by CR and LF characters:
tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var name = tsvColumns[0];             
    var m = tsvColumns[1];   
    var prem = tsvColumns[2];            
    // Update Name & ID
    var n = Model.Database.Name = name;
    var i = Model.Database.ID = name;

    // Update model mode
    if(m == "DirectQuery" || m == "Direct Query" || m == "DQ")
    {
    	Model.DefaultMode = ModeType.DirectQuery;
    }

    //This enables overwriting deployments to Power BI Premium
    if(prem == "Yes" || prem == "Y")
    {
        Model.DefaultPowerBIDataSourceVersion = PowerBIDataSourceVersion.PowerBI_V3;	
    }  
}

/************Auto-create relationships***************/

fileName = "ModelAutoBuild_Tables.txt";

Metadata = ReadFile(folderName+fileName);

// Split the file into rows by CR and LF characters:
tsvRows = Metadata.Split(new[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);

// Loop through all rows but skip the first one:
foreach(var row in tsvRows.Skip(1))
{
    var tsvColumns = row.Split('\t');     // Assume file uses tabs as column separator
    var name = tsvColumns[0];            
    var tableType = tsvColumns[2].ToUpper();
    var keySuffix = "Id";
    
    if(tableType == "FACT")
    {
        foreach(var factColumn in Model.Tables[name].Columns.Where(c => c.Name.EndsWith(keySuffix)))
        {
            var dim = Model.Tables.FirstOrDefault(t => factColumn.Name.EndsWith(t.Name + keySuffix));
                
            if(dim != null)
            {
                 var dimColumn = dim.Columns.FirstOrDefault(c => factColumn.Name.EndsWith(c.Name));
                 if(dimColumn != null)
                 {
                     var rel = Model.AddRelationship();
                     rel.FromColumn = factColumn;
                     rel.ToColumn = dimColumn;
                 }
                 
                    
            }
        }
            
    }
}
/*****************************************************/