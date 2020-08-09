using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace plgDBConnect
{
    /// <summary>
    /// �������, ������������ ��� �������, ���������� ��� ��������� ���������� �� ������� ��.
    /// ������������ � �������� ��� POstgreSQL. ���������� SQLite - �� �������������, ��������� ��� �� �� ������������ ���������. FireBird - �� �����������
    /// (������ �������������).</summary>
    /// <param name="PID">������������� �������� (Process ID). � �������� C# - �� ������ ������� ������, ��������� ������ ���������� ���������/������/��������
    /// ��� ��� ���� � ����� ��������� ����� ������ PID. ����� ����� ������ �����, ����� ���������� ����������� �� ������ ��������� � ����������� ��� ������.</param>
    /// <param name="channel">����� (��� ���������)</param>
    /// <param name="addinfo">�������������� ��������� ���������. PostgreSQL ��������� ������ � ������� ���������� �������������� ����������, ������� ����������� �������� ����� �������.</param>
    /// <remarks>�������� �������� ��� ������ � ���� ��� ��������������������� ������� - ��� ����������, ��� ������ ���������� � ���������� ��������� ������������
    /// � �� ��������, ����� ��� ������ �������� ���� ������. ������ ������������ (��� ���) ������ ������� �� ���������������.</remarks>
    public delegate void EventLogger(long PID, string channel, string addinfo);

    /// <summary>
    /// ���� ����������� � ������ ������
    /// <list type="number">
    /// <item>SQLite</item>
    /// <item>PostgreSQL</item>
    /// <item>FireBird (�������� �� ����������)</item>
    ///</list>
    /// </summary>
    public enum DBInterfaceType
    {
        /// <summary>
        /// ����������� ��� �������
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSQL,
        /// <summary>
        /// SQLite
        /// </summary>
        SQLite
    };//, FireBird };
    /// <summary>
    /// ������ �������, ������� ��� ������������ (��������� ������ ��� FireBird)
    /// </summary>
    public enum DBServerStateType { Standart, Embedded };
    /// <summary>
    /// ��������� ������ � ����� ������, ����������� ����� ����������� �����
    /// </summary>
    public abstract class IplgDBConnect : IDisposable
    {
        /// <summary>
        /// ������� ���������� ��������� ����������� ������ � ������ ������� ����������
        /// </summary>
        /// <returns>������, ���������� ��������� ����������� ������</returns>
        public abstract string GetLastSQL();

        /// <summary>
        /// ������� ���������� ��� ����������, ������������ �����������
        /// </summary>
        /// <returns>��� ����������</returns>
        public abstract DBInterfaceType GetConnectType();

        /// <summary>
        /// ���������� �������������� SQL �������, ������� � ���������� ����������� ��� ��������������� ����������
        /// </summary>
        /// <param name="SQL">����� SQL-������� � ��������� ���������� ��� �������������. �������� ����������� � ������ ������� � ��������� @</param>
        /// <returns>�������������� SQL ������� ���� IDbCommand</returns>
        public abstract IDbCommand GetSQLCommand(string SQL);

        /// <summary>
        /// ��������� �������� ��������� �������������� SQL-������� ����� �� �����������
        /// </summary>
        /// <param name="Cmd">�������������� �������, ���������� � ������� ��������� �������� � ���� @PARNAME</param>
        /// <param name="ParamName">��� ��������� � ���������� ����������� @</param>
        /// <param name="Val">�������� ���������</param>
        public abstract void AddParametersValue(IDbCommand Cmd, string ParamName, object Val);

        /// <summary>
        /// ���������� ��������� SQL ������� �� �������
        /// </summary>
        /// <param name="Cmd">�������������� �������</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ������������ ������� ��� -1 ��� ������</returns>
        public abstract int ExecuteNonQuery(IDbCommand Cmd, IDbTransaction tr = null);

        /// <summary>
        /// ���������� ��������� SQL ������ �� �������
        /// </summary>
        /// <param name="SQL">������, ���������� ������</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ������������ ������� ��� -1 ��� ������</returns>
        public abstract int ExecuteNonQuery(string SQL, IDbTransaction tr = null);

        /// <summary>
        ///  ���������� ��������, ������������ � ������ ������ ������� ���� ���������� ������� ���� ������
        /// </summary>
        /// <param name="SQL">����� SQL-������� � ��������� ���������� ��� �������������.</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ��������</returns>
        public abstract object ExecuteScalar(string SQL, IDbTransaction tr = null);

        /// <summary>
        ///  ���������� ��������, ������������ � ������ ������ ������� ���� ���������� ������� ���� ������
        /// </summary>
        /// <param name="Cmd">�������������� �������</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ��������</returns>
        public abstract object ExecuteScalar(IDbCommand Cmd, IDbTransaction tr = null);

        /// <summary>
        /// ������� ���������� ��������� ������� � ��������� �� ������� ����������� �� �������
        /// </summary>
        /// <param name="tbl">��� �������</param>
        /// <param name="Cmd">�������������� �������</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ���������� ������� ��� -1 ��� ������</returns>
        public abstract long FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr = null);

        /// <summary>
        /// ������� ���������� ��������� ������� � ��������� �� ������� ����������� �� �������
        /// </summary>
        /// <param name="tbl">��� �������</param>
        /// <param name="Cmd">������, ���������� ������</param>
        /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
        /// <returns>���������� ������� ��� -1 ��� ������</returns>
        public abstract long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr = null);

        /// <summary>
        /// ������ ����������
        /// </summary>
        /// <returns>������ ������� ���������� ��� NULL ���� �� �������������� ��� ���������� � �� �������</returns>
        public abstract IDbTransaction BeginTransaction();

        /// <summary>
        /// ������������� ���������, ��������� � �� ����������� SQL. �������� ������ �����, ����� �������������� ���� ������� ����������.
        /// ����� ������������� ���������� �����������.
        /// </summary>
        public abstract void Commit();

        /// <summary>
        /// ����� ���������, ��������� � �� ����������� SQL. �������� ������ �����, ����� �������������� ���� ������� ����������.
        /// ����� ������������� ���������� �����������.
        /// </summary>
        public abstract void Rollback();

        /// <summary>
        /// �������� ���������� � ����� ������, � �������������� ������ ����������, ������������������ � ������������
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// �������� ���������� � ����� ������, � �������������� ������ ����������, ������������������ � ������������
        /// </summary>
        public abstract void OpenAsync();

        /// <summary>
        /// �������� ���������� � ����� ������.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// ����������� DataReader �� �������������� ������
        /// </summary>
        /// <returns>��������� IDataReader</returns>
        public abstract IDataReader ExecuteReader(IDbCommand Cmd);
        /// <summary>
        /// ����������� ������ ��������������� �������
        /// </summary>
        /// <param name="Cmd"></param>
        /// <returns></returns>
        public abstract Task<DbDataReader> ExecuteReaderAsync(IDbCommand Cmd);
        /// <summary>
        /// ��������� �������� ���������� � ���������� �������� �������
        /// </summary>
        /// <param name="logger"></param>
        abstract public void SetDBNotification(EventLogger logger = null);
        /// <summary>
        /// ���������� �� ������� ���������� ������� ��� �������� ����������
        /// </summary>
        abstract public void ClearDBNotification();

        /// <summary>
        /// ���������� ���������� IDisposable, ������� ���������� ������ ������
        /// </summary>
        public abstract void Dispose();

    }

    /// <summary>
    /// ��������������� ����� ����������, ��� ���������� ���������
    /// </summary>
    [Serializable()]
    public class DBConnectException : Exception, ISerializable
    {
        /// <summary>
        /// ��� ����������, ������� ������ ����������
        /// </summary>
        public DBInterfaceType errConnectorType { get; set; }
        /// <summary>
        /// ����� �������, ���������� � ������
        /// </summary>
        public string errSqlText { get; set; }
        /// <summary>
        /// ��������� ������� ������ � ������� (����� ������� � �������)
        /// </summary>
        public int errSqlPos { get; set; }
        /// <summary>
        /// ���������� ��� ������ ���� (������� �� ���� �������)
        /// </summary>
        public string errSqlState { get; set; }
        /// <summary>
        /// ����� ������
        /// </summary>
        public string errMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        public DBConnectException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        /// <param name="message">���������, ����������� ������.</param>
        public DBConnectException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public DBConnectException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        /// <param name="info">������ <see cref="T:System.Runtime.Serialization.SerializationInfo" />, ���������� ��������������� ������ ������� � ��������� ����������.</param>
        /// <param name="context">������ <see cref="T:System.Runtime.Serialization.StreamingContext" />, ���������� ����������� �������� �� ��������� ��� ����������.</param>
        protected DBConnectException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        /// <summary>
        /// ��� ��������������� � ����������� ������ ������ ������ <see cref="T:System.Runtime.Serialization.SerializationInfo" /> �� ���������� �� ����������.
        /// </summary>
        /// <param name="info">������ <see cref="T:System.Runtime.Serialization.SerializationInfo" />, ���������� ��������������� ������ ������� � ��������� ����������.</param>
        /// <param name="context">������ <see cref="T:System.Runtime.Serialization.StreamingContext" />, ���������� ����������� �������� �� ��������� ��� ����������.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errConnectorType", errConnectorType);
            info.AddValue("errSqlText", errSqlText);
            info.AddValue("errSqlState", errSqlState);
            info.AddValue("errSqlPos", errSqlPos);
            info.AddValue("errMessage", errMessage);
        }
    }

}