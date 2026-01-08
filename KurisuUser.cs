using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KurisuBot
{
    public class KurisuUser
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public KurisuUserSettings Settings { get; set; }
        public DateTime LastQuizRegen { get; set; }

        private int _coins;
        private int _dustR;
        private int _dustSR;
        private int _dustSSR;
        private int _dustUR;
        private int _quizTokens;

        public KurisuUser() { }

        public KurisuUser(long id, string username)
        {
            Id = id;
            Username = username;
            Settings = new();
            LastQuizRegen = DateTime.UtcNow;
        }
    }

    public class KurisuUserSettings
    {

    }
}
