using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRTeaServer.DB
{
	internal class DBConnector
	{
		const string Connection = @"Server=localhost;Database=vrtworld;Uid=VRT;Pwd=password;Charset=utf8mb4;";

		public async Task InitializeDatabaseAsync()
		{
			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			// テーブルがなければ作成するSQL
			// login_count はデフォルト 0 に設定しておくと、INSERT時に省略も可能です
			var createTableSql = @"
			CREATE TABLE IF NOT EXISTS users (
				id INT AUTO_INCREMENT PRIMARY KEY,
				username VARCHAR(50) NOT NULL UNIQUE,
				password_hash VARCHAR(256) NOT NULL,
				login_count INT NOT NULL DEFAULT 0,
				created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
			) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

			using var cmd = new MySqlCommand(createTableSql, conn);
			await cmd.ExecuteNonQueryAsync();

			Console.WriteLine("Database check complete: 'users' table is ready.");
		}

		// 1. ユーザーの有無確認
		public async Task<bool> ExistsAsync(string username)
		{
			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			var query = "SELECT COUNT(*) FROM users WHERE username = @username";
			using var cmd = new MySqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@username", username);

			var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
			return count > 0;
		}

		// 2. ユーザー登録
		public async Task<bool> RegisterAsync(string username, string passwordHash)
		{
			// 既に存在する場合は登録しない
			if (await ExistsAsync(username))
				return false;

			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			var query = "INSERT INTO users (username, password_hash, login_count) VALUES (@username, @password_hash, 0)";
			using var cmd = new MySqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@username", username);
			cmd.Parameters.AddWithValue("@password_hash", passwordHash);

			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		// 3. パスワード確認（認証）
		public async Task<bool> VerifyPasswordAsync(string username, string passwordHash)
		{
			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			// ユーザー名とハッシュが一致するレコードがあるか探す
			var query = "SELECT COUNT(*) FROM users WHERE username = @username AND password_hash = @password_hash";
			using var cmd = new MySqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@username", username);
			cmd.Parameters.AddWithValue("@password_hash", passwordHash);

			var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
			return count > 0;
		}

		// 4. ログインカウントの取得
		public async Task<int> GetLoginCountAsync(string username)
		{
			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			var query = "SELECT login_count FROM users WHERE username = @username";
			using var cmd = new MySqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@username", username);

			var result = await cmd.ExecuteScalarAsync();

			// ユーザーが存在しない場合は 0 を返す
			return result != null ? Convert.ToInt32(result) : 0;
		}

		// 5. ログインカウントの加算 (+1)
		public async Task<bool> IncrementLoginCountAsync(string username)
		{
			using var conn = new MySqlConnection(Connection);
			await conn.OpenAsync();

			// 現在の値に +1 して更新
			var query = "UPDATE users SET login_count = login_count + 1 WHERE username = @username";
			using var cmd = new MySqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@username", username);

			// 更新された行数が 1 以上なら成功
			return await cmd.ExecuteNonQueryAsync() > 0;
		}
	}
}
