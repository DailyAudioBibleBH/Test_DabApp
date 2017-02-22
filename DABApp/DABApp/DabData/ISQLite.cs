using System;
using SQLite;

namespace DABApp
{
	public interface ISQLite
	{
		SQLiteConnection GetConnection(bool ResetDatabaseOnStart);
	}
}
