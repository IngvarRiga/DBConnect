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

    private NpgsqlConnectionStringBuilder csb; //-- формирование строки соединения
    private string ConnectString; //-- сформированная строка соединения
    private NpgsqlConnection db; //-- соединение с базой данных
    private NpgsqlTransaction tr; //-- транзакция
    private string LastSQL;

    #endregion

    //-----------------------------------------------------------------------------------------------------

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
      // csb.SyncNotification = true; //-- 
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

    public override string GetLastSQL()
    {
      return LastSQL;
    }

    public override DBInterfaceType GetConnectType()
    {
      return DBInterfaceType.PostgreSQL;
    }

    public override IDbCommand GetSQLCommand(string SQL)
    {
      if (string.IsNullOrEmpty(SQL)) throw new DBConnectException(Properties.Resources.errCommandNotDefined);
      return new NpgsqlCommand(SQL, db);
    }

    public override long FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr = null)
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

    public override long FillDataTable(DataTable tbl, string Cmd, IDbTransaction tr = null)
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

    public override IDbTransaction BeginTransaction()
    {
      if (db?.State == ConnectionState.Closed) return null;
      tr = db.BeginTransaction();
      return tr;
    }

    /// <summary>
    /// Подтверждение изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
    /// После подтверждения транзакция закрывается.
    /// </summary>
    public override void Commit()
    {
      if (db?.State == ConnectionState.Closed) return;
      if (tr?.Connection != null)
      {
        tr.Commit();
        tr.Dispose();
      }
    }

    /// <inheritdoc />
    /// <summary>
    /// Откат изменений, внесенных в БД операторами SQL. Работает только тогда, когда предварительно была открыта транзакция.
    /// После подтверждения транзакция закрывается.
    /// </summary>
    public override void Rollback()
    {
      if (db?.State != ConnectionState.Closed)
      {
        if (tr?.Connection != null)
        {
          tr.Rollback();
          tr.Dispose();
        }
      }
    }

    /// <summary>
    /// Открытие соединения с базой данных, с использованием строки соединения, инициализированной в контрукторе
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
      Rollback();
      if (db?.State == ConnectionState.Open)
      {
        db.Close();
      }
    }

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



    public override int ExecuteNonQuery(IDbCommand cmd, IDbTransaction tr = null)
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

    public override int ExecuteNonQuery(string SQL, IDbTransaction tr = null)
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

    public override object ExecuteScalar(string SQL, IDbTransaction tr = null)
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

    public override object ExecuteScalar(IDbCommand Cmd, IDbTransaction tr = null)
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
    
    public override IDataReader ExecuteReader(IDbCommand cmd)
    {
      IDataReader res = null;
      if (cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
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

    /*
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
          LastSQL = "Вызов функции Open()";
          ThrowException(ex);
        }
      }
    }



    public override Task<DbDataReader> ExecuteReaderAsync(IDbCommand cmd)
     {
       throw new NotImplementedException("ExecuteReaderAsync для PostgreSQL временно отключен");
       if (cmd == null) throw new DBConnectException(Properties.Resources.errCommandNotFormed);
       LastSQL = cmd.CommandText;
       try
       {
          return ((NpgsqlCommand)cmd).ExecuteReaderAsync();
       }
       catch (Exception ex)
       {
         ThrowException(ex);
       }
       return null;
     }*/
    #endregion
    #endregion
  }
}

#endif