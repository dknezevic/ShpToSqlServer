using GeoAPI.Geometries;
using Microsoft.SqlServer.Types;
using Microsoft.Win32;
using SharpMap.Data;
using SharpMap.Data.Providers;
using ShpToSql.SqlConnectionControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ShpToSQL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private SqlConnectionString _cString;
        private FeatureDataTable _table;

        public MainWindow()
        {
            InitializeComponent();
            CustomInitialization();
        }

        private void CustomInitialization()
        {
            setControlAvailability(false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SqlConnectionString CString
        {
            get { return _cString; }
            set
            {
                _cString = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("CString"));
            }
        }

        #region Events

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _shapefilePath.Text = ShowShapefileDialog(_shapefilePath.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            String spName = LoadShapefile(_shapefilePath.Text);
            if (spName != String.Empty)
            {
                _status.Text = "Loaded shapefile " + spName;
                _tableName.Text = spName.Replace(' ', '_');
                if (_table.Columns.Count > 0) _primaryKey.Text = _table.Columns[0].ColumnName;
                setControlAvailability(true);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (!CheckFields()) return;

            try 
            { 
                if (ImportFromShapefile(_shapefilePath.Text, CString.ToString())!=String.Empty) _status.Text = "Import has finished";            
            }
            catch (System.Data.SqlClient.SqlException)
            {
                _status.Text = "Problem with db connection. Check the connection string";
            }
            catch
            {
                _status.Text = "Something has gone horribly wrong";
            }
        }

        #endregion Events

        #region Import

        public String LoadShapefile(String shapefile)
        {
            FeatureDataTable dt;

            try
            {
                dt = GetFeatureTableFromShapefile(shapefile);
            }
            catch (Exception)
            {
                _status.Text = "Bad Shape file path";
                return String.Empty;
            }

            _textBoxPanel.Children.Clear();
            foreach (DataColumn column in dt.Columns)
            {
                TextBox columnName = new TextBox();
                columnName.Margin = new Thickness(5);
                columnName.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                columnName.Text = column.ColumnName;
                _textBoxPanel.Children.Add(columnName);
            }

            _table = dt;
            return Path.GetFileNameWithoutExtension(shapefile);
        }

        public FeatureDataTable GetFeatureTableFromShapefile(string shapeFilePath)
        {
            GeoAPI.GeometryServiceProvider.Instance = new NetTopologySuite.NtsGeometryServices();
            int cd;
            if (!int.TryParse(_enconding.Text, out cd))
            {
                _status.Text = "Bad code page";
            }

            ShapeFile sf = new ShapeFile(shapeFilePath);
            sf.Encoding = Encoding.GetEncoding(int.Parse(_enconding.Text));
            sf.Open();
            Envelope ext = sf.GetExtents();
            FeatureDataSet ds = new FeatureDataSet();
            sf.ExecuteIntersectionQuery(ext, ds);
            //ds.Tables[0].Columns.Remove("Oid");
            return ds.Tables[0];
        }

        public String ImportFromShapefile(String shapeFilePath, String connectionString)
        {
            String shapeFileName = Path.GetFileNameWithoutExtension(shapeFilePath);

            //Define destination table
            DataTable bulkSqlTable = new DataTable();
            var newNames = _textBoxPanel.Children.OfType<TextBox>().ToList();
               
            int rowNo = 0;
            foreach (DataColumn col in _table.Columns)
            {
                bulkSqlTable.Columns.Add(newNames[rowNo++].Text, col.DataType);
            }
            bulkSqlTable.Columns.Add("Geom", typeof(SqlGeometry));

            //Populate destination table
            foreach (FeatureDataRow row in _table)
            {
                rowNo = 0;
                DataRow newTableRow = bulkSqlTable.NewRow();
                
                foreach (DataColumn col in _table.Columns)
                {
                    newTableRow[newNames[rowNo++].Text] = row[col.ColumnName];
                }
                newTableRow["Geom"] = SqlGeometry.STGeomFromWKB(new SqlBytes(row.Geometry.AsBinary()), row.Geometry.SRID);

                bulkSqlTable.Rows.Add(newTableRow);
            }

            try
            {
                bulkSqlTable.PrimaryKey = new DataColumn[] { bulkSqlTable.Columns[_primaryKey.Text] };
                if (bulkSqlTable.Columns[_primaryKey.Text].DataType == typeof(String)) bulkSqlTable.Columns[_primaryKey.Text].MaxLength = 400;
            }
            catch
            {
                _status.Text = "Wrong primary key, bad column choice";
                return String.Empty;
            }

            //Save destination table
            using (SqlConnection connection = new SqlConnection(connectionString))
            { 
                SqlTransaction transaction = null;
                connection.Open();
                try
                {
                    transaction = connection.BeginTransaction();

                    SqlTableCreator tableCreator = new SqlTableCreator(connection, transaction);
                    tableCreator.DestinationTableName = _tableName.Text;
                    tableCreator.CreateFromDataTable(bulkSqlTable);
                    
                    using (var sqlBulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction))
                    {
                        sqlBulkCopy.DestinationTableName = _tableName.Text;
                        sqlBulkCopy.BatchSize = 10000;
                        sqlBulkCopy.WriteToServer(bulkSqlTable);
                    }
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }
                    
                    _status.Text = "Bad table name. Probabbly already exists. Check the primary key too.";
                    return String.Empty;
                }
                finally
                {
                    connection.Close();
                    SqlConnection.ClearPool(connection);
                }
            }

            return shapeFileName;
        }

        #endregion Import

        #region Tools

        public String ShowShapefileDialog(String oldState)
        {
            String path = oldState; ;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".shp";
            dlg.Filter = "Shape Files (*.shp) | *.shp";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                path = dlg.FileName;
            }

            return path;
        }

        private bool CheckFields()
        {
            DbConnectionStringBuilder csb = new DbConnectionStringBuilder();
            if (!CString.IsValid())
            {
                _status.Text = "Bad connection string";
                return false;
            }

            if (_tableName.Text == String.Empty || _tableName.Text.Contains(' '))
            {
                _status.Text = "Bad destination table name";
                return false;
            }
            if (!_table.Columns.Contains(_primaryKey.Text))
            {
                _status.Text = "Bad primary key";
                return false;
            }

            return true;
        }

        private void setControlAvailability(bool isEnabled)
        {
            _primaryKey.IsEnabled = isEnabled;
            _tableName.IsEnabled = isEnabled;
            _startImportBtn.IsEnabled = isEnabled;
        }

        #endregion Tools


    }
}
