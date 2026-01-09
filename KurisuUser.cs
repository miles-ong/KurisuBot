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
        public int CrushRewardMin { get; set; } = 3;
        public int CrushRewardMax { get; set; } = 5;

        public int MaxQuizTokens { get; set; } = 10;
        public int QuizTokenRefreshInterval { get; set; } = 15;
        public int QuizTimeToAnswer { get; set; } = 30;
        public int QuizRewardMin { get; set; } = 20;
        public int QuizRewardMax { get; set; } = 30;

        public string BinderDescription { get; set; } = "A collection of cards.";
        public string CardCollectionEmbedColor { get; set; } = "4FC1E8";
        public string WalletEmbedColor { get; set; } = "4FC1E8";
    }
}
