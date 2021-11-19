using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace ClasesJuegoCS.Game
{

    public class GameServer
    {

        public Tablero Hidden { get; set; } = new();
        public Tablero Visible { get; set; } = null;

        public List<Player> Players { get; private set; } = new();

        public bool Playing { get; private set; } = false;
        public TimeSpan PlayTime { get; set; } = TimeSpan.FromSeconds(30);
        public ulong PlayScore { get; set; } = 100;

        Stopwatch counter = new();
        HttpListener listener = null;
        System.Timers.Timer timer = new();

        public GameServer()
        {
            timer.Elapsed += (s, e) =>
            {
                Stop();
            };
        }

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
                Playing = true;
                if(!Hidden.Validate()) {
                    throw new InvalidProgramException("Las operaciones del crucigrama deben ser validas");
                }
                Visible = Hidden.HideRandom();
                Players.ForEach(x => x.Guessed = false);
                timer.Interval = PlayTime.TotalMilliseconds;
                timer.Start();
                counter.Restart();
            }
        }

        public void StartListen()
        {
            if (listener == null)
            {
                listener = new();
                listener.Prefixes.Add("http://localhost:8000/");
                listener.Start();
                Task.Run(() =>
                {
                    while (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        if (context.Request.Url.LocalPath == "/Join")
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
                        }
                        else if (context.Request.Url.LocalPath == "/Guessing")
                        {
                            if (Playing)
                            {
                                var json = JsonSerializer.Serialize(Visible.Numbers);
                                var data = Encoding.UTF8.GetBytes(json);
                                context.Response.StatusCode = (int)HttpStatusCode.OK;
                                context.Response.OutputStream.Write(data);
                                context.Response.ContentType = "application/json";
                            }
                            else
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Conflict;

                            }
                        }
                        else if (context.Request.Url.LocalPath == "/Play" && context.Request.HttpMethod == "POST")
                        {
                            if (Playing)
                            {
                                /**/
                                var buffer = new byte[1024];
                                context.Request.InputStream.Read(buffer, 0, buffer.Length);
                                var guessjson = Encoding.UTF8.GetString(buffer);
                                var guess = JsonSerializer.Deserialize<Jugada>(guessjson);
                                /**/
                                if (guess.Player != null && guess.Numbers != null)
                                {
                                    var guessed = PlayerGuess(guess);
                                    if (guessed == true)
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                                    }
                                    else if (guessed == false)
                                    {
                                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                    }
                                }
                            }
                        }
                        else if (context.Request.Url.LocalPath == "/Leave" && context.Request.HttpMethod == "DELETE")
                        {
                            var playername = context.Request.QueryString["Name"];
                            if (playername != null)
                            {
                                if (RemovePlayerByName(playername))
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.OK;

                                }
                                else
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                                }
                            }
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
                listener = null;
            }
        }

        public void Stop()
        {
            if (Playing)
            {
                Playing = false;
                timer.Stop();
                counter.Stop();
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
        public bool? PlayerGuess(Jugada guess)
        {
            var elpased = counter.Elapsed;
            var player = Players.FirstOrDefault(x => x.Name == guess.Player);
            if (player != null && !player.Guessed)
            {
                if (Array.Equals(Hidden.Numbers, guess.Numbers))
                {
                    player.Guessed = true;
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