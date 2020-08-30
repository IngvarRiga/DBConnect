using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
#if POSTGRESQL
using Npgsql;


namespace plgDBConnect
{
    /// <summary>
    ///  Класс, обеспечивающий соединение с БД PostgreSQL через интерфейсную библиотеку NpgSQL
    /// </summary>
    public class plgPGConnect : IplgDBConnect
    {
        #region Внутренние члены класса

        /// <summary>Сформированная строка соединения</summary>
        private NpgsqlConnectionStringBuilder csb;
        /// <summary>Сформированная строка соединения</summary>
        private string ConnectString;
        /// <summary>
        /// Соединение с базой данных
        /// </summary>
        private NpgsqlConnection db;
        /// <summary>
        /// Транзакция
        /// </summary>
        private NpgsqlTransaction tr;
        /// <summary>
        /// Строка, содержащая последний выполненный (или нет) запрос. Используется при отладке ошибок, чтобы понять, какой именно запрос вызвал ошибку.
        /// </summary>
        private string LastSQL;
        /// <summary>
        /// Ссылка на делегат, который вызывается при получении оповещения от сервера, что произошло событие
        /// </summary>
        EventLogger EventProc;

        #endregion

        //-----------------------------------------------------------------------------------------------------
        /// <inheritdoc/>
        public override void Dispose()
        {
            if (tr != null)
            {
                tr.Dispose();
                tr = null;
            }
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }


        /// <summary>
        /// Конструктор класса сразу инициализирует соединение с БД 
        /// </summary>
        /// <param name="Host">Хост на котором расположена база данных. Может задаваться как IP адрес</param>
        /// <param name="DB">Имя базы данных</param>
        /// <param name="Port">Номер порта, по которому сервер принимает запросы (по умолчанию 5432)</param>
        /// <param name="user">Имя пользователя</param>
        /// <param name="pass">Пароль</param>
        /// <param name="Timeout">Таймаут в секундах для ожидания ответа от сервера</param>
        public plgPGConnect(
          string Host, //-- имя компьютера или IP адрес сервера, "localhost" - сервер на локальной машине
          string DB, //-- имя базы данных или файла базы данных 
          int Port, //-- порт, по которому сервер слушает обращение к себе
          string user, //-- пользователь, под которым производится соединение
          string pass, //-- пароль пользователя
          int Timeout = 120)
        {
            LastSQL = string.Empty;
            //-- инициализация необходимых переменных 
            //-- настройка соединения с сервером и БД
            csb = new NpgsqlConnectionStringBuilder
            {
                Host = Host, //-- Хост, на котором развернута БД
                Port = Port, //-- Порт
                Database = DB, //-- база данных
                Username = user, //-- пользователь
                CommandTimeout = Timeout //-- время задается в секундах, по умолчанию - 120 секунд, а потом генерируется ошибка
            };
            //-- следующая строка устарела в новых версиях библиотеки
            //csb.SyncNotification = true; //-- 
            csb.Add("PASSWORD", pass);
            //-- инициализация строки подключения к базе данных
            ConnectString = csb.ToString();
            try
            {
                db = new NpgsqlConnection(ConnectString);
            }
            catch (Exception ex)
            {
                Dispose();
                ThrowException(ex);
            }
        }
        /// <inheritdoc/>
        private void ThrowException(Exception ex)
        {
            var nex = new DBConnectException(ex.Message, ex);
            var tip = nex.InnerException.GetType();
            if (tip.Name == "PostgresException")
            {
                //-- ошибка вызвана PostgreSQL
                nex.errConnectorType = DBInterfaceType.PostgreSQL;
                //-- запрос, вызвавший ошибку
                nex.errSqlText = ((PostgresException)(nex.InnerException)).Statement.SQL;
                //-- индекс ошибки СУБД
                nex.errSqlState = ((PostgresException)(nex.InnerException)).SqlState;
                //-- примерная позиция ошибки
                nex.errSqlPos = ((PostgresException)(nex.InnerException)).Position;
                //-- текст ошибки
                nex.errMessage = ex.Message;
            }
            throw nex;
        }

        //---------------------------------------------------------------------------

        #region -- Реализация интерфейса IplgDBConnect --

        /// <inheritdoc/>
        public override string GetLastSQL()
        {
            return LastSQL;
        }

        /// <inheritdoc/>
        public override DBInterfaceType GetConnectType()
        {
            return DBInterfaceType.PostgreSQL;
        }

        /// <inheritdoc/>
        public override IDbCommand GetSQLCommand(string SQL)
        {
            if (string.IsNullOrEmpty(SQL))
                throw new DBConnectException(Properties.Resources.errCommandNotDefined);
            return new NpgsqlCommand(SQL, db);
        }

        /// <inheritdoc/>
        public override long FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr)
        {
            long res = -1;
            if (tbl == null) throw new DBConnectException(Properties.Resources.errDataTableNotDefined);
            if (Cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (tr == null) throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            //-- перед заполнением таблицы - очистка
            tbl.Clear();
            Cmd.Transaction = tr;
            try
            {
                LastSQL = Cmd.CommandText;
                using (var ad = new NpgsqlDataAdapter((NpgsqlCommand)Cmd))
                {
                    res = ad.Fill(tbl);
                }
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return res;
        }

        /// <inheritdoc/>
        public override long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr)
        {
            long res = -1;
            if (tbl == null) throw new DBConnectException(Properties.Resources.errDataTableNotDefined);
            if (string.IsNullOrEmpty(Cmd)) throw new DBConnectException(Properties.Resources.errCommandNotDefined);
            if (tr == null) throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            //-- перед заполнением таблицы - очистка
            tbl.Clear();
            try
            {
                LastSQL = Cmd;
                using (var SQLEx = new NpgsqlCommand(Cmd, db, (NpgsqlTransaction)tr))
                {
                    using (var ad = new NpgsqlDataAdapter(SQLEx))
                    {
                        res = ad.Fill(tbl);
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return res;
        }
        /// <inheritdoc/>
        public override IDbTransaction BeginTransaction()
        {
            if (db?.State == ConnectionState.Closed)
                throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            tr = db.BeginTransaction();
            return tr;
        }

        /// <summary>
        /// Подтверждение изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
        /// После подтверждения транзакция закрывается.
        /// </summary>
        public override void Commit()
        {
            if (db?.State == ConnectionState.Closed)
                throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            if (tr?.Connection != null)
            {
                tr.Commit();
                tr.Dispose();
            }
        }

        /// <summary>
        /// Откат изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
        /// После подтверждения транзакция закрывается.
        /// </summary>
        public override void Rollback()
        {
            if (db?.State == ConnectionState.Closed)
                throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            if (tr?.Connection != null)
            {
                tr.Rollback();
                tr.Dispose();
            }
        }

        /// <summary>
        /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в конструкторе
        /// </summary>
        public override void Open()
        {
            if (db?.State != ConnectionState.Closed) return;
            try
            {
                db.Open();
            }
            catch (Exception ex)
            {
                LastSQL = "Вызов функции Open()";
                ThrowException(ex);
            }
        }

        /// <summary>
        /// Закрытие соединения с базой данных.
        /// </summary>
        public override void Close()
        {
            if (db?.State == ConnectionState.Open)
            {
                Rollback();
                db.Close();
            }
        }

        /// <inheritdoc/>
        public override void AddParametersValue(IDbCommand Cmd, string ParamName, object Val)
        {
            if (Cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (!((NpgsqlCommand)Cmd).Parameters.Contains(ParamName))
            {
                ((NpgsqlCommand)Cmd).Parameters.AddWithValue(ParamName, Val);
            }
            else
                throw new DBConnectException(string.Format(Properties.Resources.errParametrAlreadyDefined, ParamName));
        }

        /// <inheritdoc/>
        public override int ExecuteNonQuery(IDbCommand cmd, IDbTransaction tr)
        {
            var res = -1;
            if (cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (db.State == ConnectionState.Closed) throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            cmd.Transaction = tr ?? throw new Exception(Properties.Resources.errTransactionNotDefined);
            LastSQL = cmd.CommandText;
            try
            {
                res = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return res;
        }

        /// <inheritdoc/>
        public override int ExecuteNonQuery(string SQL, IDbTransaction tr)
        {
            var Res = -1;
            if (string.IsNullOrEmpty(SQL)) throw new DBConnectException(Properties.Resources.errCommandNotDefined);
            if (db.State == ConnectionState.Closed) throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            if (tr == null) throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            try
            {
                LastSQL = SQL;
                using (var cmd = new NpgsqlCommand(SQL, db, (NpgsqlTransaction)tr))
                {
                    Res = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return Res;
        }

        /// <inheritdoc/>
        public override object ExecuteScalar(string SQL, IDbTransaction tr)
        {
            object Res = null;
            if (string.IsNullOrEmpty(SQL)) throw new DBConnectException(Properties.Resources.errCommandNotDefined);
            if (db.State == ConnectionState.Closed) throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            if (tr == null) throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            try
            {
                LastSQL = SQL;
                using (var cmd = new NpgsqlCommand(SQL, db, (NpgsqlTransaction)tr))
                {
                    Res = cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return Res;
        }

        /// <inheritdoc/>
        public override object ExecuteScalar(IDbCommand Cmd, IDbTransaction tr)
        {
            object res = null;
            if (Cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (db.State == ConnectionState.Closed) throw new DBConnectException(Properties.Resources.errDataBaseNotConnected);
            Cmd.Transaction = tr ?? throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            LastSQL = Cmd.CommandText;
            try
            {
                res = Cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return res;
        }

        /// <inheritdoc/>
        public override IDataReader ExecuteReader(IDbCommand cmd, IDbTransaction tr)
        {
            IDataReader res = null;
            if (cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            cmd.Transaction = tr ?? throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            LastSQL = cmd.CommandText;
            try
            {
                res = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return res;
        }
        #region -- Функции асинхронной работы с базой данных


        /// <inheritdoc/>
        public override async void OpenAsync()
        {
            if (db?.State == ConnectionState.Closed)
            {
                try
                {
                    await db.OpenAsync();
                }
                catch (Exception ex)
                {
                    LastSQL = "Вызов асинхронной функции Open()";
                    ThrowException(ex);
                }
            }
        }

        /// <inheritdoc/>
        public override async Task<DbDataReader> ExecuteReaderAsync(IDbCommand cmd, IDbTransaction tr)
        {
            if (cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            cmd.Transaction = tr ?? throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            LastSQL = cmd.CommandText;
            try
            {
                return await ((NpgsqlCommand)cmd).ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                ThrowException(ex);
            }
            return null;
        }

        /// <inheritdoc/>
        public override void SetDBNotification(EventLogger logger = null)
        {
            if (logger == null)
                throw new ArgumentNullException("Не задана функция обработки сообщений сервера");
            EventProc = logger;
            db.Notification += Db_Notification;
        }

        /// <inheritdoc/>
        public override void ClearDBNotification()
        {
            EventProc = null;
            db.Notification -= Db_Notification;
            //db.ClearPool();
        }

        /// <summary>
        /// Функция, вызываемая при возникновении оповещения от сервера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Db_Notification(object sender, NpgsqlNotificationEventArgs e)
        {
            EventProc?.Invoke(e.PID, e.Channel, e.Payload);
        }
        #endregion
        #endregion
    }
}

#endif