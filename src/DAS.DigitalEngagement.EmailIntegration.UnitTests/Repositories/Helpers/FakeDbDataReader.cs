using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DAS.DigitalEngagement.EmailIntegration.UnitTests.Repositories.Helpers
{
    public class FakeDbDataReader: DbDataReader
    {
        private int _readCount;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
            => Task.FromResult(++_readCount == 1);

        public override int FieldCount => 2;
        public override string GetName(int i) => i == 0 ? "EmployeeId" : "Name";
        public override object GetValue(int i) => i == 0 ? 123 : "John Doe";

        public override Task<bool> IsDBNullAsync(int i, CancellationToken ct)
            => Task.FromResult(false);

        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int RecordsAffected => 1;

        public override bool Read() => throw new NotImplementedException();
        public override bool NextResult() => false;

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(0);

        #region Not needed
        public override int Depth => 0;
        public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
        public override byte GetByte(int ordinal) => throw new NotImplementedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override char GetChar(int ordinal) => throw new NotImplementedException();
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
        public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
        public override double GetDouble(int ordinal) => throw new NotImplementedException();
        public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override short GetInt16(int ordinal) => throw new NotImplementedException();
        public override int GetInt32(int ordinal) => throw new NotImplementedException();
        public override long GetInt64(int ordinal) => throw new NotImplementedException();
        public override string GetString(int ordinal) => throw new NotImplementedException();
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }
        #endregion



    }
}
