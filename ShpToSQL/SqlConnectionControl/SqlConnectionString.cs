using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ShpToSql.SqlConnectionControl
{
    public class SqlConnectionString : INotifyPropertyChanged
    {
        private readonly System.Data.SqlClient.SqlConnectionStringBuilder _builder = new System.Data.SqlClient.SqlConnectionStringBuilder
                                                                   {
                                                                       Pooling = false,
                                                                       IntegratedSecurity = true,
                                                                       TypeSystemVersion = "Sql Server 2012"
                                                                   };

        private ObservableCollection<String> _typeSystemVersions = new ObservableCollection<string> { "Sql Server 2008", "Sql Server 2012" };

        public SqlConnectionString()
        { }

        public SqlConnectionString(string connectionString)
        {
            _builder.ConnectionString = connectionString;
        }

        public static implicit operator string(SqlConnectionString connectionString)
        {
            return connectionString.ToString();
        }

        public override string ToString()
        {
            if (Server.EndsWith(".sdf"))
                if (string.IsNullOrEmpty(Password))
                    return new System.Data.SqlClient.SqlConnectionStringBuilder {DataSource = Server}.ConnectionString;
                else
                    return new System.Data.SqlClient.SqlConnectionStringBuilder {DataSource = Server, Password = Password}.
                        ConnectionString;

            return _builder.ConnectionString;
        }

        /// <summary>
        /// Creates a copy of this connection string with the specified database instead of the current
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <returns></returns>
        public SqlConnectionString WithDatabase(string databaseName)
        {
            return new SqlConnectionString
                       {
                           Server = Server,
                           Database = databaseName,
                           IntegratedSecurity = IntegratedSecurity,
                           UserName = UserName,
                           Password = Password,
                           Pooling = Pooling
                       };
        }


        public ObservableCollection<string> TypeSystemVersions
        {
            get
            {
                return _typeSystemVersions;
            }
            private set
            {
                if (_typeSystemVersions != value)
                { 
                    _typeSystemVersions = value;
                    OnPropertyChanged("TypeSystemVersions");
                }
            }
        }


        public string Server
        {
            get
            {
                return _builder.DataSource;
            }
            set
            {
                if (_builder.DataSource == value) return;
                _builder.DataSource = value;
                OnPropertyChanged("Server");
                OnPropertyChanged("IsValid");
            }
        }

        public string Database
        {
            get
            {
                return _builder.InitialCatalog;
            }
            set
            {
                if (_builder.InitialCatalog == value) return;
                _builder.InitialCatalog = value;
                OnPropertyChanged("Database");
                OnPropertyChanged("IsValid");
            }
        }

        public string TypeSystemVersion
        {
            get
            {
                return _builder.TypeSystemVersion;
            }
            set
            {
                if (_builder.TypeSystemVersion == value) return;
                _builder.TypeSystemVersion = value;
            }
        }

        public string UserName
        {
            get
            {
                return _builder.UserID;
            }
            set
            {
                if (_builder.UserID == value) return;
                _builder.UserID = value;
                OnPropertyChanged("UserName");
                OnPropertyChanged("IsValid");
            }
        }

        public bool Pooling
        {
            get
            {
                return _builder.Pooling;
            }
            set
            {
                if (_builder.Pooling == value) return;
                _builder.Pooling = value;
                OnPropertyChanged("Pooling");
                OnPropertyChanged("IsValid");
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                return _builder.ConnectTimeout;
            }
            set
            {
                if (_builder.ConnectTimeout == value) return;
                _builder.ConnectTimeout = value;
                OnPropertyChanged("ConnectionTimeout");
                OnPropertyChanged("IsValid");
            }
        }

        public string Password
        {
            get
            {
                return _builder.Password;
            }
            set
            {
                if (_builder.Password == value) return;
                _builder.Password = value;
                OnPropertyChanged("Password");
                OnPropertyChanged("IsValid");
            }
        }

        public bool IntegratedSecurity
        {
            get
            {
                return _builder.IntegratedSecurity;
            }
            set
            {
                if (_builder.IntegratedSecurity == value) return;
                _builder.IntegratedSecurity = value;
                OnPropertyChanged("IntegratedSecurity");
                OnPropertyChanged("IsValid");
            }
        }

        public bool IsValid()
        {
            return 
                (!string.IsNullOrEmpty(Server) && Server.EndsWith(".sdf")) ||
                (!string.IsNullOrEmpty(Server) &&
                 !string.IsNullOrEmpty(Database) &&
                 (IntegratedSecurity || (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))));
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}


