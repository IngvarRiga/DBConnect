using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace plgDBConnect
{
  /// <summary>
  /// ���� ����������� � ������ ������
  /// </summary>
  public enum DBInterfaceType { PostgreSQL, SQLite, FireBird };
  /// <summary>
  /// ������ �������, ������� ��� ������������
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
    /// <param name="ParamName">��� ��������� � ���������� ���������� (@)</param>
    /// <param name="Val">�������� ���������</param>
    public abstract void AddParametersValue(IDbCommand Cmd, string ParamName, object Val);

    /// <summary>
    /// ���������� ��������� SQL ������� �� �������
    /// </summary>
    /// <param name="Cmd">�������������� �������</param>
    /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
    /// <returns>���������� ���������� ������������ ������� ��� -1 ��� ������</returns>
    public abstract int ExecuteNonQuery(IDbCommand Cmd, IDbTransaction tr = null);

    /// <summary>
    /// ���������� ��������� SQL ������ �� �������
    /// </summary>
    /// <param name="SQL">������, ���������� ������</param>
    /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
    /// <returns>���������� ���������� ������������ ������� ��� -1 ��� ������</returns>
    public abstract int ExecuteNonQuery(string SQL, IDbTransaction tr = null);

    /// <summary>
    ///  ���������� ��������, ������������ � ������ ������ ������� ���� ���������� ������� ���� ������
    /// </summary>
    /// <param name="SQL">����� SQL-������� � ��������� ���������� ��� �������������.</param>
    /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
    /// <returns></returns>
    public abstract object ExecuteScalar(string SQL, IDbTransaction tr = null);

    /// <summary>
    ///  ���������� ��������, ������������ � ������ ������ ������� ���� ���������� ������� ���� ������
    /// </summary>
    /// <param name="Cmd">�������������� �������</param>
    /// <param name="tr">����������, � ������ ������� ����������� ������, �� ��������� �� ������</param>
    /// <returns></returns>
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
    /// <returns>���������� ���������� ������� ��� -1 ��� ������</returns>
    public abstract long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr = null);

    /// <summary>
    /// ������ ����������
    /// </summary>
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
    /// �������� ���������� � ����� ������, � �������������� ������ ����������, ������������������ � �����������
    /// </summary>
    public abstract void Open();

    /// <summary>
    /// �������� ���������� � ����� ������, � �������������� ������ ����������, ������������������ � �����������
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

    public abstract Task<DbDataReader> ExecuteReaderAsync(IDbCommand Cmd);
   
    /// <summary>
    /// ��������� ���������� IDisposable, ������� ���������� ������ ������
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
    public int    errSqlPos { get; set; }
    /// <summary>
    /// ���������� ��� ������ ���� (������� �� ���� �������)
    /// </summary>
    public string errSqlState { get; set; }
    /// <summary>
    /// ����� ������
    /// </summary>
    public string errMessage { get; set; }

    public DBConnectException() { }
    public DBConnectException(string message) : base(message) { }
    public DBConnectException(string message, Exception inner) : base(message, inner) { }
    protected DBConnectException(SerializationInfo info, StreamingContext context) : base(info, context) { }
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