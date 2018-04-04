//-- ветвление условной компиляции
#define POSTGRESQL
//#define FIREBIRD
#define SQLITE

using System;
using System.Data;

namespace plgDBConnect
{
  /// <summary>
  /// Класс, описывающий ОДНО соединение с базой данных. По его параметрам будет формироваться объект
  /// </summary>
  public class Connector
  {
    /// <summary>
    /// тип соединения
    /// </summary>
    public DBInterfaceType fConnectType { get; set; }
    /// <summary>
    /// Тип сервера
    /// </summary>
    public DBServerStateType fServerStateType { get; set; }
    /// <summary>
    /// имя базы данных или файла базы данных 
    /// </summary>
    public string fDB { get; set; } 
    /// <summary>
    /// Пароль
    /// </summary>
    public string fpass { get; set; } //-- пароль
    /// <summary>
    /// имя компьютера или IP адрес сервера, "localhost" - сервер на локальной машине
    /// </summary>
    public string fHost { get; set; }
    /// <summary>
    /// порт, по которому сервер слушает обращение к себе
    /// </summary>
    public int fPort { get; set; } 
    /// <summary>
    /// пользователь, под которым производится соединение
    /// </summary>
    public string fuser { get; set; }
    /// <summary>
    /// максимальное время ожидания ответа сервера
    /// </summary>
    public int fTimeout { get; set; } 
  }

  /// <summary>
  /// Статический класс управления соединениями с базой данных
  /// </summary>
  public static class DM
  {
    /*
     Под основным коннектором подразумевается соединение с базой данных, которая является основной (приоритетной) при работе с программой,
     что, в большинстве случаев и происходит. Т.е. программа работает только с одной конкретной базой данных. Однако, в некоторых случаях
     необходимо чтобы программа могла одновременно обращаться сразу к нескольким базам, причем возморно и разного типа (разные СУБД), в этом случае
     можно создавать любое количество коннекторов и использовать их так как посчитаете нужным. Более того, Основной коннектор может вообще не использоваться
     (он сделан просто для своего удобства, чтобы можно было задавать значения параметров по умолчанию), вместо него можно просто
     создавать любое количество вторичных коннекторов и управлять их существованием самостоятельно.
       */
    /// <summary>
    /// Экземпляр основного коннектора.
    /// </summary>
    private static Connector MainDB = new Connector();
 
    /// <summary>
    /// Инициализация данных основного соединения с БД (которое будет использоваться по умолчанию в случае соединения с несколькими БД)
    /// </summary>
    /// <param name="ConnectType"> Тип соединения</param>
    /// <param name="DB">Имя базы данных или файла базы данных</param>
    /// <param name="pass">Пароль пользователя</param>
    /// <param name="Host">Имя компьютера или IP адрес сервера, "localhost" - сервер на локальной машине</param>
    /// <param name="Port">Порт, по которому сервер слушает обращение к себе</param>
    /// <param name="user">Пользователь, под которым производится соединение</param>
    /// <param name="Timeout">Максимальное время ожидания ответа сервера</param>
    /// <param name="ServerStateType">Задает тип используюемого сервера Стандартный или встраиваемый (имеет значение только для FireBird)</param>
    /// <param name="mainDB">true (по умолчанию) - задает параметры соединения с основной БД, которые сохраняются внутри класса (фнункция возвращает null). false - задает параметры соединения
    /// с вторичной БД, при этом настроенный коннектор возвращается функцией</param>
    public static Connector Init(DBInterfaceType ConnectType,
      string DB,
      string pass,
      string Host,
      int Port,
      string user,
      int Timeout = 120,
      DBServerStateType ServerStateType = DBServerStateType.Standart,
      bool mainDB = true
      )
    {
      if (mainDB)
      {
        MainDB.fTimeout = Timeout;
        MainDB.fConnectType = ConnectType;
        MainDB.fDB = DB;
        MainDB.fpass = pass;
        MainDB.fHost = Host;
        MainDB.fPort = Port;
        MainDB.fuser = user;
        MainDB.fServerStateType = ServerStateType;
        //-- данные соединения с основной базой данных хранятся внутри статического экземпляра
        //-- и используются по умолчанию, если не задается параметр Connector в вызывающих функциях
        return MainDB;
      }
      else
      {
        return new Connector()
        {
          fTimeout = Timeout,
          fConnectType = ConnectType,
          fDB = DB,
          fpass = pass,
          fHost = Host,
          fPort = Port,
          fuser = user,
          fServerStateType = ServerStateType
        };
      }
    }

    /// <summary>
    /// Получение эксклюзивного соединения с БД, для выполнения необходимых операций
    /// </summary>
    /// <param name="con">Коннектор, если не задан - используется соединение по умолчанию, основная БД</param>
    /// <param name="ErrorTime">Таймаут соединения, по умолчанию = 120 секундам</param>
    /// <returns>Интерфейс запрошенного соединения. Не открывает соединение по умолчанию, пользователь самостоятельно должен это сделать</returns>
    public static IplgDBConnect GetConnect(Connector con=null, int ErrorTime=120)
    {
      //-- если коннектор не задан - используем основную базу
      if (con == null) con = MainDB;

      //-- если коннектор задан...
      switch (con.fConnectType)
      {
        case DBInterfaceType.PostgreSQL:
          return new plgPGConnect(con.fHost, con.fDB, con.fPort, con.fuser, con.fpass, ErrorTime);
/*
  #if FIREBIRD
          //-- FireBird не используется в проектах, поэтому исключен из решения. Более того, не смотря на то, что исходник остался
          //-- он требует доработки, в соответсвии с новым интерфейсом. см. пример в plgPGConnect.cs
        case DBInterfaceType.FireBird:
          return new plgFBConnect(con.fDB, con.fuser, con.fpass, con.fServerStateType);
#endif
*/
        case DBInterfaceType.SQLite:
          return new plgSQLiteConnect(con.fDB, con.fpass, ErrorTime);
        default:
          return null;
      }
    }

    /// <summary>
    /// Функция загрузки или обновления содержимого всей таблицы. При обновлении сперва уничтожаются все данные, которые находятся в таблице
    /// Транзакция задается внутри фукнции, т.е. запрос выполняется внутри своей транзакции
    /// </summary>
    /// <param name="Table">Таблица</param>
    /// <param name="SQL">Запрос, возвращающий данные в таблицу</param>
    public static bool FillTable( DataTable Table, string SQL, Connector con=null)
    {
      var res = true;
      Table.BeginLoadData();
      if (con == null) con = MainDB;
      using (var db = GetConnect(con))
      {
        try
        {
          //-- открыть соединение
          db.Open();
          using (var tr = db.BeginTransaction())
          {
            db.FillDataTable(Table, db.GetSQLCommand(SQL), tr);
            tr.Rollback();
          }
        }
        catch (Exception ex)
        {
          res = false;
          //-- передаем обработку ошибок вызывающей программе
          throw ex;
        }
        finally
        {
          db.Close();
        }
      }
      Table.EndLoadData();
      return res;
    }

    /// <summary>
    /// Функция - обертка для быстрого выполнения единичного запроса в рамках отдельной транзакции. Транзакция фиксируется.
    /// </summary>
    /// <param name="SQL">Текст SQL запроса</param>
    /// <param name="con">Коннектор по умолчанию (для операций на разных БД). Если не задано, сипользуется соединение основной БД</param>
    /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
    public static void ExecNoQuery( string SQL, Connector con=null, int ErrorTime = 120)
    {
      if (con == null) con = MainDB;
      using (var db = GetConnect(con, ErrorTime))
      {
        try
        {
          db.Open();
          using (var tr = db.BeginTransaction())
          {
            db.ExecuteNonQuery(SQL, tr);
            tr.Commit();
          }
        }
        catch (Exception ex)
        {
          db.Rollback();
          //-- передаем обработку ошибок вызывающей программе
          throw ex;
        }
        finally
        {
          db.Close();
        }
      }
    }
    /// <summary>
    /// Функция - обертка для быстрого выполнения единичного запроса в рамках отдельной транзакции. Транзакция фиксируется.
    /// </summary>
    /// <param name="SQL">Подготовленная команда SQL запроса</param>
    /// <param name="con">Коннектор по умолчанию (для операций на разных БД). Если не задано, сипользуется соединение основной БД</param>
    /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
    public static void ExecNoQuery( IDbCommand SQL, Connector con=null, int ErrorTime = 120)
    {
      if (con == null) con = MainDB;
      using (var db = GetConnect(con))
      {
        try
        {
          db.Open();
          using (var tr = db.BeginTransaction())
          {
            db.ExecuteNonQuery(SQL, tr);
            tr.Commit();
          }
        }
        catch (Exception ex)
        {
          db.Rollback();
          //-- передаем обработку ошибок вызывающей программе
          throw ex;
        }
        finally
        {
          db.Close();
        }
      }
    }

    /// <summary>
    /// Функция - обертка для быстрого получения значения в рамках отдельной транзакции.
    /// </summary>
    /// <param name="SQL">Текст SQL запроса</param>
    /// <param name="con">Активный коннектор, если не задан - используется основной, инициализируемый через Init</param>
    public static object ExecuteScalar(string SQL, Connector con=null )
    {
      object obj = null;
      if (con == null) con = MainDB;
      using (var db = GetConnect(con))
      {
        try
        {
          db.Open();
          using (var tr = db.BeginTransaction())
          {
            obj = db.ExecuteScalar(SQL, tr);
          }
        }
        catch (Exception ex)
        {
          db.Rollback();
          obj = null;
          //-- передаем обработку ошибок вызывающей программе
          throw ex;
        }
        finally
        {
          db.Close();
        }
      }
      return obj;
    }

    /// <summary>
    /// Функция - обертка для быстрого получения значения в рамках отдельной транзакции.
    /// </summary>
    /// <param name="SQL">Текст SQL запроса</param>
    /// <param name="con">Активный коннектор, если не задан - используется основной, инициализируемый через Init</param>
    public static object ExecuteScalar(IDbCommand SQL, Connector con=null )
    {
      object obj = null;
      if (con == null) con = MainDB;
      using (var db = GetConnect(con))
      {
        try
        {
          db.Open();
          using (var tr = db.BeginTransaction())
          {
            obj = db.ExecuteScalar(SQL, tr);
          }
        }
        catch (Exception ex)
        {
          db.Rollback();
          obj = null;
          //-- передаем обработку ошибок вызывающей программе
          throw ex;
        }
        finally
        {
          db.Close();
        }
      }
      return obj;
    }
  }
}
