using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace AI_TestMaker.DB.Login
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string dbPath = "tests.db")
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        private string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password + salt);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public bool Register(string username, string password)
        {
            string salt = GenerateSalt();
            string hash = HashPassword(password, salt);

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sql = @"
                INSERT INTO Users (Username, PasswordHash, Salt, FechaRegistro)
                VALUES (@u, @h, @s, @f);
            ";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@h", hash);
            cmd.Parameters.AddWithValue("@s", salt);
            cmd.Parameters.AddWithValue("@f", DateTime.Now.ToString("s"));

            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false; // usuario duplicado
            }
        }

        public User Login(string username, string password)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            string sql = "SELECT * FROM Users WHERE Username=@u";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            string salt = reader["Salt"].ToString();
            string hash = reader["PasswordHash"].ToString();

            string hashInput = HashPassword(password, salt);

            if (hashInput != hash)
                return null;

            return new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Username = reader["Username"].ToString(),
                PasswordHash = hash,
                Salt = salt,
                FechaRegistro = DateTime.Parse(reader["FechaRegistro"].ToString())
            };
        }

        public bool CambiarContraseña(int userId, string contraseñaActual, string nuevaContraseña)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1. Obtener usuario
                string query = "SELECT PasswordHash, Salt FROM Users WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return false;

                        string hashActual = reader.GetString(0);
                        string salt = reader.GetString(1);

                        // 2. Validar contraseña actual
                        string hashComprobado = HashPassword(contraseñaActual, salt);
                        if (hashComprobado != hashActual)
                            return false;

                        // 3. Generar nuevo hash
                        string nuevoSalt = GenerateSalt();
                        string nuevoHash = HashPassword(nuevaContraseña, nuevoSalt);

                        // 4. Guardar en DB
                        string update = "UPDATE Users SET PasswordHash = @hash, Salt = @salt WHERE Id = @id2";
                        using (var cmd2 = new SQLiteCommand(update, conn))
                        {
                            cmd2.Parameters.AddWithValue("@hash", nuevoHash);
                            cmd2.Parameters.AddWithValue("@salt", nuevoSalt);
                            cmd2.Parameters.AddWithValue("@id2", userId);
                            cmd2.ExecuteNonQuery();
                        }

                        return true;
                    }
                }
            }
        }

        public bool EliminarCuenta(int userId)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Borrar tests del usuario
                        string deleteTests = "DELETE FROM Tests WHERE UserId = @id";
                        using (var cmd = new SQLiteCommand(deleteTests, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Borrar usuario
                        string deleteUser = "DELETE FROM Users WHERE Id = @id";
                        using (var cmd = new SQLiteCommand(deleteUser, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        return false;
                    }
                }
            }
        }

    }
}
