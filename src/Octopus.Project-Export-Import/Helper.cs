using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Octopus.Client.Model;

namespace OctopusClient
{
    public static class Helper
    {

        #region Public Methods

        public static bool ActionsAreEquals(this DeploymentActionResource dpar1, DeploymentActionResource dpar2)
        {
            return dpar1.ActionType == dpar2.ActionType &&
                   dpar1.Name == dpar2.Name;
        }

        public static bool ActionsAreEquals(this IList<DeploymentActionResource> dpar1, IList<DeploymentActionResource> dpar2)
        {

            if (dpar1 == null || dpar2 == null || dpar1.Count != dpar2.Count)
            {
                return false;
            }
            for (var i = 0; i < dpar1.Count; i++)
            {
                if (!dpar1.ElementAt(i).ActionsAreEquals(dpar2.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;
        }

        public static WebResponse BuildPostRequest(string url, JObject data)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Headers.Add("X-Octopus-ApiKey", Context.ApiKey);
            webRequest.ContentType = "application/json;charset=utf-8";
            webRequest.Method = WebRequestMethods.Http.Post;
            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                streamWriter.Write(data.ToString());
                streamWriter.Flush();
                streamWriter.Close();
            }

            return webRequest.GetResponse();
        }

        public static WebResponse BuildPutRequest(string url, JObject data)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Headers.Add("X-Octopus-ApiKey", Context.ApiKey);
            webRequest.ContentType = "application/json;charset=utf-8";
            webRequest.Method = WebRequestMethods.Http.Put;
            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                streamWriter.Write(data.ToString());
                streamWriter.Flush();
                streamWriter.Close();
            }

            return webRequest.GetResponse();
        }


        public static WebResponse BuildResponse(string url)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Headers.Add("X-Octopus-ApiKey", Context.ApiKey);
            return webRequest.GetResponse();
        }

        public static bool ChannelsAreEquals(this ChannelResource chr1, ChannelResource chr2)
        {
            var x1 = chr1.Description == chr2.Description;
            var x2 = chr1.IsDefault == chr2.IsDefault;
            var x3 = chr1.Name == chr2.Name;
            var x4 = chr1.LifecycleId == chr2.LifecycleId;
            return x1 && x2 && x3 && x4;


        }

        public static bool ChannelsAreEquals(this IList<ChannelResource> chr1, IList<ChannelResource> chr2)
        {
            if (chr1 == null || chr2 == null || chr1.Count != chr2.Count)
            {
                return false;
            }
            for (var i = 0; i < chr1.Count; i++)
            {
                if (!chr1.ElementAt(i).ChannelsAreEquals(chr2.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;
        }

        public static void CreateFolderIfNotExists(string location, string folderName)
        {
            if (!Directory.Exists(Path.Combine(location, folderName)))
                Directory.CreateDirectory(Path.Combine(location, folderName));
        }

        public static string GenerateOctopusSlug(this string phrase)
        {
            var str = phrase.RemoveAccent().ToLower();

            str = Regex.Replace(str, @"[\.+@$*%\^]", "-");
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // replace spaces with hyphen
            str = Regex.Replace(str, @"\-+", "-"); // remove multiple hyphens   
            str = Regex.Replace(str, @"\-*$", ""); // remove hyphens in the end of the string
            return str;
        }

        public static bool ProcessesAreEquals(this DeploymentStepResource dpsr1, DeploymentStepResource dpsr2)
        {
            var cond1 = dpsr1.Name == dpsr2.Name;
            var listActionsBool = new List<bool>();

            for (var i = 0; i < dpsr1.Actions.Count; i++)
            {
                if (dpsr1.Actions[i].Name.Equals(dpsr2.Actions[i].Name) &&
                    dpsr1.Actions[i].ActionType.Equals(dpsr2.Actions[i].ActionType))
                {
                    listActionsBool.Add(true);
                }

                else
                {
                    listActionsBool.Add(false);
                }
            }
            var cond2 = !listActionsBool.Contains(false);
            var cond3 = dpsr1.Condition == dpsr2.Condition;
            return cond1 && cond2 && cond3;
        }

        public static bool ProcessesAreEquals(this IList<DeploymentStepResource> dpr1,
            IList<DeploymentStepResource> dpr2)
        {

            if (dpr1 == null || dpr2 == null || dpr1.Count != dpr2.Count)
            {
                return false;
            }
            for (var i = 0; i < dpr1.Count; i++)
            {
                if (!dpr1.ElementAt(i).ProcessesAreEquals(dpr2.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;
        }

        public static string RemoveAccent(this string txt)
        {
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }

        public static string RetrieveJsonResponse(string url)
        {
            var webResp = BuildResponse(url);
            if (webResp == null) return null;

            var responseStream = webResp.GetResponseStream();
            if (responseStream == null) return null;

            var reader = new StreamReader(responseStream);
            var responseFromServer = reader.ReadToEnd();
            return responseFromServer;
        }
        public static bool VariablesAreEqual(this VariableResource vr1, VariableResource vr2)
        {
            return
                vr1.Id.Equals(vr2.Id) &&
                vr1.Scope.Equals(vr2.Scope) &&
                vr1.Name.Equals(vr2.Name) &&
                vr1.Value == vr2.Value &&
                vr1.IsSensitive.Equals(vr2.IsSensitive) &&
                vr1.IsEditable.Equals(vr2.IsEditable);
        }

        public static bool VariablesAreEqual(this IList<VariableResource> evr1, IList<VariableResource> evr2)
        {
            if (evr1 == null || evr2 == null || evr1.Count != evr2.Count)
            {
                return false;
            }

            for (var i = 0; i < evr1.Count; i++)
            {
                if (!evr1.ElementAt(i).VariablesAreEqual(evr2.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion Public Methods
    }
}
