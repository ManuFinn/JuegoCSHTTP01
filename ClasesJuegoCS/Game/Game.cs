using System;
using System.Diagnostics;
using System.Net.Http;

namespace ClasesJuegoCS.Game
{

    public class Game
    {

        public string Frase { get; set; }
        public string UrlImageA { get; set; }
        public string UrlImageB { get; set; }
        public string UrlImageC { get; set; }
        public string UrlImageD { get; set; }

        public List<Player> Players { get; set; } = new();

        public bool Playing { get; private set; } = false;
        public TimeSpan PlayTime { get; set; } = TimeSpan.FromSeconds(30);
        public ulong PlayScore { get; set; } = 100;

        Stopwatch counter = new();

        public ulong GetScoreByElpasedTime(TimeSpan elapsed) {
            if(elapsed > PlayTime) return 0;
            return (ulong)((PlayTime - elapsed) * PlayScore / PlayTime);
        }

        public string AddPlayerByName(string playername)
        {
            Player player = new()
            {
                Score = 0
            };
            var count = Players.Count(x => x.Name == playername);
            if (count > 0)
            {
                player.Name = $"{playername} {count + 1}";
            }
            else
            {
                player.Name = playername;
            }
            Players.Add(player);
            return player.Name;
        }

        public void Start()
        {
            if (!Playing)
            {
                counter.Restart();
                Playing = true;
            }
        }

        public bool? PlayerGuess(string playername, string guess) {
            var elpased = counter.Elapsed;
            var player = Players.FirstOrDefault(x => x.Name == playername);
            if(player != null) {
                if(guess ==Frase) {
                    player.Score += GetScoreByElpasedTime(elpased);
                    return true;
                }
                return false;
            }
            return null;
        }

    }

}