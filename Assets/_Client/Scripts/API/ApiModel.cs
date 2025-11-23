using System; 

using UnityEngine;


namespace PPGIA.X540.Project3.API
{
    [Serializable]
    public class ApiModel
    {
        public static T FromJson<T>(string json) =>
            JsonUtility.FromJson<T>(json);

        public static string ToJson<T>(T obj) =>
            JsonUtility.ToJson(obj);
    }

    [Serializable]
    public class Session: ApiModel
    {
        public string session_id;
        public string created_at;

        // Optional convenience properties with C#-style names:
        public string SessionId => session_id;
        public DateTime CreatedAt => DateTime.Parse(created_at);
    }

    [Serializable]
    public class ChatServicePayload : ApiModel
    {
        public string message;
    }

    public class ChatServiceResponse : ApiModel
    {
        public string session_id;
        public string message;
        public string audio_url;
        public string audio_key;
        public int expires_in;

        // Optional convenience properties with C#-style names:
        public string SessionId => session_id;
        public string Message => message;
        public string AudioUrl => audio_url;
        public string AudioKey => audio_key;
        public int ExpiresIn => expires_in;
    }

    internal enum Environment
    {
        Development,
        Production
    }
}