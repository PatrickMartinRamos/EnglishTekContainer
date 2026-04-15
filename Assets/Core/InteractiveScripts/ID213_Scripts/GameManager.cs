using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using EnglishTek.Core;

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
            string manifestKey = "";
            switch (Difficulty)
            {
                case "Practice": manifestKey = "ItemBankPractice_ET1ID213"; break;
                case "Workout": manifestKey = "ItembankWorkout_ET1ID213"; break;
                case "Quiz": manifestKey = "ItembankQuiz_ET1ID213"; break;
            }
            string NODE = "Activity/Item";

            string xmlContent = GameSession.CurrentManifest.GetXMLText(manifestKey);

            if(string.IsNullOrEmpty(xmlContent))
            {
                Debug.LogError("XML content is empty for manifest key: " + manifestKey);
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

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
            // replace this with the correct interactive code
            string xmlContent = GameSession.CurrentManifest.GetXMLText("Feedback_ET1ID213");
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            string NODE = "Feedback/";
            var percentage = (float) Score / TotalItem * 100;

            if (percentage >= 100) NODE += "Perfect";
            else if (percentage > 70) NODE += "Average";
            else NODE += "Fail";

            string feedback = xmlDoc.SelectSingleNode(NODE).InnerText + 
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
            // replace this with the correct interactive code
            string xmlContent = GameSession.CurrentManifest.GetXMLText("Instruction_ET1ID213");
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            XmlNode node = xmlDoc.SelectSingleNode("Instruction");
            return node != null ? node.InnerText : "No Instructions Found";
        }
        #endregion
    }
}