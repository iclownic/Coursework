using Npgsql;
using ToDoApi.Model;
using Microsoft.Extensions.Configuration;

namespace WebApplication1
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task InsertToDoAsync(ToDo toDo)
        {
            using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
            {
                string sql = "INSERT INTO public.\"TooDoo\"(\"Title\", \"Description\", \"Status\", \"Date\") VALUES (@Title, @Description, @Status, @Date)";
                await con.OpenAsync();

                using (NpgsqlCommand comm = new NpgsqlCommand(sql, con))
                {
                    comm.Parameters.AddWithValue("@Title", toDo.Title);
                    comm.Parameters.AddWithValue("@Description", toDo.Description);
                    comm.Parameters.AddWithValue("@Status", toDo.Status);
                    comm.Parameters.AddWithValue("@Date", DateTime.Now);

                    await comm.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
