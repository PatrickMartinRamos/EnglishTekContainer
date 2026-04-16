Open Interactive script and copy this codes and replace Gamemanager function to this.
NOTE: Replace the interactive code eg., "ItemBankPractice_ET1ID106" 
=========================================================================================================================

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

=========================================================================================================================

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

=========================================================================================================================

static string GetInstructions()
{
    // replace this with the correct interactive code
    string xmlContent = GameSession.CurrentManifest.GetXMLText("Instruction_ET1ID106");
    
    XmlDocument xmlDoc = new XmlDocument();
    xmlDoc.LoadXml(xmlContent);

    XmlNode node = xmlDoc.SelectSingleNode("Instruction");
    return node != null ? node.InnerText : "No Instructions Found";
}