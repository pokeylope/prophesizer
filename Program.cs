﻿using Npgsql;
using System;
using System.Threading.Tasks;

namespace SIBR
{
	class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				// Try to migrate the DB to the latest schema
				var connection  = new NpgsqlConnection (Environment.GetEnvironmentVariable("PSQL_CONNECTION_STRING"));
				var evolve = new Evolve.Evolve(connection, msg => Console.WriteLine(msg))
				{
					Locations = new[] { "migrations" },
					SqlMigrationSuffix = ".pgsql",
					CommandTimeout = 60,
					//IsEraseDisabled = true, //< Recommended in production
				};

				evolve.Migrate();
			}
			catch (Exception ex)
			{
				// Oh noes
				Console.WriteLine("Database migration failed.", ex);
				throw;
			}


			Prophesizer prophesizer = new Prophesizer();

			int lastHour = -1;
			int numUpdates = 0;
			while (true)
			{
				var hour = DateTime.UtcNow.Hour;
				PollResult result = await prophesizer.Poll();
				numUpdates += result.NumUpdatesProcessed;
				if ((hour > lastHour) || (hour==0 && lastHour == 23))
				{
					Prophesizer.ConsoleOrWebhook($"Processed {numUpdates} updates in the past hour. Last recorded game is Season {result.Latest.Season+1}, Day {result.Latest.Day+1}.");
					//Prophesizer.ConsoleOrWebhook($"    {prophesizer.NumNetworkOutcomes} games used the network outcomes.json file, {prophesizer.NumLocalOutcomes} did not.");
					lastHour = hour;
					numUpdates = 0;
				}
				await Task.Delay(1 * 60 * 1000);

			}
		}
	}
}
