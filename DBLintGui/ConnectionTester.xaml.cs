using System;
using System.Threading;
using System.Windows;
using DBLint.DataAccess;

namespace DBLint.DBLintGui
{
    internal class ConnectionTester
    {
        private Connection _connection;

        private string _errMessage = "An unknown error occured when trying to connect to the database";
        private string _errType = "Unknown error, or timeout";
        private volatile bool _successful = false;
        public bool TestConnection()
        {
            Thread t = new Thread(() =>
                                      {
                                          try
                                          {
                                              if (new DataAccess.Extractor(_connection).Database.TestConnection())
                                              {
                                                  _successful = true;
                                              }
                                          }
                                          catch (Exception exception)
                                          {
                                              lock (this)
                                              {
                                                  _errType = exception.GetType().Name;
                                                  _errMessage = exception.Message;
                                              }
                                          }
                                      });
            t.Start();
            if (!t.Join(2000) || !_successful)
            {
                lock (this)
                {
                    MessageBox.Show(_errMessage, _errType);
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public ConnectionTester(Connection connection)
        {
            this._connection = connection;
        }
    }
}