using System; 

using UnityEngine;


namespace PPGIA.X540.Project3.API
{
    [Serializable]
    public class Session
    {
        public string session_id;
        public string created_at;

        // Optional convenience properties with C#-style names:
        public string SessionId => session_id;
        public DateTime CreatedAt => DateTime.Parse(created_at);

        public static Session FromJson(string json) =>
            JsonUtility.FromJson<Session>(json);
    }
}