using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static inputparser.Program;

namespace inputparser
{
    class NewDatabaseBuilder
    {
        static int skippedQuestionCount = 0;
        static Dictionary<string, Dictionary<string, Test>> categories = new Dictionary<string, Dictionary<string, Test>>();
        static HashSet<string> alreadyDone = new HashSet<string>();
        public static void BuildDatabase(string[][] csvFile)
        {

            int totalQuestionCount = 0;
            foreach (var line in csvFile)
            {
                Console.Title = "Parsing... Total Questions: " + totalQuestionCount + $", Skipped: {skippedQuestionCount}";
                if (line.Length != 5) continue;
                //Parse CSV to useful
                var link = line[0];
                var category = line[1];
                var subcategory = line[2];
                var redownload = line[3] == "yes";
                var defaultSelect = line[4] == "yes";

                //Allow for dupes
                if (alreadyDone.Contains(link))
                {
                    Console.WriteLine("Duplicate link: " + link);
                    continue;
                }
                alreadyDone.Add(link);

                //first, try and retreive from cache 
                List<QANode> nodes = BuildAnswerCache.FetchCached(link);

                if (!redownload && nodes != null)
                {
                    totalQuestionCount += nodes.Count;
                    //quick scan to remove any nodes that have no answers or garbage answers
                    var nNodes = nodes.Where(a => a.Correct.Length > 0 && a.Correct.Count(b => b < 0 || b >= a.Answers.Length) == 0).Where(a => a.Answers.Length > 0).ToList();
                    skippedQuestionCount += nodes.Count - nNodes.Count;
                    if (nodes.Count - nNodes.Count > 0)
                        Console.WriteLine($"\tDeleting {nodes.Count - nNodes.Count} questions without answers");

                    //Add UIDs
                    foreach (var q in nNodes)
                    {
                        q.QuestionId = BuildAnswerCache.CalculateMD5Hash(q.Question);
                    }

                    Insert(category, subcategory, defaultSelect, nNodes);
                    continue;
                }

                //Otherwise, we have to redownload
                var pageData = DownloadWebpage(link);
                if (pageData == null)
                {
                    Console.WriteLine("404 Not Found: " + link);
                    continue;
                }
                Console.WriteLine($"  Downloading and parsing: {link}");
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(Encoding.UTF7.GetString(Encoding.UTF7.GetBytes(pageData)));

                //Figure out which parser
                if (link.ToLower().Contains("currentnursing"))
                    nodes = ParseCurrentNursing(link, html);

                if (link.ToLower().Contains("nurseslabs"))
                    nodes = ParseNursesLabs(link, html);

                //Delete questions with no answers
                if (nodes != null)
                {
                    var newNodes = nodes.Where(a => a.Correct.Length > 0 && a.Correct.Count(b => b < 0 || b >= a.Answers.Length) == 0).Where(a => a.Answers.Length > 0).ToList();
                    skippedQuestionCount += nodes.Count - newNodes.Count;
                    if (nodes.Count - newNodes.Count > 0)
                        Console.WriteLine($"\tDeleting {nodes.Count - newNodes.Count} questions without answers");
                    nodes = newNodes;

                    //Add UIDs
                    foreach (var q in nodes)
                    {
                        q.QuestionId = BuildAnswerCache.CalculateMD5Hash(q.Question);
                    }
                }

                //If not null, insert it, otherwise warn

                if (nodes == null)
                {
                    Console.WriteLine("\tFailed to Parse");
                }
                else Insert(category, subcategory, defaultSelect, nodes);
                if (nodes != null)
                    totalQuestionCount += nodes.Count;
            }

            //Finally, save the database
            File.WriteAllText("question_db.json", Newtonsoft.Json.JsonConvert.SerializeObject(categories, Newtonsoft.Json.Formatting.Indented));
        }

        static void Insert(string category, string subcategory, bool active, List<QANode> qs)
        {
            if (!categories.ContainsKey(category))
                categories.Add(category, new Dictionary<string, Test>());

            Test test = new Test();
            test.Category = category;
            test.Subcategory = subcategory;
            test.StartActive = false;

            if (!categories[category].ContainsKey(subcategory))
                categories[category].Add(subcategory, test);
            else
                test = categories[category][subcategory];

            test.Questions.AddRange(qs);
            if (active)
                test.StartActive = true;
        }

        static bool BuildFromCache(string link, Test test)
        {
            var fromCache = BuildAnswerCache.FetchCached(link);
            if (fromCache == null) return false;

            test.Questions = fromCache;
            return true;
        }

        static string DownloadWebpage(string link)
        {
            //Check if in cache
            var fname = "html_cache/" + BuildAnswerCache.CalculateMD5Hash(link) + ".txt";
            Directory.CreateDirectory("html_cache");
            if (File.Exists(fname))
            {
                //CONSOLE WRITE
                Console.WriteLine("\t(already in new cache)");
                return File.ReadAllText(fname);
            }

            try
            {
                var wc = new System.Net.WebClient();
                //COMMENT THIS TO ALLOW DOWNLOADS -- right now it won't actually download anything new
                return null;
                string downloaded = wc.DownloadString(link);

                //And save
                File.WriteAllText(fname, downloaded);
                return downloaded;
            }
            catch
            {
                return null;
            }
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

        static List<QANode> ParseCurrentNursing(string link, HtmlDocument html)
        {
            var results = new List<QANode>();
            //Mark the question nodes
            HtmlAgilityPack.HtmlNode[] qNode = null;
            int qNodeNumber = 0;
            //Mark the answer nodes
            HtmlAgilityPack.HtmlNode[] aNode = null;
            //Find the question node and number
            for (qNodeNumber = 0; qNodeNumber < 10; qNodeNumber++)
            {
                var baseNode = html.DocumentNode
                    ///                  
                    .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{qNodeNumber}]/td/table");
                if (baseNode == null) continue;
                qNode = baseNode.ChildNodes.Where(a => a.Name == "tr").ToArray();
                if (qNode.Length == 0)
                {
                    qNode = null;
                    continue;
                }
                break;
            }

            //Special case: qNode has 2 children = we were wrong
            if (qNode?.Count() == 2)
            {
                qNodeNumber = 0;
                for (qNodeNumber = 10; qNodeNumber > 0; qNodeNumber--)
                {
                    var baseNode = html.DocumentNode
                    ///                 /html/body/table/tr[8]/td[3]/table/tr[6]/td/table          
                    .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{qNodeNumber}]/td/div/table");
                    if (baseNode == null) baseNode = html.DocumentNode
                    .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{qNodeNumber}]/td/table");
                    if (baseNode == null) continue;
                    if (baseNode.InnerText.Contains("Back to Quiz Corner")) continue; //Answer node found first, not question node -- hazards of backwards searching
                    qNode = baseNode.ChildNodes.Where(a => a.Name == "tr").ToArray();
                    if (qNode.Length == 0)
                    {
                        qNode = null;
                        continue;
                    }
                    break;
                }
                if (qNode == null)
                {
                    Console.WriteLine("\tMisparsed document with 2 children, discarding");
                    return null;
                }
            }

            if (qNode == null)
            {
                Console.WriteLine("\tCould not find question node");
                skippedQuestionCount++;
                return null;
            }

            //Finding answer nodes: first, scan the question nodes to see if someone slipped an answer key in there
            //Skip the first two nodes since there's occasionally parser errors in them
            for (int i = 0; i < qNode.Length - 2; i++)
            {
                //Three criteria: the node contains the word "answer key", the next node contains 1. (the prerequisite to the first answer), and that node does not contain a "B. " which indicates an answer
                if (qNode[i].InnerText.Contains("Answer Key") && qNode[i + 1].InnerText.Contains("1.") && !qNode[i + 1].InnerText.Contains("B.") &&
                    (qNode[i + 1].InnerText.Contains("2.") || qNode[i + 2].InnerText.Contains("2.")))
                {
                    // Answer node is part of question node -- why does every single page have a different structure
                    aNode = qNode.Skip(i).ToArray();
                    break;
                }
            }

            //Find answer node
            //First, we check if there's an answer node directly in the same root as the question node (happens sometimes)
            //                                                                                                                  
            HtmlNode baseAnswerNode = html.DocumentNode.SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{qNodeNumber}]/td/table[2]");
            if (baseAnswerNode != null && aNode == null)
            {
                //Select sub nodes -- we found it maybe
                aNode = baseAnswerNode.ChildNodes.Where(a => a.Name == "tr").ToArray();
                if (aNode.Length == 0) aNode = null;
                baseAnswerNode = null;
            }

            //More aggressive search if needed
            if (aNode == null)
                for (var aNum = qNodeNumber + 1; aNum < 15; aNum++)
                {///html/body/table/tbody/tr[8]/td[3]/table/tbody/tr[7]/td/table/tbody
                    baseAnswerNode = html.DocumentNode
                        .SelectSingleNode($"/html/body/table/tr[8]/td[3]/table/tr[{aNum}]/td/table");
                    if (baseAnswerNode == null) continue;

                    //RARELY, a Tbody node exists and we have to adapt
                    var tbodyFixNode = baseAnswerNode.ChildNodes.Where(a => a.Name == "tbody").ToArray();
                    if (tbodyFixNode.Count() > 0)
                    {
                        baseAnswerNode = tbodyFixNode[0];
                    }

                    aNode = baseAnswerNode.ChildNodes.Where(a => a.Name == "tr").ToArray();
                    if (aNode.Length == 0) aNode = null;
                    //Stop searching if found
                    if (aNode != null) break;
                }

            //Trim the answer node
            if (aNode == null)
            {
                Console.WriteLine("\tCould not find find answer node!");
                skippedQuestionCount++;
                return null;
            }
            //Skip first subnode as it just says "Answer Key"
            if (aNode[0].InnerText.Contains("Answer Key"))
                aNode = aNode.Skip(1).ToArray();
            //Skip last two nodes which say Back to Quiz Corner Home and Back To Top
            //if (aNode.Last().InnerText.Contains("Back to Quiz Corner"))
            //   aNode = aNode.Take(aNode.Length - 2).ToArray();
            //If contains references node, remove it
            //if (aNode.Last().InnerText.Contains("References"))
            //  aNode = aNode.Take(aNode.Length - 1).ToArray();



            string answersUnstructured = "";
            foreach (var ans in aNode)
                answersUnstructured += ans.InnerText;

            for (int i = 0; i < qNode.Length; i++)
            {
                skippedQuestionCount++;

                var questionFull = System.Web.HttpUtility.HtmlDecode(qNode[i].InnerText.Trim());
                if (questionFull.EndsWith("Answer Key"))
                    questionFull = questionFull.Substring(0, questionFull.Length - "Answer Key".Length);
                //Scan for correct answer
                int answerOff = answersUnstructured.IndexOf($"{i + 1}.");
                if (answerOff == -1)
                {
                    Console.WriteLine($"\tQuestion #{i + 1} malformed. Unable to locate question number in answer key.");
                    skippedQuestionCount++;
                    continue;
                }
                answerOff += $"{i + 1}.".Length;
                int correctAnswer = LetterToInt(answersUnstructured.ToUpper().Substring(answerOff).Trim()[0].ToString());
                correctAnswer -= 1;
                if (correctAnswer < 0)
                {
                    Console.WriteLine($"\tQuestion #{i + 1} malformed. Unable to parse answer {answersUnstructured[answerOff].ToString()} correctly.");
                    skippedQuestionCount++;
                    continue;
                }
                //Scan for the first answer
                int indexOffset = questionFull.IndexOf("A. ");
                if (indexOffset == -1)
                {
                    Console.WriteLine($"\tQuestion #{i + 1} malformed. Unable to find answer 'A'");
                    skippedQuestionCount++;
                    continue;
                }

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

                    //To avoid searching backwards and tripping on initials (e.g. Albert E. Smith), we cut off as we move forward
                    int nextAns = questionFull.Substring(ansIndex).IndexOf($"{nextItem}. ");
                    if (nextAns == -1)
                        answers.Add(questionFull.Substring(ansIndex).Trim().Replace("\r\n", ""));
                    else answers.Add(questionFull.Substring(ansIndex, nextAns).Trim().Replace("\r\n", ""));
                }
                if (answers.Count == 0)
                {
                    Console.WriteLine($"\tQuestion #{i} malformed. Could not parse any answers");
                    skippedQuestionCount++;
                    continue;
                }

                results.Add(new QANode
                {
                    SourceUrl = link,
                    Question = question,
                    Answers = answers.ToArray(),
                    Rationale = "",
                    IsSelectAll = false,
                    Correct = new[] { correctAnswer
}
                });

                skippedQuestionCount--;
            }
            return results;
        }

        static List<QANode> ParseNursesLabs(string link, HtmlDocument html)
        {
            var nodes = new List<QANode>();
            ///html[1]/body[1]/div[6]/div[2]/div[1]/article[1]/div[2]/div[1]/div[1]/div[1]/div[4]/div[3]
            //                                        /html/body/div[6]/div[2]/div/div[2]/div[1]/div/article/div[3]/div[4]/div/div[3]
            var node = html.DocumentNode.SelectNodes("/html/body/div[6]/div[2]/div/div[2]/div[1]/div/article/div[3]/div[4]/div[3]");
            if (node == null)
                node = html.DocumentNode.SelectNodes("/html[1]/body[1]/div[6]/div[2]/div[1]/article[1]/div[2]/div[1]/div[1]/div[1]/div[3]/div[3]");
            if (node == null)
                node = html.DocumentNode.SelectNodes("/html/body/div[6]/div[2]/div/div[2]/div[1]/div/article/div[3]/div[3]/div[3]");
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
            //int questionNum = 0;
            int failCount = 0;
            for (int questionNum = 1; questionNum < 1000; questionNum++)
            {
                //Find an index for it
                int questionIndex = 0;
                for (questionIndex = 0; questionIndex < qNodes.Length; questionIndex++)
                {
                    if (qNodes[questionIndex].InnerText.StartsWith((questionNum).ToString() + "."))
                        break;
                }
                if (questionIndex == qNodes.Length)
                {
                    if (failCount > 4 || questionNum * 2 >= qNodes.Length) break;
                    Console.WriteLine("Unable to locate node for question #" + questionNum);
                    skippedQuestionCount++;
                    failCount++;
                    continue;
                }

                ////Skip malformed questions
                //questionNum++;
                //if (!qNodes[i].InnerText.StartsWith((questionNum).ToString()))
                //{
                //    //Look ahead slightly

                //    if (!qNodes[i + 1].InnerText.StartsWith((questionNum).ToString()))
                //    {
                //        Console.WriteLine("Skipping malformed question");
                //        continue;
                //    }
                //    else i++;
                //}
                var question = qNodes[questionIndex].InnerText.Substring($"{questionNum}. ".Length);


                //Scan up to 5 farther for a <ul> block
                foreach (var q in qNodes.Skip(questionIndex + 1))
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
                {
                    string itxt = aNodes[ax].InnerText.Trim();
                    if (itxt.StartsWith(questionNum.ToString() + ".") ||
                    itxt.StartsWith(questionNum.ToString() + " ") ||
                    itxt.StartsWith(questionNum.ToString() + "?") || itxt.StartsWith(questionNum.ToString() + "&nbsp;"))
                    {
                        aNode = aNodes[ax];
                        //Check for a rationale
                        if (ax + 1 < aNodes.Length)
                            if (aNodes[ax + 1].InnerText.Length > 0 && !int.TryParse(aNodes[ax + 1].InnerText[0].ToString(), out _))
                                rNode = aNodes[ax + 1];
                        break;
                    }
                }

                if (aNode == null)
                {
                    Console.WriteLine("Unable to locate answer for question #" + questionNum);
                    skippedQuestionCount++;
                    //skippedQuestionCount++;
                    continue;
                }
                if (selectAll)
                {
                    var itx = aNode.InnerText.Replace("&nbsp;", "");
                    if (itx.Contains("and") && itx.Length < "2. Answer:&nbsp;  A. 1 and 3".Length)
                    {
                        itx = itx.Replace("and", ",");
                    }
                    string ans = itx.Substring(itx.IndexOf(":") + 1);
                    if (ans[0] == '?')
                        ans = ans.Substring(1);
                    correct = ans.Trim().Split(", ").Select(a => a.Trim()).Where(a => a.Length > 1).Select(a =>
                         {
                             a = a.Trim().Substring(0, 1).ToUpper();
                             int ix = 0;
                             if (int.TryParse(a, out ix))
                                 return ix - 1;
                             var l = LetterToInt(a);
                             if (l == -1) throw new Exception(itx);
                             return l - 1;
                         }).ToArray();

                    if (correct.Length == 1)
                    {
                        var cor = new List<int>();
                        //Reparse
                        var opts = new[] { "A", "B", "C", "D", "E", "F", "G" };
                        foreach (var opt in opts)
                        {
                            if (itx.Contains(opt + ". "))
                            {
                                cor.Add(LetterToInt(opt) - 1);
                            }
                        }

                        correct = cor.ToArray();

                        if (correct.Length == 1)
                        {
                        }

                        if (correct.Length == 1)
                        {
                            Console.WriteLine("\tOnly 1 correct answer to select all question #" + questionNum);
                            skippedQuestionCount++;
                            continue;

                        }
                    }
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
                var answers = qNodes[questionIndex + 1].InnerHtml.Split("\n").Select(a => a.Substring(3)).ToArray();
                //If only 1 answer, we misparsed
                if (answers.Length == 1)
                    answers = qNodes[questionIndex + 1].InnerHtml.Split("<br>").Where(a => a.Length > 3).Select(a => a.Substring(3)).Select(a => Regex.Replace(a, "<.*?>", string.Empty)).ToArray();
                if (answers.Length == 1)
                    answers = qNodes[questionIndex + 1].InnerHtml.Split("</li>").Where(a => a.Length > 3).Select(a => a.Substring(3)).Select(a => Regex.Replace(a, "<.*?>", string.Empty)).ToArray();
                if (answers.Length == 1)
                {
                    //Skip
                    Console.WriteLine("\tOnly 1 answer to question #" + questionNum);
                    skippedQuestionCount++;
                    continue;
                }
                var rationale = rNode?.InnerText ?? "";
                nodes.Add(new QANode
                {
                    Question = System.Web.HttpUtility.HtmlDecode(question),
                    SourceUrl = link,
                    Answers = answers.Select((Func<string, string>)System.Web.HttpUtility.HtmlDecode).ToArray(),
                    Rationale = System.Web.HttpUtility.HtmlDecode(rationale),
                    IsSelectAll = selectAll,
                    Correct = correct
                });
            }
            return nodes;
        }
    }
}
