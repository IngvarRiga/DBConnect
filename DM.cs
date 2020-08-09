//-- ветвление условной компиляции
#define POSTGRESQL //-- подключение PostgreSQL
//#define FIREBIRD //-- подключение FireBird
#define SQLITE //-- подключение SQLite

using System;
using System.Data;

namespace plgDBConnect
{
    /// <summary>
    /// Класс, описывающий ОДНО соединение с базой данных. По его параметрам будет формироваться объект соединения с БД. Обратите внимание, что
    /// сам по себе объект коннектора НЕ является соединением, а просто содержит параметры для соединения с БД.
    /// </summary>
    public class Connector
    {
        /// <summary>
        /// Тип соединения
        /// </summary>
        public DBInterfaceType ConnectType { get; set; }
        /// <summary>
        /// Тип сервера
        /// </summary>
        public DBServerStateType ServerStateType { get; set; }
        /// <summary>
        /// Имя базы данных или файла базы данных 
        /// </summary>
        public string DB_Name { get; set; }
        /// <summary>
        /// Пароль для соединения с базой данных
        /// </summary>
        public string DB_Password { get; set; } //-- пароль
        /// <summary>
        /// Имя компьютера или IP адрес сервера, "localhost" - сервер на локальной машине
        /// </summary>
        public string DB_Host { get; set; }
        /// <summary>
        /// Порт, по которому сервер слушает обращение к себе
        /// </summary>
        public int DB_Port { get; set; }
        /// <summary>
        /// Пользователь, под которым производится соединение
        /// </summary>
        public string DB_User { get; set; }
        /// <summary>
        /// Максимальное время ожидания ответа сервера (секунд)
        /// </summary>
        public int DB_Timeout { get; set; }
        /// <summary>
        /// Создание объекта коннектора с параметрами по умолчанию
        /// </summary>
        public Connector()
        {
            //-- неизвестный тип сервера
            ConnectType = DBInterfaceType.Unknown;
            //-- стандартный (только FireBird использует тип Embedded)
            ServerStateType = DBServerStateType.Standart;
            //-- время ожидания = 2 минуты
            DB_Timeout = 120;
        }

        /// <summary>
        /// Клонирование параметров текущего коннектора в новом объекте
        /// </summary>
        /// <returns>Вновь созданный объект коннектора</returns>
        public Connector Clone()
        {
            return new Connector
            {
                ConnectType = ConnectType,
                ServerStateType = ServerStateType,
                DB_Host = DB_Host,
                DB_Name = DB_Name,
                DB_Password = DB_Password,
                DB_Port = DB_Port,
                DB_Timeout = DB_Timeout,
                DB_User = DB_User
            };
        }
    }

    /// <summary>
    /// Статический класс управления соединениями с базой данных
    /// </summary>
    public static class DM
    {
        /// <summary>
        /// Последний выполненный запрос, важен для отслеживания ошибок
        /// </summary>
        private static string LastSQL = string.Empty;
        /*
         Под основным коннектором подразумевается соединение с базой данных, которая является основной (приоритетной) при работе с программой,
         что, в большинстве случаев и происходит. Т.е. программа работает только с одной конкретной базой данных. Однако, в некоторых случаях
         необходимо чтобы программа могла одновременно обращаться сразу к нескольким базам, причем возможно и разного типа (разные СУБД), в этом случае
         можно создавать любое количество коннекторов и использовать их так как посчитаете нужным. Более того, основной коннектор может вообще не использоваться
         (он сделан просто для своего удобства, чтобы можно было задавать значения параметров по умолчанию), вместо него можно просто
         создавать любое количество вторичных коннекторов и управлять их существованием самостоятельно.
        */
        /// <summary>
        /// Экземпляр основного коннектора.
        /// </summary>
        private static readonly Connector MainDB = new Connector();

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
        /// <param name="ServerStateType">Задает тип используемого сервера Стандартный или встраиваемый (имеет значение только для FireBird)</param>
        /// <param name="mainDB">true (по умолчанию) - задает параметры соединения с основной БД, которые сохраняются внутри класса (функция возвращает null). false - задает параметры соединения
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
                MainDB.DB_Timeout = Timeout;
                MainDB.ConnectType = ConnectType;
                MainDB.DB_Name = DB;
                MainDB.DB_Password = pass;
                MainDB.DB_Host = Host;
                MainDB.DB_Port = Port;
                MainDB.DB_User = user;
                MainDB.ServerStateType = ServerStateType;
                //-- данные соединения с основной базой данных хранятся внутри статического экземпляра
                //-- и используются по умолчанию, если не задается параметр Connector в вызывающих функциях
                return MainDB;
            }
            else
            {
                return new Connector()
                {
                    DB_Timeout = Timeout,
                    ConnectType = ConnectType,
                    DB_Name = DB,
                    DB_Password = pass,
                    DB_Host = Host,
                    DB_Port = Port,
                    DB_User = user,
                    ServerStateType = ServerStateType
                };
            }
        }

        /// <summary>
        /// Возвращает объект коннектора основного соединения.
        /// </summary>
        /// <returns></returns>
        public static Connector GetMainBD()
        {
            return MainDB;
        }

        /// <summary>
        /// Получение эксклюзивного соединения с БД, для выполнения необходимых операций
        /// </summary>
        /// <param name="con">Коннектор, если не задан - используется соединение по умолчанию, основная БД</param>
        /// <param name="ErrorTime">Таймаут соединения, по умолчанию = 120 секундам</param>
        /// <returns>Интерфейс запрошенного соединения. Не открывает соединение по умолчанию, пользователь самостоятельно должен это сделать</returns>
        public static IplgDBConnect GetConnect(Connector con = null, int ErrorTime = 120)
        {
            //-- если коннектор не задан - используем основную базу
            if (con == null) con = MainDB;

            //-- если коннектор задан...
            switch (con.ConnectType)
            {
                case DBInterfaceType.Unknown:
                    throw new ArgumentException("Тип соединения заявлен как Unknown (неизвестный). С каким типом сервера соединяться - непонятно.");
                case DBInterfaceType.PostgreSQL:
                    return new plgPGConnect(con.DB_Host, con.DB_Name, con.DB_Port, con.DB_User, con.DB_Password, ErrorTime);
                /*
                  #if FIREBIRD
                          //-- FireBird не используется в проектах, поэтому исключен из решения. Более того, не смотря на то, что исходник остался
                          //-- он требует доработки, в соответствии с новым интерфейсом. см. пример в plgPGConnect.cs
                        case DBInterfaceType.FireBird:
                          return new plgFBConnect(con.DB_Name, con.DB_User, con.DB_Password, con.ServerStateType);
                #endif
                */
                case DBInterfaceType.SQLite:
                    return new plgSQLiteConnect(con.DB_Name, con.DB_Password, ErrorTime);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Получение последнего выполняемого запроса
        /// </summary>
        /// <returns></returns>
        public static string GetLastSQL()
        {
            return LastSQL;
        }

        /// <summary>
        /// Функция загрузки или обновления содержимого всей таблицы. При обновлении сперва уничтожаются все данные, которые находятся в таблице
        /// Транзакция задается внутри функции, т.е. запрос выполняется внутри своей транзакции
        /// </summary>
        /// <param name="Table">Таблица</param>
        /// <param name="SQL">Запрос, возвращающий данные в таблицу</param>
        /// <param name="con">Коннектор, через который будет выполнена операция</param>
        public static void FillTable(DataTable Table, string SQL, Connector con = null)
        {
            Table.BeginLoadData();
            if (con == null) con = MainDB;
            LastSQL = SQL;
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
                finally
                {
                    db.Close();
                }
            }
            Table.EndLoadData();
        }

        /// <summary>
        /// Функция - обертка для быстрого выполнения единичного запроса в рамках отдельной транзакции. Транзакция фиксируется.
        /// </summary>
        /// <param name="SQL">Текст SQL запроса</param>
        /// <param name="con">Коннектор по умолчанию (для операций на разных БД). Если не задано, используется соединение основной БД</param>
        /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
        public static void ExecNoQuery(string SQL, Connector con = null, int ErrorTime = 120)
        {
            if (con == null) con = MainDB;
            LastSQL = SQL;
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
                catch (Exception)
                {
                    db.Rollback();
                    //-- передаем обработку ошибок вызывающей программе
                    throw;
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
        /// <param name="con">Коннектор по умолчанию (для операций на разных БД). Если не задано, используется соединение основной БД</param>
        /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
        public static void ExecNoQuery(IDbCommand SQL, Connector con = null, int ErrorTime = 120)
        {
            if (con == null) con = MainDB;
            LastSQL = SQL.CommandText;

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
                catch (Exception)
                {
                    db.Rollback();
                    //-- передаем обработку ошибок вызывающей программе
                    throw;
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
        /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
        public static object ExecuteScalar(string SQL, Connector con = null, int ErrorTime = 120)
        {
            object obj;
            if (con == null) con = MainDB;
            LastSQL = SQL;
            using (var db = GetConnect(con, ErrorTime))
            {
                try
                {
                    db.Open();
                    using (var tr = db.BeginTransaction())
                    {
                        obj = db.ExecuteScalar(SQL, tr);
                    }
                }
                catch (Exception)
                {
                    db.Rollback();
                    //-- передаем обработку ошибок вызывающей программе
                    throw;
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
        /// <param name="ErrorTime">Таймаут выполнения, по умолчанию = 120 секунд, но иногда требуется и больше</param>
        public static object ExecuteScalar(IDbCommand SQL, Connector con = null, int ErrorTime = 120)
        {
            object obj;
            if (con == null) con = MainDB;
            LastSQL = SQL.CommandText;
            using (var db = GetConnect(con, ErrorTime))
            {
                try
                {
                    db.Open();
                    using (var tr = db.BeginTransaction())
                    {
                        obj = db.ExecuteScalar(SQL, tr);
                    }
                }
                catch (Exception)
                {
                    db.Rollback();
                    //-- передаем обработку ошибок вызывающей программе
                    throw;
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
