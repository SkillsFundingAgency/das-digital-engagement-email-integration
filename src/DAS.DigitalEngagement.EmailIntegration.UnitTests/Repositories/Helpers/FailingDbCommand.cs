#nullable enable
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Repositories.Helpers
{
    public class FailingDbCommand : DbCommand
    {
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override string CommandText { get; set; } = string.Empty;
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => throw new InvalidOperationException("ExecuteReader failed");

        // Correct async extensibility point to override on DbCommand
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(
            CommandBehavior behavior,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("ExecuteReader failed");

        public override int ExecuteNonQuery() => throw new NotImplementedException();
        public override object? ExecuteScalar() => throw new NotImplementedException();

        public override void Prepare() { }
        public override void Cancel() { }

        protected override DbParameter CreateDbParameter()
            => throw new NotImplementedException();

        // Fix for CS8765: Ensure nullability matches base member
        protected override DbConnection? DbConnection { get; set; } = null!;
        protected override DbTransaction? DbTransaction { get; set; } = null!;

        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
    }
}
