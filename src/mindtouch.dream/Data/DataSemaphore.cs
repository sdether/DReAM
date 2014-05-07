using System;
using System.Data;

namespace MindTouch.Data {
    public class DataSemaphore : IDisposable {

        public bool _acquired = false;
        public readonly string Name;
        private IDbConnection _connection;
        private readonly DataFactory _factory;

        public DataSemaphore(string name, int timeoutSeconds, DataFactory factory, string connectionString) {
            Name = name;
            _factory = factory;
            try {
                _connection = factory.OpenConnection(connectionString);
                using(var command = _factory.CreateQuery(string.Format("SELECT GET_LOCK(?NAME,?TIMEOUT);"))) {
                    command.CommandType = CommandType.Text;
                    command.Connection = _connection;
                    command.Parameters.Add(_factory.CreateParameter("NAME", Name, ParameterDirection.Input));
                    command.Parameters.Add(_factory.CreateParameter("TIMEOUT", timeoutSeconds, ParameterDirection.Input));
                    var value = command.ExecuteScalar();
                    _acquired = SysUtil.ChangeType<int>(value) == 1;
                }
            } catch {
                if(_connection != null) {
                    _connection.Dispose();
                    _connection = null;
                }
                throw;
            }
            if(Acquired) {
                return;
            }
            _connection.Dispose();
            _connection = null;
        }

        public bool Acquired { get { return _acquired; } }

        public void Dispose() {
            if(_connection == null) {
                return;
            }
            try {
                using(var command = _factory.CreateQuery(string.Format("SELECT RELEASE_LOCK(?NAME);"))) {
                    command.CommandType = CommandType.Text;
                    command.Connection = _connection;
                    command.Parameters.Add(_factory.CreateParameter("NAME", Name, ParameterDirection.Input));
                    command.ExecuteScalar();
                }
            } catch {}
            _acquired = false;
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
    }
}