using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using EnglishTek.Core;

namespace EnglishTek.Grade1.ID106
{
    
    //for new interactive scripts, we will pull instructions from the bundle instead of hardcoding them in the script. This allows for easier updates and localization in the future.
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
        // Current choices
        public static string Choices { set; get; }

        // List of questions
        static List<string> Questions { set; get; }
        // List of correct answers
        static List<string> Corrects { set; get; }
        // List of wrong1
        static List<string> Wrongs1 { set; get; }
        // List of wrong2
        static List<string> Wrongs2 { set; get; }
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
            // CHANGE: Instead of PATH strings for Resources.Load, we use our Manifest Keys
            string manifestKey = "";
            switch (Difficulty)
            {
                                                // replace this with the correct interactive code
                case "Practice": manifestKey = "ItemBankPractice_ET1ID106"; break;
                case "Workout": manifestKey = "ItembankWorkout_ET1ID106"; break;
                case "Quiz": manifestKey = "ItembankQuiz_ET1ID106"; break;
            }

            // GET DATA FROM BUNDLE
            string xmlContent = GameSession.CurrentManifest.GetXMLText(manifestKey);
            
            if (string.IsNullOrEmpty(xmlContent)) {
                Debug.LogError("Could not find XML in Bundle for key: " + manifestKey);
                return;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            string NODE = "Activity/Item";
            Questions = new List<string>();
            Corrects = new List<string>();
            Wrongs1 = new List<string>();
            Wrongs2 = new List<string>();

            foreach (XmlNode node in xmlDoc.SelectNodes(NODE))
            {
                foreach (XmlNode innerNode in node.ChildNodes)
                {
                    if (innerNode.Name == "Question") Questions.Add(innerNode.InnerText);
                    if (innerNode.Name == "Correct") Corrects.Add(innerNode.InnerText);
                    if (innerNode.Name == "Wrong1") Wrongs1.Add(innerNode.InnerText);
                    if (innerNode.Name == "Wrong2") Wrongs2.Add(innerNode.InnerText);
                }
            }
        }

        public static void NextItem()
        {
            int randomItem = Random.Range(0, Corrects.Count);
            Item++;

            Question = Questions[randomItem];
            Correct = Corrects[randomItem];
            Choices = GetChoices(randomItem);

            Questions.RemoveAt(randomItem);
            Corrects.RemoveAt(randomItem);
            Wrongs1.RemoveAt(randomItem);
            Wrongs2.RemoveAt(randomItem);
        }

        static string GetChoices(int index)
        {
            string choices = "";
            List<string> tempChoices = new List<string>();
            tempChoices.Add(Corrects[index]);
            tempChoices.Add(Wrongs1[index]);
            tempChoices.Add(Wrongs2[index]);

            for (int i = 0; i < 3; i++)
            {
                int random = Random.Range(0, tempChoices.Count);
                if (choices == "")
                    choices += tempChoices[random];
                else
                    choices += "," + tempChoices[random];
                tempChoices.RemoveAt(random);

            }
            return choices;
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
            string xmlContent = GameSession.CurrentManifest.GetXMLText("Feedback_ET1ID106");
            
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


        #region Inter2nal Operations
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
            string xmlContent = GameSession.CurrentManifest.GetXMLText("Instruction_ET1ID106");
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            XmlNode node = xmlDoc.SelectSingleNode("Instruction");
            return node != null ? node.InnerText : "No Instructions Found";
        }
        #endregion
    }
}
