﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Yintai.Architecture.Common;
using Yintai.Architecture.Common.Logger;
using Yintai.Architecture.Framework.ServiceLocation;

namespace com.intime.fashion.common
{
   public static class ErpServiceHelper
    {
       public const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
       public static bool SendHttpMessage(string url, dynamic requestData, Action<dynamic> successCallback, Action<dynamic> failCallback)
       {

           if (string.IsNullOrEmpty(url))
               throw new ArgumentException("url is empty");

           /*
           var requestbody = JsonConvert.SerializeObject(new
           {
               data = requestData
           });
            * */
           string requestUrl = ConstructHttpRequestUrl(url,requestData);
           var client = WebRequest.CreateHttp(requestUrl);
           client.ContentType = "application/json";
           client.Method = "Post";
           /*
           using (var request = client.GetRequestStream())
           using (var streamWriter = new StreamWriter(request))
           {
               streamWriter.Write(requestbody);
           }
            * */
           StringBuilder sb = new StringBuilder();
           using (var response = client.GetResponse())
           {
               var body = response.GetResponseStream();
               using (var streamReader = new StreamReader(body, Encoding.UTF8))
               {

                   sb.Append(streamReader.ReadToEnd());

               }
           }
           dynamic jsonResponse = null;
           try
           {
               jsonResponse = JsonConvert.DeserializeObject<dynamic>(sb.ToString());
           }
           catch (Exception ex)
           {
               Logger.Error(requestUrl);
               Logger.Error(sb.ToString());
               throw ex;
           }

           if (jsonResponse != null &&
               jsonResponse.error == string.Empty)
           {
               if (successCallback != null)
                   successCallback(jsonResponse);
               return true;
           }
           else
           {
               Logger.Debug(sb.ToString());
               if (failCallback != null)
                   failCallback(jsonResponse);
               return false;
           }


       }
       private static ILog Logger
       {
           get
           {
               return ServiceLocator.Current.Resolve<ILog>();
           }
       }
       private static string ConstructHttpRequestUrl(string host,object data)
       {

           Dictionary<string, string> query = new Dictionary<string, string>();
         /*
           query.Add("nonce", new Random(1000).Next().ToString());
           query.Add("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"));
           var signingValue = new StringBuilder();
           var signedValue = string.Empty;
           foreach (var s in query.Values.ToArray().OrderBy(s => s))
               signingValue.Append(s);
           using (HMACSHA1 hmac = new HMACSHA1(Encoding.ASCII.GetBytes(string.Empty)))
           {
               var hashValue = hmac.ComputeHash(Encoding.ASCII.GetBytes(signingValue.ToString()));
               signedValue = Convert.ToBase64String(hashValue);

           }
           query.Add("sign", signedValue);
          * */
           if (null != data)
           {
               foreach (var attr in data.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public))
               {
                   var attr_value = attr.GetValue(data);
                   if (attr_value is string)
                       query.Add(attr.Name, attr_value.ToString());
                   else
                       query.Add(attr.Name,JsonConvert.SerializeObject(attr.GetValue(data)));
       
               }
           }
           var requestUrl = new StringBuilder();
           requestUrl.Append(host);
           requestUrl.Append("?");
           return query.Keys.Aggregate(requestUrl, (s, e) => s.AppendFormat("{0}={1}&", e, HttpUtility.UrlEncode(query[e])), s => s.ToString());

       }


    }
}
