using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
				//Debug.WriteLine($"-- Begin Request --");
				var paramDictionary = parameters.ToDictionary(w => w.Key, w => w.Value);
				var json = JToken.FromObject(paramDictionary).ToString(Formatting.None);
				//Debug.WriteLine($"request data: {json}");
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				//Debug.WriteLine($"url: https://{host}/api/{endpoint}");
				var res = await this.Client.PostAsync($"https://{host}/api/{endpoint}", content);
				//Debug.WriteLine($"status: {res.StatusCode}");
				var resData = await res.Content.ReadAsStringAsync();
				//Debug.WriteLine($"response data: {resData}");
				var parsedToken = JToken.Parse(resData);
				//Debug.WriteLine($"parsed token: {parsedToken}");
				//Debug.WriteLine($"-- End Request --");
				return parsedToken;
			}
		}
	}

	public static class Configuration
	{
		public static IRequester Requester = new HttpRequester(new HttpClient());
	}

	public class App
	{
		public string Host { get; private set; }
		public string Secret { get; private set; }

		public App(string host, string appSecret)
		{
			this.Host = host;
			this.Secret = appSecret;
		}

		public static async Task<App> Create(string host, string name, string description, IEnumerable<string> permissions, string callbackUrl = null)
		{
			var res = await Configuration.Requester.Request(
				host,
				"app/create",
				new[] {
					new KeyValuePair<string, object>("name", name),
					new KeyValuePair<string, object>("description", description),
					new KeyValuePair<string, object>("permission", permissions),
					new KeyValuePair<string, object>("callbackUrl", callbackUrl)
				},
				false);

			return new App(host, res.Value<string>("secret"));
		}
	}

	public class UserToken
	{
		public string AccessToken { get; set; }
		public JObject User { get; set; }

		public UserToken(string accessToken, JObject user)
		{
			this.AccessToken = accessToken;
			this.User = user;
		}
	}

	public class AuthSession
	{
		private App App;
		private string Token;
		public string Url { get; private set; }

		public AuthSession(App app, string token, string url)
		{
			this.App = app;
			this.Token = token;
			this.Url = url;
		}

		public static async Task<AuthSession> Generate(App app)
		{
			var authSession = await Configuration.Requester.Request(app.Host, "auth/session/generate", new[] {
				new KeyValuePair<string, object>("appSecret", app.Secret)
			}, false);
			return new AuthSession(app, authSession.Value<string>("token"), authSession.Value<string>("url"));
		}

		public async Task<UserToken> GetUserToken()
		{
			var userToken = await Configuration.Requester.Request(this.App.Host, "auth/session/userkey", new [] {
				new KeyValuePair<string, object>("appSecret", this.App.Secret),
				new KeyValuePair<string, object>("token", this.Token)
			}, false);
			if (userToken.SelectToken("error") != null) {
				//Debug.WriteLine(userToken.ToString());
				return null;
			}
			return new UserToken(userToken.Value<string>("accessToken"), userToken["user"].ToObject<JObject>());
		}

		public async Task<Account> WaitForAuth(CancellationToken? ct = null)
		{
			UserToken token;
			while (true)
			{
				ct?.ThrowIfCancellationRequested();
				token = await this.GetUserToken();
				if (token != null) break;
				ct?.ThrowIfCancellationRequested();
				await Task.Delay(2000);
			}
			return new Account(this.App, token.AccessToken);
		}
	}

	public class Account
	{
		public App App { get; private set; }
		public string AccessToken { get; private set; }
		private string i;

		public Account(App app, string accessToken)
		{
			string calcAccessToken(string appSecret, string userToken)
			{
				using (var sha256 = SHA256.Create())
				{
					var tokenCodeBytes = Encoding.ASCII.GetBytes($"{userToken}{appSecret}");
					var hashBytes = sha256.ComputeHash(tokenCodeBytes);
					return string.Join("", hashBytes.Select(x => $"{x:x2}"));
				}
			}

			this.App = app;
			this.AccessToken = accessToken;
			this.i = calcAccessToken(app.Secret, accessToken);
		}

		public Task<JToken> Request(string endpoint, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			var baseParam = new [] {
				new KeyValuePair<string, object>("i", this.i)
			};
			return Configuration.Requester.Request(this.App.Host, endpoint, baseParam.Concat(parameters), false);
		}
	}
}
