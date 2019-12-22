using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace plgDBConnect
{
  /// <summary>
  /// Типы интерфейсов с базами данных
  /// </summary>
  public enum DBInterfaceType { PostgreSQL, SQLite, FireBird };
  /// <summary>
  /// Статус сервера, обычный или встраиваемый
  /// </summary>
  public enum DBServerStateType { Standart, Embedded };
  /// <summary>
  /// Интерфейс работы с базой данных, реализуемый через абстрактный класс
  /// </summary>
  public abstract class IplgDBConnect : IDisposable
  {
    /// <summary>
    /// Функция возвращает последний выполняемый запрос в рамках данного соединения
    /// </summary>
    /// <returns>Строка, содержащая последний выполняемый запрос</returns>
    public abstract string GetLastSQL();

    /// <summary>
    /// Функция возвращает тип соединения, реализуемого интерфейсом
    /// </summary>
    /// <returns>Тип соединения</returns>
    public abstract DBInterfaceType GetConnectType();

    /// <summary>
    /// Возвращает сформированную SQL команду, готовую к заполнению параметрами или непосредственно выполнению
    /// </summary>
    /// <param name="SQL">Текст SQL-запроса с указанием параметров при необходимости. Параметр указывается в строке запроса с префиксом @</param>
    /// <returns>Сформированная SQL команда типа IDbCommand</returns>
    public abstract IDbCommand GetSQLCommand(string SQL);

    /// <summary>
    /// Установка значения параметра сформированной SQL-соманды перед ее выполнением
    /// </summary>
    /// <param name="Cmd">Сформированная команда, содержащая в составе параметры заданные в виде @PARNAME</param>
    /// <param name="ParamName">Имя параметра с лидирующим амперсанда (@)</param>
    /// <param name="Val">Значение параметра</param>
    public abstract void AddParametersValue(IDbCommand Cmd, string ParamName, object Val);

    /// <summary>
    /// Немедленно выполняет SQL команду на сервере
    /// </summary>
    /// <param name="Cmd">Сформированная команда</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns>Возвращает количество обработанных записей или -1 при ошибке</returns>
    public abstract int ExecuteNonQuery(IDbCommand Cmd, IDbTransaction tr = null);

    /// <summary>
    /// Немедленно выполняет SQL запрос на сервере
    /// </summary>
    /// <param name="SQL">Строка, содержащая запрос</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns>Возвращает количество обработанных записей или -1 при ошибке</returns>
    public abstract int ExecuteNonQuery(string SQL, IDbTransaction tr = null);

    /// <summary>
    ///  Возвращает значение, содержащееся в первой строке первого поля указанного объекта базы данных
    /// </summary>
    /// <param name="SQL">Текст SQL-запроса с указанием параметров при необходимости.</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns></returns>
    public abstract object ExecuteScalar(string SQL, IDbTransaction tr = null);

    /// <summary>
    ///  Возвращает значение, содержащееся в первой строке первого поля указанного объекта базы данных
    /// </summary>
    /// <param name="Cmd">Сформированная команда</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns></returns>
    public abstract object ExecuteScalar(IDbCommand Cmd, IDbTransaction tr = null);

    /// <summary>
    /// Очищает содержимое указанной таблицы и заполняет ее данными получаемыми из запроса
    /// </summary>
    /// <param name="tbl">Имя таблицы</param>
    /// <param name="Cmd">Сформированная команда</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns>Возвращает количество записей или -1 при ошибке</returns>
    public abstract long FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr = null);

    /// <summary>
    /// Очищает содержимое указанной таблицы и заполняет ее данными получаемыми из запроса
    /// </summary>
    /// <param name="tbl">Имя таблицы</param>
    /// <param name="Cmd">Строка, содержащая запрос</param>
    /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
    /// <returns>Возвращает количество записей или -1 при ошибке</returns>
    public abstract long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr = null);

    /// <summary>
    /// Начать транзакцию
    /// </summary>
    public abstract IDbTransaction BeginTransaction();

    /// <summary>
    /// Подтверждение изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
    /// После подтверждения транзакция закрывается.
    /// </summary>
    public abstract void Commit();

    /// <summary>
    /// Откат изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
    /// После подтверждения транзакция закрывается.
    /// </summary>
    public abstract void Rollback();

    /// <summary>
    /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в контрукторе
    /// </summary>
    public abstract void Open();

    /// <summary>
    /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в контрукторе
    /// </summary>
    public abstract void OpenAsync();
   
    /// <summary>
    /// Закрытие соединения с базой данных.
    /// </summary>
    public abstract void Close();

    /// <summary>
    /// Запрашивает DataReader на подготовленный запрос
    /// </summary>
    /// <returns>Интерфейс IDataReader</returns>
    public abstract IDataReader ExecuteReader(IDbCommand Cmd);

    public abstract Task<DbDataReader> ExecuteReaderAsync(IDbCommand Cmd);
   
    /// <summary>
    /// Рализация интерфейса IDisposable, очистка внутренних данных класса
    /// </summary>
    public abstract void Dispose();

  }

  /// <summary>
  /// Переопределённый класс исключений, для унификации обработки
  /// </summary>
  [Serializable()]
  public class DBConnectException : Exception, ISerializable
  {
    /// <summary>
    /// Тип коннектора, который вызвал исключение
    /// </summary>
    public DBInterfaceType errConnectorType { get; set; }
    /// <summary>
    /// Текст запроса, приведшего к ошибке
    /// </summary>
    public string errSqlText { get; set; }
    /// <summary>
    /// Примерная позиция ошибки в запросе (номер символа в запросе)
    /// </summary>
    public int    errSqlPos { get; set; }
    /// <summary>
    /// Внутренний код ошибки СУБД (зависит от типа сервера)
    /// </summary>
    public string errSqlState { get; set; }
    /// <summary>
    /// Текст ошибки
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