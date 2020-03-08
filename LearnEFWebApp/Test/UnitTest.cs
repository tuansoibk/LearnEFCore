using System;
using System.Collections.Generic;
using System.IO;
using LearnEFWebApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace LearnEFWebApp.Test
{
    public class UnitTest
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        
        private readonly ITestOutputHelper output;

        public UnitTest(ITestOutputHelper output)
        {
            this.output = output;
        }
        
        [Fact]
        public void TestInit()
        {
            dynamic initData = JsonConvert.DeserializeObject(ReadFile(@"C:\workspaces\Learning\LearnEFWebApp\LearnEFWebApp\initdata.json"));
            List<Author> authors = new List<Author>();
            foreach (JObject jObject in initData.authors)
            {
                var author = jObject.ToObject<Author>();
                output.WriteLine(JsonConvert.SerializeObject(author, Formatting.Indented, JsonSerializerSettings));
            }
            output.WriteLine(JsonConvert.SerializeObject(authors, Formatting.Indented, JsonSerializerSettings));
        }

        private string ReadFile(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Open);
            using StreamReader reader = new StreamReader(fileStream);
            return reader.ReadToEnd();
        }
    }
}