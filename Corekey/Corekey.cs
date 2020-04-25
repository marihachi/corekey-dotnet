using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Corekey
{
	public interface IRequester
	{
		Task<JToken> Request(string host, string endpoint, IEnumerable<KeyValuePair<string, object>> parameters, bool isBinary);
	}

	public class HttpRequester : IRequester
	{
		private HttpClient Client;

		public HttpRequester(HttpClient httpClient)
		{
			this.Client = httpClient;
		}

		public async Task<JToken> Request(string host, string endpoint, IEnumerable<KeyValuePair<string, object>> parameters, bool isBinary)
		{
			if (isBinary)
			{
				throw new NotImplementedException("binary request is not implemented yet");
			}
			else
			{
				Debug.WriteLine($"-- Begin Request --");
				var paramDictionary = parameters.ToDictionary(w => w.Key, w => w.Value);
				var json = JToken.FromObject(paramDictionary).ToString(Formatting.None);
				Debug.WriteLine($"request data: {json}");
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				Debug.WriteLine($"url: https://{host}/api/{endpoint}");
				var res = await this.Client.PostAsync($"https://{host}/api/{endpoint}", content);
				Debug.WriteLine($"status: {res.StatusCode}");
				var resData = await res.Content.ReadAsStringAsync();
				Debug.WriteLine($"response data: {resData}");
				var parsedToken = JToken.Parse(resData);
				Debug.WriteLine($"parsed token: {parsedToken}");
				Debug.WriteLine($"-- End Request --");
				return parsedToken;
			}
		}
	}

	public static class Configurator
	{
		public static IRequester Requester = new HttpRequester(new HttpClient());
	}

	public class App
	{
		public string Host { get; set; }
		public string Secret { get; set; }
		
		public App(string host, string appSecret)
		{
			this.Host = host;
			this.Secret = appSecret;
		}

		public static async Task<App> Create(string host, string name, string description, IEnumerable<string> permissions, string callbackUrl = null)
		{
			var res = await Configurator.Requester.Request(
				host,
				"app/create",
				new List<KeyValuePair<string, object>> {
					new KeyValuePair<string, object>("name", name),
					new KeyValuePair<string, object>("description", description),
					new KeyValuePair<string, object>("permission", permissions),
					new KeyValuePair<string, object>("callbackUrl", callbackUrl)
				},
				false);

			return new App(host, res.Value<string>("secret"));
		}
	}
}
