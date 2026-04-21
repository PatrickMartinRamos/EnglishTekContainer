using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;

namespace EnglishTek.Grade1.ID213
{
    public static class GameManager
    {
        #region Variables
        // ID of the game
        public static int GameID { set; get; }
        // Instructions of the game
        public static string Instructions { set; get; }
        // Difficulty level
        public static string Difficulty { set; get; }
        // Player score
        public static int Score { set; get; }
        // Current item
        public static int Item { set; get; }
        // Total items in the game
        public static int TotalItem { set; get; }
        // Current question
        public static string Question { set; get; }
        // Current correct answer
        public static string Correct { set; get; }
        // Current wrong answer
        public static string Wrong { set; get; }

        // List of questions
        static List<string> Questions { set; get; }
        // List of correct answers
        static List<string> Corrects { set; get; }
        // List of wrong answers
        static List<string> Wrongs { set; get; }
        #endregion

        public static void Initialize()
        {
            GameID = GetId();
            Instructions = GetInstructions();
            Score = 0;
            Item = 0;
            TotalItem = 10;
        }

        public static void GenerateItem()
        {
            string instructions = string.Empty;
            string PATH = "XML/" + GameManager.GameID + "/";
            switch (Difficulty)
            {
                case "Practice": PATH += "Itembank_Practice"; break;
                case "Workout": PATH += "Itembank_Workout"; break;
                case "Quiz": PATH += "Itembank_Quiz"; break;
            }
            string NODE = "Activity/Item";

            TextAsset textAsset = (TextAsset)Resources.Load(PATH, typeof(TextAsset));
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(textAsset.text);

            Questions = new List<string>();
            Corrects = new List<string>();
            Wrongs = new List<string>();

            foreach (XmlNode node in xmlDoc.SelectNodes(NODE))
            {
                foreach (XmlNode innerNode in node.ChildNodes)
                {
                    if (innerNode.Name == "Question") Questions.Add(innerNode.InnerText);
                    if (innerNode.Name == "Correct") Corrects.Add(innerNode.InnerText);
                    if (innerNode.Name == "Wrong") Wrongs.Add(innerNode.InnerText);
                }
            }
        }

        public static void NextItem()
        {
            int randomItem = Random.Range(0, Questions.Count);
            Item++;

            Question = Questions[randomItem];
            Correct = Corrects[randomItem];
            Wrong = Wrongs[randomItem];

            Questions.RemoveAt(randomItem);
            Corrects.RemoveAt(randomItem);
            Wrongs.RemoveAt(randomItem);
        }

        public static bool CheckAnswer(string answer = "")
        {
            bool correct = false;
            correct = (Correct == answer) ? true : false;
            return correct;
        }

        public static string Feedback()
        {
            string instructions = string.Empty;
            string PATH = "XML/" + GameManager.GameID + "/Feedback";
            string NODE = "Feedback/";
            string feedback = string.Empty;

            var percentage = (float)Score / TotalItem;
            percentage = percentage * 100;

            if (percentage >= 100)
                NODE += "Perfect";
            else if (percentage < 100 && percentage > 70)
                NODE += "Average";
            else if (percentage < 70)
                NODE += "Fail";

            TextAsset textAsset = (TextAsset)Resources.Load(PATH, typeof(TextAsset));
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(textAsset.text);

            feedback = xmlDoc.SelectSingleNode(NODE).InnerText +
                "\nYour score: " + Score + "/" + TotalItem;
            return feedback;
        }


        #region Internal Operations
        static int GetId()
        {
            string id = string.Empty;
            var _namespace = typeof(GameManager).Namespace.Split('.').Last();
            id = _namespace.Replace("ID", "");
            return int.Parse(id);
        }

        static string GetInstructions()
        {
            string instructions = string.Empty;
            string PATH = "XML/" + GameManager.GameID + "/Instruction";
            string NODE = "Instruction";
            TextAsset textAsset = (TextAsset)Resources.Load(PATH, typeof(TextAsset));
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(textAsset.text);

            foreach (XmlNode node in xmlDoc.SelectNodes(NODE))
            {
                foreach (XmlNode innerNode in node.ChildNodes)
                {
                    instructions = innerNode.InnerText.ToString();
                }
            }
            return instructions;
        }
        #endregion
    }
}