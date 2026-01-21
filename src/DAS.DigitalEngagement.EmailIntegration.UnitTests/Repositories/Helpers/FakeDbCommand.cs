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
    public class FakeDbCommand: DbCommand
    {
        private readonly DbDataReader _reader;

        public FakeDbCommand(DbDataReader reader)
        {
            _reader = reader;
        }

        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => _reader;

        public override int ExecuteNonQuery() => 0;
        public override object ExecuteScalar() => null;

        public override void Prepare() { }
        public override void Cancel() { }

        protected override DbParameter CreateDbParameter()
            => throw new NotImplementedException();

        protected override DbConnection DbConnection { get; set; } = null!;
        protected override DbTransaction DbTransaction { get; set; } = null!;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
    }
}
