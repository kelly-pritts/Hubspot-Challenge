using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;

namespace Hubspot_Challenge
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            string getUri = "https://candidate.hubteam.com/candidateTest/v3/problem/dataset?userKey=984349804cd8bd48b2f5aebc8539";
            string postUri = "https://candidate.hubteam.com/candidateTest/v3/problem/result?userKey=984349804cd8bd48b2f5aebc8539";

            var requestData = await GetData(getUri);
            if (!requestData.Success)
            {
                Console.WriteLine($"Unable to process get request - {requestData.Message}");
                Console.ReadLine();
            }
            else
            {

                Response postData = SortInbox(requestData);
                string jsonPostData = JsonConvert.SerializeObject(postData);
                ApiCallResponse callResponse = PostData(postUri, jsonPostData);

                Console.WriteLine($"{callResponse.Message}");
                Console.ReadLine();

            }
        }

        public static async Task<Request> GetData(string uri)
        {
            Request request = new Request()
            {
                Success = false
            };

            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage getData = await client.GetAsync(uri);
                if (!getData.IsSuccessStatusCode)
                {
                    request.Message = getData.ReasonPhrase;
                    return request;
                }
                var responseBody = await getData.Content.ReadAsStringAsync();
                request.DataSet = JsonConvert.DeserializeObject<MyDataSet>(responseBody);

                if (request.DataSet != null)
                {
                    request.Success = true;
                }

            }
            catch (Exception ex)
            {
                request.Message = $"Exception occurred {ex.Message}";
            }


            return request;
        }

        public static ApiCallResponse PostData(string uri, object dataToPost)
        {
            ApiCallResponse callResponse = new ApiCallResponse();

            using (var client = new HttpClient())
            {
                var content = new StringContent(dataToPost.ToString(), Encoding.UTF8, "application/json");
                var result = client.PostAsync(uri, content).Result;
                callResponse.Success = result.IsSuccessStatusCode;
                callResponse.Message = result.ReasonPhrase;

            }
            return callResponse;
        }

        public static Response SortInbox(Request data)
        {
            data.DataSet.messages = data.DataSet.messages.OrderByDescending(m => m.timestamp).ToArray();
            List<int> userIds = data.DataSet.users.Select(u => u.id).ToList();
            List<Conversation> conversations = new List<Conversation>();

            foreach (int userId in userIds)
            {
                List<Message> convos = data.DataSet.messages.Where(u => u.fromUserId == userId || u.toUserId == userId).ToList();
                Message mostRecent = convos.OrderByDescending(m => m.timestamp).First();
                User user = data.DataSet.users.Where(u => u.id == userId).First();
                Conversation conversation = new Conversation()
                {
                    avatar = user.avatar,
                    totalMessages = convos.Count,
                    userId = userId,
                    lastName = user.lastName,
                    firstName = user.firstName,
                    mostRecentMessage = new Mostrecentmessage()
                    {
                        userId = mostRecent.fromUserId, //Note: I missed this small detail so it took me way longer to get a successful post!
                        content = mostRecent.content,
                        timestamp = mostRecent.timestamp,
                    },
                };
                conversations.Add(conversation);

            }
            Response sortedInbox = new Response();
            conversations = conversations.OrderByDescending(c => c.mostRecentMessage.timestamp).ToList();
            sortedInbox.conversations = conversations;

            return sortedInbox;

        }


    }

    public class ApiCallResponse
    {
        public bool Success { get; set; }
        public String Message { get; set; }
    }


    public class Request : ApiCallResponse
    {
        public MyDataSet DataSet { get; set; }
    }


    public class MyDataSet
    {
        public int userId { get; set; }
        public Message[] messages { get; set; }
        public User[] users { get; set; }
    }

    public class Message
    {
        public int fromUserId { get; set; }
        public int toUserId { get; set; }
        public long timestamp { get; set; }
        public string content { get; set; }

        public DateTime ConvertedTimeStamp
        {
            get
            {
                DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                long unixTimeStampInTicks = (timestamp / 1000 * TimeSpan.TicksPerSecond);
                return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
            }
        }
    }

    public class User
    {
        public int id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string avatar { get; set; }
    }


    public class Response
    {
        public List<Conversation> conversations { get; set; }
    }

    public class Conversation
    {
        public string avatar { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public Mostrecentmessage mostRecentMessage { get; set; }
        public int totalMessages { get; set; }
        public int userId { get; set; }
    }

    public class Mostrecentmessage
    {
        public string content { get; set; }
        public long timestamp { get; set; }
        public int userId { get; set; }
        public DateTime ConvertedTimeStamp
        {
            get
            {
                DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                long unixTimeStampInTicks = (timestamp / 1000 * TimeSpan.TicksPerSecond);
                return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
            }
        }


    }
}
