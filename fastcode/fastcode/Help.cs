using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fastcode
{
    public struct HelpTopic
    {
        public string Name;
        public string[] Tags;
        public Dictionary<string, string> Information;
    }

    public class Help
    {
        public static Dictionary<string, HelpTopic> topicHelpPairs = new Dictionary<string, HelpTopic>();

        public static void addTopic(string identifier, string tags, string information)
        {
            if(topicHelpPairs.ContainsKey(identifier))
            {
                throw new Exception("Help Documentation already contains information regarding the topic '" + identifier + "'.");
            }
            HelpTopic topic = new HelpTopic();
            topic.Name = identifier;
            topic.Tags = tags.Split(',');
            topic.Information = new Dictionary<string, string>();
            string[] parts = information.Split(';');
            foreach(string part in parts)
            {
                string[] partsplit = part.Split(':');
                if(partsplit.Length != 2)
                {
                    throw new Exception("Help feilds must include an descriptor and information speperated by a colon, ':'!");
                }
                topic.Information.Add(partsplit[0], partsplit[1]);
            }
        }

        public static void addTopic(string identifier, string tags,string arguments, string synopsis)
        {
            if (topicHelpPairs.ContainsKey(identifier))
            {
                throw new Exception("Help Documentation already contains information regarding the topic '" + identifier + "'.");
            }
            HelpTopic topic = new HelpTopic();
            topic.Name = identifier;
            topic.Tags = tags.Split(',');
            topic.Information = new Dictionary<string, string>();
            topic.Information.Add("args", arguments);
            topic.Information.Add("synopsis", synopsis);
        }

        private static int countMatchingKeywords(string[] keywords, HelpTopic topic)
        {
            int count = 0;
            foreach(string tag in topic.Tags)
            {
                foreach(string keyword in keywords)
                {
                    if(tag.ToUpper() == keyword.ToUpper())
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static HelpTopic[] SearchForTopic(string term)
        {
            string[] keywords = term.Split(' ');
            HelpTopic[] toSearch = topicHelpPairs.Values.ToArray();
            for (int i = 0; i < toSearch.Length-1; i++)
            {
                if(countMatchingKeywords(keywords,toSearch[i]) < countMatchingKeywords(keywords, toSearch[i+1]))
                {
                    HelpTopic pivot = toSearch[i];
                    toSearch[i] = toSearch[i + 1];
                    toSearch[i + 1] = pivot;
                }
            }
            return toSearch;
        }
    }
}
