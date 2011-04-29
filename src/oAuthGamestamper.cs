using System;
using System.Data;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

public class oAuthGamestamper
{
    public enum Method { GET, POST };
    public const string AUTHORIZE = "https://www.gamestamper.com/oauth/authorize";
    public const string ACCESS_TOKEN = "https://www.gamestamper.com/oauth/access_token";
    public const string CALLBACK_URL = "http://www.yourserver.com/GHClient/callbackpage.aspx";

    private string _consumerKey = "{Your GameStamper Key}";
    private string _consumerSecret = "{Your GameStamper Secret}";
    private string _token = "";
    private string _rawResponse = "";
    private string _rawCodeResponse = "";
    private DateTime? _expires = null;

    #region Properties

    public string ConsumerKey
    {
        get
        {
            if (_consumerKey.Length == 0)
            {
                _consumerKey = "1111111111111";
            }
            return _consumerKey;
        }
        set { _consumerKey = value; }
    }

    public string ConsumerSecret
    {
        get
        {
            if (_consumerSecret.Length == 0)
            {
                _consumerSecret = "11111111111111111111111111111111";
            }
            return _consumerSecret;
        }
        set { _consumerSecret = value; }
    }

    public string Token { get { return _token; } set { _token = value; } }
    public string RawTokenResponse { get { return _rawResponse; } set { _rawResponse = value; } }
    public string RawCodeResponse { get { return _rawCodeResponse; } set { _rawCodeResponse = value; } }
    public DateTime? Expires { get { return _expires; } set { _expires = value; } }
    
    #endregion

    public string AuthorizationLinkGet(string permissions)
    {
        return AuthorizationLinkGet(permissions, null);
    }

    /// <summary>
    /// Get the link to Facebook's authorization page for this application.
    /// </summary>
    /// <returns>The url with a valid request token, or a null string.</returns>
    public string AuthorizationLinkGet(string permissions, string query)
    {
        string callback = CALLBACK_URL + (query != null ? "?" + query : "");
        string s = string.Format("{0}?client_id={1}&redirect_uri={2}", AUTHORIZE, this.ConsumerKey, callback);
        if (permissions != null) s += "&scope=" + permissions;

        return s;
    }

    public void AccessTokenGet(string authToken)
    {
        AccessTokenGet(authToken, null);
    }

    /// <summary>
    /// Exchange the Facebook "code" for an access token.
    /// </summary>
    /// <param name="authToken">The oauth_token or "code" is supplied by Facebook's authorization page following the callback.</param>
    public void AccessTokenGet(string authToken, string query)
    {
        this.Token = authToken;
        string callback = HttpContext.Current.Server.UrlPathEncode(CALLBACK_URL + (query != null ? "?" + query : ""));

        string accessTokenUrl = string.Format("{0}?client_id={1}&redirect_uri={2}&client_secret={3}&code={4}",
        ACCESS_TOKEN, this.ConsumerKey, callback, this.ConsumerSecret, authToken);

        string response = WebRequest(Method.GET, accessTokenUrl, String.Empty);
        bool tokenset = false;
        if (response.Length > 0)
        {
            RawTokenResponse = response;
            //Store the returned access_token
            NameValueCollection qs = HttpUtility.ParseQueryString(response);

            if (qs["access_token"] != null)
            {
                tokenset = true;
                this.Token = qs["access_token"];
            }

            if (qs["expires"] != null)
            {
                int ts = Convert.ToInt32(qs["expires"]);
                this.Expires = DateTime.Now.AddSeconds(ts);
            }
        }
        if (!tokenset)
        {
            HttpContext.Current.Response.Write("No token set for url " + accessTokenUrl + " " + response);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
    }

    /// <summary>
    /// Web Request Wrapper
    /// </summary>
    /// <param name="method">Http Method</param>
    /// <param name="url">Full url to the web resource</param>
    /// <param name="postData">Data to post in querystring format</param>
    /// <returns>The web server response.</returns>
    public string WebRequest(Method method, string url, string postData)
    {

        HttpWebRequest webRequest = null;
        StreamWriter requestWriter = null;
        string responseData = "";

        webRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;
        webRequest.Method = method.ToString();
        webRequest.ServicePoint.Expect100Continue = false;
        webRequest.UserAgent = "GHClient";
        webRequest.Timeout = 20000;

        if (method == Method.POST)
        {
            webRequest.ContentType = "application/x-www-form-urlencoded";

            //POST the data.
            requestWriter = new StreamWriter(webRequest.GetRequestStream());

            try
            {
                requestWriter.Write(postData);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                requestWriter.Close();
                requestWriter = null;
            }
        }

        responseData = WebResponseGet(webRequest);
        webRequest = null;
        return responseData;
    }

    /// <summary>
    /// Process the web response.
    /// </summary>
    /// <param name="webRequest">The request object.</param>
    /// <returns>The response data.</returns>
    public string WebResponseGet(HttpWebRequest webRequest)
    {
        StreamReader responseReader = null;
        string responseData = "";
        WebResponse wr = null;
        try
        {
            wr = webRequest.GetResponse();
            responseReader = new StreamReader(wr.GetResponseStream());
            responseData = responseReader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            responseReader.Close();
            responseReader = null;
        }

        return responseData;
    }
}