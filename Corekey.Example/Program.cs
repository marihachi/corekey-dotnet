using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Corekey;

namespace Corekey.Example
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("A example project of Corekey");

			// create an app
			var app = await App.Create("misskey.io", "Corekey.Example", "A test app", new[] { "write:notes" });

			// generate an auth session
			var session = await AuthSession.Generate(app);
			Console.WriteLine($"session url: {session.Url}");

			// wait the authorization
			Console.WriteLine("Waiting authorization is complete ...");
			var account = await session.WaitForAuth();

			// post a note
			var res = await account.Request("notes/create", new [] {
				new KeyValuePair<string, object>("text", "Corekey.NET example")
			});
			Console.WriteLine(res.ToString());
		}
	}
}

