using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Repositories.Helpers
{
    public  class FakeDbConnection : DbConnection
    {
        private readonly DbCommand _command;

        public FakeDbConnection(DbCommand command)
        {
            _command = command;
        }

        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "Test";
        public override string DataSource => "Test";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;

        public override Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override void Open() { }
        public override void Close() { }
        public override void ChangeDatabase(string databaseName) { }

        protected override DbCommand CreateDbCommand() => _command;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }
    }
}
