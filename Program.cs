using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Context;
using Spring.Context.Support;
using BotR.API;
using System.Xml.Linq;
using System.IO;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace BotRAPIConsole {
    class Program {

        static void Main(string[] args) {
            try {

                IApplicationContext ctx = ContextRegistry.GetContext();
                BotRAPI api = ctx.GetObject("BotRAPI") as BotRAPI;

                //test video listing
                Console.WriteLine(api.Call("/videos/list"));

                //params to store with a new video
                NameValueCollection col = new NameValueCollection() {

                {"title", "New test video"},
                    {"tags", "new, test, video, upload"},
                    {"description", "New video2"},
                    {"link", "http://www.bitsontherun.com"},
                    {"author", "Bits on the Run"}
                };

                //create the new video
                string xml = api.Call("/videos/create", col);

                Console.WriteLine(xml);

                XDocument doc = XDocument.Parse(xml);
                var result = (from d in doc.Descendants("status")
                              select new {
                                  Status = d.Value
                              }).FirstOrDefault();

                //make sure the status was "ok" before trying to upload
                if (result.Status.Equals("ok", StringComparison.CurrentCultureIgnoreCase)) {

                    var response = doc.Descendants("link").FirstOrDefault();
                    string url = string.Format("{0}://{1}{2}", response.Element("protocol").Value, response.Element("address").Value, response.Element("path").Value);

                    string filePath = Path.Combine(Environment.CurrentDirectory, "test.mp4");

                    col = new NameValueCollection();
                    FileStream fs = new FileStream(filePath, FileMode.Open);

                    col["file_size"] = fs.Length.ToString();
                    col["file_md5"] = BitConverter.ToString(HashAlgorithm.Create("MD5").ComputeHash(fs)).Replace("-", "").ToLower();
                    col["key"] = response.Element("query").Element("key").Value;
                    col["token"] = response.Element("query").Element("token").Value;

                    fs.Dispose();
                    string uploadResponse = api.Upload(url, col, filePath);

                    Console.WriteLine(uploadResponse);
                }

            } catch (Exception ex) {
                Console.WriteLine(ex.GetBaseException().Message);
            }
        }
    }
}
