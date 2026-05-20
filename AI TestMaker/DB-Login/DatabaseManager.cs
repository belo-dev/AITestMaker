using AI_TestMaker.DB.Login;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace AI_TestMaker.DB
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AITestMaker"
            );

            Directory.CreateDirectory(folder);

            string dbPath = Path.Combine(folder, "tests.db");

            bool crearTablas = !File.Exists(dbPath);

            _connectionString = $"Data Source={dbPath};Version=3;";

            if (crearTablas)
                CrearTablas();
        }



        public void CrearTablas()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sqlUsers = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    FechaRegistro TEXT NOT NULL,
                    Nombre TEXT,
                    Foto BLOB
                );
            ";


            string sqlTest = @"
                CREATE TABLE IF NOT EXISTS Test (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Dificultad TEXT,
                    Tema TEXT,
                    Fecha TEXT,
                    TiempoMaximo INTEGER,
                    TiempoEmpleado INTEGER,
                    Nota REAL,
                    FOREIGN KEY(UserId) REFERENCES Users(Id)
                );
            ";

            string sqlPregunta = @"
                CREATE TABLE IF NOT EXISTS Pregunta (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestId INTEGER,
                    Enunciado TEXT,
                    RespuestaUsuario INTEGER,
                    FOREIGN KEY(TestId) REFERENCES Test(Id)
                );
            ";

            string sqlOpcion = @"
                CREATE TABLE IF NOT EXISTS Opcion (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PreguntaId INTEGER,
                    Texto TEXT,
                    EsCorrecta INTEGER,
                    FOREIGN KEY(PreguntaId) REFERENCES Pregunta(Id)
                );
            ";

            using var cmd = new SQLiteCommand(sqlUsers, conn);
            cmd.ExecuteNonQuery();

            cmd.CommandText = sqlTest;
            cmd.ExecuteNonQuery();

            cmd.CommandText = sqlPregunta;
            cmd.ExecuteNonQuery();

            cmd.CommandText = sqlOpcion;
            cmd.ExecuteNonQuery();
        }

        public int GuardarTest(Test test)
        {
            if (Session.IsGuest)
                return -1;
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();

            string sqlInsertTest = @"
                INSERT INTO Test (UserId, Dificultad, Tema, Fecha, TiempoMaximo, TiempoEmpleado, Nota)
                VALUES (@uid, @dif, @tema, @fecha, @tmax, @temp, @nota);
                SELECT last_insert_rowid();
            ";

            using var cmdTest = new SQLiteCommand(sqlInsertTest, conn);
            cmdTest.Parameters.AddWithValue("@uid", Session.UserId);
            cmdTest.Parameters.AddWithValue("@dif", test.Dificultad);
            cmdTest.Parameters.AddWithValue("@tema", test.Tema);
            cmdTest.Parameters.AddWithValue("@fecha", test.Fecha.ToString("s"));
            cmdTest.Parameters.AddWithValue("@tmax", (int)test.TiempoMaximo.TotalSeconds);
            cmdTest.Parameters.AddWithValue("@temp", (int)test.TiempoEmpleado.TotalSeconds);
            cmdTest.Parameters.AddWithValue("@nota", test.CalcularNota());

            int testId = Convert.ToInt32(cmdTest.ExecuteScalar());

            foreach (var p in test.Preguntas)
            {
                string sqlInsertPregunta = @"
                    INSERT INTO Pregunta (TestId, Enunciado, RespuestaUsuario)
                    VALUES (@tid, @enun, @resp);
                    SELECT last_insert_rowid();
                ";

                using var cmdPregunta = new SQLiteCommand(sqlInsertPregunta, conn);
                cmdPregunta.Parameters.AddWithValue("@tid", testId);
                cmdPregunta.Parameters.AddWithValue("@enun", p.Enunciado);
                cmdPregunta.Parameters.AddWithValue("@resp", p.RespuestaUsuario.HasValue ? p.RespuestaUsuario.Value : -1);

                int preguntaId = Convert.ToInt32(cmdPregunta.ExecuteScalar());

                foreach (var o in p.Opciones)
                {
                    string sqlInsertOpcion = @"
                        INSERT INTO Opcion (PreguntaId, Texto, EsCorrecta)
                        VALUES (@pid, @txt, @ok);
                    ";

                    using var cmdOpcion = new SQLiteCommand(sqlInsertOpcion, conn);
                    cmdOpcion.Parameters.AddWithValue("@pid", preguntaId);
                    cmdOpcion.Parameters.AddWithValue("@txt", o.Texto);
                    cmdOpcion.Parameters.AddWithValue("@ok", o.EsCorrecta ? 1 : 0);

                    cmdOpcion.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            return testId;
        }

        public Test CargarTest(int id)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sqlTest = "SELECT * FROM Test WHERE Id = @id AND UserId = @uid";

            using var cmdTest = new SQLiteCommand(sqlTest, conn);
            cmdTest.Parameters.AddWithValue("@id", id);
            cmdTest.Parameters.AddWithValue("@uid", Session.UserId);

            using var reader = cmdTest.ExecuteReader();

            if (!reader.Read())
                return null;

            string dificultad = reader["Dificultad"].ToString();
            string tema = reader["Tema"].ToString();
            DateTime fecha = DateTime.Parse(reader["Fecha"].ToString());
            TimeSpan tiempoMax = TimeSpan.FromSeconds(Convert.ToInt32(reader["TiempoMaximo"]));
            TimeSpan tiempoEmp = TimeSpan.FromSeconds(Convert.ToInt32(reader["TiempoEmpleado"]));

            var preguntas = new List<Pregunta>();

            reader.Close();

            string sqlPreguntas = "SELECT * FROM Pregunta WHERE TestId = @id";

            using var cmdPreg = new SQLiteCommand(sqlPreguntas, conn);
            cmdPreg.Parameters.AddWithValue("@id", id);

            using var rPreg = cmdPreg.ExecuteReader();

            while (rPreg.Read())
            {
                int pregId = Convert.ToInt32(rPreg["Id"]);
                string enun = rPreg["Enunciado"].ToString();
                int respUser = Convert.ToInt32(rPreg["RespuestaUsuario"]);

                var opciones = new List<Opcion>();

                string sqlOpc = "SELECT * FROM Opcion WHERE PreguntaId = @pid";

                using var cmdOpc = new SQLiteCommand(sqlOpc, conn);
                cmdOpc.Parameters.AddWithValue("@pid", pregId);

                using var rOpc = cmdOpc.ExecuteReader();

                while (rOpc.Read())
                {
                    opciones.Add(new Opcion(
                        rOpc["Texto"].ToString(),
                        Convert.ToInt32(rOpc["EsCorrecta"]) == 1
                    ));
                }

                var pregunta = new Pregunta(enun, opciones);

                if (respUser >= 0)
                    pregunta.RespuestaUsuario = respUser;

                preguntas.Add(pregunta);
            }

            var test = new Test(dificultad, preguntas, tiempoMax, tema)
            {
                Fecha = fecha,
                Fin = tiempoEmp > TimeSpan.Zero ? DateTime.Now : null
            };

            return test;
        }

        public List<TestResumen> ListarTests()
        {
            var lista = new List<TestResumen>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sql = @"
                SELECT Id, Dificultad, Tema, Fecha, Nota
                FROM Test
                WHERE UserId = @uid
                ORDER BY Fecha DESC
            ";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", Session.UserId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new TestResumen
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Dificultad = reader["Dificultad"].ToString(),
                    Tema = reader["Tema"].ToString(),
                    Fecha = DateTime.Parse(reader["Fecha"].ToString()),
                    Nota = Convert.ToDouble(reader["Nota"])
                });
            }

            return lista;
        }

        public void BorrarTest(int id)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(conn);

            cmd.CommandText = "DELETE FROM Opcion WHERE PreguntaId IN (SELECT Id FROM Pregunta WHERE TestId=@id)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DELETE FROM Pregunta WHERE TestId=@id";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DELETE FROM Test WHERE Id=@id AND UserId=@uid";
            cmd.Parameters.AddWithValue("@uid", Session.UserId);
            cmd.ExecuteNonQuery();
        }
    }
}
