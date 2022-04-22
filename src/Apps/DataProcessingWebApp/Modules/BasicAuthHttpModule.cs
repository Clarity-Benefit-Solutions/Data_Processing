using System;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using CoreUtils.Classes;

namespace DataProcessingWebApp.Modules
{

    public class BasicAuthHttpModule : IHttpModule
    {
        private const string Realm = "DataProcessing Realm";

        public void Init(HttpApplication context)
        {
            // Register event handlers
            context.AuthenticateRequest += OnApplicationAuthenticateRequest;
            context.EndRequest += OnApplicationEndRequest;
        }

        public void Dispose()
        {
        }

        private static void SetPrincipal(IPrincipal principal)
        {
            Thread.CurrentPrincipal = principal;
            if (HttpContext.Current != null)
            {
                HttpContext.Current.User = principal;
            }
        }

        private static bool CheckPassword(string username, string password)
        {
            return
                username == Utils.GetAppSetting("BasicAuthUserName")
                && password == Utils.GetAppSetting("BasicAuthPassword");
        }

        private static void AuthenticateUser(string credentials)
        {
            try
            {
                if (credentials != null)
                {
                    if (credentials.IndexOf(":", StringComparison.Ordinal) < 0)
                    {
                        var encoding = Encoding.GetEncoding("iso-8859-1");
                        credentials = encoding.GetString(Convert.FromBase64String(credentials));
                    }

                    var separator = credentials.IndexOf(':');
                    var name = credentials.Substring(0, separator);
                    var password = credentials.Substring(separator + 1);

                    if (CheckPassword(name, password))
                    {
                        var identity = new GenericIdentity(name);
                        SetPrincipal(new GenericPrincipal(identity, null));
                    }
                    else
                    {
                        // Invalid username or password.
                        HttpContext.Current.Response.StatusCode = 401;
                    }
                }
                else
                {
                    // Invalid username or password.
                    HttpContext.Current.Response.StatusCode = 401;
                }
            }
            catch (FormatException)
            {
                // Credentials were not formatted correctly.
                HttpContext.Current.Response.StatusCode = 401;
            }
        }

        private static void OnApplicationAuthenticateRequest(object sender, EventArgs e)
        {
            var request = HttpContext.Current.Request;
            var authHeader = request.Headers["Authorization"];
            if (authHeader == null)
            {
                HttpContext.Current.Response.StatusCode = 401;
                return;
            }

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);

            // RFC 2617 sec 1.2, "scheme" name is case-insensitive
            if (authHeaderVal.Scheme.Equals("basic",
                    StringComparison.OrdinalIgnoreCase) &&
                authHeaderVal.Parameter != null)
            {
                AuthenticateUser(authHeaderVal.Parameter);
            }
        }

        // If the request was unauthorized, add the WWW-Authenticate header 
        // to the response.
        private static void OnApplicationEndRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current == null)
            {
                return;
            }

            var response = HttpContext.Current.Response;
            if (response.StatusCode == 401)
            {
                response.Headers.Add("WWW-Authenticate",
                    $"Basic realm=\"{BasicAuthHttpModule.Realm}\"");
            }
        }
    }

}