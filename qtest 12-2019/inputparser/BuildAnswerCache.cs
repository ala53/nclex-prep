using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static inputparser.Program;
using System.Linq;

namespace inputparser
{
    public class BuildAnswerCache
    {
        /// <summary>
        /// Builds dynamic cache from the database json file
        /// </summary>
        /// <param name="link"></param>
        public static int BuildDynCache(string link, List<QANode> questions)
        {
            //Generate file md5
            var fname = CalculateMD5Hash(link);
            int count = 0;

            if (link.ToLower().Contains("nurseslabs"))
            {
                if (link.ToLower().EndsWith("-items/"))
                {
                    //Get the 2 numbers before this
                    var substr = link.Substring(link.Length - "22-items/".Length, 2);
                    if (!int.TryParse(substr, out count))
                    {
                        count = 0;
                    }
                }
            }

            System.IO.Directory.CreateDirectory("parsed_question_cache");
            //Find all qs
            List<QANode> myNodes = new List<QANode>();
            foreach (var node in questions)
            {
                if (node.SourceUrl == link)
                    myNodes.Add(node);
            }

            System.IO.File.WriteAllText("parsed_question_cache/" + fname + ".txt", Newtonsoft.Json.JsonConvert.SerializeObject(myNodes));
            if (count > myNodes.Count || myNodes.Count == 0)
            {
                Console.WriteLine($"\t{count}\t{myNodes.Count}\t{link}");
            }
            return myNodes.Count;
        }

        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static List<QANode> FetchCached(string link)
        {
            var fname = CalculateMD5Hash(link);
            fname = "parsed_question_cache/" + fname + ".txt";
            if (System.IO.File.Exists(fname))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<QANode>>(System.IO.File.ReadAllText(fname));
            }
            return null;
        }
    }
}
