using System;
using System.IO;
using System.Data;

//-- использование библиотек доступа к Firebird
using plgService;

#if FIREBIRD

using FirebirdSql.Data.FirebirdClient;

namespace plgDBConnect
{
  /// <summary>
  /// Класс, обеспечивающий соединение с БД Firebird через интерфейсную библиотеку FirebirdSql
  /// </summary>
  public class plgFBConnect : IplgDBConnect
  {

    #region Внутренние члены класса

    FbConnectionStringBuilder csb; //-- формирование строки соединения
    string ConnectString; //-- сформированная строка соединения
    FbConnection db; //-- соединение с базой данных
    FbTransaction tr; //-- транзакция
    String LastSQL;

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
    public plgFBConnect(
      string DB, //-- имя базы данных или файла базы данных 
      string user = "sysdba", //-- пользователь, под которым производится соединение
      string pass = "masterkey", //-- пароль пользователя
      DBServerStateType ServerStateType = DBServerStateType.Standart,
      string Host = "", //-- имя компьютера или IP адрес сервера, "" - сервер на локальной машине
      int Port = 3050, //-- порт, по которому сервер слушает обращение к себе
      int Timeout = 120)
    {
      LastSQL = String.Empty;
      if (!File.Exists(DB))
      {
        ServiceWins.ShowError(new FileNotFoundException(String.Format("Файл {0} не найден.", DB)));
        this.Dispose();
        return;
      }
      //-- инициализация необходимых переменных 
      csb = new FbConnectionStringBuilder();
      //-- настройка соединения с сервером и БД
//   csb.Host = Host; //-- Хост, на котором развернута БД
      //  csb.Port = Port; //-- Порт
      csb.Database = DB; //-- база данных
      csb.UserID = user; //-- пользователь
      csb.IsolationLevel = IsolationLevel.ReadCommitted;
      csb.Port = Port;
      csb.Charset = "UTF8";
      csb.Dialect = 3;
      csb.ConnectionTimeout = Timeout;
        //-- время задается в секундах, по умолчанию - 20 секунд, а потом генерируется ошибка
      csb.Password = pass;
      //-- тип сервера (Embedded - переносной (portable)
      if (ServerStateType == DBServerStateType.Standart)
      {
        csb.ServerType = FbServerType.Default;
      }
      else
      {
        csb.ServerType = FbServerType.Embedded;
      }
      //-- инициализация строки подключения к базе данных
      ConnectString = csb.ToString();
      try
      {
        db = new FbConnection(ConnectString);
      }
      catch (Exception ex)
      {
        ServiceWins.ShowError(ex);
        Dispose();
      }
    }

    public plgFBConnect()
    {
      LastSQL = String.Empty;
      //-- инициализация необходимых переменных 
      csb = new FbConnectionStringBuilder();
      //-- настройка соединения с сервером и БД
      //csb.Host = DM.Host; //-- Хост, на котором развернута БД
      csb.Database = DM.Database; //-- база данных
      csb.UserID = DM.User; //-- пользователь
      csb.IsolationLevel = IsolationLevel.ReadCommitted;
      csb.Port = DM.Port;
      csb.Dialect = 3;
      csb.ConnectionTimeout = DM.Timeout;
        //-- время задается в секундах, по умолчанию - 20 секунд, а потом генерируется ошибка
      csb.Password = DM.Password;
      //-- тип сервера (Embedded - переносной (portable)
      csb.ServerType = FbServerType.Embedded;
      //-- csb.ServerType = FbServerType.Default;

      //-- инициализация строки подключения к базе данных
      ConnectString = csb.ToString();
      try
      {
        db = new FbConnection(ConnectString);
      }
      catch (Exception ex)
      {
        ServiceWins.ShowError(ex);
        Dispose();
      }
    }

    //-----------------------------------------------------------------------------------------------------

    #region -- Реализация интерфейса IplgDBConnect --

    public override String GetLastSQL()
    {
      return LastSQL;
    }

    public override DBInterfaceType GetConnectType()
    {
      return DBInterfaceType.FireBird;
    }

    public override IDbCommand GetSQLCommand(string SQL)
    {
      FbCommand res = null;
      if (SQL == String.Empty)
      {
        throw new Exception(DM.errInfo[0]);
      }
      else
      {
        res = new FbCommand(SQL, db);
      }
      return res;
    }

    public override Int64 FillDataTable(DataTable tbl, IDbCommand Cmd, IDbTransaction tr)
    {
      Int64 res = -1;
      if (Cmd == null)
      {
        throw new Exception(DM.errInfo[1]);
      }
      if (tr == null)
      {
        throw new Exception(DM.errInfo[6]);
      }
      else
      {
        //-- перед заполнением таблицы - очистка
        tbl.Clear();
        Cmd.Transaction = tr;
        using (FbDataAdapter ad = new FbDataAdapter((FbCommand) Cmd))
        {
          LastSQL = Cmd.CommandText;
          res = ad.Fill(tbl);
        }
      }
      return res;
    }

    public override Int64 FillDataTable(DataTable tbl, String Cmd, IDbTransaction tr)
    {
      Int64 res = -1;
      if (Cmd == String.Empty)
      {
        throw new Exception(DM.errInfo[0]);
      }
      if (tr == null)
      {
        throw new Exception(DM.errInfo[6]);
      }
      else
      {
        //-- перед заполнением таблицы - очистка
        tbl.Clear();
        using (FbCommand SQLex = new FbCommand(Cmd, db, (FbTransaction) tr))
        {
          using (FbDataAdapter ad = new FbDataAdapter(SQLex))
          {
            LastSQL = Cmd;
            res = ad.Fill(tbl);
          }
        }
      }
      return res;
    }

    public override bool InsUpdRowDataTable(DataTable tbl, DataRowView row, String ins_SQL, String upd_SQL,
      String[] fields)
    {
      bool res = false;
      if (tbl != null && row != null && ins_SQL != String.Empty && upd_SQL != String.Empty && fields != null)
      {
        using (FbCommand ins_cmd = new FbCommand(ins_SQL, db), upd_cmd = new FbCommand(upd_SQL, db))
        {
          using (FbDataAdapter ad = new FbDataAdapter())
          {
            ad.InsertCommand = ins_cmd;
            ad.UpdateCommand = upd_cmd;
            for (int i = 0; i < fields.Length; i++)
            {
              ad.InsertCommand.Parameters.AddWithValue("@" + fields[i], row[fields[i]]);
              ad.UpdateCommand.Parameters.AddWithValue("@" + fields[i], row[fields[i]]);
            }
            ad.Update(tbl);
          }
          res = true;
        }
      }
      else
      {
        if (tbl != null)
        {
          throw new Exception(DM.errInfo[2]);
        }
        if (row != null)
        {
          throw new Exception(DM.errInfo[3]);
        }
        if (ins_SQL == String.Empty)
        {
          throw new Exception(DM.errInfo[0] + " (добавление данных)");
        }
        if (upd_SQL == String.Empty)
        {
          throw new Exception(DM.errInfo[0] + " (обновление данных)");
        }
        if (fields != null)
        {
          throw new Exception(DM.errInfo[4]);
        }
      }
      return res;
    }

    public override bool DelRowDataTable(DataTable tbl, DataRowView row, String del_SQL, String[] fields)
    {
      bool res = false;
      if (tbl != null && row != null && del_SQL != String.Empty && fields != null)
      {
        using (FbCommand del_cmd = new FbCommand(del_SQL, db))
        {
          using (FbDataAdapter ad = new FbDataAdapter())
          {
            ad.DeleteCommand = del_cmd;
            for (int i = 0; i < fields.Length; i++)
            {
              ad.DeleteCommand.Parameters.AddWithValue("@" + fields[i], row[fields[i]]);
            }
            ad.Update(tbl);
          }
        }
        res = true;
      }
      else
      {
        if (tbl != null)
        {
          throw new Exception(DM.errInfo[2]);
        }
        if (row != null)
        {
          throw new Exception(DM.errInfo[3]);
        }
        if (del_SQL == String.Empty)
        {
          throw new Exception(DM.errInfo[0] + " (удаление данных)");
        }
        if (fields != null)
        {
          throw new Exception(DM.errInfo[4]);
        }
      }
      return res;
    }

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
        if ((tr != null) && (tr.Connection != null))
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
      ;
      if (db.State != ConnectionState.Closed)
      {
        if ((tr != null) && (tr.Connection != null))
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
      if (db.State == ConnectionState.Closed)
      {
        db.Open();
      }
    }

    /// <summary>
    /// Закрытие соединения с базой данных.
    /// </summary>
    public override void Close()
    {
      Rollback();
      if (db != null)
      {
        if (db.State == ConnectionState.Open)
        {
          db.Close();
        }
        /// В случае, если открытие/закрытие БД происходит во время цикла, то 
        /// удаление объекта приведет к ошибке

        //   db.Dispose();
        //   db = null;
      }
      /*  if (tr != null)  { tr.Dispose(); tr = null; }*/
    }

    public override void AddPartametersValue(IDbCommand Cmd, string ParamName, object Val)
    {
      if (Cmd == null)
      {
        throw new Exception(DM.errInfo[1]);
      }
      else
      {
        ((FbCommand) Cmd).Parameters.AddWithValue(ParamName, Val);
      }
    }

    public override IDataReader ExecuteReader(IDbCommand Cmd)
    {
      if (Cmd == null)
      {
        throw new Exception(DM.errInfo[1]);
      }
      else
      {
        LastSQL = Cmd.CommandText;
        return Cmd.ExecuteReader();
      }
    }

    public override IDataReader ExecuteReader(String SQL)
    {
      FbDataReader res = null;
      if (SQL == String.Empty)
      {
        throw new Exception(DM.errInfo[0]);
      }
      else
      {
        using (FbCommand Cmd = new FbCommand(SQL, db))
        {
          LastSQL = SQL;
          res = Cmd.ExecuteReader();
        }
      }
      return res;
    }

    public override int ExecuteNonQuery(IDbCommand Cmd, IDbTransaction tr = null)
    {
      if (Cmd == null)
        throw new Exception(DM.errInfo[1]);
      if (tr == null)
        throw new Exception(DM.errInfo[6]);
      else
      {
        Cmd.Transaction = tr;
        if (db.State == ConnectionState.Closed)
          throw new Exception(DM.errInfo[5]);
        else
        {
          LastSQL = Cmd.CommandText;
          return Cmd.ExecuteNonQuery();
        }
      }
    }

    public override int ExecuteNonQuery(string SQL, IDbTransaction tr = null)
    {
      int res = -1;
      if (SQL == String.Empty)
      {
        throw new Exception(DM.errInfo[0]);
      }
      if (tr == null)
      {
        throw new Exception(DM.errInfo[6]);
      }
      else
      {
        if (db.State == ConnectionState.Closed)
          throw new Exception(DM.errInfo[5]);
        else
        {
          using (FbCommand Cmd = new FbCommand(SQL, db, (FbTransaction) tr))
          {
            LastSQL = SQL;
            res = Cmd.ExecuteNonQuery();
          }
        }
      }
      return res;
    }

    public override object ExecuteScalar(string SQL, IDbTransaction tr = null)
    {
      object res = null;
      if (SQL == String.Empty)
      {
        throw new Exception(DM.errInfo[0]);
      }
      if (tr == null)
      {
        throw new Exception(DM.errInfo[6]);
      }
      else
      {
        if (db.State == ConnectionState.Closed)
          throw new Exception(DM.errInfo[5]);
        else
        {
          using (FbCommand Cmd = new FbCommand(SQL, db, (FbTransaction) tr))
          {
            LastSQL = SQL;
            res = Cmd.ExecuteScalar();
          }
        }
      }
      return res;
    }

    public override object ExecuteScalar(IDbCommand Cmd, IDbTransaction tr = null)
    {
      object Res = null;
      if (Cmd == null)
        throw new Exception(DM.errInfo[1]);
      else
      {
        if (db.State == ConnectionState.Closed)
          throw new Exception(DM.errInfo[5]);
        else
        {
          Cmd.Transaction = tr;
          LastSQL = Cmd.CommandText;
          Res = Cmd.ExecuteScalar();
        }
      }
      return Res;
    }

    public override bool CheckUserExist(String User)
    {
      /* bool res = false;
   try
   {
    Open();
    object data = ExecuteScalar(String.Format("select count(*) from pg_user where usename='{0}'", User));
    if (data.ToString()=="1") { res=true; };
   }
   catch (Exception ex)
   {
    res=false;
    ServiceWins.ShowError(ex);
   }
   finally
   {
    db.Close();
   }*/
      return true;
    }

    public override bool CheckUserGroup(string usename, string groname)
    {
/*   bool res=false;
   using (IplgDBConnect db = DM.GetConnect())
   {
    db.Open();
    try
    {
     Int32 usesysid = (Int32)db.ExecuteScalar(String.Format("select usesysid from pg_user where usename ='{0}'", usename));
     Int32[] grosysid = (Int32[])db.ExecuteScalar(String.Format("select grolist from pg_group where groname='{0}'", groname));
     for (int i = 0; i<grosysid.Length; i++)
     {
      if (grosysid[i]==usesysid)
      {
       res=true;
       break;
      }
     }
    }
    catch (Exception ex)
    {
     res=false;
     ServiceWins.ShowError(ex);
    }
    finally
    {
     db.Close();
    }
   }*/
      return true;
    }


    #endregion
  }
}

#endif