using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;

namespace FilipinoTek.Grade2.ID101
{
    public static class GameManager
    {
        public static int TotalItems { set; get; }
        public static int Score { set; get; }
        public static string Difficulty { set; get; }
        public static string Instructions { set; get; }
        public static int Levels { set; get; }
        public static string Question { set; get; }
        public static string Correct { set; get; }
        public static string Choices { set; get; }
        private static List<string> Questions { set; get; }
        public static List<string> Corrects { set; get; }
        private static List<string> ChoicesList { set; get; }

        public static List<string> Dialouges { set; get; }
        public static string Dialouge { set; get; }

        public static int CurItem { set; get; }
        public static int CurSound { set; get; }

        public static void Initialize()
        {
            Levels = 1;
            Difficulty = "Level 1";
            Score = 0;
            TotalItems = 5;

            CurItem = 1;
            CurSound = 1;

            LoadItembanks();
            LoadInstruction();
            LoadDialouges();
        }

        public static void LoadItembanks()
        {
            string mainNode = "Itembank/" + Difficulty.Replace(" ", "") + "/Item";

            XmlDocument xmlDoc = new XmlDocument();
            TextAsset textAsset = Resources.Load("XML/" + GetId() + "/Itembanks") as TextAsset;
            xmlDoc.LoadXml(textAsset.text);

            var nodes = xmlDoc.SelectNodes(mainNode);

            Questions = new List<string>();
            Corrects = new List<string>();
            ChoicesList = new List<string>();

            foreach (XmlNode n in nodes)
            {
                foreach (XmlNode innerNode in n.ChildNodes)
                {
                    if (innerNode.Name == "Question") Questions.Add(innerNode.InnerText);
                    if (innerNode.Name == "Correct") Corrects.Add(innerNode.InnerText);
                    if (innerNode.Name == "Choices") ChoicesList.Add(RandomizeChoices(innerNode.InnerText));
                }
            }
            /*foreach (string s in Questions)
            {
                Debug.Log(s);
            }*/
        }

        public static void LoadDialouges()
        {
            string mainNode = "Dialougebank/" + Difficulty.Replace(" ", "") + "/Item";

            XmlDocument xmlDoc = new XmlDocument();
            TextAsset textAsset = Resources.Load("XML/" + GetId() + "/Dialougebanks") as TextAsset;
            xmlDoc.LoadXml(textAsset.text);

            var nodes = xmlDoc.SelectNodes(mainNode);

            Dialouges = new List<string>();

            foreach (XmlNode n in nodes)
            {
                foreach (XmlNode innerNode in n.ChildNodes)
                {
                    if (innerNode.Name == "Dialouge") Dialouges.Add(innerNode.InnerText);

                }
            }

            Debug.Log(Dialouges.Count);
        }

    


        public static void LoadInstruction()
        {
            string mainNode = "Instruction/" + Difficulty.Replace(" ", "");

           //Debug.Log(mainNode);

            XmlDocument xmlDoc = new XmlDocument();
            TextAsset textAsset = Resources.Load("XML/" + GameManager.GetId() + "/Instructions_" + Difficulty.Replace(" ", "")) as TextAsset;
            xmlDoc.LoadXml(textAsset.text);
            Instructions = xmlDoc.InnerText;
            
        }

        public static void NextItem()
        {
            // modify this depending on GDD, this will not randomize the questions
            //int random = Random.Range(0, Corrects.Count);
            Question = Questions[CurItem - 1];
            Choices = ChoicesList[CurItem - 1];
            Correct = Corrects[CurItem - 1];
            
            //Debug.Log(string.Format("Q: {0}, C: {1}, A: {2}", Question, Correct, Choices));

           /* Questions.RemoveAt(random);
            Corrects.RemoveAt(random);
            ChoicesList.RemoveAt(random);*/


            


        }



        #region Inter2nal Operations
        private static string RandomizeChoices(string choices)
        {
            string[] _choices = choices.Split('|');

            List<string> tempChoices = new List<string>();
            for (int i = 0; i < _choices.Length; i++)
            {
                tempChoices.Add(_choices[i]);
            }

            List<string> randomizedChoices = new List<string>();
            for (int i = 0; i < _choices.Length; i++)
            {
                int random = Random.Range(0, tempChoices.Count);
                randomizedChoices.Add(tempChoices[random]);

                tempChoices.RemoveAt(random);
            }

            return string.Join("|", randomizedChoices.ToArray());
        }

        public static int GetId()
        {
            string id = string.Empty;
            var _namespace = typeof(GameManager).Namespace.Split('.').Last();
            id = _namespace.Replace("ID", "");
            return int.Parse(id);
        }
        #endregion
    }
}