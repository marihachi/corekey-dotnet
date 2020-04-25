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

			var app = await App.Create("misskey.io", "Corekey.Example", "A test app", new[] { "write:notes" });
			Console.WriteLine($"app host: {app.Host}");
			Console.WriteLine($"app secret: {app.Secret}");
		}
	}
}
