using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace plgDBConnect
{
    /// <summary>
    /// Делегат, определяющий тип функции, вызываемой при получении оповещения от сервера БД.
    /// Используется в основном для POstgreSQL. Реализация SQLite - не предусмотрена, поскольку эта БД не поддерживает сообщения. FireBird - не реализована
    /// (небыло необходимости).</summary>
    /// <param name="PID">Идентификатор процесса (Process ID). В условиях C# - не играет особого смысла, поскольку работы происходит подключил/сделал/отключил
    /// так что даже в одной программе будут разные PID. Имеет смысл только тогда, когда соединение открывается на старте программы и закрывается при выходе.</param>
    /// <param name="channel">Канал (имя сообщения)</param>
    /// <param name="addinfo">Дополнительные параметры сообщения. PostgreSQL позволяет вместе с каналом передавать дополнительную информацию, которая существенно упрощает обмен данными.</param>
    /// <remarks>Основная проблема при работе с СУБД при многопользовательском доступе - это определить, что данные изменились и оперативно уведомить подключенных
    /// к БД клиентов, чтобы они смогли обновить свои данные. Вопрос правильности (или нет) такого подхода не рассматривается.</remarks>
    public delegate void EventLogger(long PID, string channel, string addinfo);

    /// <summary>
    /// Типы интерфейсов с базами данных
    /// <list type="number">
    /// <item>SQLite</item>
    /// <item>PostgreSQL</item>
    /// <item>FireBird (отключен от реализации)</item>
    ///</list>
    /// </summary>
    public enum DBInterfaceType
    {
        /// <summary>
        /// Неизвестный тип сервера
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
    /// Статус сервера, обычный или встраиваемый (последний только для FireBird)
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
        /// Установка значения параметра сформированной SQL-команды перед ее выполнением
        /// </summary>
        /// <param name="Cmd">Сформированная команда, содержащая в составе параметры заданные в виде @PARNAME</param>
        /// <param name="ParamName">Имя параметра с лидирующим амперсандом @</param>
        /// <param name="Val">Значение параметра</param>
        public abstract void AddParametersValue(IDbCommand Cmd, string ParamName, object Val);

        /// <summary>
        /// Немедленно выполняет SQL команду на сервере
        /// </summary>
        /// <param name="Cmd">Сформированная команда</param>
        /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
        /// <returns>Количество обработанных записей или -1 при ошибке</returns>
        public abstract int ExecuteNonQuery(IDbCommand Cmd, IDbTransaction tr = null);

        /// <summary>
        /// Немедленно выполняет SQL запрос на сервере
        /// </summary>
        /// <param name="SQL">Строка, содержащая запрос</param>
        /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
        /// <returns>Количество обработанных записей или -1 при ошибке</returns>
        public abstract int ExecuteNonQuery(string SQL, IDbTransaction tr = null);

        /// <summary>
        ///  Возвращает значение, содержащееся в первой строке первого поля указанного объекта базы данных
        /// </summary>
        /// <param name="SQL">Текст SQL-запроса с указанием параметров при необходимости.</param>
        /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
        /// <returns>Полученное значение</returns>
        public abstract object ExecuteScalar(string SQL, IDbTransaction tr = null);

        /// <summary>
        ///  Возвращает значение, содержащееся в первой строке первого поля указанного объекта базы данных
        /// </summary>
        /// <param name="Cmd">Сформированная команда</param>
        /// <param name="tr">Транзакция, в рамках которой выполняется запрос, по умолчанию не задана</param>
        /// <returns>Полученное значение</returns>
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
        /// <returns>Количество записей или -1 при ошибке</returns>
        public abstract long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr = null);

        /// <summary>
        /// Начать транзакцию
        /// </summary>
        /// <returns>Объект начатой транзакции или NULL если не поддерживается или соединение с БД закрыто</returns>
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
        /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в конструкторе
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в конструкторе
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
        /// <summary>
        /// Асинхронное чтение подготовленного запроса
        /// </summary>
        /// <param name="Cmd"></param>
        /// <returns></returns>
        public abstract Task<DbDataReader> ExecuteReaderAsync(IDbCommand Cmd);
        /// <summary>
        /// Установка делегата оповещения о присланном сервером событии
        /// </summary>
        /// <param name="logger"></param>
        abstract public void SetDBNotification(EventLogger logger = null);
        /// <summary>
        /// Отключение от системы оповещения сервера при закрытии соединения
        /// </summary>
        abstract public void ClearDBNotification();

        /// <summary>
        /// Реализация интерфейса IDisposable, очистка внутренних данных класса
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
        public int errSqlPos { get; set; }
        /// <summary>
        /// Внутренний код ошибки СУБД (зависит от типа сервера)
        /// </summary>
        public string errSqlState { get; set; }
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string errMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        public DBConnectException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DBConnectException"/> class.
        /// </summary>
        /// <param name="message">Сообщение, описывающее ошибку.</param>
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
        /// <param name="info">Объект <see cref="T:System.Runtime.Serialization.SerializationInfo" />, содержащий сериализованные данные объекта о созданном исключении.</param>
        /// <param name="context">Объект <see cref="T:System.Runtime.Serialization.StreamingContext" />, содержащий контекстные сведения об источнике или назначении.</param>
        protected DBConnectException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        /// <summary>
        /// При переопределении в производном классе задает объект <see cref="T:System.Runtime.Serialization.SerializationInfo" /> со сведениями об исключении.
        /// </summary>
        /// <param name="info">Объект <see cref="T:System.Runtime.Serialization.SerializationInfo" />, содержащий сериализованные данные объекта о созданном исключении.</param>
        /// <param name="context">Объект <see cref="T:System.Runtime.Serialization.StreamingContext" />, содержащий контекстные сведения об источнике или назначении.</param>
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