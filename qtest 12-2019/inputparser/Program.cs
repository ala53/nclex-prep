using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace inputparser
{
    public class Program
    {
        static Dictionary<string, Dictionary<string, Test>> testdic = new Dictionary<string, Dictionary<string, Test>>();
        static void Main(string[] args)
        {
            //System.IO.Directory.Delete("cache");
            System.IO.Directory.CreateDirectory("cache");
            Console.WriteLine("Hello World!");
            var inparse = new CsvParser.CsvParser(',');
            var res = inparse.Parse(System.IO.File.ReadAllText("dataset_reparse.csv")); //See dataset.csv for corresponding to old code below

            /*  //Generate question set
              Dictionary<string, Dictionary<string, Test>> loaded = 
              Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Test>>>(System.IO.File.ReadAllText("database.json"));

              List<QANode> qs = new List<QANode>();
              foreach (var str in loaded)
              {
                  foreach (var child in str.Value)
                  {
                      qs.AddRange(child.Value.Questions);
                  }
              }
              //Build answer cache
              var qc = 0;
              foreach (var line in res)
              {
                  if (line.Length != 3)
                  {
                      Console.WriteLine($"Skipping {line}");
                      continue;
                  }
                qc +=  BuildAnswerCache.BuildDynCache(line[0], qs); //link
              }
              */


            NewDatabaseBuilder.BuildDatabase(res.ToArray());
            Console.ReadLine();
            return;
            //OLD CODE
            int id = 0;
            foreach (var line in res)
            {
                try
                {
                    if (line.Length != 3) continue;
                    id++;
                    var link = line[0];
                    var cat = line[1];
                    var cat_special = line[2];
                    var wc = new System.Net.WebClient();
                    string contents;
                    if (!System.IO.File.Exists($"cache/{id}.html"))
                    {
                        contents = wc.DownloadString(link);
                        System.IO.File.WriteAllText($"cache/{id}.html", contents);
                        Console.WriteLine($"{id} Downloaded {link}");
                    }
                    else
                        contents = System.IO.File.ReadAllText($"cache/{id}.html");
                    wc.Dispose();

                    Test test;
                    Dictionary<string, Test> testGroup;
                    if (!testdic.TryGetValue(cat, out testGroup))
                    {
                        testGroup = testdic[cat] = new Dictionary<string, Test>();
                    }
                    if (!testGroup.TryGetValue(cat_special, out test))
                    {
                        test = testGroup[cat_special] = new Test
                        {
                            Category = cat,
                            Subcategory = cat_special,
                        };
                    }

                    Console.WriteLine($"Parsing # {id} / {link}");
                    if (link.StartsWith("https://nurseslabs.com"))
                    {
                        var nodes = NurseLabParse(contents, link);
                        test.Questions.AddRange(nodes);
                    }
                    else if (link.StartsWith("http://currentnursing.com"))
                    {
                        var nodes = ParseCurrentNursing(contents, link);
                        test.Questions.AddRange(nodes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Download Error!");
                    Console.WriteLine(ex.ToString());

                }
            }
            Console.WriteLine("Saving...");
            System.IO.File.WriteAllText("database.json", Newtonsoft.Json.JsonConvert.SerializeObject(testdic, Newtonsoft.Json.Formatting.Indented));

            Console.WriteLine("Categories (" + testdic.Count + ")");

            int qCt = 0;
            foreach (var group in testdic)
            {
                Console.WriteLine("\t - " + group.Key);
                foreach (var sg in group.Value)
                {
                    Console.WriteLine("\t\t - " + sg.Key);
                    qCt += sg.Value.Questions.Count;
                }

            }
            Console.WriteLine($"{qCt} questions");

            Console.ReadLine();
        }

        public class Test
        {
            public string Category;
            public string Subcategory;
            public List<QANode> Questions = new List<QANode>();
            public bool StartActive = false;
        }
        public class QANode
        {
            public string SourceUrl;
            public string Question, Rationale;
            public string QuestionId;
            public bool IsSelectAll;
            public string[] Answers;
            public int[] Correct;
        }
        static QANode[] NurseLabParse(string content, string url)
        {
            var nodes = new List<QANode>();
            try
            {

                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(
                    System.Text.Encoding.UTF7.GetString(System.Text.Encoding.UTF7.GetBytes(content)));
                var node = html.DocumentNode.SelectNodes("/html[1]/body[1]/div[6]/div[2]/div[1]/article[1]/div[2]/div[1]/div[1]/div[1]/div[4]/div[3]");
                if (node == null)
                    node = html.DocumentNode.SelectNodes("/html[1]/body[1]/div[6]/div[2]/div[1]/article[1]/div[2]/div[1]/div[1]/div[1]/div[3]/div[3]");
                var groupNode = node[0].ChildNodes.ToList();
                var firstQ = groupNode.IndexOf(groupNode.First(a => a.InnerText.StartsWith("1.")));
                groupNode = groupNode.Skip(firstQ).ToList(); //Skip intro
                                                             //Find all the nodes before the <h3 nodes>
                var aTagNode = groupNode.IndexOf(groupNode.First(a => a.Name == "h3" || a.InnerText.StartsWith("Answers and Rationale")));
                var qNodes = groupNode.Take(aTagNode).Where(a => a.Name == "p" || a.Name == "ul").Where(a =>
                {
                    var txt = a.InnerText;
                    if (txt.Length == 0 || txt == null)
                        return false;
                    return true;
                    if (int.TryParse(txt.Substring(0, 1), out _))
                        return true;
                    else if (txt.StartsWith("A"))
                        return true;
                    return false;

                }).ToArray();
                var aNodes = groupNode.Skip(aTagNode + 1).Take(groupNode.Count - 1 - aTagNode).Where(a => a.Name == "p").ToArray();
                int questionNum = 0;
                for (int i = 0; i < qNodes.Count(); i += 2)
                {
                    try
                    {
                        //Skip malformed questions
                        if (!qNodes[i].InnerText.StartsWith((questionNum + 1).ToString()))
                        {
                            Console.WriteLine("Skipping malformed question");
                            continue;
                        }
                        questionNum++;


                        var question = qNodes[i].InnerText.Substring($"{questionNum}. ".Length);
                        //Scan up to 5 farther for a <ul> block
                        foreach (var q in qNodes.Skip(i + 1))
                        {
                            if (q.Name == "p")
                                break;
                            else if (q.Name == "ul")
                            {
                                question += "\n" + q.InnerText;
                                break;
                            }
                        }
                        var selectAll = question.ToLower().Contains("all that apply");
                        int[] correct;
                        //Scan for the correct answer node
                        HtmlAgilityPack.HtmlNode aNode = null;
                        HtmlAgilityPack.HtmlNode rNode = null;
                        for (int ax = 0; ax < aNodes.Length; ax++)
                            if (aNodes[ax].InnerText.StartsWith(questionNum.ToString() + ".") ||
                            aNodes[ax].InnerText.StartsWith(questionNum.ToString() + " ") ||
                            aNodes[ax].InnerText.StartsWith(questionNum.ToString() + "?"))
                            {
                                aNode = aNodes[ax];
                                //Check for a rationale
                                if (ax + 1 < aNodes.Length)
                                    if (aNodes[ax + 1].InnerText.Length > 0 && !int.TryParse(aNodes[ax + 1].InnerText[0].ToString(), out _))
                                        rNode = aNodes[ax + 1];
                                break;
                            }
                        if (selectAll)
                        {

                            string ans = aNode.InnerText.Substring(aNode.InnerText.IndexOf(":") + 1);
                            if (ans[0] == '?')
                                ans = ans.Substring(1);
                            correct = ans.Trim().Split(", ").Select(a =>
                            {
                                a = a.Substring(0, 1).ToUpper();
                                int ix = 0;
                                if (int.TryParse(a, out ix))
                                    return ix - 1;
                                var l = LetterToInt(a);
                                if (l == -1) throw new Exception(aNode.InnerText);
                                return l - 1;
                            }).ToArray();
                        }
                        else
                        {
                            string t = "X";
                            var opt = aNode.InnerText.IndexOf(":");
                            if (aNode.InnerText.Contains("Answer?"))
                            {
                                opt = $"{questionNum}. Answe".Length;
                            }
                            if (opt == -1)
                                opt = $"{questionNum}. Answer".Length;
                            t = aNode.InnerText.Substring(opt + 2, 1);
                            int ix = 0;
                            if (!int.TryParse(t, out ix))
                                ix = LetterToInt(t);
                            correct = new[] { ix - 1 };
                            if (correct[0] == -1) throw new Exception();
                        }
                        var answers = qNodes[i + 1].InnerText.Split("\n").Select(a => a.Substring(3)).ToArray();
                        var rationale = rNode?.InnerText ?? "";
                        nodes.Add(new QANode
                        {
                            Question = System.Web.HttpUtility.HtmlDecode(question),
                            SourceUrl = url,
                            Answers = answers.Select((Func<string, string>)System.Web.HttpUtility.HtmlDecode).ToArray(),
                            Rationale = System.Web.HttpUtility.HtmlDecode(rationale),
                            IsSelectAll = selectAll,
                            Correct = correct
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);

                    }
                }
            }
            catch (Exception ex)
            {
                throw;
                Console.WriteLine("Skipping entire test. FATAL.");
                Console.WriteLine(ex);

            }
            return nodes.ToArray();
        }

        static int LetterToInt(string ltr)
        {
            var letter = ltr[0];
            switch (letter)
            {
                case 'A':
                    return 1;
                case 'B':
                    return 2;
                case 'C':
                    return 3;
                case 'D':
                    return 4;
                case 'E':
                    return 5;
                case 'F':
                    return 6;
                case 'G':
                    return 7;
                case 'H':
                    return 8;
                case 'I':
                    return 9;
                case 'J':
                    return 10;
                case 'K':
                    return 11;
                case 'L':
                    return 12;
                default: return -1;
            }
        }


        static QANode[] ParseCurrentNursing(string content, string url)
        {
            var results = new List<QANode>();
            try
            {
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(
                    System.Text.Encoding.UTF7.GetString(System.Text.Encoding.UTF7.GetBytes(content)));
                HtmlAgilityPack.HtmlNode[] qNode = null;
                HtmlAgilityPack.HtmlNode[] aNode = null;
                for (var nodeNum = 0; nodeNum < 10; nodeNum++)
                {
                    try
                    {
                        qNode = html.DocumentNode
                        .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{nodeNum}]/td/table")
                        ?.ChildNodes?.Where(a => a.Name == "tr")?.ToArray();
                        if (qNode != null)
                        {
                            bool wasFound = false;
                            for (var aNum = nodeNum + 1; aNum < 11; aNum++)
                            {
                                aNode = html.DocumentNode
                                .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{aNum}]/td/table")
                                ?.ChildNodes?.Where(a => a.Name == "tr")?.ToArray();
                                if (aNode != null)
                                {
                                    //Skip first one "Answer Key"
                                    aNode = aNode.Skip(1).ToArray();
                                    //Skip last two: Back to Quiz Corner Home and Back To Top
                                    aNode = aNode.Take(aNode.Length - 1).ToArray();
                                    wasFound = true;
                                    break;
                                }
                            }
                            if (wasFound)
                                break;
                        }
                    }
                    catch { }
                }

                string answersUnstructured = "";
                foreach (var ans in aNode)
                    answersUnstructured += ans.InnerText;

                for (int i = 0; i < qNode.Length; i++)
                {
                    try
                    {
                        var questionFull = System.Web.HttpUtility.HtmlDecode(qNode[i].InnerText.Trim());
                        if (questionFull.EndsWith("Answer Key"))
                            questionFull = questionFull.Substring(0, questionFull.Length - "Answer Key".Length);
                        //Scan for correct answer
                        int answerOff = answersUnstructured.IndexOf($"{i + 1}. ");
                        if (answerOff == -1)
                            throw new Exception("Unable to locate correct answer");
                        answerOff += $"{i + 1}. ".Length;
                        int correctAnswer = LetterToInt(answersUnstructured[answerOff].ToString());
                        correctAnswer -= 1;
                        if (correctAnswer == -1)
                            throw new Exception("Unable to parse correct answer");
                        //Scan for the first 1
                        int indexOffset = questionFull.IndexOf("A. ");
                        if (indexOffset == -1)
                            throw new Exception("Unable to find answer 'A'");

                        var question = questionFull.Substring($"{i + 1}. ".Length, indexOffset - $"{i + 1}. ".Length).Trim().Replace("\r\n", "");
                        var answers = new List<string>();
                        var possibilities = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" };
                        for (int j = 0; j < possibilities.Length - 1; j++)
                        {
                            var possibility = possibilities[j];
                            var nextItem = possibilities[j + 1];
                            int ansIndex = questionFull.IndexOf($"{possibility}. ");
                            if (ansIndex == -1)
                                break;
                            ansIndex += 3;
                            int nextAns = questionFull.IndexOf($"{nextItem}. ");
                            if (nextAns == -1)
                                answers.Add(questionFull.Substring(ansIndex).Trim().Replace("\r\n", ""));
                            else answers.Add(questionFull.Substring(ansIndex, nextAns - ansIndex).Trim().Replace("\r\n", ""));
                        }
                        if (answers.Count == 0)
                            throw new Exception("No answers parsed");

                        results.Add(new QANode
                        {
                            SourceUrl = url,
                            Question = question,
                            Answers = answers.ToArray(),
                            Rationale = "",
                            IsSelectAll = false,
                            Correct = new[] { correctAnswer
}
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Skipping malformed question");
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Skipping entire test. FATAL");
                Console.WriteLine(ex.ToString());
            }
            return results.ToArray();
        }
    }
}
