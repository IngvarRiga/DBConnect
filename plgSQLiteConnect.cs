using System;
using System.Data;
using System.IO;
using System.Data.Common;
using System.Threading.Tasks;

//-- использование библиотек доступа к SQLite
#if SQLITE
using System.Data.SQLite;


namespace plgDBConnect
{
    /// <summary>
    /// Класс, обеспечивающий соединение с БД SQLite через интерфейсную библиотеку
    /// </summary>
    public class plgSQLiteConnect : IplgDBConnect
    {

        #region Внутренние члены класса

        SQLiteConnectionStringBuilder csb; //-- формирование строки соединения
        string ConnectString; //-- сформированная строка соединения
        SQLiteConnection db; //-- соединение с базой данных
        SQLiteTransaction tr; //-- транзакция
        string LastSQL;

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

        //-- Конструктор класса сразу инициализирует соединение с БД
        /// <inheritdoc/>
        public plgSQLiteConnect(
          string DB, //-- пользователь, под которым производится соединение
          string pass, //-- пароль декодирования БД
          int Timeout = 120)
        {
            //-- инициализация необходимых переменных
            csb = new SQLiteConnectionStringBuilder();
            //-- настройка соединения с сервером и БД
            //   csb.AddHost = Host;     //-- Хост, на котором развернута БД
            //   csb.Port = Port;     //-- Порт
            csb.DataSource = DB; //-- полный путь до БД
                                 //csb.UserName = user; //-- пользователь
            csb.DefaultTimeout = Timeout;
            //-- время задается в секундах, по умолчанию - 120 секунд, а потом генерируется ошибка
            // csb.SyncNotification = true; //--
            if (!string.IsNullOrEmpty(pass))
            {
                csb.Password = pass; //-- Пароль кодирования / декодирования БД
            }
            //-- инициализация строки подключения к базе данных
            ConnectString = csb.ToString();
            if (!File.Exists(DB))
            {
                //-- файла не существует!
                throw new ArgumentException($"База данных SQLite « {DB} » не найдена");
            }
            try
            {
                db = new SQLiteConnection(ConnectString);
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
            if (tip.Name == "SQLiteException")
            {
                //-- ошибка вызвана PostgreSQL
                nex.errConnectorType = DBInterfaceType.SQLite;
                //-- запрос, вызвавший ошибку
                nex.errSqlText = LastSQL;
                //-- индекс ошибки СУБД
                nex.errSqlState = ((SQLiteException)(nex.InnerException)).ErrorCode.ToString();
                //-- примерная позиция ошибки
                //-- в SQLite позиция ошибки не определяется...
                nex.errSqlPos = -1;
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
            return DBInterfaceType.SQLite;
        }

        /// <inheritdoc/>
        public override IDbCommand GetSQLCommand(string SQL)
        {
            if (SQL == string.Empty) throw new DBConnectException(Properties.Resources.errCommandNotDefined);
            return new SQLiteCommand(SQL, db);
        }

        /// <inheritdoc/>
        public override long FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr)
        {
            long res = -1;
            if (tbl == null) throw new DBConnectException(Properties.Resources.errDataTableNotDefined);
            if (Cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            //-- перед заполнением таблицы - очистка
            tbl.Clear();
            Cmd.Transaction = tr ?? throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            try
            {
                LastSQL = Cmd.CommandText;
                using (var ad = new SQLiteDataAdapter((SQLiteCommand)Cmd))
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
            if (string.IsNullOrEmpty(Cmd)) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (tr == null) throw new DBConnectException(Properties.Resources.errTransactionNotDefined);
            //-- перед заполнением таблицы - очистка
            tbl.Clear();
            try
            {
                LastSQL = Cmd;
                using (var SQLEx = new SQLiteCommand(Cmd, db, (SQLiteTransaction)tr))
                {
                    using (var ad = new SQLiteDataAdapter(SQLEx))
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
            IDbTransaction res = null;
            if ((db.State != ConnectionState.Closed))
            {
                tr = db.BeginTransaction();
                res = tr;
            }
            return res;
        }

        /// <summary>
        /// Подтверждение изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
        /// После подтверждения транзакция закрывается.
        /// </summary>
        public override void Commit()
        {
            if (db == null) { return; }
          ;
            if (db.State != ConnectionState.Closed)
            {
                if (tr != null)
                {
                    tr.Commit();
                    tr.Dispose();
                }
            }
        }

        /// <summary>
        /// Откат изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
        /// После подтверждения транзакция закрывается.
        /// </summary>
        public override void Rollback()
        {
            if (db == null) { return; }
            if (db.State != ConnectionState.Closed)
            {
                /*        if (tr != null)
                        {
                          tr.Rollback();
                          tr.Dispose();
                        }*/
            }
        }

        /// <summary>
        /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в конструкторе
        /// </summary>
        public override void Open()
        {
            if (db?.State == ConnectionState.Closed)
            {
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
        }

        /// <summary>
        /// Закрытие соединения с базой данных.
        /// </summary>
        public override void Close()
        {
            Rollback();
            if (db?.State == ConnectionState.Open)
            {
                db.Close();
            }
        }

        /// <inheritdoc/>
        public override void AddParametersValue(IDbCommand Cmd, string ParamName, object Val)
        {
            if (Cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
            if (!((SQLiteCommand)Cmd).Parameters.Contains(ParamName))
            {
                ((SQLiteCommand)Cmd).Parameters.AddWithValue(ParamName, Val);
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
                using (var cmd = new SQLiteCommand(SQL, db, (SQLiteTransaction)tr))
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
                using (var cmd = new SQLiteCommand(SQL, db, (SQLiteTransaction)tr))
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
                return await ((SQLiteCommand)cmd).ExecuteReaderAsync();
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
            //-- Возможность обработки событий не поддерживается!
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void ClearDBNotification()
        {
            //-- Возможность обработки событий не поддерживается!
            throw new NotImplementedException();
        }



        #endregion

        #endregion
    }
}

#endif
