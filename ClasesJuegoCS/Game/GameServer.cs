using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ClasesJuegoCS.Game
{

    public class GameServer
    {

        public string Word { get; set; }
        public List<string> ImageUrls { get; set; } = new();

        public List<Player> Players { get; set; } = new();

        public bool Playing { get; private set; } = false;
        public TimeSpan PlayTime { get; set; } = TimeSpan.FromSeconds(30);
        public ulong PlayScore { get; set; } = 100;

        Stopwatch counter = new();
        HttpListener listener = null;

        public ulong GetScoreByElpasedTime(TimeSpan elapsed)
        {
            if (elapsed > PlayTime) return 0;
            return (ulong)((PlayTime - elapsed) * PlayScore / PlayTime);
        }

        /// <summary>
        /// Agrega un nuevo jugador a la partida actual.
        /// </summary>
        /// <param name="playername">El nombre del jugador que se quiere agregar</param>
        /// <returns>El nombre del jugadador modificado si es necesario</returns>
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

        public void StartListen()
        {
            if (listener == null)
            {
                listener = new();
                listener.Prefixes.Add("http://GuessThePicture:8000/");
                listener.Start();
                Task.Run(() =>
                {
                    while (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        if (context.Request.RawUrl == "/Join" && context.Request.HttpMethod == "POST")
                        {
                            var playername = context.Request.QueryString["Name"];
                            if (playername != null)
                            {
                                var name = AddPlayerByName(playername);
                                var json = JsonSerializer.Serialize(name);
                                var data = Encoding.UTF8.GetBytes(json);
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                context.Response.OutputStream.Write(data);
                                context.Response.ContentType = "application/json";
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                        else if (context.Request.RawUrl == "/Guessing" && context.Request.HttpMethod == "GET")
                        {
                            if (Playing)
                            {
                                var json = JsonSerializer.Serialize(ImageUrls);
                                var data = Encoding.UTF8.GetBytes(json);
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                context.Response.OutputStream.Write(data);
                                context.Response.ContentType = "application/json";
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        }
                        else if (context.Request.RawUrl == "/Play" && context.Request.HttpMethod == "POST")
                        {
                            var playername = context.Request.QueryString["Name"];
                            var guess = context.Request.QueryString["Guess"];
                            if (playername != null && guess != null)
                            {
                                var guessed = PlayerGuess(playername, guess);
                                if (guessed == true)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                                }
                                else if (guessed == false)
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                }
                            }
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                        context.Response.Close();
                    }
                });
            }
        }

        public void StopListen()
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }

        public void Stop()
        {
            if (Playing)
            {
                counter.Stop();
                Playing = false;
            }
        }

        /// <summary>
        /// Establece la puntacion de todos los jugadores a 0 sin sacarlos de la partica actual.
        /// </summary>
        public void ResetScores()
        {
            Players.ForEach(x => x.Score = 0);
        }

        /// <summary>
        /// Saca de la partida actual a todos los jugadores.
        /// </summary>
        public void ResetPlayers()
        {
            Players.Clear();
        }

        /// <summary>
        /// Inteta adivinar la frase para un jugador en especifico por nombre.
        /// </summary>
        /// <param name="playername">El nombre del jugador que esta intentando adivinar</param>
        /// <param name="guess">La frase que con la que el jugador esta intentando adivinar</param>
        /// <returns>True si ha logrado adivinar. False si no logro adivinar. Null si el jugador no existe</returns>
        public bool? PlayerGuess(string playername, string guess)
        {
            var elpased = counter.Elapsed;
            var player = Players.FirstOrDefault(x => x.Name == playername);
            if (player != null)
            {
                if (guess == Word)
                {
                    player.Score += GetScoreByElpasedTime(elpased);
                    return true;
                }
                return false;
            }
            return null;
        }

        public bool RemovePlayerByName(string name)
        {
            return Players.RemoveAll(x => x.Name == name) > 0;
        }

    }

}